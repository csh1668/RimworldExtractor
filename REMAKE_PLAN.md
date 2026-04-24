# 🔄 RimworldExtractor 리메이크 계획서 (v2)

> **작성일**: 2026-04-24
> **현재 상태**: .NET 7 / WinForms / static-heavy 단일 어셈블리
> **목표**: **.NET 10 LTS** / Clean Architecture + MVVM / 크로스 플랫폼 / 테스트 가능 / 플러그인 아키텍처

---

## 📊 1부: 현재 코드 정밀 분석

이전 계획서 초안은 일부 부정확한 근거(`BinaryFormatter` 사용 단정 등)를 담고 있었습니다. 실제 코드를 파일 단위로 재검토한 결과를 바탕으로 다시 정리합니다.

### 1.1 프로젝트 구조 현황

```
RimworldExtractor/
├── RimworldExtractorGUI/              # WinForms UI (net7.0-windows)
│   ├── FormMain.cs/Designer.cs
│   ├── FormSettings.cs
│   ├── FormSelectMod.cs
│   ├── FormTranslationAnalyzer.cs
│   ├── FormTranslationAnalyzerPathSelect.cs
│   ├── FormInitialPathSelect.cs
│   ├── FormStopCallback.cs            # 중복 발생 시 사용자 선택 다이얼로그
│   ├── FormXmlister.cs
│   └── FormImageFileCombiner.cs
├── RimworldExtractorInternal/         # 핵심 라이브러리 (net7.0)
│   ├── Extractor.cs                   # 메인 파이프라인 (374 L, static)
│   ├── Extractor.DefsUtils.cs         # Defs 추출 상세 (469 L)
│   ├── IO.cs                          # Excel/XML I/O (900 L, static)
│   ├── Prefabs.cs                     # 설정 + 추출 규칙 (288 L, static)
│   ├── ModLister.cs                   # 모드 탐색 + 캐시 (static)
│   ├── PatchOperations.cs             # PatchOperation* 처리
│   ├── Log.cs                         # 리플렉션 기반 정적 로거
│   ├── LibreExcelFixer.cs             # LibreOffice XLSX 후처리
│   ├── TranslationAnalyzerTool.cs     # 번역 드리프트 검출기
│   ├── Utils.cs
│   ├── Program.cs                     # 사실상 빈 스텁 (11 L)
│   ├── DataTypes/                     # TranslationEntry, ModMetadata 등 5개
│   └── Compats/                       # BaseCompat + 서브클래스 + CompatManager
└── RimworldExtractorTest/             # 테스트 1개 (하드코딩 경로 의존)
```

### 1.2 🚨 실제 문제점 (확인된 근거와 함께)

#### ❌ A. .NET 7 (EOL) — 2024-05 지원 종료

`RimworldExtractorInternal.csproj` / `RimworldExtractorGUI.csproj` 모두 `net7.0` / `net7.0-windows`. 보안 패치와 런타임 개선이 끊긴 지 2년 가까이 지났습니다. **2026-04 현재 LTS는 .NET 10**(2025-11 릴리즈, 2028-11까지 지원)입니다.

#### ❌ B. 직렬화 — BinaryFormatter는 미사용, 실제 문제는 별개

`Prefabs.cs:6`에 `using System.Runtime.Serialization.Formatters.Binary;` 가 남아 있지만 **실제로 사용되지 않습니다**. `Save()`는 `File.WriteAllLines`로 라인 구분 텍스트를 기록하고, `Load()`는 인덱스 순서대로 파싱합니다(`Prefabs.cs:196-253`).

**진짜 문제는 다음입니다:**

- 순서 의존적 포맷 — 필드 순서가 바뀌면 `idx++` 인덱스 계산이 전부 어긋남
- 스키마 버전은 정수 문자열 하나(`Version = "9"`)이며, 불일치 시 `SerializationException`을 던져 **설정이 통째로 리셋**됨
- 중첩/컬렉션을 슬래시/파이프로 인코딩(`NodeReplacement`는 `key1|val1/key2|val2` 형식, `ExtractableTags`는 `tag+white,list-black,list` 같은 DSL을 문자열 하나로 표현) — 파싱·디버깅이 어렵고 특수문자에 취약
- 쓰기 실패 시 부분 손상 가능(원자적 교체 없음)

#### ❌ C. WinForms — Windows 전용 + 디자이너 결합

`RimworldExtractorGUI.csproj`: `net7.0-windows` + `UseWindowsForms=true`. `Microsoft-WindowsAPICodePack-Shell 1.1.5` 의존성 포함(폴더 선택 다이얼로그용, 현재 유지보수 중단 상태).

#### ❌ D. God Class + 전역 가변 상태

| 클래스 | 라인 수 | 책임 |
|--------|---------|------|
| `IO.cs` | 900 | `ToExcel`/`ModifyExcel`/`FromExcel`/`ToLanguageXml`/`FromLanguageXml`/`DescendantFiles`/`SaveSafely` 전부 한 static 클래스 |
| `Extractor.cs` + `DefsUtils.cs` | 843 | 파이프라인 전체 + `CombinedDefs`, `ParentNodeLookUp`, `_isOfficialContent` 정적 가변 필드 |
| `Prefabs.cs` | 288 | 유저 설정 + 런타임 캐시 + 추출 DSL + 콜백까지 |
| `ModLister.cs` | ~400 | 모드 탐색 + 메타데이터 파싱 + 캐시 |

확인된 전역 가변 상태 예시:

```csharp
// Extractor.cs:16-19
internal static XmlDocument? CombinedDefs;
public static readonly Dictionary<string, XmlNode> ParentNodeLookUp = new();
private static bool _isOfficialContent = false;
```

추출 실행마다 `Reset()` 호출로 초기화하지만, **동시 실행 불가**·테스트 격리 불가.

#### ❌ E. Compat 시스템 — 리플렉션 기반 자동 등록

`CompatManager.cs:16-33`이 정적 생성자에서 `Assembly.GetExecutingAssembly().GetTypes()`로 `BaseCompat` 서브클래스를 찾아 `Activator.CreateInstance`로 인스턴스화하고 `[CompatPriority]` 애트리뷰트로 정렬합니다.

```csharp
var compatTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type =>
    baseType.IsAssignableFrom(type) && type is { IsAbstract: false, IsClass: true });
foreach (var compatType in compatTypes)
{
    var compat = (BaseCompat?)Activator.CreateInstance(compatType);
    if (compat != null) compats.Add(compat);
}
```

- 의존성 주입 불가(매개변수 없는 생성자만)
- 런타임 외부 로드 불가 — "플러그인"이라 부르기 어려움
- AOT / 트리밍에 적대적 — .NET 10 AOT CLI 빌드에 장애
- 테스트 시 특정 compat만 실행할 방법 없음

#### ❌ F. `XmlDocument` 레거시 API

전체 XML 처리가 `System.Xml.XmlDocument` + `XmlNode` + `XPath` 조합. `Extractor.cs`는 이미 `using System.Xml.Linq`를 임포트해두었지만 실제로는 쓰지 않습니다. `XDocument` / `XElement`는 value-equality, LINQ 쿼리, immutable-friendly 설계로 파이프라인 스테이지 간 데이터 전달에 훨씬 적합.

#### ❌ G. 파일 충돌 처리 — delegate 콜백 주입

`Prefabs.StopCallbackXlsx`, `StopCallbackXml`, `StopCallbackTxt`가 `Action<...>?` 필드로 선언되어 있고 GUI 쪽 `FormStopCallback`이 이를 채웁니다. Infrastructure가 UI에게 제어를 역주입하는 구조로, Core/Infra/UI 경계를 침범.

#### ❌ H. UI 스레드 처리

`Task.Factory.StartNew` + `InvokeRequired` + `Invoke(...)` 패턴. `async/await` + `IProgress<T>` + `CancellationToken` 미사용. 취소 불가, 진행률 보고 불일관.

#### ❌ I. 테스트 빈약

`UnitTest1.TestMethod1()` 하나, `Prefabs.PathRimworld = "C:\\Games\\Steam\\..."` 하드코딩 → CI 실행 불가.

#### ❌ J. NuGet

- `ClosedXML 0.104.0-preview2` — 프리뷰 버전을 프로덕션에 사용
- `Microsoft-WindowsAPICodePack-Shell 1.1.5` — 유지보수 중단

#### ❌ K. 하드코딩된 DSL + 언어 목록

`Prefabs.Init()`(`Prefabs.cs:168-194`)에 300여 개 태그 이름이 슬래시 구분 문자열로 하드코딩. 언어 드롭다운은 `FormSettings`에 두 번 복붙.

#### ❌ L. 로컬라이제이션 부재

로그 접두사(`"에러"`, `"경고"`, `"메시지"`), 오류 메시지, 폼 레이블이 전부 한국어 리터럴. `.resx` 파일들은 디자이너가 생성한 레이아웃 전용으로 문자열 리소스가 없음.

---

## 📋 2부: 리메이크 설계

### 2.1 🏗️ 프로젝트 구조 (Clean Architecture + Pipeline)

```
RimworldExtractor.sln
│
├── src/
│   ├── RimworldExtractor.Domain/                # 🔵 순수 도메인 (의존성 0)
│   │   ├── Entities/
│   │   │   ├── Mod.cs                           # 불변 레코드
│   │   │   ├── ModMetadata.cs
│   │   │   ├── ExtractableFolder.cs
│   │   │   ├── TranslationEntry.cs              # record + IReadOnlyDictionary
│   │   │   └── RequiredMods.cs
│   │   ├── ValueObjects/
│   │   │   ├── GameVersion.cs                   # "1.6" 등을 타입으로
│   │   │   ├── PackageId.cs
│   │   │   ├── DefName.cs
│   │   │   └── LanguageCode.cs                  # IETF BCP 47 기반
│   │   ├── Rules/
│   │   │   ├── ExtractionRule.cs                # Tag + Whitelist + Blacklist (record)
│   │   │   ├── NodeReplacementRule.cs
│   │   │   └── TranslationHandle.cs
│   │   ├── Abstractions/                        # 도메인 서비스 인터페이스
│   │   │   ├── IExtractor.cs
│   │   │   ├── IExtractionPipeline.cs
│   │   │   └── IConflictResolver.cs
│   │   ├── Results/
│   │   │   └── Result<T>.cs                     # 예외 대신 성공/실패 표현
│   │   └── Enums/
│   │       ├── ExtractionFormat.cs              # Excel / Languages / LanguagesWithComments
│   │       ├── DuplicatesPolicy.cs
│   │       └── FolderKind.cs                    # Defs / Keyed / Strings / Patches
│   │
│   ├── RimworldExtractor.Application/           # 🟢 유즈케이스
│   │   ├── Extraction/
│   │   │   ├── Stages/                          # 파이프라인 스테이지 (각각 ≤200L)
│   │   │   │   ├── IExtractionStage.cs
│   │   │   │   ├── LoadReferenceDefsStage.cs
│   │   │   │   ├── ApplyPatchesStage.cs
│   │   │   │   ├── ResolveInheritanceStage.cs
│   │   │   │   ├── ExtractDefsStage.cs
│   │   │   │   ├── ExtractKeyedStage.cs
│   │   │   │   ├── ExtractStringsStage.cs
│   │   │   │   ├── ExtractPatchesStage.cs
│   │   │   │   ├── CompatPreProcessStage.cs
│   │   │   │   └── CompatPostProcessStage.cs
│   │   │   ├── ExtractionContext.cs             # 한 실행당 단일 인스턴스 (CombinedDefs, LookUp 등 캡슐화)
│   │   │   ├── ExtractionRequest.cs             # 입력 (record)
│   │   │   ├── ExtractionResult.cs              # 출력 (record)
│   │   │   └── ExtractionPipeline.cs            # 스테이지 실행자
│   │   ├── Conversion/
│   │   │   ├── XlsxToLanguagesService.cs
│   │   │   └── LanguagesToXlsxService.cs
│   │   ├── Analysis/
│   │   │   └── TranslationDriftAnalyzer.cs
│   │   ├── ModDiscovery/
│   │   │   └── ModDiscoveryService.cs
│   │   ├── Settings/
│   │   │   └── SettingsMigrator.cs              # Prefabs.dat → settings.json 변환
│   │   └── DependencyInjection.cs
│   │
│   ├── RimworldExtractor.Infrastructure/        # 🟡 I/O 구현
│   │   ├── FileSystem/
│   │   │   ├── FileSystemModLister.cs
│   │   │   └── FileSystemGateway.cs             # IFileSystem 추상화 (테스트용 in-memory 대체 가능)
│   │   ├── Xml/
│   │   │   ├── XDocumentDefParser.cs            # XmlDocument → XDocument 전환
│   │   │   ├── XPatchProcessor.cs
│   │   │   ├── XmlInheritanceResolver.cs
│   │   │   └── XmlLanguagesWriter.cs
│   │   ├── Excel/
│   │   │   ├── ClosedXmlReader.cs
│   │   │   ├── ClosedXmlWriter.cs
│   │   │   └── LibreOfficePostProcessor.cs
│   │   ├── Output/                              # 전략 패턴
│   │   │   ├── IOutputStrategy.cs
│   │   │   ├── ExcelOutputStrategy.cs
│   │   │   ├── LanguagesOutputStrategy.cs
│   │   │   └── LanguagesWithCommentsOutputStrategy.cs
│   │   ├── Settings/
│   │   │   ├── JsonSettingsStore.cs             # System.Text.Json + source generator
│   │   │   └── LegacyPrefabsReader.cs           # Prefabs.dat 역파싱 (마이그레이션 전용, readonly)
│   │   └── DependencyInjection.cs
│   │
│   ├── RimworldExtractor.Plugins/               # 🟠 Compat 플러그인 API
│   │   ├── ICompatPlugin.cs                     # 명시적 계약
│   │   ├── CompatPriorityAttribute.cs
│   │   ├── PluginRegistration.cs                # AddCompatPlugin<T>() 확장 메서드
│   │   └── BuiltIn/
│   │       ├── MvcfCompatPlugin.cs              # 기존 Compat_MVCF
│   │       ├── VerbCompatPlugin.cs
│   │       ├── FactionDefCompatPlugin.cs
│   │       └── ...                              # 기존 10개 compat 포팅
│   │
│   ├── RimworldExtractor.Ui.Avalonia/           # 🔴 Avalonia 11 + CommunityToolkit.Mvvm
│   │   ├── Views/
│   │   ├── ViewModels/
│   │   ├── Services/
│   │   │   ├── DialogService.cs                 # IDialogService 구현
│   │   │   └── FileConflictInteractionService.cs# IConflictResolver 구현 (UI 호출)
│   │   ├── Resources/
│   │   │   ├── Strings.ko.json                  # i18n
│   │   │   └── Strings.en.json
│   │   └── App.axaml
│   │
│   └── RimworldExtractor.Cli/                   # 🟣 .NET 10 Native AOT 빌드 대상
│       ├── Commands/
│       │   ├── ExtractCommand.cs
│       │   ├── ConvertCommand.cs
│       │   └── AnalyzeCommand.cs
│       └── Program.cs
│
├── tests/
│   ├── RimworldExtractor.Domain.Tests/
│   ├── RimworldExtractor.Application.Tests/     # ExtractionPipeline 스테이지별 단위 테스트
│   ├── RimworldExtractor.Infrastructure.Tests/  # in-memory IFileSystem 사용
│   └── RimworldExtractor.Integration.Tests/     # 실제 모드 폴더 fixture (tests/fixtures/sample-mod/)
│
├── samples/
│   └── sample-mod/                              # 합성된 미니 림월드 모드 (CI에서 사용)
│
├── .github/workflows/
│   ├── ci.yml                                   # 빌드 + 테스트 + 트리밍 검증
│   └── release.yml                              # 태그 → 크로스 플랫폼 바이너리 자동 릴리즈
│
├── Directory.Build.props                        # <TargetFramework>net10.0</TargetFramework>
├── Directory.Packages.props                     # 중앙 집중식 패키지 버전 (CPM)
└── global.json                                  # SDK 10.0.x 고정
```

### 2.2 ⚙️ 기술 스택 업그레이드

| 항목 | 현재 | 리메이크 (.NET 10 기준) | 이유 |
|------|------|-------------------------|------|
| **.NET** | net7.0 (EOL) | **.NET 10 LTS** | 2028-11까지 지원, 성능·Native AOT·트리밍 개선 |
| **UI 프레임워크** | WinForms | **Avalonia UI 11** | 크로스 플랫폼 (Win/macOS/Linux), MVVM 네이티브 |
| **CLI** | 없음 | **System.CommandLine** + **Native AOT** | 단일 파일 실행, CI에서 모드 자동 추출 |
| **아키텍처** | static 모놀리스 | **Clean Arch + Pipeline + DI** | 관심사 분리, 동시 실행 가능, 테스트 용이 |
| **DI** | 없음 | **Microsoft.Extensions.DependencyInjection** | 표준 IoC |
| **설정 직렬화** | 순서 의존 텍스트(Prefabs.dat) | **System.Text.Json + STJ Source Generator** | 스키마 가독성, AOT 호환, 원자적 쓰기 |
| **설정 마이그레이션** | N/A | `LegacyPrefabsReader`(읽기 전용) → JSON 1회 변환 | 기존 사용자 무손실 업그레이드 |
| **패키지 버전 관리** | 파편화 | **Central Package Management** (`Directory.Packages.props`) | 버전 일원화 |
| **XML** | `XmlDocument` | **`XDocument` / `XElement`** | LINQ, value-equality, immutable friendly |
| **Excel** | ClosedXML 0.104 preview2 | **ClosedXML 안정 버전** 또는 **DocumentFormat.OpenXml 직접 사용** | 프리뷰 제거 |
| **로깅** | 커스텀 `Log` static | **`Microsoft.Extensions.Logging` + Serilog** | 구조화 로깅, 파일/콘솔 분리, OpenTelemetry 친화적 |
| **Compat 플러그인** | 리플렉션 자동 스캔 | **명시적 DI 등록 + `[CompatPriority]`** | AOT 친화적, 테스트 시 부분 등록 가능 |
| **파일 충돌 처리** | `Prefabs.StopCallback*` delegate 필드 | **`IConflictResolver` 추상화** | UI → Infra 역주입 제거 |
| **파일 다이얼로그** | WindowsAPICodePack (deprecated) | **Avalonia StorageProvider API** | 크로스 플랫폼, 유지보수 |
| **비동기** | `Task.Factory.StartNew` + `Invoke` | **`async/await` + `IProgress<T>` + `CancellationToken`** | 취소 가능, 진행률 표준화 |
| **테스트** | MSTest (1개) | **xUnit + FluentAssertions + NSubstitute + Verify** | 스냅샷 테스트로 XML 출력 검증 |
| **언어 목록** | 두 번 하드코딩 | `LanguageCode` value object + 리소스 파일 단일 소스 | |
| **추출 규칙 DSL** | `"tag+white,list-black,list"` 문자열 | `ExtractionRule` 레코드 + JSON 표현 | 특수문자 안전, 타입 안전 |

> **.NET 10 LTS**: 2025-11-11 릴리즈, 2028-11까지 지원. Native AOT 생태계가 성숙해 `RimworldExtractor.Cli`를 단일 실행 파일로 배포 가능합니다.

### 2.3 📐 핵심 설계 변경 (실제 코드 기반)

#### A. 추출 파이프라인 — 전역 상태 제거

**Before** (`Extractor.cs:16-19`):

```csharp
internal static XmlDocument? CombinedDefs;
public static readonly Dictionary<string, XmlNode> ParentNodeLookUp = new();
private static bool _isOfficialContent = false;
```

→ 실행 간 상태 공유, 동시 실행 불가, 테스트 격리 불가.

**After**: 실행당 1개 `ExtractionContext`를 스테이지 체인에 주입.

```csharp
public sealed class ExtractionContext
{
    public required ExtractionRequest Request { get; init; }
    public XDocument CombinedDefs { get; } = new(new XElement("Defs"));
    public Dictionary<DefName, XElement> ParentLookup { get; } = new();
    public List<TranslationEntry> Results { get; } = new();
    public bool IsOfficialContent => Request.Target.IsOfficialContent;
    public CancellationToken CancellationToken { get; init; }
    public IProgress<ExtractionProgress>? Progress { get; init; }
}

public interface IExtractionStage
{
    Task ExecuteAsync(ExtractionContext ctx);
}

public sealed class ExtractionPipeline(IEnumerable<IExtractionStage> stages) : IExtractionPipeline
{
    public async Task<ExtractionResult> RunAsync(ExtractionRequest req, CancellationToken ct)
    {
        var ctx = new ExtractionContext { Request = req, CancellationToken = ct };
        foreach (var stage in stages)
        {
            ct.ThrowIfCancellationRequested();
            await stage.ExecuteAsync(ctx);
        }
        return new ExtractionResult(ctx.Results);
    }
}
```

각 스테이지는 단일 책임 + ≤200 L + 개별 단위 테스트 가능.

#### B. Compat — 리플렉션 자동 스캔 → 명시적 DI 등록

**Before** (`CompatManager.cs:16-33`): `Assembly.GetExecutingAssembly().GetTypes()` 스캔 + `Activator.CreateInstance`. AOT 불가, DI 불가, 외부 확장 불가.

**After**:

```csharp
public interface ICompatPlugin
{
    void PreProcess(XDocument combinedDefs, ExtractionContext ctx);
    IEnumerable<TranslationEntry> PostProcess(IReadOnlyList<TranslationEntry> entries, ExtractionContext ctx);
}

[CompatPriority(50)]
public sealed class MvcfCompatPlugin(ILogger<MvcfCompatPlugin> logger) : ICompatPlugin { ... }

// Startup 시 명시적 등록 (트리밍/AOT 호환)
services.AddCompatPlugin<MvcfCompatPlugin>()
        .AddCompatPlugin<VerbCompatPlugin>()
        .AddCompatPlugin<FactionDefCompatPlugin>();
```

- DI로 `ILogger`/`ISettings` 주입 가능
- 테스트에서 특정 plugin만 등록하여 단위 테스트
- 향후 외부 어셈블리 로드 원하면 `AssemblyLoadContext` 기반 옵션을 별도 모듈로 추가(기본값은 아님)

#### C. 충돌 해결 — UI 역주입 제거

**Before** (`Prefabs.cs:148-150`):

```csharp
public static Action<XLWorkbook, string>? StopCallbackXlsx = null;
public static Action<XmlDocument, string>? StopCallbackXml = null;
public static Action<IEnumerable<string>, string>? StopCallbackTxt = null;
```

UI가 Infrastructure의 static 필드에 콜백을 주입 — 레이어 경계 침범.

**After**:

```csharp
public interface IConflictResolver
{
    Task<ConflictDecision> ResolveAsync(ConflictContext ctx, CancellationToken ct);
}

public enum ConflictDecision { Overwrite, KeepOriginal, Merge, Abort }

// UI 구현 (Avalonia)
public sealed class InteractiveConflictResolver(IDialogService dialogs) : IConflictResolver { ... }

// CLI 구현
public sealed class PolicyBasedConflictResolver(DuplicatesPolicy policy) : IConflictResolver { ... }
```

DI 등록에서 UI/CLI가 각자 구현을 선택. Core/Infra는 인터페이스만 알면 됨.

#### D. 설정 직렬화 — 순서 의존 텍스트 → JSON + Source Generator

**Before** (`Prefabs.cs:196-253`): `File.WriteAllLines` / `ReadAllLines` + `idx++`. 필드 추가 시 전 유저 설정 리셋.

**After**:

```csharp
public sealed record AppSettings
{
    public int SchemaVersion { get; init; } = 2;
    public PathSettings Paths { get; init; } = new();
    public LanguageSettings Languages { get; init; } = new();
    public ExtractionSettings Extraction { get; init; } = new();
    public OutputSettings Output { get; init; } = new();
}

[JsonSerializable(typeof(AppSettings))]
internal partial class AppSettingsJsonContext : JsonSerializerContext { }

public sealed class JsonSettingsStore(IFileSystem fs, ILogger<JsonSettingsStore> log) : ISettingsStore
{
    private const string Path = "settings.json";
    public async Task SaveAsync(AppSettings s, CancellationToken ct)
    {
        // 원자적 쓰기: tmp 파일 → 검증 → File.Replace
        var tmp = Path + ".tmp";
        await using var stream = fs.OpenWrite(tmp);
        await JsonSerializer.SerializeAsync(stream, s, AppSettingsJsonContext.Default.AppSettings, ct);
        fs.Replace(tmp, Path, backup: Path + ".bak");
    }
}
```

- STJ source generator로 AOT 호환 + 리플렉션 제거
- `File.Replace`로 원자적 교체 + 자동 백업
- 스키마 업그레이드는 `SchemaVersion`별 마이그레이터(`ISettingsMigration`) 체인

**마이그레이션**: `LegacyPrefabsReader`가 `Prefabs.dat`을 읽기 전용으로 파싱해 `AppSettings`로 변환 → 성공 시 `Prefabs.dat`을 `Prefabs.dat.bak`으로 이름 변경. 기존 사용자 손실 없음.

#### E. 추출 규칙 DSL — 문자열 인코딩 제거

**Before**: `"label+ThingDef,PawnKindDef-TrashThingDef"` 를 +/− 구분자로 파싱(`Prefabs.cs:62-132`).

**After**:

```csharp
public sealed record ExtractionRule(
    string Tag,
    IReadOnlySet<DefName> Whitelist,
    IReadOnlySet<DefName> Blacklist)
{
    public bool CanExtract(DefName defName) =>
        (Whitelist.Count == 0 || Whitelist.Contains(defName)) && !Blacklist.Contains(defName);
}
```

JSON 저장 시:

```json
{
  "rules": [
    { "tag": "label" },
    { "tag": "description", "blacklist": ["TrashThingDef"] }
  ]
}
```

레거시 Prefabs.dat의 DSL 문자열은 `LegacyPrefabsReader`가 `ExtractionRule` 레코드로 파싱.

#### F. IO.cs (900 L) 해체

| 현재 메서드 | 이동 대상 | 라인 참조 |
|-------------|-----------|-----------|
| `IO.ToExcel` | `Infrastructure/Excel/ClosedXmlWriter` | IO.cs:75-278 |
| `IO.ModifyExcel` | `Infrastructure/Excel/ClosedXmlWriter.UpdateAsync` | IO.cs:21-74 |
| `IO.FromExcel` | `Infrastructure/Excel/ClosedXmlReader` | IO.cs:280-348 |
| `IO.ToLanguageXml` | `Infrastructure/Xml/XmlLanguagesWriter` | IO.cs:349-668 |
| `IO.FromLanguageXml` | `Infrastructure/Xml/XmlLanguagesReader` | IO.cs:669-720 |
| `IO.SaveSafely` | `Infrastructure/Output/SafeFileWriter` + `IConflictResolver` | IO.cs:723-809 |
| `IO.DescendantFiles` | `Infrastructure/FileSystem/FileSystemGateway.EnumerateFiles` | IO.cs:877-898 |

출력 형식 분기(`Prefabs.Method == Excel/Languages/LanguagesWithComments`)는 전략 패턴으로:

```csharp
public interface IOutputStrategy { Task WriteAsync(ExtractionResult, OutputOptions, CancellationToken); }
// ExcelOutputStrategy, LanguagesOutputStrategy, LanguagesWithCommentsOutputStrategy
```

#### G. MVVM (Avalonia 11 + CommunityToolkit.Mvvm)

```csharp
public partial class MainViewModel(
    IExtractionPipeline pipeline,
    IModDiscoveryService discovery,
    IConflictResolver resolver,
    IDialogService dialogs,
    ILogger<MainViewModel> log) : ObservableObject
{
    [ObservableProperty] private ModMetadata? _selectedMod;
    [ObservableProperty] private bool _isExtracting;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _statusMessage = "준비됨";

    [RelayCommand(CanExecute = nameof(CanExtract))]
    private async Task ExtractAsync(CancellationToken ct)
    {
        IsExtracting = true;
        var progress = new Progress<ExtractionProgress>(p =>
        {
            Progress = p.Percentage;
            StatusMessage = p.Message;
        });
        try
        {
            var req = new ExtractionRequest(SelectedMod!, /* ... */);
            var result = await pipeline.RunAsync(req, ct);
            StatusMessage = $"추출 완료: {result.Entries.Count}건";
        }
        catch (OperationCanceledException) { StatusMessage = "취소됨"; }
        finally { IsExtracting = false; }
    }

    private bool CanExtract() => SelectedMod is not null && !IsExtracting;
}
```

View는 `{Binding ExtractCommand}` / `{Binding Progress}`만 사용. `Invoke` / `InvokeRequired` 불필요 (Avalonia `Dispatcher.UIThread` 자동 처리).

---

## 📏 3부: 개발 규칙

### 3.1 코드 품질

| # | 규칙 | 설명 |
|---|------|------|
| 1 | **Static mutable 금지** | 모든 가변 상태는 DI 서비스 인스턴스가 소유 |
| 2 | **Interface First** | 서비스는 인터페이스 먼저. 모크 가능 |
| 3 | **Async by default** | I/O는 `async/await` + `CancellationToken` 필수 |
| 4 | **Records for DTOs** | `record` / `record struct`로 불변성 보장 |
| 5 | **Nullable enable + Warnings as Errors** | `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` |
| 6 | **SRP / 파일 500 L 초과 금지** | 초과 시 분리 검토 |
| 7 | **AOT/Trim 적대적 API 지양** | 리플렉션 스캔, `dynamic`, `BinaryFormatter` 금지. STJ는 Source Generator만 사용 |
| 8 | **Core/Domain은 의존성 0** | `Domain.csproj`는 NuGet 참조 금지 (System.* 제외) |
| 9 | **MVVM에서 View 타입 참조 금지** | `IDialogService`, `IConflictResolver` 같은 추상화 사용 |
| 10 | **Result 패턴 선호** | 예측 가능한 실패는 예외 대신 `Result<T>` |

### 3.2 테스트

| # | 규칙 | 설명 |
|---|------|------|
| 1 | Domain/Application 커버리지 **80%+** | |
| 2 | `MethodName_Scenario_ExpectedResult` 네이밍 | |
| 3 | 외부 의존성은 `NSubstitute`로 stub | |
| 4 | 파일 시스템은 `System.IO.Abstractions` 또는 자체 `IFileSystem` in-memory 구현 | |
| 5 | XML/XLSX 출력은 `Verify`로 스냅샷 테스트 | |
| 6 | CI에서 실행 가능 — 로컬 경로 하드코딩 금지 | `tests/fixtures/sample-mod/` 사용 |

### 3.3 Git / CI

| # | 규칙 | 설명 |
|---|------|------|
| 1 | `main` 직접 푸시 금지 | PR + CI 통과 필수 |
| 2 | Conventional Commits | `feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`, `perf:` |
| 3 | Branch: `feat/*`, `fix/*`, `refactor/*` | |
| 4 | Squash merge 기본 | |
| 5 | CI: 빌드 + 테스트 + **AOT 게이트** | CLI가 Native AOT로 빌드되는지 워크플로우에서 검증 |

### 3.4 공통 프로젝트 설정

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <IsAotCompatible>true</IsAotCompatible>
    <IsTrimmable>true</IsTrimmable>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
```

```xml
<!-- global.json -->
{ "sdk": { "version": "10.0.100", "rollForward": "latestFeature" } }
```

---

## 🗓️ 4부: 마이그레이션 로드맵

각 Phase 끝에서 **기존 기능이 전부 동작하는 상태**를 유지하는 것이 원칙입니다. "리메이크하며 동시에 사용 가능" 을 목표로 합니다.

### Phase 0: 사전 작업 (~1주)

- [ ] `master` 브랜치 보호 + PR 강제
- [ ] `.github/workflows/ci.yml` — 현 net7 기준 빌드 + 테스트 자동화부터 달성
- [ ] `tests/fixtures/sample-mod/` 합성 모드 구성 (Defs/Keyed/Strings/Patches 각각 최소 샘플)
- [ ] 현 동작의 **출력 스냅샷** 확보 — 리메이크 후 회귀 비교용

### Phase 1: 토대 (~2주)

- [ ] .NET 10 SDK로 전체 프로젝트 타깃 변경 (`net10.0` / `net10.0-windows`)
- [ ] `Directory.Build.props`, `Directory.Packages.props`, `global.json` 도입
- [ ] `ClosedXML` 안정 버전 승격 (또는 `DocumentFormat.OpenXml` 직접 사용 검토)
- [ ] `using System.Runtime.Serialization.Formatters.Binary;` 같은 미사용 import 청소
- [ ] Core Architecture 빈 프로젝트 생성 (`src/*` 레이아웃)

### Phase 2: Domain + Settings (~2주)

- [ ] `RimworldExtractor.Domain` 도메인 모델 (record/value object) 추출
- [ ] `ExtractionRule` DSL → 레코드 + JSON 포맷 설계
- [ ] `JsonSettingsStore` + STJ Source Generator
- [ ] `LegacyPrefabsReader` — 기존 `Prefabs.dat` 1회 변환(읽기 전용)
- [ ] Domain 단위 테스트 (80%+)

### Phase 3: Infrastructure 분해 (~3주)

- [ ] `IO.cs`(900 L) → `ClosedXmlReader/Writer`, `XmlLanguagesReader/Writer`, `FileSystemGateway` 분리
- [ ] `ModLister` → `FileSystemModLister` (캐시는 `IMemoryCache` 기반)
- [ ] `IXmlDefParser` — `XmlDocument` → `XDocument` 전환
- [ ] `IConflictResolver` + `PolicyBasedConflictResolver` / `InteractiveConflictResolver`
- [ ] `Microsoft.Extensions.Logging` + Serilog 도입, `Log` static 제거
- [ ] Infrastructure 테스트 (in-memory `IFileSystem`)

### Phase 4: Application 파이프라인 (~3주)

- [ ] `ExtractionContext` + `ExtractionPipeline` + 스테이지 10개 구현
- [ ] `CompatManager` → DI 기반 `ICompatPlugin` + 10개 내장 플러그인 포팅
- [ ] `TranslationDriftAnalyzer` (현 `TranslationAnalyzerTool` 대체)
- [ ] 스테이지별 단위 테스트 + 통합 테스트 (`sample-mod` fixture)
- [ ] **회귀 검증**: Phase 0 스냅샷과 신규 출력 diff → 0

### Phase 5: UI 리메이크 — Avalonia 11 (~3주)

- [ ] `RimworldExtractor.Ui.Avalonia` 프로젝트 + CommunityToolkit.Mvvm
- [ ] ViewModel 구현: `MainViewModel`, `SettingsViewModel`, `ModSelectViewModel`, `AnalyzerViewModel`
- [ ] `IDialogService` (Avalonia StorageProvider 기반)
- [ ] `async/await` + `IProgress<T>` 기반 UI 반응성
- [ ] 리소스 파일(`Strings.ko.json` / `Strings.en.json`) + 런타임 스위치
- [ ] WinForms 프로젝트는 별도 브랜치로 아카이빙 (v1 유지 관리용)

### Phase 6: CLI + 릴리즈 (~1주)

- [ ] `RimworldExtractor.Cli` — `System.CommandLine` + Native AOT
- [ ] `rimextract extract --mod "X" --output ./out --format xlsx --version 1.6`
- [ ] `release.yml` — 태그 시 Windows/macOS/Linux Avalonia 바이너리 + AOT CLI 자동 빌드/업로드
- [ ] 마이그레이션 가이드 + README 전면 개편
- [ ] v2.0.0 릴리즈

---

## 💡 5부: 추가 권장 사항

### 5.1 관찰 가능성 (Observability)

`Microsoft.Extensions.Logging` + Serilog에 **구조화 로그**(key-value 형태)를 기본으로 도입. OpenTelemetry 익스포터를 옵션으로 연결 가능하게 설계하면 추출 성능 프로파일링을 Grafana 등에 내보낼 수 있음.

### 5.2 Native AOT CLI

.NET 10에서 Native AOT 지원이 성숙했으므로 `RimworldExtractor.Cli`를 단일 실행 파일(~15MB, 콜드 스타트 ≤50ms)로 배포. 모드 번역 CI 파이프라인에서 "PR마다 번역 추출 → 검증" 같은 자동화가 저비용으로 가능.

### 5.3 플러그인 외부 로딩 (Optional)

핵심 10개 compat은 내장 어셈블리로 두고, 원하는 사용자만 `plugins/` 폴더의 외부 어셈블리를 `AssemblyLoadContext`로 동적 로드하는 옵션 모듈을 별도 제공. 기본값은 off — AOT/트리밍 유지.

### 5.4 스냅샷 테스트로 회귀 방지

`Verify` 라이브러리로 "주어진 sample-mod에 대해 추출한 XLSX/DefInjected XML"을 스냅샷화. Phase 0에 현 net7 코드 기준으로 스냅샷을 찍어두면, 리메이크 각 Phase 끝마다 자동 회귀 검증.

### 5.5 UI 다국어 (i18n)

현재 모든 UI 문자열 + 로그 접두사가 한국어 하드코딩. `Strings.{lang}.json` 리소스 + `ILocalizer.GetString(key)` 로 통일. 기본은 한국어, 영어 번역 병기. 장기적으로 림월드 커뮤니티 기여 가능.

### 5.6 자동 업데이트

GitHub Releases API 폴링 → 새 버전 감지 시 다운로드 링크 표시. 현재 `linkLabelLatestVersion`이 수동 링크 클릭 방식 → `IUpdateChecker` 서비스로 추상화.

---

## 📌 UI 프레임워크: Avalonia UI 11 권장

| 기준 | Avalonia 11 | WPF |
|------|-------------|-----|
| 크로스 플랫폼 | ✅ Win / macOS / Linux | ❌ Windows 전용 |
| MVVM | ✅ 네이티브 | ✅ 네이티브 |
| .NET 10 지원 | ✅ | ✅ |
| 학습 곡선 | 중 (XAML 변형) | 저 |
| 생태계 | 성장 중 + CommunityToolkit 호환 | 성숙하나 정체 |
| 디자이너 | Rider / JetBrains 지원 | VS Designer 완전 |
| 권장 상황 | macOS/Linux 사용자 존재 시 | Windows 전용이면 충분 |

림월드가 macOS/Linux도 공식 지원하므로 **Avalonia 11 권장**. WPF로 시작 후 Avalonia 전환 전략은 초기 속도는 유리하지만 UI 코드 두 번 작성 비용이 큼 — 처음부터 Avalonia를 권장.

---

## 🔑 요약: 이 계획서가 기존 계획서와 다른 점

1. **타깃 런타임**: .NET 8 → **.NET 10 LTS** (2028-11까지 지원, AOT 성숙)
2. **BinaryFormatter 오해 정정**: 실제 문제는 *순서 의존 텍스트 포맷*이며, 보안 이슈가 아닌 **스키마 진화 실패 위험** 을 해결 대상으로 재정의
3. **Compat 시스템 실상 반영**: 단순 static 클래스가 아니라 리플렉션 기반 자동 스캔(`CompatManager.cs:16-33`). 해법은 **DI 명시 등록**으로 AOT·테스트 동시 해결
4. **파이프라인 아키텍처 신규 도입**: `ExtractionContext` + `IExtractionStage` × 10으로 전역 가변 상태(`CombinedDefs`, `ParentNodeLookUp`, `_isOfficialContent`) 제거 + 동시 실행 가능
5. **IConflictResolver 추상화**: `Prefabs.StopCallback*` delegate 필드(UI → Infra 역주입)를 레이어 경계를 지키는 인터페이스로 교체
6. **XmlDocument → XDocument 전환**: LINQ/immutable 친화적 파이프라인
7. **Native AOT CLI**: 별도 GUI 없이도 CI/CD에서 활용 가능한 단일 바이너리
8. **Central Package Management + Verify 스냅샷 테스트**: 패키지 버전 일원화 + 현재 출력의 회귀 없는 검증 체계
9. **로드맵 Phase 0 추가**: 리메이크 전에 현 동작의 출력 스냅샷을 먼저 확보해 매 단계 회귀 검증 기준선 확립

---

*이 문서는 [csh1668/RimworldExtractor](https://github.com/csh1668/csh1668-rimworld-extractor)의 2026-04-24 시점 `master` 브랜치 코드(`Extractor.cs` 374L, `IO.cs` 900L, `Prefabs.cs` 288L, `CompatManager.cs` 등)를 실제로 읽고 작성되었습니다.*
