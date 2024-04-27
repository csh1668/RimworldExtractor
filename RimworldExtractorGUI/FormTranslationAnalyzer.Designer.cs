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
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            columnHeader4 = new ColumnHeader();
            columnHeader5 = new ColumnHeader();
            columnHeader6 = new ColumnHeader();
            columnHeader7 = new ColumnHeader();
            columnHeader8 = new ColumnHeader();
            labelModTitle = new Label();
            labelTitle = new Label();
            panel1 = new Panel();
            buttonOpenSelectMod = new Button();
            button1 = new Button();
            button2 = new Button();
            button3 = new Button();
            label1 = new Label();
            comboBox1 = new ComboBox();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // listViewResults
            // 
            listViewResults.CheckBoxes = true;
            listViewResults.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader4, columnHeader5, columnHeader6, columnHeader7, columnHeader8 });
            listViewResults.FullRowSelect = true;
            listViewResults.Location = new Point(12, 44);
            listViewResults.Name = "listViewResults";
            listViewResults.Size = new Size(767, 356);
            listViewResults.TabIndex = 0;
            listViewResults.UseCompatibleStateImageBehavior = false;
            listViewResults.View = View.Details;
            listViewResults.ItemChecked += listViewResults_ItemChecked;
            listViewResults.SelectedIndexChanged += listViewResults_SelectedIndexChanged;
            listViewResults.MouseDown += listViewResults_MouseDown;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "선택";
            columnHeader1.Width = 40;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "모드 정보";
            columnHeader2.Width = 150;
            // 
            // columnHeader4
            // 
            columnHeader4.Text = "파일 이름";
            columnHeader4.Width = 170;
            // 
            // columnHeader5
            // 
            columnHeader5.Text = "원본 개수";
            columnHeader5.Width = 70;
            // 
            // columnHeader6
            // 
            columnHeader6.Text = "변동 사항";
            columnHeader6.Width = 160;
            // 
            // columnHeader7
            // 
            columnHeader7.Text = "재추출방법";
            columnHeader7.Width = 80;
            // 
            // columnHeader8
            // 
            columnHeader8.Text = "저장방법";
            columnHeader8.Width = 100;
            // 
            // labelModTitle
            // 
            labelModTitle.AutoSize = true;
            labelModTitle.Location = new Point(3, 9);
            labelModTitle.Name = "labelModTitle";
            labelModTitle.Size = new Size(150, 15);
            labelModTitle.TabIndex = 1;
            labelModTitle.Text = "수정할 모드를 선택하세요.";
            labelModTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelTitle
            // 
            labelTitle.Location = new Point(12, 18);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(300, 23);
            labelTitle.TabIndex = 2;
            labelTitle.Text = "label2";
            labelTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panel1
            // 
            panel1.AutoScroll = true;
            panel1.Controls.Add(labelModTitle);
            panel1.Location = new Point(785, 18);
            panel1.Name = "panel1";
            panel1.Size = new Size(280, 74);
            panel1.TabIndex = 3;
            // 
            // buttonOpenSelectMod
            // 
            buttonOpenSelectMod.Enabled = false;
            buttonOpenSelectMod.Location = new Point(785, 98);
            buttonOpenSelectMod.Name = "buttonOpenSelectMod";
            buttonOpenSelectMod.Size = new Size(280, 46);
            buttonOpenSelectMod.TabIndex = 4;
            buttonOpenSelectMod.Text = "재추출할 모드 직접 지정";
            buttonOpenSelectMod.UseVisualStyleBackColor = true;
            buttonOpenSelectMod.Click += buttonOpenSelectMod_Click;
            // 
            // button1
            // 
            button1.Location = new Point(785, 345);
            button1.Name = "button1";
            button1.Size = new Size(280, 55);
            button1.TabIndex = 5;
            button1.Text = "선택된 파일들 수정";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(634, 19);
            button2.Name = "button2";
            button2.Size = new Size(145, 23);
            button2.TabIndex = 6;
            button2.Text = "모두 해제";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // button3
            // 
            button3.Location = new Point(483, 18);
            button3.Name = "button3";
            button3.Size = new Size(145, 23);
            button3.TabIndex = 7;
            button3.Text = "가능한 모두 선택";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // label1
            // 
            label1.Location = new Point(785, 147);
            label1.Name = "label1";
            label1.Size = new Size(132, 23);
            label1.TabIndex = 8;
            label1.Text = "저장방법 지정:";
            label1.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // comboBox1
            // 
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.FormattingEnabled = true;
            comboBox1.Items.AddRange(new object[] { "덧붙이기", "재구성하기(덮어씌우기)", "재구성하기(새로만들기)", "추가된 노드만 새로만들기" });
            comboBox1.Location = new Point(885, 147);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(180, 23);
            comboBox1.TabIndex = 9;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // FormTranslationAnalyzer
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1077, 412);
            Controls.Add(comboBox1);
            Controls.Add(label1);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(buttonOpenSelectMod);
            Controls.Add(panel1);
            Controls.Add(labelTitle);
            Controls.Add(listViewResults);
            Name = "FormTranslationAnalyzer";
            Text = "번역 분석기";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private ListView listViewResults;
        private Label labelModTitle;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ColumnHeader columnHeader4;
        private ColumnHeader columnHeader5;
        private ColumnHeader columnHeader6;
        private ColumnHeader columnHeader7;
        private ColumnHeader columnHeader8;
        private Label labelTitle;
        private Panel panel1;
        private Button buttonOpenSelectMod;
        private Button button1;
        private Button button2;
        private Button button3;
        private Label label1;
        private ComboBox comboBox1;
    }
}