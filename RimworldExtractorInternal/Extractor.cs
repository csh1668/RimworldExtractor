using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using RimworldExtractorInternal.Records;

namespace RimworldExtractorInternal
{
    public static partial class Extractor
    {
        internal static XmlDocument? CombinedDefs;
        public static readonly Dictionary<string, XmlNode> ParentNodeLookUp = new();

        public static void Reset()
        {
            CombinedDefs = new XmlDocument();
            CombinedDefs.AppendElement("Defs");
            ParentNodeLookUp.Clear();
        }

        public static void PrepareDefs(List<ExtractableFolder> extracableFolders, List<string>? referenceDefsRoots)
        {

            if (CombinedDefs == null)
                Reset();

            if (referenceDefsRoots != null) LoadReferenceDefs(referenceDefsRoots);

            extracableFolders.ForEach(extractableFolder =>
            {
                var defsRoot = extractableFolder.FullPath;
                var requiredPackageId = extractableFolder.RequiredPackageId;
                foreach (var filePath in IO.DescendantFiles(defsRoot).Where(x => x.ToLower().EndsWith(".xml")))
                {
                    try
                    {
                        var childDoc = IO.ReadXml(filePath);

                        foreach (XmlNode node in childDoc.DocumentElement!.ChildNodes)
                        {
                            var newNode = CombinedDefs!.ImportNode(node, true);

                            if (requiredPackageId != null)
                            {
                                var newAttribute = newNode.Attributes?.Append(CombinedDefs.CreateAttribute("RequiredPackageId"))!;
                                newAttribute.Value = requiredPackageId;
                            }

                            CombinedDefs.DocumentElement!.AppendChild(newNode);
                            var attributeName = node.Attributes?["Name"]?.Value;
                            if (attributeName != null)
                            {
                                ParentNodeLookUp[attributeName] = newNode;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Err($"{filePath}를 읽는 중 에러 발생, {e.Message}");
                    }
                }
            });
            DoXmlInheritance();
        }

        public static IEnumerable<TranslationEntry> ExtractDefs()
        {
            if (CombinedDefs == null)
            {
                throw new InvalidOperationException("You need to call PrepareDefs first");
            }

            foreach (XmlNode node in CombinedDefs.DocumentElement!.ChildNodes.OfType<XmlNode>()
                         .Where(x => x.Attributes?["Reference"]?.Value.ToLower() != "true"))
            {
                var defName = node["defName"]?.InnerText;
                if (defName == null)
                {
                    if (node.Name != "SongDef")
                        Log.Wrn($"SongDef과 Abstract가 아닌 XML 요소 {node.Name}에서 'defName' 태그를 찾지 못했습니다. InnerXml: {node.InnerXml}");
                    continue;
                }

                var requiredPackageIds = node.Attributes?["RequiredPackageId"]?.Value.Split(',');
                if (requiredPackageIds != null)
                {
                    for (int i = 0; i < requiredPackageIds.Length; i++)
                    {
                        
                        if (ModLister.TryGetModMetadataByPackageId(requiredPackageIds[i], out var requiredMod) && requiredMod != null)
                        {
                            requiredPackageIds[i] = requiredMod.ModName;
                        }
                        else
                        {
                            requiredPackageIds[i] = "##packageId##" + requiredPackageIds;
                        }
                    }
                }


                var className = node.Attributes?["Class"]?.Value ?? node.Name;

                foreach (var translationEntry in FindExtractableNodes(defName, className, node))
                {
                    yield return translationEntry with
                    {
                        requiredMods = translationEntry.requiredMods?
                            .Concat(requiredPackageIds ?? Array.Empty<string>()).ToHashSet()
                    };
                }
            }
        }

        public static IEnumerable<TranslationEntry> ExtractKeyed(ExtractableFolder keyed)
        {
            var keyedRoot = keyed.FullPath;
            HashSet<string>? requiredMods = null;
            if (keyed.RequiredPackageId == null)
            {
                requiredMods = null;
            }
            else if (ModLister.TryGetModMetadataByPackageId(keyed.RequiredPackageId, out var modMetadata) && modMetadata != null)
            {
                requiredMods = new HashSet<string> { modMetadata.ModName };
            }
            else
            {
                requiredMods = new HashSet<string> { $"##packageId##{keyed.RequiredPackageId}" };
            }
            
            foreach (var filePath in IO.DescendantFiles(keyedRoot).Where(x => x.ToLower().EndsWith(".xml")))
            {
                //var nodeName = Path.GetRelativePath(keyedRoot, filePath);
                //nodeName = Path.GetFileNameWithoutExtension(nodeName).Replace('\\', '.');

                var doc = IO.ReadXml(filePath);
                foreach (XmlNode node in doc.DocumentElement!.ChildNodes)
                {
                    yield return new TranslationEntry("Keyed", node.Name, node.InnerText, null, requiredMods);
                }
            }
        }

        public static IEnumerable<TranslationEntry> ExtractStrings(ExtractableFolder strings)
        {
            var stringsRoot = strings.FullPath;
            HashSet<string>? requiredMods = null;
            if (strings.RequiredPackageId == null)
            {
                requiredMods = null;
            }
            else if (ModLister.TryGetModMetadataByPackageId(strings.RequiredPackageId, out var modMetadata) && modMetadata != null)
            {
                requiredMods = new HashSet<string> { modMetadata.ModName };
            }
            else
            {
                requiredMods = new HashSet<string> { $"##packageId##{strings.RequiredPackageId}" };
            }
            foreach (var filePath in IO.DescendantFiles(stringsRoot).Where(x => x.ToLower().EndsWith(".txt")))
            {
                var nodeName = Path.GetRelativePath(stringsRoot, filePath);
                nodeName = Path.GetFileNameWithoutExtension(nodeName.Replace('\\', '.'));

                var lines = File.ReadAllLines(filePath);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    yield return new TranslationEntry("Strings", $"{nodeName}.{i}", line, null, requiredMods);
                }
            }
        }

        public static IEnumerable<TranslationEntry> ExtractPatches(ExtractableFolder patches)
        {
            if (CombinedDefs == null)
            {
                Log.Err($"{nameof(CombinedDefs)} is null. should call ExtractDefs() first before call ExtractPatches().");
                yield break;
            }

            PatchesUtils.DefsAddedByPatches.Clear();

            var patchesRoot = patches.FullPath;

            List<string>? requiredMods = null;
            if (patches.RequiredPackageId == null)
            {
                requiredMods = null;
            }
            else if (ModLister.TryGetModMetadataByPackageId(patches.RequiredPackageId, out var modMetadata) && modMetadata != null)
            {
                requiredMods = new List<string> { modMetadata.ModName };
            }
            else
            {
                requiredMods = new List<string> { $"##packageId##{patches.RequiredPackageId}" };
            }

            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement("Patch"));
            foreach (var filePath in IO.DescendantFiles(patchesRoot).Where(x => x.ToLower().EndsWith(".xml")))
            {
                var childDoc = IO.ReadXml(filePath);
                foreach (XmlNode node in childDoc.DocumentElement.ChildNodes)
                {
                    if (node.Name != "Operation")
                        continue;

                    var newNode = doc.ImportNode(node, true);
                    doc.DocumentElement.AppendChild(newNode);
                }
            }

            foreach (XmlNode node in doc.DocumentElement!.ChildNodes)
            {
                foreach (var translationEntry in PatchesUtils.PatchOperationRecursive(node, null))
                {
                    yield return translationEntry with
                    {
                        requiredMods = translationEntry.requiredMods == null
                            ? requiredMods?.ToHashSet()
                            : (requiredMods == null
                                ? translationEntry.requiredMods
                                : translationEntry.requiredMods.Concat(requiredMods).ToHashSet())
                    };
                }
            }


            if (PatchesUtils.DefsAddedByPatches.Count == 0)
                yield break;

            foreach (var (listRequiredMods, node) in PatchesUtils.DefsAddedByPatches)
            {
                var name = node.Attributes?["Name"]?.Value;
                if (listRequiredMods != null)
                {
                    node.AppendElement("REQUIREDMODS", tmpNode =>
                    {
                        foreach (var requiredMod in listRequiredMods)
                        {
                            tmpNode.AppendElement("li", requiredMod);
                        }
                    });
                }
                if (name != null)
                {
                    ParentNodeLookUp[name] = node;
                }
            }
            DoXmlInheritance(PatchesUtils.DefsAddedByPatches.Select(x => x.Item2));

            foreach (var translation in ExtractDefs())
            {
                yield return translation with
                {
                    className = $"Patches.{translation.className}",
                    requiredMods = translation.requiredMods == null
                        ? requiredMods?.ToHashSet()
                        : (requiredMods == null
                            ? translation.requiredMods
                            : translation.requiredMods.Concat(requiredMods).ToHashSet())
                };
            }
            yield break;
        }


        private static bool IsListNode(this XmlNode? curNode) => curNode?.Name == "li";

        private static bool IsTextNode(this XmlNode? curNode) =>
            curNode?.ChildNodes.Count == 1 && curNode.FirstChild!.NodeType == XmlNodeType.Text;
    }
}
