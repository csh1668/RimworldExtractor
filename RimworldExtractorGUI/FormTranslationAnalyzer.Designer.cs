namespace RimworldExtractorGUI
{
    partial class FormTranslationAnalyzer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            listViewResults = new ListView();
            label1 = new Label();
            SuspendLayout();
            // 
            // listViewResults
            // 
            listViewResults.Location = new Point(12, 44);
            listViewResults.Name = "listViewResults";
            listViewResults.Size = new Size(522, 97);
            listViewResults.TabIndex = 0;
            listViewResults.UseCompatibleStateImageBehavior = false;
            listViewResults.View = View.Details;
            listViewResults.ColumnClick += listViewResults_ColumnClick;
            listViewResults.DrawColumnHeader += listViewResults_DrawColumnHeader;
            listViewResults.DrawItem += listViewResults_DrawItem;
            listViewResults.DrawSubItem += listViewResults_DrawSubItem;
            // 
            // label1
            // 
            label1.Location = new Point(540, 44);
            label1.Name = "label1";
            label1.Size = new Size(248, 23);
            label1.TabIndex = 1;
            label1.Text = "label1";
            label1.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // FormTranslationAnalyzer
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(label1);
            Controls.Add(listViewResults);
            Name = "FormTranslationAnalyzer";
            Text = "번역 분석기";
            ResumeLayout(false);
        }

        #endregion

        private ListView listViewResults;
        private Label label1;
    }
}