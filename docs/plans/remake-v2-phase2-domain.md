# Phase 2 — Domain + Settings Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Populate `RimworldExtractor.Domain` with immutable records/value objects/rules for every concept in the legacy codebase, and stand up `Infrastructure/Settings/` that round-trips an `AppSettings` through JSON plus converts a legacy `Prefabs.dat` into `settings.json` without data loss.

**Architecture:** Pure-data Domain (no XML, no file I/O, no DSL parsing). Infrastructure owns persistence and legacy-format translation. `System.Text.Json` source generator for AOT-safe serialization. `ISettingsStore` abstraction lives in `Domain.Abstractions`; `JsonSettingsStore` concrete implementation lives in `Infrastructure.Settings`.

**Tech Stack:** .NET 10 · C# records + value objects · System.Text.Json source generator · xunit.v3 · FluentAssertions 6.12.2 · NSubstitute · Verify.XunitV3.

**Branch:** `feat/remake-v2` (continuation). Phase 1 ended at commit `53fde49`.

**Legacy references** (do NOT modify — read-only):
- `legacy/RimworldExtractorInternal/DataTypes/TranslationEntry.cs` — source of truth for entry shape + computed props
- `legacy/RimworldExtractorInternal/DataTypes/ModMetadata.cs` — mod metadata record
- `legacy/RimworldExtractorInternal/DataTypes/ExtractableFolder.cs` — folder record
- `legacy/RimworldExtractorInternal/DataTypes/RequiredMods.cs` — complex DSL-bearing type; simplify in new code
- `legacy/RimworldExtractorInternal/Prefabs.cs` — settings + extraction rule DSL + save/load format

---

## Task Grouping Overview

| Group | Tasks | Outcome |
|-------|-------|---------|
| **2A** | T1-T3: Enums | `DuplicatesPolicy`, `ExtractionFormat`, `FolderKind` |
| **2B** | T4-T7: Value objects | `GameVersion`, `PackageId`, `DefName`, `LanguageCode` |
| **2C** | T8-T10: Entities | `TranslationEntry`, `ModMetadata`, `ExtractableFolder` |
| **2D** | T11-T13: Mod references + Rules | `ModReference`+`RequiredMods`, `ExtractionRule`, `NodeReplacementRule`+`TranslationHandle` |
| **2E** | T14-T16: Settings schema + JSON | `AppSettings` tree, STJ source generator, `ISettingsStore` |
| **2F** | T17-T18: Persistence | `JsonSettingsStore` (atomic write), crash-safety test |
| **2G** | T19-T21: Legacy conversion | DSL parser, `LegacyPrefabsReader`, integration round-trip |
| **2H** | T22: Verification gate | Coverage report + user checkpoint |

Each task = red test → green implementation → commit. Groups don't need their own commit — the per-task commits tell the story.

---

## Group 2A — Enums

### Task 1: DuplicatesPolicy enum

**Files:**
- Create: `src/RimworldExtractor.Domain/Enums/DuplicatesPolicy.cs`
- Test: `tests/RimworldExtractor.Domain.Tests/Enums/DuplicatesPolicyTests.cs`

**Legacy reference:** `legacy/RimworldExtractorInternal/Prefabs.cs:276-281`
```csharp
public enum DuplicatesPolicy { Stop = 0, Overwrite, KeepOriginal }
```

- [ ] **Step 1: Write the failing test** (`tests/RimworldExtractor.Domain.Tests/Enums/DuplicatesPolicyTests.cs`)

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.Enums;

namespace RimworldExtractor.Domain.Tests.Enums;

public class DuplicatesPolicyTests
{
    [Fact]
    public void Enum_HasExpectedMembers_InLegacyOrder()
    {
        var values = Enum.GetValues<DuplicatesPolicy>();

        values.Should().Equal(
            DuplicatesPolicy.Stop,
            DuplicatesPolicy.Overwrite,
            DuplicatesPolicy.KeepOriginal);
    }

    [Theory]
    [InlineData(DuplicatesPolicy.Stop, 0)]
    [InlineData(DuplicatesPolicy.Overwrite, 1)]
    [InlineData(DuplicatesPolicy.KeepOriginal, 2)]
    public void EnumValue_PreservesLegacyOrdinal(DuplicatesPolicy policy, int expected)
    {
        ((int)policy).Should().Be(expected);
    }
}
```

- [ ] **Step 2: Run the test — expect compile failure**

```bash
dotnet test tests/RimworldExtractor.Domain.Tests/ 2>&1 | tail -10
```
Expected: fails with `CS0246: type 'DuplicatesPolicy' could not be found`.

- [ ] **Step 3: Write the implementation** (`src/RimworldExtractor.Domain/Enums/DuplicatesPolicy.cs`)

```csharp
namespace RimworldExtractor.Domain.Enums;

/// <summary>
/// How to handle a translation entry whose key collides with an existing one.
/// Ordinals preserved from legacy Prefabs.dat schema for migration compatibility.
/// </summary>
public enum DuplicatesPolicy
{
    Stop = 0,
    Overwrite = 1,
    KeepOriginal = 2,
}
```

- [ ] **Step 4: Run the test — expect pass**

```bash
dotnet test tests/RimworldExtractor.Domain.Tests/ 2>&1 | grep -E "Passed|Failed|통과|실패"
```
Expected: 2 passed (1 `Fact` + 1 `Theory` with 3 `InlineData` = 3 data cases, but xunit counts as 4 total if it splits; either way 0 failed).

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Domain/Enums/DuplicatesPolicy.cs \
        tests/RimworldExtractor.Domain.Tests/Enums/DuplicatesPolicyTests.cs
git commit -m "feat(domain): add DuplicatesPolicy enum (Stop/Overwrite/KeepOriginal)"
```

### Task 2: ExtractionFormat enum

**Files:**
- Create: `src/RimworldExtractor.Domain/Enums/ExtractionFormat.cs`
- Test: `tests/RimworldExtractor.Domain.Tests/Enums/ExtractionFormatTests.cs`

**Legacy reference:** `legacy/RimworldExtractorInternal/Prefabs.cs:283-286` — renamed from `ExtractionMethod` to `ExtractionFormat` per REMAKE_PLAN §2.2.
```csharp
public enum ExtractionMethod { Excel = 0, Languages, LanguagesWithComments }
```

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.Enums;

namespace RimworldExtractor.Domain.Tests.Enums;

public class ExtractionFormatTests
{
    [Fact]
    public void Enum_HasExpectedMembers_InLegacyOrder()
    {
        var values = Enum.GetValues<ExtractionFormat>();

        values.Should().Equal(
            ExtractionFormat.Excel,
            ExtractionFormat.Languages,
            ExtractionFormat.LanguagesWithComments);
    }

    [Theory]
    [InlineData(ExtractionFormat.Excel, 0)]
    [InlineData(ExtractionFormat.Languages, 1)]
    [InlineData(ExtractionFormat.LanguagesWithComments, 2)]
    public void EnumValue_PreservesLegacyOrdinal(ExtractionFormat format, int expected)
    {
        ((int)format).Should().Be(expected);
    }
}
```

- [ ] **Step 2: Run — expect compile fail.**

- [ ] **Step 3: Write the implementation**

```csharp
namespace RimworldExtractor.Domain.Enums;

/// <summary>
/// Output format choice for a translation extraction.
/// Ordinals preserved from legacy Prefabs.dat (was named <c>ExtractionMethod</c>).
/// </summary>
public enum ExtractionFormat
{
    Excel = 0,
    Languages = 1,
    LanguagesWithComments = 2,
}
```

- [ ] **Step 4: Run — expect pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Domain/Enums/ExtractionFormat.cs \
        tests/RimworldExtractor.Domain.Tests/Enums/ExtractionFormatTests.cs
git commit -m "feat(domain): add ExtractionFormat enum (renamed from legacy ExtractionMethod)"
```

### Task 3: FolderKind enum

**Files:**
- Create: `src/RimworldExtractor.Domain/Enums/FolderKind.cs`
- Test: `tests/RimworldExtractor.Domain.Tests/Enums/FolderKindTests.cs`

**Legacy reference:** `legacy/RimworldExtractorInternal/Extractor.cs:55-68` — the `switch` on `Path.GetFileName(extractableFolder.FolderName)` dispatches on folder name strings `"Defs"`, `"Keyed"`, `"Strings"`, `"Patches"`. Elevate to enum.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.Enums;

namespace RimworldExtractor.Domain.Tests.Enums;

public class FolderKindTests
{
    [Fact]
    public void Enum_HasExactlyFourMembers()
    {
        Enum.GetValues<FolderKind>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData("Defs", FolderKind.Defs)]
    [InlineData("Keyed", FolderKind.Keyed)]
    [InlineData("Strings", FolderKind.Strings)]
    [InlineData("Patches", FolderKind.Patches)]
    public void TryParse_WithCanonicalName_ReturnsKind(string input, FolderKind expected)
    {
        FolderKind.TryParse(input, ignoreCase: false, out var kind).Should().BeTrue();
        kind.Should().Be(expected);
    }

    [Theory]
    [InlineData("defs")]
    [InlineData("DEFS")]
    [InlineData("unknown")]
    public void TryParse_WithWrongCasingOrUnknown_ReturnsFalseOrCaseSensitiveResult(string input)
    {
        var caseSensitive = FolderKind.TryParse(input, ignoreCase: false, out _);
        caseSensitive.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

```csharp
namespace RimworldExtractor.Domain.Enums;

/// <summary>
/// The four translation-bearing folder kinds in a RimWorld mod tree.
/// Names match the on-disk folder names exactly (case-sensitive).
/// </summary>
public enum FolderKind
{
    Defs = 0,
    Keyed = 1,
    Strings = 2,
    Patches = 3,
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Domain/Enums/FolderKind.cs \
        tests/RimworldExtractor.Domain.Tests/Enums/FolderKindTests.cs
git commit -m "feat(domain): add FolderKind enum (Defs/Keyed/Strings/Patches)"
```

---

## Group 2B — Value Objects

Value objects are `readonly record struct` where a single immutable value has invariants (e.g., non-empty). They carry validation in their `static Create` factory or implicit operator. No primitive-obsession.

### Task 4: DefName value object

**Files:**
- Create: `src/RimworldExtractor.Domain/ValueObjects/DefName.cs`
- Test: `tests/RimworldExtractor.Domain.Tests/ValueObjects/DefNameTests.cs`

**Legacy reference:** DefName is the string identifier in `<defName>` elements — used as dictionary keys in `Extractor.ParentNodeLookUp`. Legacy stores as `string`; we strengthen with a value object that rejects empty/whitespace.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Domain.Tests.ValueObjects;

public class DefNameTests
{
    [Theory]
    [InlineData("SampleMod_WoodenSpear")]
    [InlineData("A")]
    [InlineData("ThingDef_With.Dots")]
    public void Create_WithNonEmpty_ReturnsValue(string input)
    {
        var result = DefName.Create(input);

        result.Value.Should().Be(input);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Create_WithEmptyOrWhitespace_Throws(string input)
    {
        var act = () => DefName.Create(input);

        act.Should().Throw<ArgumentException>().WithMessage("*DefName*");
    }

    [Fact]
    public void Create_WithNull_Throws()
    {
        var act = () => DefName.Create(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_IsValueBased()
    {
        var a = DefName.Create("Foo");
        var b = DefName.Create("Foo");
        var c = DefName.Create("Bar");

        a.Should().Be(b);
        a.Should().NotBe(c);
    }

    [Fact]
    public void ToString_ReturnsUnderlyingValue()
    {
        DefName.Create("Foo").ToString().Should().Be("Foo");
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

```csharp
namespace RimworldExtractor.Domain.ValueObjects;

/// <summary>
/// Strongly-typed RimWorld def identifier (the content of <c>&lt;defName&gt;</c>). Non-empty, trimmed.
/// </summary>
public readonly record struct DefName
{
    public string Value { get; }

    private DefName(string value) => Value = value;

    public static DefName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("DefName must be non-empty and not whitespace.", nameof(value));
        return new DefName(value);
    }

    public override string ToString() => Value;
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Domain/ValueObjects/DefName.cs \
        tests/RimworldExtractor.Domain.Tests/ValueObjects/DefNameTests.cs
git commit -m "feat(domain): add DefName value object (non-empty identifier)"
```

### Task 5: PackageId value object

**Files:**
- Create: `src/RimworldExtractor.Domain/ValueObjects/PackageId.cs`
- Test: `tests/RimworldExtractor.Domain.Tests/ValueObjects/PackageIdTests.cs`

**Legacy reference:** PackageId appears throughout `ModLister.cs` and `RequiredMods.cs` as `string packageId`. Canonical form is `author.modname` (dot-separated, case-insensitive in RimWorld). We normalize to lowercase.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Domain.Tests.ValueObjects;

public class PackageIdTests
{
    [Fact]
    public void Create_PreservesInputButNormalizesForComparison()
    {
        var id = PackageId.Create("Ludeon.RimWorld");

        id.Value.Should().Be("Ludeon.RimWorld", "display form keeps original casing");
        id.Normalized.Should().Be("ludeon.rimworld", "comparison key is lowercase");
    }

    [Fact]
    public void Equality_IsCaseInsensitive()
    {
        var a = PackageId.Create("Ludeon.RimWorld");
        var b = PackageId.Create("ludeon.rimworld");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NoDotInside")]
    public void Create_WithEmptyOrMissingDot_Throws(string input)
    {
        var act = () => PackageId.Create(input);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToString_ReturnsOriginalCasing()
    {
        PackageId.Create("Ludeon.RimWorld").ToString().Should().Be("Ludeon.RimWorld");
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

```csharp
namespace RimworldExtractor.Domain.ValueObjects;

/// <summary>
/// RimWorld packageId (e.g. <c>Ludeon.RimWorld</c>). Comparison is case-insensitive; display form preserves original casing.
/// </summary>
public readonly record struct PackageId
{
    public string Value { get; }
    public string Normalized { get; }

    private PackageId(string value, string normalized)
    {
        Value = value;
        Normalized = normalized;
    }

    public static PackageId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("PackageId must be non-empty.", nameof(value));
        if (!value.Contains('.'))
            throw new ArgumentException("PackageId must contain at least one '.' separator.", nameof(value));
        return new PackageId(value, value.ToLowerInvariant());
    }

    public bool Equals(PackageId other) => Normalized == other.Normalized;

    public override int GetHashCode() => Normalized.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Domain/ValueObjects/PackageId.cs \
        tests/RimworldExtractor.Domain.Tests/ValueObjects/PackageIdTests.cs
git commit -m "feat(domain): add PackageId value object (case-insensitive equality)"
```

### Task 6: GameVersion value object

**Files:**
- Create: `src/RimworldExtractor.Domain/ValueObjects/GameVersion.cs`
- Test: `tests/RimworldExtractor.Domain.Tests/ValueObjects/GameVersionTests.cs`

**Legacy reference:** `Prefabs.CurrentVersion` = `"1.6"`. `PatternVersion` = `@"^[1]\.\d+"` (matches `1.0` through `1.99`). `PatternVersionWithV` = `@"^v[1]\.\d+"` (for workshop subfolder names like `v1.6/`). Encapsulate parsing + comparison + match-with-v-prefix here.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Domain.Tests.ValueObjects;

public class GameVersionTests
{
    [Theory]
    [InlineData("1.0", 1, 0)]
    [InlineData("1.6", 1, 6)]
    [InlineData("1.12", 1, 12)]
    public void Parse_WithMajorMinor_SetsComponents(string input, int major, int minor)
    {
        var v = GameVersion.Parse(input);

        v.Major.Should().Be(major);
        v.Minor.Should().Be(minor);
        v.ToString().Should().Be(input);
    }

    [Theory]
    [InlineData("v1.6")]
    [InlineData("1.6.0")]
    [InlineData("2.0")]
    [InlineData("Common")]
    [InlineData("")]
    public void Parse_WithInvalid_Throws(string input)
    {
        var act = () => GameVersion.Parse(input);

        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("1.6", true)]
    [InlineData("v1.6", true)]
    [InlineData("v1.0", true)]
    [InlineData("Common", false)]
    [InlineData("default", false)]
    [InlineData("1", false)]
    public void TryParseAny_AcceptsBothBareAndVPrefixedForms(string input, bool expectedOk)
    {
        var ok = GameVersion.TryParseAny(input, out var result);

        ok.Should().Be(expectedOk);
        if (expectedOk)
        {
            result.Major.Should().Be(1);
        }
    }

    [Fact]
    public void Compare_OrdersByMajorThenMinor()
    {
        var a = GameVersion.Parse("1.5");
        var b = GameVersion.Parse("1.6");
        var c = GameVersion.Parse("1.12");

        a.CompareTo(b).Should().BeNegative();
        b.CompareTo(c).Should().BeNegative();
        c.CompareTo(GameVersion.Parse("1.12")).Should().Be(0);
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

```csharp
using System.Text.RegularExpressions;

namespace RimworldExtractor.Domain.ValueObjects;

/// <summary>
/// RimWorld major.minor version (e.g. <c>1.6</c>). Only the 1.x series is supported
/// per legacy <c>Prefabs.PatternVersion</c> regex.
/// </summary>
public readonly record struct GameVersion : IComparable<GameVersion>
{
    private static readonly Regex BareForm = new(@"^1\.(\d+)$", RegexOptions.Compiled);
    private static readonly Regex VPrefixedForm = new(@"^v1\.(\d+)$", RegexOptions.Compiled);

    public int Major { get; }
    public int Minor { get; }

    private GameVersion(int major, int minor)
    {
        Major = major;
        Minor = minor;
    }

    public static GameVersion Parse(string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new FormatException("GameVersion cannot be empty.");
        var match = BareForm.Match(value);
        if (!match.Success)
            throw new FormatException($"GameVersion '{value}' does not match pattern '1.MINOR'.");
        return new GameVersion(1, int.Parse(match.Groups[1].Value));
    }

    public static bool TryParseAny(string value, out GameVersion result)
    {
        result = default;
        if (string.IsNullOrEmpty(value)) return false;

        var m = BareForm.Match(value);
        if (!m.Success) m = VPrefixedForm.Match(value);
        if (!m.Success) return false;

        result = new GameVersion(1, int.Parse(m.Groups[1].Value));
        return true;
    }

    public int CompareTo(GameVersion other)
    {
        var majorCompare = Major.CompareTo(other.Major);
        return majorCompare != 0 ? majorCompare : Minor.CompareTo(other.Minor);
    }

    public override string ToString() => $"{Major}.{Minor}";
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Domain/ValueObjects/GameVersion.cs \
        tests/RimworldExtractor.Domain.Tests/ValueObjects/GameVersionTests.cs
git commit -m "feat(domain): add GameVersion value object (1.x parsing, v-prefix support)"
```

### Task 7: LanguageCode value object

**Files:**
- Create: `src/RimworldExtractor.Domain/ValueObjects/LanguageCode.cs`
- Test: `tests/RimworldExtractor.Domain.Tests/ValueObjects/LanguageCodeTests.cs`

**Legacy reference:** `Prefabs.OriginalLanguage = "English"`, `TranslationLanguage = "Korean (한국어)"` — display strings, not codes. RimWorld stores translations in `Languages/<Name>/` where `<Name>` is the display name. We keep the display form but add a `FolderName` property that is the display string stripped of the parenthesized native label.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Domain.Tests.ValueObjects;

public class LanguageCodeTests
{
    [Theory]
    [InlineData("English", "English")]
    [InlineData("Korean (한국어)", "Korean")]
    [InlineData("ChineseSimplified (简体中文)", "ChineseSimplified")]
    [InlineData("Japanese (日本語)", "Japanese")]
    public void FolderName_StripsParentheticalNativeLabel(string display, string expectedFolder)
    {
        var lang = LanguageCode.Create(display);

        lang.Display.Should().Be(display);
        lang.FolderName.Should().Be(expectedFolder);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("(한국어)")]
    public void Create_WithEmptyOrMissingName_Throws(string input)
    {
        var act = () => LanguageCode.Create(input);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_IsCaseSensitiveOnDisplay()
    {
        var a = LanguageCode.Create("English");
        var b = LanguageCode.Create("English");
        var c = LanguageCode.Create("english");

        a.Should().Be(b);
        a.Should().NotBe(c, "RimWorld folder names are case-sensitive on POSIX");
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

```csharp
namespace RimworldExtractor.Domain.ValueObjects;

/// <summary>
/// A RimWorld language identifier. Carries both display form (e.g. <c>Korean (한국어)</c>)
/// and the folder-name form (e.g. <c>Korean</c>) used for <c>Languages/{FolderName}/</c> paths.
/// </summary>
public readonly record struct LanguageCode
{
    public string Display { get; }
    public string FolderName { get; }

    private LanguageCode(string display, string folderName)
    {
        Display = display;
        FolderName = folderName;
    }

    public static LanguageCode Create(string display)
    {
        if (string.IsNullOrWhiteSpace(display))
            throw new ArgumentException("LanguageCode display must be non-empty.", nameof(display));
        var folder = StripParenthetical(display).Trim();
        if (folder.Length == 0)
            throw new ArgumentException("LanguageCode must contain a folder-name portion before any parenthetical.", nameof(display));
        return new LanguageCode(display, folder);
    }

    private static string StripParenthetical(string input)
    {
        var open = input.IndexOf('(');
        return open < 0 ? input : input[..open];
    }

    public override string ToString() => Display;
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Domain/ValueObjects/LanguageCode.cs \
        tests/RimworldExtractor.Domain.Tests/ValueObjects/LanguageCodeTests.cs
git commit -m "feat(domain): add LanguageCode value object (display + folder-name split)"
```

---

## Group 2C — Entities

### Task 8: TranslationEntry record

**Files:**
- Create: `src/RimworldExtractor.Domain/Entities/TranslationEntry.cs`
- Test: `tests/RimworldExtractor.Domain.Tests/Entities/TranslationEntryTests.cs`

**Legacy reference:** `legacy/RimworldExtractorInternal/DataTypes/TranslationEntry.cs` defines a record with:
- Primary ctor: `(ClassName, Node, Original, Translated?, RequiredMods?, SourceFile?)`
- Computed props: `ClassNode` = `"{ClassName}+{Node}"`, `DefName` = prefix of Node before first `.`, `RealNode` = suffix after first `.`
- Mutable `_extensions` dictionary (**dropping** — YAGNI until Phase 4 needs it)

We will use a temporary `object? RequiredMods` placeholder for now (actual `RequiredMods` type arrives in Task 12); once `RequiredMods` exists we will refactor the placeholder.

Actually — to avoid refactoring, we'll define `TranslationEntry` with a nullable `RequiredMods` property typed as the forthcoming `Mods.RequiredMods`. That means Task 12 (`RequiredMods` type) must land before Task 8 compiles cleanly. **Task order update:** do Tasks 11-12 (RequiredMods/ModReference) before Task 8.

**Revised Task order: 1-7 → 11 (ModReference) → 12 (RequiredMods) → 8 (TranslationEntry) → 9 (ModMetadata) → 10 (ExtractableFolder) → 13 (Rules) → …**

The numbering below is reorganized:

See restructured task sequence in "Task Execution Order" section at end of plan.

For the purpose of each task's detail, I write them in dependency-resolved order below.

---

### Actual Task Ordering (applied below)

1. Enums (Tasks 1-3)
2. Value objects (Tasks 4-7)
3. **Mod references** (Task 8, was 11): `ModReference`
4. **RequiredMods** (Task 9, was 12)
5. **TranslationEntry** (Task 10, was 8)
6. **ModMetadata** (Task 11, was 9)
7. **ExtractableFolder** (Task 12, was 10)
8. Rules (Tasks 13-15)
9. Settings schema + JSON (Tasks 16-18)
10. Settings store (Tasks 19-20)
11. Legacy conversion (Tasks 21-23)
12. Gate (Task 24)

The task detail below uses the **executed order**.

---

### Task 8 (execution order): ModReference value object

**Files:**
- Create: `src/RimworldExtractor.Domain/Mods/ModReference.cs`
- Test: `tests/RimworldExtractor.Domain.Tests/Mods/ModReferenceTests.cs`

**Legacy reference:** `legacy/.../RequiredMods.cs` uses the internal `PackageIdModNamePair` struct which can be identified either by `PackageId` OR by `ModName`. We simplify into a discriminated value object with a `Kind` enum.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.Mods;

namespace RimworldExtractor.Domain.Tests.Mods;

public class ModReferenceTests
{
    [Fact]
    public void ByPackageId_StoresKindAndValue()
    {
        var r = ModReference.ByPackageId("Ludeon.RimWorld");

        r.Kind.Should().Be(ModReferenceKind.PackageId);
        r.Value.Should().Be("Ludeon.RimWorld");
    }

    [Fact]
    public void ByModName_StoresKindAndValue()
    {
        var r = ModReference.ByModName("Core");

        r.Kind.Should().Be(ModReferenceKind.ModName);
        r.Value.Should().Be("Core");
    }

    [Theory]
    [InlineData(ModReferenceKind.PackageId)]
    [InlineData(ModReferenceKind.ModName)]
    public void Construct_WithEmptyValue_Throws(ModReferenceKind kind)
    {
        var act = () => new ModReference("", kind);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_IsValueBased_AndKindSensitive()
    {
        var a = ModReference.ByPackageId("Foo");
        var b = ModReference.ByPackageId("Foo");
        var c = ModReference.ByModName("Foo");

        a.Should().Be(b);
        a.Should().NotBe(c, "different Kind means different reference even with the same Value");
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

```csharp
namespace RimworldExtractor.Domain.Mods;

/// <summary>Whether a <see cref="ModReference"/> points at a PackageId or a ModName.</summary>
public enum ModReferenceKind
{
    PackageId = 0,
    ModName = 1,
}

/// <summary>
/// A reference to a mod by either its PackageId or its ModName. Used in <see cref="RequiredMods"/> gates.
/// </summary>
public sealed record ModReference(string Value, ModReferenceKind Kind)
{
    public ModReference(string value, ModReferenceKind kind) : this()
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ModReference value must be non-empty.", nameof(value));
        Value = value;
        Kind = kind;
    }

    public static ModReference ByPackageId(string packageId) => new(packageId, ModReferenceKind.PackageId);
    public static ModReference ByModName(string modName) => new(modName, ModReferenceKind.ModName);
}
```

Wait — `sealed record` with a primary constructor cannot have a second explicit constructor with the same signature. Fix: move validation into the primary constructor via init/set with validation, or remove the primary ctor parameter list and use a manual all-args constructor. Use explicit constructor pattern:

```csharp
namespace RimworldExtractor.Domain.Mods;

public enum ModReferenceKind
{
    PackageId = 0,
    ModName = 1,
}

public sealed record ModReference
{
    public string Value { get; }
    public ModReferenceKind Kind { get; }

    public ModReference(string value, ModReferenceKind kind)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ModReference value must be non-empty.", nameof(value));
        Value = value;
        Kind = kind;
    }

    public static ModReference ByPackageId(string packageId) => new(packageId, ModReferenceKind.PackageId);
    public static ModReference ByModName(string modName) => new(modName, ModReferenceKind.ModName);
}
```

Use this form in Step 3.

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Domain/Mods/ModReference.cs \
        tests/RimworldExtractor.Domain.Tests/Mods/ModReferenceTests.cs
git commit -m "feat(domain): add ModReference (PackageId|ModName discriminated)"
```

### Task 9 (execution order): RequiredMods record

**Files:**
- Create: `src/RimworldExtractor.Domain/Mods/RequiredMods.cs`
- Test: `tests/RimworldExtractor.Domain.Tests/Mods/RequiredModsTests.cs`

**Legacy reference:** `legacy/.../RequiredMods.cs` encodes AND-of-ORs: outer list joined with `" && "`, inner list joined with `" || "`, negated group prefixed with `" ~~ "`. We keep the **semantics** (AND-of-ORs over allowed set, plus AND-of-ORs over disallowed set) but drop the DSL string — serialization is Infrastructure's concern.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.Mods;

namespace RimworldExtractor.Domain.Tests.Mods;

public class RequiredModsTests
{
    [Fact]
    public void Empty_HasNoAllowedOrDisallowed()
    {
        var empty = RequiredMods.Empty;

        empty.Allowed.Should().BeEmpty();
        empty.Disallowed.Should().BeEmpty();
        empty.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void WithOneAllowedGroup_StoresIt()
    {
        var mods = new RequiredMods(
            allowed: new[] { new[] { ModReference.ByPackageId("a.b") } },
            disallowed: Array.Empty<ModReference[]>());

        mods.Allowed.Should().HaveCount(1);
        mods.Allowed[0].Should().ContainSingle();
        mods.Allowed[0][0].Value.Should().Be("a.b");
        mods.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Combine_WithNull_ReturnsSelf()
    {
        var self = new RequiredMods(
            allowed: new[] { new[] { ModReference.ByPackageId("a.b") } },
            disallowed: Array.Empty<ModReference[]>());

        self.Combine(null).Should().BeSameAs(self);
    }

    [Fact]
    public void Combine_UnionsAllowedAndDisallowed()
    {
        var a = new RequiredMods(
            allowed: new[] { new[] { ModReference.ByPackageId("a.a") } },
            disallowed: Array.Empty<ModReference[]>());
        var b = new RequiredMods(
            allowed: new[] { new[] { ModReference.ByPackageId("b.b") } },
            disallowed: new[] { new[] { ModReference.ByPackageId("c.c") } });

        var merged = a.Combine(b);

        merged.Allowed.Should().HaveCount(2);
        merged.Disallowed.Should().HaveCount(1);
    }

    [Fact]
    public void Equality_IsStructural()
    {
        var a = new RequiredMods(
            allowed: new[] { new[] { ModReference.ByPackageId("a.b") } },
            disallowed: Array.Empty<ModReference[]>());
        var b = new RequiredMods(
            allowed: new[] { new[] { ModReference.ByPackageId("a.b") } },
            disallowed: Array.Empty<ModReference[]>());

        a.Should().Be(b);
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

```csharp
using System.Collections;

namespace RimworldExtractor.Domain.Mods;

/// <summary>
/// A condition on which mods must / must not be loaded, expressed as AND-of-ORs.
/// Each inner list is an OR-group (any one satisfies); outer lists are ANDed.
/// Legacy DSL serialization (<c>" &amp;&amp; "</c> / <c>" || "</c> / <c>" ~~ "</c>) is handled in Infrastructure.
/// </summary>
public sealed record RequiredMods
{
    public IReadOnlyList<IReadOnlyList<ModReference>> Allowed { get; }
    public IReadOnlyList<IReadOnlyList<ModReference>> Disallowed { get; }

    public static RequiredMods Empty { get; } = new(
        Array.Empty<IReadOnlyList<ModReference>>(),
        Array.Empty<IReadOnlyList<ModReference>>());

    public RequiredMods(
        IEnumerable<IEnumerable<ModReference>> allowed,
        IEnumerable<IEnumerable<ModReference>> disallowed)
    {
        Allowed = allowed.Select(g => (IReadOnlyList<ModReference>)g.ToArray()).ToArray();
        Disallowed = disallowed.Select(g => (IReadOnlyList<ModReference>)g.ToArray()).ToArray();
    }

    public bool IsEmpty => Allowed.Count == 0 && Disallowed.Count == 0;

    public RequiredMods Combine(RequiredMods? other)
    {
        if (other is null) return this;
        return new RequiredMods(
            allowed: Allowed.Concat(other.Allowed),
            disallowed: Disallowed.Concat(other.Disallowed));
    }

    public bool Equals(RequiredMods? other)
    {
        if (other is null) return false;
        return SequenceEquals(Allowed, other.Allowed) && SequenceEquals(Disallowed, other.Disallowed);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var group in Allowed)
            foreach (var r in group) hash.Add(r);
        foreach (var group in Disallowed)
            foreach (var r in group) hash.Add(r);
        return hash.ToHashCode();
    }

    private static bool SequenceEquals(
        IReadOnlyList<IReadOnlyList<ModReference>> a,
        IReadOnlyList<IReadOnlyList<ModReference>> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (!a[i].SequenceEqual(b[i])) return false;
        return true;
    }
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Domain/Mods/RequiredMods.cs \
        tests/RimworldExtractor.Domain.Tests/Mods/RequiredModsTests.cs
git commit -m "feat(domain): add RequiredMods (AND-of-ORs over allowed/disallowed mod refs)"
```

### Task 10: TranslationEntry record

**Files:**
- Create: `src/RimworldExtractor.Domain/Entities/TranslationEntry.cs`
- Test: `tests/RimworldExtractor.Domain.Tests/Entities/TranslationEntryTests.cs`

Mirrors legacy structure minus the mutable extension bag.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Mods;

namespace RimworldExtractor.Domain.Tests.Entities;

public class TranslationEntryTests
{
    [Fact]
    public void Ctor_StoresAllFields()
    {
        var entry = new TranslationEntry(
            ClassName: "ThingDef",
            Node: "SampleMod_Weapon.label",
            Original: "sword",
            Translated: "검",
            RequiredMods: RequiredMods.Empty,
            SourceFile: "Defs/Weapons.xml");

        entry.ClassName.Should().Be("ThingDef");
        entry.Node.Should().Be("SampleMod_Weapon.label");
        entry.Original.Should().Be("sword");
        entry.Translated.Should().Be("검");
        entry.RequiredMods.Should().BeSameAs(RequiredMods.Empty);
        entry.SourceFile.Should().Be("Defs/Weapons.xml");
    }

    [Fact]
    public void ClassNode_JoinsClassAndNodeWithPlus()
    {
        var entry = new TranslationEntry("ThingDef", "Foo.label", "x", null, null, null);

        entry.ClassNode.Should().Be("ThingDef+Foo.label");
    }

    [Theory]
    [InlineData("Foo.label", "Foo", "label")]
    [InlineData("Foo.bar.baz", "Foo", "bar.baz")]
    [InlineData("SampleMod_WoodenSpear.description", "SampleMod_WoodenSpear", "description")]
    [InlineData("NoDots", "NoDots", "NoDots")]
    public void DefNameAndRealNode_SplitAtFirstDot(string node, string expectedDef, string expectedReal)
    {
        var entry = new TranslationEntry("ThingDef", node, "x", null, null, null);

        entry.DefName.Should().Be(expectedDef);
        entry.RealNode.Should().Be(expectedReal);
    }

    [Fact]
    public void Equality_IsRecordValueBased()
    {
        var a = new TranslationEntry("ThingDef", "Foo.label", "x", null, null, null);
        var b = new TranslationEntry("ThingDef", "Foo.label", "x", null, null, null);

        a.Should().Be(b);
    }

    [Fact]
    public void With_RecordDeconstructionSupported()
    {
        var a = new TranslationEntry("ThingDef", "Foo.label", "x", null, null, null);
        var b = a with { Translated = "y" };

        b.Translated.Should().Be("y");
        b.Original.Should().Be("x");
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

```csharp
using RimworldExtractor.Domain.Mods;

namespace RimworldExtractor.Domain.Entities;

/// <summary>
/// A single translatable value extracted from a RimWorld mod.
/// </summary>
/// <param name="ClassName">Def kind (e.g. <c>ThingDef</c>, <c>Keyed</c>, <c>Strings</c>, <c>Patches.ThingDef</c>).</param>
/// <param name="Node">Position within the Def (e.g. <c>DefName.label</c>, <c>Names.Last.0</c>, or a keyed identifier).</param>
/// <param name="Original">Source-language text.</param>
/// <param name="Translated">Target-language text, or null when untranslated.</param>
/// <param name="RequiredMods">Mods required/excluded for this entry (Phase 3 writes these into Patches XML).</param>
/// <param name="SourceFile">Relative path to the source XML/text file within the mod, or null for synthetic entries.</param>
public sealed record TranslationEntry(
    string ClassName,
    string Node,
    string Original,
    string? Translated,
    RequiredMods? RequiredMods,
    string? SourceFile)
{
    /// <summary>Compound key used in duplicate detection: <c>ClassName+Node</c>.</summary>
    public string ClassNode => $"{ClassName}+{Node}";

    /// <summary>The DefName portion of <see cref="Node"/> (everything before the first dot), or the whole Node if no dot.</summary>
    public string DefName => Node.Contains('.') ? Node[..Node.IndexOf('.')] : Node;

    /// <summary>The path-within-def portion of <see cref="Node"/> (everything after the first dot), or the whole Node if no dot.</summary>
    public string RealNode => Node.Contains('.') ? Node[(Node.IndexOf('.') + 1)..] : Node;
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Domain/Entities/TranslationEntry.cs \
        tests/RimworldExtractor.Domain.Tests/Entities/TranslationEntryTests.cs
git commit -m "feat(domain): add TranslationEntry record (ClassNode/DefName/RealNode helpers)"
```

### Task 11: ModMetadata record

**Files:**
- Create: `src/RimworldExtractor.Domain/Entities/ModMetadata.cs`
- Test: `tests/RimworldExtractor.Domain.Tests/Entities/ModMetadataTests.cs`

**Legacy reference:** `legacy/.../ModMetadata.cs`. We drop the custom `Equals`/`GetHashCode` (records auto-generate) and preserve the `Identifier` computed property.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Domain.Tests.Entities;

public class ModMetadataTests
{
    [Fact]
    public void Ctor_StoresAllFields()
    {
        var meta = new ModMetadata(
            RootDir: "/Mods/Foo",
            Id: "2997308585",
            ModName: "Foo Mod",
            PackageId: "author.foo",
            IsOfficialContent: false,
            ModDependencies: new List<string> { "Ludeon.RimWorld" });

        meta.RootDir.Should().Be("/Mods/Foo");
        meta.Id.Should().Be("2997308585");
        meta.ModName.Should().Be("Foo Mod");
        meta.PackageId.Should().Be("author.foo");
        meta.IsOfficialContent.Should().BeFalse();
        meta.ModDependencies.Should().ContainSingle().Which.Should().Be("Ludeon.RimWorld");
    }

    [Fact]
    public void Identifier_Official_IsJustModName()
    {
        var meta = new ModMetadata("/Data/Core", "Core", "Core", "Ludeon.RimWorld", IsOfficialContent: true);

        meta.Identifier.Should().Be("Core");
    }

    [Fact]
    public void Identifier_Unofficial_WithKnownId_IsModNameDashId()
    {
        var meta = new ModMetadata("/Mods/Foo", "2997308585", "Foo Mod", "author.foo", IsOfficialContent: false);

        meta.Identifier.Should().Be("Foo Mod - 2997308585");
    }

    [Fact]
    public void Identifier_Unofficial_WithPlaceholderId_IsJustModName()
    {
        var meta = new ModMetadata("/Mods/Foo", "???", "Foo Mod", "author.foo", IsOfficialContent: false);

        meta.Identifier.Should().Be("Foo Mod");
    }

    [Fact]
    public void Empty_IsDefaultSingleton()
    {
        var a = ModMetadata.Empty;
        var b = ModMetadata.Empty;

        a.Should().BeSameAs(b);
        a.RootDir.Should().BeEmpty();
    }

    [Fact]
    public void Equality_IsStructural()
    {
        var a = new ModMetadata("/a", "1", "m", "a.m", true);
        var b = new ModMetadata("/a", "1", "m", "a.m", true);

        a.Should().Be(b);
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

```csharp
namespace RimworldExtractor.Domain.Entities;

/// <summary>
/// Metadata about a RimWorld mod discovered on disk. <see cref="Id"/> is the workshop ID
/// for Steam mods, the folder name for local mods, or <c>"???"</c> when unknown.
/// </summary>
public sealed record ModMetadata(
    string RootDir,
    string Id,
    string ModName,
    string PackageId,
    bool IsOfficialContent,
    IReadOnlyList<string>? ModDependencies = null)
{
    public const string UnknownId = "???";

    /// <summary>Human-readable identifier: "ModName" for official content, "ModName - Id" otherwise (falling back to "ModName" when Id is unknown).</summary>
    public string Identifier
    {
        get
        {
            if (IsOfficialContent) return ModName;
            return Id == UnknownId ? ModName : $"{ModName} - {Id}";
        }
    }

    public static ModMetadata Empty { get; } = new("", "", "", "", IsOfficialContent: false);
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Domain/Entities/ModMetadata.cs \
        tests/RimworldExtractor.Domain.Tests/Entities/ModMetadataTests.cs
git commit -m "feat(domain): add ModMetadata record (Identifier: Official|Name-Id|Name)"
```

### Task 12: ExtractableFolder record

**Files:**
- Create: `src/RimworldExtractor.Domain/Entities/ExtractableFolder.cs`
- Test: `tests/RimworldExtractor.Domain.Tests/Entities/ExtractableFolderTests.cs`

**Legacy reference:** `legacy/.../ExtractableFolder.cs` record `(Root, FolderName, RequiredPackageId, VersionInfo="default")`.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Domain.Tests.Entities;

public class ExtractableFolderTests
{
    [Fact]
    public void Ctor_WithDefaults_SetsDefaultVersion()
    {
        var mod = new ModMetadata("/Mods/Foo", "1", "Foo", "author.foo", false);
        var folder = new ExtractableFolder(mod, "Defs", RequiredPackageId: null);

        folder.Root.Should().Be(mod);
        folder.FolderName.Should().Be("Defs");
        folder.RequiredPackageId.Should().BeNull();
        folder.VersionInfo.Should().Be("default");
    }

    [Fact]
    public void FullPath_CombinesRootDirAndFolderName()
    {
        var mod = new ModMetadata("/Mods/Foo", "1", "Foo", "author.foo", false);
        var folder = new ExtractableFolder(mod, "1.6/Defs", null);

        folder.FullPath.Should().Be(Path.Combine("/Mods/Foo", "1.6/Defs"));
    }

    [Fact]
    public void VersionInfo_CanBeExplicit()
    {
        var mod = new ModMetadata("/Mods/Foo", "1", "Foo", "author.foo", false);
        var folder = new ExtractableFolder(mod, "1.6/Defs", null, VersionInfo: "1.6");

        folder.VersionInfo.Should().Be("1.6");
    }

    [Fact]
    public void Equality_IsStructural()
    {
        var mod = new ModMetadata("/Mods/Foo", "1", "Foo", "author.foo", false);
        var a = new ExtractableFolder(mod, "Defs", null);
        var b = new ExtractableFolder(mod, "Defs", null);

        a.Should().Be(b);
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

```csharp
namespace RimworldExtractor.Domain.Entities;

/// <summary>
/// A folder within a mod that contains translation sources (Defs/Keyed/Strings/Patches).
/// <see cref="FolderName"/> is relative to the mod root and may include a version prefix (e.g. <c>1.6/Defs</c>).
/// </summary>
public sealed record ExtractableFolder(
    ModMetadata Root,
    string FolderName,
    string? RequiredPackageId,
    string VersionInfo = "default")
{
    public string FullPath => Path.Combine(Root.RootDir, FolderName);
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Domain/Entities/ExtractableFolder.cs \
        tests/RimworldExtractor.Domain.Tests/Entities/ExtractableFolderTests.cs
git commit -m "feat(domain): add ExtractableFolder record (Root+FolderName+VersionInfo)"
```

---

## Group 2D — Extraction Rules

### Task 13: ExtractionRule record

**Files:**
- Create: `src/RimworldExtractor.Domain/Rules/ExtractionRule.cs`
- Test: `tests/RimworldExtractor.Domain.Tests/Rules/ExtractionRuleTests.cs`

**Legacy reference:** `Prefabs.cs:61-132` inner class `ExtractionRule` has `Tag`, `Whitelist: HashSet<string>`, `Blacklist: HashSet<string>`, plus `CanExtract(defName)`. We keep the semantics but use `IReadOnlySet<string>`.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.Rules;

namespace RimworldExtractor.Domain.Tests.Rules;

public class ExtractionRuleTests
{
    [Fact]
    public void TagOnly_CanExtract_AnyDefName()
    {
        var rule = new ExtractionRule("label");

        rule.CanExtract("Anything").Should().BeTrue();
    }

    [Fact]
    public void WithWhitelist_CanExtract_OnlyListedDefs()
    {
        var rule = new ExtractionRule("label", whitelist: new[] { "ThingDef" });

        rule.CanExtract("ThingDef").Should().BeTrue();
        rule.CanExtract("PawnKindDef").Should().BeFalse();
    }

    [Fact]
    public void WithBlacklist_CanExtract_AllExceptListed()
    {
        var rule = new ExtractionRule("label", blacklist: new[] { "JunkDef" });

        rule.CanExtract("ThingDef").Should().BeTrue();
        rule.CanExtract("JunkDef").Should().BeFalse();
    }

    [Fact]
    public void WithWhitelistAndBlacklist_BothConstraintsApply()
    {
        var rule = new ExtractionRule(
            "label",
            whitelist: new[] { "ThingDef", "PawnKindDef" },
            blacklist: new[] { "ThingDef" });

        rule.CanExtract("ThingDef").Should().BeFalse("blacklist wins");
        rule.CanExtract("PawnKindDef").Should().BeTrue();
        rule.CanExtract("BuildingDef").Should().BeFalse("not in whitelist");
    }

    [Fact]
    public void Equality_IsStructural()
    {
        var a = new ExtractionRule("label", whitelist: new[] { "ThingDef" });
        var b = new ExtractionRule("label", whitelist: new[] { "ThingDef" });

        a.Should().Be(b);
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

```csharp
namespace RimworldExtractor.Domain.Rules;

/// <summary>
/// A rule deciding whether a given XML tag under a given DefName should be treated as translatable.
/// Whitelist restricts to specific DefNames; Blacklist excludes specific DefNames (Blacklist beats Whitelist).
/// </summary>
public sealed record ExtractionRule
{
    public string Tag { get; }
    public IReadOnlySet<string> Whitelist { get; }
    public IReadOnlySet<string> Blacklist { get; }

    public ExtractionRule(
        string tag,
        IEnumerable<string>? whitelist = null,
        IEnumerable<string>? blacklist = null)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag must be non-empty.", nameof(tag));
        Tag = tag;
        Whitelist = whitelist?.ToHashSet() ?? (IReadOnlySet<string>)new HashSet<string>();
        Blacklist = blacklist?.ToHashSet() ?? (IReadOnlySet<string>)new HashSet<string>();
    }

    public bool CanExtract(string defName)
    {
        if (Whitelist.Count > 0 && !Whitelist.Contains(defName)) return false;
        if (Blacklist.Contains(defName)) return false;
        return true;
    }

    public bool Equals(ExtractionRule? other)
    {
        if (other is null) return false;
        return Tag == other.Tag
            && Whitelist.SetEquals(other.Whitelist)
            && Blacklist.SetEquals(other.Blacklist);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Tag);
        foreach (var w in Whitelist.OrderBy(x => x, StringComparer.Ordinal)) hash.Add(w);
        foreach (var b in Blacklist.OrderBy(x => x, StringComparer.Ordinal)) hash.Add(b);
        return hash.ToHashCode();
    }
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Domain/Rules/ExtractionRule.cs \
        tests/RimworldExtractor.Domain.Tests/Rules/ExtractionRuleTests.cs
git commit -m "feat(domain): add ExtractionRule (Tag + Whitelist + Blacklist; Blacklist wins)"
```

### Task 14: NodeReplacementRule record

**Files:**
- Create: `src/RimworldExtractor.Domain/Rules/NodeReplacementRule.cs`
- Test: `tests/RimworldExtractor.Domain.Tests/Rules/NodeReplacementRuleTests.cs`

**Legacy reference:** `Prefabs.cs:137` — `NodeReplacement: Dictionary<string, string>`. Each entry maps a "class+tagpattern" string like `CombatExtended.AmmoDef+*` to its replacement like `ThingDef+*`. In the new domain we treat each entry as a typed record.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.Rules;

namespace RimworldExtractor.Domain.Tests.Rules;

public class NodeReplacementRuleTests
{
    [Fact]
    public void Ctor_StoresFromAndTo()
    {
        var rule = new NodeReplacementRule("CombatExtended.AmmoDef+*", "ThingDef+*");

        rule.From.Should().Be("CombatExtended.AmmoDef+*");
        rule.To.Should().Be("ThingDef+*");
    }

    [Theory]
    [InlineData("", "ThingDef+*")]
    [InlineData("CombatExtended.AmmoDef+*", "")]
    public void Ctor_WithEmptyPart_Throws(string from, string to)
    {
        var act = () => new NodeReplacementRule(from, to);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_IsStructural()
    {
        var a = new NodeReplacementRule("a", "b");
        var b = new NodeReplacementRule("a", "b");

        a.Should().Be(b);
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

```csharp
namespace RimworldExtractor.Domain.Rules;

/// <summary>
/// Maps a source Def-class pattern (e.g. <c>CombatExtended.AmmoDef+*</c>) to a replacement
/// pattern (e.g. <c>ThingDef+*</c>) so extraction treats mod classes as base-class entries.
/// </summary>
public sealed record NodeReplacementRule
{
    public string From { get; }
    public string To { get; }

    public NodeReplacementRule(string from, string to)
    {
        if (string.IsNullOrEmpty(from))
            throw new ArgumentException("From pattern must be non-empty.", nameof(from));
        if (string.IsNullOrEmpty(to))
            throw new ArgumentException("To pattern must be non-empty.", nameof(to));
        From = from;
        To = to;
    }
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Domain/Rules/NodeReplacementRule.cs \
        tests/RimworldExtractor.Domain.Tests/Rules/NodeReplacementRuleTests.cs
git commit -m "feat(domain): add NodeReplacementRule (From/To def-class pattern map)"
```

### Task 15: TranslationHandle record

**Files:**
- Create: `src/RimworldExtractor.Domain/Rules/TranslationHandle.cs`
- Test: `tests/RimworldExtractor.Domain.Tests/Rules/TranslationHandleTests.cs`

**Legacy reference:** `Prefabs.cs:142` — `TranslationHandles: List<string>`. Each entry is a tag like `*verbClass` or `*compClass` whose content identifies a translatable sub-object. Legacy stored as raw strings; we wrap with a typed record that also exposes whether the prefix `*` means "wildcard class name".

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.Rules;

namespace RimworldExtractor.Domain.Tests.Rules;

public class TranslationHandleTests
{
    [Theory]
    [InlineData("*verbClass", "verbClass", true)]
    [InlineData("*compClass", "compClass", true)]
    [InlineData("label", "label", false)]
    public void Parse_ExtractsTagAndWildcardFlag(string raw, string expectedTag, bool expectedWildcard)
    {
        var handle = TranslationHandle.Parse(raw);

        handle.Tag.Should().Be(expectedTag);
        handle.IsWildcardClass.Should().Be(expectedWildcard);
        handle.ToString().Should().Be(raw);
    }

    [Theory]
    [InlineData("")]
    [InlineData("*")]
    public void Parse_WithEmptyOrJustStar_Throws(string raw)
    {
        var act = () => TranslationHandle.Parse(raw);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_IsStructural()
    {
        var a = TranslationHandle.Parse("*verbClass");
        var b = TranslationHandle.Parse("*verbClass");

        a.Should().Be(b);
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

```csharp
namespace RimworldExtractor.Domain.Rules;

/// <summary>
/// A translation-handle rule from legacy <c>Prefabs.TranslationHandles</c>. A handle tells the
/// extractor to dive into a sub-node referenced by a class attribute. A leading <c>*</c>
/// in the raw form means "any class name" (wildcard).
/// </summary>
public sealed record TranslationHandle
{
    public string Tag { get; }
    public bool IsWildcardClass { get; }

    private TranslationHandle(string tag, bool isWildcardClass)
    {
        Tag = tag;
        IsWildcardClass = isWildcardClass;
    }

    public static TranslationHandle Parse(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            throw new ArgumentException("TranslationHandle must be non-empty.", nameof(raw));
        var wildcard = raw.StartsWith('*');
        var tag = wildcard ? raw[1..] : raw;
        if (tag.Length == 0)
            throw new ArgumentException("TranslationHandle tag must be non-empty.", nameof(raw));
        return new TranslationHandle(tag, wildcard);
    }

    public override string ToString() => IsWildcardClass ? "*" + Tag : Tag;
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Domain/Rules/TranslationHandle.cs \
        tests/RimworldExtractor.Domain.Tests/Rules/TranslationHandleTests.cs
git commit -m "feat(domain): add TranslationHandle record (parse wildcard-* prefix)"
```

---

## Group 2E — Settings Schema + JSON Source Generator

### Task 16: AppSettings record tree + schema version constant

**Files:**
- Create: `src/RimworldExtractor.Domain/Settings/AppSettings.cs`
- Create: `src/RimworldExtractor.Domain/Settings/PathSettings.cs`
- Create: `src/RimworldExtractor.Domain/Settings/LanguageSettings.cs`
- Create: `src/RimworldExtractor.Domain/Settings/ExtractionSettings.cs`
- Create: `src/RimworldExtractor.Domain/Settings/OutputSettings.cs`
- Test: `tests/RimworldExtractor.Domain.Tests/Settings/AppSettingsTests.cs`

`AppSettings` is a record tree. It lives in Domain (not Infrastructure) because the schema is part of our domain model — Infrastructure only adds JSON serialization.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Domain.Rules;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Domain.Tests.Settings;

public class AppSettingsTests
{
    [Fact]
    public void CurrentSchemaVersion_IsTwo()
    {
        AppSettings.CurrentSchemaVersion.Should().Be(2);
    }

    [Fact]
    public void Default_HasSensibleValues()
    {
        var s = AppSettings.Default;

        s.SchemaVersion.Should().Be(2);
        s.Paths.Should().NotBeNull();
        s.Paths.Rimworld.Should().BeEmpty();
        s.Paths.Workshop.Should().BeEmpty();
        s.Paths.BaseRefList.Should().BeEmpty();

        s.Languages.Original.Display.Should().Be("English");
        s.Languages.Translation.Display.Should().Be("Korean (한국어)");

        s.Extraction.CurrentVersion.ToString().Should().Be("1.6");
        s.Extraction.CommentOriginal.Should().BeFalse();
        s.Extraction.EnableTkey.Should().BeFalse();
        s.Extraction.Rules.Should().BeEmpty();
        s.Extraction.FullListTags.Should().BeEmpty();
        s.Extraction.NodeReplacements.Should().BeEmpty();
        s.Extraction.TranslationHandles.Should().BeEmpty();

        s.Output.Policy.Should().Be(DuplicatesPolicy.Overwrite);
        s.Output.Format.Should().Be(ExtractionFormat.Languages);
    }

    [Fact]
    public void With_IsImmutableClone()
    {
        var original = AppSettings.Default;
        var modified = original with { Paths = original.Paths with { Rimworld = "/rw" } };

        modified.Paths.Rimworld.Should().Be("/rw");
        original.Paths.Rimworld.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

`src/RimworldExtractor.Domain/Settings/PathSettings.cs`:

```csharp
namespace RimworldExtractor.Domain.Settings;

public sealed record PathSettings(
    string Rimworld,
    string Workshop,
    string BaseRefList)
{
    public static PathSettings Default { get; } = new("", "", "");
}
```

`src/RimworldExtractor.Domain/Settings/LanguageSettings.cs`:

```csharp
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Domain.Settings;

public sealed record LanguageSettings(
    LanguageCode Original,
    LanguageCode Translation)
{
    public static LanguageSettings Default { get; } = new(
        LanguageCode.Create("English"),
        LanguageCode.Create("Korean (한국어)"));
}
```

`src/RimworldExtractor.Domain/Settings/ExtractionSettings.cs`:

```csharp
using RimworldExtractor.Domain.Rules;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Domain.Settings;

public sealed record ExtractionSettings(
    GameVersion CurrentVersion,
    bool CommentOriginal,
    bool EnableTkey,
    IReadOnlyList<ExtractionRule> Rules,
    IReadOnlyList<string> FullListTags,
    IReadOnlyList<NodeReplacementRule> NodeReplacements,
    IReadOnlyList<TranslationHandle> TranslationHandles)
{
    public static ExtractionSettings Default { get; } = new(
        CurrentVersion: GameVersion.Parse("1.6"),
        CommentOriginal: false,
        EnableTkey: false,
        Rules: Array.Empty<ExtractionRule>(),
        FullListTags: Array.Empty<string>(),
        NodeReplacements: Array.Empty<NodeReplacementRule>(),
        TranslationHandles: Array.Empty<TranslationHandle>());
}
```

`src/RimworldExtractor.Domain/Settings/OutputSettings.cs`:

```csharp
using RimworldExtractor.Domain.Enums;

namespace RimworldExtractor.Domain.Settings;

public sealed record OutputSettings(
    DuplicatesPolicy Policy,
    ExtractionFormat Format)
{
    public static OutputSettings Default { get; } = new(
        Policy: DuplicatesPolicy.Overwrite,
        Format: ExtractionFormat.Languages);
}
```

`src/RimworldExtractor.Domain/Settings/AppSettings.cs`:

```csharp
namespace RimworldExtractor.Domain.Settings;

/// <summary>
/// Complete user-facing settings tree. Persisted as <c>settings.json</c> in Phase 2+.
/// Schema version is bumped whenever a breaking change occurs; <see cref="CurrentSchemaVersion"/>
/// is the target version for new files.
/// </summary>
public sealed record AppSettings(
    int SchemaVersion,
    PathSettings Paths,
    LanguageSettings Languages,
    ExtractionSettings Extraction,
    OutputSettings Output)
{
    public const int CurrentSchemaVersion = 2;

    public static AppSettings Default { get; } = new(
        SchemaVersion: CurrentSchemaVersion,
        Paths: PathSettings.Default,
        Languages: LanguageSettings.Default,
        Extraction: ExtractionSettings.Default,
        Output: OutputSettings.Default);
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Domain/Settings/ \
        tests/RimworldExtractor.Domain.Tests/Settings/AppSettingsTests.cs
git commit -m "feat(domain): add AppSettings record tree with schema version 2"
```

### Task 17: JSON source generator for AppSettings

**Files:**
- Create: `src/RimworldExtractor.Infrastructure/Settings/AppSettingsJsonContext.cs`
- Test: `tests/RimworldExtractor.Infrastructure.Tests/Settings/AppSettingsJsonContextTests.cs`

STJ source generator enables AOT-compatible serialization. The context class declares every type reachable from `AppSettings`.

**Prerequisite:** the Infrastructure project already has `System.Text.Json` via the Microsoft.Extensions.* packages (transitive); if not, add it. Check:
```bash
dotnet list src/RimworldExtractor.Infrastructure package
```

If `System.Text.Json` is not listed, add a pin in `Directory.Packages.props` and a `PackageReference` in Infrastructure. For .NET 10, `System.Text.Json 10.0.0` comes in-box with the runtime, so no explicit package reference is needed.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Text.Json;
using FluentAssertions;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure.Settings;

namespace RimworldExtractor.Infrastructure.Tests.Settings;

public class AppSettingsJsonContextTests
{
    [Fact]
    public void Serialize_Default_ProducesJson()
    {
        var json = JsonSerializer.Serialize(AppSettings.Default, AppSettingsJsonContext.Default.AppSettings);

        json.Should().Contain("\"schemaVersion\": 2");
        json.Should().Contain("\"original\":");
    }

    [Fact]
    public void RoundTrip_DefaultSettings_PreservesValue()
    {
        var original = AppSettings.Default;
        var json = JsonSerializer.Serialize(original, AppSettingsJsonContext.Default.AppSettings);

        var deserialized = JsonSerializer.Deserialize(json, AppSettingsJsonContext.Default.AppSettings);

        deserialized.Should().NotBeNull();
        deserialized.Should().Be(original);
    }

    [Fact]
    public void Serialize_IsIndented()
    {
        var json = JsonSerializer.Serialize(AppSettings.Default, AppSettingsJsonContext.Default.AppSettings);

        json.Should().Contain("\n", "indented JSON has newlines");
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Domain.Rules;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Infrastructure.Settings;

/// <summary>
/// System.Text.Json source-generated context for the AppSettings tree.
/// Emits AOT-safe serialization that does not require runtime reflection.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(PathSettings))]
[JsonSerializable(typeof(LanguageSettings))]
[JsonSerializable(typeof(ExtractionSettings))]
[JsonSerializable(typeof(OutputSettings))]
[JsonSerializable(typeof(ExtractionRule))]
[JsonSerializable(typeof(NodeReplacementRule))]
[JsonSerializable(typeof(TranslationHandle))]
[JsonSerializable(typeof(GameVersion))]
[JsonSerializable(typeof(LanguageCode))]
[JsonSerializable(typeof(DuplicatesPolicy))]
[JsonSerializable(typeof(ExtractionFormat))]
public sealed partial class AppSettingsJsonContext : JsonSerializerContext
{
}
```

**Likely compilation issue:** `GameVersion` and `LanguageCode` are `readonly record struct` — STJ can't auto-serialize them without converters. Two options:
- (a) add a `[JsonConverter(typeof(...))]` to each struct
- (b) add a property-based shape that uses constructor round-trip

For simplicity and determinism, write explicit `JsonConverter`s. Do this now rather than discovering it during testing.

**Additional files to create in Step 3:**

`src/RimworldExtractor.Infrastructure/Settings/GameVersionJsonConverter.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Infrastructure.Settings;

public sealed class GameVersionJsonConverter : JsonConverter<GameVersion>
{
    public override GameVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString() ?? throw new JsonException("Expected GameVersion string.");
        return GameVersion.Parse(raw);
    }

    public override void Write(Utf8JsonWriter writer, GameVersion value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
```

`src/RimworldExtractor.Infrastructure/Settings/LanguageCodeJsonConverter.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Infrastructure.Settings;

public sealed class LanguageCodeJsonConverter : JsonConverter<LanguageCode>
{
    public override LanguageCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString() ?? throw new JsonException("Expected LanguageCode string.");
        return LanguageCode.Create(raw);
    }

    public override void Write(Utf8JsonWriter writer, LanguageCode value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Display);
}
```

Register the converters on the context by adding to the attribute block:

```csharp
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    Converters = new[] { typeof(GameVersionJsonConverter), typeof(LanguageCodeJsonConverter) })]
```

**Wait — `JsonSourceGenerationOptions` does NOT accept a `Converters` array parameter.** Converters on a source-gen context are registered via attributes on the individual `[JsonConverter(typeof(...))]` on each struct type. Adjust Task 6 & Task 7 retroactively by adding the attribute to `GameVersion` and `LanguageCode`:

- Add `[JsonConverter(typeof(RimworldExtractor.Infrastructure.Settings.GameVersionJsonConverter))]` to `GameVersion` — but this creates a Domain → Infrastructure dependency, violating the Clean Architecture rule "Domain has no external deps".

**Resolution:** put the converter types in `Domain/Settings/Json/` and reference them via the attribute. The converters themselves are a "Domain-shape-to-JSON-text" concern and are reasonably part of Domain. Alternatively, define the converter as a type in Infrastructure but register it via `JsonSerializerOptions.Converters` at call site (not attribute). The cleanest: put converters in Domain.

Final decision: **converters live in `Domain/Settings/Json/`** and are registered via `[JsonConverter(...)]` on the struct type itself. This keeps the Domain aware of its own JSON shape but stays pure (no file I/O, no logging dependencies — just shape).

**Updated Step 3 files:**

1. Move `GameVersionJsonConverter.cs` to `src/RimworldExtractor.Domain/Settings/Json/GameVersionJsonConverter.cs` (rename namespace to `RimworldExtractor.Domain.Settings.Json`).
2. Move `LanguageCodeJsonConverter.cs` to `src/RimworldExtractor.Domain/Settings/Json/LanguageCodeJsonConverter.cs`.
3. Add `[JsonConverter(typeof(GameVersionJsonConverter))]` to `GameVersion` struct. This needs a retroactive edit to `src/RimworldExtractor.Domain/ValueObjects/GameVersion.cs` — include the `[JsonConverter]` attribute when rewriting.
4. Same for `LanguageCode`.
5. `AppSettingsJsonContext` stays in Infrastructure.

**Retroactive edit at this task:** open `src/RimworldExtractor.Domain/ValueObjects/GameVersion.cs` and `LanguageCode.cs` to add the `JsonConverter` attribute. Commit that edit along with the new converter files and AppSettingsJsonContext as a single task commit.

Concrete Step 3 actions:

1. Create `src/RimworldExtractor.Domain/Settings/Json/GameVersionJsonConverter.cs` (with `RimworldExtractor.Domain.Settings.Json` namespace):

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Domain.Settings.Json;

public sealed class GameVersionJsonConverter : JsonConverter<GameVersion>
{
    public override GameVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString() ?? throw new JsonException("Expected GameVersion string.");
        return GameVersion.Parse(raw);
    }

    public override void Write(Utf8JsonWriter writer, GameVersion value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
```

2. Create `src/RimworldExtractor.Domain/Settings/Json/LanguageCodeJsonConverter.cs` (likewise in `RimworldExtractor.Domain.Settings.Json`):

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Domain.Settings.Json;

public sealed class LanguageCodeJsonConverter : JsonConverter<LanguageCode>
{
    public override LanguageCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString() ?? throw new JsonException("Expected LanguageCode string.");
        return LanguageCode.Create(raw);
    }

    public override void Write(Utf8JsonWriter writer, LanguageCode value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Display);
}
```

3. Edit `src/RimworldExtractor.Domain/ValueObjects/GameVersion.cs`. Add `using System.Text.Json.Serialization;` and `using RimworldExtractor.Domain.Settings.Json;` at top; add attribute:

```csharp
[JsonConverter(typeof(GameVersionJsonConverter))]
public readonly record struct GameVersion : IComparable<GameVersion>
```

4. Edit `src/RimworldExtractor.Domain/ValueObjects/LanguageCode.cs` similarly:

```csharp
[JsonConverter(typeof(LanguageCodeJsonConverter))]
public readonly record struct LanguageCode
```

5. Finally, create `src/RimworldExtractor.Infrastructure/Settings/AppSettingsJsonContext.cs`:

```csharp
using System.Text.Json.Serialization;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Domain.Rules;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Infrastructure.Settings;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(PathSettings))]
[JsonSerializable(typeof(LanguageSettings))]
[JsonSerializable(typeof(ExtractionSettings))]
[JsonSerializable(typeof(OutputSettings))]
[JsonSerializable(typeof(ExtractionRule))]
[JsonSerializable(typeof(NodeReplacementRule))]
[JsonSerializable(typeof(TranslationHandle))]
[JsonSerializable(typeof(GameVersion))]
[JsonSerializable(typeof(LanguageCode))]
[JsonSerializable(typeof(DuplicatesPolicy))]
[JsonSerializable(typeof(ExtractionFormat))]
public sealed partial class AppSettingsJsonContext : JsonSerializerContext
{
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Domain/Settings/Json/ \
        src/RimworldExtractor.Domain/ValueObjects/GameVersion.cs \
        src/RimworldExtractor.Domain/ValueObjects/LanguageCode.cs \
        src/RimworldExtractor.Infrastructure/Settings/AppSettingsJsonContext.cs \
        tests/RimworldExtractor.Infrastructure.Tests/Settings/AppSettingsJsonContextTests.cs
git commit -m "feat(infrastructure): add AppSettings JSON source-generator context"
```

### Task 18: ISettingsStore abstraction in Domain

**Files:**
- Create: `src/RimworldExtractor.Domain/Abstractions/ISettingsStore.cs`
- Test: (deferred — no stand-alone test; `JsonSettingsStoreTests` in Task 19 covers the contract)

- [ ] **Step 1: Write the interface**

```csharp
using RimworldExtractor.Domain.Settings;

namespace RimworldExtractor.Domain.Abstractions;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> to a durable location. Implementations
/// must provide atomic writes (i.e. partial failure never produces a corrupt file).
/// </summary>
public interface ISettingsStore
{
    /// <summary>Returns persisted settings, or <see cref="AppSettings.Default"/> if no store exists yet.</summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Writes the given settings atomically. Existing file is replaced or backed up.</summary>
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Build + confirm Domain still compiles**

```bash
dotnet build src/RimworldExtractor.Domain -c Release 2>&1 | tail -3
```
Expected: 0 warnings, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/RimworldExtractor.Domain/Abstractions/ISettingsStore.cs
git commit -m "feat(domain): add ISettingsStore abstraction (LoadAsync/SaveAsync)"
```

---

## Group 2F — JsonSettingsStore

### Task 19: JsonSettingsStore with atomic write

**Files:**
- Create: `src/RimworldExtractor.Infrastructure/Settings/JsonSettingsStore.cs`
- Test: `tests/RimworldExtractor.Infrastructure.Tests/Settings/JsonSettingsStoreTests.cs`

**Atomic write strategy:** serialize → write to `settings.json.tmp` → `File.Replace(tmp, settings.json, backup: settings.json.bak)`. `File.Replace` is atomic on Windows NTFS and POSIX (same filesystem).

- [ ] **Step 1: Write the failing test**

```csharp
using System.Text.Json;
using FluentAssertions;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure.Settings;

namespace RimworldExtractor.Infrastructure.Tests.Settings;

public class JsonSettingsStoreTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly string _settingsPath;

    public JsonSettingsStoreTests()
    {
        _tmpDir = Directory.CreateTempSubdirectory("rwx-settings-").FullName;
        _settingsPath = Path.Combine(_tmpDir, "settings.json");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tmpDir, recursive: true); } catch { /* swallow */ }
    }

    [Fact]
    public async Task LoadAsync_WhenFileAbsent_ReturnsDefault()
    {
        var store = new JsonSettingsStore(_settingsPath);

        var loaded = await store.LoadAsync();

        loaded.Should().Be(AppSettings.Default);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsEqual()
    {
        var store = new JsonSettingsStore(_settingsPath);
        var original = AppSettings.Default with
        {
            Paths = PathSettings.Default with { Rimworld = "/rw" }
        };

        await store.SaveAsync(original);
        var loaded = await store.LoadAsync();

        loaded.Should().Be(original);
    }

    [Fact]
    public async Task SaveAsync_CreatesBackupOfPrevious()
    {
        var store = new JsonSettingsStore(_settingsPath);
        var v1 = AppSettings.Default with { Paths = PathSettings.Default with { Rimworld = "/rw1" } };
        var v2 = AppSettings.Default with { Paths = PathSettings.Default with { Rimworld = "/rw2" } };

        await store.SaveAsync(v1);
        await store.SaveAsync(v2);

        var backupPath = _settingsPath + ".bak";
        File.Exists(backupPath).Should().BeTrue();
        var backupText = await File.ReadAllTextAsync(backupPath);
        backupText.Should().Contain("/rw1");
    }

    [Fact]
    public async Task SaveAsync_DoesNotLeaveTempFile()
    {
        var store = new JsonSettingsStore(_settingsPath);
        await store.SaveAsync(AppSettings.Default);

        File.Exists(_settingsPath + ".tmp").Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WithCorruptFile_Throws()
    {
        await File.WriteAllTextAsync(_settingsPath, "{{{ not valid json");
        var store = new JsonSettingsStore(_settingsPath);

        Func<Task> act = () => store.LoadAsync();

        await act.Should().ThrowAsync<JsonException>();
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

```csharp
using System.Text.Json;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Settings;

namespace RimworldExtractor.Infrastructure.Settings;

/// <summary>
/// Persists <see cref="AppSettings"/> as JSON with atomic writes.
/// Uses a <c>.tmp</c> sibling file and <see cref="File.Replace(string, string, string)"/>
/// so crashes never produce a partially-written file.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string _path;
    private readonly string _tempPath;
    private readonly string _backupPath;

    public JsonSettingsStore(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
        _tempPath = _path + ".tmp";
        _backupPath = _path + ".bak";
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path)) return AppSettings.Default;

        await using var stream = File.OpenRead(_path);
        var loaded = await JsonSerializer.DeserializeAsync(
            stream,
            AppSettingsJsonContext.Default.AppSettings,
            cancellationToken);
        return loaded ?? AppSettings.Default;
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using (var stream = File.Create(_tempPath))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                settings,
                AppSettingsJsonContext.Default.AppSettings,
                cancellationToken);
        }

        if (File.Exists(_path))
        {
            File.Replace(_tempPath, _path, _backupPath);
        }
        else
        {
            File.Move(_tempPath, _path);
        }
    }
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Infrastructure/Settings/JsonSettingsStore.cs \
        tests/RimworldExtractor.Infrastructure.Tests/Settings/JsonSettingsStoreTests.cs
git commit -m "feat(infrastructure): add JsonSettingsStore with atomic write + backup"
```

### Task 20: Register JsonSettingsStore in Infrastructure DI

**Files:**
- Create: `src/RimworldExtractor.Infrastructure/DependencyInjection.cs`
- Test: `tests/RimworldExtractor.Infrastructure.Tests/DependencyInjectionTests.cs`

**Context:** Phase 1 added `AddApplication` in the Application project. Now add `AddInfrastructure(settingsPath)` to wire `ISettingsStore` → `JsonSettingsStore`.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Infrastructure;
using RimworldExtractor.Infrastructure.Settings;

namespace RimworldExtractor.Infrastructure.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ResolvesISettingsStore()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(settingsPath: "/tmp/settings.json");

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var store = provider.GetRequiredService<ISettingsStore>();

        store.Should().BeOfType<JsonSettingsStore>();
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

```csharp
using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Infrastructure.Settings;

namespace RimworldExtractor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string settingsPath)
    {
        services.AddSingleton<ISettingsStore>(_ => new JsonSettingsStore(settingsPath));
        return services;
    }
}
```

**Visibility note:** the test asserts `BeOfType<JsonSettingsStore>`. For this check to compile from the test project, `JsonSettingsStore` must be `public`. It already is per Task 19's listing. If you made it `internal`, change to `public` here.

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Infrastructure/DependencyInjection.cs \
        tests/RimworldExtractor.Infrastructure.Tests/DependencyInjectionTests.cs
git commit -m "feat(infrastructure): add AddInfrastructure DI extension with settings path parameter"
```

---

## Group 2G — Legacy Conversion

### Task 21: Legacy ExtractionRule DSL parser

**Files:**
- Create: `src/RimworldExtractor.Infrastructure/Legacy/LegacyExtractionRuleParser.cs`
- Test: `tests/RimworldExtractor.Infrastructure.Tests/Legacy/LegacyExtractionRuleParserTests.cs`

**Legacy reference:** `Prefabs.cs:67-104` parses strings of the form `"tag+whiteA,whiteB-blackA,blackB"`. The first `+` or `-` after the tag marks the boundary; alternating `+`/`-` segments list whitelist/blacklist items.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.Rules;
using RimworldExtractor.Infrastructure.Legacy;

namespace RimworldExtractor.Infrastructure.Tests.Legacy;

public class LegacyExtractionRuleParserTests
{
    [Theory]
    [InlineData("label")]
    [InlineData("description")]
    public void Parse_TagOnly_HasEmptyLists(string raw)
    {
        var rule = LegacyExtractionRuleParser.Parse(raw);

        rule.Tag.Should().Be(raw);
        rule.Whitelist.Should().BeEmpty();
        rule.Blacklist.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithWhitelist_ExtractsItems()
    {
        var rule = LegacyExtractionRuleParser.Parse("label+ThingDef,PawnKindDef");

        rule.Tag.Should().Be("label");
        rule.Whitelist.Should().BeEquivalentTo(new[] { "ThingDef", "PawnKindDef" });
        rule.Blacklist.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithBlacklist_ExtractsItems()
    {
        var rule = LegacyExtractionRuleParser.Parse("label-TrashDef,JunkDef");

        rule.Tag.Should().Be("label");
        rule.Whitelist.Should().BeEmpty();
        rule.Blacklist.Should().BeEquivalentTo(new[] { "TrashDef", "JunkDef" });
    }

    [Fact]
    public void Parse_WithBoth_ExtractsBoth()
    {
        var rule = LegacyExtractionRuleParser.Parse("label+ThingDef,PawnKindDef-TrashDef");

        rule.Whitelist.Should().BeEquivalentTo(new[] { "ThingDef", "PawnKindDef" });
        rule.Blacklist.Should().BeEquivalentTo(new[] { "TrashDef" });
    }

    [Fact]
    public void Parse_WithBlacklistThenWhitelist_ExtractsBoth()
    {
        var rule = LegacyExtractionRuleParser.Parse("label-TrashDef+ThingDef");

        rule.Whitelist.Should().BeEquivalentTo(new[] { "ThingDef" });
        rule.Blacklist.Should().BeEquivalentTo(new[] { "TrashDef" });
    }

    [Fact]
    public void Parse_EmptyString_Throws()
    {
        var act = () => LegacyExtractionRuleParser.Parse("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RoundTrip_Format_Then_Parse_IsIdempotent()
    {
        var rule = new ExtractionRule(
            "label",
            whitelist: new[] { "PawnKindDef", "ThingDef" },
            blacklist: new[] { "TrashDef" });

        var formatted = LegacyExtractionRuleParser.Format(rule);
        var roundTripped = LegacyExtractionRuleParser.Parse(formatted);

        roundTripped.Tag.Should().Be(rule.Tag);
        roundTripped.Whitelist.Should().BeEquivalentTo(rule.Whitelist);
        roundTripped.Blacklist.Should().BeEquivalentTo(rule.Blacklist);
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

```csharp
using RimworldExtractor.Domain.Rules;

namespace RimworldExtractor.Infrastructure.Legacy;

/// <summary>
/// Parses / formats the legacy Prefabs.dat DSL for extraction rules:
/// <c>tag+white,list-black,list</c>. First <c>+</c> or <c>-</c> marks the tag boundary;
/// alternating segments populate whitelist / blacklist.
/// </summary>
public static class LegacyExtractionRuleParser
{
    public static ExtractionRule Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Raw rule must be non-empty.", nameof(raw));

        var plusIndex = raw.IndexOf('+');
        var minusIndex = raw.IndexOf('-');

        if (plusIndex == -1 && minusIndex == -1)
        {
            return new ExtractionRule(raw);
        }

        var firstSep = (plusIndex != -1 && minusIndex != -1)
            ? Math.Min(plusIndex, minusIndex)
            : Math.Max(plusIndex, minusIndex);

        var tag = raw[..firstSep];
        var remain = raw[firstSep..];

        var whitelist = new HashSet<string>();
        var blacklist = new HashSet<string>();

        int i = 0;
        while (i < remain.Length)
        {
            char mode = remain[i];
            int nextPlus = remain.IndexOf('+', i + 1);
            int nextMinus = remain.IndexOf('-', i + 1);
            int nextSep = (nextPlus == -1 && nextMinus == -1) ? remain.Length
                : (nextPlus == -1) ? nextMinus
                : (nextMinus == -1) ? nextPlus
                : Math.Min(nextPlus, nextMinus);

            var content = remain[(i + 1)..nextSep];
            var items = content.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var target = (mode == '+') ? whitelist : blacklist;
            foreach (var item in items) target.Add(item.Trim());

            i = nextSep;
        }

        return new ExtractionRule(tag, whitelist, blacklist);
    }

    public static string Format(ExtractionRule rule)
    {
        var sb = new System.Text.StringBuilder(rule.Tag);
        if (rule.Whitelist.Count > 0)
        {
            sb.Append('+');
            sb.Append(string.Join(',', rule.Whitelist.OrderBy(x => x, StringComparer.Ordinal)));
        }
        if (rule.Blacklist.Count > 0)
        {
            sb.Append('-');
            sb.Append(string.Join(',', rule.Blacklist.OrderBy(x => x, StringComparer.Ordinal)));
        }
        return sb.ToString();
    }
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Infrastructure/Legacy/LegacyExtractionRuleParser.cs \
        tests/RimworldExtractor.Infrastructure.Tests/Legacy/LegacyExtractionRuleParserTests.cs
git commit -m "feat(infrastructure): add LegacyExtractionRuleParser (tag+/- DSL parse+format)"
```

### Task 22: LegacyPrefabsReader

**Files:**
- Create: `src/RimworldExtractor.Infrastructure/Legacy/LegacyPrefabsReader.cs`
- Test: `tests/RimworldExtractor.Infrastructure.Tests/Legacy/LegacyPrefabsReaderTests.cs`

**Legacy reference:** `Prefabs.Save` and `Prefabs.Load` in `legacy/.../Prefabs.cs:196-253`. The format is line-delimited, 18 lines total:

```
Line 0: "DO NOT EDIT THIS MANUALLY"
Line 1: Version (e.g. "9")
Line 2: EnableTkey (True/False)
Line 3: PathRimworld
Line 4: PathWorkshop
Line 5: PathBaseRefList
Line 6: CurrentVersion (e.g. "1.6")
Line 7: PatternVersion (regex, e.g. "^[1]\.\d+")
Line 8: PatternVersionWithV (regex, e.g. "^v[1]\.\d+")
Line 9: OriginalLanguage (e.g. "English")
Line 10: TranslationLanguage (e.g. "Korean (한국어)")
Line 11: CommentOriginal (True/False)
Line 12: ExtractableTags (slash-delimited DSL list)
Line 13: FullListTranslationTags (slash-delimited)
Line 14: NodeReplacement (slash-delimited key|value pairs)
Line 15: TranslationHandles (slash-delimited)
Line 16: Policy (DuplicatesPolicy enum name)
Line 17: Method (ExtractionMethod enum name)
```

`LegacyPrefabsReader.Read(path) → AppSettings`. Lines 7–8 (regex patterns) are intentionally DROPPED — the new `GameVersion` type owns that logic. Line 0 is a human marker and skipped. Line 1 is the schema version — we only support `"9"` (log a warning otherwise).

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Infrastructure.Legacy;

namespace RimworldExtractor.Infrastructure.Tests.Legacy;

public class LegacyPrefabsReaderTests : IDisposable
{
    private readonly string _tmpDir;

    public LegacyPrefabsReaderTests()
    {
        _tmpDir = Directory.CreateTempSubdirectory("rwx-legacy-").FullName;
    }

    public void Dispose()
    {
        try { Directory.Delete(_tmpDir, recursive: true); } catch { }
    }

    private string WritePrefabs(string[] lines)
    {
        var path = Path.Combine(_tmpDir, "Prefabs.dat");
        File.WriteAllLines(path, lines);
        return path;
    }

    private static string[] SampleLines() => new[]
    {
        "DO NOT EDIT THIS MANUALLY",
        "9",                                                    // version
        "False",                                                // enableTkey
        "/games/RimWorld",                                      // pathRimworld
        "/games/workshop/294100",                               // pathWorkshop
        "",                                                     // pathBaseRefList
        "1.6",                                                  // currentVersion
        @"^[1]\.\d+",                                           // patternVersion (dropped)
        @"^v[1]\.\d+",                                          // patternVersionWithV (dropped)
        "English",                                              // originalLanguage
        "Korean (한국어)",                                      // translationLanguage
        "False",                                                // commentOriginal
        "label/description/baseDesc+ThingDef-TrashDef",         // extractableTags
        "rulesFiles/rulesStrings",                              // fullListTags
        "CombatExtended.AmmoDef+*|ThingDef+*/Meow.X+*|ThingDef+*", // nodeReplacement
        "*verbClass/*compClass",                                // translationHandles
        "Overwrite",                                            // policy
        "Languages",                                            // method
    };

    [Fact]
    public void Read_ValidFile_ReturnsAppSettingsWithExpectedShape()
    {
        var path = WritePrefabs(SampleLines());

        var settings = LegacyPrefabsReader.Read(path);

        settings.SchemaVersion.Should().Be(Domain.Settings.AppSettings.CurrentSchemaVersion);
        settings.Paths.Rimworld.Should().Be("/games/RimWorld");
        settings.Paths.Workshop.Should().Be("/games/workshop/294100");
        settings.Paths.BaseRefList.Should().BeEmpty();
        settings.Languages.Original.Display.Should().Be("English");
        settings.Languages.Translation.Display.Should().Be("Korean (한국어)");
        settings.Extraction.CurrentVersion.ToString().Should().Be("1.6");
        settings.Extraction.CommentOriginal.Should().BeFalse();
        settings.Extraction.EnableTkey.Should().BeFalse();
        settings.Output.Policy.Should().Be(DuplicatesPolicy.Overwrite);
        settings.Output.Format.Should().Be(ExtractionFormat.Languages);
    }

    [Fact]
    public void Read_ValidFile_ParsesExtractionRulesViaDsl()
    {
        var path = WritePrefabs(SampleLines());

        var settings = LegacyPrefabsReader.Read(path);

        settings.Extraction.Rules.Should().HaveCount(3);
        var baseDesc = settings.Extraction.Rules.Single(r => r.Tag == "baseDesc");
        baseDesc.Whitelist.Should().ContainSingle().Which.Should().Be("ThingDef");
        baseDesc.Blacklist.Should().ContainSingle().Which.Should().Be("TrashDef");
    }

    [Fact]
    public void Read_ValidFile_ParsesFullListTags()
    {
        var path = WritePrefabs(SampleLines());

        var settings = LegacyPrefabsReader.Read(path);

        settings.Extraction.FullListTags.Should().BeEquivalentTo(new[] { "rulesFiles", "rulesStrings" });
    }

    [Fact]
    public void Read_ValidFile_ParsesNodeReplacements()
    {
        var path = WritePrefabs(SampleLines());

        var settings = LegacyPrefabsReader.Read(path);

        settings.Extraction.NodeReplacements.Should().HaveCount(2);
        settings.Extraction.NodeReplacements[0].From.Should().Be("CombatExtended.AmmoDef+*");
        settings.Extraction.NodeReplacements[0].To.Should().Be("ThingDef+*");
    }

    [Fact]
    public void Read_ValidFile_ParsesTranslationHandles()
    {
        var path = WritePrefabs(SampleLines());

        var settings = LegacyPrefabsReader.Read(path);

        settings.Extraction.TranslationHandles.Should().HaveCount(2);
        settings.Extraction.TranslationHandles[0].Tag.Should().Be("verbClass");
        settings.Extraction.TranslationHandles[0].IsWildcardClass.Should().BeTrue();
    }

    [Fact]
    public void Read_WithWrongVersion_ThrowsInvalidDataException()
    {
        var lines = SampleLines();
        lines[1] = "8";
        var path = WritePrefabs(lines);

        var act = () => LegacyPrefabsReader.Read(path);

        act.Should().Throw<InvalidDataException>().WithMessage("*version*");
    }

    [Fact]
    public void Read_NonexistentPath_ThrowsFileNotFoundException()
    {
        var act = () => LegacyPrefabsReader.Read(Path.Combine(_tmpDir, "missing.dat"));

        act.Should().Throw<FileNotFoundException>();
    }
}
```

- [ ] **Step 2: Run — compile fails.**

- [ ] **Step 3: Write the implementation**

```csharp
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Domain.Rules;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Infrastructure.Legacy;

/// <summary>
/// Reads a legacy <c>Prefabs.dat</c> (line-delimited text, schema version 9) into the
/// modern <see cref="AppSettings"/> shape. Read-only; does not write back to the legacy format.
/// </summary>
public static class LegacyPrefabsReader
{
    private const string SupportedVersion = "9";

    public static AppSettings Read(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Legacy Prefabs.dat not found: {path}", path);

        var lines = File.ReadAllLines(path);
        if (lines.Length < 18)
            throw new InvalidDataException($"Prefabs.dat is truncated (expected 18 lines, got {lines.Length}).");

        // Line 0: header marker — ignored.
        var version = lines[1];
        if (version != SupportedVersion)
            throw new InvalidDataException(
                $"Unsupported Prefabs.dat version '{version}'. Supported: '{SupportedVersion}'.");

        var enableTkey = bool.Parse(lines[2]);
        var pathRw = lines[3];
        var pathWs = lines[4];
        var pathBase = lines[5];
        var currentVersion = GameVersion.Parse(lines[6]);
        // Lines 7-8 (patternVersion, patternVersionWithV) are intentionally dropped.
        var origLang = LanguageCode.Create(lines[9]);
        var transLang = LanguageCode.Create(lines[10]);
        var commentOriginal = bool.Parse(lines[11]);

        var rules = ParseList(lines[12], LegacyExtractionRuleParser.Parse).ToList();
        var fullListTags = ParseList(lines[13], s => s).ToList();
        var nodeReplacements = ParseList(lines[14], ParseNodeReplacement).ToList();
        var translationHandles = ParseList(lines[15], TranslationHandle.Parse).ToList();

        var policy = Enum.Parse<DuplicatesPolicy>(lines[16]);
        // Legacy enum name was "ExtractionMethod" with identical member names.
        var format = Enum.Parse<ExtractionFormat>(lines[17]);

        return new AppSettings(
            SchemaVersion: AppSettings.CurrentSchemaVersion,
            Paths: new PathSettings(pathRw, pathWs, pathBase),
            Languages: new LanguageSettings(origLang, transLang),
            Extraction: new ExtractionSettings(
                CurrentVersion: currentVersion,
                CommentOriginal: commentOriginal,
                EnableTkey: enableTkey,
                Rules: rules,
                FullListTags: fullListTags,
                NodeReplacements: nodeReplacements,
                TranslationHandles: translationHandles),
            Output: new OutputSettings(policy, format));
    }

    private static IEnumerable<T> ParseList<T>(string line, Func<string, T> parse)
    {
        if (string.IsNullOrEmpty(line)) return Array.Empty<T>();
        return line.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(parse);
    }

    private static NodeReplacementRule ParseNodeReplacement(string raw)
    {
        var pipe = raw.IndexOf('|');
        if (pipe <= 0 || pipe == raw.Length - 1)
            throw new FormatException($"NodeReplacement entry missing '|' separator: {raw}");
        var from = raw[..pipe].Trim();
        var to = raw[(pipe + 1)..].Trim();
        return new NodeReplacementRule(from, to);
    }
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**

```bash
git add src/RimworldExtractor.Infrastructure/Legacy/LegacyPrefabsReader.cs \
        tests/RimworldExtractor.Infrastructure.Tests/Legacy/LegacyPrefabsReaderTests.cs
git commit -m "feat(infrastructure): add LegacyPrefabsReader (v9 Prefabs.dat → AppSettings)"
```

### Task 23: End-to-end migration integration test

**Files:**
- Create: `tests/RimworldExtractor.Infrastructure.Tests/Integration/LegacyMigrationIntegrationTests.cs`

Validates the full migration path: legacy `Prefabs.dat` → `LegacyPrefabsReader.Read` → `JsonSettingsStore.SaveAsync` → `LoadAsync` → equals original parse.

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure.Legacy;
using RimworldExtractor.Infrastructure.Settings;

namespace RimworldExtractor.Infrastructure.Tests.Integration;

public class LegacyMigrationIntegrationTests : IDisposable
{
    private readonly string _tmpDir;

    public LegacyMigrationIntegrationTests()
    {
        _tmpDir = Directory.CreateTempSubdirectory("rwx-migrate-").FullName;
    }

    public void Dispose()
    {
        try { Directory.Delete(_tmpDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task Migrate_LegacyPrefabsToJsonSettings_RoundTripsEqual()
    {
        // Arrange: build a realistic legacy Prefabs.dat
        var prefabsPath = Path.Combine(_tmpDir, "Prefabs.dat");
        File.WriteAllLines(prefabsPath, new[]
        {
            "DO NOT EDIT THIS MANUALLY",
            "9",
            "False",
            "/games/RimWorld",
            "/games/workshop/294100",
            "",
            "1.6",
            @"^[1]\.\d+",
            @"^v[1]\.\d+",
            "English",
            "Korean (한국어)",
            "False",
            "label/description+ThingDef-TrashDef",
            "rulesStrings",
            "Mod.X+*|ThingDef+*",
            "*verbClass",
            "Overwrite",
            "Languages",
        });

        // Act: legacy read → JSON save → JSON load
        var readFromLegacy = LegacyPrefabsReader.Read(prefabsPath);
        var jsonPath = Path.Combine(_tmpDir, "settings.json");
        var store = new JsonSettingsStore(jsonPath);
        await store.SaveAsync(readFromLegacy);
        var readFromJson = await store.LoadAsync();

        // Assert: lossless
        readFromJson.Should().Be(readFromLegacy);
        File.Exists(jsonPath).Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run — should pass immediately** (all components exist). If it fails, investigate — likely a JSON converter issue for one of the domain types that lacks a converter or has bad equality.

Expected: **1 passed**.

- [ ] **Step 3: Commit**

```bash
git add tests/RimworldExtractor.Infrastructure.Tests/Integration/LegacyMigrationIntegrationTests.cs
git commit -m "test(infrastructure): integration test for legacy Prefabs.dat → settings.json"
```

---

## Group 2H — Verification Gate

### Task 24: Full build + test + coverage + checkpoint summary

- [ ] **Step 1: Full build**

```bash
dotnet build RimworldExtractor.slnx -c Release 2>&1 | tail -5
```
Expected: 0 warnings, 0 errors.

- [ ] **Step 2: Full test run**

```bash
dotnet test RimworldExtractor.slnx -c Release 2>&1 | tail -15
```
Expected: all tests pass. Count the expected tests: ~40 from Phase 2 + 1 from Phase 1.

- [ ] **Step 3: Coverage check**

```bash
dotnet test RimworldExtractor.slnx -c Release \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults
```

Find the coverage file: `ls TestResults/*/coverage.cobertura.xml`. Open it and confirm Domain + Infrastructure line coverage ≥ 80%. If below, add tests for uncovered branches before declaring done. Report actual coverage percentages in the checkpoint summary.

- [ ] **Step 4: Format check**

```bash
dotnet format RimworldExtractor.slnx --verify-no-changes
```
Expected: exit 0.

- [ ] **Step 5: Legacy unaffected**

```bash
dotnet test legacy/RimworldExtractorTest/RimworldExtractorTest.csproj -c Release --filter "FullyQualifiedName~LegacyBaselineTests" 2>&1 | tail -5
```
Expected: 1 passed.

- [ ] **Step 6: Push branch**

```bash
git push origin feat/remake-v2
```

- [ ] **Step 7: Report Phase 2 checkpoint summary to the user**

Include:
- Commit count (from `git log --oneline 53fde49..HEAD | wc -l`)
- Test count (passed / failed / skipped)
- Coverage numbers per project
- Any deviations from the plan (e.g., if a test revealed a missing JSON converter)
- Any concerns for Phase 3

---

## File Structure (Final — Phase 2 end state)

```
src/RimworldExtractor.Domain/
├── Abstractions/
│   ├── IExtractionPipeline.cs          (Phase 1)
│   └── ISettingsStore.cs               (Task 18)
├── Enums/
│   ├── DuplicatesPolicy.cs             (Task 1)
│   ├── ExtractionFormat.cs             (Task 2)
│   └── FolderKind.cs                   (Task 3)
├── ValueObjects/
│   ├── DefName.cs                      (Task 4)
│   ├── PackageId.cs                    (Task 5)
│   ├── GameVersion.cs                  (Task 6, edited in Task 17)
│   └── LanguageCode.cs                 (Task 7, edited in Task 17)
├── Mods/
│   ├── ModReference.cs                 (Task 8)
│   └── RequiredMods.cs                 (Task 9)
├── Entities/
│   ├── TranslationEntry.cs             (Task 10)
│   ├── ModMetadata.cs                  (Task 11)
│   └── ExtractableFolder.cs            (Task 12)
├── Rules/
│   ├── ExtractionRule.cs               (Task 13)
│   ├── NodeReplacementRule.cs          (Task 14)
│   └── TranslationHandle.cs            (Task 15)
└── Settings/
    ├── AppSettings.cs                  (Task 16)
    ├── PathSettings.cs                 (Task 16)
    ├── LanguageSettings.cs             (Task 16)
    ├── ExtractionSettings.cs           (Task 16)
    ├── OutputSettings.cs               (Task 16)
    └── Json/
        ├── GameVersionJsonConverter.cs (Task 17)
        └── LanguageCodeJsonConverter.cs(Task 17)

src/RimworldExtractor.Infrastructure/
├── DependencyInjection.cs              (Task 20)
├── Settings/
│   ├── AppSettingsJsonContext.cs       (Task 17)
│   └── JsonSettingsStore.cs            (Task 19)
└── Legacy/
    ├── LegacyExtractionRuleParser.cs   (Task 21)
    └── LegacyPrefabsReader.cs          (Task 22)

tests/
├── RimworldExtractor.Domain.Tests/…    (16 test files)
└── RimworldExtractor.Infrastructure.Tests/
    ├── DependencyInjectionTests.cs
    ├── Settings/
    │   ├── AppSettingsJsonContextTests.cs
    │   └── JsonSettingsStoreTests.cs
    ├── Legacy/
    │   ├── LegacyExtractionRuleParserTests.cs
    │   └── LegacyPrefabsReaderTests.cs
    └── Integration/
        └── LegacyMigrationIntegrationTests.cs
```

---

## Self-Review

**Spec coverage:**
- ✅ Domain records + value objects + rules (Tasks 1-15)
- ✅ AppSettings schema with nested settings (Task 16)
- ✅ STJ source generator for AOT-safe serialization (Task 17)
- ✅ ISettingsStore abstraction in Domain (Task 18)
- ✅ JsonSettingsStore with atomic write + backup (Task 19)
- ✅ LegacyPrefabsReader for Prefabs.dat → AppSettings (Task 22)
- ✅ DI wiring for Infrastructure (Task 20)
- ✅ Integration round-trip test (Task 23)
- ✅ Coverage + format gate (Task 24)

**Placeholder scan:** none. All step code blocks are concrete.

**Type consistency:**
- `RequiredMods` namespace: `RimworldExtractor.Domain.Mods` (Task 9), referenced consistently in `TranslationEntry` (Task 10) and elsewhere.
- `ModReference`: `RimworldExtractor.Domain.Mods` (Task 8).
- `ExtractionRule.Whitelist`/`Blacklist` are `IReadOnlySet<string>` — the `SequenceEqual` in `Equals` would miss unordered equivalence, so I use `SetEquals`. Consistent across tests and impl.
- `AppSettings.CurrentSchemaVersion = 2`. `LegacyPrefabsReader` supports legacy version `"9"` (the legacy schema version), outputs `SchemaVersion = 2` (new schema). These numbers are NOT the same concept — clear from context.

**Known risk:** `AppSettingsJsonContext` with `ExtractionRule` containing `IReadOnlySet<string>` — STJ source generator may not handle `IReadOnlySet` interface type cleanly. If it complains, change `ExtractionRule` properties to concrete `IReadOnlyCollection<string>` (with internal `HashSet` storage) or add a converter. Plan to surface this at Task 17 execution and fix before Task 18.

---

## Task Execution Order (for subagent dispatch)

1. Tasks 1-3 (Enums) — Group 2A
2. Tasks 4-7 (Value Objects) — Group 2B
3. Tasks 8-9 (Mods) — Group 2D-Mods
4. Tasks 10-12 (Entities) — Group 2C
5. Tasks 13-15 (Rules) — Group 2D-Rules
6. Task 16 (AppSettings tree) — Group 2E
7. Task 17 (JSON source gen + converters + Domain edits) — Group 2E
8. Task 18 (ISettingsStore) — Group 2E
9. Task 19 (JsonSettingsStore) — Group 2F
10. Task 20 (DI wiring) — Group 2F
11. Task 21 (LegacyExtractionRuleParser) — Group 2G
12. Task 22 (LegacyPrefabsReader) — Group 2G
13. Task 23 (Integration test) — Group 2G
14. Task 24 (Gate + checkpoint) — Group 2H

Batching for subagent dispatch: 2A + 2B as one agent (7 mechanical tasks) / 2D + 2C + 2D-Rules as one agent (8 mechanical tasks) / 2E as one agent (3 tasks with JSON converter choreography) / 2F as one agent (2 tasks) / 2G as one agent (3 tasks with domain-semantic parsing) / 2H solo (verification).

Total: 6 subagent dispatches, each with spec + code-quality review.

---

## Execution Handoff

Plan complete and saved to `docs/plans/remake-v2-phase2-domain.md`. Two execution options:

**1. Subagent-Driven (recommended)** — I dispatch a fresh subagent per task group, review between tasks, fast iteration.

**2. Inline Execution** — Execute tasks in this session using executing-plans, batch execution with checkpoints.

Which approach?
