using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimworldExtractorInternal.Records
{
    public record TranslationEntry(string ClassName, string Node, string Original, string? Translated, HashSet<string>? RequiredMods = null);
}
