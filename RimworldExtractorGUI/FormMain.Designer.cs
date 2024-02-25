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
            button2.Location = new Point(12, 168);
            button2.Name = "button2";
            button2.Size = new Size(200, 46);
            button2.TabIndex = 2;
            button2.Text = "옵션";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // buttonConvertXlsx
            // 
            buttonConvertXlsx.Enabled = false;
            buttonConvertXlsx.Location = new Point(12, 116);
            buttonConvertXlsx.Name = "buttonConvertXlsx";
            buttonConvertXlsx.Size = new Size(97, 46);
            buttonConvertXlsx.TabIndex = 3;
            buttonConvertXlsx.Text = "XML -> XLSX";
            buttonConvertXlsx.UseVisualStyleBackColor = true;
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
            label2.Location = new Point(12, 217);
            label2.Name = "label2";
            label2.Size = new Size(776, 23);
            label2.TabIndex = 7;
            label2.Text = "Log";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // richTextBoxLog
            // 
            richTextBoxLog.BackColor = SystemColors.ControlText;
            richTextBoxLog.Location = new Point(12, 246);
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
            labelSelectedMods.Text = "선택된 모드가 없습니다.\r\n\r\n===기본적인 사용방법===\r\n1) '1. 추출할 모드 선택'을 통해 번역할 모드를 선택하세요.\r\n2) '2. 번역 데이터 추출'을 통해 번역 텍스트를 추출하세요.\r\n3) 추출된 엑셀 파일을 번역하세요.\r\n4) 'XLSX -> XML'을 통해 엑셀 파일을 배포용 XML 파일로 변환하세요.\r\n5) 끝!";
            labelSelectedMods.TextAlign = ContentAlignment.TopCenter;
            // 
            // button1
            // 
            button1.Location = new Point(650, 220);
            button1.Name = "button1";
            button1.Size = new Size(138, 23);
            button1.TabIndex = 10;
            button1.Text = "경고나 에러 발생 시";
            button1.UseVisualStyleBackColor = true;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
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
    }
}