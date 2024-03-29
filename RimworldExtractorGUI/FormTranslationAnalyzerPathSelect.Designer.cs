namespace RimworldExtractorGUI
{
    partial class FormTranslationAnalyzerPathSelect
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
            textBox1 = new TextBox();
            buttonSelectSingleFile = new Button();
            buttonSelectDir = new Button();
            label1 = new Label();
            button1 = new Button();
            SuspendLayout();
            // 
            // textBox1
            // 
            textBox1.Location = new Point(12, 12);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(401, 23);
            textBox1.TabIndex = 0;
            textBox1.TextChanged += textBox1_TextChanged;
            // 
            // buttonSelectSingleFile
            // 
            buttonSelectSingleFile.Location = new Point(419, 6);
            buttonSelectSingleFile.Name = "buttonSelectSingleFile";
            buttonSelectSingleFile.Size = new Size(75, 75);
            buttonSelectSingleFile.TabIndex = 1;
            buttonSelectSingleFile.Text = "엑셀 파일 선택";
            buttonSelectSingleFile.UseVisualStyleBackColor = true;
            buttonSelectSingleFile.Click += buttonSelectSingleFile_Click;
            // 
            // buttonSelectDir
            // 
            buttonSelectDir.Location = new Point(500, 6);
            buttonSelectDir.Name = "buttonSelectDir";
            buttonSelectDir.Size = new Size(75, 75);
            buttonSelectDir.TabIndex = 2;
            buttonSelectDir.Text = "엑셀 파일들이 있는 폴더 선택";
            buttonSelectDir.UseVisualStyleBackColor = true;
            buttonSelectDir.Click += buttonSelectDir_Click;
            // 
            // label1
            // 
            label1.Location = new Point(12, 38);
            label1.Name = "label1";
            label1.Size = new Size(401, 107);
            label1.TabIndex = 3;
            label1.Text = "Q. 번역 분석기가 뭔가요?\r\nA. 기존에 추출기로 추출했던 엑셀 파일(들)을 읽고 분석한 후, \r\n해당 모드의 번역이 모드 업데이트로 인해 새로 번역해야\r\n할 소요가 있는지 알려줍니다.\r\n번역을 해야 한다면, 기존의 엑셀 파일에다가 추가하는 방식으로,\r\n새로 번역해야 하는 노드를 추가해줍니다.\r\n";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // button1
            // 
            button1.Enabled = false;
            button1.Location = new Point(419, 87);
            button1.Name = "button1";
            button1.Size = new Size(156, 58);
            button1.TabIndex = 4;
            button1.Text = "선택 완료";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // FormTranslationAnalyzerPathSelect
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(587, 157);
            Controls.Add(button1);
            Controls.Add(label1);
            Controls.Add(buttonSelectDir);
            Controls.Add(buttonSelectSingleFile);
            Controls.Add(textBox1);
            Name = "FormTranslationAnalyzerPathSelect";
            Text = "엑셀 파일이 있는 파일 경로를 지정해주세요.";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBox1;
        private Button buttonSelectSingleFile;
        private Button buttonSelectDir;
        private Label label1;
        private Button button1;
    }
}