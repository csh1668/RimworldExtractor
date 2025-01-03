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
        public ModMetadata? SelectedMod;
        public List<ExtractableFolder>? SelectedFolders;
        public List<ModMetadata>? ReferenceMods;

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
                MessageBox.Show("Prefabs.dat ������ ������ �������̰ų� �ջ�Ǿ����ϴ�. ���� ���� �� �ٽ� �������ּ���.\n" +
                                $"�����޽���: {e.Message}");
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
                            linkLabelLatestVersion.Text = $"{current} �ֽ� �����Դϴ�";
                        }
                        else
                        {
                            linkLabelLatestVersion.Text = $"{current} < {latest} �ֽ� ���� ��밡��";
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
                    Log.Wrn($"�ֽ� ���� Ȯ�ο� �����Ͽ����ϴ�: {e.Message}");
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

                labelSelectedMods.Text = $"���õ� ���: {SelectedMod.ModName}";
                if (ReferenceMods?.Count > 0)
                {
                    var concatText = string.Join(", ", ReferenceMods.Select(x => x.ModName));
                    var stripedText = concatText.Substring(0, Math.Min(concatText.Length, 200));
                    if (concatText.Length > 200)
                        stripedText += "...";
                    labelSelectedMods.Text += $"\n������ ���õ� ���: {concatText}";
                }
            }
        }

        private void buttonExtract_Click(object sender, EventArgs e)
        {
            if (ReferenceMods is null || SelectedFolders is null || SelectedMod is null)
            {
                return;
            }

            Log.Msg("���� ����...");

            var extraction = Extractor.ExtractTranslationData(SelectedMod, SelectedFolders, ReferenceMods);

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
            Log.Msg($"���� ������ ��: �� {extraction.Count}�� �� Defs {cntDefs}��, Keyed {cntKeyed}��, Strings {cntStrings}��, Patches {cntPatches}��, �Ϸ�!");

            var hasError = HasErrorAfter("���� ����...");

            if (hasError)
            {
                if (MessageBox.Show("�Ϸ�Ǿ����� ���� �� ������ �߻��Ͽ����ϴ�. �ƹ�ư ����� ������ ��ġ�� Ž����� �����?", "�Ϸ�?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start("explorer.exe", outPath);
                }
            }
            else
            {
                if (MessageBox.Show("�Ϸ�Ǿ����ϴ�! ����� ������ ��ġ�� Ž����� �����?", "�Ϸ�", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start("explorer.exe", outPath);
                }
            }
        }



        private void buttonConvertXml_Click(object sender, EventArgs e)
        {
            var openfileDialog = new OpenFileDialog();
            openfileDialog.Title = "�� ����⿡�� ������ ���� ������ �������ּ���.";
            openfileDialog.FileName = "";
            openfileDialog.Filter = "���� ������ ����|*.xlsx";

            if (openfileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var path = openfileDialog.FileName;
                    var fileName = Path.GetFileNameWithoutExtension(path);
                    var translations = IO.FromExcel(path);
                    IO.ToLanguageXml(translations, true, Prefabs.CommentOriginal, fileName, Path.GetDirectoryName(path) ?? "");
                    if (MessageBox.Show("�Ϸ�Ǿ����ϴ�! ��ȯ�� ������ ��ġ�� Ž����� �����?", "�Ϸ�", MessageBoxButtons.YesNo) == DialogResult.Yes)
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
                for (var i = 0; i < roots.Length; i++)
                {
                    var root = roots[i];
                    var translations = IO.FromLanguageXml(root);
                    IO.ToExcel(translations, Path.Combine(root, Path.GetFileNameWithoutExtension(root)));
                    Log.Msg($"{i + 1}/{roots.Length}::���� �Ϸ�: {root}");
                }

                MessageBox.Show("��ȯ�� �Ϸ�Ǿ����ϴ�!");
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
            Process.Start("explorer.exe", GithubVersionCheker.DiscussionUrl);
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
                        string newPath;
                        var analyzerEntry = analyzerEntries[i];
                        switch (analyzerEntry.SaveMethod)
                        {
                            case TranslationAnalyzerEntry.SaveMethodEnum.Append:
                                IO.ModifyExcel(analyzerEntry.Changes.ToList(), analyzerEntry.FilePath);
                                break;
                            case TranslationAnalyzerEntry.SaveMethodEnum.Overwrite:
                                analyzerEntry.MergeTranslation();
                                IO.ToExcel(analyzerEntry.NewTranslations!,
                                    Path.Combine(Path.GetDirectoryName(analyzerEntry.FilePath),
                                        Path.GetFileNameWithoutExtension(analyzerEntry.FilePath)));
                                break;
                            case TranslationAnalyzerEntry.SaveMethodEnum.RewriteNewFile:
                                analyzerEntry.MergeTranslation();
                                newPath = Path.Combine(
                                    Path.GetDirectoryName(analyzerEntry.FilePath),
                                    Path.GetFileNameWithoutExtension(analyzerEntry.FilePath) + "- ������");
                                IO.ToExcel(analyzerEntry.NewTranslations, newPath, true);
                                Log.Msg($"{i + 1}/{analyzerEntries.Count}::���� �Ϸ�: {newPath}");
                                continue;
                            case TranslationAnalyzerEntry.SaveMethodEnum.New:
                                newPath = Path.Combine(
                                    Path.GetDirectoryName(analyzerEntry.FilePath),
                                    Path.GetFileNameWithoutExtension(analyzerEntry.FilePath) + "- ������");
                                IO.ToExcel(
                                    analyzerEntry.Changes
                                        .Where(x => x.Reason == TranslationAnalyzerEntry.ChangeReason.AddedNewly)
                                        .Select(x => x.New).ToList(), newPath);
                                Log.Msg($"{i + 1}/{analyzerEntries.Count}::���� �Ϸ�: {newPath}");
                                continue;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        Log.Msg($"{i + 1}/{analyzerEntries.Count}::���� �Ϸ�: {analyzerEntry.FilePath}");
                    }

                    MessageBox.Show($"{analyzerEntries.Count}���� ���Ͽ� ���� ������ �Ϸ�Ǿ����ϴ�.");
                }
            }
        }
    }
}