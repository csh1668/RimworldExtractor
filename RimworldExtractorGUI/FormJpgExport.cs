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
using RimworldExtractorGUI.Export;
using System.Diagnostics;

namespace RimworldExtractorGUI
{
    public partial class FormJpgExport : Form
    {
        public FormJpgExport()
        {
            InitializeComponent();
        }

        private void buttonSelectPathImage_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "JPG 이미지 파일을 지정해주세요.";
            dialog.FileName = "";
            dialog.Filter = "JPG 파일|*.jpg";
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = false;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBoxPathImage.Text = dialog.FileName;
            }
        }

        private void buttonSelectPathFile_Click(object sender, EventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.Title = "패키징할 압축파일의 경로를 지정해주세요.";
            dialog.Filters.Add(new CommonFileDialogFilter("ZIP 압축파일", "*.zip"));


            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                textBoxPathFile.Text = dialog.FileName;
            }
        }

        private void buttonDone_Click(object sender, EventArgs e)
        {
            var filePath = textBoxPathFile.Text;
            var imgPath = string.IsNullOrEmpty(textBoxPathImage.Text) ? null : textBoxPathImage.Text;

            if (imgPath != null && !File.Exists(imgPath))
            {
                MessageBox.Show("경로 상에 이미지 파일이 존재하지 않거나 엑세스 권한이 없습니다.\n" +
                                "파일이 존재함에도 에러가 발생한다면 관리자 권한으로 실행하거나, 파일을 다른 위치로 옮긴 후 다시 시도해주세요.");
                return;
            }

            if (File.Exists(filePath))
            {

                var destPath = Path.Combine(Path.GetDirectoryName(filePath) ?? "",
                    Path.GetFileNameWithoutExtension(filePath) + ".jpg");
                JpgPackageHelper.Package(filePath, destPath, imgPath);
                if (MessageBox.Show("완료되었습니다! 패키징된 파일의 위치를 탐색기로 열까요?", "완료", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start("explorer.exe", Path.GetDirectoryName(destPath) ?? "");
                }

                if (textBoxPathFile.Text != filePath)
                {
                    File.Delete(filePath);
                }
            }
            else if (Directory.Exists(filePath))
            {
                var newFilePath = Path.Combine(Path.GetDirectoryName(filePath) ?? "",
                    Path.GetFileNameWithoutExtension(filePath) + ".zip");
                JpgPackageHelper.ZipDir(filePath, newFilePath);

                var destPath = Path.Combine(Path.GetDirectoryName(filePath) ?? "",
                    Path.GetFileNameWithoutExtension(filePath) + ".jpg");
                JpgPackageHelper.Package(newFilePath, destPath, imgPath);
                File.Delete(newFilePath);
                if (MessageBox.Show("완료되었습니다! 패키징된 파일의 위치를 탐색기로 열까요?", "완료", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start("explorer.exe", Path.GetDirectoryName(destPath) ?? "");
                }
            }
            else
            {
                MessageBox.Show("경로 상에 파일/폴더가 존재하지 않거나 엑세스 권한이 없습니다.\n" +
                                "파일이 존재함에도 에러가 발생한다면 관리자 권한으로 실행하거나, 파일을 다른 위치로 옮긴 후 다시 시도해주세요.");
            }
        }

        private void buttonSelectPathDir_Click(object sender, EventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Title = "패키징할 폴더의 경로를 지정해주세요.";

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                textBoxPathFile.Text = dialog.FileName;
            }
        }
    }
}
