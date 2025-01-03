@ -0,0 + 1,54 @@
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RimworldExtractorInternal.Compats
{
    /// <summary>
    /// 어반루인 모드 호환성
    /// </summary>
    internal class Compat_AncientMarket_Libraray : BaseCompat
    {
        public const string selector = "Defs/AncientMarket_Libraray.CustomMapDataDef";

        public override void DoPreProcessing(XmlDocument doc)
        {
            var nodes = doc.SelectNodesSafe(selector);
            if (nodes == null) return;

            foreach (XmlNode node in nodes)
            {
                //ProcessNodeRecursive(node);
                var q = new Queue<XmlNode>();
                q.Enqueue(node);

                while (q.Count > 0)
                {
                    var n = q.Dequeue();
                    var toRemove = new List<XmlNode>();

                    foreach (XmlNode child in n.ChildNodes)
                    {
                        if (child.IsTextNode() && child.InnerText.StartsWith('(') && child.InnerText.EndsWith(')'))
                        {
                            toRemove.Add(child);
                        }
                        else
                        {
                            q.Enqueue(child);
                        }
                    }

                    foreach (var child in toRemove)
                    {
                        n.RemoveChild(child);
                    }
                }
            }
        }
    }
}