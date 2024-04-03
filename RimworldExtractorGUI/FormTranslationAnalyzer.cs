using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RimworldExtractorInternal;
using RimworldExtractorInternal.DataTypes;

namespace RimworldExtractorGUI
{
    public partial class FormTranslationAnalyzer : Form
    {
        private readonly List<ListViewItem> _items;

        public IEnumerable<TranslationAnalyzerEntry> Entries
        {
            get
            {
                return _items.Where(x => x.Checked && ((TranslationAnalyzerEntry)x.Tag).HasChanges)
                    .Select(x => (TranslationAnalyzerEntry)x.Tag);
            }
        }
        public FormTranslationAnalyzer(string[] paths)
        {
            InitializeComponent();
            _items = new List<ListViewItem>();
            Task.Factory.StartNew(() => { AnalyzeTranslation(paths); });
        }

        private void AnalyzeTranslation(string[] paths)
        {
            for (var i = 0; i < paths.Length; i++)
            {
                if (labelTitle.InvokeRequired)
                {
                    labelTitle.Invoke(() => { labelTitle.Text = $"번역 데이터를 분석하고 있습니다... {i}/{paths.Length}"; });
                }
                else
                {
                    labelTitle.Text = $"번역 데이터를 분석하고 있습니다... {i}/{paths.Length}";
                }

                var path = paths[i];
                var item = ConvertToItem(path);
                _items.Add(item);
                if (listViewResults.InvokeRequired)
                {
                    listViewResults.Invoke(() => { listViewResults.Items.Add(item); });
                }
                else
                {
                    listViewResults.Items.Add(item);
                }
            }

            if (labelTitle.InvokeRequired)
            {
                labelTitle.Invoke(() => { labelTitle.Text = "분석 완료!"; });
            }
            else
            {
                labelTitle.Text = "분석 완료!";
            }
        }

        private static ListViewItem ConvertToItem(string filePath)
        {
            var item = new ListViewItem();
            var analyzerEntry = new TranslationAnalyzerEntry(filePath);
            if (analyzerEntry.Metadata != null)
            {
                var autoSelectedExtractableFolders = ModLister.GetExtractableFolders(analyzerEntry.Metadata)
                    .Where(x => x.IsAutoSelectable()).ToList();

                var autoSelectedReferenceMods = new List<ModMetadata>();
                foreach (var modMetadata in ModLister.FindAllReferenceMods(analyzerEntry.Metadata))
                {
                    if (autoSelectedReferenceMods.Contains(modMetadata))
                        continue;
                    autoSelectedReferenceMods.Add(modMetadata);
                }
                analyzerEntry.ReExtract(autoSelectedExtractableFolders, autoSelectedReferenceMods);
            }
            item.Tag = analyzerEntry;
            var rowTextData = new[]
            {
                analyzerEntry.Metadata?.Identifier ?? "UNKNOWN",
                "...\\" + Path.Combine(Path.GetFileName(Path.GetDirectoryName(filePath) ?? ""), Path.GetFileName(filePath)), analyzerEntry.OriginalTranslations.Count.ToString(),
                analyzerEntry.ChangesString, analyzerEntry.Metadata == null ? "지정 필요": "자동", "덧붙이기"
            };
            item.SubItems.AddRange(rowTextData);
            item.Checked = analyzerEntry.HasChanges;
            return item;
        }

        private void listViewResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewResults.SelectedItems.Count == 0)
            {
                buttonOpenSelectMod.Enabled = false;
                labelModTitle.Text = "수정할 모드를 선택하세요.";
                return;
            }

            buttonOpenSelectMod.Enabled = true;
            var selected = listViewResults.SelectedItems[0];
            var analyzerEntry = (TranslationAnalyzerEntry)selected.Tag;
            labelModTitle.Text = analyzerEntry.Metadata?.ToString() ?? "원본 모드를 찾을 수 없었습니다. 수동으로 지정해주세요.";

        }

        private void buttonOpenSelectMod_Click(object sender, EventArgs e)
        {
            var curSelected = listViewResults.SelectedItems[0];
            var curEntry = (TranslationAnalyzerEntry)curSelected.Tag;
            var curMetaData = curEntry.Metadata;
            var form = new FormSelectMod(curMetaData);
            form.StartPosition = FormStartPosition.CenterParent;
            if (form.ShowDialog() == DialogResult.OK)
            {
                curEntry.Metadata = form.SelectedMod;
                curEntry.ResetChanges();
                curEntry.ReExtract(form.SelectedFolders, form.ReferenceMods);
                curSelected.SubItems[(int)Column.ModName] = new ListViewItem.ListViewSubItem()
                { Text = curEntry.Metadata?.Identifier ?? "UNKNOWN" };
                curSelected.SubItems[(int)Column.OriginalCount] = new ListViewItem.ListViewSubItem()
                { Text = curEntry.OriginalTranslations.Count.ToString() };
                curSelected.SubItems[(int)Column.ChangesCount] = new ListViewItem.ListViewSubItem()
                { Text = curEntry.ChangesString };
                curSelected.SubItems[(int)Column.ExtractionMethod] = new ListViewItem.ListViewSubItem()
                { Text = "수동" };
                curSelected.Selected = true;
            }
        }

        private void listViewResults_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (e.Item.Checked && ((TranslationAnalyzerEntry)e.Item.Tag).Metadata == null)
            {
                MessageBox.Show("이 파일의 원본 모드를 알 수 없으므로 재추출 할 수 없습니다.");
                e.Item.Checked = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem listViewItem in listViewResults.Items)
            {
                var entry = ((TranslationAnalyzerEntry)listViewItem.Tag);
                if (entry.Metadata == null || !entry.HasChanges)
                {
                    continue;
                }
                else
                {
                    listViewItem.Checked = true;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem listViewItem in listViewResults.Items)
            {
                listViewItem.Checked = false;
            }

        }

        private void listViewResults_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;
            var item = listViewResults.FocusedItem;
            if (item == null || !item.Bounds.Contains(e.Location))
                return;
            var entry = (TranslationAnalyzerEntry)item.Tag;
            var contextMenu = new ContextMenuStrip();

            var menuItem1 = new ToolStripMenuItem("엑셀 파일을 파일 탐색기에서 열기");
            menuItem1.Click += (o, args) =>
            {
                Process.Start("explorer.exe", Path.GetDirectoryName(entry.FilePath) ?? "");
            };
            contextMenu.Items.Add(menuItem1);

            if (entry.Metadata != null)
            {
                var menuItem2 = new ToolStripMenuItem("모드 루트 폴더를 파일 탐색기에서 열기");
                menuItem2.Click += (o, args) =>
                {
                    Process.Start("explorer.exe", Path.GetDirectoryName(entry.Metadata!.RootDir) ?? "");
                };
                contextMenu.Items.Add(menuItem2);
            }

            contextMenu.Show(MousePosition);
        }

        private enum Column
        {
            ModName = 1, FilePath, OriginalCount, ChangesCount, ExtractionMethod, SaveMethod
        }
    }
}
