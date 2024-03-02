using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using RimworldExtractorInternal;
using System.Windows.Controls;

namespace RimworldExtractorGUI
{
    public partial class FormStopCallback : Form
    {
        public FormStopCallback(string path)
        {
            InitializeComponent();
            label1.Text = path;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Yes; 
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;
            Close();
        }

        public static void StopCallbackXlsx(XLWorkbook xlsx, string path)
        {
            var form = new FormStopCallback(path);
            form.StartPosition = FormStartPosition.CenterScreen;
            if (form.ShowDialog() == DialogResult.Yes)
            {
                try
                {
                    xlsx.SaveAs(path);
                }
                catch (IOException io)
                {
                    Log.Err($"파일이 이미 사용 중이기 때문에 파일을 저장할 수 없었습니다. {io.Message}");
                }
            }
            else
            {
                return;
            }
        }

        public static void StopCallbackXml(XmlDocument doc, string path)
        {
            var form = new FormStopCallback(path);
            form.StartPosition = FormStartPosition.CenterScreen;
            if (form.ShowDialog() == DialogResult.Yes)
            {
                doc.Save(path);
            }
            else
            {
                return;
            }
        }

        public static void StopCallbackTxt(IEnumerable<string> lines, string path)
        {
            var form = new FormStopCallback(path);
            form.StartPosition = FormStartPosition.CenterScreen;
            if (form.ShowDialog() == DialogResult.Yes)
            {
                File.WriteAllLines(path, lines);
            }
            else
            {
                return;
            }
        }
    }
}
