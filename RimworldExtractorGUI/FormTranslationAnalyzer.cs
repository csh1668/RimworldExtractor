/*
 * 디자인 출처: https://hyokim.tistory.com/16
 */

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
    public partial class FormTranslationAnalyzer : Form
    {
        private string[] _paths;
        public FormTranslationAnalyzer(string[] paths)
        {
            InitializeComponent();
            _paths = paths;
            listViewResults.CheckBoxes = true;
            var testItem1 = new ListViewItem(new[] { string.Empty, "Hello", "World" });
            var testItem2 = new ListViewItem(new[] { string.Empty, "Hello2", "World2" });
            var testItem3 = new ListViewItem(new[] { string.Empty, "Hello3", "World3" });
            listViewResults.Items.AddRange(new[] { testItem1, testItem2, testItem3 });
            var columnCheck = new ColumnHeader();
            var columnHeader1 = new ColumnHeader();
            var columnHeader2 = new ColumnHeader();
            columnHeader1.Text = "항목1";
            columnHeader1.Width = 200;
            columnHeader2.Text = "항목2";
            columnHeader2.Width = 200;


            listViewResults.Columns.AddRange(new[] { columnCheck, columnHeader1, columnHeader2 });
        }

        private void listViewResults_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void listViewResults_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void listViewResults_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                e.DrawBackground();
                bool value = false;
                try
                {
                    value = Convert.ToBoolean(e.Header?.Tag);
                }
                catch (Exception)
                {
                    // ignored
                }

                CheckBoxRenderer.DrawCheckBox(e.Graphics, new Point(e.Bounds.Left + 4, e.Bounds.Top + 4),
                    value ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal :
                        System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);
            }
            else e.DrawDefault = true;
        }

        private void listViewResults_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == 0)
            {
                bool value = false;
                try
                {
                    value = Convert.ToBoolean(listViewResults.Columns[e.Column].Tag);
                }
                catch (Exception)
                {
                }
                listViewResults.Columns[e.Column].Tag = !value;
                foreach (ListViewItem item in listViewResults.Items) item.Checked = !value;
                listViewResults.Invalidate();
            }
        }
    }
}
