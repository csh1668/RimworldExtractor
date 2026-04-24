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
