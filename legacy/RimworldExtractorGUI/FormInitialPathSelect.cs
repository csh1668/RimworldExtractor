using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using RimworldExtractorInternal;

namespace RimworldExtractorGUI
{
    public partial class FormInitialPathSelect : Form
    {
        public FormInitialPathSelect()
        {
            InitializeComponent();
            Prefabs.Init();
            textBoxPathRimworld.Text = Prefabs.PathRimworld;
            textBoxPathWorkshop.Text = Prefabs.PathWorkshop;
        }

        private void buttonSelectPathRimworld_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "RimWorldWin64.exe를 지정해주세요";
            dialog.FileName = "";
            dialog.Filter = "림월드 실행 파일|RimWorldWin64.exe";
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = false;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBoxPathRimworld.Text = Path.GetDirectoryName(dialog.FileName);
            }
        }

        private void buttonSelectPathWorkshop_Click(object sender, EventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Title = "림월드 창작마당 경로를 지정해주세요 => Steam\\steamapps\\workshop\\content\\294100";

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                textBoxPathWorkshop.Text = dialog.FileName;
            }
        }

        private void buttonDone_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Prefabs.PathRimworld = textBoxPathRimworld.Text;
            Prefabs.PathWorkshop = textBoxPathWorkshop.Text;
            Prefabs.Save();
            Close();
        }
    }
}
