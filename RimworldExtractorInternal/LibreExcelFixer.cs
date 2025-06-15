using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimworldExtractorInternal
{
    public class LibreExcelFixer : IDisposable
    {
        private readonly string _path;
        private readonly string _tmpRoot;
        private readonly string _tmpFilePath;

        public LibreExcelFixer(string path)
        {
            _path = path;
            _tmpRoot = Path.Combine(Path.GetTempPath(), nameof(LibreExcelFixer), Guid.NewGuid().ToString());
            _tmpFilePath = MakeCopy();
        }

        public string DoFix()
        {
            // unzip excel file
            try
            {
                using var zip = ZipFile.Open(_tmpFilePath, ZipArchiveMode.Update);

                // 1. remove /xl/comments1.xml
                zip.GetEntry("xl/comments1.xml")?.Delete();

                // 2. /xl/worksheets/_rels/sheet1.xml.rels 에서 Target 어트리뷰트가 comments1.xml 인 Relationship 노드 삭제
                var relsEntry = zip.GetEntry("xl/worksheets/_rels/sheet1.xml.rels");
                if (relsEntry != null)
                {
                    using var relsStream = relsEntry.Open();
                    using var relsReader = new StreamReader(relsStream, Encoding.UTF8);
                    var relsContent = relsReader.ReadToEnd();

                    var target =
                        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/comments\" Target=\"../comments1.xml\"/>";

                    relsContent = relsContent.Replace(target, string.Empty);

                    // 저장
                    using var relsWriter = new StreamWriter(relsStream, Encoding.UTF8);
                    relsWriter.BaseStream.SetLength(0); // Clear the stream before writing

                    relsWriter.Write(relsContent);
                }
            }
            catch (Exception ex)
            {
                Log.Err($"Failed to fix LibreOffice Excel file {_path}: {ex.Message}");
                return null;
            }

            return _tmpFilePath;
        }

        private string MakeCopy()
        {
            if (!Directory.Exists(_tmpRoot)) Directory.CreateDirectory(_tmpRoot);

            var fileName = Path.GetFileName(_path);
            var tmpFilePath = Path.Combine(_tmpRoot, fileName);
            if (!File.Exists(tmpFilePath))
            {
                File.Copy(_path, tmpFilePath);
            }

            return tmpFilePath;
        }

        public void Dispose()
        {
            if (Directory.Exists(_tmpRoot))
            {
                try
                {
                    Directory.Delete(_tmpRoot, true);
                }
                catch (Exception ex)
                {
                    Log.Err($"Failed to delete temporary directory {_tmpRoot}: {ex.Message}");
                }
            }
        }
    }
}
