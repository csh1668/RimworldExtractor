using System.IO.Compression;
using FluentAssertions;
using RimworldExtractor.Infrastructure.Excel;

namespace RimworldExtractor.Infrastructure.Tests.Excel;

/// <summary>
/// Tests for <see cref="LibreOfficePostProcessor"/>.
/// These tests use real files on disk (Path.GetTempPath) because <see cref="ZipArchive"/>
/// requires a seekable stream that an in-memory fake file system cannot provide reliably.
/// </summary>
public class LibreOfficePostProcessorTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>Creates a minimal XLSX-like zip on disk with the given entries.</summary>
    private static string CreateSyntheticXlsx(
        string tempDir,
        IEnumerable<(string Name, string Content)> entries)
    {
        string path = Path.Combine(tempDir, $"{Guid.NewGuid()}.xlsx");
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach ((string name, string content) in entries)
        {
            ZipArchiveEntry entry = zip.CreateEntry(name);
            using Stream stream = entry.Open();
            using var writer = new StreamWriter(stream);
            writer.Write(content);
        }
        return path;
    }

    private static string MakeTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"LibreTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    // -----------------------------------------------------------------------
    // Tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Process_RemovesCommentXmlEntries()
    {
        string tempDir = MakeTempDir();
        try
        {
            string xlsx = CreateSyntheticXlsx(tempDir,
            [
                ("xl/workbook.xml", "<workbook/>"),
                ("xl/worksheets/sheet1.xml", "<worksheet/>"),
                ("xl/comments1.xml", "<comments/>"),
                ("xl/comments2.xml", "<comments/>"),
            ]);

            using var proc = new LibreOfficePostProcessor(xlsx);
            string fixedPath = proc.Process();

            using var zip = ZipFile.OpenRead(fixedPath);
            zip.Entries.Should().NotContain(e => e.Name.StartsWith("comments") && e.Name.EndsWith(".xml"),
                because: "LibreOffice comment files should be stripped");
            zip.Entries.Should().Contain(e => e.Name == "workbook.xml",
                because: "unrelated entries must survive");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Process_RemovesCommentRelationshipsFromSheetRels()
    {
        string tempDir = MakeTempDir();
        try
        {
            const string relsXml = """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink" Target="https://example.com" TargetMode="External"/>
                  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/comments" Target="./comments1.xml"/>
                </Relationships>
                """;

            string xlsx = CreateSyntheticXlsx(tempDir,
            [
                ("xl/worksheets/sheet1.xml", "<worksheet/>"),
                ("xl/worksheets/_rels/sheet1.xml.rels", relsXml),
                ("xl/comments1.xml", "<comments/>"),
            ]);

            using var proc = new LibreOfficePostProcessor(xlsx);
            string fixedPath = proc.Process();

            using var zip = ZipFile.OpenRead(fixedPath);
            ZipArchiveEntry? relsEntry = zip.Entries.FirstOrDefault(e => e.Name == "sheet1.xml.rels");
            relsEntry.Should().NotBeNull();

            using Stream stream = relsEntry!.Open();
            using var reader = new StreamReader(stream);
            string content = reader.ReadToEnd();

            content.Should().NotContain("comments1.xml",
                because: "the comment relationship element should have been removed");
            content.Should().Contain("https://example.com",
                because: "non-comment relationships must be preserved");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Process_LeavesFileUnchangedWhenNoComments()
    {
        string tempDir = MakeTempDir();
        try
        {
            string xlsx = CreateSyntheticXlsx(tempDir,
            [
                ("xl/workbook.xml", "<workbook/>"),
                ("xl/worksheets/sheet1.xml", "<worksheet/>"),
            ]);

            using var proc = new LibreOfficePostProcessor(xlsx);
            string fixedPath = proc.Process();

            using var zip = ZipFile.OpenRead(fixedPath);
            zip.Entries.Should().HaveCount(2);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Dispose_DeletesTempDirectory()
    {
        string tempDir = MakeTempDir();
        try
        {
            string xlsx = CreateSyntheticXlsx(tempDir,
            [
                ("xl/workbook.xml", "<workbook/>"),
            ]);

            string tmpRoot;
            using (var proc = new LibreOfficePostProcessor(xlsx))
            {
                tmpRoot = proc.Process();
                tmpRoot = Path.GetDirectoryName(tmpRoot)!;
                Directory.Exists(tmpRoot).Should().BeTrue("temp directory should exist while processor is alive");
            }

            Directory.Exists(tmpRoot).Should().BeFalse("Dispose should delete the temp directory");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        string tempDir = MakeTempDir();
        try
        {
            string xlsx = CreateSyntheticXlsx(tempDir,
            [
                ("xl/workbook.xml", "<workbook/>"),
            ]);

            var proc = new LibreOfficePostProcessor(xlsx);
            proc.Process();
            proc.Dispose();
            var act = proc.Invoking(p => p.Dispose());
            act.Should().NotThrow("double-dispose must be safe");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
