# Phase 6 — CLI + Release Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` or `superpowers:executing-plans`.

**Goal:** Ship a `rimextract` CLI binary (Native AOT, cross-platform) and a GitHub Actions release workflow that publishes CLI + GUI artifacts for Windows/macOS/Linux on tag push. Finish with README rewrite and v2.0.0 release tag.

**Branch:** `feat/remake-v2` at Phase 5 gate (354 tests, working UI).

**Legacy:** `tools.py` has version-bumping + packaging helpers for the old WinForms app. Keep but don't adapt — replaced by the new workflow.

---

## Sub-batch overview

| # | Sub-batch | Tasks | Outcome |
|---|-----------|-------|---------|
| 6A | CLI commands | T1-T5 | `Extract`, `Convert`, `Analyze` subcommands via `System.CommandLine` |
| 6B | AOT setup | T6-T7 | `<PublishAot>true</PublishAot>`, `InvariantGlobalization`, `rd.xml` if needed |
| 6C | Release workflow | T8-T9 | `.github/workflows/release.yml` — tag-triggered, matrix publish |
| 6D | Docs + v2.0.0 | T10-T11 | README rewrite, CHANGELOG, tag |

**~11 tasks, 4 dispatches.**

---

## Key design decisions

1. **CLI uses `System.CommandLine` 2.0.0-beta5.** Prerelease but only option with good .NET 10 support. Has subcommand DSL + automatic `--help`.
2. **Native AOT for CLI only.** GUI (Avalonia) stays non-AOT (reflection-heavy). CLI is the "fast cold-start" target.
3. **CLI reads settings from `~/.config/rimworld-extractor/settings.json`** (unix convention) or `%APPDATA%/RimworldExtractor/settings.json` (Windows). Override via `--config <path>`.
4. **Release artifacts** (6 total):
   - `rimextract-win-x64.exe` (Native AOT CLI, ~15MB)
   - `rimextract-linux-x64` (AOT)
   - `rimextract-osx-arm64` (AOT)
   - `RimworldExtractor-GUI-win-x64.zip` (Avalonia self-contained)
   - `RimworldExtractor-GUI-linux-x64.tar.gz`
   - `RimworldExtractor-GUI-osx-arm64.zip`

---

## Group 6A — CLI commands

### Task 1: CLI Program.cs with root command

Replace `src/RimworldExtractor.Cli/Program.cs` with System.CommandLine setup:

```csharp
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using RimworldExtractor.Application;
using RimworldExtractor.Cli.Commands;
using RimworldExtractor.Infrastructure;

var configOption = new Option<string?>(
    name: "--config",
    description: "Path to settings.json (defaults to user config dir).");

var root = new RootCommand("RimWorld translation extractor");
root.AddGlobalOption(configOption);

root.AddCommand(new ExtractCommand(configOption));
root.AddCommand(new ConvertCommand(configOption));
root.AddCommand(new AnalyzeCommand(configOption));

return await root.InvokeAsync(args);
```

Commit: `feat(cli): bootstrap System.CommandLine root with 3 subcommands`

### Task 2: ExtractCommand

File: `src/RimworldExtractor.Cli/Commands/ExtractCommand.cs`

Subcommand signature:
```
rimextract extract --mod <name-or-id> --out <dir> [--format xlsx|languages|comments] [--version <x.y>] [--include-ref-mods]
```

Flow:
1. Parse options
2. Build DI container (AddInfrastructure + AddApplication) with settings from `--config` or default path
3. Discover mod by name/packageId/id
4. Build ExtractionRequest, run pipeline
5. Write output via selected IOutputStrategy
6. Report exit code (0 success, 1 failure)

Commit: `feat(cli): add ExtractCommand`

### Task 3: ConvertCommand

`rimextract convert --input <file.xlsx> --output <dir>` — round-trips XLSX → Languages/ XML. Uses `ClosedXmlReader` + `XmlLanguagesWriter`.

Commit: `feat(cli): add ConvertCommand`

### Task 4: AnalyzeCommand

`rimextract analyze --mod <name> --against <file.xlsx>` — compares current extraction to an older XLSX, reports changes. (MVP: skip if complex; stub with "not implemented in v2.0" and return exit code 2.)

Commit: `feat(cli): add AnalyzeCommand (stub — full impl deferred)`

### Task 5: CLI integration test

`tests/RimworldExtractor.Integration.Tests/CliSmokeTests.cs` — invokes CLI programmatically against `samples/sample-mod/`, asserts exit 0, verifies output files created.

Commit: `test(cli): smoke test exercising Extract command against sample-mod`

---

## Group 6B — AOT setup

### Task 6: AOT-enable CLI csproj

Edit `src/RimworldExtractor.Cli/RimworldExtractor.Cli.csproj`:
```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <InvariantGlobalization>true</InvariantGlobalization>
  <StripSymbols>true</StripSymbols>
  <OptimizationPreference>Size</OptimizationPreference>
  <RootNamespace>RimworldExtractor.Cli</RootNamespace>
</PropertyGroup>
```

Commit: `build(cli): enable Native AOT publishing`

### Task 7: AOT trim warnings triage

Run `dotnet publish src/RimworldExtractor.Cli -c Release -r win-x64 --self-contained -p:PublishAot=true` — expect warnings. Fix:
- Add `DynamicallyAccessedMembers` attributes where needed
- Suppress warnings for System.Text.Json (already handled via source gen in Phase 2) and ClosedXML (library-level — document as known)
- If ClosedXML blocks AOT entirely, add `<PublishTrimmed>true</PublishTrimmed>` + acceptance warnings, OR disable AOT for ClosedXML paths and fall back to `PublishReadyToRun` only

MVP: if AOT is intractable with ClosedXML, ship `PublishReadyToRun` + `PublishSingleFile=true` instead. Document trade-off.

Commit: `build(cli): triage AOT trim warnings (final trade-off: <x>)`

---

## Group 6C — Release workflow

### Task 8: release.yml workflow

File: `.github/workflows/release.yml`

Triggered on tag push matching `v*`. Matrix: 3 RIDs (win-x64, linux-x64, osx-arm64) × 2 projects (CLI, GUI).

Each job:
1. Checkout
2. Setup .NET 10
3. For CLI: `dotnet publish src/RimworldExtractor.Cli -c Release -r <rid> --self-contained -o publish/cli-<rid>`
4. For GUI: `dotnet publish src/RimworldExtractor.Ui.Avalonia -c Release -r <rid> --self-contained -o publish/gui-<rid>`
5. Archive (zip/tar) + upload to release

Final job: create GitHub Release with artifacts.

Commit: `ci: add release workflow (tag-triggered cross-platform publish)`

### Task 9: Workflow test run via pre-release tag

Push tag `v2.0.0-rc1` to trigger workflow. Monitor via GitHub Actions UI (user does this, not the agent). Fix any failures.

Commit (if fixes): `ci: fix release workflow <specific-issue>`

---

## Group 6D — Docs + v2.0.0

### Task 10: README rewrite

File: `README.md`

Sections:
- Quick Start (download GUI or CLI binary, run)
- CLI Usage (each command + example)
- GUI Features
- Migration from v1 (Prefabs.dat → settings.json auto-convert on first launch)
- Building from Source
- Contributing

Delete legacy-specific content.

Commit: `docs: rewrite README for v2`

### Task 11: CHANGELOG + v2.0.0 tag

File: `CHANGELOG.md`

```markdown
# Changelog

## [2.0.0] - YYYY-MM-DD

Complete rewrite:
- .NET 10 (was .NET 7 EOL)
- Cross-platform Avalonia UI (was Windows-only WinForms)
- Native AOT CLI (`rimextract`)
- Clean Architecture: Domain / Application / Infrastructure / Plugins / UI / CLI
- Pipeline-based extraction (9 stages) replacing static-heavy monolith
- Plugin-based compat system (7 built-in compats, DI-registered)
- Byte-identical parity with v1 extraction for sample mod baseline
- 354 automated tests (was 1)

### Breaking changes
- `Prefabs.dat` → `settings.json` (automatic migration on first launch)
- WinForms UI removed (old version preserved in `legacy/` for reference)
- ...
```

Then:
```bash
git tag -a v2.0.0 -m "RimworldExtractor v2.0.0 — complete rewrite"
git push origin v2.0.0
```

Commit before tag: `docs: add CHANGELOG for v2.0.0`

---

## Verification gate

- `dotnet build` → 0W/0E
- `dotnet test` → all green
- `dotnet publish ... -r <rid>` → succeeds for each RID
- Release workflow green on test tag
- README / CHANGELOG reviewed

---

## Scope

- Do NOT edit `legacy/`, `samples/`, `tests/__snapshots__/`, Phase 2-5 code (except where Phase 6 needs integration).
- Do NOT push the final `v2.0.0` tag autonomously — leave for user to confirm.

---

## Execution Handoff

Plan saved. Final phase. Execute → push → stop at user tag decision.
