# 🔄 RimworldExtractor 리메이크 계획서

> **작성일**: 2026-02-19
> **현재 상태**: .NET 7 / WinForms / BinaryFormatter 기반
> **목표**: .NET 8 LTS / MVVM + Clean Architecture / 크로스 플랫폼 지원

---

## 📊 1부: 현재 코드 분석 및 문제점

### 1.1 프로젝트 구조 현황

```
RimworldExtractor/
├── RimworldExtractorGUI/         # WinForms UI (net7.0-windows)
│   ├── FormMain.cs/Designer.cs   # 메인 폼
│   ├── FormSettings.cs           # 설정 폼
│   ├── FormSelectMod.cs          # 모드 선택 폼
│   ├── FormTranslationAnalyzer.cs # 번역 분석기
│   ├── FormXmlister.cs           # XML 리스터
│   ├── FormImageFileCombiner.cs  # 이미지+파일 결합
│   └── FormInitialPathSelect.cs  # 초기 경로 설정
├── RimworldExtractorInternal/    # 핵심 로직 라이브러리 (net7.0)
│   ├── Extractor.cs              # 번역 추출 메인 (14KB+)
│   ├── Extractor.DefsUtils.cs    # Defs 추출 유틸 (18KB+)
│   ├── IO.cs                     # 엑셀/XML 입출력 (43KB+)
│   ├── ModLister.cs              # 모드 목록 관리 (14KB+)
│   ├── Prefabs.cs                # 설정값 저장/로드 (14KB+)
│   ├── Log.cs                    # 로깅
│   ├── Utils.cs                  # 유틸리티
│   ├── LibreExcelFixer.cs        # LibreOffice 호환 수정
│   ├── TranslationAnalyzerTool.cs # 번역 분석 도구
│   ├── DataTypes/                # 데이터 타입
│   └── Compats/                  # 호환성 모듈
└── RimworldExtractorTest/        # 테스트 (net7.0, MSTest)
    └── UnitTest1.cs              # 테스트 1개
```

### 1.2 🚨 심각한 문제점들

#### ❌ A. 오래된 .NET 버전 — `net7.0` (EOL)

```xml
<!-- RimworldExtractorInternal.csproj -->
<TargetFramework>net7.0</TargetFramework>
```

- **.NET 7은 2024년 5월에 지원 종료(EOL)** 되었음
- 보안 패치 및 버그 수정이 더 이상 제공되지 않음
- 현재 LTS 버전인 **.NET 8** (2026년 11월까지 지원) 또는 **.NET 9**로 마이그레이션 필요

#### ❌ B. `BinaryFormatter` 사용 — 보안 취약점

```csharp
// Prefabs.cs
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
```

- `BinaryFormatter`는 **.NET 8부터 완전히 제거**되었으며, 심각한 보안 취약점(원격 코드 실행, RCE)이 존재
- `Prefabs.dat` 파일의 직렬화/역직렬화에 사용 중이며, .NET 8 마이그레이션 시 **빌드 자체가 불가능**
- 설정 파일 포맷의 전면 교체 필요 (→ JSON)

#### ❌ C. Windows Forms — 레거시 UI 프레임워크

```xml
<!-- RimworldExtractorGUI.csproj -->
<TargetFramework>net7.0-windows</TargetFramework>
<UseWindowsForms>true</UseWindowsForms>
```

- **Windows 전용**이라 macOS/Linux 사용자는 사용 불가
- WinForms의 Designer 기반 코드는 **UI와 비즈니스 로직이 강하게 결합**
- 현대적 UI/UX 구현이 어렵고, DPI 스케일링 등에 취약
- 오래된 종속성: `Microsoft-WindowsAPICodePack-Shell 1.1.5` (더 이상 유지보수되지 않는 패키지)

#### ❌ D. God Class & 관심사 미분리

```csharp
// Prefabs.cs — 하나의 static 클래스에 모든 것이 집중
public static class Prefabs
{
    private static readonly string Version = "9";
    public static bool EnableTkey = false;
    public static string PathRimworld = string.Empty;
    public static string PathWorkshop = string.Empty;
    public static string PathBaseRefList = "";
    public static string CurrentVersion = string.Empty;
    public static string PatternVersion = string.Empty;
    public static string PatternVersionWithV = string.Empty;
    public static string OriginalLanguage = string.Empty;
    public static string TranslationLanguage = string.Empty;
    public static bool CommentOriginal = false;
    // ... ExtractionRule까지 한 클래스에 전부 포함
}
```

| 클래스 | 파일 크기 | 문제 |
|--------|-----------|------|
| `IO.cs` | 43KB+ | 엑셀 읽기/쓰기, XML 읽기/쓰기를 단일 static 클래스에서 처리 |
| `Prefabs.cs` | 14KB+ | 설정 저장, 로드, 추출 규칙, 경로 관리, 버전 관리를 모두 담당 |
| `Extractor.cs` + `Extractor.DefsUtils.cs` | 32KB+ | partial class로 나뉘어 있지만 모든 추출 로직이 static 상태에 의존 |
| `ModLister.cs` | 14KB+ | 모드 탐색, 메타데이터 파싱, 캐시 관리를 모두 담당 |

#### ❌ E. 전역 Mutable Static State 과다

```csharp
// Extractor.cs
internal static XmlDocument? CombinedDefs;
public static readonly Dictionary<string, XmlNode> ParentNodeLookUp = new();
private static bool _isOfficialContent = false;
```

- 거의 모든 핵심 클래스가 `static`이며 mutable state를 공유
- **스레드 안전성 없음** — 멀티스레드 환경에서 경합 조건(race condition) 발생 가능
- 테스트 시 상태 격리가 불가능하여 **단위 테스트 작성이 극도로 어려움**

#### ❌ F. UI 스레드 처리 문제

```csharp
// FormTranslationAnalyzer.cs
Task.Factory.StartNew(() => { AnalyzeTranslation(paths); });
// ...
if (labelTitle.InvokeRequired)
{
    labelTitle.Invoke(() => { labelTitle.Text = $"번역 데이터를 분석하고 있습니다... {i}/{paths.Length}"; });
}
```

- `Task.Factory.StartNew` 대신 `async/await` 패턴을 사용해야 함
- `InvokeRequired` + `Invoke()` 패턴은 WinForms 특유의 boilerplate로, MVVM 패턴에서는 불필요
- 진행률 보고에 `IProgress<T>` 미사용

#### ❌ G. 테스트 코드 부실

```csharp
// UnitTest1.cs — 프로젝트 전체에 테스트가 단 1개
[TestMethod]
public void TestMethod1()
{ 
    Prefabs.Init();
    Prefabs.PathRimworld = "C:\Games\Steam\steamapps\common\RimWorld";
    Prefabs.PathWorkshop = "C:\Games\Steam\steamapps\workshop\content\294100";
    // ...
}
```

- 테스트가 **단 1개**, 이름도 `TestMethod1`으로 의미 없음
- **하드코딩된 로컬 경로**에 의존 → CI/CD에서 실행 불가
- 핵심 비즈니스 로직(Extractor, IO, ModLister 등)에 대한 테스트가 전무

#### ❌ H. 하드코딩된 언어 목록

```csharp
// FormSettings.cs — 동일한 언어 목록이 두 번 중복 하드코딩
comboBoxOriginalLanguage.Items.AddRange(new object[]
{
    "English", "Korean (한국어)", "Catalan (Català)", ...
});
comboBoxTranslationLanguage.Items.AddRange(new object[]
{
    "English", "Korean (한국어)", "Catalan (Català)", ...
});
```

- 동일한 언어 목록이 **두 번 중복 하드코딩**
- 언어 추가/수정 시 여러 곳을 수정해야 하며, 오류 가능성 높음

#### ❌ I. 불안정한 NuGet 패키지 버전

```xml
<!-- RimworldExtractorInternal.csproj -->
<PackageReference Include="ClosedXML" Version="0.104.0-preview2" />
```

- **preview 버전**을 프로덕션에서 사용 중 — API 변경, 버그 위험

---

## 📋 2부: 리메이크 설계

### 2.1 🏗️ 새로운 프로젝트 구조 (Clean Architecture)

```
RimworldExtractor.sln
│
├── src/
│   ├── RimworldExtractor.Core/              # 🔵 도메인 모델 & 인터페이스
│   │   ├── Models/
│   │   │   ├── TranslationEntry.cs
│   │   │   ├── ModMetadata.cs
│   │   │   ├── ExtractableFolder.cs
│   │   │   ├── ExtractionRule.cs
│   │   │   └── RequiredMods.cs
│   │   ├── Interfaces/
│   │   │   ├── IExtractor.cs
│   │   │   ├── IModLister.cs
│   │   │   ├── ITranslationIO.cs
│   │   │   ├── ISettingsProvider.cs
│   │   │   └── ITranslationAnalyzer.cs
│   │   └── Enums/
│   │       ├── ExtractionMethod.cs
│   │       └── SupportedLanguage.cs
│   │
│   ├── RimworldExtractor.Application/       # 🟢 유즈케이스 & 비즈니스 로직
│   │   ├── Services/
│   │   │   ├── ExtractionService.cs
│   │   │   ├── TranslationAnalysisService.cs
│   │   │   ├── ConversionService.cs         # XML ↔ XLSX 변환
│   │   │   └── ModDiscoveryService.cs
│   │   └── DependencyInjection.cs
│   │
│   ├── RimworldExtractor.Infrastructure/    # 🟡 외부 의존성 구현
│   │   ├── FileSystem/
│   │   │   ├── ModLister.cs
│   │   │   └── FileSystemHelper.cs
│   │   ├── Excel/
│   │   │   ├── ClosedXmlTranslationIO.cs
│   │   │   └── LibreOfficeExcelFixer.cs
│   │   ├── Xml/
│   │   │   ├── XmlDefParser.cs
│   │   │   ├── XmlPatchProcessor.cs
│   │   │   └── XmlTranslationWriter.cs
│   │   ├── Settings/
│   │   │   └── JsonSettingsProvider.cs      # JSON 기반 설정
│   │   └── DependencyInjection.cs
│   │
│   ├── RimworldExtractor.UI/               # 🔴 프레젠테이션 레이어
│   │   ├── (Option A) Avalonia UI/          # 크로스 플랫폼
│   │   │   ├── Views/
│   │   │   ├── ViewModels/
│   │   │   └── App.axaml
│   │   └── (Option B) WPF/                 # Windows 전용 (간편)
│   │       ├── Views/
│   │       ├── ViewModels/
│   │       └── App.xaml
│   │
│   └── RimworldExtractor.CLI/              # 🟣 CLI 인터페이스 (Optional)
│       └── Program.cs
│
├── tests/
│   ├── RimworldExtractor.Core.Tests/
│   ├── RimworldExtractor.Application.Tests/
│   └── RimworldExtractor.Infrastructure.Tests/
│
├── .github/
│   └── workflows/
│       ├── ci.yml                           # PR/푸시 시 빌드+테스트
│       └── release.yml                      # 태그 시 자동 릴리즈
│
└── Directory.Build.props                    # 공통 프로젝트 속성
```

### 2.2 ⚙️ 기술 스택 업그레이드

| 항목 | 현재 (Before) | 리메이크 (After) | 이유 |
|------|--------------|-----------------|------|
| **.NET 버전** | .NET 7 (EOL) | **.NET 8 LTS** | LTS 지원, 성능 개선, BinaryFormatter 제거 대응 |
| **UI 프레임워크** | WinForms | **Avalonia UI 11** 또는 **WPF** | 크로스 플랫폼 / MVVM 네이티브 지원 |
| **아키텍처 패턴** | 없음 (static 위주) | **MVVM + Clean Architecture** | 관심사 분리, 테스트 용이성 |
| **DI 컨테이너** | 없음 | **Microsoft.Extensions.DependencyInjection** | 의존성 관리 표준화 |
| **설정 직렬화** | `BinaryFormatter` (.dat) | **System.Text.Json** (.json) | 보안, 가독성, 호환성 |
| **로깅** | 커스텀 `Log` static 클래스 | **Microsoft.Extensions.Logging** + **Serilog** | 표준화, 구조화된 로깅, 파일/콘솔 출력 |
| **Excel 라이브러리** | ClosedXML 0.104.0-preview2 | **ClosedXML stable** 또는 **MiniExcel** | 안정 버전 사용 |
| **테스트 프레임워크** | MSTest 2.x (테스트 1개) | **xUnit** + **FluentAssertions** + **NSubstitute** | 현대적, 풍부한 생태계 |
| **파일 다이얼로그** | WindowsAPICodePack (deprecated) | **Avalonia FilePicker** 또는 **CommunityToolkit** | 유지보수 가능한 패키지 |
| **비동기 패턴** | `Task.Factory.StartNew` + `Invoke` | **async/await** + `IProgress<T>` | 표준 패턴, 가독성, 안전성 |
| **MVVM 툴킷** | 없음 | **CommunityToolkit.Mvvm** | Source Generator 기반 boilerplate 감소 |

### 2.3 📐 핵심 설계 변경

#### A. DI 기반 인스턴스화 (Static → Instance)

**Before:**
```csharp
// 모든 것이 static, 전역 상태 공유
public static partial class Extractor
{
    internal static XmlDocument? CombinedDefs;
    private static bool _isOfficialContent = false;
    
    public static List<TranslationEntry> ExtractTranslationData(...)
    {
        // Prefabs.CurrentVersion 등 전역 상태 직접 참조
    }
}
```

**After:**
```csharp
// 인터페이스 정의 (Core)
public interface IExtractor
{
    Task<List<TranslationEntry>> ExtractAsync(
        ModMetadata modMetadata,
        List<ExtractableFolder> selectedFolders,
        List<ModMetadata>? referenceMods,
        IProgress<ExtractionProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

// 구현 (Application)
public class ExtractionService : IExtractor
{
    private readonly ISettingsProvider _settings;
    private readonly IModLister _modLister;
    private readonly IXmlDefParser _xmlDefParser;

    public ExtractionService(
        ISettingsProvider settings,
        IModLister modLister,
        IXmlDefParser xmlDefParser)
    {
        _settings = settings;
        _modLister = modLister;
        _xmlDefParser = xmlDefParser;
    }

    public async Task<List<TranslationEntry>> ExtractAsync(...)
    {
        // 인스턴스 상태만 사용, 전역 상태 참조 없음
    }
}
```

#### B. JSON 기반 설정 (BinaryFormatter → System.Text.Json)

**Before (Prefabs.cs):**
```csharp
using System.Runtime.Serialization.Formatters.Binary;

public static void Save()
{
    var formatter = new BinaryFormatter();
    using var stream = File.Create("Prefabs.dat");
    formatter.Serialize(stream, ...);  // ⚠️ .NET 8에서 완전 제거됨
}
```

**After (JsonSettingsProvider.cs):**
```csharp
public class JsonSettingsProvider : ISettingsProvider
{
    private const string SettingsFile = "settings.json";
    
    public AppSettings Current { get; private set; } = new();

    public async Task LoadAsync()
    {
        if (!File.Exists(SettingsFile)) return;
        var json = await File.ReadAllTextAsync(SettingsFile);
        Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new();
    }

    public async Task SaveAsync()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(Current, options);
        await File.WriteAllTextAsync(SettingsFile, json);
    }
}
```

**설정 모델 (AppSettings.cs):**
```csharp
public class AppSettings
{
    public int Version { get; set; } = 1;
    public string PathRimworld { get; set; } = string.Empty;
    public string PathWorkshop { get; set; } = string.Empty;
    public string CurrentGameVersion { get; set; } = string.Empty;
    public string OriginalLanguage { get; set; } = "English";
    public string TranslationLanguage { get; set; } = "Korean";
    public bool CommentOriginal { get; set; } = false;
    public List<string> ExtractionRules { get; set; } = new();
}
```

> 💡 **마이그레이션**: 기존 사용자를 위해 `Prefabs.dat`이 존재하면 한 번 읽어서 `settings.json`으로 변환하는 마이그레이션 로직을 첫 실행 시 포함

#### C. 언�� 목록 Enum화 (하드코딩 제거)

**Before:**
```csharp
// FormSettings.cs — 동일한 배열이 2번 반복
comboBoxOriginalLanguage.Items.AddRange(new object[]
{
    "English", "Korean (한국어)", "Catalan (Català)", ...
});
comboBoxTranslationLanguage.Items.AddRange(new object[]
{
    "English", "Korean (한국어)", "Catalan (Català)", ...  // 중복!
});
```

**After:**
```csharp
// Core/Enums/SupportedLanguage.cs — Single Source of Truth
public enum SupportedLanguage
{
    [Display(Name = "English")]
    English,

    [Display(Name = "Korean (한국어)")]
    Korean,

    [Display(Name = "Japanese (日本語)")]
    Japanese,

    [Display(Name = "ChineseSimplified (简体中文)")]
    ChineseSimplified,

    [Display(Name = "ChineseTraditional (繁體中文)")]
    ChineseTraditional,

    // ... 나머지 언어들
}

// 사용 시
var languages = Enum.GetValues<SupportedLanguage>()
    .Select(lang => lang.GetDisplayName())
    .ToList();
```

#### D. MVVM ViewModel (UI 로직 분리)

**Before (FormTranslationAnalyzer.cs):**
```csharp
// UI 코드에 비즈니스 로직이 직접 포함
Task.Factory.StartNew(() => { AnalyzeTranslation(paths); });

private void AnalyzeTranslation(string[] paths)
{
    if (labelTitle.InvokeRequired)
    {
        labelTitle.Invoke(() => { labelTitle.Text = "분석 중..."; });
    }
}
```

**After (MainViewModel.cs):**
```csharp
public partial class MainViewModel : ObservableObject
{
    private readonly IExtractionService _extractionService;
    private readonly IModDiscoveryService _modDiscoveryService;

    [ObservableProperty] private ModMetadata? _selectedMod;
    [ObservableProperty] private bool _isExtracting;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _statusMessage = "준비됨";

    public MainViewModel(
        IExtractionService extractionService,
        IModDiscoveryService modDiscoveryService)
    {
        _extractionService = extractionService;
        _modDiscoveryService = modDiscoveryService;
    }

    [RelayCommand]
    private async Task ExtractAsync(CancellationToken cancellationToken)
    {
        IsExtracting = true;
        var progressReporter = new Progress<ExtractionProgress>(p =>
        {
            Progress = p.Percentage;
            StatusMessage = p.Message;
        });

        try
        {
            await _extractionService.ExtractAsync(
                SelectedMod!, progressReporter, cancellationToken);
            StatusMessage = "추출 완료!";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "추출이 취소되었습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"오류: {ex.Message}";
        }
        finally
        {
            IsExtracting = false;
        }
    }
}
```

#### E. IO 클래스 분리 (43KB God Class 해체)

**Before:**
```csharp
// IO.cs (43KB) — 모든 I/O가 한 클래스에
public static class IO
{
    public static void ToExcel(...) { }
    public static List<TranslationEntry> FromExcel(...) { }
    public static void ToXml(...) { }
    public static XmlDocument ReadXml(...) { }
    public static IEnumerable<string> DescendantFiles(...) { }
    // ... 수백 줄의 메서드들
}
```

**After:**
```
Infrastructure/
├── Excel/
│   ├── IExcelReader.cs         → FromExcel 관련
│   ├── IExcelWriter.cs         → ToExcel 관련
│   ├── ClosedXmlReader.cs      → 구현
│   └── ClosedXmlWriter.cs      → 구현
├── Xml/
│   ├── IXmlDefParser.cs        → XML Def 파싱
│   ├── IXmlTranslationWriter.cs → XML 번역 파일 생성
│   ├── XmlDefParser.cs         → 구현
│   └── XmlTranslationWriter.cs → 구현
└── FileSystem/
    ├── IFileSystemHelper.cs    → 파일 탐색
    └── FileSystemHelper.cs     → 구현
```

---

## 📏 3부: 개발 규칙 (Coding Guidelines)

### 3.1 코드 품질 규칙

| # | 규칙 | 설명 |
|---|------|------|
| 1 | **No Static Mutable State** | 전역 static 변수 금지. 모든 상태는 DI로 주입된 서비스 인스턴스가 관리 |
| 2 | **Interface First** | 모든 서비스는 인터페이스를 먼저 정의하고 구현. 테스트 시 mock 가능하도록 설계 |
| 3 | **Async by Default** | I/O 작업(파일, 엑셀, XML)은 반드시 async/await 사용. `CancellationToken` 지원 필수 |
| 4 | **MVVM 엄격 준수** | ViewModel에서 View(UI) 타입 직접 참조 금지. `IDialogService` 등 추상화 사용 |
| 5 | **Nullable Reference Types** | 모든 프로젝트에서 `<Nullable>enable</Nullable>` 유지. `null!` 사용 최소화 |
| 6 | **Single Responsibility** | 한 클래스/메서드는 하나의 책임만. 파일 크기 500줄 초과 시 분리 검토 |
| 7 | **No Hardcoded Values** | 파일 경로, 매직 넘버, 문자열 리터럴은 상수 또는 설정으로 분리 |
| 8 | **XML Doc Comments** | Public API에는 반드시 `<summary>` XML 문서 주석 작성 |

### 3.2 테스트 규칙

| # | 규칙 | 설명 |
|---|------|------|
| 1 | **테스트 커버리지** | Core/Application 레이어 **80% 이상** 커버리지 목표 |
| 2 | **테스트 네이밍** | `MethodName_Scenario_ExpectedResult` 형식 (예: `Extract_WithValidMod_ReturnsTranslations`) |
| 3 | **외부 의존성 Mock** | 파일 시스템, Excel 등 외부 의존성은 반드시 mock/stub 사용 |
| 4 | **테스트 데이터** | 하드코딩된 로컬 경로 대신 테스트 프로젝트 내 fixture 파일 사용 |
| 5 | **CI에서 실행 가능** | 모든 테스트는 특정 환경에 의존하지 않고 CI에서 실행 가능해야 함 |

### 3.3 Git 워크플로우 규칙

| # | 규칙 | 설명 |
|---|------|------|
| 1 | **PR-based Workflow** | `main` 브랜치 직접 푸시 금지. 모든 변경은 PR로 진행, CI 통과 필수 |
| 2 | **Conventional Commits** | 커밋 메시지 형식: `feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:` |
| 3 | **Branch Naming** | `feat/기능명`, `fix/버그명`, `refactor/대상명` 형식 |
| 4 | **Squash Merge** | PR 머지 시 Squash Merge를 기본으로 사용하여 깔끔한 히스토리 유지 |

### 3.4 프로젝트 공통 설정

```xml
<!-- Directory.Build.props — 모든 프로젝트에 자동 적용 -->
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

---

## 🗓️ 4부: 마이그레이션 로드맵

### Phase 1: 기반 구축 (2~3주)

- [ ] .NET 8 LTS 마이그레이션
- [ ] BinaryFormatter → System.Text.Json 교체
- [ ] Clean Architecture 프로젝트 구조 생성 (src/, tests/ 분리)
- [ ] DI 컨테이너 설정 (Microsoft.Extensions.DependencyInjection)
- [ ] `Directory.Build.props`로 공통 속성 통합
- [ ] CI/CD 파이프라인 구축 (GitHub Actions — 빌드 + 테스트)
- [ ] 기존 `Prefabs.dat` → `settings.json` 마이그레이션 유틸리티 작성

### Phase 2: Core & Application 리팩토링 (3~4주)

- [ ] 모델 클래스 정리 → `RimworldExtractor.Core/Models/`
- [ ] 서비스 인터페이스 정의 → `RimworldExtractor.Core/Interfaces/`
- [ ] `Extractor` → `ExtractionService` 리팩토링 (static → instance)
- [ ] `ModLister` → `ModDiscoveryService` 리팩토링
- [ ] `IO.cs` (43KB) → `ExcelReader`, `ExcelWriter`, `XmlDefParser`, `XmlTranslationWriter` 분리
- [ ] `Prefabs` → `AppSettings` + `JsonSettingsProvider` 분리
- [ ] `Log` → `Microsoft.Extensions.Logging` + `Serilog` 교체
- [ ] 단위 테스트 작성 (xUnit + NSubstitute + FluentAssertions)
- [ ] 언어 목록 `SupportedLanguage` enum 생성

### Phase 3: UI 리메이크 (3~4주)

- [ ] UI 프레임워크 선정: **Avalonia UI** (크로스 플랫폼) 또는 **WPF** (Windows 한정)
- [ ] CommunityToolkit.Mvvm 적용
- [ ] ViewModel 구현 (MainViewModel, SettingsViewModel, ModSelectViewModel 등)
- [ ] View 구현 (XAML — 기존 WinForms 기능 1:1 이전)
- [ ] async/await + IProgress<T> 적용으로 UI 반응성 확보
- [ ] IDialogService를 통한 파일/폴더 다이얼로그 추상화
- [ ] WindowsAPICodePack 의존성 제거

### Phase 4: 안정화 & 배포 (1~2주)

- [ ] 통합 테스트 작성 및 수행
- [ ] 기존 사용자 마이그레이션 가이드 문서 작성
- [ ] README.md 전면 업데이트
- [ ] GitHub Actions release 워크플로우 (태그 → 자동 빌드 → 릴리즈)
- [ ] 첫 v2.0.0 릴리즈

---

## 💡 5부: 추가 권장 사항

### 5.1 CLI 지원 추가

`RimworldExtractor.CLI` 프로젝트를 추가하여 GUI 없이 명령줄에서 추출 가능하도록 함.
- CI/CD 파이프라인에서 자동화된 번역 추출에 유용
- 예: `rimextract extract --mod "ModName" --output ./translations --format xlsx`

### 5.2 플러그인 아키텍처

`Compats/` 폴더의 호환성 모듈(XmlExtensions, MVCF 등)을 플러그인 패턴으로 전환:
- `ICompatPlugin` 인터페이스 정의
- 런타임에 플러그인 동적 로드
- 새로운 모드 호환성 추가 시 코어 코드 수정 불필요

### 5.3 UI 다국어 지원 (Localization)

UI 문자열을 리소스 파일로 분리하여 다국어 UI 지원:
- 현재 모든 UI 텍스트가 한국어로 하드코딩되어 있음
- `.resx` 또는 Avalonia의 리소스 시스템 활용
- 한국어를 기본으로 하되, 영어 등 추가 가능하도록 설계

### 5.4 자동 업데이트 기능

- GitHub Releases API를 활용한 앱 내 업데이트 확인
- 현재 `linkLabelLatestVersion`으로 수동 확인하는 방식에서 자동화로 전환

---

## 📌 UI 프레임워크 선택 가이드

| 기준 | Avalonia UI | WPF |
|------|------------|-----|
| **크로스 플랫폼** | ✅ Windows, macOS, Linux | ❌ Windows 전용 |
| **MVVM 지원** | ✅ 네이티브 | ✅ 네이티브 |
| **학습 곡선** | 중간 (XAML 유사하나 차이 있음) | 낮음 (WinForms에서 전환 시) |
| **생태계 성숙도** | 성장 중 | 매우 성숙 |
| **커뮤니티 크기** | 중간 | 큼 |
| **디자인 도구** | VS 미지원, Rider 지원 | VS Designer 완전 지원 |
| **권장 시나리오** | macOS/Linux 사용자 존재 시 | Windows 전용으로 충분 시 |

> **권장**: 림월드가 macOS/Linux도 지원하므로, 장기적으로 **Avalonia UI**를 추천합니다. 단, 개발 속도를 우선한다면 WPF로 시작 후 Avalonia로 전환하는 전략도 가능합니다.

---

*이 문서는 [csh1668/RimworldExtractor](https://github.com/csh1668/RimworldExtractor) 코드베이스 분석을 기반으로 작성되었습니다.*