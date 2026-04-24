# Phase 5 — Avalonia 11 UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` or `superpowers:executing-plans`. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Build a cross-platform Avalonia 11.3.12 GUI that replicates every feature of the legacy WinForms app: mod selection, settings editing, extraction with progress, translation analyzer, initial path setup. MVVM strict — no logic in code-behind beyond `InitializeComponent()`.

**Architecture:** CommunityToolkit.Mvvm source-generated `[ObservableProperty]` / `[RelayCommand]`. One ViewModel per View. ViewModels receive the Phase 4 pipeline + Phase 3 infrastructure via constructor injection. Views are pure AXAML + `DataContext` bindings. Dialogs/file-pickers through `IDialogService` abstraction. An `InteractiveConflictResolver` replaces the default `PolicyBasedConflictResolver` for UI-driven runs.

**Tech Stack:** .NET 10 · Avalonia 11.3.12 · Avalonia.Themes.Fluent · CommunityToolkit.Mvvm 8.4.0 · xunit.v3 · FluentAssertions 6.12.2.

**Branch:** `feat/remake-v2` at Phase 4 gate (329 tests, snapshot parity).

**Legacy references (read-only for UX structure only):**
- `legacy/RimworldExtractorGUI/FormMain.cs` + `.Designer.cs`
- `legacy/RimworldExtractorGUI/FormSettings.cs`
- `legacy/RimworldExtractorGUI/FormSelectMod.cs`
- `legacy/RimworldExtractorGUI/FormTranslationAnalyzer.cs`
- `legacy/RimworldExtractorGUI/FormInitialPathSelect.cs`
- `legacy/RimworldExtractorGUI/FormStopCallback.cs`

Use these only to understand the UX workflows and fields; do NOT port the WinForms code literally.

---

## Sub-batch overview

| # | Sub-batch | Tasks | Outcome |
|---|-----------|-------|---------|
| 5A | MVVM foundation | T1-T4 | `App.axaml`, base types, DI integration, `ILocalizer` + resource loading |
| 5B | Services | T5-T8 | `IDialogService`, `IFilePicker`, `InteractiveConflictResolver`, `ExtractionCoordinator` |
| 5C | Settings VM + View | T9-T11 | `SettingsViewModel`, `SettingsView`, round-trip binding tests |
| 5D | Mod selection VM + View | T12-T14 | `ModSelectViewModel`, `ModSelectView`, populates from `ModDiscoveryService` |
| 5E | Main VM + View (extraction flow) | T15-T18 | `MainViewModel` (drives the pipeline), `MainView`, progress wiring |
| 5F | Analyzer + Initial-path VMs | T19-T22 | `AnalyzerViewModel` (translation drift detection), `InitialPathViewModel` |
| 5G | Resources + i18n | T23-T24 | `Strings.ko.json`, `Strings.en.json`, localizer wire-up |
| 5H | Manual smoke test + verification | T25-T27 | Windows smoke test doc, Linux smoke test doc, final gate |

**~27 tasks, 7-8 dispatches.**

---

## Key design decisions

1. **No code-behind logic.** Every View's code-behind file contains only `public partial class XxxView : UserControl { public XxxView() => InitializeComponent(); }`. No event handlers, no manual wiring. Bind everything via AXAML.

2. **ViewModels constructed via DI.** `App.axaml.cs` builds the service provider at startup; Views receive VMs through a `ViewLocator` that maps View → ViewModel by naming convention.

3. **Single-window design.** `MainView` is the shell; other views are tab items or popups. Simpler than multi-window like legacy, more modern.

4. **Extraction is non-blocking.** `MainViewModel.ExtractCommand` is `[RelayCommand(AllowConcurrentExecutions = false)]` and uses `IProgress<ExtractionProgress>` to update the VM's `Progress` and `StatusMessage` properties which the View binds to.

5. **`ModDiscoveryService.DiscoverAll()` is the only place that enumerates mods.** The UI's Mod Select dialog binds to its cached list.

6. **Settings are edited in memory then saved atomically.** `SettingsViewModel` has a local `AppSettings` clone; on Save, it pushes through `ISettingsStore.SaveAsync`.

7. **Conflict resolution is interactive.** When user initiates extraction, we register a scoped `InteractiveConflictResolver` that displays a dialog for each collision.

8. **i18n via JSON resources loaded at startup.** `ILocalizer.GetString("main.button.extract")` returns localized text. Default: Korean (legacy matches); fallback: English.

9. **Avalonia 11.3.12 gotchas**:
   - `Application.xaml` → `App.axaml` (already handled in Phase 1 scaffold)
   - Use `Window.DataContext` binding, NOT code-behind
   - StorageProvider (Avalonia 11+) for file pickers — no WinForms fallback needed
   - Dispose pattern on services — Avalonia doesn't call Dispose on VMs by default; explicit cleanup in `Window.Closing` via DI container scope

10. **Testing strategy:**
    - Unit tests for every ViewModel property + command (NSubstitute for pipeline/services)
    - Integration tests for DialogService / FilePicker mocks
    - NO Avalonia UI rendering tests (headless Avalonia is possible but adds complexity; skip for Phase 5 — manual smoke test covers end-to-end)

---

## Group 5A — MVVM foundation

### Task 1: ViewModelBase + DI integration

**Files:**
- Create: `src/RimworldExtractor.Ui.Avalonia/ViewModels/ViewModelBase.cs`
- Modify: `src/RimworldExtractor.Ui.Avalonia/App.axaml.cs`
- Modify: `src/RimworldExtractor.Ui.Avalonia/Program.cs`

ViewModelBase:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace RimworldExtractor.Ui.Avalonia.ViewModels;

public abstract class ViewModelBase : ObservableObject { }
```

App.axaml.cs changes: build a service provider in `OnFrameworkInitializationCompleted()`, register Infrastructure + Application + UI services, resolve the `MainWindow` (or `MainView`) VM and set it as DataContext.

Commit: `feat(ui): add ViewModelBase + DI-integrated App.axaml.cs`

### Task 2: ViewLocator (map View → ViewModel by convention)

File: `src/RimworldExtractor.Ui.Avalonia/ViewLocator.cs`

Standard Avalonia MVVM pattern:
```csharp
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null) return null;
        var viewTypeName = data.GetType().FullName!.Replace("ViewModel", "View");
        var type = Type.GetType(viewTypeName);
        return type is null
            ? new TextBlock { Text = "View not found for " + viewTypeName }
            : (Control)Activator.CreateInstance(type)!;
    }
    public bool Match(object? data) => data is ViewModelBase;
}
```

Register in `App.axaml`: `<Application.DataTemplates><local:ViewLocator/></Application.DataTemplates>`.

Commit: `feat(ui): add ViewLocator (convention-based View↔ViewModel mapping)`

### Task 3: ILocalizer + JsonLocalizer

File: `src/RimworldExtractor.Ui.Avalonia/Services/ILocalizer.cs` + `JsonLocalizer.cs`

```csharp
public interface ILocalizer
{
    string CurrentLanguage { get; }
    string GetString(string key);
    void SwitchLanguage(string language);
}

public sealed class JsonLocalizer : ILocalizer
{
    private Dictionary<string, string> _strings = new();
    public string CurrentLanguage { get; private set; } = "ko";
    // Load Resources/Strings.{lang}.json from embedded resources
    // Fallback to English key if missing
}
```

Commit: `feat(ui): add ILocalizer + JsonLocalizer`

### Task 4: UI DI extension method

File: `src/RimworldExtractor.Ui.Avalonia/DependencyInjection.cs`

Registers: `ILocalizer` → `JsonLocalizer`, all VMs as transient, all UI-specific services (DialogService, FilePicker, InteractiveConflictResolver).

Commit: `feat(ui): add AddUi DI extension method`

---

## Group 5B — Services (4 tasks)

### Task 5: IDialogService

Abstracts message dialogs + confirmation dialogs. Avalonia-implementation returns `Task<bool>`. Test mock returns deterministic values.

### Task 6: IFilePicker

Abstracts folder/file picking via Avalonia's `StorageProvider`. `Task<string?> PickFolderAsync(string title)` etc.

### Task 7: InteractiveConflictResolver

Implements `IConflictResolver` (from Phase 3). Displays a dialog when a collision occurs, returns the user's choice.

### Task 8: ExtractionCoordinator

Application-layer service that orchestrates the "user clicked Extract" flow: get selected mod, resolve reference mods, invoke pipeline, handle exceptions, report progress. Not strictly necessary (MainViewModel could do this) but extracts the logic out of the VM for testability.

Each task: interface + impl + test + commit `feat(ui): add <Service>`.

---

## Group 5C — Settings VM + View (3 tasks)

### Task 9: SettingsViewModel

Properties:
- `[ObservableProperty]` for each AppSettings field (Paths, Languages, flags)
- `[RelayCommand]` SaveAsync → pushes to ISettingsStore
- `[RelayCommand]` BrowseRimworldPath / BrowseWorkshopPath → uses IFilePicker

### Task 10: SettingsView.axaml

Pure XAML bindings. `TextBox` / `CheckBox` / `ComboBox` for languages (populated from `LanguageCode` list in code-behind? NO — from VM's `AvailableLanguages` property).

### Task 11: SettingsView tests (unit)

ViewModel tests: set properties, invoke Save, verify ISettingsStore.SaveAsync called with correct AppSettings.

Commit each.

---

## Group 5D — Mod Select VM + View (3 tasks)

### Task 12: ModSelectViewModel

Properties:
- `ObservableCollection<ModItem>` — mods discovered via ModDiscoveryService
- `ModItem? SelectedMod`
- `[RelayCommand]` Refresh, SelectAll, Deselect, OpenFolder
- `ObservableCollection<FolderItem> ExtractableFolders` — folders for selected mod

### Task 13: ModSelectView.axaml

`ListBox` bound to `Mods` with custom `ItemTemplate` showing mod name + id + thumbnail (mod `About/Preview.png` if present).

### Task 14: Tests

Commit each.

---

## Group 5E — Main VM + View + extraction flow (4 tasks)

### Task 15: MainViewModel

Properties:
- `[ObservableProperty] bool IsExtracting`
- `[ObservableProperty] double Progress`
- `[ObservableProperty] string StatusMessage`
- `[ObservableProperty] ModItem? SelectedMod`
- `[RelayCommand]` ExtractAsync with `CancellationToken` → calls ExtractionCoordinator, reports progress, writes output via selected `IOutputStrategy`

### Task 16: MainView.axaml

Top-level: `TabControl` with tabs {Extract, Settings, Analyzer}. Extract tab: ModSelect on left, extraction controls on right, progress bar + log at bottom.

### Task 17: Progress reporting test

MainViewModel.ExtractCommand with mocked pipeline that reports progress → VM properties updated.

### Task 18: Cancellation test

VM.CancelCommand sets CancellationTokenSource.Cancel, pipeline throws OperationCanceledException, VM transitions to "Cancelled" state.

---

## Group 5F — Analyzer + Initial-path (4 tasks)

### Task 19: AnalyzerViewModel

Legacy `FormTranslationAnalyzer` provides "translation drift detection" — compare old Excel with new extraction, find changed/removed/added entries.

Port the analyzer logic to a service (already exists in legacy `TranslationAnalyzerTool.cs` — port to Infrastructure or Application). For Phase 5 MVP, the VM can call the pipeline then diff against a user-selected Excel file via `ClosedXmlReader`.

### Task 20: AnalyzerView.axaml

### Task 21: InitialPathViewModel

Shown at first launch if `AppSettings.Paths.Rimworld` is empty. Prompts user for RimWorld + Workshop paths.

### Task 22: InitialPathView.axaml

Commit each.

---

## Group 5G — i18n resources (2 tasks)

### Task 23: Strings.ko.json

File: `src/RimworldExtractor.Ui.Avalonia/Resources/Strings.ko.json` (embedded resource). Keys for all UI strings currently hardcoded in Korean in legacy forms. Extract the master list from legacy.

Commit: `feat(ui): add Korean localization strings`

### Task 24: Strings.en.json

Translate the Korean strings to English. Mechanical work; ask for help if any ambiguous phrases.

Commit: `feat(ui): add English localization strings`

---

## Group 5H — Smoke tests + gate (3 tasks)

### Task 25: Windows smoke test walkthrough doc

File: `docs/smoke-test-phase5-windows.md`

Manual checklist:
1. Launch the app on Windows 10/11
2. First run: InitialPathView appears, enter paths, save
3. MainView loads with mods discovered
4. Select a mod (e.g., Core official content)
5. Click Extract → progress bar moves, completes, output file appears
6. Open Settings, change translation language, save
7. Re-extract → output reflects new language
8. Close + reopen → settings persisted

### Task 26: Linux smoke test walkthrough doc

File: `docs/smoke-test-phase5-linux.md`

Same checklist on Ubuntu/Fedora. Known issues: `libfontconfig1` may need install.

### Task 27: Phase 5 verification gate

- `dotnet build RimworldExtractor.slnx -c Release` → 0W/0E
- `dotnet test RimworldExtractor.slnx -c Release` → all green (no UI rendering tests, just ViewModel logic)
- Launch UI on both platforms, complete checklists
- Push branch

---

## Scope discipline

- NO code-behind logic. If you find yourself writing a handler, STOP and move it to a ViewModel command.
- NO `DispatcherTimer` or threading primitives directly in VMs. Use `IProgress<T>` + async commands.
- NO dependency on Avalonia types from ViewModels. If you need `IStorageProvider`, inject `IFilePicker` instead.
- NO hardcoded strings. Every user-facing string goes through `ILocalizer.GetString`.
- NO WinForms compat. The legacy app stays in `legacy/`; Phase 5 is a fresh UI.

---

## Known risks

- **Avalonia 11.3.12 vs 12.x API drift.** Plugin project was bumped to 11.3.12 in Phase 1. Stay on 11.x unless something's fundamentally broken.
- **Headless test mode.** Avalonia.Headless package enables UI tests without a display. SKIPPED for Phase 5 — manual smoke test is sufficient. If flaky behavior appears in Phase 6, add headless UI tests then.
- **ClosedXML font dependency on Linux.** Noted in Phase 3. If Linux smoke test fails at Excel write, document `sudo apt install libfontconfig1` as a prerequisite.
- **`InitialPathView` as first-run screen.** Implementing "if Paths.Rimworld is empty, show InitialPathView, else show MainView" requires either a router or a simple conditional in App.axaml.cs. Prefer the latter (simpler).

---

## Task execution order

1. **5A** — foundation (T1-T4)
2. **5B** — services (T5-T8)
3. **5C** — Settings VM+View (T9-T11)
4. **5D** — ModSelect VM+View (T12-T14)
5. **5E** — Main VM+View + extraction (T15-T18)
6. **5F** — Analyzer + Initial path (T19-T22)
7. **5G** — i18n (T23-T24)
8. **5H** — manual smoke tests + gate (T25-T27)

~8 dispatches.

---

## Execution Handoff

Plan saved. Continue autonomous subagent-driven execution.
