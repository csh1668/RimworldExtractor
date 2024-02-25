using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimworldExtractorInternal.Records
{
    public record ModMetadata(string RootDir, string Id, string ModName, string PackageId, bool IsOfficialContent, List<string>? ModDependencies = null)
    {
        public string Identifier
        {
            get
            {
                if (IsOfficialContent) return ModName;
                return Id == "???" ? ModName : $"{ModName} - {Id}";
            }
        }

        public override string ToString()
        {
            return $"{(IsOfficialContent ? "Official" : "Mod")}:{Identifier}:requires={string.Join(',', ModDependencies ?? new List<string>())}";
        }
    }
}
