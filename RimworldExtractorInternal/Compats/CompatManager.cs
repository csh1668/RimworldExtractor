using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RimworldExtractorInternal.Records;

namespace RimworldExtractorInternal.Compats
{
    public static class CompatManager
    {
        private static readonly Type baseType = typeof(BaseCompat);
        private static readonly List<BaseCompat> compats = new();
        static CompatManager()
        {
            var compatTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type =>
                baseType.IsAssignableFrom(type) && type is { IsAbstract: false, IsClass: true });
            foreach (var compatType in compatTypes)
            {
                var compat = (BaseCompat?)Activator.CreateInstance(compatType);
                if (compat != null)
                    compats.Add(compat);
            }
            Log.Msg($"{compats.Count}개의 compat가 로드됨");
        }

        public static IEnumerable<TranslationEntry> DoPostProcessing(IEnumerable<TranslationEntry> entries)
        {
            var translations = entries.ToList();
            foreach (var compat in compats)
            {
                translations = compat.DoPostProcessing(translations).ToList();
            }

            foreach (var entry in translations)
            {
                yield return entry;
            }
        }
    }
}
