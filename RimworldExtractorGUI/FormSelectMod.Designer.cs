namespace RimworldExtractorGUI
{
    partial class FormSelectMod
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
            components = new System.ComponentModel.Container();
            listBoxMods = new ListBox();
            textBoxSearch = new TextBox();
            listBoxExtractableFolders = new ListBox();
            buttonDone = new Button();
            label1 = new Label();
            labelSelectedMod = new Label();
            label2 = new Label();
            label3 = new Label();
            checkBoxFilterSelected = new CheckBox();
            toolTip1 = new ToolTip(components);
            panel1 = new Panel();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // listBoxMods
            // 
            listBoxMods.DrawMode = DrawMode.OwnerDrawFixed;
            listBoxMods.FormattingEnabled = true;
            listBoxMods.ItemHeight = 30;
            listBoxMods.Location = new Point(12, 91);
            listBoxMods.Name = "listBoxMods";
            listBoxMods.Size = new Size(488, 364);
            listBoxMods.TabIndex = 0;
            listBoxMods.DrawItem += listBoxMods_DrawItem;
            listBoxMods.SelectedIndexChanged += listBoxMods_SelectedIndexChanged;
            listBoxMods.KeyDown += listBoxMods_KeyDown;
            listBoxMods.MouseDown += listBoxMods_MouseDown;
            // 
            // textBoxSearch
            // 
            textBoxSearch.Location = new Point(12, 62);
            textBoxSearch.Name = "textBoxSearch";
            textBoxSearch.Size = new Size(488, 23);
            textBoxSearch.TabIndex = 1;
            textBoxSearch.TextChanged += textBoxSearch_TextChanged;
            // 
            // listBoxExtractableFolders
            // 
            listBoxExtractableFolders.DrawMode = DrawMode.OwnerDrawFixed;
            listBoxExtractableFolders.FormattingEnabled = true;
            listBoxExtractableFolders.HorizontalScrollbar = true;
            listBoxExtractableFolders.ItemHeight = 30;
            listBoxExtractableFolders.Location = new Point(506, 91);
            listBoxExtractableFolders.Margin = new Padding(3, 3, 8, 3);
            listBoxExtractableFolders.Name = "listBoxExtractableFolders";
            listBoxExtractableFolders.SelectionMode = SelectionMode.MultiSimple;
            listBoxExtractableFolders.Size = new Size(282, 304);
            listBoxExtractableFolders.TabIndex = 3;
            listBoxExtractableFolders.DrawItem += listBoxExtractableFolders_DrawItem;
            listBoxExtractableFolders.MouseMove += listBoxExtractableFolders_MouseMove;
            // 
            // buttonDone
            // 
            buttonDone.Location = new Point(506, 401);
            buttonDone.Name = "buttonDone";
            buttonDone.Size = new Size(282, 54);
            buttonDone.TabIndex = 4;
            buttonDone.Text = "선택 완료";
            buttonDone.UseVisualStyleBackColor = true;
            buttonDone.Click += buttonDone_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 458);
            label1.Name = "label1";
            label1.Size = new Size(768, 15);
            label1.TabIndex = 5;
            label1.Text = "조작법: '좌클릭' = 추출 모드로 선택, '우클릭' = 작업 메뉴 열기, 'A' = 파일 탐색기에서 열기, 'S' = 참조 모드로 선택, 'D' = 선택한 모드만 보기\r\n";
            // 
            // labelSelectedMod
            // 
            labelSelectedMod.AutoSize = true;
            labelSelectedMod.Location = new Point(3, 8);
            labelSelectedMod.Name = "labelSelectedMod";
            labelSelectedMod.Size = new Size(135, 15);
            labelSelectedMod.TabIndex = 6;
            labelSelectedMod.Text = "추출 모드를 선택하세요";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(149, 9);
            label2.Name = "label2";
            label2.Size = new Size(147, 15);
            label2.TabIndex = 7;
            label2.Text = "추출할 모드를 선택하세요";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(575, 73);
            label3.Name = "label3";
            label3.Size = new Size(147, 15);
            label3.TabIndex = 8;
            label3.Text = "추출할 폴더를 선택하세요";
            // 
            // checkBoxFilterSelected
            // 
            checkBoxFilterSelected.AutoSize = true;
            checkBoxFilterSelected.Location = new Point(370, 37);
            checkBoxFilterSelected.Name = "checkBoxFilterSelected";
            checkBoxFilterSelected.Size = new Size(130, 19);
            checkBoxFilterSelected.TabIndex = 9;
            checkBoxFilterSelected.Text = "선택한 모드만 보기";
            checkBoxFilterSelected.UseVisualStyleBackColor = true;
            checkBoxFilterSelected.CheckedChanged += checkBoxFilterSelected_CheckedChanged;
            // 
            // panel1
            // 
            panel1.AutoScroll = true;
            panel1.Controls.Add(labelSelectedMod);
            panel1.Location = new Point(506, 9);
            panel1.Name = "panel1";
            panel1.Size = new Size(282, 61);
            panel1.TabIndex = 10;
            // 
            // FormSelectMod
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 478);
            Controls.Add(panel1);
            Controls.Add(checkBoxFilterSelected);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(buttonDone);
            Controls.Add(listBoxExtractableFolders);
            Controls.Add(textBoxSearch);
            Controls.Add(listBoxMods);
            Name = "FormSelectMod";
            Text = "추출할 모드를 선택하세요";
            MouseDown += FormSelectMod_MouseDown;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListBox listBoxMods;
        private TextBox textBoxSearch;
        private ListBox listBoxExtractableFolders;
        private Button buttonDone;
        private Label label1;
        private Label labelSelectedMod;
        private Label label2;
        private Label label3;
        private CheckBox checkBoxFilterSelected;
        private ToolTip toolTip1;
        private Panel panel1;
    }
}