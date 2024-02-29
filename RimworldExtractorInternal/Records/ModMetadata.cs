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

        public virtual bool Equals(ModMetadata? other)
        {
            return other != null && GetHashCode() == other.GetHashCode();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = RootDir.GetHashCode();
                hashCode = (hashCode * 397) ^ Id.GetHashCode();
                hashCode = (hashCode * 397) ^ ModName.GetHashCode();
                hashCode = (hashCode * 397) ^ PackageId.GetHashCode();
                hashCode = (hashCode * 397) ^ IsOfficialContent.GetHashCode();
                hashCode = (hashCode * 397) ^ (string.Concat(ModDependencies ?? new List<string>())).GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{(IsOfficialContent ? "Official" : "Mod")}:{Identifier}:requires={string.Join(',', ModDependencies ?? new List<string>())}";
        }

        public static ModMetadata Emptry => new("", "", "", "", false, null);
    }
}
