using System.Xml.Linq;
using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Plugins.BuiltIn;

/// <summary>
/// ScenarioDef compat plugin. Injects <c>scenario/name</c> and <c>scenario/description</c>
/// elements (copied from top-level <c>label</c> and <c>description</c>) during PreProcess,
/// then in PostProcess rewrites those injected entries back to the canonical
/// <c>DefName.label</c> / <c>DefName.description</c> nodes when the originals are absent.
///
/// Ports <c>legacy/Compats/Compat_ScenarioDef.cs</c>.
/// Priority: 200 (from [CompatPriority(200)] in legacy — runs late).
/// </summary>
[CompatPriority(200)]
public sealed class ScenarioDefCompatPlugin : CompatPluginBase
{
    public override string Name => "ScenarioDef";

    public override void PreProcess(XDocument combinedDefs)
    {
        var scenarioDefs = combinedDefs.Root?.Elements("ScenarioDef") ?? [];

        foreach (var node in scenarioDefs)
        {
            var label = node.Element("label");
            var description = node.Element("description");

            if (label is null || description is null)
                continue; // skip if label or description is missing

            var scenario = node.Element("scenario");
            if (scenario?.Element("name") is not null || scenario?.Element("description") is not null)
                continue; // already has scenario.name or scenario.description

            var scenarioEl = new XElement("scenario",
                new XElement("name", label.Value),
                new XElement("description", description.Value));
            node.Add(scenarioEl);
        }
    }

    public override IEnumerable<TranslationEntry> PostProcess(IEnumerable<TranslationEntry> entries)
    {
        var lst = entries.ToList();

        foreach (var entry in lst)
        {
            if (entry.ClassName.EndsWith("ScenarioDef", StringComparison.Ordinal) && entry.RealNode == "scenario.name")
            {
                // If the original top-level "label" entry is not present, emit it via scenario.name
                if (!lst.Any(x => x.ClassName.EndsWith("ScenarioDef", StringComparison.Ordinal) && x.RealNode == "label"))
                {
                    yield return entry with { Node = entry.DefName + ".label" };
                }
            }
            else if (entry.ClassName.EndsWith("ScenarioDef", StringComparison.Ordinal) && entry.RealNode == "scenario.description")
            {
                if (!lst.Any(x => x.ClassName.EndsWith("ScenarioDef", StringComparison.Ordinal) && x.RealNode == "description"))
                {
                    yield return entry with { Node = entry.DefName + ".description" };
                }
            }

            yield return entry;
        }
    }
}
