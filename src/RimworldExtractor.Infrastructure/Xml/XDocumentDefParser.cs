using System.Xml;
using System.Xml.Linq;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Xml;

/// <summary>
/// Reads a Def XML file through an <c>IFileSystem</c> and parses it with XDocument.
/// Legacy <c>ReadXml</c> (IO.cs:861-875) settings preserved: <c>IgnoreComments=true</c>,
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
