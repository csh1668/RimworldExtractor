using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RimworldExtractorInternal.Compats
{
    internal class Compat_Verb : BaseCompat
    {
        private const string selector =
            "Defs/ThingDef/verbs/*[.//verbClass[contains(text(), 'Verb_Shoot') or contains(text(), 'Verb_ShootOneUse') or contains(text(), 'Verb_ShootWithSmoke')]]";
        public override void DoPreProcessing(XmlDocument doc)
        {
            var nodes = doc.SelectNodesSafe(selector);
            if (nodes == null)
                return;
            foreach (XmlNode node in nodes)
            {
                var root = Extractor.GetRootDefNode(node, out _);
                if (root == null || root.HasAttribute("Abstract"))
                    continue;
                var labelNode = root["label"];
                if (labelNode == null)
                {
                    Log.Wrn($"Abstract가 아닌 Def 노드에 label 노드가 없습니다. {node.InnerXml}");
                    continue;
                }

                var verbLabelNode = node["label"];
                if (verbLabelNode == null)
                {
                    node.AppendElement("label", labelNode.InnerText);
                }
            }
        }
    }
}
