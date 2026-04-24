using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using RimworldExtractorInternal.DataTypes;

namespace RimworldExtractorInternal.Compats
{
    public abstract class BaseCompat
    {
        public virtual IEnumerable<TranslationEntry> DoPostProcessing(IEnumerable<TranslationEntry> entries)
        {
            foreach (var entry in entries)
            {
                yield return entry;
            }
        }

        public virtual void DoPreProcessing(XmlDocument doc)
        {
            return;
        }
    }
}
