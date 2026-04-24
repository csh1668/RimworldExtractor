# RimworldExtractor .NET 10 Rewrite — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the .NET 7 / WinForms / static-heavy RimworldExtractor with a .NET 10 LTS, Clean Architecture, cross-platform rewrite whose outputs are byte-identical to the legacy pipeline for a committed sample mod.

**Architecture:** Pipeline-based extraction (`IExtractionStage`-chained stages, per-run `ExtractionContext`) over a layered solution — `Domain` (pure models/rules) → `Application` (use-cases + pipeline) → `Infrastructure` (XDocument/ClosedXML/file-system adapters) → `Plugins` (DI-registered compats) → `Ui.Avalonia` / `Cli`. DI via `Microsoft.Extensions.DependencyInjection`, logging via `Microsoft.Extensions.Logging` + Serilog, settings via `System.Text.Json` source generator.

**Tech Stack:** .NET 10 LTS · C# latest · Avalonia UI 11 · CommunityToolkit.Mvvm · xUnit + FluentAssertions + NSubstitute + Verify · ClosedXML (stable) · System.CommandLine (CLI, Native AOT) · GitHub Actions CI/CD · Central Package Management.

**Branch:** `feat/remake-v2` (current). `legacy/` folder holds the frozen net7 code for baseline snapshot generation and reference. Do **not** edit `legacy/*.cs` files; only add a minimal test fixture + one baseline test file to `legacy/RimworldExtractorTest/`.

---

## Phase Overview & Checkpoints

Each phase ends with a **human review checkpoint** — do not proceed to the next phase without explicit user approval.

| # | Phase | Outcome | Verification Gate | Detailed Plan |
|---|-------|---------|-------------------|---------------|
| 0 | **Baseline Snapshots** | Golden extraction outputs for `samples/sample-mod/` committed; running legacy against fixture matches snapshot exactly | `dotnet test legacy/RimworldExtractor.sln` all green; `samples/sample-mod/` + `tests/__snapshots__/legacy/*.json` committed | **Inline (below)** |
| 1 | **Solution Scaffolding** | Empty .NET 10 solution builds + CI green + DI smoke test passes | `dotnet build RimworldExtractor.sln` (no warnings), GitHub Actions workflow passes, DI container resolves a no-op `IExtractionPipeline` | **Inline (below)** |
| 2 | Domain + Settings | `Domain` records + `JsonSettingsStore` + `LegacyPrefabsReader` pass round-trip tests | `dotnet test` Domain+Settings > 80% coverage; existing `Prefabs.dat` converts to `settings.json` without data loss | Authored at Phase 1→2 checkpoint → `docs/plans/remake-v2-phase2-domain.md` |
| 3 | Infrastructure | `ClosedXmlReader/Writer`, `XmlLanguagesReader/Writer`, `FileSystemGateway`, `XDocument`-based parsers pass round-trip tests | Given sample mod XML, `XDocumentDefParser → XmlLanguagesWriter` round-trip equals input (semantic XML diff); Excel round-trip identical | Authored at Phase 2→3 checkpoint → `docs/plans/remake-v2-phase3-infra.md` |
| 4 | Application Pipeline | Full extraction via new pipeline produces output **byte-identical** to Phase 0 snapshot | `dotnet test` Integration.Tests: Pipeline on `samples/sample-mod/` matches `tests/__snapshots__/legacy/*.json` | Authored at Phase 3→4 checkpoint → `docs/plans/remake-v2-phase4-pipeline.md` |
| 5 | UI — Avalonia 11 | Cross-platform GUI replicates legacy feature set (mod select, settings, extract, analyzer) | Manual: Windows + Linux smoke test walk-through; unit: ViewModel tests green | Authored at Phase 4→5 checkpoint → `docs/plans/remake-v2-phase5-ui.md` |
| 6 | CLI + Release | Native AOT CLI ships; GitHub Actions release workflow produces Win/macOS/Linux artifacts | CI tag-triggered release builds succeed; `rimextract extract --mod X --out ./out` works on all 3 platforms | Authored at Phase 5→6 checkpoint → `docs/plans/remake-v2-phase6-release.md` |

> **Sub-plan authorship rule:** When a checkpoint is reached, the next phase's detailed plan is written as a dedicated file using the `superpowers:writing-plans` skill. This keeps each plan focused, incorporates lessons from the prior phase, and avoids stale detail. It is **not** a "TBD" placeholder — it is a named deliverable with a defined trigger.

---

## Global Conventions

**Directory layout (target):**
```
/                                   # repo root
├── RimworldExtractor.sln           # NEW (Phase 1)
├── Directory.Build.props           # NEW (Phase 1)
├── Directory.Packages.props        # NEW (Phase 1) — Central Package Management
├── global.json                     # NEW (Phase 1)
├── REMAKE_PLAN.md                  # already committed (v2)
├── README.md
├── LICENSE.txt
├── tools.py                        # legacy CI helper (updated Phase 6)
├── src/
│   ├── RimworldExtractor.Domain/
│   ├── RimworldExtractor.Application/
│   ├── RimworldExtractor.Infrastructure/
│   ├── RimworldExtractor.Plugins/
│   ├── RimworldExtractor.Ui.Avalonia/
│   └── RimworldExtractor.Cli/
├── tests/
│   ├── RimworldExtractor.Domain.Tests/
│   ├── RimworldExtractor.Application.Tests/
│   ├── RimworldExtractor.Infrastructure.Tests/
│   ├── RimworldExtractor.Integration.Tests/
│   └── __snapshots__/
│       └── legacy/                 # Phase 0 golden outputs
├── samples/
│   └── sample-mod/                 # Phase 0 fixture (synthetic RimWorld mod)
├── docs/
│   └── plans/                      # this file + per-phase sub-plans
├── .github/
│   └── workflows/
│       ├── ci.yml                  # Phase 1
│       └── release.yml             # Phase 6
└── legacy/                         # frozen net7 code + baseline test (Phase 0 adds 1 test file)
```

**Commit conventions (Conventional Commits):**
- `feat: …` new capability
- `fix: …` bug fix
- `refactor: …` non-behavioral restructuring
- `test: …` tests only
- `docs: …` documentation
- `chore: …` tooling, dependencies, CI
- `build: …` build system changes

**Per-task commit rule:** Commit after every completed task (~5 min of work). Small commits make review and rollback cheap.

**Verification commands:**
- Build: `dotnet build RimworldExtractor.sln -c Release`
- Test: `dotnet test RimworldExtractor.sln -c Release --logger "console;verbosity=detailed"`
- Format check: `dotnet format --verify-no-changes RimworldExtractor.sln`

**Nullability + warnings-as-errors are ON globally (`Directory.Build.props`)** from Phase 1 onward. Exception: `legacy/` untouched.

---

## Phase 0: Baseline Snapshot Infrastructure

**Purpose:** Before rewriting anything, lock the current legacy pipeline's behavior as executable golden tests. Every future phase asserts byte-equality against these snapshots. If a Phase 4 refactor accidentally changes an XML attribute order, the snapshot test will catch it.

**Files:**
- Create: `samples/sample-mod/About/About.xml`
- Create: `samples/sample-mod/Defs/ThingDefs/Weapons.xml`
- Create: `samples/sample-mod/Languages/English/Keyed/Greetings.xml`
- Create: `samples/sample-mod/Languages/English/Strings/Names/Last.txt`
- Create: `samples/sample-mod/Patches/Patch_ThingDefs.xml`
- Create: `legacy/RimworldExtractorTest/LegacyBaselineTests.cs`
- Create: `tests/__snapshots__/.gitkeep`
- Create: `tests/__snapshots__/legacy/sample-mod.extraction.json` (generated by test, committed)

### Task 0.1: Create `samples/sample-mod/` fixture — About.xml

A synthetic mod small enough to review by hand, large enough to cover `Defs`, `Keyed`, `Strings`, and `Patches` folder types.

- [ ] **Step 1: Create fixture directory**

```bash
mkdir -p samples/sample-mod/About \
         samples/sample-mod/Defs/ThingDefs \
         samples/sample-mod/Languages/English/Keyed \
         samples/sample-mod/Languages/English/Strings/Names \
         samples/sample-mod/Patches
```

- [ ] **Step 2: Write `samples/sample-mod/About/About.xml`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModMetaData>
  <name>RimworldExtractor Sample Mod</name>
  <packageId>RimworldExtractor.SampleMod</packageId>
  <author>RimworldExtractor</author>
  <supportedVersions>
    <li>1.6</li>
  </supportedVersions>
  <description>Synthetic fixture for baseline snapshot tests. Do not modify without regenerating snapshots.</description>
</ModMetaData>
```

- [ ] **Step 3: Commit fixture skeleton**

```bash
git add samples/sample-mod/About/About.xml
git commit -m "test: add sample-mod About.xml fixture skeleton"
```

### Task 0.2: Add `Defs/ThingDefs/Weapons.xml` with 3 translatable fields

- [ ] **Step 1: Write `samples/sample-mod/Defs/ThingDefs/Weapons.xml`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <ThingDef>
    <defName>SampleMod_WoodenSpear</defName>
    <label>wooden spear</label>
    <description>A sharpened stick. Primitive but effective.</description>
  </ThingDef>
  <ThingDef Name="SampleMod_SwordBase" Abstract="True">
    <statBases>
      <MeleeWeapon_AverageDPS>8</MeleeWeapon_AverageDPS>
    </statBases>
  </ThingDef>
  <ThingDef ParentName="SampleMod_SwordBase">
    <defName>SampleMod_IronSword</defName>
    <label>iron sword</label>
    <description>A balanced iron blade.</description>
  </ThingDef>
</Defs>
```

- [ ] **Step 2: Commit**

```bash
git add samples/sample-mod/Defs/ThingDefs/Weapons.xml
git commit -m "test: add sample-mod Defs fixture (labels, inheritance)"
```

### Task 0.3: Add `Languages/English/Keyed/Greetings.xml`

- [ ] **Step 1: Write `samples/sample-mod/Languages/English/Keyed/Greetings.xml`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>
  <SampleMod_Hello>Hello, traveler!</SampleMod_Hello>
  <SampleMod_Farewell>Safe travels.</SampleMod_Farewell>
</LanguageData>
```

- [ ] **Step 2: Commit**

```bash
git add samples/sample-mod/Languages/English/Keyed/Greetings.xml
git commit -m "test: add sample-mod Keyed fixture"
```

### Task 0.4: Add `Languages/English/Strings/Names/Last.txt`

- [ ] **Step 1: Write `samples/sample-mod/Languages/English/Strings/Names/Last.txt`**

```
Ashwood
Brightforge
Cinderfall
```

- [ ] **Step 2: Commit**

```bash
git add samples/sample-mod/Languages/English/Strings/Names/Last.txt
git commit -m "test: add sample-mod Strings fixture"
```

### Task 0.5: Add `Patches/Patch_ThingDefs.xml`

- [ ] **Step 1: Write `samples/sample-mod/Patches/Patch_ThingDefs.xml`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Patch>
  <Operation Class="PatchOperationAdd">
    <xpath>Defs/ThingDef[defName="SampleMod_WoodenSpear"]</xpath>
    <value>
      <tradeTags>
        <li>PrimitiveWeapon</li>
      </tradeTags>
    </value>
  </Operation>
  <Operation Class="PatchOperationReplace">
    <xpath>Defs/ThingDef[defName="SampleMod_IronSword"]/label</xpath>
    <value>
      <label>steel-edged iron sword</label>
    </value>
  </Operation>
</Patch>
```

- [ ] **Step 2: Commit**

```bash
git add samples/sample-mod/Patches/Patch_ThingDefs.xml
git commit -m "test: add sample-mod Patches fixture"
```

### Task 0.6: Add `MSBuildProjectDirectory`-relative fixture access helper

The legacy test needs to find `samples/sample-mod/` from its bin/Debug output directory. We use `MSBuildProjectDirectory` via a `.runsettings`-free property.

- [ ] **Step 1: Edit `legacy/RimworldExtractorTest/RimworldExtractorTest.csproj` to copy the fixture path as a compile-time constant**

Add before the closing `</Project>`:

```xml
  <ItemGroup>
    <AssemblyAttribute Include="System.Reflection.AssemblyMetadataAttribute">
      <_Parameter1>RepositoryRoot</_Parameter1>
      <_Parameter2>$(MSBuildThisFileDirectory)..\..</_Parameter2>
    </AssemblyAttribute>
  </ItemGroup>
```

- [ ] **Step 2: Run `dotnet build legacy/RimworldExtractor.sln -c Release` and verify the test project still compiles**

Expected: build succeeds with no new warnings.

- [ ] **Step 3: Commit**

```bash
git add legacy/RimworldExtractorTest/RimworldExtractorTest.csproj
git commit -m "test: embed RepositoryRoot assembly metadata for fixture lookup"
```

### Task 0.7: Write `LegacyBaselineTests` — happy path (snapshot-capture mode)

**File:** `legacy/RimworldExtractorTest/LegacyBaselineTests.cs`

This test drives the legacy `Extractor.ExtractTranslationData()` against `samples/sample-mod/`, serializes the `List<TranslationEntry>` to JSON with deterministic ordering, and compares it to a committed golden file. On first run (no golden file), it writes the file and fails with an explicit message — that is the "record baseline" step.

- [ ] **Step 1: Write the failing test**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RimworldExtractorInternal;
using RimworldExtractorInternal.DataTypes;

namespace RimworldExtractorTest;

[TestClass]
public class LegacyBaselineTests
{
    private static string RepoRoot => typeof(LegacyBaselineTests).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .First(a => a.Key == "RepositoryRoot")
        .Value!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [TestMethod]
    public void ExtractSampleMod_MatchesCommittedSnapshot()
    {
        // Arrange
        Prefabs.Init();
        var sampleModRoot = Path.Combine(RepoRoot, "samples", "sample-mod");
        Prefabs.PathRimworld = RepoRoot; // irrelevant for this mod, but Init requires it
        Prefabs.PathWorkshop = RepoRoot;
        Prefabs.CurrentVersion = "1.6";

        var mod = ModLister.GetModMetadataByModRoot(sampleModRoot)
            ?? throw new InvalidOperationException("Fixture mod not discoverable");
        var folders = ModLister.GetExtractableFolders(mod).ToList();

        // Act
        var entries = Extractor.ExtractTranslationData(mod, folders, referenceMods: null);
        var ordered = entries
            .OrderBy(e => e.ClassName, StringComparer.Ordinal)
            .ThenBy(e => e.Node, StringComparer.Ordinal)
            .ThenBy(e => e.Original, StringComparer.Ordinal)
            .Select(e => new
            {
                e.ClassName,
                e.Node,
                e.Original,
                e.Source
            })
            .ToList();
        var actual = JsonSerializer.Serialize(ordered, JsonOptions);

        // Assert / capture
        var snapshotPath = Path.Combine(
            RepoRoot, "tests", "__snapshots__", "legacy", "sample-mod.extraction.json");
        Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);

        if (!File.Exists(snapshotPath))
        {
            File.WriteAllText(snapshotPath, actual);
            Assert.Fail($"Snapshot created at {snapshotPath}. Review it and re-run the test.");
        }

        var expected = File.ReadAllText(snapshotPath);
        Assert.AreEqual(expected, actual, "Extraction output drifted from committed snapshot.");
    }
}
```

- [ ] **Step 2: Run the test to capture the initial snapshot**

```bash
dotnet test legacy/RimworldExtractorTest/RimworldExtractorTest.csproj \
  --filter "FullyQualifiedName~LegacyBaselineTests" \
  --logger "console;verbosity=detailed"
```

Expected: **FAIL** with message `Snapshot created at .../sample-mod.extraction.json. Review it and re-run the test.`

- [ ] **Step 3: Manually inspect the snapshot file**

Open `tests/__snapshots__/legacy/sample-mod.extraction.json` and verify:
- Entries for `label = "wooden spear"`, `"steel-edged iron sword"` (post-patch), descriptions
- `Source` values point to the correct files
- Ordering is deterministic

If the output looks correct, move on. If it looks wrong, fix the fixture or the test ordering before committing the snapshot.

- [ ] **Step 4: Re-run the test; it should now pass**

```bash
dotnet test legacy/RimworldExtractorTest/RimworldExtractorTest.csproj \
  --filter "FullyQualifiedName~LegacyBaselineTests"
```

Expected: **PASS**.

- [ ] **Step 5: Commit**

```bash
git add legacy/RimworldExtractorTest/LegacyBaselineTests.cs \
        tests/__snapshots__/legacy/sample-mod.extraction.json
git commit -m "test: capture baseline extraction snapshot for sample-mod"
```

### Task 0.8: Add `tests/__snapshots__/README.md` explaining regeneration

- [ ] **Step 1: Write `tests/__snapshots__/README.md`**

```markdown
# Baseline Snapshots

Golden outputs of the **legacy** (.NET 7) extraction pipeline against
`samples/sample-mod/`. The rewrite must reproduce these byte-for-byte.

## Regenerate

If `samples/sample-mod/` changes intentionally:

1. Delete the stale snapshot:
   `rm tests/__snapshots__/legacy/sample-mod.extraction.json`
2. Run the legacy baseline test — it writes the new snapshot and fails.
3. Inspect the new snapshot manually.
4. Commit fixture + new snapshot in the same commit.

Never edit a snapshot by hand.
```

- [ ] **Step 2: Commit**

```bash
git add tests/__snapshots__/README.md
git commit -m "docs: document snapshot regeneration workflow"
```

### ✅ Phase 0 Verification Gate

Run all of these before requesting checkpoint review:

- [ ] `dotnet build legacy/RimworldExtractor.sln -c Release` → succeeds
- [ ] `dotnet test legacy/RimworldExtractor.sln -c Release` → all tests pass (including `LegacyBaselineTests`)
- [ ] `git log --oneline feat/remake-v2 ^master` → shows the Phase 0 commits in order
- [ ] `ls tests/__snapshots__/legacy/` → contains `sample-mod.extraction.json`

### 🛑 Phase 0 → Phase 1 Checkpoint (USER REVIEW)

Stop. Post a short summary:
- Links to the sample mod fixture files
- Snapshot content summary (count of entries, classes covered)
- Any surprises encountered

Wait for explicit user approval before starting Phase 1. Approval triggers authorship of **this plan's Phase 1 (below)** — no new document needed at this boundary because Phase 1 is inline.

---

## Phase 1: Solution Scaffolding & .NET 10 Foundation

**Purpose:** Stand up an empty, well-configured .NET 10 solution alongside the legacy code. At the end of this phase, `dotnet build RimworldExtractor.sln` succeeds, DI resolves a stub `IExtractionPipeline`, and CI runs on every push. **Zero business logic yet** — this is pure scaffolding.

**Files:**
- Create: `global.json`
- Create: `Directory.Build.props`
- Create: `Directory.Packages.props`
- Create: `RimworldExtractor.sln`
- Create: `src/RimworldExtractor.Domain/RimworldExtractor.Domain.csproj`
- Create: `src/RimworldExtractor.Application/RimworldExtractor.Application.csproj`
- Create: `src/RimworldExtractor.Infrastructure/RimworldExtractor.Infrastructure.csproj`
- Create: `src/RimworldExtractor.Plugins/RimworldExtractor.Plugins.csproj`
- Create: `src/RimworldExtractor.Ui.Avalonia/RimworldExtractor.Ui.Avalonia.csproj`
- Create: `src/RimworldExtractor.Cli/RimworldExtractor.Cli.csproj`
- Create: `tests/RimworldExtractor.Domain.Tests/RimworldExtractor.Domain.Tests.csproj`
- Create: `tests/RimworldExtractor.Application.Tests/RimworldExtractor.Application.Tests.csproj`
- Create: `tests/RimworldExtractor.Infrastructure.Tests/RimworldExtractor.Infrastructure.Tests.csproj`
- Create: `tests/RimworldExtractor.Integration.Tests/RimworldExtractor.Integration.Tests.csproj`
- Create: `src/RimworldExtractor.Domain/Abstractions/IExtractionPipeline.cs` (stub)
- Create: `src/RimworldExtractor.Application/DependencyInjection.cs` (stub)
- Create: `src/RimworldExtractor.Application/Extraction/NoOpExtractionPipeline.cs`
- Create: `tests/RimworldExtractor.Application.Tests/DependencyInjectionTests.cs`
- Create: `.editorconfig`
- Create: `.github/workflows/ci.yml`

### Task 1.1: Pin .NET SDK with `global.json`

- [ ] **Step 1: Verify local SDK availability**

```bash
dotnet --list-sdks
```

Expected: at least one `10.0.x` SDK line (install from https://dot.net if missing; CI uses the same).

- [ ] **Step 2: Write `global.json`**

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

- [ ] **Step 3: Verify roll-forward picks the installed SDK**

```bash
dotnet --version
```

Expected: `10.0.xxx` (where `xxx >= 100`).

- [ ] **Step 4: Commit**

```bash
git add global.json
git commit -m "build: pin .NET 10.0.100+ via global.json"
```

### Task 1.2: Add `Directory.Build.props` (shared project settings)

- [ ] **Step 1: Write `Directory.Build.props`**

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <IsAotCompatible>true</IsAotCompatible>
    <IsTrimmable>true</IsTrimmable>
    <Deterministic>true</Deterministic>
  </PropertyGroup>

  <!-- Exempt legacy/ from the net10 settings above -->
  <PropertyGroup Condition="$(MSBuildProjectFullPath.Contains('\legacy\')) Or $(MSBuildProjectFullPath.Contains('/legacy/'))">
    <TargetFramework></TargetFramework>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <IsAotCompatible>false</IsAotCompatible>
    <IsTrimmable>false</IsTrimmable>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Verify the legacy solution still builds**

```bash
dotnet build legacy/RimworldExtractor.sln -c Release
```

Expected: succeeds (because the legacy condition blanks `TargetFramework`, letting each legacy csproj's own `<TargetFramework>net7.0</TargetFramework>` win).

- [ ] **Step 3: Commit**

```bash
git add Directory.Build.props
git commit -m "build: add Directory.Build.props with net10 defaults (legacy opt-out)"
```

### Task 1.3: Add `Directory.Packages.props` (Central Package Management)

- [ ] **Step 1: Write `Directory.Packages.props`**

```xml
<Project>
  <ItemGroup>
    <!-- Core -->
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Logging" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Options" Version="10.0.0" />

    <!-- Logging -->
    <PackageVersion Include="Serilog" Version="4.2.0" />
    <PackageVersion Include="Serilog.Extensions.Logging" Version="9.0.0" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageVersion Include="Serilog.Sinks.File" Version="6.0.0" />

    <!-- MVVM / UI -->
    <PackageVersion Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageVersion Include="Avalonia" Version="11.2.1" />
    <PackageVersion Include="Avalonia.Desktop" Version="11.2.1" />
    <PackageVersion Include="Avalonia.Themes.Fluent" Version="11.2.1" />
    <PackageVersion Include="Avalonia.Fonts.Inter" Version="11.2.1" />
    <PackageVersion Include="Avalonia.ReactiveUI" Version="11.2.1" />

    <!-- Excel / XML -->
    <PackageVersion Include="ClosedXML" Version="0.104.2" />

    <!-- CLI -->
    <PackageVersion Include="System.CommandLine" Version="2.0.0-beta5.25306.1" />

    <!-- Tests -->
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="FluentAssertions" Version="7.0.0" />
    <PackageVersion Include="NSubstitute" Version="5.3.0" />
    <PackageVersion Include="Verify.Xunit" Version="28.0.0" />
    <PackageVersion Include="coverlet.collector" Version="6.0.2" />
  </ItemGroup>
</Project>
```

> **Version note:** Before starting this task, use `mcp__plugin_context7_context7__query-docs` to verify each package's latest stable version compatible with .NET 10. Adjust versions above if newer stable releases exist.

- [ ] **Step 2: Commit**

```bash
git add Directory.Packages.props
git commit -m "build: add central package versions for .NET 10 stack"
```

### Task 1.4: Add `.editorconfig`

- [ ] **Step 1: Write `.editorconfig`**

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
indent_style = space
indent_size = 4
insert_final_newline = true
trim_trailing_whitespace = true

[*.{json,yml,yaml,md,xml,csproj,props}]
indent_size = 2

[*.cs]
csharp_new_line_before_open_brace = all
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_namespace_declarations = file_scoped:warning
dotnet_style_qualification_for_field = false:warning
dotnet_style_qualification_for_property = false:warning
dotnet_style_readonly_field = true:warning
dotnet_diagnostic.CA1819.severity = warning
dotnet_diagnostic.IDE0005.severity = warning
```

- [ ] **Step 2: Commit**

```bash
git add .editorconfig
git commit -m "chore: add .editorconfig for consistent formatting"
```

### Task 1.5: Scaffold the empty solution + all project stubs

Use the `dotnet` CLI to create each project so templates + ProjectTypeGuids are correct, then add all to the solution.

- [ ] **Step 1: Create the empty solution**

```bash
dotnet new sln -n RimworldExtractor
```

- [ ] **Step 2: Create Domain (classlib, no deps)**

```bash
dotnet new classlib -o src/RimworldExtractor.Domain -n RimworldExtractor.Domain --force
rm src/RimworldExtractor.Domain/Class1.cs
```

- [ ] **Step 3: Create Application (classlib, depends on Domain)**

```bash
dotnet new classlib -o src/RimworldExtractor.Application -n RimworldExtractor.Application --force
rm src/RimworldExtractor.Application/Class1.cs
dotnet add src/RimworldExtractor.Application reference src/RimworldExtractor.Domain
```

- [ ] **Step 4: Create Infrastructure (classlib, depends on Application + Domain)**

```bash
dotnet new classlib -o src/RimworldExtractor.Infrastructure -n RimworldExtractor.Infrastructure --force
rm src/RimworldExtractor.Infrastructure/Class1.cs
dotnet add src/RimworldExtractor.Infrastructure reference src/RimworldExtractor.Application src/RimworldExtractor.Domain
```

- [ ] **Step 5: Create Plugins (classlib, depends on Domain)**

```bash
dotnet new classlib -o src/RimworldExtractor.Plugins -n RimworldExtractor.Plugins --force
rm src/RimworldExtractor.Plugins/Class1.cs
dotnet add src/RimworldExtractor.Plugins reference src/RimworldExtractor.Domain
```

- [ ] **Step 6: Create UI Avalonia (app, depends on Application + Infrastructure + Plugins)**

```bash
dotnet new install Avalonia.Templates::11.2.1
dotnet new avalonia.app -o src/RimworldExtractor.Ui.Avalonia -n RimworldExtractor.Ui.Avalonia --force
dotnet add src/RimworldExtractor.Ui.Avalonia reference \
  src/RimworldExtractor.Application \
  src/RimworldExtractor.Infrastructure \
  src/RimworldExtractor.Plugins
```

> If `avalonia.app` template resolution fails, fetch the current install command from Avalonia's docs via `mcp__plugin_context7_context7__query-docs` with `avalonia 11 dotnet template`.

- [ ] **Step 7: Create CLI (console, depends on Application + Infrastructure + Plugins)**

```bash
dotnet new console -o src/RimworldExtractor.Cli -n RimworldExtractor.Cli --force
dotnet add src/RimworldExtractor.Cli reference \
  src/RimworldExtractor.Application \
  src/RimworldExtractor.Infrastructure \
  src/RimworldExtractor.Plugins
```

- [ ] **Step 8: Create all four test projects**

```bash
for name in Domain Application Infrastructure Integration; do
  dotnet new xunit -o tests/RimworldExtractor.${name}.Tests -n RimworldExtractor.${name}.Tests --force
  rm tests/RimworldExtractor.${name}.Tests/UnitTest1.cs
done
dotnet add tests/RimworldExtractor.Domain.Tests reference src/RimworldExtractor.Domain
dotnet add tests/RimworldExtractor.Application.Tests reference src/RimworldExtractor.Application src/RimworldExtractor.Domain
dotnet add tests/RimworldExtractor.Infrastructure.Tests reference src/RimworldExtractor.Infrastructure src/RimworldExtractor.Application src/RimworldExtractor.Domain
dotnet add tests/RimworldExtractor.Integration.Tests reference src/RimworldExtractor.Application src/RimworldExtractor.Infrastructure src/RimworldExtractor.Plugins src/RimworldExtractor.Domain
```

- [ ] **Step 9: Add all projects to the solution**

```bash
dotnet sln RimworldExtractor.sln add \
  src/RimworldExtractor.Domain \
  src/RimworldExtractor.Application \
  src/RimworldExtractor.Infrastructure \
  src/RimworldExtractor.Plugins \
  src/RimworldExtractor.Ui.Avalonia \
  src/RimworldExtractor.Cli \
  tests/RimworldExtractor.Domain.Tests \
  tests/RimworldExtractor.Application.Tests \
  tests/RimworldExtractor.Infrastructure.Tests \
  tests/RimworldExtractor.Integration.Tests
```

- [ ] **Step 10: Verify empty solution builds**

```bash
dotnet build RimworldExtractor.sln -c Release
```

Expected: succeeds with **zero warnings** (Warnings-as-Errors is on).

- [ ] **Step 11: Commit**

```bash
git add RimworldExtractor.sln src/ tests/
git commit -m "build: scaffold .NET 10 solution with 6 src + 4 test projects"
```

### Task 1.6: Wire up required NuGet packages per project

Each csproj gets its packages via `PackageReference` (no versions — CPM).

- [ ] **Step 1: Add packages to Domain**

```bash
dotnet add src/RimworldExtractor.Domain package Microsoft.Extensions.Logging.Abstractions
```

- [ ] **Step 2: Add packages to Application**

```bash
dotnet add src/RimworldExtractor.Application package Microsoft.Extensions.DependencyInjection
dotnet add src/RimworldExtractor.Application package Microsoft.Extensions.Logging.Abstractions
```

- [ ] **Step 3: Add packages to Infrastructure**

```bash
dotnet add src/RimworldExtractor.Infrastructure package Microsoft.Extensions.DependencyInjection
dotnet add src/RimworldExtractor.Infrastructure package Microsoft.Extensions.Logging.Abstractions
dotnet add src/RimworldExtractor.Infrastructure package ClosedXML
```

- [ ] **Step 4: Add packages to Plugins**

```bash
dotnet add src/RimworldExtractor.Plugins package Microsoft.Extensions.DependencyInjection.Abstractions
dotnet add src/RimworldExtractor.Plugins package Microsoft.Extensions.Logging.Abstractions
```

- [ ] **Step 5: Add packages to Cli**

```bash
dotnet add src/RimworldExtractor.Cli package Microsoft.Extensions.Hosting
dotnet add src/RimworldExtractor.Cli package Serilog.Extensions.Logging
dotnet add src/RimworldExtractor.Cli package Serilog.Sinks.Console
dotnet add src/RimworldExtractor.Cli package System.CommandLine
```

- [ ] **Step 6: Add packages to Ui.Avalonia**

```bash
dotnet add src/RimworldExtractor.Ui.Avalonia package CommunityToolkit.Mvvm
dotnet add src/RimworldExtractor.Ui.Avalonia package Microsoft.Extensions.Hosting
dotnet add src/RimworldExtractor.Ui.Avalonia package Serilog.Extensions.Logging
dotnet add src/RimworldExtractor.Ui.Avalonia package Serilog.Sinks.File
```

- [ ] **Step 7: Add packages to test projects**

```bash
for proj in Domain Application Infrastructure Integration; do
  dotnet add tests/RimworldExtractor.${proj}.Tests package FluentAssertions
  dotnet add tests/RimworldExtractor.${proj}.Tests package NSubstitute
  dotnet add tests/RimworldExtractor.${proj}.Tests package Verify.Xunit
done
```

- [ ] **Step 8: Verify build still succeeds**

```bash
dotnet restore RimworldExtractor.sln
dotnet build RimworldExtractor.sln -c Release
```

Expected: succeeds with zero warnings.

- [ ] **Step 9: Commit**

```bash
git add src/ tests/
git commit -m "build: wire package references for all projects"
```

### Task 1.7: Define `IExtractionPipeline` stub + no-op implementation (TDD)

The goal is a red → green → commit cycle that proves DI works end to end.

- [ ] **Step 1: Write the failing test — `tests/RimworldExtractor.Application.Tests/DependencyInjectionTests.cs`**

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Application;
using RimworldExtractor.Domain.Abstractions;
using Xunit;

namespace RimworldExtractor.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_ResolvesExtractionPipeline()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var pipeline = provider.GetService<IExtractionPipeline>();

        pipeline.Should().NotBeNull("AddApplication must register IExtractionPipeline");
    }
}
```

- [ ] **Step 2: Run the test to confirm it fails (red)**

```bash
dotnet test tests/RimworldExtractor.Application.Tests/RimworldExtractor.Application.Tests.csproj
```

Expected: **FAIL** with compile errors (types `IExtractionPipeline`, `AddApplication` missing).

- [ ] **Step 3: Write `src/RimworldExtractor.Domain/Abstractions/IExtractionPipeline.cs`**

```csharp
namespace RimworldExtractor.Domain.Abstractions;

/// <summary>
/// Runs an extraction pipeline end-to-end. Phase 1 ships a no-op implementation;
/// real stages arrive in Phase 4.
/// </summary>
public interface IExtractionPipeline
{
    Task<ExtractionResult> RunAsync(ExtractionRequest request, CancellationToken cancellationToken);
}

public sealed record ExtractionRequest(string ModPath);

public sealed record ExtractionResult(IReadOnlyList<string> Messages)
{
    public static ExtractionResult Empty { get; } = new(Array.Empty<string>());
}
```

- [ ] **Step 4: Write `src/RimworldExtractor.Application/Extraction/NoOpExtractionPipeline.cs`**

```csharp
using RimworldExtractor.Domain.Abstractions;

namespace RimworldExtractor.Application.Extraction;

internal sealed class NoOpExtractionPipeline : IExtractionPipeline
{
    public Task<ExtractionResult> RunAsync(ExtractionRequest request, CancellationToken cancellationToken)
        => Task.FromResult(ExtractionResult.Empty);
}
```

- [ ] **Step 5: Write `src/RimworldExtractor.Application/DependencyInjection.cs`**

```csharp
using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Application.Extraction;
using RimworldExtractor.Domain.Abstractions;

namespace RimworldExtractor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IExtractionPipeline, NoOpExtractionPipeline>();
        return services;
    }
}
```

- [ ] **Step 6: Run the test to confirm it passes (green)**

```bash
dotnet test tests/RimworldExtractor.Application.Tests/RimworldExtractor.Application.Tests.csproj
```

Expected: **PASS** — 1 test passed, 0 failed.

- [ ] **Step 7: Commit**

```bash
git add src/RimworldExtractor.Domain/Abstractions/IExtractionPipeline.cs \
        src/RimworldExtractor.Application/Extraction/NoOpExtractionPipeline.cs \
        src/RimworldExtractor.Application/DependencyInjection.cs \
        tests/RimworldExtractor.Application.Tests/DependencyInjectionTests.cs
git commit -m "feat(application): add IExtractionPipeline stub + DI registration"
```

### Task 1.8: Verify full solution test pass + format

- [ ] **Step 1: Run full test suite**

```bash
dotnet test RimworldExtractor.sln -c Release
```

Expected: all projects pass. At this point only the Application stub test exists in the new solution; legacy tests run via `legacy/RimworldExtractor.sln` (not included in root sln).

- [ ] **Step 2: Run format check**

```bash
dotnet format RimworldExtractor.sln --verify-no-changes
```

Expected: **no changes needed**. If format fails, run `dotnet format RimworldExtractor.sln` and commit as `chore: apply dotnet format`.

### Task 1.9: Add GitHub Actions CI workflow

- [ ] **Step 1: Write `.github/workflows/ci.yml`**

```yaml
name: CI

on:
  push:
    branches: [master, main, 'feat/**', 'fix/**', 'refactor/**']
  pull_request:
    branches: [master, main]

jobs:
  build:
    runs-on: ${{ matrix.os }}
    strategy:
      fail-fast: false
      matrix:
        os: [ubuntu-latest, windows-latest]

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json

      - name: Restore
        run: dotnet restore RimworldExtractor.sln

      - name: Build
        run: dotnet build RimworldExtractor.sln -c Release --no-restore

      - name: Format check
        run: dotnet format RimworldExtractor.sln --verify-no-changes --no-restore

      - name: Test
        run: dotnet test RimworldExtractor.sln -c Release --no-build --logger "console;verbosity=detailed"

  legacy-baseline:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 7 for legacy baseline
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 7.0.x

      - name: Build legacy
        run: dotnet build legacy/RimworldExtractor.sln -c Release

      - name: Run legacy baseline tests
        run: dotnet test legacy/RimworldExtractor.sln -c Release --filter "FullyQualifiedName~LegacyBaselineTests"
```

- [ ] **Step 2: Push branch and confirm CI runs**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: add GitHub Actions workflow (build, test, format, legacy-baseline)"
git push -u origin feat/remake-v2
```

- [ ] **Step 3: Open the Actions tab on GitHub and confirm both jobs (build × 2 OS, legacy-baseline) succeed**

If any job fails, fix it before proceeding. Common causes: package version typo, missing project reference, Avalonia template version drift — check with `mcp__plugin_context7_context7__query-docs` for the latest stable.

### ✅ Phase 1 Verification Gate

- [ ] `dotnet build RimworldExtractor.sln -c Release` → succeeds, **zero warnings**
- [ ] `dotnet test RimworldExtractor.sln -c Release` → all tests pass
- [ ] `dotnet format RimworldExtractor.sln --verify-no-changes` → clean
- [ ] `dotnet build legacy/RimworldExtractor.sln -c Release` → still succeeds (untouched)
- [ ] `dotnet test legacy/RimworldExtractor.sln` (includes `LegacyBaselineTests`) → green
- [ ] GitHub Actions CI → green on Ubuntu + Windows, legacy-baseline job → green
- [ ] `git log --oneline feat/remake-v2 ^master` → shows clean sequence of Conventional-Commit titles

### 🛑 Phase 1 → Phase 2 Checkpoint (USER REVIEW)

Stop. Summarize:
- Solution structure (tree output)
- Total project count
- CI badge / workflow URL
- Any deviations from the plan (version bumps from context7 checks, Avalonia template issues, etc.)

Wait for explicit user approval. On approval, invoke `superpowers:writing-plans` with the Phase 2 spec to produce `docs/plans/remake-v2-phase2-domain.md` before any code is written.

---

## Phase 2: Domain + Settings — Milestone Summary

> Detailed plan authored at Phase 1 → Phase 2 checkpoint → `docs/plans/remake-v2-phase2-domain.md`.

**Outcome:** The `Domain` project contains immutable records for every concept (`TranslationEntry`, `ModMetadata`, `ExtractionRule`, `GameVersion`, `DefName`, etc.) and the `Infrastructure/Settings/` layer can round-trip an `AppSettings` through JSON and can convert a legacy `Prefabs.dat` into `settings.json` without data loss.

**Files to create (minimum):**
- `src/RimworldExtractor.Domain/Entities/{TranslationEntry,ModMetadata,ExtractableFolder,RequiredMods}.cs`
- `src/RimworldExtractor.Domain/ValueObjects/{GameVersion,PackageId,DefName,LanguageCode}.cs`
- `src/RimworldExtractor.Domain/Rules/{ExtractionRule,NodeReplacementRule,TranslationHandle}.cs`
- `src/RimworldExtractor.Domain/Enums/{ExtractionFormat,DuplicatesPolicy,FolderKind}.cs`
- `src/RimworldExtractor.Infrastructure/Settings/{AppSettings,AppSettingsJsonContext,JsonSettingsStore,LegacyPrefabsReader}.cs`
- `tests/RimworldExtractor.Domain.Tests/{ExtractionRuleTests,GameVersionTests,LanguageCodeTests}.cs`
- `tests/RimworldExtractor.Infrastructure.Tests/{JsonSettingsStoreTests,LegacyPrefabsReaderTests}.cs`

**Verification gate:**
- Domain unit tests reach ≥ 80% coverage
- `LegacyPrefabsReader` against a real `Prefabs.dat` (sampled from `legacy/`) produces an `AppSettings` that, serialized then round-tripped, equals itself
- `JsonSettingsStore.SaveAsync` is atomic (writes tmp → replace with backup) — test kills the process mid-write and asserts either old or new state, never corrupted

**Commit milestones:** (1) Domain records + enums, (2) Domain value objects + rules, (3) AppSettings record + STJ source generator, (4) JsonSettingsStore atomic write, (5) LegacyPrefabsReader + round-trip test.

**Exit criteria:** `dotnet test` green across `Domain.Tests` + `Infrastructure.Tests`; user-approved design doc for `AppSettings` schema.

---

## Phase 3: Infrastructure — Milestone Summary

> Detailed plan authored at Phase 2 → Phase 3 checkpoint → `docs/plans/remake-v2-phase3-infra.md`.

**Outcome:** `Infrastructure` adapters can read a mod directory, parse XML defs with `XDocument`, apply patch operations, resolve inheritance, and write both Excel (XLSX) and RimWorld `Languages/*` XML outputs. `IO.cs` (900L legacy) is fully replaced by ≤ 200 L focused adapters.

**Files to create (minimum):**
- `src/RimworldExtractor.Infrastructure/FileSystem/{IFileSystem,PhysicalFileSystem,FileSystemGateway,FileSystemModLister}.cs`
- `src/RimworldExtractor.Infrastructure/Xml/{XDocumentDefParser,XmlInheritanceResolver,XPatchProcessor,XmlLanguagesWriter,XmlLanguagesReader}.cs`
- `src/RimworldExtractor.Infrastructure/Excel/{ClosedXmlReader,ClosedXmlWriter,LibreOfficePostProcessor}.cs`
- `src/RimworldExtractor.Infrastructure/Output/{IOutputStrategy,ExcelOutputStrategy,LanguagesOutputStrategy,LanguagesWithCommentsOutputStrategy,SafeFileWriter,IConflictResolver,PolicyBasedConflictResolver}.cs`
- Matching unit tests under `tests/RimworldExtractor.Infrastructure.Tests/`

**Verification gate:**
- Given `samples/sample-mod/Defs/ThingDefs/Weapons.xml`, `XDocumentDefParser` emits the same node set as the legacy parser (diff via Verify snapshot)
- Excel round-trip: write `List<TranslationEntry>` → read → equals original (`FluentAssertions.BeEquivalentTo`)
- `XPatchProcessor` applies `PatchOperationReplace` from fixture and result matches legacy
- `IConflictResolver` chosen behavior (Overwrite / KeepOriginal / Abort) validated per policy

**Exit criteria:** in-memory `IFileSystem` test double works; infra unit tests green; `dotnet format` clean.

---

## Phase 4: Application Pipeline — Milestone Summary

> Detailed plan authored at Phase 3 → Phase 4 checkpoint → `docs/plans/remake-v2-phase4-pipeline.md`.

**Outcome:** The pipeline runs end-to-end against `samples/sample-mod/` and produces output **byte-identical** to the Phase 0 baseline snapshot. Compat plugins are registered explicitly via DI with `[CompatPriority]`-driven ordering. No static mutable state remains.

**Files to create (minimum):**
- `src/RimworldExtractor.Application/Extraction/ExtractionContext.cs`
- `src/RimworldExtractor.Application/Extraction/ExtractionPipeline.cs` (replaces `NoOpExtractionPipeline`)
- `src/RimworldExtractor.Application/Extraction/Stages/{LoadReferenceDefs,ApplyPatches,ResolveInheritance,ExtractDefs,ExtractKeyed,ExtractStrings,ExtractPatches,CompatPreProcess,CompatPostProcess}Stage.cs` (9 stages)
- `src/RimworldExtractor.Application/ModDiscovery/ModDiscoveryService.cs`
- `src/RimworldExtractor.Plugins/{ICompatPlugin,CompatPriorityAttribute,PluginRegistration}.cs`
- `src/RimworldExtractor.Plugins/BuiltIn/*.cs` — 10 plugins ported from `legacy/RimworldExtractorInternal/Compats/`
- `tests/RimworldExtractor.Integration.Tests/SampleModSnapshotTests.cs` — the critical regression test

**Verification gate:**
- **The integration test:** `SampleModSnapshotTests.ExtractSampleMod_MatchesLegacyBaseline` reads `tests/__snapshots__/legacy/sample-mod.extraction.json` (Phase 0 output) and asserts equality with the new pipeline's output — serialized with identical ordering
- Each stage has unit tests that drive it with a prebuilt `ExtractionContext`
- No `static` mutable field in `src/` (grep check in CI)
- Each compat plugin has a unit test proving its pre/post-processing effect

**Exit criteria:** the new pipeline passes the snapshot test; legacy `Prefabs`-style globals are fully gone; all 10 compat plugins have tests.

---

## Phase 5: UI — Avalonia 11 — Milestone Summary

> Detailed plan authored at Phase 4 → Phase 5 checkpoint → `docs/plans/remake-v2-phase5-ui.md`.

**Outcome:** A cross-platform Avalonia 11 GUI that replicates every feature of the legacy WinForms app — mod selection, settings editing, extraction with progress, analyzer, initial path setup. WinForms project stays frozen in `legacy/`.

**Files to create (minimum):**
- `src/RimworldExtractor.Ui.Avalonia/App.axaml` + `.axaml.cs`
- `src/RimworldExtractor.Ui.Avalonia/Views/{MainView,SettingsView,ModSelectView,AnalyzerView,InitialPathView}.axaml` + code-behind
- `src/RimworldExtractor.Ui.Avalonia/ViewModels/{MainViewModel,SettingsViewModel,ModSelectViewModel,AnalyzerViewModel,InitialPathViewModel}.cs`
- `src/RimworldExtractor.Ui.Avalonia/Services/{DialogService,InteractiveConflictResolver,AvaloniaLocalizer}.cs`
- `src/RimworldExtractor.Ui.Avalonia/Resources/{Strings.ko.json,Strings.en.json}`
- `tests/.../ViewModels/*Tests.cs` — tests for every command + observable-property binding

**Verification gate:**
- Manual: launch on Windows and Linux; execute "select sample-mod → extract → Excel" end-to-end
- Unit: every `[RelayCommand]` has a test covering happy path + cancellation + exception
- No code-behind logic other than `InitializeComponent()` (grep check)
- All user-facing strings resolved through `ILocalizer`, not inline

**Exit criteria:** GUI ships feature parity with legacy, confirmed by walkthrough screencast attached to the checkpoint PR.

---

## Phase 6: CLI + Release — Milestone Summary

> Detailed plan authored at Phase 5 → Phase 6 checkpoint → `docs/plans/remake-v2-phase6-release.md`.

**Outcome:** A Native-AOT-compiled `rimextract` binary (single file, ~15 MB, cold start ≤ 50 ms) for Windows/macOS/Linux, plus a tag-triggered GitHub Actions release workflow publishing both the GUI and CLI.

**Files to create (minimum):**
- `src/RimworldExtractor.Cli/Commands/{ExtractCommand,ConvertCommand,AnalyzeCommand}.cs`
- `src/RimworldExtractor.Cli/Program.cs` (replaces template stub; uses `System.CommandLine` + `Microsoft.Extensions.Hosting`)
- Update `src/RimworldExtractor.Cli/RimworldExtractor.Cli.csproj` — add `<PublishAot>true</PublishAot>` + `<InvariantGlobalization>true</InvariantGlobalization>`
- `.github/workflows/release.yml` — on tag `v*`, publish AOT binaries for `win-x64`, `linux-x64`, `osx-arm64` + Avalonia app for each platform
- Update `tools.py` — port version-bump + packaging helpers for the new layout
- `README.md` — complete rewrite with new install/usage

**Verification gate:**
- `dotnet publish src/RimworldExtractor.Cli -c Release -r win-x64 --self-contained` produces a single-file AOT binary
- CI publishes all 6 artifacts (CLI × 3 RIDs + GUI × 3 RIDs) on tag push
- Smoke test: extract `samples/sample-mod/` via each AOT CLI binary → output matches snapshot
- README covers: install, extract via GUI, extract via CLI, migration from v1 (`Prefabs.dat` auto-import)

**Exit criteria:** `v2.0.0` tag published with GitHub Release + all 6 artifacts attached.

---

## Self-Review

After completing Phase 0 and Phase 1 execution, before starting Phase 2:

1. **Spec coverage:** Re-read `REMAKE_PLAN.md` §2.3 (Core Design Changes). Confirm every subsection (A-G) has a concrete home in the phase map. ✅ (A→Phase 4, B→Phase 4, C→Phase 3, D→Phase 2, E→Phase 2, F→Phase 3, G→Phase 5).
2. **Placeholder scan:** grep this file for `TBD|TODO|fill in|similar to`. Fix any matches.
3. **Type consistency:** names used in Phase 2-6 summaries (e.g., `ExtractionContext`, `IExtractionPipeline`, `ICompatPlugin`) must match the eventual sub-plans. Any divergence forces a rename commit, not a silent drift.
4. **Phase independence:** each phase ends in a checkpoint with a green solution + green tests. No phase leaves the tree in a broken state.

---

## Execution Handoff

Plan saved to `docs/plans/remake-v2.md`.

**Recommended execution path:**

**1. Subagent-Driven (recommended)** — fresh subagent per task, two-stage review between tasks. Use `superpowers:subagent-driven-development`.

**2. Inline Execution** — execute Phase 0 + Phase 1 in the current session with checkpoints. Use `superpowers:executing-plans`.

For this rewrite, **Subagent-Driven is the better fit** because:
- Phase boundaries are natural hand-offs (fresh context each time)
- Context-window pressure is real once Phase 3+ starts
- Two-stage review enforces the per-task commit discipline

Which approach would you like to start with? If approved for Subagent-Driven, the next action is to dispatch a subagent for **Task 0.1: Create `samples/sample-mod/` fixture skeleton**.
