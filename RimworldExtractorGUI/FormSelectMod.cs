using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using Microsoft.Win32;
using RimworldExtractorInternal;
using RimworldExtractorInternal.DataTypes;
using ListBox = System.Windows.Controls.ListBox;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using ToolTip = System.Windows.Forms.ToolTip;

namespace RimworldExtractorGUI
{
    public partial class FormSelectMod : Form
    {
        public ModMetadata? SelectedMod { get; private set; }
        public List<ExtractableFolder> SelectedFolders { get; private set; }
        public List<ModMetadata> ReferenceMods { get; init; }

        private readonly List<ModMetadata> _officialModsCached;
        private readonly List<ModMetadata> _localModsCached;
        private readonly List<ModMetadata> _workshopModsCached;
        private readonly List<ModMetadata> _allModsCached;

        public FormSelectMod()
        {
            InitializeComponent();
            ModLister.ResetCache();
            _officialModsCached = ModLister.OfficialMods.ToList();
            _localModsCached = ModLister.LocalMods.ToList();
            _workshopModsCached = ModLister.WorkshopMods.ToList();
            _allModsCached = _officialModsCached.Concat(_localModsCached).Concat(_workshopModsCached).ToList();

            SelectedFolders = new List<ExtractableFolder>();
            ReferenceMods = new List<ModMetadata>();

            if (!string.IsNullOrEmpty(Prefabs.PathBaseRefList))
            {
                var lines = File.ReadAllLines(Prefabs.PathBaseRefList);
                foreach (var mod in _allModsCached)
                {
                    if (lines.Any(x => mod.Identifier == x))
                    {
                        ReferenceMods.Add(mod);
                    }
                }
            }

            ResetListBoxMods();
        }

        public FormSelectMod(ModMetadata? initSelectedMod) : this()
        {
            if (initSelectedMod == null)
                return;
            SelectedMod = initSelectedMod;
            listBoxMods.SelectedItem = initSelectedMod;
            listBoxMods.TopIndex = listBoxMods.SelectedIndex;
            var autoSelected = listBoxExtractableFolders.Items.OfType<ExtractableFolder>().Where(x => x.IsAutoSelectable()).ToList();

            foreach (ExtractableFolder extractableFolder in autoSelected)
            {
                listBoxExtractableFolders.SelectedItems.Add(extractableFolder);
            }
        }

        private void ResetListBoxMods(bool filterSelected = false)
        {
            listBoxMods.Items.Clear();
            var keyword = textBoxSearch.Text.ToLower();

            listBoxMods.Items.Add(CenteredText("==================== OFFICIAL ====================", 55));
            foreach (var officialContent in _officialModsCached)
            {
                if (string.IsNullOrEmpty(keyword) || officialContent.Identifier.ToLower().Contains(keyword))
                {
                    if (filterSelected && !ReferenceMods.Contains(officialContent) && SelectedMod != officialContent)
                        continue;
                    listBoxMods.Items.Add(officialContent);
                }
            }
            listBoxMods.Items.Add(CenteredText("==================== LOCAL MODS ====================", 55));
            foreach (var localMod in _localModsCached)
            {
                if (string.IsNullOrEmpty(keyword) || localMod.Identifier.ToLower().Contains(keyword))
                {
                    if (filterSelected && !ReferenceMods.Contains(localMod) && SelectedMod != localMod)
                        continue;
                    listBoxMods.Items.Add(localMod);
                }
            }
            listBoxMods.Items.Add(CenteredText("==================== WORKSHOP MODS ====================", 55));
            foreach (var workshopMod in _workshopModsCached)
            {
                if (string.IsNullOrEmpty(keyword) || workshopMod.Identifier.ToLower().Contains(keyword))
                {
                    if (filterSelected && !ReferenceMods.Contains(workshopMod) && SelectedMod != workshopMod)
                        continue;

                    listBoxMods.Items.Add(workshopMod);
                }
            }

            if (SelectedMod != null)
            {
                listBoxMods.SelectedItem = SelectedMod;
            }
        }

        private ModMetadata? SelectCurrentMod()
        {
            listBoxExtractableFolders.Items.Clear();
            var item = listBoxMods.SelectedItem as ModMetadata;
            if (item != null)
            {
                foreach (var extractableFolder in ModLister.GetExtractableFolders(item))
                {
                    listBoxExtractableFolders.Items.Add(extractableFolder);
                }
                var autoSelected = listBoxExtractableFolders.Items.OfType<ExtractableFolder>().Where(x => x.IsAutoSelectable()).ToList();

                foreach (ExtractableFolder extractableFolder in autoSelected)
                {
                    listBoxExtractableFolders.SelectedItems.Add(extractableFolder);
                }
            }

            return item;
        }

        private static string CenteredText(string text, int totalLen)
        {
            if (totalLen < text.Length)
            {
                totalLen = text.Length;
            }
            int padding = (totalLen - text.Length) / 2;
            string centeredText = text.PadLeft(padding + text.Length).PadRight(totalLen);
            return centeredText;
        }
        private static string GetModDisplayText(ModMetadata metadata)
        {
            if (metadata.IsOfficialContent)
            {
                return $"{CenteredText("Official", 26)}:::{CenteredText(metadata.ModName, 40)}";
            }
            else
            {
                return $"{CenteredText(metadata.Id, metadata.Id == "???" ? 26 : 20)}:::{CenteredText(metadata.ModName, 65)}";
            }
        }
        private static void OpenWithExplorer(ModMetadata item)
        {
            Process.Start("explorer.exe", item.RootDir);
        }

        private IEnumerable<ToolStripMenuItem> BaseMenus
        {
            get
            {
                var menuSaveRefModsList = new ToolStripMenuItem("(선택) 참조 모드 목록 저장");
                menuSaveRefModsList.Click += (sender, args) =>
                {
                    var saveFileDialog = new System.Windows.Forms.SaveFileDialog();
                    saveFileDialog.Title = "선택한 참조 모드의 목록을 저장합니다.";
                    saveFileDialog.InitialDirectory = Assembly.GetExecutingAssembly().Location;
                    saveFileDialog.Filter = "참조 모드 리스트 파일|*.refMods";
                    saveFileDialog.DefaultExt = "refMods";
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllLines(saveFileDialog.FileName, ReferenceMods.Select(x => x.Identifier));
                    }
                };
                yield return menuSaveRefModsList;
                var menuLoadRefModsList = new ToolStripMenuItem("(선택) 참조 모드 목록 불러오기");
                menuLoadRefModsList.Click += (sender, args) =>
                {
                    var openFileDialog = new OpenFileDialog();
                    openFileDialog.Title = "선택한 파일로부터 참조 모드의 목록을 불러옵니다.";
                    openFileDialog.InitialDirectory = Assembly.GetExecutingAssembly().Location;
                    openFileDialog.Filter = "참조 모드 리스트 파일|*.refMods";
                    openFileDialog.DefaultExt = "refMods";
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        ReferenceMods.Clear();
                        var lines = File.ReadAllLines(openFileDialog.FileName);
                        foreach (var mod in _allModsCached)
                        {
                            if (lines.Any(x => mod.Identifier == x))
                            {
                                ReferenceMods.Add(mod);
                            }
                        }

                        ResetListBoxMods();
                    }
                };
                yield return menuLoadRefModsList;
            }
        }

        private void textBoxSearch_TextChanged(object sender, EventArgs e)
        {
            ResetListBoxMods();
        }
        private void listBoxMods_SelectedIndexChanged(object sender, EventArgs e)
        {
            var currentMod = SelectCurrentMod();
            labelSelectedMod.Text = "추출할 모드를 선택하세요";
            buttonDone.Enabled = true;
            if (currentMod == null)
                return;
            SelectedMod = currentMod;
            labelSelectedMod.Text = currentMod.ModName;
            if (currentMod.ModDependencies is { Count: > 0 })
            {
                labelSelectedMod.Text += $"\n[선행모드: {string.Join(';', currentMod.ModDependencies)}]";
            }
        }
        private void buttonDone_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            foreach (ExtractableFolder extractableFolder in listBoxExtractableFolders.SelectedItems)
            {
                SelectedFolders.Add(extractableFolder);
            }
            Close();
        }
        private void listBoxMods_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;
            var idx = listBoxMods.IndexFromPoint(e.Location);
            var item = listBoxMods.Items[idx] as ModMetadata;
            if (item == null) return;
            var contextMenu = new ContextMenuStrip();

            var menuItem1 = new ToolStripMenuItem("추출 모드로 선택");
            menuItem1.Click += (o, args) =>
            {
                listBoxMods.SelectedIndex = idx;
                SelectedMod = SelectCurrentMod();
            };
            contextMenu.Items.Add(menuItem1);

            var menuItem2 = new ToolStripMenuItem("파일 탐색기에서 열기");
            menuItem2.Click += (o, args) => { OpenWithExplorer(item); };
            contextMenu.Items.Add(menuItem2);

            if (ReferenceMods.Contains(item))
            {
                var menuItem3 = new ToolStripMenuItem("참조 모드로 선택 해제");
                menuItem3.Click += (o, args) =>
                {
                    ReferenceMods.Remove(item);
                    listBoxMods.Invalidate(listBoxMods.GetItemRectangle(idx));
                };
                contextMenu.Items.Add(menuItem3);
            }
            else
            {
                var menuItem3 = new ToolStripMenuItem("참조 모드로 선택");
                menuItem3.Click += (o, args) =>
                {
                    ReferenceMods.Add(item);
                    listBoxMods.Invalidate(listBoxMods.GetItemRectangle(idx));
                };
                contextMenu.Items.Add(menuItem3);
            }

            var menuItem4 = new ToolStripMenuItem("이 모드와 관련된 모든 모드를 참조 모드로 선택");
            menuItem4.Click += (o, args) =>
            {
                var requiredMods = item.ModDependencies?.Select(x => _allModsCached.Find(y => y.PackageId == x))
                    .ToList();
                if (requiredMods == null)
                {
                    MessageBox.Show("이 모드는 선행 모드나 선택적 선행 모드가 없습니다!");
                    return;
                }

                foreach (var modMetadata in ModLister.FindAllReferenceMods(item))
                {
                    if (ReferenceMods.Contains(modMetadata))
                        continue;
                    ReferenceMods.Add(modMetadata);
                }

                listBoxMods.Refresh();
            };
            contextMenu.Items.Add(menuItem4);

            foreach (var toolStripMenuItem in BaseMenus)
            {
                contextMenu.Items.Add(toolStripMenuItem);
            }

            contextMenu.Show(MousePosition);
        }

        private void listBoxMods_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.A:
                    {
                        var item = SelectCurrentMod();
                        if (item == null) return;
                        OpenWithExplorer(item);
                        break;
                    }
                case Keys.S:
                    {
                        var mod = SelectCurrentMod();
                        var selectedItem = listBoxMods.SelectedItem as ModMetadata;
                        var selectedIdx = listBoxMods.SelectedIndex;
                        if (mod == null || selectedItem == null) return;
                        if (ReferenceMods.Contains(mod))
                        {
                            ReferenceMods.Remove(mod);
                            listBoxMods.Invalidate(listBoxMods.GetItemRectangle(selectedIdx));
                        }
                        else
                        {
                            ReferenceMods.Add(mod);
                            listBoxMods.Invalidate(listBoxMods.GetItemRectangle(selectedIdx));
                        }

                        break;
                    }
                case Keys.D:
                    checkBoxFilterSelected.Checked = !checkBoxFilterSelected.Checked;
                    break;
            }
        }
        private void listBoxMods_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index == -1)
                return;

            if (listBoxMods.Items[e.Index] is string sep)
            {
                e.DrawBackground();
                e.Graphics.DrawString(sep,
                    e.Font, Brushes.Black, e.Bounds, new StringFormat(StringFormatFlags.NoWrap));
                e.DrawFocusRectangle();
                return;
            }
            var curMod = (ModMetadata)listBoxMods.Items[e.Index];
            var text = GetModDisplayText(curMod);
            if (ReferenceMods.Any(x => curMod.RootDir == x.RootDir))
                text = $"(참조)" + text;

            e.DrawBackground();
            e.Graphics.DrawString(text,
                e.Font, Brushes.Black, e.Bounds, new StringFormat(StringFormatFlags.NoWrap));
            e.DrawFocusRectangle();
        }
        private void listBoxExtractableFolders_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index == -1)
                return;
            e.DrawBackground();
            e.Graphics.DrawString(listBoxExtractableFolders.Items[e.Index].ToString(),
                e.Font, Brushes.Black, e.Bounds, StringFormat.GenericDefault);
            e.DrawFocusRectangle();
        }
        private void checkBoxFilterSelected_CheckedChanged(object sender, EventArgs e)
        {
            ResetListBoxMods(checkBoxFilterSelected.Checked);
        }
        private void FormSelectMod_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;
            var contextMenu = new ContextMenuStrip();
            foreach (var toolStripMenuItem in BaseMenus)
            {
                contextMenu.Items.Add(toolStripMenuItem);
            }

            contextMenu.Show(MousePosition);
        }



        private void listBoxExtractableFolders_MouseMove(object sender, MouseEventArgs e)
        {
            var idx = listBoxExtractableFolders.IndexFromPoint(listBoxExtractableFolders.PointToClient(MousePosition));
            if (idx == -1)
            {
                toolTip1.Active = false;
                return;
            }
            if (listBoxExtractableFolders.Items[idx] is not ExtractableFolder item)
                return;

            toolTip1.Active = true;
            var prevToolTip = toolTip1.GetToolTip(listBoxExtractableFolders);
            var curToolTip = item.ToString();

            if (prevToolTip == curToolTip)
                return;
            toolTip1.SetToolTip(listBoxExtractableFolders, curToolTip);
        }
    }
}
