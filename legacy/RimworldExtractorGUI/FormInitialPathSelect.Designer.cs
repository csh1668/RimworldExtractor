namespace RimworldExtractorGUI
{
    partial class FormInitialPathSelect
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
            textBoxPathRimworld = new TextBox();
            textBoxPathWorkshop = new TextBox();
            buttonSelectPathRimworld = new Button();
            buttonSelectPathWorkshop = new Button();
            label1 = new Label();
            label2 = new Label();
            buttonDone = new Button();
            SuspendLayout();
            // 
            // textBoxPathRimworld
            // 
            textBoxPathRimworld.Location = new Point(129, 12);
            textBoxPathRimworld.Name = "textBoxPathRimworld";
            textBoxPathRimworld.Size = new Size(365, 23);
            textBoxPathRimworld.TabIndex = 0;
            // 
            // textBoxPathWorkshop
            // 
            textBoxPathWorkshop.Location = new Point(129, 41);
            textBoxPathWorkshop.Name = "textBoxPathWorkshop";
            textBoxPathWorkshop.Size = new Size(365, 23);
            textBoxPathWorkshop.TabIndex = 1;
            // 
            // buttonSelectPathRimworld
            // 
            buttonSelectPathRimworld.Location = new Point(500, 11);
            buttonSelectPathRimworld.Name = "buttonSelectPathRimworld";
            buttonSelectPathRimworld.Size = new Size(75, 23);
            buttonSelectPathRimworld.TabIndex = 2;
            buttonSelectPathRimworld.Text = "...";
            buttonSelectPathRimworld.UseVisualStyleBackColor = true;
            buttonSelectPathRimworld.Click += buttonSelectPathRimworld_Click;
            // 
            // buttonSelectPathWorkshop
            // 
            buttonSelectPathWorkshop.Location = new Point(500, 41);
            buttonSelectPathWorkshop.Name = "buttonSelectPathWorkshop";
            buttonSelectPathWorkshop.Size = new Size(75, 23);
            buttonSelectPathWorkshop.TabIndex = 3;
            buttonSelectPathWorkshop.Text = "...";
            buttonSelectPathWorkshop.UseVisualStyleBackColor = true;
            buttonSelectPathWorkshop.Click += buttonSelectPathWorkshop_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 15);
            label1.Name = "label1";
            label1.Size = new Size(99, 15);
            label1.TabIndex = 4;
            label1.Text = "림월드 경로 지정";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 41);
            label2.Name = "label2";
            label2.Size = new Size(111, 15);
            label2.TabIndex = 5;
            label2.Text = "창작마당 경로 지정";
            // 
            // buttonDone
            // 
            buttonDone.Location = new Point(12, 75);
            buttonDone.Name = "buttonDone";
            buttonDone.Size = new Size(563, 23);
            buttonDone.TabIndex = 6;
            buttonDone.Text = "완료";
            buttonDone.UseVisualStyleBackColor = true;
            buttonDone.Click += buttonDone_Click;
            // 
            // FormInitialPathSelect
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(587, 110);
            ControlBox = false;
            Controls.Add(buttonDone);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(buttonSelectPathWorkshop);
            Controls.Add(buttonSelectPathRimworld);
            Controls.Add(textBoxPathWorkshop);
            Controls.Add(textBoxPathRimworld);
            Name = "FormInitialPathSelect";
            Text = "림월드와 창작마당의 경로를 알려주세요";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TextBox textBoxPathRimworld;
        private TextBox textBoxPathWorkshop;
        private Button buttonSelectPathRimworld;
        private Button buttonSelectPathWorkshop;
        private Label label1;
        private Label label2;
        private Button buttonDone;
    }
}