using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RimworldExtractorInternal
{
    public static class DebugTools
    {
        public static void DumpXml(XmlDocument doc)
        {
            const string path = "dump.xml";
            doc.Save(path);

            //Process.Start(path);
        }

        public static void DumpXml(XmlNode node)
        {
            const string path = "dump.xml";
            var doc = new XmlDocument();
            doc.AppendChild(doc.ImportNode(node, true));
            doc.Save(path);

            //Process.Start(path);
        }
    }
}
