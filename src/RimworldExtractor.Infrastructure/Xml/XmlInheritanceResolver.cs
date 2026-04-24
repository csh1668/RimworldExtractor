using System.Xml.Linq;

namespace RimworldExtractor.Infrastructure.Xml;

/// <summary>
/// Port of <c>Extractor.DefsUtils.cs:284-421</c> — <c>DoXmlInheritance</c> +
/// <c>XmlOverwriteRecursive</c>. Resolves Name/ParentName inheritance in XDocument form.
/// </summary>
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

            // Walk parent chain, push to stack so oldest ancestor is processed first
            var parents = new Stack<XElement>();
            parents.Push(node);
            var current = parentName;
            while (current is not null)
            {
                if (!parentLookup.TryGetValue(current, out var parent)) break;
                parents.Push(parent);
                current = parent.Attribute("ParentName")?.Value;
            }

            // Start with empty merged element (child's attributes minus ParentName)
            var merged = new XElement(node.Name,
                node.Attributes().Where(a => a.Name.LocalName != "ParentName"));

            // Pop = grandparent first, child last
            while (parents.Count > 0)
            {
                var p = parents.Pop();
                MergeChildren(merged, p);
            }

            // Preserve special attributes from the original child node (may have been overwritten)
            if (node.Attribute("RequiredPackageId") is { } reqPid)
                merged.SetAttributeValue("RequiredPackageId", reqPid.Value);
            if (node.Attribute("SourceFile") is { } src)
                merged.SetAttributeValue("SourceFile", src.Value);
            if (string.Equals(node.Attribute("Reference")?.Value, "True", StringComparison.OrdinalIgnoreCase))
                merged.SetAttributeValue("Reference", "True");

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

    /// <summary>
    /// Port of <c>XmlOverwriteRecursive</c>: merges <paramref name="source"/> children into
    /// <paramref name="target"/> following RimWorld inheritance rules.
    /// </summary>
    private static void MergeChildren(XElement target, XElement source)
    {
        foreach (var sourceChild in source.Elements())
        {
            var existing = target.Element(sourceChild.Name);

            // 1. Missing in target — append copy from source
            if (existing is null)
            {
                target.Add(new XElement(sourceChild));
                continue;
            }

            // 2. Inherit="false" on source child — replace
            var inherit = !string.Equals(
                sourceChild.Attribute("Inherit")?.Value, "false",
                StringComparison.OrdinalIgnoreCase);
            if (!inherit)
            {
                existing.Remove();
                target.Add(new XElement(sourceChild));
                continue;
            }

            // 3. Existing is a text node — replace with source
            if (existing.IsTextNode())
            {
                existing.Remove();
                target.Add(new XElement(sourceChild));
                continue;
            }

            // 4. Existing contains <li> list — append source children to the list
            if (existing.Elements().FirstOrDefault()?.IsListNode() == true)
            {
                foreach (var item in sourceChild.Elements())
                    existing.Add(new XElement(item));
                continue;
            }

            // 5. Recurse
            MergeChildren(existing, sourceChild);
        }
    }
}
