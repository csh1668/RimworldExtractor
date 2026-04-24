using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RimworldExtractor.Infrastructure.Excel;

/// <summary>
/// Strips LibreOffice-generated comment artefacts from an XLSX file so that Excel / ClosedXML
/// can open the result without warnings.  Port of <c>LibreExcelFixer</c> from the legacy codebase.
/// </summary>
/// <remarks>
/// Usage pattern:
/// <code>
/// using var proc = new LibreOfficePostProcessor(inputPath);
/// string fixedPath = proc.Process();
/// // use fixedPath …
/// </code>
/// <see cref="Dispose"/> deletes the temporary directory that holds the working copy.
/// </remarks>
public sealed class LibreOfficePostProcessor : IDisposable
{
    private static readonly Regex CommentFilePattern = new(@"^comments\d+\.xml$", RegexOptions.Compiled);
    private static readonly Regex SheetRelsPattern = new(@"^sheet\d+\.xml\.rels$", RegexOptions.Compiled);
    private static readonly Regex CommentTargetPattern = new(@"\./comments\d+\.xml$", RegexOptions.Compiled);

    private readonly string _sourcePath;
    private readonly string _tmpRoot;
    private readonly string _tmpFilePath;

    public LibreOfficePostProcessor(string sourcePath)
    {
        _sourcePath = sourcePath;
        _tmpRoot = Path.Combine(Path.GetTempPath(), nameof(LibreOfficePostProcessor), Guid.NewGuid().ToString());
        _tmpFilePath = MakeCopy();
    }

    /// <summary>
    /// Removes <c>comments##.xml</c> entries and their relationship references from the XLSX zip,
    /// then returns the path to the fixed temporary file.
    /// </summary>
    public string Process()
    {
        using var zip = ZipFile.Open(_tmpFilePath, ZipArchiveMode.Update);

        // Pass 1: collect and delete all comments##.xml entries.
        var toRemove = new List<ZipArchiveEntry>();
        foreach (ZipArchiveEntry entry in zip.Entries)
        {
            if (CommentFilePattern.IsMatch(entry.Name))
                toRemove.Add(entry);
        }

        foreach (ZipArchiveEntry entry in toRemove)
            entry.Delete();

        // Pass 2: process sheet##.xml.rels entries — remove relationship nodes pointing at comment files.
        for (int i = zip.Entries.Count - 1; i >= 0; i--)
        {
            ZipArchiveEntry entry = zip.Entries[i];

            if (!SheetRelsPattern.IsMatch(entry.Name))
                continue;

            XDocument relsDoc;
            using (Stream relsStream = entry.Open())
            {
                relsDoc = XDocument.Load(relsStream);
            }

            bool edited = false;
            var descendants = relsDoc.Descendants().ToList();
            for (int j = descendants.Count - 1; j >= 0; j--)
            {
                XElement element = descendants[j];
                string? target = element.Attribute("Target")?.Value;
                if (target is not null && CommentTargetPattern.IsMatch(target))
                {
                    element.Remove();
                    edited = true;
                }
            }

            if (edited)
            {
                string fullName = entry.FullName;
                entry.Delete();

                ZipArchiveEntry newEntry = zip.CreateEntry(fullName);
                using Stream newStream = newEntry.Open();
                relsDoc.Save(newStream);
            }
        }

        return _tmpFilePath;
    }

    private string MakeCopy()
    {
        if (!Directory.Exists(_tmpRoot))
            Directory.CreateDirectory(_tmpRoot);

        string fileName = Path.GetFileName(_sourcePath);
        string tmpFilePath = Path.Combine(_tmpRoot, fileName);

        if (!File.Exists(tmpFilePath))
            File.Copy(_sourcePath, tmpFilePath);

        return tmpFilePath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tmpRoot))
        {
            try
            {
                Directory.Delete(_tmpRoot, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup; temp files will be reclaimed by OS eventually.
            }
        }
    }
}
