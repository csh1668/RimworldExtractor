using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using RimworldExtractorInternal.Compats;
using RimworldExtractorInternal.DataTypes;

namespace RimworldExtractorInternal
{
    public static partial class Extractor
    {
        internal static XmlDocument? CombinedDefs;
        public static readonly Dictionary<string, XmlNode> ParentNodeLookUp = new();

        public static List<TranslationEntry> ExtractTranslationData(List<ExtractableFolder> selectedFolders, List<ModMetadata>? referenceMods)
        {
            var refDefs = new List<string>();
            if (referenceMods != null)
            {
                foreach (var referenceMod in referenceMods)
                {
                    refDefs.AddRange(from extractableFolder in ModLister.GetExtractableFolders(referenceMod)
                        where (extractableFolder.VersionInfo == "default" || extractableFolder.VersionInfo == Prefabs.CurrentVersion)
                              && Path.GetFileName(extractableFolder.FolderName) == "Defs"
                        select Path.Combine(referenceMod.RootDir, extractableFolder.FolderName));
                }
            }

            var extraction = new List<TranslationEntry>();
            Reset();
            var defs = selectedFolders.Where(x => Path.GetFileName(x.FolderName) == "Defs").ToList();
            if (defs.Count > 0)
            {
                var prePatches = selectedFolders.Where(x => Path.GetFileName(x.FolderName) == "Patches").ToList();
                PrepareDefs(defs, refDefs, prePatches);
                extraction.AddRange(ExtractDefs());
            }
            foreach (var extractableFolder in selectedFolders)
            {
                switch (Path.GetFileName(extractableFolder.FolderName))
                {
                    case "Defs":
                        break;
                    case "Keyed":
                        extraction.AddRange(ExtractKeyed(extractableFolder));
                        break;
                    case "Strings":
                        extraction.AddRange(ExtractStrings(extractableFolder));
                        break;
                    case "Patches":
                        extraction.AddRange(ExtractPatches(extractableFolder));
                        break;
                    default:
                        Log.Wrn($"지원하지 않는 폴더입니다. {extractableFolder.FolderName}");
                        continue;
                }
            }

            var set = new HashSet<(string, string)>();
            foreach (var entry in extraction)
            {
                var tuple = (entry.ClassName + "+" + entry.Node, entry.Original);
                var pair = set.FirstOrDefault(x => x.Item1 == tuple.Item1);
                if (pair != default)
                {
                    if (pair.Item2 != entry.Original)
                    {
                        Log.Wrn(
                            $"원문이 다른 중복되는 노드가 있습니다. {entry.ClassName}+{entry.Node}| {pair.Item2} | {entry.Original} ");
                    }
                }

                set.Add(tuple);
            }

            return extraction.DistinctBy(x => $"{x.ClassName}+{x.Node}").ToList();
        }

        private static void Reset()
        {
            CombinedDefs = new XmlDocument();
            CombinedDefs.AppendElement("Defs");
            ParentNodeLookUp.Clear();
        }

        private static void PrepareDefs(List<ExtractableFolder> extracableFolders, List<string>? referenceDefsRoots, List<ExtractableFolder> prePatches)
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
                                newNode.AppendAttribute("RequiredPackageId", requiredPackageId);
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

            DoPrePatch(prePatches);

            DoXmlInheritance();
        }

        private static void DoPrePatch(List<ExtractableFolder> prePatches)
        {
            foreach (var patchDir in prePatches)
            {
                foreach (var entry in ExtractPatches(patchDir, true))
                {
                    // do nothing
                }
            }

            foreach (XmlNode node in CombinedDefs!.DocumentElement!.ChildNodes)
            {
                var attributeName = node.Attributes?["Name"]?.Value;
                if (attributeName != null)
                {
                    ParentNodeLookUp[attributeName] = node;
                }
            }
        }

        internal static IEnumerable<TranslationEntry> ExtractDefs()
        {
            var rawExtraction = ExtractDefsInternal().ToList();
            foreach (var entry in CompatManager.DoPostProcessing(rawExtraction))
            {
                yield return entry;
            }
        }
        private static IEnumerable<TranslationEntry> ExtractDefsInternal()
        {
            if (CombinedDefs == null)
            {
                throw new InvalidOperationException("You need to call PrepareDefs first");
            }

            CompatManager.DoPreProcessing(CombinedDefs);

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

                var requiredMods = new RequiredMods();
                var requiredPackageIds = node.Attributes?["RequiredPackageId"]?.Value.Split(',');
                if (requiredPackageIds != null)
                {
                    requiredMods.AddAllowedByPackageIds(requiredPackageIds);
                }


                var className = node.Attributes?["Class"]?.Value ?? node.Name;

                foreach (var translationEntry in FindExtractableNodes(defName, className, node))
                {
                    yield return translationEntry with
                    {
                        RequiredMods = translationEntry.RequiredMods + requiredMods
                    };
                }
            }
        }

        internal static IEnumerable<TranslationEntry> ExtractKeyed(ExtractableFolder keyed)
        {
            var keyedRoot = keyed.FullPath;
            RequiredMods? requiredMods = null;
            if (keyed.RequiredPackageId == null)
            {
                requiredMods = null;
            }
            else
            {
                requiredMods = new RequiredMods();
                requiredMods.AddAllowedByPackageIds(keyed.RequiredPackageId.Split(','));
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

        internal static IEnumerable<TranslationEntry> ExtractStrings(ExtractableFolder strings)
        {
            var stringsRoot = strings.FullPath;
            RequiredMods? requiredMods = null;
            if (strings.RequiredPackageId == null)
            {
                requiredMods = null;
            }
            else
            {
                requiredMods = new RequiredMods();
                requiredMods.AddAllowedByPackageIds(strings.RequiredPackageId.Split(','));
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


        internal static IEnumerable<TranslationEntry> ExtractPatches(ExtractableFolder patches, bool prePatchMode = false)
        {
            var rawExtraction = ExtractPatchesInternal(patches, prePatchMode).ToList();
            foreach (var entry in CompatManager.DoPostProcessing(rawExtraction))
            {
                yield return entry;
            }
        }

        private static IEnumerable<TranslationEntry> ExtractPatchesInternal(ExtractableFolder patches, bool prePatchMode = false)
        {
            if (CombinedDefs == null)
            {
                Log.Err($"{nameof(CombinedDefs)} is null. should call ExtractDefs() first before call ExtractPatches().");
                yield break;
            }

            PatchOperations.DefsAddedByPatches.Clear();

            var patchesRoot = patches.FullPath;

            RequiredMods? requiredMods = null;
            if (patches.RequiredPackageId == null)
            {
                requiredMods = null;
            }
            else
            {
                requiredMods = new RequiredMods();
                requiredMods.AddAllowedByPackageIds(patches.RequiredPackageId.Split(','));
            }

            var doc = new XmlDocument();
            doc.AppendElement("Patch");
            foreach (var filePath in IO.DescendantFiles(patchesRoot).Where(x => x.ToLower().EndsWith(".xml")))
            {
                var childDoc = IO.ReadXml(filePath);
                foreach (XmlNode node in childDoc.DocumentElement!.ChildNodes)
                {
                    if (node.Name != "Operation")
                        continue;

                    var newNode = doc.ImportNode(node, true);
                    doc.DocumentElement!.AppendChild(newNode);
                }
            }

            foreach (XmlNode node in doc.DocumentElement!.ChildNodes)
            {
                foreach (var translationEntry in PatchOperations.PatchOperationRecursive(node, null, prePatchMode))
                {
                    yield return translationEntry with
                    {
                        RequiredMods = translationEntry.RequiredMods + requiredMods
                    };
                }
            }


            if (PatchOperations.DefsAddedByPatches.Count == 0)
                yield break;
            CompatManager.DoPreProcessing(doc);
            foreach (var (requiredModsPatches, node) in PatchOperations.DefsAddedByPatches)
            {
                var name = node.Attributes?["Name"]?.Value;
                if (requiredModsPatches != null)
                {
                    node.AppendElement("REQUIREDMODS", requiredModsPatches.ToString());
                }
                if (name != null)
                {
                    ParentNodeLookUp[name] = node;
                }
            }
            DoXmlInheritance(PatchOperations.DefsAddedByPatches.Select(x => x.Item2));

            foreach (var translation in ExtractDefs())
            {
                yield return translation with
                {
                    ClassName = $"Patches.{translation.ClassName}",
                    RequiredMods = translation.RequiredMods + requiredMods
                };
            }
            yield break;
        }
    }
}
