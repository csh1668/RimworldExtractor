using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimworldExtractorInternal.DataTypes;

namespace RimworldExtractorInternal
{
    public static class TranslationAnalyzerTool
    {
        public static string[] GetXlsxPaths(string rootPath) =>
            IO.DescendantFiles(rootPath).Where(x => x.ToLower().EndsWith(".xlsx")).ToArray();

        public static ModMetadata? GetModMetadataFromFilePath(string filePath)
        {
            if (ModMetadatasByFilePath.TryGetValue(filePath, out var value))
            {
                return value;
            }
            else
            {
                var tmp = GetModMetadataFromFilePathInternal(filePath);
                ModMetadatasByFilePath[filePath] = tmp;
                return tmp;
            }
        }
        private static ModMetadata? GetModMetadataFromFilePathInternal(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            // Step 1: (ModName - Id)
            var dashIdx = fileName.LastIndexOf('-');
            if (dashIdx > 0)
            {
                var modName = fileName[..dashIdx].Trim();
                var modMetadata = ModLister.AllMods.FirstOrDefault(x =>
                    string.Equals(x.ModName.StripInvaildChars(), modName, StringComparison.CurrentCultureIgnoreCase));
                if (modMetadata != null)
                {
                    return modMetadata;
                }

                var id = fileName[(dashIdx + 1)..].Trim();
                modMetadata = ModLister.AllMods.FirstOrDefault(x => x.Id == id);
                if (modMetadata != null)
                {
                    return modMetadata;
                }
            }
            // Step 2 (ModName)
            {
                var modName = fileName;
                var modMetadata = ModLister.AllMods.FirstOrDefault(x =>
                    string.Equals(x.ModName.StripInvaildChars(), modName, StringComparison.CurrentCultureIgnoreCase));
                if (modMetadata != null)
                {
                    return modMetadata;
                }
            }
            // Step 3 (Id)
            if (long.TryParse(fileName, out _))
            {
                var modMetadata = ModLister.AllMods.FirstOrDefault(x => x.Id == fileName);
                if (modMetadata != null)
                {
                    return modMetadata;
                }
            }
            // Step 4 (FolderName)
            var dirName = Path.GetFileName(Path.GetDirectoryName(filePath));
            if (dirName == null)
            {
                return null;
            }
            // Step 4-1: (ModName - Id)
            dashIdx = dirName.LastIndexOf('-');
            if (dashIdx > 0)
            {
                var modName = dirName[..dashIdx];
                var modMetadata = ModLister.AllMods.FirstOrDefault(x =>
                    string.Equals(x.ModName.StripInvaildChars(), modName, StringComparison.CurrentCultureIgnoreCase));
                if (modMetadata != null)
                {
                    return modMetadata;
                }

                var id = dirName[(dashIdx + 1)..].Trim();
                modMetadata = ModLister.AllMods.FirstOrDefault(x => x.Id == id);
                if (modMetadata != null)
                {
                    return modMetadata;
                }
            }
            // Step 4-2 (ModName)
            {
                var modName = dirName;
                var modMetadata = ModLister.AllMods.FirstOrDefault(x =>
                    string.Equals(x.ModName.StripInvaildChars(), modName, StringComparison.CurrentCultureIgnoreCase));
                if (modMetadata != null)
                {
                    return modMetadata;
                }
            }
            // Step 3 (Id)
            if (long.TryParse(dirName, out _))
            {
                var modMetadata = ModLister.AllMods.FirstOrDefault(x => x.Id == dirName);
                if (modMetadata != null)
                {
                    return modMetadata;
                }
            }

            return null;
        }

        private static readonly Dictionary<string, ModMetadata?> ModMetadatasByFilePath = new();
    }
}
