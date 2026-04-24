# RimworldExtractor v2

**An extraction and translation tool for RimWorld mods.** Supports `Defs`, `Keyed`, `Strings`, and `Patches` extraction with full XML inheritance resolution, TranslationHandle, Full-List Translation, TKey (SlateRef), and patch-based content.

**림추출기 v2:** 림월드 공식 컨텐츠 및 모드의 번역 데이터 추출 도구.

### Download

Grab the latest binaries from [Releases](https://github.com/csh1668/RimworldExtractor/releases):

| Platform | CLI | GUI |
|----------|-----|-----|
| Windows x64 | `rimextract-win-x64.exe` | `RimworldExtractor-GUI-win-x64.zip` |
| Linux x64 | `rimextract-linux-x64` | `RimworldExtractor-GUI-linux-x64.tar.gz` |
| macOS ARM64 | `rimextract-osx-arm64` | `RimworldExtractor-GUI-osx-arm64.zip` |

---

## Quick Start

### GUI

1. Download and extract the GUI archive for your platform.
2. Launch `RimworldExtractor.Ui.Avalonia` (or `.exe` on Windows).
3. On first launch, configure paths: **RimWorld installation**, **Steam Workshop**, and **output directory**.
4. Select a mod, choose format (`Languages/` XML, XLSX, or XML with comments), and click **Extract**.

### CLI

```bash
# Extract a mod to Languages/ XML (default format)
rimextract extract --mod "YourModName" --out ./output

# Extract to XLSX
rimextract extract --mod "ludeon.rimworld.royalty" --out ./output --format xlsx

# Override game version
rimextract extract --mod "SomeMod" --out ./output --version 1.6

# Use a custom settings file
rimextract --config /path/to/settings.json extract --mod "SomeMod" --out ./output

# Convert an existing XLSX back to Languages/ XML
rimextract convert --input extraction.xlsx --output ./Languages
```

---

## CLI Reference

### `rimextract extract`

Extracts translatable strings from a mod and writes output files.

| Option | Description |
|--------|-------------|
| `--mod <name>` | Mod name, `packageId`, or folder ID (required) |
| `--out <dir>` | Output directory (required) |
| `--format <fmt>` | `languages` (default), `xlsx`, or `comments` |
| `--version <x.y>` | Override RimWorld version (e.g. `1.6`) |

Exit codes: `0` = success, `1` = error.

### `rimextract convert`

Converts an XLSX extraction file back to RimWorld `Languages/` XML.

| Option | Description |
|--------|-------------|
| `--input <file.xlsx>` | Source XLSX file (required) |
| `--output <dir>` | Output directory (required) |

### `rimextract analyze`

> Not implemented in v2.0. Full diff support is planned for v2.1.
> Use the GUI for comparison workflows in the meantime.

Exit code `2`.

### Global option

`--config <path>` — override the settings file path (defaults to `%APPDATA%\RimworldExtractor\settings.json` on Windows, `~/.config/rimworld-extractor/settings.json` on Linux/macOS).

---

## GUI Features

- Cross-platform desktop UI built with **Avalonia** (Windows, macOS, Linux).
- Settings editor: paths, language codes, extraction rules, node replacements, version.
- Mod browser with search and filtering.
- Output formats: `Languages/` XML, XLSX (spreadsheet), and XML with original-language comments.
- XML ↔ XLSX round-trip conversion.
- Compat plugins for popular framework mods: MVCF, VerbFramework, FactionDef extras, NoTranslate, ScenarioDef, AncientMarketLibrary, and node-replacement rules.

---

## Migration from v1

v2 is a complete rewrite. Settings are automatically migrated on first launch:

- `Prefabs.dat` → `settings.json` (automatic via `LegacyPrefabsReader` on first run with old file present)
- The extraction pipeline produces byte-identical output to v1 for all baseline sample mods.
- The legacy `tools.py` packaging scripts remain in `legacy/` for v1 release maintenance.

**Breaking changes:**
- `Prefabs.dat` is no longer written; edit `settings.json` directly or use the GUI.
- The WinForms UI (`RimWorldExtractor.exe`) is replaced by the Avalonia GUI. The old binary is preserved in `legacy/` for reference.

---

## Building from Source

**Prerequisites:** .NET 10 SDK (see `global.json` for exact version).

```bash
git clone https://github.com/csh1668/RimworldExtractor.git
cd RimworldExtractor

# Build everything
dotnet build RimworldExtractor.slnx -c Release

# Run tests
dotnet test RimworldExtractor.slnx -c Release

# Run the GUI
dotnet run --project src/RimworldExtractor.Ui.Avalonia

# Publish CLI (single-file, self-contained)
dotnet publish src/RimworldExtractor.Cli -c Release -r win-x64 \
  --self-contained -p:PublishSingleFile=true -o publish/cli
```

---

## Architecture

v2 follows Clean Architecture with these layers:

| Project | Role |
|---------|------|
| `RimworldExtractor.Domain` | Entities, value objects, enums, settings, domain abstractions |
| `RimworldExtractor.Application` | Pipeline stages (9), compat registry, mod discovery |
| `RimworldExtractor.Infrastructure` | File system, XML, Excel (ClosedXML), settings JSON, output strategies |
| `RimworldExtractor.Plugins` | 7 built-in compat plugins (DI-registered) |
| `RimworldExtractor.Ui.Avalonia` | Cross-platform desktop GUI (Avalonia) |
| `RimworldExtractor.Cli` | CLI binary (`rimextract`) using System.CommandLine |

---

## Contributing

Pull requests are welcome. Please:

1. Open an issue to discuss significant changes before implementation.
2. Run `dotnet test` and `dotnet format --verify-no-changes` before submitting.
3. Target the `feat/remake-v2` branch for v2 features; `master` for v1 hotfixes.
