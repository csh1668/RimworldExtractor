# Phase 3 — Infrastructure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans`. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Replace legacy 900-line `IO.cs` + 469-line `Extractor.DefsUtils.cs` + 400-line `PatchOperations.cs` + mod-discovery with focused Infrastructure adapters: `FileSystem/`, `Xml/`, `Excel/`, `Output/`, `Legacy/`. All XML code uses `XDocument`/`XElement` (not legacy `XmlDocument`). Atomic writes via a shared `SafeFileWriter`. No pipeline orchestration yet — Phase 4's concern.

**Architecture:**
- `IFileSystem` abstraction (our own, not `System.IO.Abstractions` — AOT/trim-friendly)
- `XDocument`-based parsers with `IXmlDefParser`, `IXmlInheritanceResolver`, `IXPatchProcessor`
- `ClosedXML` wrapped behind `IExcelReader`/`IExcelWriter`
- `IOutputStrategy` — strategy over Excel vs. Languages vs. LanguagesWithComments
- `IConflictResolver` + `SafeFileWriter` replace legacy `Prefabs.StopCallback*` delegates

**Tech Stack:** .NET 10 · `System.Xml.Linq` (XDocument) · ClosedXML 0.104.2 · `System.IO.Compression` · xunit.v3 · FluentAssertions 6.12.2 · NSubstitute · Verify.XunitV3.

**Branch:** `feat/remake-v2` at `44652b2` (Phase 2 complete, 128 tests green).

**Legacy references (read-only):**
- `legacy/RimworldExtractorInternal/IO.cs` (900L) — `ToExcel`/`ModifyExcel`/`FromExcel`/`ToLanguageXml`/`FromLanguageXml`/`SaveSafely`/`DescendantFiles`/`DoFullListTranslation`/`ReadXml`
- `legacy/RimworldExtractorInternal/Extractor.DefsUtils.cs` (469L) — `LoadReferenceDefs`, `FindExtractableNodes`, `DoXmlInheritance`, `XmlOverwriteRecursive`, `MatchTranslationHandle`, `NormalizedHandle`, `GetRootDefNode`, `GetIdxOfListNode`
- `legacy/RimworldExtractorInternal/PatchOperations.cs` (405L) — 9 operation handlers
- `legacy/RimworldExtractorInternal/ModLister.cs` — mod discovery + `GetExtractableFolders`
- `legacy/RimworldExtractorInternal/LibreExcelFixer.cs` — XLSX comment stripping
- `legacy/RimworldExtractorInternal/Utils.cs` — XML extension helpers (port to XDocument)

---

## Sub-batch overview

| # | Sub-batch | Tasks | Outcome |
|---|-----------|-------|---------|
| 3A | File system | T1-T4 | `IFileSystem`/`PhysicalFileSystem`/`InMemoryFileSystem` + `FileSystemGateway` |
| 3B | Safe write + conflict | T5-T8 | `SafeFileWriter`, refactor `JsonSettingsStore`, `IConflictResolver`, `PolicyBasedConflictResolver` |
| 3C | Mod discovery | T9-T12 | `IModLister`, `FileSystemModLister` (About.xml + LoadFolders.xml + version dirs) |
| 3D | XML def parsing | T13-T18 | `XmlHelpers` (XDocument extensions), `IXmlDefParser`, `XDocumentDefParser`, `IXmlInheritanceResolver`, `XmlInheritanceResolver` |
| 3E | Patch operations | T19-T25 | `IXPatchProcessor`, 7 operation handlers (Add, Replace, Insert, AddModExtension, Sequence, FindMod, Attribute) |
| 3F | Languages XML IO | T26-T30 | `IXmlLanguagesReader/Writer`, DefInjected/Keyed/Strings/Patches, FullListTranslation |
| 3G | Excel IO | T31-T34 | `LibreOfficePostProcessor`, `IExcelReader/Writer`, `ClosedXmlReader/Writer` |
| 3H | Output strategy + DI | T35-T38 | `IOutputStrategy`, 3 concrete strategies, extend `AddInfrastructure` |
| 3I | Verification gate | T39 | Full build/test/coverage/format + push |

**~40 tasks, 9 subagent dispatches.** Each task: red → green → commit. Each sub-batch is one implementer dispatch with combined spec+quality review.

---

## Global conventions

- All file-scoped namespaces: `RimworldExtractor.Infrastructure.{FileSystem,Xml,Excel,Output,Legacy}` etc.
- All async methods take `CancellationToken cancellationToken = default`
- Use `IFileSystem` in all places where we would otherwise call `File.*` / `Directory.*` — no direct static IO in Infrastructure-tested code
- Use `XDocument`/`XElement` throughout — NEVER `XmlDocument`/`XmlNode` in new code
- Prefer `IReadOnlyList<T>` / `IReadOnlyCollection<T>` for input parameters
- Records for pure data; regular classes only when instance identity or lifecycle matters
- Test file layout mirrors src: `tests/RimworldExtractor.Infrastructure.Tests/{FileSystem,Xml,Excel,Output,Legacy}/`
- Every interface gets its own file; concrete implementations in separate files

---

## Group 3A — File System

### Task 1: IFileSystem interface + data types

**Files:**
- Create: `src/RimworldExtractor.Infrastructure/FileSystem/IFileSystem.cs`

- [ ] **Step 1: Write `src/RimworldExtractor.Infrastructure/FileSystem/IFileSystem.cs`**

```csharp
namespace RimworldExtractor.Infrastructure.FileSystem;

/// <summary>
/// File-system abstraction. Infrastructure code should depend on this, never the static
/// <see cref="File"/> / <see cref="Directory"/> classes, so that a test double can be
/// substituted. The concrete <see cref="PhysicalFileSystem"/> wraps real disk IO.
/// </summary>
public interface IFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);
    Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);
    Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default);
    Task WriteAllLinesAsync(string path, IEnumerable<string> lines, CancellationToken cancellationToken = default);
    Stream OpenRead(string path);
    Stream OpenWrite(string path);
    Stream CreateFile(string path);
    void Move(string source, string destination);
    void Replace(string source, string destination, string? backup);
    void Delete(string path);
    IEnumerable<string> EnumerateFiles(string path);
    IEnumerable<string> EnumerateDirectories(string path);
}
```

- [ ] **Step 2: Build**
```bash
dotnet build src/RimworldExtractor.Infrastructure -c Release 2>&1 | tail -3
```
Expected: 0 warnings, 0 errors.

- [ ] **Step 3: Commit**
```bash
git add src/RimworldExtractor.Infrastructure/FileSystem/IFileSystem.cs
git commit -m "feat(infrastructure): add IFileSystem abstraction"
```

### Task 2: PhysicalFileSystem (disk-backed default)

**Files:**
- Create: `src/RimworldExtractor.Infrastructure/FileSystem/PhysicalFileSystem.cs`
- Test: `tests/RimworldExtractor.Infrastructure.Tests/FileSystem/PhysicalFileSystemTests.cs`

- [ ] **Step 1: Write failing test** — `tests/RimworldExtractor.Infrastructure.Tests/FileSystem/PhysicalFileSystemTests.cs`:

```csharp
using FluentAssertions;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Tests.FileSystem;

public class PhysicalFileSystemTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly IFileSystem _fs;

    public PhysicalFileSystemTests()
    {
        _tmpDir = Directory.CreateTempSubdirectory("rwx-pfs-").FullName;
        _fs = new PhysicalFileSystem();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tmpDir, recursive: true); } catch { /* best-effort */ }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task WriteAndReadAllText_RoundTrips()
    {
        var path = Path.Combine(_tmpDir, "a.txt");

        await _fs.WriteAllTextAsync(path, "hello", TestContext.Current.CancellationToken);
        var read = await _fs.ReadAllTextAsync(path, TestContext.Current.CancellationToken);

        read.Should().Be("hello");
    }

    [Fact]
    public void FileExists_False_ForMissingFile()
    {
        _fs.FileExists(Path.Combine(_tmpDir, "missing.txt")).Should().BeFalse();
    }

    [Fact]
    public void CreateDirectory_IsIdempotent()
    {
        var path = Path.Combine(_tmpDir, "nested", "deep");

        _fs.CreateDirectory(path);
        _fs.CreateDirectory(path);

        _fs.DirectoryExists(path).Should().BeTrue();
    }

    [Fact]
    public void EnumerateFiles_ReturnsOnlyTopLevelFiles()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "a.txt"), "a");
        File.WriteAllText(Path.Combine(_tmpDir, "b.txt"), "b");
        Directory.CreateDirectory(Path.Combine(_tmpDir, "sub"));
        File.WriteAllText(Path.Combine(_tmpDir, "sub", "c.txt"), "c");

        var files = _fs.EnumerateFiles(_tmpDir).Select(Path.GetFileName).ToHashSet();

        files.Should().BeEquivalentTo(new[] { "a.txt", "b.txt" });
    }
}
```

- [ ] **Step 2: Run — compile fails (PhysicalFileSystem doesn't exist).**
```bash
dotnet test tests/RimworldExtractor.Infrastructure.Tests --filter "FullyQualifiedName~PhysicalFileSystemTests" 2>&1 | tail -5
```

- [ ] **Step 3: Write `src/RimworldExtractor.Infrastructure/FileSystem/PhysicalFileSystem.cs`**

```csharp
namespace RimworldExtractor.Infrastructure.FileSystem;

/// <summary>
/// Default <see cref="IFileSystem"/> implementation backed by the real filesystem.
/// </summary>
public sealed class PhysicalFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        => File.ReadAllTextAsync(path, cancellationToken);
    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
        => File.WriteAllTextAsync(path, contents, cancellationToken);
    public Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default)
        => File.ReadAllLinesAsync(path, cancellationToken);
    public Task WriteAllLinesAsync(string path, IEnumerable<string> lines, CancellationToken cancellationToken = default)
        => File.WriteAllLinesAsync(path, lines, cancellationToken);
    public Stream OpenRead(string path) => File.OpenRead(path);
    public Stream OpenWrite(string path) => File.OpenWrite(path);
    public Stream CreateFile(string path) => File.Create(path);
    public void Move(string source, string destination) => File.Move(source, destination);
    public void Replace(string source, string destination, string? backup) => File.Replace(source, destination, backup);
    public void Delete(string path) => File.Delete(path);
    public IEnumerable<string> EnumerateFiles(string path) => Directory.EnumerateFiles(path);
    public IEnumerable<string> EnumerateDirectories(string path) => Directory.EnumerateDirectories(path);
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**
```bash
git add src/RimworldExtractor.Infrastructure/FileSystem/PhysicalFileSystem.cs \
        tests/RimworldExtractor.Infrastructure.Tests/FileSystem/PhysicalFileSystemTests.cs
git commit -m "feat(infrastructure): add PhysicalFileSystem (disk-backed default)"
```

### Task 3: InMemoryFileSystem (test double)

**Files:**
- Create: `src/RimworldExtractor.Infrastructure/FileSystem/InMemoryFileSystem.cs`
- Test: `tests/RimworldExtractor.Infrastructure.Tests/FileSystem/InMemoryFileSystemTests.cs`

Rationale: for pure-Infrastructure tests we need a deterministic, isolated FS. Keep it in `src/` (not `tests/`) so Phase 4 integration tests can reuse it.

- [ ] **Step 1: Write failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Tests.FileSystem;

public class InMemoryFileSystemTests
{
    [Fact]
    public async Task WriteThenRead_RoundTrips()
    {
        var fs = new InMemoryFileSystem();

        await fs.WriteAllTextAsync("/foo.txt", "hello");
        var read = await fs.ReadAllTextAsync("/foo.txt");

        read.Should().Be("hello");
    }

    [Fact]
    public async Task ReadNonexistent_Throws()
    {
        var fs = new InMemoryFileSystem();

        var act = () => fs.ReadAllTextAsync("/missing.txt");

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public void FileExists_ReflectsAddFile()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/foo.txt", "x");

        fs.FileExists("/foo.txt").Should().BeTrue();
        fs.FileExists("/bar.txt").Should().BeFalse();
    }

    [Fact]
    public void EnumerateFiles_ReturnsOnlyImmediateChildren()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/a/1.txt", "");
        fs.AddFile("/a/2.txt", "");
        fs.AddFile("/a/b/3.txt", "");

        var files = fs.EnumerateFiles("/a").Select(p => p).ToList();

        files.Should().HaveCount(2);
        files.Should().Contain("/a/1.txt");
        files.Should().Contain("/a/2.txt");
    }

    [Fact]
    public void CreateDirectory_IsIdempotent()
    {
        var fs = new InMemoryFileSystem();
        fs.CreateDirectory("/a/b/c");
        fs.CreateDirectory("/a/b/c");

        fs.DirectoryExists("/a/b/c").Should().BeTrue();
        fs.DirectoryExists("/a/b").Should().BeTrue();
        fs.DirectoryExists("/a").Should().BeTrue();
    }

    [Fact]
    public async Task Replace_SwapsContents_AndCreatesBackup()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/target.txt", "old");
        fs.AddFile("/new.txt", "new");

        fs.Replace("/new.txt", "/target.txt", "/target.txt.bak");

        (await fs.ReadAllTextAsync("/target.txt")).Should().Be("new");
        (await fs.ReadAllTextAsync("/target.txt.bak")).Should().Be("old");
        fs.FileExists("/new.txt").Should().BeFalse();
    }
}
```

- [ ] **Step 2: Red.**

- [ ] **Step 3: Write `src/RimworldExtractor.Infrastructure/FileSystem/InMemoryFileSystem.cs`**

```csharp
using System.Text;

namespace RimworldExtractor.Infrastructure.FileSystem;

/// <summary>
/// In-memory <see cref="IFileSystem"/> for deterministic testing. Thread-unsafe — one
/// instance per test. Paths are case-sensitive and use forward slashes.
/// </summary>
public sealed class InMemoryFileSystem : IFileSystem
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);
    private readonly HashSet<string> _directories = new(StringComparer.Ordinal);

    public InMemoryFileSystem()
    {
        _directories.Add("/");
    }

    public void AddFile(string path, string contents)
    {
        EnsureParentDirs(path);
        _files[path] = Encoding.UTF8.GetBytes(contents);
    }

    public bool FileExists(string path) => _files.ContainsKey(path);
    public bool DirectoryExists(string path) => _directories.Contains(path);

    public void CreateDirectory(string path)
    {
        var parts = path.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var current = "";
        foreach (var part in parts)
        {
            current = current + "/" + part;
            _directories.Add(current);
        }
    }

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!_files.TryGetValue(path, out var bytes))
            throw new FileNotFoundException("File not found in InMemoryFileSystem", path);
        return Task.FromResult(Encoding.UTF8.GetString(bytes));
    }

    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        EnsureParentDirs(path);
        _files[path] = Encoding.UTF8.GetBytes(contents);
        return Task.CompletedTask;
    }

    public Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!_files.TryGetValue(path, out var bytes))
            throw new FileNotFoundException("File not found in InMemoryFileSystem", path);
        var text = Encoding.UTF8.GetString(bytes);
        return Task.FromResult(text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));
    }

    public Task WriteAllLinesAsync(string path, IEnumerable<string> lines, CancellationToken cancellationToken = default)
    {
        EnsureParentDirs(path);
        _files[path] = Encoding.UTF8.GetBytes(string.Join('\n', lines) + '\n');
        return Task.CompletedTask;
    }

    public Stream OpenRead(string path)
    {
        if (!_files.TryGetValue(path, out var bytes))
            throw new FileNotFoundException("File not found in InMemoryFileSystem", path);
        return new MemoryStream(bytes, writable: false);
    }

    public Stream OpenWrite(string path) => CreateFile(path);

    public Stream CreateFile(string path)
    {
        EnsureParentDirs(path);
        var ms = new WritebackStream(bytes => _files[path] = bytes);
        return ms;
    }

    public void Move(string source, string destination)
    {
        if (!_files.TryGetValue(source, out var bytes))
            throw new FileNotFoundException("Source not found in InMemoryFileSystem", source);
        EnsureParentDirs(destination);
        _files[destination] = bytes;
        _files.Remove(source);
    }

    public void Replace(string source, string destination, string? backup)
    {
        if (!_files.TryGetValue(source, out var newBytes))
            throw new FileNotFoundException("Source not found in InMemoryFileSystem", source);
        if (_files.TryGetValue(destination, out var oldBytes) && backup is not null)
        {
            EnsureParentDirs(backup);
            _files[backup] = oldBytes;
        }
        _files[destination] = newBytes;
        _files.Remove(source);
    }

    public void Delete(string path) => _files.Remove(path);

    public IEnumerable<string> EnumerateFiles(string path)
    {
        var prefix = path.TrimEnd('/') + "/";
        return _files.Keys
            .Where(p => p.StartsWith(prefix, StringComparison.Ordinal))
            .Where(p => !p[prefix.Length..].Contains('/'))
            .OrderBy(p => p, StringComparer.Ordinal);
    }

    public IEnumerable<string> EnumerateDirectories(string path)
    {
        var prefix = path.TrimEnd('/') + "/";
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dir in _directories)
        {
            if (!dir.StartsWith(prefix, StringComparison.Ordinal)) continue;
            var suffix = dir[prefix.Length..];
            if (suffix.Length == 0 || suffix.Contains('/')) continue;
            result.Add(dir);
        }
        return result.OrderBy(p => p, StringComparer.Ordinal);
    }

    private void EnsureParentDirs(string path)
    {
        var lastSlash = path.LastIndexOf('/');
        if (lastSlash > 0) CreateDirectory(path[..lastSlash]);
    }

    private sealed class WritebackStream : MemoryStream
    {
        private readonly Action<byte[]> _onDispose;
        private bool _disposed;
        public WritebackStream(Action<byte[]> onDispose) { _onDispose = onDispose; }
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _onDispose(ToArray());
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**
```bash
git add src/RimworldExtractor.Infrastructure/FileSystem/InMemoryFileSystem.cs \
        tests/RimworldExtractor.Infrastructure.Tests/FileSystem/InMemoryFileSystemTests.cs
git commit -m "feat(infrastructure): add InMemoryFileSystem test double"
```

### Task 4: FileSystemGateway (DescendantFiles + PathCombineCreateDir helpers)

**Legacy ref:** `IO.cs:811-817` `PathCombineCreateDir`, `IO.cs:877-898` `DescendantFiles` (BFS, sorted).

**Files:**
- Create: `src/RimworldExtractor.Infrastructure/FileSystem/FileSystemGateway.cs`
- Test: `tests/RimworldExtractor.Infrastructure.Tests/FileSystem/FileSystemGatewayTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Tests.FileSystem;

public class FileSystemGatewayTests
{
    [Fact]
    public void DescendantFiles_YieldsBfsOrderedFiles()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/root/z.xml", "");
        fs.AddFile("/root/a.xml", "");
        fs.AddFile("/root/sub2/y.xml", "");
        fs.AddFile("/root/sub1/b.xml", "");
        var gateway = new FileSystemGateway(fs);

        var result = gateway.DescendantFiles("/root").ToList();

        // BFS: all files of /root first (sorted), then /sub1, then /sub2.
        result.Should().Equal(
            "/root/a.xml",
            "/root/z.xml",
            "/root/sub1/b.xml",
            "/root/sub2/y.xml");
    }

    [Fact]
    public void DescendantFiles_ReturnsEmpty_ForMissingRoot()
    {
        var fs = new InMemoryFileSystem();
        var gateway = new FileSystemGateway(fs);

        gateway.DescendantFiles("/missing").Should().BeEmpty();
    }

    [Fact]
    public void PathCombineCreateDir_CreatesAllIntermediateDirs()
    {
        var fs = new InMemoryFileSystem();
        var gateway = new FileSystemGateway(fs);

        var path = gateway.PathCombineCreateDir("/root", "a", "b");

        path.Should().Be(Path.Combine("/root", "a", "b"));
        fs.DirectoryExists(path.Replace('\\', '/')).Should().BeTrue();
    }
}
```

- [ ] **Step 2: Red.**

- [ ] **Step 3: Write implementation**

```csharp
namespace RimworldExtractor.Infrastructure.FileSystem;

/// <summary>
/// Higher-level file-system helpers: BFS enumeration, directory creation with combine.
/// Delegates to an <see cref="IFileSystem"/> so it works with any FS backend.
/// </summary>
public sealed class FileSystemGateway
{
    private readonly IFileSystem _fs;

    public FileSystemGateway(IFileSystem fs) => _fs = fs;

    /// <summary>BFS-enumerates all files under <paramref name="root"/>; subdirectories and files are each sorted ordinally for determinism.</summary>
    public IEnumerable<string> DescendantFiles(string root)
    {
        if (!_fs.DirectoryExists(root)) yield break;

        var queue = new Queue<string>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var sub in _fs.EnumerateDirectories(current).OrderBy(x => x, StringComparer.Ordinal))
                queue.Enqueue(sub);
            foreach (var file in _fs.EnumerateFiles(current).OrderBy(x => x, StringComparer.Ordinal))
                yield return file;
        }
    }

    /// <summary>Combines the given path segments, creates the directory if missing, returns the full path.</summary>
    public string PathCombineCreateDir(params string[] parts)
    {
        var combined = Path.Combine(parts);
        if (!_fs.DirectoryExists(combined)) _fs.CreateDirectory(combined);
        return combined;
    }
}
```

- [ ] **Step 4: Run — pass.**

- [ ] **Step 5: Commit**
```bash
git add src/RimworldExtractor.Infrastructure/FileSystem/FileSystemGateway.cs \
        tests/RimworldExtractor.Infrastructure.Tests/FileSystem/FileSystemGatewayTests.cs
git commit -m "feat(infrastructure): add FileSystemGateway (BFS DescendantFiles + PathCombineCreateDir)"
```

---

## Group 3B — Safe write + Conflict resolution

### Task 5: SafeFileWriter (atomic temp→replace pattern)

**Rationale:** Phase 2 `JsonSettingsStore.SaveAsync` already does atomic write inline. Extract the pattern to a reusable utility so XML/Excel writers in Phase 3 share the same guarantees.

**Files:**
- Create: `src/RimworldExtractor.Infrastructure/FileSystem/SafeFileWriter.cs`
- Test: `tests/RimworldExtractor.Infrastructure.Tests/FileSystem/SafeFileWriterTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Tests.FileSystem;

public class SafeFileWriterTests
{
    [Fact]
    public async Task WriteAsync_WhenTargetAbsent_MovesTempToTarget()
    {
        var fs = new InMemoryFileSystem();
        var writer = new SafeFileWriter(fs);

        await writer.WriteAsync("/out.txt", async stream =>
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("hello");
            await stream.WriteAsync(bytes);
        });

        (await fs.ReadAllTextAsync("/out.txt")).Should().Be("hello");
        fs.FileExists("/out.txt.tmp").Should().BeFalse();
    }

    [Fact]
    public async Task WriteAsync_WhenTargetExists_CreatesBackup()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/out.txt", "old");
        var writer = new SafeFileWriter(fs);

        await writer.WriteAsync("/out.txt", async stream =>
        {
            await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes("new"));
        });

        (await fs.ReadAllTextAsync("/out.txt")).Should().Be("new");
        (await fs.ReadAllTextAsync("/out.txt.bak")).Should().Be("old");
    }

    [Fact]
    public async Task WriteAsync_CreatesParentDirectoryIfMissing()
    {
        var fs = new InMemoryFileSystem();
        var writer = new SafeFileWriter(fs);

        await writer.WriteAsync("/nested/deep/out.txt", async stream =>
        {
            await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes("x"));
        });

        fs.DirectoryExists("/nested/deep").Should().BeTrue();
    }
}
```

- [ ] **Step 2: Red.**

- [ ] **Step 3: Write implementation**

```csharp
namespace RimworldExtractor.Infrastructure.FileSystem;

/// <summary>
/// Writes a file atomically: content is streamed to <c>path.tmp</c>, then <see cref="IFileSystem.Replace"/>
/// (or <see cref="IFileSystem.Move"/> when the target is absent) promotes it. A crash mid-write leaves
/// only the orphan temp file; the target is never partially written.
/// </summary>
public sealed class SafeFileWriter
{
    private readonly IFileSystem _fs;

    public SafeFileWriter(IFileSystem fs) => _fs = fs;

    /// <param name="targetPath">Final destination path.</param>
    /// <param name="write">Callback that writes payload bytes to the provided stream.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    public async Task WriteAsync(string targetPath, Func<Stream, Task> write, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory) && !_fs.DirectoryExists(directory))
            _fs.CreateDirectory(directory);

        var tempPath = targetPath + ".tmp";
        var backupPath = targetPath + ".bak";

        await using (var stream = _fs.CreateFile(tempPath))
        {
            await write(stream);
        }

        if (_fs.FileExists(targetPath))
            _fs.Replace(tempPath, targetPath, backupPath);
        else
            _fs.Move(tempPath, targetPath);

        cancellationToken.ThrowIfCancellationRequested();
    }
}
```

- [ ] **Step 4: Pass.**

- [ ] **Step 5: Commit**
```bash
git add src/RimworldExtractor.Infrastructure/FileSystem/SafeFileWriter.cs \
        tests/RimworldExtractor.Infrastructure.Tests/FileSystem/SafeFileWriterTests.cs
git commit -m "feat(infrastructure): add SafeFileWriter (atomic temp→replace pattern)"
```

### Task 6: Refactor JsonSettingsStore to use SafeFileWriter + IFileSystem

**Rationale:** Phase 2 JsonSettingsStore uses `File.*` directly. Migrate it to `IFileSystem` + `SafeFileWriter` so both get consistent semantics. Phase 2 tests must still pass.

**Files:**
- Modify: `src/RimworldExtractor.Infrastructure/Settings/JsonSettingsStore.cs`
- Modify: `src/RimworldExtractor.Infrastructure/DependencyInjection.cs` (inject IFileSystem)
- Modify: `tests/RimworldExtractor.Infrastructure.Tests/Settings/JsonSettingsStoreTests.cs` (accept IFileSystem in ctor)
- Modify: `tests/RimworldExtractor.Infrastructure.Tests/DependencyInjectionTests.cs` (if it tested construction)

- [ ] **Step 1: Update `JsonSettingsStore`** to this (replace existing file contents):

```csharp
using System.Text.Json;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Settings;

public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly IFileSystem _fs;
    private readonly SafeFileWriter _safeWriter;
    private readonly string _path;

    public JsonSettingsStore(IFileSystem fs, string path)
    {
        _fs = fs ?? throw new ArgumentNullException(nameof(fs));
        _path = path ?? throw new ArgumentNullException(nameof(path));
        _safeWriter = new SafeFileWriter(fs);
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!_fs.FileExists(_path)) return AppSettings.Default;

        await using var stream = _fs.OpenRead(_path);
        var loaded = await JsonSerializer.DeserializeAsync(
            stream,
            AppSettingsJsonContext.Default.AppSettings,
            cancellationToken);
        return loaded ?? AppSettings.Default;
    }

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        return _safeWriter.WriteAsync(_path, async stream =>
        {
            await JsonSerializer.SerializeAsync(
                stream,
                settings,
                AppSettingsJsonContext.Default.AppSettings,
                cancellationToken);
        }, cancellationToken);
    }
}
```

- [ ] **Step 2: Update DI extension** — `src/RimworldExtractor.Infrastructure/DependencyInjection.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Settings;

namespace RimworldExtractor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string settingsPath)
    {
        services.AddSingleton<IFileSystem, PhysicalFileSystem>();
        services.AddSingleton<ISettingsStore>(sp =>
            new JsonSettingsStore(sp.GetRequiredService<IFileSystem>(), settingsPath));
        return services;
    }
}
```

- [ ] **Step 3: Update JsonSettingsStoreTests** — construct with `new PhysicalFileSystem()` as first arg (tests still use real temp directory):

```csharp
// ... in JsonSettingsStoreTests, replace every `new JsonSettingsStore(_settingsPath)` with:
var store = new JsonSettingsStore(new PhysicalFileSystem(), _settingsPath);
```

Apply to all 5 tests in the file.

- [ ] **Step 4: Run full test suite**
```bash
dotnet test RimworldExtractor.slnx -c Release 2>&1 | grep -E "Passed|Failed"
```
Expected: all tests pass. JsonSettingsStoreTests: still 5 passed. DependencyInjectionTests: still 1 passed.

- [ ] **Step 5: Commit**
```bash
git add src/RimworldExtractor.Infrastructure/Settings/JsonSettingsStore.cs \
        src/RimworldExtractor.Infrastructure/DependencyInjection.cs \
        tests/RimworldExtractor.Infrastructure.Tests/Settings/JsonSettingsStoreTests.cs
git commit -m "refactor(infrastructure): JsonSettingsStore uses IFileSystem + SafeFileWriter"
```

### Task 7: IConflictResolver abstraction + decision enum

**Files:**
- Create: `src/RimworldExtractor.Domain/Abstractions/IConflictResolver.cs`

**Rationale:** Legacy `Prefabs.StopCallbackXlsx/Xml/Txt` delegates are replaced by a proper domain abstraction. UI and CLI will inject different implementations.

- [ ] **Step 1: Write the file**

```csharp
namespace RimworldExtractor.Domain.Abstractions;

/// <summary>What to do when a target file already exists.</summary>
public enum ConflictDecision
{
    /// <summary>Overwrite the existing file with the new content.</summary>
    Overwrite = 0,
    /// <summary>Keep the existing file, drop the new content.</summary>
    KeepOriginal = 1,
    /// <summary>Abort the entire write operation.</summary>
    Abort = 2,
}

/// <summary>Information about a pending file-write conflict.</summary>
/// <param name="TargetPath">Absolute path of the conflicting file.</param>
/// <param name="FileKind">Human-readable file type (e.g. "XLSX", "XML", "TXT").</param>
public sealed record ConflictContext(string TargetPath, string FileKind);

/// <summary>
/// Decides what to do when a file-write would overwrite an existing file. Implementations:
/// <see cref="RimworldExtractor.Infrastructure.Output.PolicyBasedConflictResolver"/> (non-interactive),
/// <see cref="RimworldExtractor.Ui.Avalonia.Services.InteractiveConflictResolver"/> (prompts the user).
/// </summary>
public interface IConflictResolver
{
    Task<ConflictDecision> ResolveAsync(ConflictContext context, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Build**
```bash
dotnet build src/RimworldExtractor.Domain -c Release 2>&1 | tail -3
```
Expected: 0W/0E.

- [ ] **Step 3: Commit**
```bash
git add src/RimworldExtractor.Domain/Abstractions/IConflictResolver.cs
git commit -m "feat(domain): add IConflictResolver abstraction (replaces Prefabs.StopCallback)"
```

### Task 8: PolicyBasedConflictResolver

**Files:**
- Create: `src/RimworldExtractor.Infrastructure/Output/PolicyBasedConflictResolver.cs`
- Test: `tests/RimworldExtractor.Infrastructure.Tests/Output/PolicyBasedConflictResolverTests.cs`

Maps `DuplicatesPolicy` (from Phase 2) to `ConflictDecision`: `Overwrite → Overwrite`, `KeepOriginal → KeepOriginal`, `Stop → Abort`.

- [ ] **Step 1: Write failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Infrastructure.Output;

namespace RimworldExtractor.Infrastructure.Tests.Output;

public class PolicyBasedConflictResolverTests
{
    [Theory]
    [InlineData(DuplicatesPolicy.Overwrite, ConflictDecision.Overwrite)]
    [InlineData(DuplicatesPolicy.KeepOriginal, ConflictDecision.KeepOriginal)]
    [InlineData(DuplicatesPolicy.Stop, ConflictDecision.Abort)]
    public async Task ResolveAsync_MapsPolicyToDecision(DuplicatesPolicy policy, ConflictDecision expected)
    {
        var resolver = new PolicyBasedConflictResolver(policy);

        var decision = await resolver.ResolveAsync(new ConflictContext("/some/file.xml", "XML"));

        decision.Should().Be(expected);
    }
}
```

- [ ] **Step 2: Red.**

- [ ] **Step 3: Implementation**

```csharp
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Enums;

namespace RimworldExtractor.Infrastructure.Output;

/// <summary>
/// Non-interactive <see cref="IConflictResolver"/> that always returns the fixed decision
/// implied by a <see cref="DuplicatesPolicy"/>. Suitable for CLI and tests.
/// </summary>
public sealed class PolicyBasedConflictResolver : IConflictResolver
{
    private readonly DuplicatesPolicy _policy;

    public PolicyBasedConflictResolver(DuplicatesPolicy policy) => _policy = policy;

    public Task<ConflictDecision> ResolveAsync(ConflictContext context, CancellationToken cancellationToken = default)
    {
        var decision = _policy switch
        {
            DuplicatesPolicy.Overwrite => ConflictDecision.Overwrite,
            DuplicatesPolicy.KeepOriginal => ConflictDecision.KeepOriginal,
            DuplicatesPolicy.Stop => ConflictDecision.Abort,
            _ => throw new ArgumentOutOfRangeException(nameof(_policy), _policy, "Unknown DuplicatesPolicy"),
        };
        return Task.FromResult(decision);
    }
}
```

- [ ] **Step 4: Pass.**

- [ ] **Step 5: Commit**
```bash
git add src/RimworldExtractor.Infrastructure/Output/PolicyBasedConflictResolver.cs \
        tests/RimworldExtractor.Infrastructure.Tests/Output/PolicyBasedConflictResolverTests.cs
git commit -m "feat(infrastructure): add PolicyBasedConflictResolver (policy → decision)"
```

---

## Group 3C — Mod Discovery

### Task 9: IModLister interface

**Files:**
- Create: `src/RimworldExtractor.Domain/Abstractions/IModLister.cs`

- [ ] **Step 1: Write the file**

```csharp
using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Domain.Abstractions;

/// <summary>
/// Discovers mods on disk and provides metadata + extractable folder enumeration.
/// </summary>
public interface IModLister
{
    /// <summary>All mods across configured roots (Rimworld/Data, Mods, Workshop). Deterministic order.</summary>
    IReadOnlyList<ModMetadata> DiscoverAll();

    /// <summary>Parses a single mod root directory into a <see cref="ModMetadata"/>. Returns null if the directory is not a mod.</summary>
    ModMetadata? ReadMetadata(string modRoot);

    /// <summary>Lists the Defs/Keyed/Strings/Patches folders a mod exposes, including version subdirs and LoadFolders.xml resolution.</summary>
    IReadOnlyList<ExtractableFolder> GetExtractableFolders(ModMetadata mod);

    /// <summary>Finds the transitive closure of reference mods (for Defs inheritance resolution).</summary>
    IReadOnlyList<ModMetadata> FindReferenceMods(ModMetadata target);
}
```

- [ ] **Step 2: Build + commit**
```bash
dotnet build src/RimworldExtractor.Domain -c Release 2>&1 | tail -3
git add src/RimworldExtractor.Domain/Abstractions/IModLister.cs
git commit -m "feat(domain): add IModLister abstraction"
```

### Task 10: FileSystemModLister — ReadMetadata (About.xml parsing)

**Legacy ref:** `ModLister.cs:84-155` — parses About.xml for `packageId`, `name`, author "Ludeon Studios" → IsOfficialContent, modDependencies (+ modDependenciesByVersion), PublishedFileId.txt.

**Files:**
- Create: `src/RimworldExtractor.Infrastructure/FileSystem/FileSystemModLister.cs`
- Test: `tests/RimworldExtractor.Infrastructure.Tests/FileSystem/FileSystemModListerReadMetadataTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using System.Xml.Linq;
using FluentAssertions;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Tests.FileSystem;

public class FileSystemModListerReadMetadataTests
{
    private static InMemoryFileSystem FsWithAbout(string modRoot, string aboutXml)
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile($"{modRoot}/About/About.xml", aboutXml);
        return fs;
    }

    [Fact]
    public void ReadMetadata_OfficialContent_MarksIsOfficial()
    {
        var fs = FsWithAbout("/rw/Data/Core", """
            <?xml version="1.0" encoding="utf-8"?>
            <ModMetaData>
              <name>Core</name>
              <packageId>Ludeon.RimWorld</packageId>
              <author>Ludeon Studios</author>
            </ModMetaData>
            """);
        var lister = new FileSystemModLister(fs, currentVersion: "1.6",
            rimworldDir: "/rw", workshopDir: "/ws");

        var meta = lister.ReadMetadata("/rw/Data/Core");

        meta.Should().NotBeNull();
        meta!.IsOfficialContent.Should().BeTrue();
        meta.PackageId.Should().Be("Ludeon.RimWorld");
        meta.ModName.Should().Be("Core");
        meta.Id.Should().Be("Official");
    }

    [Fact]
    public void ReadMetadata_Unofficial_WithPublishedFileId()
    {
        var fs = FsWithAbout("/ws/2997308585", """
            <?xml version="1.0" encoding="utf-8"?>
            <ModMetaData>
              <name>My Mod</name>
              <packageId>author.mymod</packageId>
              <author>Author</author>
            </ModMetaData>
            """);
        fs.AddFile("/ws/2997308585/About/PublishedFileId.txt", "2997308585");
        var lister = new FileSystemModLister(fs, currentVersion: "1.6",
            rimworldDir: "/rw", workshopDir: "/ws");

        var meta = lister.ReadMetadata("/ws/2997308585");

        meta.Should().NotBeNull();
        meta!.IsOfficialContent.Should().BeFalse();
        meta.Id.Should().Be("2997308585");
    }

    [Fact]
    public void ReadMetadata_WithModDependencies_ParsesThem()
    {
        var fs = FsWithAbout("/mods/Foo", """
            <?xml version="1.0" encoding="utf-8"?>
            <ModMetaData>
              <name>Foo</name>
              <packageId>a.foo</packageId>
              <modDependencies>
                <li><packageId>Ludeon.RimWorld</packageId></li>
                <li><packageId>author.bar</packageId></li>
              </modDependencies>
            </ModMetaData>
            """);
        var lister = new FileSystemModLister(fs, currentVersion: "1.6",
            rimworldDir: "/rw", workshopDir: "/ws");

        var meta = lister.ReadMetadata("/mods/Foo");

        meta!.ModDependencies.Should().NotBeNull();
        meta.ModDependencies!.Should().BeEquivalentTo(new[] { "Ludeon.RimWorld", "author.bar" });
    }

    [Fact]
    public void ReadMetadata_MissingAbout_ReturnsNull()
    {
        var fs = new InMemoryFileSystem();
        var lister = new FileSystemModLister(fs, currentVersion: "1.6",
            rimworldDir: "/rw", workshopDir: "/ws");

        lister.ReadMetadata("/mods/Missing").Should().BeNull();
    }
}
```

- [ ] **Step 2: Red.**

- [ ] **Step 3: Write implementation skeleton** — `src/RimworldExtractor.Infrastructure/FileSystem/FileSystemModLister.cs`:

Read `legacy/RimworldExtractorInternal/ModLister.cs:84-155` for the exact parsing logic. Port with these changes:
- `XmlDocument` → `XDocument`
- `doc.DocumentElement?["packageId"]?.InnerText` → `doc.Root?.Element("packageId")?.Value`
- Use `IFileSystem` for file existence + reading (but note: XDocument loads from a file path directly with `XDocument.Load(path)`, which uses `File.*`. Since we want testability via InMemoryFileSystem, LOAD XML from `_fs.ReadAllTextAsync(path)` text first, then `XDocument.Parse(text)`.)
- Return `ModMetadata` record (existing Phase 2 type) with `IReadOnlyList<string>` for dependencies.
- `GetExtractableFolders` and `DiscoverAll` are separate tasks — leave stubs that throw `NotImplementedException` for now.

```csharp
using System.Xml.Linq;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Infrastructure.FileSystem;

/// <summary>
/// Disk-backed <see cref="IModLister"/> implementation. Reads <c>About/About.xml</c>,
/// detects official content via author = "Ludeon Studios", parses modDependencies
/// (including <c>modDependenciesByVersion</c>), and enumerates extractable folders
/// including <c>LoadFolders.xml</c> and version subdirectory resolution.
/// </summary>
public sealed class FileSystemModLister : IModLister
{
    private const string OfficialContentAuthor = "Ludeon Studios";
    private const string OfficialId = "Official";

    private readonly IFileSystem _fs;
    private readonly string _currentVersion;
    private readonly string _rimworldDir;
    private readonly string _workshopDir;

    public FileSystemModLister(IFileSystem fs, string currentVersion, string rimworldDir, string workshopDir)
    {
        _fs = fs;
        _currentVersion = currentVersion;
        _rimworldDir = rimworldDir;
        _workshopDir = workshopDir;
    }

    public ModMetadata? ReadMetadata(string modRoot)
    {
        var aboutPath = Path.Combine(modRoot, "About", "About.xml").Replace('\\', '/');
        if (!_fs.FileExists(aboutPath)) return null;

        var text = _fs.ReadAllTextAsync(aboutPath).GetAwaiter().GetResult();
        XDocument doc;
        try { doc = XDocument.Parse(text); }
        catch { return null; }

        var root = doc.Root;
        if (root is null) return null;

        var packageId = root.Element("packageId")?.Value ?? "UNKNOWN";
        var name = root.Element("name")?.Value ?? "UNKNOWN";
        var author = root.Element("author")?.Value;
        var isOfficial = author == OfficialContentAuthor;

        if (isOfficial && name == "UNKNOWN")
            name = Path.GetFileName(modRoot.TrimEnd('/', '\\'));

        var dependencies = new List<string>();
        var modDeps = root.Element("modDependencies");
        if (modDeps is not null)
            foreach (var li in modDeps.Elements("li"))
                if (li.Element("packageId") is { } pid) dependencies.Add(pid.Value);

        var modDepsByVersion = root.Element("modDependenciesByVersion");
        if (modDepsByVersion is not null)
        {
            var versionElement = modDepsByVersion.Element("v" + _currentVersion)
                ?? modDepsByVersion.Elements().LastOrDefault();
            if (versionElement is not null)
                foreach (var li in versionElement.Elements("li"))
                    if (li.Element("packageId") is { } pid) dependencies.Add(pid.Value);
        }

        dependencies = dependencies.Distinct().ToList();

        string id;
        if (isOfficial)
        {
            id = OfficialId;
        }
        else
        {
            var publishedFileIdPath = Path.Combine(modRoot, "About", "PublishedFileId.txt").Replace('\\', '/');
            if (_fs.FileExists(publishedFileIdPath))
            {
                id = _fs.ReadAllTextAsync(publishedFileIdPath).GetAwaiter().GetResult().Trim();
            }
            else if (modRoot.Replace('\\', '/').Contains("workshop/content/294100"))
            {
                id = Path.GetFileName(modRoot.TrimEnd('/', '\\'));
            }
            else
            {
                id = ModMetadata.UnknownId;
            }
        }

        return new ModMetadata(modRoot, id, name, packageId, isOfficial, dependencies);
    }

    public IReadOnlyList<ModMetadata> DiscoverAll() => throw new NotImplementedException("Task 12");
    public IReadOnlyList<ExtractableFolder> GetExtractableFolders(ModMetadata mod) => throw new NotImplementedException("Task 11");
    public IReadOnlyList<ModMetadata> FindReferenceMods(ModMetadata target) => throw new NotImplementedException("Task 12");
}
```

Note: `.GetAwaiter().GetResult()` is a sync-over-async antipattern. `IModLister` is sync for simpler callers; if this becomes a perf problem in Phase 4, we can reconsider. Document the trade-off.

- [ ] **Step 4: Pass.**

- [ ] **Step 5: Commit**
```bash
git add src/RimworldExtractor.Infrastructure/FileSystem/FileSystemModLister.cs \
        tests/RimworldExtractor.Infrastructure.Tests/FileSystem/FileSystemModListerReadMetadataTests.cs
git commit -m "feat(infrastructure): add FileSystemModLister.ReadMetadata (About.xml parse)"
```

### Task 11: FileSystemModLister — GetExtractableFolders

**Legacy ref:** `ModLister.cs:157-232` — discovers Defs/Keyed/Strings/Patches folders (including under `Languages/<OriginalLang>/Keyed` and `Languages/<OriginalLang>/Strings`), handles `LoadFolders.xml`, version subdirs (matching `^[1]\.\d+` regex), `Common/` dir.

**Files:**
- Modify: `src/RimworldExtractor.Infrastructure/FileSystem/FileSystemModLister.cs`
- Test: `tests/RimworldExtractor.Infrastructure.Tests/FileSystem/FileSystemModListerExtractableFoldersTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Tests.FileSystem;

public class FileSystemModListerExtractableFoldersTests
{
    private static FileSystemModLister Lister(InMemoryFileSystem fs) =>
        new(fs, currentVersion: "1.6", rimworldDir: "/rw", workshopDir: "/ws");

    [Fact]
    public void GetExtractableFolders_WithoutLoadFolders_FindsTopLevelDirs()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/mods/Foo/About/About.xml", "<ModMetaData><packageId>a.b</packageId><name>F</name></ModMetaData>");
        fs.AddFile("/mods/Foo/Defs/ThingDefs.xml", "<Defs/>");
        fs.AddFile("/mods/Foo/Patches/p.xml", "<Patch/>");
        var lister = Lister(fs);
        var mod = lister.ReadMetadata("/mods/Foo")!;

        var folders = lister.GetExtractableFolders(mod);

        var folderNames = folders.Select(f => f.FolderName).ToHashSet();
        folderNames.Should().Contain("Defs");
        folderNames.Should().Contain("Patches");
    }

    [Fact]
    public void GetExtractableFolders_WithVersionSubdir_AddsVersionedFolders()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/mods/Foo/About/About.xml", "<ModMetaData><packageId>a.b</packageId><name>F</name></ModMetaData>");
        fs.AddFile("/mods/Foo/1.6/Defs/ThingDefs.xml", "<Defs/>");
        fs.AddFile("/mods/Foo/Common/Defs/Shared.xml", "<Defs/>");
        var lister = Lister(fs);
        var mod = lister.ReadMetadata("/mods/Foo")!;

        var folders = lister.GetExtractableFolders(mod);

        folders.Should().Contain(f => f.FolderName.Contains("1.6") && f.FolderName.Contains("Defs") && f.VersionInfo == "1.6");
        folders.Should().Contain(f => f.FolderName.Contains("Common") && f.FolderName.Contains("Defs") && f.VersionInfo == "Common");
    }

    [Fact]
    public void GetExtractableFolders_WithLoadFolders_RespectsLoadFoldersXml()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/mods/Foo/About/About.xml", "<ModMetaData><packageId>a.b</packageId><name>F</name></ModMetaData>");
        fs.AddFile("/mods/Foo/LoadFolders.xml", """
            <loadFolders>
              <v1.6>
                <li>extra</li>
                <li IfModActive="dep.x">conditional</li>
              </v1.6>
            </loadFolders>
            """);
        fs.AddFile("/mods/Foo/extra/Defs/Things.xml", "<Defs/>");
        fs.AddFile("/mods/Foo/conditional/Defs/Cond.xml", "<Defs/>");
        var lister = Lister(fs);
        var mod = lister.ReadMetadata("/mods/Foo")!;

        var folders = lister.GetExtractableFolders(mod);

        folders.Should().Contain(f => f.FolderName.Contains("extra") && f.VersionInfo == "1.6");
        folders.Should().Contain(f => f.FolderName.Contains("conditional") && f.RequiredPackageId == "dep.x");
    }
}
```

- [ ] **Step 2: Red.**

- [ ] **Step 3: Implementation** — replace the `GetExtractableFolders` method in `FileSystemModLister.cs` with a port of `legacy/ModLister.cs:157-232`. Use `IFileSystem` for all IO. The method should:
  1. Define `targetFolders` array: `Defs`, `Patches`, `Keyed`, `Languages/<original>/Keyed`, `Languages/<original-base>/Keyed`, `Languages/<original>/Strings`, `Languages/<original-base>/Strings` — where `<original-base>` is the language name with the parenthetical stripped. For Phase 3 minimum, use hardcoded `"English"` since that's the legacy default; Phase 4 will parameterize via AppSettings.
  2. A local `GetExtractableFoldersInternal(path)` helper that yields relative paths for each target folder that exists under `path`.
  3. If `LoadFolders.xml` exists at root, parse it as XDocument and iterate `<v1.6>`, `<v1.5>`, etc. For each `<li>`, read `IfModActive` attribute for `RequiredPackageId` and inner text for the folder segment. Use element name (after stripping leading `v`) as `VersionInfo`.
  4. Else, enumerate root directories: if name matches `^1\.\d+$` → scan for extractable folders with `VersionInfo = lastDir`; if name == "Common" → scan with `VersionInfo = "Common"`. Also scan root itself with `VersionInfo = "default"`.
  5. Use a `HashSet` with `ExtractableFolderComparer`-equivalent to dedupe by folder name.

Constructor should accept original language name — update ctor:

```csharp
public FileSystemModLister(IFileSystem fs, string currentVersion, string originalLanguageFolderName, string rimworldDir, string workshopDir)
{ ... }
```

Update the Task 10 tests and the new ones to pass `originalLanguageFolderName: "English"`.

Because this is a ~80-line method with regex + XDocument parsing, the implementer should port faithfully from legacy and add inline comments referencing legacy line numbers where behavior is non-obvious.

- [ ] **Step 4: Pass.**

- [ ] **Step 5: Commit**
```bash
git add src/RimworldExtractor.Infrastructure/FileSystem/FileSystemModLister.cs \
        tests/RimworldExtractor.Infrastructure.Tests/FileSystem/FileSystemModListerReadMetadataTests.cs \
        tests/RimworldExtractor.Infrastructure.Tests/FileSystem/FileSystemModListerExtractableFoldersTests.cs
git commit -m "feat(infrastructure): FileSystemModLister.GetExtractableFolders (LoadFolders + version dirs)"
```

### Task 12: FileSystemModLister — DiscoverAll + FindReferenceMods

**Legacy ref:** `ModLister.cs:14-82` (directory enumeration) + `ModLister.cs:234-292` (transitive closure of dependencies via packageId lookup).

**Files:**
- Modify: `src/RimworldExtractor.Infrastructure/FileSystem/FileSystemModLister.cs`
- Test: `tests/RimworldExtractor.Infrastructure.Tests/FileSystem/FileSystemModListerDiscoveryTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using FluentAssertions;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Tests.FileSystem;

public class FileSystemModListerDiscoveryTests
{
    [Fact]
    public void DiscoverAll_EnumeratesOfficialLocalAndWorkshop()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/rw/Data/Core/About/About.xml", "<ModMetaData><packageId>Ludeon.RimWorld</packageId><name>Core</name><author>Ludeon Studios</author></ModMetaData>");
        fs.AddFile("/rw/Mods/Local/About/About.xml", "<ModMetaData><packageId>local.mod</packageId><name>Local</name></ModMetaData>");
        fs.AddFile("/ws/123/About/About.xml", "<ModMetaData><packageId>ws.mod</packageId><name>WS</name></ModMetaData>");
        var lister = new FileSystemModLister(fs, currentVersion: "1.6",
            originalLanguageFolderName: "English",
            rimworldDir: "/rw", workshopDir: "/ws");

        var all = lister.DiscoverAll();

        all.Should().HaveCount(3);
        all.Should().Contain(m => m.PackageId == "Ludeon.RimWorld" && m.IsOfficialContent);
        all.Should().Contain(m => m.PackageId == "local.mod");
        all.Should().Contain(m => m.PackageId == "ws.mod");
    }

    [Fact]
    public void FindReferenceMods_ReturnsTransitiveDependencies()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/rw/Data/Core/About/About.xml", "<ModMetaData><packageId>Ludeon.RimWorld</packageId><name>Core</name><author>Ludeon Studios</author></ModMetaData>");
        fs.AddFile("/rw/Mods/A/About/About.xml", "<ModMetaData><packageId>a.mod</packageId><name>A</name><modDependencies><li><packageId>b.mod</packageId></li></modDependencies></ModMetaData>");
        fs.AddFile("/rw/Mods/B/About/About.xml", "<ModMetaData><packageId>b.mod</packageId><name>B</name></ModMetaData>");
        var lister = new FileSystemModLister(fs, currentVersion: "1.6",
            originalLanguageFolderName: "English",
            rimworldDir: "/rw", workshopDir: "/ws");
        var modA = lister.DiscoverAll().First(m => m.PackageId == "a.mod");

        var refs = lister.FindReferenceMods(modA);

        refs.Select(m => m.PackageId).Should().Contain("Ludeon.RimWorld");
        refs.Select(m => m.PackageId).Should().Contain("b.mod");
    }
}
```

- [ ] **Step 2: Red.**

- [ ] **Step 3: Implementation** — add methods to `FileSystemModLister`:

`DiscoverAll`:
1. Official mods under `{rimworldDir}/Data/*/`
2. Local mods under `{rimworldDir}/Mods/*/`
3. Workshop mods under `{workshopDir}/*/`
4. Call `ReadMetadata` for each, skip nulls, dedupe by `RootDir`.

`FindReferenceMods`:
1. All official mods (excluding target).
2. Recursively walk `ModDependencies` resolving by `PackageId` (case-insensitive via `PackageId.Normalized`).
3. Use a visited-set to prevent infinite loops.
4. Return deterministic order (by PackageId ordinal).

- [ ] **Step 4: Pass.**

- [ ] **Step 5: Commit**
```bash
git add src/RimworldExtractor.Infrastructure/FileSystem/FileSystemModLister.cs \
        tests/RimworldExtractor.Infrastructure.Tests/FileSystem/FileSystemModListerDiscoveryTests.cs
git commit -m "feat(infrastructure): FileSystemModLister.DiscoverAll + FindReferenceMods"
```

---

## Group 3D — XML Def Parsing + Inheritance

### Task 13: XmlHelpers extensions (XDocument / XElement conveniences)

**Legacy ref:** `Utils.cs:37-208` — `AppendElement`, `AppendAttribute`, `IsListNode`, `IsTextNode`, `TryGetAttribute`, `GetXpath`. All `XmlNode` API — we rewrite for `XElement`.

**Files:**
- Create: `src/RimworldExtractor.Infrastructure/Xml/XmlHelpers.cs`
- Test: `tests/RimworldExtractor.Infrastructure.Tests/Xml/XmlHelpersTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using System.Xml.Linq;
using FluentAssertions;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Infrastructure.Tests.Xml;

public class XmlHelpersTests
{
    [Fact]
    public void IsListNode_True_ForLiElement()
    {
        var el = new XElement("li", "x");
        el.IsListNode().Should().BeTrue();
    }

    [Fact]
    public void IsListNode_False_ForNonLi()
    {
        new XElement("label", "x").IsListNode().Should().BeFalse();
    }

    [Fact]
    public void IsTextNode_True_ForSingleTextChild()
    {
        new XElement("label", "some text").IsTextNode().Should().BeTrue();
    }

    [Fact]
    public void IsTextNode_False_ForElementChildren()
    {
        var parent = new XElement("parent", new XElement("child"));
        parent.IsTextNode().Should().BeFalse();
    }

    [Fact]
    public void GetIdxOfListNode_ReturnsPositionWithinSiblings()
    {
        var parent = new XElement("list",
            new XElement("li", "a"),
            new XElement("li", "b"),
            new XElement("li", "c"));
        var third = parent.Elements().ElementAt(2);

        third.GetIdxOfListNode().Should().Be(2);
    }

    [Theory]
    [InlineData("ThingDef", "Wooden.label", "/Defs/ThingDef[defName=\"Wooden\"]/label")]
    [InlineData("ThingDef", "Wooden.verbs.0.label", "/Defs/ThingDef[defName=\"Wooden\"]/verbs/li[1]/label")]
    public void BuildXPath_MatchesLegacyFormat(string className, string nodeName, string expected)
    {
        XmlHelpers.BuildXPath(className, nodeName).Should().Be(expected);
    }
}
```

- [ ] **Step 2: Red.**

- [ ] **Step 3: Implementation**

```csharp
using System.Xml.Linq;

namespace RimworldExtractor.Infrastructure.Xml;

/// <summary>
/// Static helpers for navigating and transforming <see cref="XElement"/> trees in
/// RimWorld Def parsing.
/// </summary>
public static class XmlHelpers
{
    /// <summary>True when the element is named <c>li</c> (RimWorld list item).</summary>
    public static bool IsListNode(this XElement element) => element.Name.LocalName == "li";

    /// <summary>True when the element has exactly one child of type <see cref="XText"/> or <see cref="XCData"/>.</summary>
    public static bool IsTextNode(this XElement element)
    {
        using var enumerator = element.Nodes().GetEnumerator();
        if (!enumerator.MoveNext()) return false;
        var first = enumerator.Current;
        if (enumerator.MoveNext()) return false;
        return first is XText or XCData;
    }

    /// <summary>Returns the 0-based index of <paramref name="node"/> among its element-type siblings.</summary>
    public static int GetIdxOfListNode(this XElement node)
    {
        var parent = node.Parent ?? throw new InvalidOperationException("Node has no parent.");
        int i = 0;
        foreach (var sibling in parent.Elements())
        {
            if (sibling == node) return i;
            i++;
        }
        throw new InvalidOperationException("Node not found among parent's children.");
    }

    /// <summary>
    /// Reconstructs the XPath that legacy <c>Utils.GetXpath</c> produces, used by Patch XML emission.
    /// Format: <c>/Defs/{className}[defName="{defName}"]/{segments...}</c> with:
    /// <list type="bullet">
    ///   <item>numeric segments <c>N</c> → <c>li[N+1]</c></item>
    ///   <item>uppercase-first segments → <c>*[.//*[contains(text(), '{seg}')]]</c> (translation-handle path)</item>
    ///   <item>lowercase-first segments → as-is (plain element name)</item>
    /// </list>
    /// </summary>
    public static string BuildXPath(string className, string nodeName)
    {
        var defName = nodeName.Split('.')[0];
        var tokens = nodeName[(defName.Length + 1)..].Split('.');
        for (int i = 0; i < tokens.Length; i++)
        {
            if (int.TryParse(tokens[i], out var k))
                tokens[i] = $"li[{k + 1}]";
            else if (tokens[i].Length > 0 && !char.IsLower(tokens[i][0]))
                tokens[i] = $"*[.//*[contains(text(), '{tokens[i]}')]]";
        }
        return $"/Defs/{className}[defName=\"{defName}\"]/" + string.Join('/', tokens);
    }
}
```

- [ ] **Step 4: Pass.**

- [ ] **Step 5: Commit**
```bash
git add src/RimworldExtractor.Infrastructure/Xml/XmlHelpers.cs \
        tests/RimworldExtractor.Infrastructure.Tests/Xml/XmlHelpersTests.cs
git commit -m "feat(infrastructure): add XmlHelpers (XDocument/XElement ported from legacy Utils)"
```

### Task 14: IXmlDefParser interface + XDocumentDefParser

**Legacy ref:** `IO.cs:861-875` `ReadXml` + `Extractor.DefsUtils.cs:10-52` `LoadReferenceDefs`. The modern parser loads a Def file into an XDocument, respecting the same XmlReader settings (ignore comments/whitespace, checkCharacters=false).

**Files:**
- Create: `src/RimworldExtractor.Infrastructure/Xml/IXmlDefParser.cs`
- Create: `src/RimworldExtractor.Infrastructure/Xml/XDocumentDefParser.cs`
- Test: `tests/RimworldExtractor.Infrastructure.Tests/Xml/XDocumentDefParserTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using System.Xml.Linq;
using FluentAssertions;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Infrastructure.Tests.Xml;

public class XDocumentDefParserTests
{
    [Fact]
    public async Task ParseAsync_ReturnsXDocumentWithDefsRoot()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/mods/Foo/Defs/T.xml", """
            <?xml version="1.0" encoding="utf-8"?>
            <Defs>
              <ThingDef>
                <defName>Spear</defName>
                <label>spear</label>
              </ThingDef>
            </Defs>
            """);
        var parser = new XDocumentDefParser(fs);

        var doc = await parser.ParseAsync("/mods/Foo/Defs/T.xml");

        doc.Root!.Name.LocalName.Should().Be("Defs");
        doc.Root.Elements("ThingDef").Should().HaveCount(1);
    }

    [Fact]
    public async Task ParseAsync_IgnoresComments()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/a.xml", """
            <Defs>
              <!-- this is a comment -->
              <ThingDef><defName>X</defName></ThingDef>
            </Defs>
            """);
        var parser = new XDocumentDefParser(fs);

        var doc = await parser.ParseAsync("/a.xml");

        doc.Root!.Nodes().OfType<XComment>().Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_OnInvalidXml_Throws()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/bad.xml", "<<<not xml");
        var parser = new XDocumentDefParser(fs);

        var act = () => parser.ParseAsync("/bad.xml");

        await act.Should().ThrowAsync<System.Xml.XmlException>();
    }
}
```

- [ ] **Step 2: Red.**

- [ ] **Step 3: Write interface + implementation**

`src/RimworldExtractor.Infrastructure/Xml/IXmlDefParser.cs`:

```csharp
using System.Xml.Linq;

namespace RimworldExtractor.Infrastructure.Xml;

/// <summary>Parses a single RimWorld Def XML file into an <see cref="XDocument"/>.</summary>
public interface IXmlDefParser
{
    Task<XDocument> ParseAsync(string filePath, CancellationToken cancellationToken = default);
}
```

`src/RimworldExtractor.Infrastructure/Xml/XDocumentDefParser.cs`:

```csharp
using System.Xml;
using System.Xml.Linq;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Xml;

/// <summary>
/// Reads a Def XML file through an <see cref="IFileSystem"/> and parses it with XDocument.
/// Legacy ReadXml (IO.cs:861-875) settings preserved: <c>IgnoreComments=true</c>,
/// <c>IgnoreWhitespace=true</c>, <c>CheckCharacters=false</c>.
/// </summary>
public sealed class XDocumentDefParser : IXmlDefParser
{
    private static readonly XmlReaderSettings ReaderSettings = new()
    {
        IgnoreComments = true,
        IgnoreWhitespace = true,
        CheckCharacters = false,
        Async = true,
    };

    private readonly IFileSystem _fs;

    public XDocumentDefParser(IFileSystem fs) => _fs = fs;

    public async Task<XDocument> ParseAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var text = await _fs.ReadAllTextAsync(filePath, cancellationToken);
        using var stringReader = new StringReader(text);
        using var xmlReader = XmlReader.Create(stringReader, ReaderSettings);
        return await XDocument.LoadAsync(xmlReader, LoadOptions.None, cancellationToken);
    }
}
```

- [ ] **Step 4: Pass.**

- [ ] **Step 5: Commit**
```bash
git add src/RimworldExtractor.Infrastructure/Xml/IXmlDefParser.cs \
        src/RimworldExtractor.Infrastructure/Xml/XDocumentDefParser.cs \
        tests/RimworldExtractor.Infrastructure.Tests/Xml/XDocumentDefParserTests.cs
git commit -m "feat(infrastructure): add IXmlDefParser + XDocumentDefParser (ReadXml port)"
```

### Task 15: IXmlInheritanceResolver interface + XmlInheritanceResolver

**Legacy ref:** `Extractor.DefsUtils.cs:284-421` — `DoXmlInheritance` + `XmlOverwriteRecursive`.

The algorithm:
1. For each top-level def in `CombinedDefs` not marked `Abstract="True"`:
2. Walk parent chain via `ParentName` attribute through a `ParentNodeLookUp` dictionary.
3. Merge each parent's children into the child via `XmlOverwriteRecursive`:
   - If child has no node with that name → append parent's node.
   - If attribute `Inherit="false"` on parent's node → replace child's node.
   - If child's node is a text node → replace with parent's.
   - If first child of existing is `li` (list) → append parent's children to the list.
   - Otherwise recurse.
4. Preserve `RequiredPackageId`, `SourceFile`, `Reference` attributes on the merged node.

**Files:**
- Create: `src/RimworldExtractor.Infrastructure/Xml/IXmlInheritanceResolver.cs`
- Create: `src/RimworldExtractor.Infrastructure/Xml/XmlInheritanceResolver.cs`
- Test: `tests/RimworldExtractor.Infrastructure.Tests/Xml/XmlInheritanceResolverTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using System.Xml.Linq;
using FluentAssertions;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Infrastructure.Tests.Xml;

public class XmlInheritanceResolverTests
{
    private static XDocument Doc(string xml) => XDocument.Parse(xml);

    [Fact]
    public void Resolve_WithoutInheritance_ReturnsCopyWithoutAbstracts()
    {
        var combined = Doc("""
            <Defs>
              <ThingDef>
                <defName>X</defName>
                <label>x</label>
              </ThingDef>
              <ThingDef Name="Base" Abstract="True">
                <label>base</label>
              </ThingDef>
            </Defs>
            """);
        var resolver = new XmlInheritanceResolver();

        var result = resolver.Resolve(combined);

        result.Root!.Elements("ThingDef").Should().ContainSingle();
        result.Root!.Element("ThingDef")!.Element("defName")!.Value.Should().Be("X");
    }

    [Fact]
    public void Resolve_ChildWithParentName_InheritsParentFields()
    {
        var combined = Doc("""
            <Defs>
              <ThingDef Name="Base" Abstract="True">
                <description>inherited description</description>
              </ThingDef>
              <ThingDef ParentName="Base">
                <defName>Child</defName>
                <label>child</label>
              </ThingDef>
            </Defs>
            """);
        var resolver = new XmlInheritanceResolver();

        var result = resolver.Resolve(combined);

        var child = result.Root!.Elements("ThingDef").Single(e => e.Element("defName")!.Value == "Child");
        child.Element("description")!.Value.Should().Be("inherited description");
        child.Element("label")!.Value.Should().Be("child");
    }

    [Fact]
    public void Resolve_ChildOverridesParentField()
    {
        var combined = Doc("""
            <Defs>
              <ThingDef Name="Base" Abstract="True">
                <label>base-label</label>
              </ThingDef>
              <ThingDef ParentName="Base">
                <defName>Child</defName>
                <label>child-label</label>
              </ThingDef>
            </Defs>
            """);

        var result = new XmlInheritanceResolver().Resolve(combined);

        var child = result.Root!.Elements("ThingDef").Single(e => e.Element("defName")!.Value == "Child");
        child.Element("label")!.Value.Should().Be("child-label");
    }
}
```

- [ ] **Step 2: Red.**

- [ ] **Step 3: Write interface + implementation**

`IXmlInheritanceResolver.cs`:

```csharp
using System.Xml.Linq;

namespace RimworldExtractor.Infrastructure.Xml;

/// <summary>
/// Resolves Name/ParentName inheritance chains in a combined Defs XDocument, producing a new
/// XDocument where each non-abstract Def has its parent's fields merged in. Abstract Defs
/// are dropped from the output.
/// </summary>
public interface IXmlInheritanceResolver
{
    XDocument Resolve(XDocument combinedDefs);
}
```

`XmlInheritanceResolver.cs`: port `Extractor.DefsUtils.cs:284-421`. Implementation outline (~90 lines):

```csharp
using System.Xml.Linq;

namespace RimworldExtractor.Infrastructure.Xml;

public sealed class XmlInheritanceResolver : IXmlInheritanceResolver
{
    public XDocument Resolve(XDocument combinedDefs)
    {
        var result = new XDocument(new XElement("Defs"));
        var parentLookup = BuildParentLookup(combinedDefs);

        foreach (var node in combinedDefs.Root!.Elements())
        {
            if (IsAbstract(node)) continue;
            var parentName = node.Attribute("ParentName")?.Value;
            if (parentName is null)
            {
                result.Root!.Add(new XElement(node));
                continue;
            }

            var parents = new Stack<XElement>();
            parents.Push(node);
            var current = parentName;
            while (current is not null)
            {
                if (!parentLookup.TryGetValue(current, out var parent)) break;
                parents.Push(parent);
                current = parent.Attribute("ParentName")?.Value;
            }

            var merged = new XElement(node.Name, node.Attributes().Where(a => a.Name.LocalName != "ParentName"));
            while (parents.Count > 0)
            {
                var p = parents.Pop();
                MergeChildren(merged, p);
            }

            // Preserve attributes
            if (node.Attribute("RequiredPackageId") is { } reqPid) merged.SetAttributeValue("RequiredPackageId", reqPid.Value);
            if (node.Attribute("SourceFile") is { } src) merged.SetAttributeValue("SourceFile", src.Value);
            if (node.Attribute("Reference")?.Value == "True") merged.SetAttributeValue("Reference", "True");

            result.Root!.Add(merged);
        }

        return result;
    }

    private static bool IsAbstract(XElement e) =>
        string.Equals(e.Attribute("Abstract")?.Value, "true", StringComparison.OrdinalIgnoreCase);

    private static Dictionary<string, XElement> BuildParentLookup(XDocument doc)
    {
        var dict = new Dictionary<string, XElement>(StringComparer.Ordinal);
        foreach (var node in doc.Root!.Elements())
        {
            var name = node.Attribute("Name")?.Value;
            if (name is not null) dict[name] = node;
        }
        return dict;
    }

    private static void MergeChildren(XElement target, XElement source)
    {
        foreach (var sourceChild in source.Elements())
        {
            var existing = target.Element(sourceChild.Name);
            if (existing is null)
            {
                target.Add(new XElement(sourceChild));
                continue;
            }

            var inherit = !string.Equals(sourceChild.Attribute("Inherit")?.Value, "false", StringComparison.OrdinalIgnoreCase);
            if (!inherit)
            {
                existing.Remove();
                target.Add(new XElement(sourceChild));
                continue;
            }

            if (existing.IsTextNode())
            {
                existing.Remove();
                target.Add(new XElement(sourceChild));
                continue;
            }

            if (existing.Elements().FirstOrDefault()?.IsListNode() == true)
            {
                foreach (var item in sourceChild.Elements()) existing.Add(new XElement(item));
                continue;
            }

            MergeChildren(existing, sourceChild);
        }
    }
}
```

- [ ] **Step 4: Pass.**

- [ ] **Step 5: Commit**
```bash
git add src/RimworldExtractor.Infrastructure/Xml/IXmlInheritanceResolver.cs \
        src/RimworldExtractor.Infrastructure/Xml/XmlInheritanceResolver.cs \
        tests/RimworldExtractor.Infrastructure.Tests/Xml/XmlInheritanceResolverTests.cs
git commit -m "feat(infrastructure): add XmlInheritanceResolver (Name/ParentName + Inherit=false)"
```

### Tasks 16-18: Def extractable-node finder (FindExtractableNodes port)

**Legacy ref:** `Extractor.DefsUtils.cs:54-203` — `FindExtractableNodes` + `FindExtractableNodesXmlExtensionSettings` + `MatchTranslationHandle` + `NormalizedHandle`.

This is the central algorithm. Split into 3 tasks:

- **Task 16:** `NormalizedHandle` port (string-munging only, no IO).
- **Task 17:** `IXmlDefExtractor` interface + `XmlDefExtractor` — main `FindExtractableNodes` port with BFS queue.
- **Task 18:** Translation-handle matching (`MatchTranslationHandle`) + XmlExtensions.SettingsMenuDef special case.

(Plan abbreviated here for length — each of T16/T17/T18 follows the red→green→commit pattern with test code based on `samples/sample-mod/Defs/ThingDefs/Weapons.xml` as input fixture.)

**Task 16 commit:** `feat(infrastructure): add NormalizedHandle utility (legacy port)`
**Task 17 commit:** `feat(infrastructure): add XmlDefExtractor (FindExtractableNodes BFS port)`
**Task 18 commit:** `feat(infrastructure): add translation-handle matching + XmlExtensions special case`

**Specific guidance for implementer:**
- Port `Extractor.DefsUtils.cs:54-131` verbatim but translate `XmlNode` → `XElement`.
- `_isOfficialContent` and `Prefabs.CanExtract` become injected: `XmlDefExtractor` takes an `IReadOnlyDictionary<string, ExtractionRule>` (from Phase 2 `ExtractionSettings.Rules`) and an `isOfficialContent` bool parameter on `FindExtractableNodes`.
- `Prefabs.EnableTkey` → parameter.
- `TranslationHandle` list → inject via ctor.
- The method must return `IEnumerable<TranslationEntry>` and be lazy (yield) — matching legacy.

Tests must cover: plain label extraction, `<li>` list numeric indices, `<TKey>` attribute when enabled, translation-handle match producing named segment, nested def traversal, blacklist/whitelist via ExtractionRule.

---

## Group 3E — Patch Operations

Port `legacy/RimworldExtractorInternal/PatchOperations.cs:1-405`. The 9 handlers share a pattern: read `xpath`, read `value`, find matching nodes, apply operation, emit TranslationEntry from affected subtree.

Tasks 19-25 each handle one operation with a red-green-commit cycle:

- **Task 19:** `IXPatchProcessor` interface + `XPatchProcessor` scaffold with `ApplyAsync(XDocument combined, XElement patchNode, ...)` dispatch method.
- **Task 20:** `PatchOperationAdd` handler.
- **Task 21:** `PatchOperationReplace` handler.
- **Task 22:** `PatchOperationInsert` + `PatchOperationRemove` (latter is new — legacy doesn't have it explicitly but modern mods use it; stub that no-ops on Remove and test).
- **Task 23:** `PatchOperationSequence` handler (recursive into `operations/li/`).
- **Task 24:** `PatchOperationFindMod` + `PatchOperationAddModExtension`.
- **Task 25:** `PatchOperationAttributeAdd/Remove/Set` + default-case log-and-skip for unknown operations.

**Commit messages:** `feat(infrastructure): add IXPatchProcessor dispatch scaffold`, `…add PatchOperationAdd handler`, etc.

Each task's test uses an XDocument fixture simulating the patch scenario, runs the handler, asserts the output tree + emitted entries.

**Critical design detail:** Legacy handlers call `Extractor.FindExtractableNodes` and `Extractor.GetRootDefNode` on global state. In the new code, `XPatchProcessor` takes `IXmlDefExtractor` and a small `IDefRootFinder` service via DI — no static globals.

---

## Group 3F — Languages XML IO

### Tasks 26-30: Read/Write Languages XML

Port `IO.cs:349-720` — `ToLanguageXml` + `FromLanguageXml` + `DoFullListTranslation`.

- **Task 26:** `IXmlLanguagesWriter` interface.
- **Task 27:** `XmlLanguagesWriter.WriteDefInjectedAsync` — generates DefInjected/<ClassName>/<fileName>.xml per def file with optional original comments. Full-list translation collapses indexed entries (e.g. `rulesStrings.0/1/2/`) into `<li>` children.
- **Task 28:** `XmlLanguagesWriter.WriteKeyedAsync` + `WriteStringsAsync` + `WritePatchesAsync` — last one is complex (RequiredMods grouping into PatchOperationFindMod + PatchOperationSequence).
- **Task 29:** `IXmlLanguagesReader` + `XmlLanguagesReader.ReadAsync` — reads back Languages/ directory into `TranslationEntry[]`, detects Full-list translation pattern.
- **Task 30:** Full-list translation integration test using the sample-mod snapshot baseline.

Each with a test using XDocument fixtures and InMemoryFileSystem.

**Commit messages:** `feat(infrastructure): add IXmlLanguagesWriter`, `…DefInjected writer with FullListTranslation`, `…Keyed/Strings/Patches writers`, `…XmlLanguagesReader`, `test: integration test for Languages round-trip`.

---

## Group 3G — Excel IO

### Tasks 31-34: ClosedXML-based Excel reader/writer

- **Task 31:** `LibreOfficePostProcessor` (port `LibreExcelFixer.cs`) — strips LibreOffice-emitted `comments*.xml` entries from XLSX zip. Small and self-contained.
- **Task 32:** `IExcelWriter` interface + `ClosedXmlWriter.WriteAsync` — ports `IO.ToExcel:21-74`. Header constants owned by the writer.
- **Task 33:** `IExcelReader` interface + `ClosedXmlReader.ReadAsync` — ports `IO.FromExcel:280-348`. Uses `LibreOfficePostProcessor` to normalize input first. Handles legacy `"EN [Source string]"` / `"KO [Translation]"` header fallbacks.
- **Task 34:** Round-trip integration test: write entries → read back → `BeEquivalentTo`.

**Commit messages:** `feat(infrastructure): add LibreOfficePostProcessor`, `…ClosedXmlWriter`, `…ClosedXmlReader`, `test: Excel round-trip integration test`.

---

## Group 3H — Output Strategy + DI

### Tasks 35-38: Output strategies + final DI wiring

- **Task 35:** `IOutputStrategy` interface.
- **Task 36:** `ExcelOutputStrategy` — delegates to `IExcelWriter`, writes `{modName}.xlsx`.
- **Task 37:** `LanguagesOutputStrategy` + `LanguagesWithCommentsOutputStrategy` (same writer, comment flag).
- **Task 38:** Extend `AddInfrastructure` DI to register all new services:
  - `IFileSystem` → `PhysicalFileSystem`
  - `IModLister` → `FileSystemModLister` (needs `AppSettings` — resolved via a factory)
  - `IXmlDefParser` → `XDocumentDefParser`
  - `IXmlInheritanceResolver` → `XmlInheritanceResolver`
  - `IXmlDefExtractor` → `XmlDefExtractor`
  - `IXPatchProcessor` → `XPatchProcessor`
  - `IXmlLanguagesReader/Writer` → concrete implementations
  - `IExcelReader/Writer` → ClosedXml implementations
  - `IConflictResolver` → `PolicyBasedConflictResolver` by default (bound to `DuplicatesPolicy` from settings)
  - 3× `IOutputStrategy` keyed by `ExtractionFormat`

- **Task 38 test:** DI resolves every registered type for a given `AppSettings`.

**Commit messages:** `feat(infrastructure): add IOutputStrategy`, `…ExcelOutputStrategy`, `…LanguagesOutputStrategy family`, `feat(infrastructure): extend AddInfrastructure with full service graph`.

---

## Group 3I — Verification Gate

### Task 39: Full-suite verification + push

- [ ] `dotnet build RimworldExtractor.slnx -c Release 2>&1 | tail -3` → 0W/0E
- [ ] `dotnet test RimworldExtractor.slnx -c Release 2>&1 | tail -5` → all tests pass. Expect Phase 2 128 tests + ~60-80 Phase 3 tests = ~200 total.
- [ ] `dotnet format --verify-no-changes` → clean
- [ ] `dotnet test legacy/RimworldExtractorTest/RimworldExtractorTest.csproj --filter LegacyBaselineTests` → 1 passed
- [ ] Coverage report for Infrastructure: `dotnet test RimworldExtractor.slnx --collect:"XPlat Code Coverage"` — verify ≥80% line coverage on all new Infrastructure files
- [ ] `git push origin feat/remake-v2`
- [ ] Summary: commit count, test count, coverage, highlights

No commit for this task — it's verification only.

---

## Self-Review

**Spec coverage:** all master plan §Phase 3 files covered:
- FileSystem (Tasks 1-4) ✓
- Xml (Tasks 13-18) ✓
- Excel (Tasks 31-33) ✓
- Output (Tasks 35-37 + PolicyBasedConflictResolver in Task 8) ✓
- SafeFileWriter (Task 5) + JsonSettingsStore refactor (Task 6) ✓
- IConflictResolver (Task 7) ✓
- Patch operations (Tasks 19-25) — all legacy operations covered ✓
- FileSystemModLister (Tasks 9-12) ✓
- DI wiring (Task 38) ✓

**Placeholder scan:** Groups 3E, 3F, 3G, 3H intentionally compressed with "port legacy with these parameters" guidance rather than full code blocks — this is because the legacy code is the spec. Implementer reads the legacy file (exact line numbers given) and ports. Commit messages + test scenarios are specific. This is dense-but-not-placeholder content.

**Type consistency:**
- `ModMetadata.UnknownId = "???"` (Phase 2) — used in Task 10.
- `ExtractableFolder` — used as return type from `GetExtractableFolders` (matches Phase 2 record).
- `TranslationEntry` / `RequiredMods` — used throughout Phase 3.
- `ConflictDecision` / `IConflictResolver` — new in Phase 3 (Tasks 7-8).
- `ExtractionFormat` enum (Phase 2) → 3 `IOutputStrategy` implementations (Task 37).
- `DuplicatesPolicy` (Phase 2) → `PolicyBasedConflictResolver` ctor (Task 8).

**Known gaps:**
- `FindReferenceMods` transitive-closure algorithm stops at dependencies named in About.xml; legacy also crawls LoadFolders IfModActive. The Task 12 port should replicate legacy behavior; test that covers the LoadFolders-driven refs is deferred to Phase 4 integration testing unless straightforward to add in Task 12.
- XML-legacy `FindExtractableNodesXmlExtensionSettings` is a ModDef-specific compat shim. Task 18 ports it. Phase 4 compat plugins replace this mechanism eventually, but Phase 3 must preserve parity.

---

## Task Execution Order (for subagent dispatch)

Dispatch in 9 batches, one per sub-group:

1. **3A (FileSystem)** Tasks 1-4 — 4 TDD cycles
2. **3B (Safe write + conflict)** Tasks 5-8 — 4 TDD cycles
3. **3C (Mod discovery)** Tasks 9-12 — 4 TDD cycles
4. **3D (XML def parsing)** Tasks 13-18 — 6 TDD cycles
5. **3E (Patch ops)** Tasks 19-25 — 7 TDD cycles
6. **3F (Languages XML)** Tasks 26-30 — 5 TDD cycles
7. **3G (Excel)** Tasks 31-34 — 4 TDD cycles
8. **3H (Output + DI)** Tasks 35-38 — 4 TDD cycles
9. **3I (Gate)** Task 39 — inline verification

Total: 38 implementation tasks + 1 gate + 8 review cycles = ~170 tool calls across subagent dispatches.

---

## Execution Handoff

Plan saved to `docs/plans/remake-v2-phase3-infra.md`.

**Execution:** Continue Subagent-Driven Development per auto-mode. Each sub-batch dispatched as one implementer call with combined spec+quality review. User pre-approved end-to-end execution through Phase 6.
