using System.Xml.Linq;

namespace RimworldExtractor.Plugins.BuiltIn;

/// <summary>
/// AncientMarket Library compat plugin (어반루인 모드 호환성).
/// Strips text nodes that start with '(' and end with ')' from
/// <c>AncientMarket_Libraray.CustomMapDataDef</c> elements. These parenthetical
/// values are internal identifiers that should not appear in translation sheets.
///
/// Ports <c>legacy/Compats/Compat_AncientMarket_Libraray.cs</c>.
/// Class name typo "Libraray" → "Library" is corrected here.
/// Priority: 100 (default — no [CompatPriority] in legacy).
/// </summary>
public sealed class AncientMarketLibraryCompatPlugin : CompatPluginBase
{
    /// <summary>
    /// The element name as it appears in the mod XML (preserving the original typo
    /// so the XPath-style element selection matches the actual XML).
    /// </summary>
    private const string DefElementName = "AncientMarket_Libraray.CustomMapDataDef";

    public override string Name => "AncientMarketLibrary";

    public override void PreProcess(XDocument combinedDefs)
    {
        // In LINQ-to-XML the element name with a dot cannot be used as a simple
        // XElement.Name — it IS a valid local name, so XDocument.Descendants(name) works.
        var defs = combinedDefs.Root?.Elements(DefElementName) ?? [];

        foreach (var def in defs)
        {
            RemoveParentheticalTextNodes(def);
        }
    }

    private static void RemoveParentheticalTextNodes(XElement root)
    {
        var queue = new Queue<XElement>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var toRemove = new List<XNode>();

            foreach (var child in node.Nodes())
            {
                if (child is XText textNode
                    && textNode.Value.StartsWith('(')
                    && textNode.Value.EndsWith(')'))
                {
                    toRemove.Add(child);
                }
                else if (child is XElement childEl)
                {
                    queue.Enqueue(childEl);
                }
            }

            foreach (var child in toRemove)
            {
                child.Remove();
            }
        }
    }
}
