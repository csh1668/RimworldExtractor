using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimworldExtractorInternal.Records;

namespace RimworldExtractorInternal.Compats
{
    internal class Compat_NodeReplacement : BaseCompat
    {
        public override IEnumerable<TranslationEntry> DoPostProcessing(IEnumerable<TranslationEntry> entries)
        {
            var translations = entries.ToList();
            foreach (var entry in translations)
            {
                if (entry.ClassName is "Keyed" or "Strings")
                    yield return entry;

                var result = DoNodeReplacement(entry);
                yield return result;
            }
        }

        private TranslationEntry DoNodeReplacement(TranslationEntry entry)
        {
            var isPatches = entry.ClassName.StartsWith("Patches.");
            var defType = isPatches ? entry.ClassName[("Patches.".Length + 1)..] : entry.ClassName;
            var defName = entry.Node.Split('.')[0];
            var nodeAfterDefName = entry.Node[(entry.Node.IndexOf('.') + 1)..];

            foreach (var (key, value) in Prefabs.NodeReplacement)
            {
                var tokenKey = key.Split('+');
                var tokenValue = value.Split("+");
                var targetDef = tokenKey[0];
                var targetNode = tokenKey[1] == "*" ? nodeAfterDefName : tokenKey[1];
                var changedDef = tokenValue[0];
                var changedNode = tokenValue[1] == "*" ? nodeAfterDefName : tokenValue[1];
                if (defType == targetDef && nodeAfterDefName == targetNode)
                {
                    return entry with { ClassName = isPatches ? $"Patches.{changedDef}" : changedDef, Node = $"{defName}.{changedNode}" };
                }
            }

            return entry;
        }
    }
}
