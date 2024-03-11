using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimworldExtractorGUI.Export
{
    internal static class ImageFilePackageHelper
    {
        public static void ZipDir(string sourcePath, string destPath)
        {
            ZipFile.CreateFromDirectory(sourcePath, destPath);
        }

        public static void Package(string filePath, string destPath, string? imagePath = null)
        {
            var fileBytes = File.ReadAllBytes(filePath);
            var imgBytes = imagePath == null ? SampleImage.GetBytes : File.ReadAllBytes(imagePath);
            var newBytes = new byte[imgBytes.Length + fileBytes.Length];
            Array.Copy(imgBytes, newBytes, imgBytes.Length);
            Array.Copy(fileBytes, 0, newBytes, imgBytes.Length, fileBytes.Length);
            File.WriteAllBytes(destPath, newBytes);
        }
        private static class SampleImage
        {
            private const string base64 =
                "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";

            public static byte[] GetBytes => Convert.FromBase64String(base64);
        }
    }
}
