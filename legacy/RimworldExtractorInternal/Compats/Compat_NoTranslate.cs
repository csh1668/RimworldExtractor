using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimworldExtractorInternal.DataTypes;

namespace RimworldExtractorInternal.Compats
{
    internal class Compat_NoTranslate : BaseCompat
    {
        private static readonly string colorChannels = "colorChannels";
        public override IEnumerable<TranslationEntry> DoPostProcessing(IEnumerable<TranslationEntry> entries)
        {
            foreach (var translationEntry in entries)
            {
                var node = translationEntry.Node;
                if (node.Contains(colorChannels))
                    continue;
                yield return translationEntry;
            }
        }
    }
}
