using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RimworldExtractorGUI
{
    public partial class FormXmlister : Form
    {
        public string[] FileNames = Array.Empty<string>();
        public FormXmlister()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Multiselect = true;
            dialog.Title = "Languages 폴더가 있는 루트 폴더를 지정해주세요.";

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                textBox1.Text = string.Join('|', dialog.FileNames);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            FileNames = textBox1.Text.Split('|');
            Close();
        }
    }
}
