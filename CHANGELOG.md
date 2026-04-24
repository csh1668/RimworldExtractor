# Changelog

All notable changes to RimworldExtractor are documented here.

## [2.0.0] - 2026-04-24

Complete rewrite of the tool from the ground up.

### New features

- **.NET 10** (was .NET 7 EOL)
- **Cross-platform Avalonia UI** (was Windows-only WinForms)
- **`rimextract` CLI** — `extract`, `convert`, `analyze` (stub) subcommands via `System.CommandLine`
- **Clean Architecture** — Domain / Application / Infrastructure / Plugins / UI / CLI layers
- **9-stage extraction pipeline** replacing the static-heavy monolith: LoadReferenceDefs, ApplyPrePatches, ResolveInheritance, CompatPreProcess, ExtractDefs, ExtractKeyed, ExtractStrings, ExtractPatches, CompatPostProcess
- **Plugin-based compat system** — 7 built-in plugins (MVCF, VerbFramework, FactionDef, NoTranslate, ScenarioDef, AncientMarketLibrary, NodeReplacement), all DI-registered
- **System.Text.Json source generation** for AOT-safe settings serialization
- **356 automated tests** across Domain, Application, Infrastructure, Integration, and UI layers (was 1)
- **Byte-identical parity** with v1 extraction output for the sample mod baseline
- **Cross-platform release pipeline** — GitHub Actions matrix (win-x64, linux-x64, osx-arm64), tag-triggered

### Breaking changes

- `Prefabs.dat` replaced by `settings.json` — automatic migration on first launch if legacy file is present
- WinForms UI (`RimWorldExtractor.exe`) removed from distribution — preserved in `legacy/` for reference
- The `tools.py` packaging scripts are legacy-only; new releases are built by the GitHub Actions workflow

### Notes on v2.0 scope (deferred to v2.1)

- `rimextract analyze` returns exit code 2 with a message — full diff implementation planned for Phase 7
- Native AOT was investigated but blocked by ClosedXML and Serilog trim-incompatibility; CLI ships as single-file + ReadyToRun (~120 MB self-contained)

---

## [1.x] — Legacy

See [GitHub Releases](https://github.com/csh1668/RimworldExtractor/releases) for the v1 release history.
The v1 codebase is preserved in `legacy/` for reference and hotfix maintenance.
