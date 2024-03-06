using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimworldExtractorInternal.Records
{
    /// <summary>
    /// 번역 데이터
    /// </summary>
    /// <param name="ClassName">번역 데이터의 종류. ○○Def, Keyed, Strings, Patches.○○Def</param>
    /// <param name="Node">위치</param>
    /// <param name="Original">원문</param>
    /// <param name="Translated">번역문</param>
    /// <param name="RequiredMods">요구 모드</param>
    public record TranslationEntry(string ClassName, string Node, string Original, string? Translated,
        HashSet<string>? RequiredMods = null)
    {
        public TranslationEntry(TranslationEntry other)
        {
            ClassName = other.ClassName;
            Node = other.Node;
            Original = other.Original;
            Translated = other.Translated;
            RequiredMods = new HashSet<string>();
            if (other.RequiredMods != null)
            {
                foreach (var otherRequiredMod in other.RequiredMods)
                {
                    RequiredMods.Add(otherRequiredMod);
                }
            }

            _extensions = new Dictionary<string, object>();
            foreach (var otherExtension in other._extensions)
            {
                _extensions.Add(otherExtension.Key, otherExtension.Value);
            }
        }

        private readonly Dictionary<string, object> _extensions = new();
        public bool TryGetExtension(string key, out object? extension)
        {
            extension = null;
            if (_extensions.TryGetValue(key, out extension) == true)
            {
                return true;
            }

            return false;
        }

        public TranslationEntry AddExtension(string key, object extension)
        {
            _extensions.Add(key, extension);
            return this;
        }
    }
}
