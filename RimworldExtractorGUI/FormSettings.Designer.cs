namespace RimworldExtractorGUI
{
    partial class FormSettings
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
            buttonSelectPathRimworld = new Button();
            label1 = new Label();
            textBoxPathWorkshop = new TextBox();
            buttonSelectPathWorkshop = new Button();
            label2 = new Label();
            label3 = new Label();
            textBoxVersionPattern = new TextBox();
            label4 = new Label();
            label5 = new Label();
            buttonAutoDetect = new Button();
            textBoxRimworldVersion = new TextBox();
            label6 = new Label();
            label7 = new Label();
            comboBoxOriginalLanguage = new ComboBox();
            comboBoxTranslationLanguage = new ComboBox();
            label8 = new Label();
            comboBoxExtractionMethod = new ComboBox();
            buttonSaveAndClose = new Button();
            buttonCancel = new Button();
            buttonReset = new Button();
            label9 = new Label();
            label10 = new Label();
            label11 = new Label();
            comboBoxFileDuplication = new ComboBox();
            groupBox1 = new GroupBox();
            groupBox2 = new GroupBox();
            textBoxBaseRefList = new TextBox();
            buttonBaseRefList = new Button();
            label14 = new Label();
            groupBox3 = new GroupBox();
            buttonHelp4 = new Button();
            buttonHelp3 = new Button();
            buttonHelp2 = new Button();
            buttonHelp1 = new Button();
            textBoxFullListTranslation = new TextBox();
            textBoxNodeReplacement = new TextBox();
            label13 = new Label();
            label12 = new Label();
            textBoxTranslationHandles = new TextBox();
            textBoxExtractableTags = new TextBox();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // textBoxPathRimworld
            // 
            textBoxPathRimworld.Location = new Point(6, 45);
            textBoxPathRimworld.Name = "textBoxPathRimworld";
            textBoxPathRimworld.Size = new Size(395, 23);
            textBoxPathRimworld.TabIndex = 0;
            // 
            // buttonSelectPathRimworld
            // 
            buttonSelectPathRimworld.Location = new Point(407, 44);
            buttonSelectPathRimworld.Name = "buttonSelectPathRimworld";
            buttonSelectPathRimworld.Size = new Size(75, 23);
            buttonSelectPathRimworld.TabIndex = 1;
            buttonSelectPathRimworld.Text = "...";
            buttonSelectPathRimworld.UseVisualStyleBackColor = true;
            buttonSelectPathRimworld.Click += buttonSelectPathRimworld_Click;
            // 
            // label1
            // 
            label1.Location = new Point(6, 19);
            label1.Name = "label1";
            label1.Size = new Size(476, 23);
            label1.TabIndex = 2;
            label1.Text = "림월드 경로 지정:";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // textBoxPathWorkshop
            // 
            textBoxPathWorkshop.Location = new Point(6, 102);
            textBoxPathWorkshop.Name = "textBoxPathWorkshop";
            textBoxPathWorkshop.Size = new Size(395, 23);
            textBoxPathWorkshop.TabIndex = 3;
            // 
            // buttonSelectPathWorkshop
            // 
            buttonSelectPathWorkshop.Location = new Point(407, 102);
            buttonSelectPathWorkshop.Name = "buttonSelectPathWorkshop";
            buttonSelectPathWorkshop.Size = new Size(75, 23);
            buttonSelectPathWorkshop.TabIndex = 4;
            buttonSelectPathWorkshop.Text = "...";
            buttonSelectPathWorkshop.UseVisualStyleBackColor = true;
            buttonSelectPathWorkshop.Click += buttonSelectPathWorkshop_Click;
            // 
            // label2
            // 
            label2.Location = new Point(6, 71);
            label2.Name = "label2";
            label2.Size = new Size(476, 23);
            label2.TabIndex = 5;
            label2.Text = "창작마당 경로 지정:";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            label3.Location = new Point(6, 128);
            label3.Name = "label3";
            label3.Size = new Size(233, 23);
            label3.TabIndex = 6;
            label3.Text = "버전 패턴 정규식:";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // textBoxVersionPattern
            // 
            textBoxVersionPattern.Location = new Point(6, 153);
            textBoxVersionPattern.Name = "textBoxVersionPattern";
            textBoxVersionPattern.Size = new Size(233, 23);
            textBoxVersionPattern.TabIndex = 7;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 12);
            label4.Name = "label4";
            label4.Size = new Size(362, 15);
            label4.TabIndex = 8;
            label4.Text = "팁: 마우스를 상호작용란 위에 올려놓아 자세한 설명을 표시합니다\r\n";
            // 
            // label5
            // 
            label5.Location = new Point(249, 128);
            label5.Name = "label5";
            label5.Size = new Size(233, 23);
            label5.TabIndex = 9;
            label5.Text = "림월드 기본 버전:";
            label5.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // buttonAutoDetect
            // 
            buttonAutoDetect.Location = new Point(407, 153);
            buttonAutoDetect.Name = "buttonAutoDetect";
            buttonAutoDetect.Size = new Size(75, 23);
            buttonAutoDetect.TabIndex = 10;
            buttonAutoDetect.Text = "자동 감지";
            buttonAutoDetect.UseVisualStyleBackColor = true;
            buttonAutoDetect.Click += buttonAutoDetect_Click;
            // 
            // textBoxRimworldVersion
            // 
            textBoxRimworldVersion.Location = new Point(249, 154);
            textBoxRimworldVersion.Name = "textBoxRimworldVersion";
            textBoxRimworldVersion.Size = new Size(152, 23);
            textBoxRimworldVersion.TabIndex = 11;
            // 
            // label6
            // 
            label6.Location = new Point(6, 19);
            label6.Name = "label6";
            label6.Size = new Size(233, 23);
            label6.TabIndex = 12;
            label6.Text = "원본 언어:";
            label6.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label7
            // 
            label7.Location = new Point(249, 19);
            label7.Name = "label7";
            label7.Size = new Size(233, 23);
            label7.TabIndex = 15;
            label7.Text = "번역 언어:";
            label7.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // comboBoxOriginalLanguage
            // 
            comboBoxOriginalLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxOriginalLanguage.FormattingEnabled = true;
            comboBoxOriginalLanguage.Location = new Point(6, 45);
            comboBoxOriginalLanguage.Name = "comboBoxOriginalLanguage";
            comboBoxOriginalLanguage.Size = new Size(233, 23);
            comboBoxOriginalLanguage.TabIndex = 16;
            // 
            // comboBoxTranslationLanguage
            // 
            comboBoxTranslationLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxTranslationLanguage.FormattingEnabled = true;
            comboBoxTranslationLanguage.Location = new Point(245, 45);
            comboBoxTranslationLanguage.Name = "comboBoxTranslationLanguage";
            comboBoxTranslationLanguage.Size = new Size(233, 23);
            comboBoxTranslationLanguage.TabIndex = 17;
            // 
            // label8
            // 
            label8.Location = new Point(6, 71);
            label8.Name = "label8";
            label8.Size = new Size(233, 23);
            label8.TabIndex = 18;
            label8.Text = "추출 형식:";
            label8.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // comboBoxExtractionMethod
            // 
            comboBoxExtractionMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxExtractionMethod.FormattingEnabled = true;
            comboBoxExtractionMethod.Location = new Point(6, 97);
            comboBoxExtractionMethod.Name = "comboBoxExtractionMethod";
            comboBoxExtractionMethod.Size = new Size(233, 23);
            comboBoxExtractionMethod.TabIndex = 19;
            // 
            // buttonSaveAndClose
            // 
            buttonSaveAndClose.Location = new Point(540, 353);
            buttonSaveAndClose.Name = "buttonSaveAndClose";
            buttonSaveAndClose.Size = new Size(248, 46);
            buttonSaveAndClose.TabIndex = 20;
            buttonSaveAndClose.Text = "저장 후 닫기";
            buttonSaveAndClose.UseVisualStyleBackColor = true;
            buttonSaveAndClose.Click += buttonSaveAndClose_Click;
            // 
            // buttonCancel
            // 
            buttonCancel.Location = new Point(794, 353);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(114, 46);
            buttonCancel.TabIndex = 21;
            buttonCancel.Text = "취소";
            buttonCancel.UseVisualStyleBackColor = true;
            buttonCancel.Click += buttonCancel_Click;
            // 
            // buttonReset
            // 
            buttonReset.Location = new Point(914, 353);
            buttonReset.Name = "buttonReset";
            buttonReset.Size = new Size(114, 46);
            buttonReset.TabIndex = 22;
            buttonReset.Text = "기본값으로\r\n초기화";
            buttonReset.UseVisualStyleBackColor = true;
            buttonReset.Click += buttonReset_Click;
            // 
            // label9
            // 
            label9.Location = new Point(6, 19);
            label9.Name = "label9";
            label9.Size = new Size(233, 23);
            label9.TabIndex = 23;
            label9.Text = "추출 가능한 태그:";
            label9.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label10
            // 
            label10.Location = new Point(249, 19);
            label10.Name = "label10";
            label10.Size = new Size(233, 23);
            label10.TabIndex = 24;
            label10.Text = "Translation Handle 태그:";
            label10.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label11
            // 
            label11.Location = new Point(249, 71);
            label11.Name = "label11";
            label11.Size = new Size(233, 23);
            label11.TabIndex = 25;
            label11.Text = "저장 중 파일 중복 발생 시:";
            label11.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // comboBoxFileDuplication
            // 
            comboBoxFileDuplication.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxFileDuplication.FormattingEnabled = true;
            comboBoxFileDuplication.Location = new Point(245, 97);
            comboBoxFileDuplication.Name = "comboBoxFileDuplication";
            comboBoxFileDuplication.Size = new Size(233, 23);
            comboBoxFileDuplication.TabIndex = 26;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(textBoxPathRimworld);
            groupBox1.Controls.Add(buttonSelectPathRimworld);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(textBoxPathWorkshop);
            groupBox1.Controls.Add(buttonSelectPathWorkshop);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(textBoxRimworldVersion);
            groupBox1.Controls.Add(buttonAutoDetect);
            groupBox1.Controls.Add(textBoxVersionPattern);
            groupBox1.Location = new Point(12, 13);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(488, 187);
            groupBox1.TabIndex = 27;
            groupBox1.TabStop = false;
            groupBox1.Text = "림월드 관련 설정";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(textBoxBaseRefList);
            groupBox2.Controls.Add(buttonBaseRefList);
            groupBox2.Controls.Add(label14);
            groupBox2.Controls.Add(label6);
            groupBox2.Controls.Add(comboBoxOriginalLanguage);
            groupBox2.Controls.Add(comboBoxFileDuplication);
            groupBox2.Controls.Add(label7);
            groupBox2.Controls.Add(label11);
            groupBox2.Controls.Add(comboBoxTranslationLanguage);
            groupBox2.Controls.Add(label8);
            groupBox2.Controls.Add(comboBoxExtractionMethod);
            groupBox2.Location = new Point(12, 206);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(488, 191);
            groupBox2.TabIndex = 28;
            groupBox2.TabStop = false;
            groupBox2.Text = "추출/저장 관련 기본 설정";
            // 
            // textBoxBaseRefList
            // 
            textBoxBaseRefList.Location = new Point(6, 147);
            textBoxBaseRefList.Name = "textBoxBaseRefList";
            textBoxBaseRefList.Size = new Size(395, 23);
            textBoxBaseRefList.TabIndex = 12;
            // 
            // buttonBaseRefList
            // 
            buttonBaseRefList.Location = new Point(407, 147);
            buttonBaseRefList.Name = "buttonBaseRefList";
            buttonBaseRefList.Size = new Size(75, 23);
            buttonBaseRefList.TabIndex = 13;
            buttonBaseRefList.Text = "...";
            buttonBaseRefList.UseVisualStyleBackColor = true;
            buttonBaseRefList.Click += buttonBaseRefList_Click;
            // 
            // label14
            // 
            label14.Location = new Point(6, 123);
            label14.Name = "label14";
            label14.Size = new Size(476, 23);
            label14.TabIndex = 12;
            label14.Text = "기본 참조 모드 리스트 경로 지정:";
            label14.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(buttonHelp4);
            groupBox3.Controls.Add(buttonHelp3);
            groupBox3.Controls.Add(buttonHelp2);
            groupBox3.Controls.Add(buttonHelp1);
            groupBox3.Controls.Add(textBoxFullListTranslation);
            groupBox3.Controls.Add(textBoxNodeReplacement);
            groupBox3.Controls.Add(label13);
            groupBox3.Controls.Add(label12);
            groupBox3.Controls.Add(textBoxTranslationHandles);
            groupBox3.Controls.Add(textBoxExtractableTags);
            groupBox3.Controls.Add(label9);
            groupBox3.Controls.Add(label10);
            groupBox3.Location = new Point(540, 13);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(488, 334);
            groupBox3.TabIndex = 29;
            groupBox3.TabStop = false;
            groupBox3.Text = "추출/저장 관련 심화 설정";
            // 
            // buttonHelp4
            // 
            buttonHelp4.Location = new Point(459, 140);
            buttonHelp4.Name = "buttonHelp4";
            buttonHelp4.Size = new Size(23, 23);
            buttonHelp4.TabIndex = 34;
            buttonHelp4.Text = "?";
            buttonHelp4.UseVisualStyleBackColor = true;
            buttonHelp4.Click += buttonHelp4_Click;
            // 
            // buttonHelp3
            // 
            buttonHelp3.Location = new Point(216, 140);
            buttonHelp3.Name = "buttonHelp3";
            buttonHelp3.Size = new Size(23, 23);
            buttonHelp3.TabIndex = 33;
            buttonHelp3.Text = "?";
            buttonHelp3.UseVisualStyleBackColor = true;
            buttonHelp3.Click += buttonHelp3_Click;
            // 
            // buttonHelp2
            // 
            buttonHelp2.Location = new Point(459, 19);
            buttonHelp2.Name = "buttonHelp2";
            buttonHelp2.Size = new Size(23, 23);
            buttonHelp2.TabIndex = 32;
            buttonHelp2.Text = "?";
            buttonHelp2.UseVisualStyleBackColor = true;
            buttonHelp2.Click += buttonHelp2_Click;
            // 
            // buttonHelp1
            // 
            buttonHelp1.Location = new Point(216, 19);
            buttonHelp1.Name = "buttonHelp1";
            buttonHelp1.Size = new Size(23, 23);
            buttonHelp1.TabIndex = 31;
            buttonHelp1.Text = "?";
            buttonHelp1.UseVisualStyleBackColor = true;
            buttonHelp1.Click += buttonHelp1_Click;
            // 
            // textBoxFullListTranslation
            // 
            textBoxFullListTranslation.Location = new Point(249, 166);
            textBoxFullListTranslation.MaxLength = 327670;
            textBoxFullListTranslation.Multiline = true;
            textBoxFullListTranslation.Name = "textBoxFullListTranslation";
            textBoxFullListTranslation.ScrollBars = ScrollBars.Vertical;
            textBoxFullListTranslation.Size = new Size(233, 92);
            textBoxFullListTranslation.TabIndex = 30;
            // 
            // textBoxNodeReplacement
            // 
            textBoxNodeReplacement.Location = new Point(6, 166);
            textBoxNodeReplacement.MaxLength = 327670;
            textBoxNodeReplacement.Multiline = true;
            textBoxNodeReplacement.Name = "textBoxNodeReplacement";
            textBoxNodeReplacement.ScrollBars = ScrollBars.Vertical;
            textBoxNodeReplacement.Size = new Size(233, 92);
            textBoxNodeReplacement.TabIndex = 29;
            // 
            // label13
            // 
            label13.Location = new Point(249, 140);
            label13.Name = "label13";
            label13.Size = new Size(233, 23);
            label13.TabIndex = 28;
            label13.Text = "Full-list Translation 태그:";
            label13.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label12
            // 
            label12.Location = new Point(6, 140);
            label12.Name = "label12";
            label12.Size = new Size(233, 23);
            label12.TabIndex = 27;
            label12.Text = "노드 대체 키워드:";
            label12.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // textBoxTranslationHandles
            // 
            textBoxTranslationHandles.Location = new Point(249, 44);
            textBoxTranslationHandles.MaxLength = 327670;
            textBoxTranslationHandles.Multiline = true;
            textBoxTranslationHandles.Name = "textBoxTranslationHandles";
            textBoxTranslationHandles.ScrollBars = ScrollBars.Vertical;
            textBoxTranslationHandles.Size = new Size(233, 92);
            textBoxTranslationHandles.TabIndex = 26;
            // 
            // textBoxExtractableTags
            // 
            textBoxExtractableTags.Location = new Point(6, 45);
            textBoxExtractableTags.MaxLength = 327670;
            textBoxExtractableTags.Multiline = true;
            textBoxExtractableTags.Name = "textBoxExtractableTags";
            textBoxExtractableTags.ScrollBars = ScrollBars.Vertical;
            textBoxExtractableTags.Size = new Size(233, 92);
            textBoxExtractableTags.TabIndex = 25;
            // 
            // FormSettings
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1040, 409);
            ControlBox = false;
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(buttonReset);
            Controls.Add(buttonCancel);
            Controls.Add(buttonSaveAndClose);
            Controls.Add(label4);
            Name = "FormSettings";
            Text = "설정";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBoxPathRimworld;
        private Button buttonSelectPathRimworld;
        private Label label1;
        private TextBox textBoxPathWorkshop;
        private Button buttonSelectPathWorkshop;
        private Label label2;
        private Label label3;
        private TextBox textBoxVersionPattern;
        private Label label4;
        private Label label5;
        private Button buttonAutoDetect;
        private TextBox textBoxRimworldVersion;
        private Label label6;
        private Label label7;
        private ComboBox comboBoxOriginalLanguage;
        private ComboBox comboBoxTranslationLanguage;
        private Label label8;
        private ComboBox comboBoxExtractionMethod;
        private Button buttonSaveAndClose;
        private Button buttonCancel;
        private Button buttonReset;
        private Label label9;
        private Label label10;
        private Label label11;
        private ComboBox comboBoxFileDuplication;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private GroupBox groupBox3;
        private TextBox textBoxTranslationHandles;
        private TextBox textBoxExtractableTags;
        private Label label12;
        private Label label13;
        private TextBox textBoxFullListTranslation;
        private TextBox textBoxNodeReplacement;
        private Button buttonHelp1;
        private Button buttonHelp4;
        private Button buttonHelp3;
        private Button buttonHelp2;
        private Label label14;
        private TextBox textBoxBaseRefList;
        private Button buttonBaseRefList;
    }
}