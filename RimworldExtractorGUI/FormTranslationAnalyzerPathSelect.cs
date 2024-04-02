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
using RimworldExtractorInternal;

namespace RimworldExtractorGUI
{
    public partial class FormTranslationAnalyzerPathSelect : Form
    {
        public string[] Paths = Array.Empty<string>();
        public FormTranslationAnalyzerPathSelect()
        {
            InitializeComponent();
        }

        private void buttonSelectSingleFile_Click(object sender, EventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.Title = "엑셀 파일(.xlsx) 파일을 선택해주세요.";
            dialog.Filters.Add(new CommonFileDialogFilter("림왈도 형식 엑셀 파일", "*.xlsx"));
            dialog.Multiselect = true;


            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                textBox1.Text = string.Join('|', dialog.FileNames);
            }
        }

        private void buttonSelectDir_Click(object sender, EventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Multiselect = true;
            dialog.Title = "엑셀 파일이 있는 루트 폴더를 선택해주세요.";

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                textBox1.Text = string.Join('|', dialog.FileNames);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = textBox1.Text.Length > 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            var tokens = textBox1.Text.Split('|');
            if (tokens.Length > 0 && Directory.Exists(tokens[0]))
            {
                tokens = tokens.SelectMany(TranslationAnalyzerTool.GetXlsxPaths).ToArray();
            }
            Paths = tokens;
            Close();
        }
    }
}
