using System.Xml.Linq;

namespace RimworldExtractor.Infrastructure.Xml;

/// <summary>Parses a single RimWorld Def XML file into an <c>XDocument</c>.</summary>
public interface IXmlDefParser
{
    Task<XDocument> ParseAsync(string filePath, CancellationToken cancellationToken = default);
}
