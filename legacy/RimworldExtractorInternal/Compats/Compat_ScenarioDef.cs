using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using RimworldExtractorInternal.DataTypes;

namespace RimworldExtractorInternal.Compats
{

    [CompatPriority(200)]
    internal class Compat_ScenarioDef : BaseCompat
    {
        private const string selector =
            "Defs/ScenarioDef";
        public override void DoPreProcessing(XmlDocument doc)
        {
            var nodes = doc.SelectNodesSafe(selector);
            if (nodes == null)
                return;

            foreach (XmlNode node in nodes)
            {
                var label = node["label"];
                var description = node["description"];

                if (label == null || description == null)
                {
                    Log.Wrn($"ScenarioDef에 label이나 description 태그가 없습니다. defName: {node["defName"]?.InnerText}");
                    continue;
                }

                if (node["scenario"]?["name"] != null || node["scenario"]?["description"] != null)
                {
                    Log.Msg($"ScenarioDef에 이미 scenario.name이나 scenario.description 태그가 존재합니다. defName: {node["defName"]?.InnerText}");
                    continue;
                }

                node.AppendElement("scenario", scenario =>
                {
                    scenario.AppendElement("name", label.InnerText);
                    scenario.AppendElement("description", description.InnerText);
                });
            }
        }

        public override IEnumerable<TranslationEntry> DoPostProcessing(IEnumerable<TranslationEntry> entries)
        {
            var lst = entries.ToList();
            foreach (var entry in lst)
            {
                if (entry.ClassName.EndsWith("ScenarioDef") && entry.RealNode is "scenario.name")
                {
                    if (!lst.Any(x =>
                            x.ClassName.EndsWith("ScenarioDef") &&
                            x.RealNode is "label"))
                    {
                        yield return entry with { Node = entry.DefName + ".label" };
                    }
                }
                else if (entry.ClassName.EndsWith("ScenarioDef") && entry.RealNode is "scenario.description")
                {
                    if (!lst.Any(x =>
                            x.ClassName.EndsWith("ScenarioDef") &&
                            x.RealNode is "description"))
                    {
                        yield return entry with { Node = entry.DefName + ".description" };
                    }
                }
                yield return entry;
            }
        }
    }
}
