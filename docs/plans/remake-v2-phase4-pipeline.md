# Phase 4 — Application Pipeline + Compat Plugins Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans`. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Wire all Phase 3 infrastructure into an `ExtractionPipeline` (9 stages) driven by a single `ExtractionContext` per run (no static mutable state). Port 7 legacy compat plugins via explicit DI registration (replacing reflection scan). End-to-end test against `samples/sample-mod/` must match the Phase 0 baseline snapshot byte-for-byte.

**Architecture:** Pipeline-of-stages pattern: each `IExtractionStage` mutates an `ExtractionContext` (which owns the combined XDocument, parent lookup, results list, cancellation, progress). Stages dispatched in fixed order. Compat plugins register into a `CompatRegistry` at startup and inject into `CompatPreProcessStage` / `CompatPostProcessStage`. ModDiscoveryService wraps `IModLister` for the Application layer.

**Tech Stack:** .NET 10 · Microsoft.Extensions.DependencyInjection · xunit.v3 · FluentAssertions 6.12.2 · Verify.XunitV3 · System.Xml.Linq.

**Branch:** `feat/remake-v2` at Phase 3 verification gate (255 tests).

**Legacy references (read-only):**
- `legacy/RimworldExtractorInternal/Extractor.cs` (374L) — main pipeline: `ExtractTranslationData`, `PrepareDefs`, `ExtractDefs`, `ExtractKeyed`, `ExtractStrings`, `ExtractPatches`, `Reset`
- `legacy/RimworldExtractorInternal/Compats/*.cs` (10 files) — 7 concrete compats + `BaseCompat` + `CompatManager` + `[CompatPriorityAttribute]`

---

## Sub-batch overview

| # | Sub-batch | Tasks | Outcome |
|---|-----------|-------|---------|
| 4A | Pipeline primitives | T1-T4 | `ExtractionRequest/Result/Context`, `IExtractionStage`, `ExtractionPipeline` |
| 4B | Setup stages | T5-T7 | Reset / LoadReferenceDefs / DoPrePatch stages |
| 4C | Inheritance + extract stages | T8-T11 | XmlInheritance / ExtractDefs / ExtractKeyed / ExtractStrings / ExtractPatches |
| 4D | Compat plugin API | T12-T13 | `ICompatPlugin` interface, `[CompatPriority]` attribute, `CompatRegistry`, Pre/Post stages |
| 4E | Compat plugin ports | T14-T20 | 7 plugins: MVCF, Verb, FactionDef, NoTranslate, NodeReplacement, ScenarioDef, AncientMarket |
| 4F | Pipeline assembly | T21-T22 | `ExtractionPipeline` wiring 9 stages; replace Phase 1's `NoOpExtractionPipeline` |
| 4G | ModDiscoveryService + Application DI | T23-T25 | `ModDiscoveryService`, extend `AddApplication` for real pipeline + compats |
| 4H | Snapshot regression test | T26-T27 | `SampleModSnapshotTests` — THE critical parity gate |
| 4I | Verification gate | T28 | Full build/test/coverage/format + push |

**~28 tasks, 9 subagent dispatches.**

---

## Key design decisions

1. **ExtractionContext is per-run.** Created at `pipeline.RunAsync(request, ct)`, disposed when the method returns. Holds XDocument, parent lookup, entries list, progress reporter. No static.

2. **Stages are sync internally** (take `ExtractionContext ctx`, mutate it) because all their dependencies are sync (`XDocument`, `XElement`). Pipeline wraps them in `Task.Run` only if CPU parallelism matters (it doesn't here).

3. **Compat plugins** registered explicitly via `services.AddCompatPlugin<MvcfCompatPlugin>()`. No reflection. The `[CompatPriority(n)]` attribute on the plugin controls ordering at resolution time (`CompatRegistry` orders plugins on construction).

4. **Output strategy is Phase 5's concern** — Phase 4 stops at producing a `List<TranslationEntry>`. Writing files is orchestrated at the UI/CLI level. The snapshot test compares the list directly against the Phase 0 snapshot JSON.

5. **Plugin model keeps `BaseCompat` semantics** — `DoPreProcess(XDocument)` + `DoPostProcess(IEnumerable<TranslationEntry>)`. Interface-based, not abstract class (more testable, no virtual-method default behavior).

6. **Parity with legacy is hard** — legacy emits duplicates and lets the final `DistinctBy` filter them. We port this behavior faithfully. The snapshot test is the truth; if a test fails, the fix is to match legacy behavior, not to "improve" it.

---

## Group 4A — Pipeline primitives

### Task 1: ExtractionRequest + ExtractionResult records (replace Phase 1 stubs)

**Files:**
- Modify: `src/RimworldExtractor.Domain/Abstractions/IExtractionPipeline.cs`

Phase 1 has `ExtractionRequest(string ModPath)` + `ExtractionResult(IReadOnlyList<string> Messages)`. Replace with richer types:

```csharp
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Rules;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Domain.Abstractions;

public sealed record ExtractionRequest(
    ModMetadata Target,
    IReadOnlyList<ExtractableFolder> SelectedFolders,
    IReadOnlyList<ModMetadata>? ReferenceMods,
    AppSettings Settings);

public sealed record ExtractionResult(IReadOnlyList<TranslationEntry> Entries)
{
    public static ExtractionResult Empty { get; } = new(Array.Empty<TranslationEntry>());
}

public sealed record ExtractionProgress(double Percentage, string Message);

public interface IExtractionPipeline
{
    Task<ExtractionResult> RunAsync(
        ExtractionRequest request,
        IProgress<ExtractionProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
```

Update Phase 1's `NoOpExtractionPipeline` to return `ExtractionResult.Empty` (already does via compat). Update `DependencyInjectionTests` test if signature changes break it.

Commit: `feat(domain): enrich ExtractionRequest/Result for real pipeline`

### Task 2: ExtractionContext (per-run mutable state)

**Files:**
- Create: `src/RimworldExtractor.Application/Extraction/ExtractionContext.cs`
- Test: `tests/RimworldExtractor.Application.Tests/Extraction/ExtractionContextTests.cs`

```csharp
using System.Xml.Linq;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Application.Extraction;

/// <summary>
/// Mutable state for one extraction run. Replaces legacy <c>Extractor.CombinedDefs</c>,
/// <c>Extractor.ParentNodeLookUp</c>, <c>Extractor._isOfficialContent</c>,
/// <c>PatchOperations.DefsAddedByPatches</c> — all were static globals before.
/// </summary>
public sealed class ExtractionContext
{
    public ExtractionRequest Request { get; }
    public IProgress<ExtractionProgress>? Progress { get; }
    public CancellationToken CancellationToken { get; }

    public XDocument CombinedDefs { get; } = new(new XElement("Defs"));
    public Dictionary<string, XElement> ParentLookup { get; } = new(StringComparer.Ordinal);
    public List<TranslationEntry> Results { get; } = new();
    public List<XElement> DefsAddedByPatches { get; } = new();

    public bool IsOfficialContent => Request.Target.IsOfficialContent;

    public ExtractionContext(
        ExtractionRequest request,
        IProgress<ExtractionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Request = request;
        Progress = progress;
        CancellationToken = cancellationToken;
    }

    public void ReportProgress(double percentage, string message)
        => Progress?.Report(new ExtractionProgress(percentage, message));
}
```

Tests (3 cases): construction stores request; `Results` starts empty; `ReportProgress` forwards to IProgress.

Commit: `feat(application): add ExtractionContext (per-run mutable state)`

### Task 3: IExtractionStage interface

**Files:**
- Create: `src/RimworldExtractor.Application/Extraction/IExtractionStage.cs`

```csharp
namespace RimworldExtractor.Application.Extraction;

/// <summary>
/// One stage of the extraction pipeline. Stages run in fixed order; each may mutate
/// <see cref="ExtractionContext"/>. Implementations should be stateless (injected dependencies only)
/// so a single instance can be reused across runs.
/// </summary>
public interface IExtractionStage
{
    /// <summary>Short, stable identifier used in progress messages and logs.</summary>
    string Name { get; }

    Task ExecuteAsync(ExtractionContext context);
}
```

No test. Commit: `feat(application): add IExtractionStage interface`

### Task 4: ExtractionPipeline orchestrator

**Files:**
- Modify: `src/RimworldExtractor.Application/Extraction/NoOpExtractionPipeline.cs` → rename to `ExtractionPipeline.cs` (or replace)
- Actually, keep `NoOpExtractionPipeline.cs` for now (used by Phase 1 smoke test). Create a NEW `src/RimworldExtractor.Application/Extraction/ExtractionPipeline.cs`.

```csharp
using RimworldExtractor.Domain.Abstractions;

namespace RimworldExtractor.Application.Extraction;

public sealed class ExtractionPipeline : IExtractionPipeline
{
    private readonly IReadOnlyList<IExtractionStage> _stages;

    public ExtractionPipeline(IEnumerable<IExtractionStage> stages)
    {
        _stages = stages.ToArray();
    }

    public async Task<ExtractionResult> RunAsync(
        ExtractionRequest request,
        IProgress<ExtractionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var context = new ExtractionContext(request, progress, cancellationToken);
        var total = _stages.Count;

        for (int i = 0; i < _stages.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stage = _stages[i];
            context.ReportProgress(
                percentage: (double)i / total,
                message: $"Stage {i + 1}/{total}: {stage.Name}");
            await stage.ExecuteAsync(context);
        }

        context.ReportProgress(1.0, "Complete");
        return new ExtractionResult(context.Results.ToArray());
    }
}
```

Test: inject 2 fake stages (via NSubstitute), run pipeline, assert both invoked in order, progress reported, final result contains context.Results.

Commit: `feat(application): add ExtractionPipeline orchestrator`

---

## Group 4B — Setup stages

Each stage is a separate file in `src/RimworldExtractor.Application/Extraction/Stages/`. All follow same pattern: inject dependencies, implement `ExecuteAsync`.

### Task 5: LoadReferenceDefsStage

**Legacy ref:** `Extractor.DefsUtils.cs:10-52` + `Extractor.cs:103-148` PrepareDefs portion.

Port to a stage: read all `.xml` files in `request.ReferenceMods` via `IFileSystem`, parse via `IXmlDefParser`, import nodes into `context.CombinedDefs` with `Reference="True"` attribute. Populate `context.ParentLookup` with nodes having `Name` attribute.

Injected deps: `IFileSystem`, `IXmlDefParser`, `IModLister` (to get reference folders for each ref mod), `FileSystemGateway` (for `DescendantFiles`), `ILogger<LoadReferenceDefsStage>`.

Include logic for loading Defs from `context.Request.SelectedFolders` (the mod itself) also — this is `PrepareDefs` first part (lines 111-148).

Test: fixture with 2 ref mods; stage populates combined defs with imported nodes + parent lookup.

Commit: `feat(application): add LoadReferenceDefsStage`

### Task 6: ApplyPrePatchesStage

**Legacy ref:** `Extractor.cs:155-173` DoPrePatch.

Runs patches in `prePatchMode=true` over the combined defs BEFORE inheritance. The patches emit no entries in prePatchMode but DO add defs to `context.DefsAddedByPatches`. Then updates `context.ParentLookup` with any `Name`-bearing nodes from patch-added defs.

Injected deps: `IXPatchProcessor`, `IXmlDefParser`, `IFileSystem`, `FileSystemGateway`.

Note: prePatches = `request.SelectedFolders.Where(f => Path.GetFileName(f.FolderName) == "Patches")`. Also include reference mods' patches folders.

Test: fixture with a patch that adds an abstract def with Name; stage inserts it into CombinedDefs + ParentLookup.

Commit: `feat(application): add ApplyPrePatchesStage`

### Task 7: ResolveInheritanceStage

**Legacy ref:** `Extractor.DefsUtils.cs:284-371` DoXmlInheritance — already ported in Phase 3 as `IXmlInheritanceResolver`.

Stage just calls `_resolver.Resolve(context.CombinedDefs)` and replaces `context.CombinedDefs` with the result. But since `CombinedDefs` is `{ get; }` (read-only ref on the context), we need to allow replacing it.

**Action:** change `ExtractionContext.CombinedDefs` to `{ get; set; }` OR provide a `ReplaceCombinedDefs(XDocument)` method. Prefer the method for encapsulation.

Update Task 2's `ExtractionContext`:
```csharp
public XDocument CombinedDefs { get; private set; } = new(new XElement("Defs"));
public void ReplaceCombinedDefs(XDocument doc) => CombinedDefs = doc;
```

Then `ResolveInheritanceStage.ExecuteAsync` does:
```csharp
var resolved = _resolver.Resolve(context.CombinedDefs);
context.ReplaceCombinedDefs(resolved);
```

Test: simple fixture where inheritance resolves a parent-name-referenced child.

Commit: `feat(application): add ResolveInheritanceStage`

---

## Group 4C — Extract stages

### Task 8: ExtractDefsStage

**Legacy ref:** `Extractor.cs:190-230` ExtractDefsInternal.

For each top-level `<ThingDef>`/etc. child of `CombinedDefs` where `Reference != "True"`:
1. Get defName from `<defName>` child element → skip if missing (log warn unless `Name == "SongDef"`)
2. Build `RequiredMods` from `RequiredPackageId` attribute (packageIds split by `,`)
3. ClassName: `node.Attribute("Class")?.Value ?? node.Name.LocalName`, capitalize first letter
4. Call `_defExtractor.Extract(defName, className, node, rules, translationHandles, enableTKey, isOfficialContent)` — iterate yielding entries
5. For each emitted entry, combine with the root `RequiredMods` via `RequiredMods.Combine(other)`

Injected: `IXmlDefExtractor`.

Reads `rules`, `translationHandles`, `enableTKey` from `context.Request.Settings.Extraction`.

Test: extract labels from fixture defs; assert correct ClassName / Node / Original.

Commit: `feat(application): add ExtractDefsStage`

### Task 9: ExtractKeyedStage

**Legacy ref:** `Extractor.cs:232-257`.

For each folder in `SelectedFolders` where `Path.GetFileName(FolderName) == "Keyed"`:
- BFS enumerate `.xml` files via `FileSystemGateway.DescendantFiles`
- For each file, parse via `IXmlDefParser`, iterate `<LanguageData>` children
- Yield `TranslationEntry("Keyed", node.Name, node.Value, null, requiredMods, sourceFile)` where:
  - requiredMods from folder's `RequiredPackageId` attribute
  - sourceFile = filename (without .xml) ONLY when `context.IsOfficialContent`, else null

Commit: `feat(application): add ExtractKeyedStage`

### Task 10: ExtractStringsStage

**Legacy ref:** `Extractor.cs:259-284`.

For each folder where `FolderName endsWith "Strings"`:
- BFS enumerate `.txt` files
- For each file:
  - `nodeName` = relative path with `\` replaced by `.`, minus extension
  - Each line `i` yields `TranslationEntry("Strings", $"{nodeName}.{i}", line, null, requiredMods, null)`

Commit: `feat(application): add ExtractStringsStage`

### Task 11: ExtractPatchesStage

**Legacy ref:** `Extractor.cs:287-372` (complex — includes post-patch def extraction via inheritance pass).

For each "Patches" folder:
1. Parse all XML files, concat `<Operation>` nodes into a single `<Patch>` doc
2. For each operation, call `_patchProcessor.Apply(combinedDefs, operation, ...)`
3. Emit the `PatchProcessingResult.Entries` with RequiredMods combined
4. `PatchProcessingResult.DefsAddedByPatches` — if non-empty:
   - Run compat PreProcessing on the Patches doc (deferred to Phase 4 compat pipeline stage — or skip for Phase 4 minimum; document as a known gap)
   - Append each def to `context.CombinedDefs`
   - Update `context.ParentLookup` with Name-bearing defs
   - Run `IXmlInheritanceResolver.Resolve` on just those defs
   - Re-run `ExtractDefsStage.ExecuteAsync`-equivalent logic on those defs only (but prefixed with `Patches.` class name)

Pragma: for Phase 4 MVP, implement patches-emit-entries fully but the "Defs added by Patches" post-processing can be stubbed if sample-mod baseline doesn't exercise it. **Actually CHECK:** sample-mod's Patches/Patch_ThingDefs.xml uses `PatchOperationAdd` targeting `SampleMod_WoodenSpear` (adds `jobString`) and `PatchOperationReplace` (replaces label). Neither adds a new def. So the post-patch def extraction path is dormant for the baseline. Stub with a TODO comment referring to Phase 5 if needed.

Commit: `feat(application): add ExtractPatchesStage`

---

## Group 4D — Compat plugin API

### Task 12: ICompatPlugin + CompatPriorityAttribute + extension

**Files:**
- Create: `src/RimworldExtractor.Plugins/ICompatPlugin.cs`
- Create: `src/RimworldExtractor.Plugins/CompatPriorityAttribute.cs`
- Create: `src/RimworldExtractor.Plugins/PluginRegistration.cs`

```csharp
using System.Xml.Linq;
using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Plugins;

/// <summary>
/// Mod-specific translation-extraction hook. Replaces legacy <c>BaseCompat</c> abstract class with
/// an interface so implementations are testable and DI-friendly.
/// </summary>
public interface ICompatPlugin
{
    /// <summary>Transform the combined Defs XDocument before extraction (e.g. mutate class names, strip nodes).</summary>
    void PreProcess(XDocument combinedDefs);

    /// <summary>Transform extracted entries (add/remove/rewrite). Must be pure-functional over the input sequence.</summary>
    IEnumerable<TranslationEntry> PostProcess(IEnumerable<TranslationEntry> entries);
}

/// <summary>
/// Default implementations for plugins that only need one of the hooks. Implementations that don't
/// override a method get the identity behavior (pass-through).
/// </summary>
public abstract class CompatPluginBase : ICompatPlugin
{
    public virtual void PreProcess(XDocument combinedDefs) { }
    public virtual IEnumerable<TranslationEntry> PostProcess(IEnumerable<TranslationEntry> entries) => entries;
}

/// <summary>Sort-key for compat plugins. Lower values run first. Default 100.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class CompatPriorityAttribute : Attribute
{
    public int Priority { get; }
    public CompatPriorityAttribute(int priority) => Priority = priority;
}
```

`PluginRegistration.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace RimworldExtractor.Plugins;

public static class PluginRegistration
{
    public static IServiceCollection AddCompatPlugin<T>(this IServiceCollection services)
        where T : class, ICompatPlugin
    {
        services.AddSingleton<ICompatPlugin, T>();
        return services;
    }
}
```

Test: register 2 plugins with different `[CompatPriority]` values; enumerate `IEnumerable<ICompatPlugin>`; verify both resolved.

Commit: `feat(plugins): add ICompatPlugin + CompatPriorityAttribute + AddCompatPlugin extension`

### Task 13: CompatRegistry + CompatPreProcessStage + CompatPostProcessStage

**Files:**
- Create: `src/RimworldExtractor.Application/Compat/CompatRegistry.cs`
- Create: `src/RimworldExtractor.Application/Extraction/Stages/CompatPreProcessStage.cs`
- Create: `src/RimworldExtractor.Application/Extraction/Stages/CompatPostProcessStage.cs`

`CompatRegistry`: takes `IEnumerable<ICompatPlugin>` via DI, orders by `[CompatPriority]` (default 100 when absent), exposes `Ordered`.

Stages:
- `CompatPreProcessStage`: iterates ordered plugins, calls `PreProcess(context.CombinedDefs)`.
- `CompatPostProcessStage`: reduces `context.Results` through each plugin's `PostProcess` sequentially.

Note: legacy runs `DoPostProcessing` in both `ExtractDefs` and `ExtractPatches` (not as an end-of-pipeline stage). For Phase 4, we run it at end-of-pipeline only (simplification) — BUT this may break parity for sample-mod. If snapshot test fails, insert a pre-strings DoPostProcessing stage (`CompatPostProcessDefsOnlyStage`) that processes only entries with ClassName not in {Keyed, Strings, Patches.*} mid-pipeline. For Phase 4 MVP, start simple.

Commit: `feat(application): add CompatRegistry + CompatPreProcessStage + CompatPostProcessStage`

---

## Group 4E — Compat plugin ports (7 plugins)

Each of Tasks 14-20 ports one legacy compat to `src/RimworldExtractor.Plugins/BuiltIn/`. Pattern: read legacy file, port to `CompatPluginBase` subclass, adjust for `XElement` (not `XmlDocument`), add `[CompatPriority(n)]` if legacy had it.

- **Task 14: `MvcfCompatPlugin`** — port `Compat_MVCF.cs:1-137`. Legacy's `MVCFForm` inner class moves to a `readonly record struct`. Post-processing: for each entry with `Comp_VerbProps.verbProps` path ending in `label`, find matching `visualLabel`/`description`/`verb.label` entries and emit synthetic ones. Commit: `feat(plugins): add MvcfCompatPlugin (Verb/VerbProps translation unification)`
- **Task 15: `VerbCompatPlugin`** — port `Compat_Verb.cs` (39L). Smaller version of MVCF. Commit: `feat(plugins): add VerbCompatPlugin`
- **Task 16: `FactionDefCompatPlugin`** — port `Compat_FactionDef.cs` (34L). Commit: `feat(plugins): add FactionDefCompatPlugin`
- **Task 17: `NoTranslateCompatPlugin`** — port `Compat_NoTranslate.cs` (24L). Filters entries. Commit: `feat(plugins): add NoTranslateCompatPlugin`
- **Task 18: `NodeReplacementCompatPlugin`** — port `Compat_NodeReplacement.cs` (50L). Uses `AppSettings.Extraction.NodeReplacements` for the rule set. Commit: `feat(plugins): add NodeReplacementCompatPlugin`
- **Task 19: `ScenarioDefCompatPlugin`** — port `Compat_ScenarioDef.cs` (75L). Commit: `feat(plugins): add ScenarioDefCompatPlugin`
- **Task 20: `AncientMarketLibraryCompatPlugin`** — port `Compat_AncientMarket_Libraray.cs` (53L). Note legacy typo "Libraray" — correct to "Library" in new code. Commit: `feat(plugins): add AncientMarketLibraryCompatPlugin`

Each with 2-3 tests covering the transformation behavior.

**Plugin priority assignments:** inspect legacy `[CompatPriority]` attributes on each file. Preserve the priorities. `NodeReplacementCompatPlugin` likely has high priority (runs early) based on legacy ordering — verify.

---

## Group 4F — Pipeline assembly

### Task 21: Extend AddApplication with pipeline + stages + plugins

**Files:**
- Modify: `src/RimworldExtractor.Application/DependencyInjection.cs`
- Modify: `tests/RimworldExtractor.Application.Tests/DependencyInjectionTests.cs`

Replace `AddSingleton<IExtractionPipeline, NoOpExtractionPipeline>()` with real pipeline:

```csharp
using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Application.Compat;
using RimworldExtractor.Application.Extraction;
using RimworldExtractor.Application.Extraction.Stages;
using RimworldExtractor.Application.ModDiscovery;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Plugins;
using RimworldExtractor.Plugins.BuiltIn;

namespace RimworldExtractor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Compat plugins
        services.AddCompatPlugin<NodeReplacementCompatPlugin>();
        services.AddCompatPlugin<MvcfCompatPlugin>();
        services.AddCompatPlugin<VerbCompatPlugin>();
        services.AddCompatPlugin<FactionDefCompatPlugin>();
        services.AddCompatPlugin<NoTranslateCompatPlugin>();
        services.AddCompatPlugin<ScenarioDefCompatPlugin>();
        services.AddCompatPlugin<AncientMarketLibraryCompatPlugin>();

        services.AddSingleton<CompatRegistry>();

        // Stages (in execution order)
        services.AddSingleton<IExtractionStage, LoadReferenceDefsStage>();
        services.AddSingleton<IExtractionStage, ApplyPrePatchesStage>();
        services.AddSingleton<IExtractionStage, ResolveInheritanceStage>();
        services.AddSingleton<IExtractionStage, CompatPreProcessStage>();
        services.AddSingleton<IExtractionStage, ExtractDefsStage>();
        services.AddSingleton<IExtractionStage, ExtractKeyedStage>();
        services.AddSingleton<IExtractionStage, ExtractStringsStage>();
        services.AddSingleton<IExtractionStage, ExtractPatchesStage>();
        services.AddSingleton<IExtractionStage, CompatPostProcessStage>();

        services.AddSingleton<IExtractionPipeline, ExtractionPipeline>();

        services.AddSingleton<ModDiscoveryService>();

        return services;
    }
}
```

Update `DependencyInjectionTests.AddApplication_ResolvesExtractionPipeline` to verify the concrete type is now `ExtractionPipeline` and pipeline is non-null.

Commit: `feat(application): wire real pipeline + 7 compat plugins into AddApplication`

### Task 22: Delete NoOpExtractionPipeline

After Task 21 replaces the registration, `NoOpExtractionPipeline` is unreachable. Delete the file.

```bash
rm src/RimworldExtractor.Application/Extraction/NoOpExtractionPipeline.cs
```

Confirm no references: `grep -r NoOpExtractionPipeline src/ tests/`. Should be empty.

Commit: `refactor(application): remove NoOpExtractionPipeline (superseded by real pipeline)`

---

## Group 4G — ModDiscoveryService + Tests

### Task 23: ModDiscoveryService

**Files:**
- Create: `src/RimworldExtractor.Application/ModDiscovery/ModDiscoveryService.cs`
- Test: `tests/RimworldExtractor.Application.Tests/ModDiscovery/ModDiscoveryServiceTests.cs`

Thin wrapper over `IModLister` that adds caching + logging + returns `IReadOnlyList<ModMetadata>` for UI consumption. Not strictly required for Phase 4's snapshot test, but needed for Phase 5 UI.

```csharp
using Microsoft.Extensions.Logging;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Application.ModDiscovery;

public sealed class ModDiscoveryService
{
    private readonly IModLister _modLister;
    private readonly ILogger<ModDiscoveryService> _logger;
    private IReadOnlyList<ModMetadata>? _cachedAll;

    public ModDiscoveryService(IModLister modLister, ILogger<ModDiscoveryService> logger)
    {
        _modLister = modLister;
        _logger = logger;
    }

    public IReadOnlyList<ModMetadata> DiscoverAll(bool refresh = false)
    {
        if (refresh || _cachedAll is null)
        {
            _cachedAll = _modLister.DiscoverAll();
            _logger.LogInformation("Discovered {Count} mods", _cachedAll.Count);
        }
        return _cachedAll;
    }

    public ModMetadata? ReadMetadata(string modRoot) => _modLister.ReadMetadata(modRoot);

    public IReadOnlyList<ExtractableFolder> GetExtractableFolders(ModMetadata mod)
        => _modLister.GetExtractableFolders(mod);

    public IReadOnlyList<ModMetadata> FindReferenceMods(ModMetadata target)
        => _modLister.FindReferenceMods(target);
}
```

Tests: caching behavior + delegation to IModLister.

Commit: `feat(application): add ModDiscoveryService (cached IModLister wrapper)`

### Task 24: Extension points for output (optional, informational)

Skip for Phase 4. Phase 5 UI will orchestrate strategy selection.

### Task 25: Wire Infrastructure + Application DI together smoke test

Add a test under `tests/RimworldExtractor.Integration.Tests/FullStackDiTests.cs` that verifies `services.AddInfrastructure(settingsPath).AddApplication()` resolves `IExtractionPipeline` with all dependencies.

Commit: `test(integration): full-stack DI resolution smoke test`

---

## Group 4H — Snapshot Regression Test

### Task 26: SampleModSnapshotTests — THE critical parity gate

**Files:**
- Create: `tests/RimworldExtractor.Integration.Tests/SampleModSnapshotTests.cs`

This is the moment of truth. The test runs the full new pipeline against `samples/sample-mod/` and compares the result to `tests/__snapshots__/legacy/sample-mod.extraction.json` (captured in Phase 0).

Algorithm:
1. Resolve paths: repo root via `RepositoryRoot` assembly metadata (same pattern as Phase 0)
2. Configure `AppSettings` equivalent to legacy `Prefabs.Init()` defaults
3. Use `PhysicalFileSystem` for real disk
4. Build DI container: `AddInfrastructure(settingsPath) + AddApplication()`
5. Discover mod: `ModDiscoveryService.ReadMetadata(".../samples/sample-mod")`
6. Get extractable folders + reference mods (empty for sample-mod)
7. Run pipeline: `await pipeline.RunAsync(request)`
8. Serialize result entries to JSON with same options/ordering as Phase 0 snapshot
9. Compare to committed snapshot: `Assert.AreEqual`

Test code sketch (actual test needs full setup):

```csharp
using System.Text.Json;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Application;
using RimworldExtractor.Application.ModDiscovery;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure;
using Xunit;

namespace RimworldExtractor.Integration.Tests;

public class SampleModSnapshotTests
{
    private static string RepoRoot => typeof(SampleModSnapshotTests).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .First(a => a.Key == "RepositoryRoot")
        .Value!;

    [Fact]
    public async Task NewPipeline_ExtractsSampleMod_MatchesLegacySnapshot()
    {
        // Arrange settings
        var tempDir = Directory.CreateTempSubdirectory("rwx-phase4-").FullName;
        try
        {
            var settingsPath = Path.Combine(tempDir, "settings.json");
            var store = new Infrastructure.Settings.JsonSettingsStore(
                new Infrastructure.FileSystem.PhysicalFileSystem(), settingsPath);
            var settings = AppSettings.Default with
            {
                Paths = AppSettings.Default.Paths with
                {
                    Rimworld = RepoRoot,
                    Workshop = RepoRoot,
                },
                Extraction = AppSettings.Default.Extraction with
                {
                    // Use same ExtractionRules as legacy Prefabs.Init() — reuse LegacyPrefabsReader
                    // on a synthetic Prefabs.dat built here OR hardcode the rule list.
                    // EASIEST: call LegacyPrefabsReader to convert a seeded Prefabs.dat.
                },
            };
            await store.SaveAsync(settings, TestContext.Current.CancellationToken);

            // Build DI
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(settingsPath);
            services.AddApplication();
            using var provider = services.BuildServiceProvider();

            var pipeline = provider.GetRequiredService<IExtractionPipeline>();
            var discovery = provider.GetRequiredService<ModDiscoveryService>();

            var sampleRoot = Path.Combine(RepoRoot, "samples", "sample-mod");
            var mod = discovery.ReadMetadata(sampleRoot)
                ?? throw new InvalidOperationException("Sample mod not discoverable");
            var folders = discovery.GetExtractableFolders(mod);

            var request = new Domain.Abstractions.ExtractionRequest(
                Target: mod,
                SelectedFolders: folders,
                ReferenceMods: null,
                Settings: settings);

            // Act
            var result = await pipeline.RunAsync(request, progress: null, TestContext.Current.CancellationToken);

            // Assert: compare to Phase 0 snapshot
            var ordered = result.Entries
                .OrderBy(e => e.ClassName, StringComparer.Ordinal)
                .ThenBy(e => e.Node, StringComparer.Ordinal)
                .ThenBy(e => e.Original, StringComparer.Ordinal)
                .Select(e => new { e.ClassName, e.Node, e.Original, e.SourceFile })
                .ToList();
            var actualJson = JsonSerializer.Serialize(ordered, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            }).Replace("\r\n", "\n").Replace("\r", "\n");

            var snapshotPath = Path.Combine(RepoRoot, "tests", "__snapshots__", "legacy", "sample-mod.extraction.json");
            var expectedJson = (await File.ReadAllTextAsync(snapshotPath, TestContext.Current.CancellationToken))
                .Replace("\r\n", "\n").Replace("\r", "\n");

            actualJson.Should().Be(expectedJson,
                "new pipeline output must match legacy baseline byte-for-byte");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }
}
```

**Critical seed**: `AppSettings.Extraction.Rules` must contain the same rules legacy's `Prefabs.Init()` seeds (the huge tag list at `Prefabs.cs:168-170`). EASIEST way: write a helper in the test that builds these rules from the legacy tag string. Alternative: add a static `LegacyDefaultExtractionSettings.Create()` helper in Infrastructure/Legacy that returns the full `ExtractionSettings` seeded per legacy defaults.

Add `tests/RimworldExtractor.Integration.Tests/RimworldExtractor.Integration.Tests.csproj` the `AssemblyMetadata` attribute for `RepositoryRoot` (same pattern as Phase 0's legacy test csproj).

**EXPECT:** the test WILL fail on first run. Analyze the diff, fix the pipeline (probably small bugs — missing RequiredMods combining, wrong dedupe order, etc.). Iterate until green. Each bug fix is its own commit with message like `fix(application): dedupe entries by ClassName+Node after extraction (parity with legacy)`.

Commit initial test: `test(integration): SampleModSnapshot test (may fail initially)`

### Task 27: Iterate until snapshot passes

Drive fixes based on diff output. Expect 2-5 additional commits to achieve parity. Common issues to anticipate:
- **Dedupe:** Legacy does `DistinctBy(x => $"{x.ClassName}+{x.Node}")` at end. Ensure pipeline does too (probably in `CompatPostProcessStage` or a separate `DedupeStage`).
- **Entry ordering:** Snapshot test sorts, so order doesn't matter for comparison.
- **RequiredMods on entries:** null for sample-mod (no dependencies).
- **Keyed entries' Original vs Translated:** legacy emits with `Translated=null`, `Original=actualText`. Verify.
- **Patches dual-entry:** Both `ThingDef.label` (original) and `Patches.ThingDef.label` (patched) should appear.
- **Inheritance:** `IronSword.description` inherited from abstract parent.
- **jobString from PatchOperationAdd:** should emit with class `Patches.ThingDef`.

**STOP AND THINK** at every failure. Don't force-green by hacking the snapshot. The snapshot is the spec.

Commit for final pass: `test(integration): SampleModSnapshot passes full parity with legacy`

---

## Group 4I — Verification Gate

### Task 28: Full-suite verification + push

- `dotnet build RimworldExtractor.slnx -c Release` → 0W/0E
- `dotnet test RimworldExtractor.slnx -c Release` → all green, including `SampleModSnapshotTests`
- `dotnet format --verify-no-changes` → clean
- `dotnet test legacy/RimworldExtractorTest/RimworldExtractorTest.csproj --filter LegacyBaselineTests` → 1 passed
- Coverage check for Application + Plugins
- `git push origin feat/remake-v2`
- Report summary

No commit.

---

## Self-Review

**Spec coverage:** every file from master plan §Phase 4 is covered:
- ExtractionContext (T2), ExtractionPipeline (T4), 9 stages (T5-T11, T13×2) ✓
- ICompatPlugin + CompatPriorityAttribute + PluginRegistration (T12) ✓
- 7 compat plugins (T14-T20) ✓
- ModDiscoveryService (T23) ✓
- SampleModSnapshotTests (T26-T27) ✓
- No static mutable state — verified via `grep -r "static.*=" src/` check in T28

**Placeholder scan:** Tasks 14-20 are compressed ("port legacy <file> faithfully") but legacy files are small (24-137 lines each) and the legacy code is the spec. Each task's commit message and plugin priority guidance is explicit.

**Type consistency:**
- `ExtractionRequest` / `ExtractionResult` — Phase 4 refactored shapes from Phase 1 stubs
- `ExtractionContext.CombinedDefs` — replaceable via `ReplaceCombinedDefs(XDocument)`
- `CompatRegistry` in Application (not Plugins) — Application aggregates across plugins
- `CompatPluginBase` provides identity defaults — T12

**Known risks:**
1. **Snapshot parity hard to achieve.** If MVCF/NodeReplacement logic diverges even slightly, the JSON won't match. Task 27 iterates — budget extra time.
2. **Compat DoPostProcessing order in legacy.** Legacy runs `CompatManager.DoPostProcessing` inside `ExtractDefs` AND inside `ExtractPatches` (before returning entries). Phase 4 places `CompatPostProcessStage` at the end of the pipeline. If the compat plugins' behavior depends on seeing only Defs entries (not Keyed/Strings/Patches mixed), parity breaks. Mitigation: `CompatPostProcessStage` filters entries by ClassName before delegating, OR we split into two compat-post stages (before and after extract stages). TBD at T27.

---

## Task Execution Order (subagent dispatch)

1. **4A** (Pipeline primitives) T1-4 — 1 dispatch
2. **4B** (Setup stages) T5-7 — 1 dispatch
3. **4C** (Extract stages) T8-11 — 1 dispatch
4. **4D** (Compat API) T12-13 — 1 dispatch
5. **4E** (7 plugins) T14-20 — 1 dispatch (they're small, all at once)
6. **4F** (Pipeline assembly) T21-22 — 1 dispatch
7. **4G** (ModDiscovery + smoke) T23, 25 — 1 dispatch
8. **4H** (Snapshot test) T26-27 — **2 dispatches** (first writes test, second iterates fixes — may need more)
9. **4I** (Gate) T28 — inline

Total: ~9-11 dispatches including iteration for Task 27.

---

## Execution Handoff

Plan saved. Continue autonomous subagent-driven execution through Phase 6.
