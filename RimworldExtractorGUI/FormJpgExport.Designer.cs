namespace RimworldExtractorGUI
{
    partial class FormJpgExport
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
            buttonDone = new Button();
            label2 = new Label();
            label1 = new Label();
            buttonSelectPathFile = new Button();
            buttonSelectPathImage = new Button();
            textBoxPathFile = new TextBox();
            textBoxPathImage = new TextBox();
            label3 = new Label();
            buttonSelectPathDir = new Button();
            SuspendLayout();
            // 
            // buttonDone
            // 
            buttonDone.Location = new Point(12, 128);
            buttonDone.Name = "buttonDone";
            buttonDone.Size = new Size(563, 23);
            buttonDone.TabIndex = 13;
            buttonDone.Text = "완료";
            buttonDone.UseVisualStyleBackColor = true;
            buttonDone.Click += buttonDone_Click;
            // 
            // label2
            // 
            label2.Location = new Point(3, 41);
            label2.Name = "label2";
            label2.Size = new Size(144, 23);
            label2.TabIndex = 12;
            label2.Text = "합칠 파일/폴더 경로 지정";
            label2.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            label1.Location = new Point(3, 13);
            label1.Name = "label1";
            label1.Size = new Size(135, 23);
            label1.TabIndex = 11;
            label1.Text = "이미지 경로 지정 (선택)";
            label1.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // buttonSelectPathFile
            // 
            buttonSelectPathFile.Location = new Point(488, 42);
            buttonSelectPathFile.Name = "buttonSelectPathFile";
            buttonSelectPathFile.Size = new Size(87, 23);
            buttonSelectPathFile.TabIndex = 10;
            buttonSelectPathFile.Text = "파일을 지정";
            buttonSelectPathFile.UseVisualStyleBackColor = true;
            buttonSelectPathFile.Click += buttonSelectPathFile_Click;
            // 
            // buttonSelectPathImage
            // 
            buttonSelectPathImage.Location = new Point(500, 12);
            buttonSelectPathImage.Name = "buttonSelectPathImage";
            buttonSelectPathImage.Size = new Size(75, 23);
            buttonSelectPathImage.TabIndex = 9;
            buttonSelectPathImage.Text = "...";
            buttonSelectPathImage.UseVisualStyleBackColor = true;
            buttonSelectPathImage.Click += buttonSelectPathImage_Click;
            // 
            // textBoxPathFile
            // 
            textBoxPathFile.Location = new Point(153, 42);
            textBoxPathFile.Name = "textBoxPathFile";
            textBoxPathFile.Size = new Size(329, 23);
            textBoxPathFile.TabIndex = 8;
            // 
            // textBoxPathImage
            // 
            textBoxPathImage.Location = new Point(153, 13);
            textBoxPathImage.Name = "textBoxPathImage";
            textBoxPathImage.Size = new Size(341, 23);
            textBoxPathImage.TabIndex = 7;
            // 
            // label3
            // 
            label3.Location = new Point(12, 71);
            label3.Name = "label3";
            label3.Size = new Size(563, 54);
            label3.TabIndex = 16;
            label3.Text = "합칠 파일/폴더가 위치한 경로에 파일이 저장됩니다.\r\n이미지 경로가 지정되지 않으면 기본 이미지가 사용됩니다.\r\n폴더가 선택되었다면 자동으로 압축 후 합쳐집니다.";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // buttonSelectPathDir
            // 
            buttonSelectPathDir.Location = new Point(488, 71);
            buttonSelectPathDir.Name = "buttonSelectPathDir";
            buttonSelectPathDir.Size = new Size(87, 23);
            buttonSelectPathDir.TabIndex = 17;
            buttonSelectPathDir.Text = "폴더를 지정";
            buttonSelectPathDir.UseVisualStyleBackColor = true;
            buttonSelectPathDir.Click += buttonSelectPathDir_Click;
            // 
            // FormJpgExport
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(587, 163);
            Controls.Add(buttonSelectPathDir);
            Controls.Add(label3);
            Controls.Add(buttonDone);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(buttonSelectPathFile);
            Controls.Add(buttonSelectPathImage);
            Controls.Add(textBoxPathFile);
            Controls.Add(textBoxPathImage);
            Name = "FormJpgExport";
            Text = "이미지(JPG) + 파일 합치기 툴 (디시 업로드용)";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button buttonDone;
        private Label label2;
        private Label label1;
        private Button buttonSelectPathFile;
        private Button buttonSelectPathImage;
        private TextBox textBoxPathFile;
        private TextBox textBoxPathImage;
        private Label label3;
        private Button buttonSelectPathDir;
    }
}