using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RimworldExtractorInternal.Compats
{
    internal class Compat_FactionDef : BaseCompat
    {
        private const string selector =
            "Defs/FactionDef";
        public override void DoPreProcessing(XmlDocument doc)
        {
            var nodes = doc.SelectNodesSafe(selector);
            if (nodes == null)
                return;
            foreach (XmlNode node in nodes)
            {
                var pawnSingular = node["pawnSingular"];
                var pawnsPlural = node["pawnsPlural"];
                var leaderTitle = node["leaderTitle"];

                if (pawnSingular == null)
                    node.AppendElement("pawnSingular", "member");
                if (pawnsPlural == null)
                    node.AppendElement("pawnsPlural", "members");
                if (leaderTitle == null)
                    node.AppendElement("leaderTitle", "leader");
            }
        }
    }
}
