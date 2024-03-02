using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using RimworldExtractorInternal.Records;

namespace RimworldExtractorInternal
{
    public static class ModLister
    {
        public static IEnumerable<string> ModRootsOfficial
        {
            get
            {
                var dirOfficial = Path.Combine(Prefabs.PathRimworld, "Data");
                if (Directory.Exists(dirOfficial))
                    foreach (var dir in Directory.EnumerateDirectories(dirOfficial))
                        yield return dir;
            }
        }

        public static IEnumerable<string> ModRootsLocal
        {
            get
            {
                var dirLocalMods = Path.Combine(Prefabs.PathRimworld, "Mods");
                if (Directory.Exists(dirLocalMods))
                    foreach (var dir in Directory.EnumerateDirectories(dirLocalMods))
                        yield return dir;
            }
        }

        public static IEnumerable<string> ModRootsWorkshop
        {
            get
            {
                var dirWorkshopMods = Prefabs.PathWorkshop;
                if (Directory.Exists(dirWorkshopMods))
                    foreach (var dir in Directory.EnumerateDirectories(dirWorkshopMods))
                        yield return dir;
            }
        }

        public static IEnumerable<string> ModRootsAll => ModRootsOfficial.Concat(ModRootsLocal).Concat(ModRootsWorkshop);
        public static IEnumerable<ModMetadata> OfficialMods => ModRootsOfficial.Select(GetModMetadataByModRoot).OrderBy(x => x.ModName);
        public static IEnumerable<ModMetadata> LocalMods => ModRootsLocal.Select(GetModMetadataByModRoot).OrderBy(x => x.ModName);
        public static IEnumerable<ModMetadata> WorkshopMods => ModRootsWorkshop.Select(GetModMetadataByModRoot).OrderBy(x => x.ModName);
        public static IEnumerable<ModMetadata> AllMods => OfficialMods.Concat(LocalMods).Concat(WorkshopMods);

        public static ModMetadata GetModMetadataByModRoot(string modRoot)
        {
            var pathAbout = Path.Combine(modRoot, "About", "About.xml");
            string name = "UNKNOWN";
            string packageId = "UNKNOWN";
            var modDependencies = new List<string>();
            if (File.Exists(pathAbout))
            {
                try
                {
                    var doc = new XmlDocument();
                    doc.LoadXml(File.ReadAllText(pathAbout));
                    packageId = doc.DocumentElement?["packageId"]?.InnerText ?? "UNKNOWN";
                    name = doc.DocumentElement?["name"]?.InnerText ?? "UNKNOWN";
                    if (name == "UNKNOWN")
                    {
                        // Official Contents
                        if (doc.DocumentElement?["author"]?.InnerText == "Ludeon Studios")
                        {
                            name = Path.GetFileName(modRoot).Trim();
                            return new ModMetadata(modRoot, "Official", name, packageId, true);
                        }
                    }

                    if (doc.DocumentElement?["modDependencies"] != null)
                    {
                        foreach (XmlNode childNode in doc.DocumentElement["modDependencies"]!.ChildNodes)
                        {
                            var packageIdModDependencies = childNode["packageId"];
                            if (packageIdModDependencies != null)
                                modDependencies.Add(packageIdModDependencies.InnerText);
                        }
                    }

                    if (doc.DocumentElement?["modDependenciesByVersion"] != null)
                    {
                        var nodes = doc.DocumentElement["modDependenciesByVersion"]?["v" + Prefabs.CurrentVersion]
                            ?.ChildNodes;
                        if (nodes != null)
                        {
                            foreach (XmlNode childNode in nodes)
                            {
                                var packageIdModDependencies = childNode["packageId"];
                                if (packageIdModDependencies != null)
                                    modDependencies.Add(packageIdModDependencies.InnerText);
                            }
                        }
                    }


                }
                catch (Exception e)
                {
                    Log.Err($"{pathAbout}에 있는 About.xml 파일을 읽을 수 없었습니다. {e.Message}");
                }
            }

            var pathPublishedFileId = Path.Combine(modRoot, "About", "PublishedFileId.txt");
            var id = "???";
            if (File.Exists(pathPublishedFileId))
            {
                id = File.ReadAllText(pathPublishedFileId).Trim();
            }
            else if (modRoot.Contains("workshop\\content\\294100"))
            {
                id = Path.GetFileName(modRoot);
            }

            modDependencies = modDependencies.Distinct().ToList();
            return new ModMetadata(modRoot, id, name, packageId, false, modDependencies);
        }

        public static ModMetadata? GetModMetadataByModName(string modName)
        {
            return AllMods.FirstOrDefault(x => x.ModName == modName);
        }

        public static List<ExtractableFolder> GetExtractableFolders(ModMetadata modMetadata)
        {
            var root = modMetadata.RootDir;

            var sets = new HashSet<ExtractableFolder>(new ExtractableFolderComparer());
            var pathLoadFolders = Path.Combine(root, "LoadFolders.xml");
            string[] targetFolders = {
                "Defs", "Patches", "Keyed", 
                Path.Combine("Languages", Prefabs.OriginalLanguage, "Keyed"),
                Path.Combine("Languages", Prefabs.OriginalLanguage.Split(' ').First(), "Keyed"),
                Path.Combine("Languages", Prefabs.OriginalLanguage, "Strings"),
                Path.Combine("Languages", Prefabs.OriginalLanguage.Split(' ').First(), "Strings")
            };

            IEnumerable<string> GetExtractableFoldersInternal(string path)
            {
                foreach (var folder in targetFolders)
                {
                    var subDir = Path.Combine(path, folder);
                    if (Directory.Exists(subDir))
                    {
                        yield return Path.GetRelativePath(root, subDir);
                    }
                }
            }

            foreach (var extractableFolder in GetExtractableFoldersInternal(root).Select(x => new ExtractableFolder(modMetadata, x, null)))
            {
                sets.Add(extractableFolder);
            }

            if (File.Exists(pathLoadFolders))
            {
                var doc = new XmlDocument();
                doc.LoadXml(File.ReadAllText(pathLoadFolders));
                foreach (XmlNode node in doc.DocumentElement!.ChildNodes)
                {
                    var name = node.Name;
                    foreach (XmlNode li in node.ChildNodes)
                    {
                        var requiredPackageIds = li.Attributes?["IfModActive"]?.Value;
                        foreach (var extractableFolder in GetExtractableFoldersInternal(Path.Combine(root, li.InnerText))
                                     .Select(x => new ExtractableFolder(modMetadata, x, requiredPackageIds, name[1..])))
                        {
                            sets.Add(extractableFolder);
                        }
                    }
                }
            }
            else
            {
                foreach (var directory in Directory.EnumerateDirectories(root))
                {
                    var lastDir = Path.GetFileName(directory);
                    if (Regex.IsMatch(lastDir, Prefabs.PatternVersion))
                    {
                        foreach (var extractableFolder in GetExtractableFoldersInternal(directory)
                                     .Select(x => new ExtractableFolder(modMetadata, x, null, lastDir)))
                        {
                            sets.Add(extractableFolder);
                        }
                    }
                }

                var commonDir = Path.Combine(root, "Common");
                if (Directory.Exists(commonDir))
                {
                    foreach (var extractableFolder in GetExtractableFoldersInternal(commonDir).Select(x => new ExtractableFolder(modMetadata, x, null, "Common")))
                    {
                        sets.Add(extractableFolder);
                    }
                }
            }

            return sets.ToList();
        }

        public static IEnumerable<ModMetadata> FindAllReferenceMods(ModMetadata target)
        {
            foreach (var officialMod in OfficialMods.Where(official => official != target))
            {
                yield return officialMod;
            }

            var set = new HashSet<ModMetadata>();
            modMetadataByPackageIdLookUp.Clear();

            IEnumerable<ModMetadata> FindAllReferenceModsInternal(ModMetadata modMetadata)
            {
                if (modMetadata.ModDependencies != null)
                {
                    foreach (var modDependency in modMetadata.ModDependencies)
                    {
                        var b = TryGetModMetadataByPackageId(modDependency, out var possible);
                        if (possible != null)
                            yield return possible;
                    }
                }

                foreach (var extractableFolder in GetExtractableFolders(modMetadata))
                {
                    if (extractableFolder.RequiredPackageId != null)
                    {
                        foreach (var modDependency in extractableFolder.RequiredPackageId.Split(','))
                        {
                            var b = TryGetModMetadataByPackageId(modDependency, out var possible);
                            if (possible != null)
                                yield return possible;
                        }
                    }
                }
            }

            IEnumerable<ModMetadata> FindAllReferenceModsRecursive(ModMetadata modMetadata)
            {
                foreach (var child in FindAllReferenceModsInternal(modMetadata))
                {
                    if (set.Contains(child))
                        continue;
                    yield return child;
                    set.Add(child);
                    foreach (var childchild in FindAllReferenceModsRecursive(child))
                    {
                        yield return childchild;
                        set.Add(childchild);
                    }
                }
            }

            foreach (var modMetadata in FindAllReferenceModsRecursive(target))
            {
                yield return modMetadata;
            }

            modMetadataByPackageIdLookUp.Clear();
        }

        internal static bool TryGetModMetadataByPackageId(string packageId, out ModMetadata? modMetadata)
        {
            if (packageId == null)
            {
                modMetadata = null;
                return false;
            }
            if (modMetadataByPackageIdLookUp.TryGetValue(packageId, out var value))
            {
                modMetadata = value;
                return value != null;
            }


            var matches = AllMods.Where(x => string.Equals(x.PackageId, packageId, StringComparison.CurrentCultureIgnoreCase)).ToList();
            switch (matches.Count)
            {
                case < 1:
                    modMetadata = null;
                    modMetadataByPackageIdLookUp[packageId] = null;
                    Log.Wrn($"모드 폴더에서 packageId가 {packageId}인 모드를 찾을 수 없었습니다.");
                    return false;
                case 1:
                    modMetadata = matches[0];
                    modMetadataByPackageIdLookUp[packageId] = modMetadata;
                    return true;
                case > 1:
                    modMetadata = matches[0];
                    modMetadataByPackageIdLookUp[packageId] = modMetadata;
                    Log.Msg($"중복되는 packageId={packageId}, 중복 갯수={matches.Count}.");
                    return true;
            }
        }

        private static readonly Dictionary<string, ModMetadata?> modMetadataByPackageIdLookUp = new();

    }
}
