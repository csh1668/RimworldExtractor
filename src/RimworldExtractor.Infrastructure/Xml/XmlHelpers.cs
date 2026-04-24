using System.Xml.Linq;

namespace RimworldExtractor.Infrastructure.Xml;

/// <summary>
/// Static helpers for navigating and transforming <c>XElement</c> trees in
/// RimWorld Def parsing. Ported from legacy <c>Utils.cs</c> XmlNode extensions.
/// </summary>
public static class XmlHelpers
{
    /// <summary>True when the element is named <c>li</c> (RimWorld list item).</summary>
    public static bool IsListNode(this XElement element) => element.Name.LocalName == "li";

    /// <summary>True when the element has exactly one child of type <c>XText</c> or <c>XCData</c>.</summary>
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
    ///   <item>numeric segments <c>N</c> — <c>li[N+1]</c></item>
    ///   <item>uppercase-first segments — <c>*[.//*[contains(text(), '{seg}')]]</c> (translation-handle path)</item>
    ///   <item>lowercase-first segments — as-is (plain element name)</item>
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
