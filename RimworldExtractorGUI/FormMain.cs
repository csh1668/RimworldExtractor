using DocumentFormat.OpenXml.Spreadsheet;
using RimworldExtractorInternal;
using System.Diagnostics;
using System.Xml;
using RimworldExtractorInternal.Compats;
using RimworldExtractorInternal.DataTypes;

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

                    void UpdateVersionText()
                    {
                        if (latest == current)
                        {
                            linkLabelLatestVersion.Text = $"{current} 최신 버전입니다";
                        }
                        else
                        {
                            linkLabelLatestVersion.Text = $"{current} < {latest} 최신 버전 사용가능";
                        }
                    }

                    if (linkLabelLatestVersion.InvokeRequired)
                    {
                        linkLabelLatestVersion.Invoke(UpdateVersionText);
                    }
                    else
                    {
                        UpdateVersionText();
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

            var extraction = Extractor.ExtractTranslationData(SelectedFolders, ReferenceMods);

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
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                var roots = form.FileNames;
                foreach (var root in roots)
                {
                    var translations = IO.FromLanguageXml(root);
                    IO.ToExcel(translations, Path.Combine(root, Path.GetFileNameWithoutExtension(root)));
                }
            }
        }

        private void buttonJpgPackager_Click(object sender, EventArgs e)
        {
            var form = new FormImageFileCombiner();
            form.StartPosition = FormStartPosition.CenterParent;
            if (form.ShowDialog(this) == DialogResult.OK)
            {

            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", GithubVersionCheker.IssueUrl);
        }

        private void buttonOpenTranslationAnalyzer_Click(object sender, EventArgs e)
        {
            var form = new FormTranslationAnalyzerPathSelect();
            form.StartPosition = FormStartPosition.CenterParent;
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                var paths = form.Paths;
                var analyzer = new FormTranslationAnalyzer(paths);
                analyzer.StartPosition = FormStartPosition.CenterParent;
                if (analyzer.ShowDialog(this) == DialogResult.OK)
                {
                    var analyzerEntries = analyzer.Entries.ToList();
                    for (var i = 0; i < analyzerEntries.Count; i++)
                    {
                        var analyzerEntry = analyzerEntries[i];
                        IO.AppendExcel(analyzerEntry.Changes.ToList(), analyzerEntry.FilePath);
                        Log.Msg($"{i + 1}/{analyzerEntries.Count}::수정 완료: {analyzerEntry.FilePath}");
                    }

                    MessageBox.Show($"{analyzerEntries.Count}개의 파일에 대한 수정이 완료되었습니다.");
                }
            }
        }
    }
}