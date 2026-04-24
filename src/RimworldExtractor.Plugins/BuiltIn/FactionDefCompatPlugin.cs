using System.Xml.Linq;

namespace RimworldExtractor.Plugins.BuiltIn;

/// <summary>
/// FactionDef compat plugin. Injects default values for <c>pawnSingular</c>,
/// <c>pawnsPlural</c>, and <c>leaderTitle</c> when absent, ensuring those nodes
/// are always present for extraction even on vanilla-style factions that omit them.
///
/// Ports <c>legacy/Compats/Compat_FactionDef.cs</c>.
/// Priority: 100 (default — no [CompatPriority] in legacy).
/// </summary>
public sealed class FactionDefCompatPlugin : CompatPluginBase
{
    public override string Name => "FactionDef";

    public override void PreProcess(XDocument combinedDefs)
    {
        var factionDefs = combinedDefs.Root?.Elements("FactionDef") ?? [];

        foreach (var node in factionDefs)
        {
            if (node.Element("pawnSingular") is null)
                node.Add(new XElement("pawnSingular", "member"));
            if (node.Element("pawnsPlural") is null)
                node.Add(new XElement("pawnsPlural", "members"));
            if (node.Element("leaderTitle") is null)
                node.Add(new XElement("leaderTitle", "leader"));
        }
    }
}
