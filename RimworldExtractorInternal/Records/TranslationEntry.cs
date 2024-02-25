using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimworldExtractorInternal.Records
{
    public record TranslationEntry(string className, string node, string original, string? translated, List<string>? requiredMods = null);
}
