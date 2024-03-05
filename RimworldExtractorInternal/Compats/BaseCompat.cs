using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimworldExtractorInternal.Records;

namespace RimworldExtractorInternal.Compats
{
    public abstract class BaseCompat
    {
        public abstract IEnumerable<TranslationEntry> DoPostProcessing(IEnumerable<TranslationEntry> entries);
    }
}
