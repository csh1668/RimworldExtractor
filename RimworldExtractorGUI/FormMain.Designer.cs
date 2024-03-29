namespace RimworldExtractorGUI
{
    partial class FormMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            buttonSelectMod = new Button();
            buttonExtract = new Button();
            button2 = new Button();
            buttonConvertXlsx = new Button();
            buttonConvertXml = new Button();
            label1 = new Label();
            label2 = new Label();
            richTextBoxLog = new RichTextBox();
            labelSelectedMods = new Label();
            button1 = new Button();
            linkLabelLatestVersion = new LinkLabel();
            buttonJpgPackager = new Button();
            buttonOpenTranslationAnalyzer = new Button();
            SuspendLayout();
            // 
            // buttonSelectMod
            // 
            buttonSelectMod.Location = new Point(12, 12);
            buttonSelectMod.Name = "buttonSelectMod";
            buttonSelectMod.Size = new Size(200, 46);
            buttonSelectMod.TabIndex = 0;
            buttonSelectMod.Text = "1. 추출할 모드 선택";
            buttonSelectMod.UseVisualStyleBackColor = true;
            buttonSelectMod.Click += buttonSelectMod_Click;
            // 
            // buttonExtract
            // 
            buttonExtract.Enabled = false;
            buttonExtract.Location = new Point(12, 64);
            buttonExtract.Name = "buttonExtract";
            buttonExtract.Size = new Size(200, 46);
            buttonExtract.TabIndex = 1;
            buttonExtract.Text = "2. 번역 데이터 추출";
            buttonExtract.UseVisualStyleBackColor = true;
            buttonExtract.Click += buttonExtract_Click;
            // 
            // button2
            // 
            button2.Location = new Point(12, 220);
            button2.Name = "button2";
            button2.Size = new Size(200, 46);
            button2.TabIndex = 2;
            button2.Text = "옵션";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // buttonConvertXlsx
            // 
            buttonConvertXlsx.Location = new Point(12, 116);
            buttonConvertXlsx.Name = "buttonConvertXlsx";
            buttonConvertXlsx.Size = new Size(97, 46);
            buttonConvertXlsx.TabIndex = 3;
            buttonConvertXlsx.Text = "XML -> XLSX";
            buttonConvertXlsx.UseVisualStyleBackColor = true;
            buttonConvertXlsx.Click += buttonConvertXlsx_Click;
            // 
            // buttonConvertXml
            // 
            buttonConvertXml.Location = new Point(115, 116);
            buttonConvertXml.Name = "buttonConvertXml";
            buttonConvertXml.Size = new Size(97, 46);
            buttonConvertXml.TabIndex = 4;
            buttonConvertXml.Text = "XLSX -> XML";
            buttonConvertXml.UseVisualStyleBackColor = true;
            buttonConvertXml.Click += buttonConvertXml_Click;
            // 
            // label1
            // 
            label1.Location = new Point(218, 9);
            label1.Name = "label1";
            label1.Size = new Size(570, 23);
            label1.TabIndex = 5;
            label1.Text = "림월드의 공식 콘텐츠나 모드의 번역 데이터를 추출하는 프로그램입니다.\r\n";
            label1.TextAlign = ContentAlignment.TopCenter;
            // 
            // label2
            // 
            label2.Location = new Point(12, 271);
            label2.Name = "label2";
            label2.Size = new Size(776, 23);
            label2.TabIndex = 7;
            label2.Text = "Log";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // richTextBoxLog
            // 
            richTextBoxLog.BackColor = SystemColors.ControlText;
            richTextBoxLog.Location = new Point(12, 297);
            richTextBoxLog.Name = "richTextBoxLog";
            richTextBoxLog.ReadOnly = true;
            richTextBoxLog.ScrollBars = RichTextBoxScrollBars.ForcedVertical;
            richTextBoxLog.Size = new Size(776, 192);
            richTextBoxLog.TabIndex = 8;
            richTextBoxLog.Text = "";
            // 
            // labelSelectedMods
            // 
            labelSelectedMods.Location = new Point(218, 32);
            labelSelectedMods.Name = "labelSelectedMods";
            labelSelectedMods.Size = new Size(570, 182);
            labelSelectedMods.TabIndex = 9;
            labelSelectedMods.Text = resources.GetString("labelSelectedMods.Text");
            labelSelectedMods.TextAlign = ContentAlignment.TopCenter;
            // 
            // button1
            // 
            button1.Location = new Point(650, 268);
            button1.Name = "button1";
            button1.Size = new Size(138, 23);
            button1.TabIndex = 10;
            button1.Text = "경고나 에러 발생 시";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // linkLabelLatestVersion
            // 
            linkLabelLatestVersion.Location = new Point(12, 269);
            linkLabelLatestVersion.Name = "linkLabelLatestVersion";
            linkLabelLatestVersion.Size = new Size(200, 23);
            linkLabelLatestVersion.TabIndex = 11;
            linkLabelLatestVersion.TextAlign = ContentAlignment.MiddleLeft;
            linkLabelLatestVersion.LinkClicked += linkLabelLatestVersion_LinkClicked;
            // 
            // buttonJpgPackager
            // 
            buttonJpgPackager.Location = new Point(115, 168);
            buttonJpgPackager.Name = "buttonJpgPackager";
            buttonJpgPackager.Size = new Size(97, 46);
            buttonJpgPackager.TabIndex = 12;
            buttonJpgPackager.Text = "이미지 + 파일 합치기";
            buttonJpgPackager.UseVisualStyleBackColor = true;
            buttonJpgPackager.Click += buttonJpgPackager_Click;
            // 
            // buttonOpenTranslationAnalyzer
            // 
            buttonOpenTranslationAnalyzer.Location = new Point(12, 168);
            buttonOpenTranslationAnalyzer.Name = "buttonOpenTranslationAnalyzer";
            buttonOpenTranslationAnalyzer.Size = new Size(97, 46);
            buttonOpenTranslationAnalyzer.TabIndex = 13;
            buttonOpenTranslationAnalyzer.Text = "번역 분석기\r\n열기";
            buttonOpenTranslationAnalyzer.UseVisualStyleBackColor = true;
            buttonOpenTranslationAnalyzer.Click += buttonOpenTranslationAnalyzer_Click;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 496);
            Controls.Add(buttonOpenTranslationAnalyzer);
            Controls.Add(buttonJpgPackager);
            Controls.Add(linkLabelLatestVersion);
            Controls.Add(button1);
            Controls.Add(labelSelectedMods);
            Controls.Add(richTextBoxLog);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(buttonConvertXml);
            Controls.Add(buttonConvertXlsx);
            Controls.Add(button2);
            Controls.Add(buttonExtract);
            Controls.Add(buttonSelectMod);
            Name = "FormMain";
            Text = "Rimworld Extractor GUI (림추출기)";
            ResumeLayout(false);
        }

        #endregion

        private Button buttonSelectMod;
        private Button buttonExtract;
        private Button button2;
        private Button buttonConvertXlsx;
        private Button buttonConvertXml;
        private Label label1;
        private Label label2;
        private RichTextBox richTextBoxLog;
        private Label labelSelectedMods;
        private Button button1;
        private LinkLabel linkLabelLatestVersion;
        private Button buttonJpgPackager;
        private Button buttonOpenTranslationAnalyzer;
    }
}