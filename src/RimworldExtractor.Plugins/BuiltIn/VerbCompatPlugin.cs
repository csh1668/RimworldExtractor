using System.Xml.Linq;

namespace RimworldExtractor.Plugins.BuiltIn;

/// <summary>
/// Verb compat plugin. For each ThingDef that has a verb with a shoot-class verbClass
/// but no explicit <c>label</c> element, injects the ThingDef's top-level <c>label</c>
/// text as the verb's label. This ensures shoot-verbs are translatable even when they
/// inherit their label from the owning def.
///
/// Ports <c>legacy/Compats/Compat_Verb.cs</c>.
/// Priority: 100 (default — no [CompatPriority] in legacy).
/// </summary>
public sealed class VerbCompatPlugin : CompatPluginBase
{
    private static readonly HashSet<string> ShootClasses = new(StringComparer.Ordinal)
    {
        "Verb_Shoot",
        "Verb_ShootOneUse",
        "Verb_ShootWithSmoke"
    };

    public override string Name => "Verb";

    public override void PreProcess(XDocument combinedDefs)
    {
        // Find all ThingDef/verbs/li (or non-li verb children) where verbClass is a shoot variant
        var thingDefs = combinedDefs.Root?.Elements("ThingDef") ?? [];

        foreach (var thingDef in thingDefs)
        {
            // Skip abstract defs
            if (thingDef.Attribute("Abstract") is not null)
                continue;

            var verbsElement = thingDef.Element("verbs");
            if (verbsElement is null)
                continue;

            var defLabelElement = thingDef.Element("label");
            if (defLabelElement is null)
            {
                // No label on the ThingDef — skip (matches legacy warning behavior)
                continue;
            }

            foreach (var verbNode in verbsElement.Elements())
            {
                var verbClass = verbNode.Descendants("verbClass").FirstOrDefault()?.Value;
                if (verbClass is null)
                    continue;

                // Check whether verbClass contains a shoot-class suffix
                var isShoot = ShootClasses.Any(cls => verbClass.Contains(cls, StringComparison.Ordinal));
                if (!isShoot)
                    continue;

                // If verb already has a label, leave it
                if (verbNode.Element("label") is not null)
                    continue;

                verbNode.Add(new XElement("label", defLabelElement.Value));
            }
        }
    }
}
