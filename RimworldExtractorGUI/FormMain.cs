using DocumentFormat.OpenXml.Spreadsheet;
using RimworldExtractorInternal;
using RimworldExtractorInternal.Records;
using System.Diagnostics;
using System.Xml;
using RimworldExtractorInternal.Compats;

namespace RimworldExtractorGUI
{
    public partial class FormMain : Form
    {
        public ModMetadata? SelectedMod { get; private set; }
        public List<ExtractableFolder>? SelectedFolders { get; private set; }
        public List<ModMetadata>? ReferenceMods { get; private set; }

        public FormMain()
        {
            InitializeComponent();
            Log.Out = new RichTextBoxWriter(richTextBoxLog);
            Prefabs.StopCallbackXlsx = FormStopCallback.StopCallbackXlsx;
            Prefabs.StopCallbackXml = FormStopCallback.StopCallbackXml;
            Prefabs.StopCallbackTxt = FormStopCallback.StopCallbackTxt;
            try
            {
                Prefabs.Load();
            }
            catch (Exception e)
            {
                MessageBox.Show("Prefabs.dat 파일의 버전이 구버전이거나 손상되었습니다. 파일 삭제 후 다시 진행해주세요.\n" +
                                $"에러메시지: {e.Message}");
                Close();
                throw;
            }

            Task.Factory.StartNew(() =>
            {
                try
                {
                    var latest = GithubVersionCheker.GetLatest();
                    var current = Program.VERSION;
                    var action = () =>
                    {
                        if (latest == current)
                        {
                            linkLabelLatestVersion.Text = $"{current} 최신 버전입니다";
                        }
                        else
                        {
                            linkLabelLatestVersion.Text = $"{current} < {latest} 최신 버전 사용가능";
                        }
                    };
                    if (linkLabelLatestVersion.InvokeRequired)
                    {
                        linkLabelLatestVersion.Invoke(action);
                    }
                    else
                    {
                        action();
                    }
                }
                catch (Exception e)
                {
                    Log.Wrn($"최신 버전 확인에 실패하였습니다: {e.Message}");
                }
            });
        }

        private static bool HasErrorAfter(string keyword)
        {
            var messages = Log.Messages.ToList();
            int i = messages.LastIndexOf(keyword);
            if (i == -1)
                return false;
            
            for (; i < messages.Count; i++)
            {
                var cur = messages[i];
                if (cur.Contains(Log.PrefixError))
                {
                    return true;
                }
            }

            return false;
        }

        private void buttonSelectMod_Click(object sender, EventArgs e)
        {
            var formSelectMod = new FormSelectMod();
            formSelectMod.StartPosition = FormStartPosition.CenterParent;
            if (formSelectMod.ShowDialog(this) == DialogResult.OK)
            {
                SelectedMod = formSelectMod.SelectedMod!;
                ReferenceMods = formSelectMod.ReferenceMods.Except(Enumerable.Repeat(SelectedMod, 1)).ToList();
                SelectedFolders = formSelectMod.SelectedFolders;
                buttonExtract.Enabled = true;
                Extractor.Reset();

                labelSelectedMods.Text = $"선택된 모드: {SelectedMod.ModName}";
                if (ReferenceMods?.Count > 1)
                {
                    var concatText = string.Join(", ", ReferenceMods.Select(x => x.ModName));
                    var stripedText = concatText.Substring(0, Math.Min(concatText.Length, 200));
                    if (concatText.Length > 200)
                        stripedText += "...";
                    labelSelectedMods.Text += $"\n참조로 선택된 모드: {concatText}";
                }
            }
        }

        private void buttonExtract_Click(object sender, EventArgs e)
        {
            if (ReferenceMods is null || SelectedFolders is null || SelectedMod is null)
            {
                return;
            }

            Log.Msg("추출 시작...");

            var refDefs = new List<string>();
            foreach (var referenceMod in ReferenceMods)
            {
                refDefs.AddRange(from extractableFolder in ModLister.GetExtractableFolders(referenceMod)
                                 where (extractableFolder.VersionInfo == "default" || extractableFolder.VersionInfo == Prefabs.CurrentVersion)
                                       && Path.GetFileName(extractableFolder.FolderName) == "Defs"
                                 select Path.Combine(referenceMod.RootDir, extractableFolder.FolderName));
            }

            var extraction = new List<TranslationEntry>();
            Extractor.Reset();
            var defs = SelectedFolders.Where(x => Path.GetFileName(x.FolderName) == "Defs").ToList();
            if (defs.Count > 0)
            {
                Extractor.PrepareDefs(defs, refDefs);
                extraction.AddRange(Extractor.ExtractDefs());
            }
            foreach (var extractableFolder in SelectedFolders)
            {
                switch (Path.GetFileName(extractableFolder.FolderName))
                {
                    case "Defs":
                        break;
                    case "Keyed":
                        extraction.AddRange(Extractor.ExtractKeyed(extractableFolder));
                        break;
                    case "Strings":
                        extraction.AddRange(Extractor.ExtractStrings(extractableFolder));
                        break;
                    case "Patches":
                        extraction.AddRange(Extractor.ExtractPatches(extractableFolder));
                        break;
                    default:
                        Log.Wrn($"지원하지 않는 폴더입니다. {extractableFolder.FolderName}");
                        continue;
                }
            }

            var outPath = SelectedMod.Identifier.StripInvaildChars();
            switch (Prefabs.Method)
            {
                case Prefabs.ExtractionMethod.Excel:
                    IO.ToExcel(extraction, Path.Combine(outPath, outPath));
                    break;
                case Prefabs.ExtractionMethod.Languages:
                    IO.ToLanguageXml(extraction, false, false, SelectedMod.Identifier.StripInvaildChars(), outPath);
                    break;
                case Prefabs.ExtractionMethod.LanguagesWithComments:
                    IO.ToLanguageXml(extraction, false, true, SelectedMod.Identifier.StripInvaildChars(), outPath);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            var (cntDefs, cntKeyed, cntStrings, cntPatches) = extraction.Count();
            Log.Msg($"번역 데이터 수: 총 {extraction.Count}개 중 Defs {cntDefs}개, Keyed {cntKeyed}개, Strings {cntStrings}개, Patches {cntPatches}개, 완료!");

            var hasError = HasErrorAfter("추출 시작...");

            if (hasError)
            {
                if (MessageBox.Show("완료되었지만 추출 중 에러가 발생하였습니다. 아무튼 추출된 파일의 위치를 탐색기로 열까요?", "완료?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start("explorer.exe", outPath);
                }
            }
            else
            {
                if (MessageBox.Show("완료되었습니다! 추출된 파일의 위치를 탐색기로 열까요?", "완료", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start("explorer.exe", outPath);
                }
            }
        }



        private void buttonConvertXml_Click(object sender, EventArgs e)
        {
            var openfileDialog = new OpenFileDialog();
            openfileDialog.Title = "림 추출기에서 생성한 엑셀 파일을 선택해주세요.";
            openfileDialog.FileName = "";
            openfileDialog.Filter = "번역 데이터 파일|*.xlsx";

            if (openfileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var path = openfileDialog.FileName;
                    var fileName = Path.GetFileNameWithoutExtension(path);
                    var translations = IO.FromExcel(path);
                    IO.ToLanguageXml(translations, true, Prefabs.CommentOriginal, fileName, Path.GetDirectoryName(path) ?? "");
                    if (MessageBox.Show("완료되었습니다! 변환된 폴더의 위치를 탐색기로 열까요?", "완료", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        Process.Start("explorer.exe", Path.GetDirectoryName(path) ?? "");
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    throw;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var form = new FormSettings();
            form.StartPosition = FormStartPosition.CenterParent;
            form.ShowDialog(this);

        }

        private void linkLabelLatestVersion_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("explorer.exe", GithubVersionCheker.LatestUrl);
        }

        private void buttonConvertXlsx_Click(object sender, EventArgs e)
        {
            var form = new FormXmlister();
            form.StartPosition = FormStartPosition.CenterParent;
            if (form.ShowDialog() == DialogResult.OK)
            {
                var roots = form.FileNames;
                foreach (var root in roots)
                {
                    var translations = IO.FromLanguageXml(root);
                    IO.ToExcel(translations, Path.Combine(root, Path.GetFileNameWithoutExtension(root)));
                }
            }
        }
    }
}