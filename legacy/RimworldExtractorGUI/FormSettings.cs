using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using RimworldExtractorInternal;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace RimworldExtractorGUI
{
    public partial class FormSettings : Form
    {
        public FormSettings()
        {
            InitializeComponent();
            if (File.Exists("Prefabs.dat"))
            {
                Prefabs.Load();
            }
            else
            {
                Prefabs.Save();
            }

            comboBoxOriginalLanguage.Items.AddRange(new object[]
            {
                "English", "Korean (한국어)", "Catalan (Català)", "ChineseSimplified (简体中文)", "ChineseTraditional (繁體中文)",
                "Czech (Čeština)", "Danish (Dansk)", "Dutch (Nederlands)", "Estonian (Eesti)",
                "Finnish (Suomi)", "French (Français)", "German (Deutsch)", "Greek (Ελληνικά)",
                "Hungarian (Magyar)", "Italian (Italiano)", "Japanese (日本語)", "Norwegian (Norsk Bokmål)",
                "Polish (Polski)", "Portuguese (Português)", "PortugueseBrazilian (Português Brasileiro)",
                "Romanian (Română)", "Russian (Русский)", "Slovak (Slovenčina)", "Spanish (Español(Castellano))",
                "SpanishLatin (Español(Latinoamérica))", "Swedish (Svenska)", "Turkish (Türkçe)",
                "Ukrainian (Українська)"
            });
            comboBoxTranslationLanguage.Items.AddRange(new object[]
            {
                "English", "Korean (한국어)", "Catalan (Català)", "ChineseSimplified (简体中文)", "ChineseTraditional (繁體中文)",
                "Czech (Čeština)", "Danish (Dansk)", "Dutch (Nederlands)", "Estonian (Eesti)",
                "Finnish (Suomi)", "French (Français)", "German (Deutsch)", "Greek (Ελληνικά)",
                "Hungarian (Magyar)", "Italian (Italiano)", "Japanese (日本語)", "Norwegian (Norsk Bokmål)",
                "Polish (Polski)", "Portuguese (Português)", "PortugueseBrazilian (Português Brasileiro)",
                "Romanian (Română)", "Russian (Русский)", "Slovak (Slovenčina)", "Spanish (Español(Castellano))",
                "SpanishLatin (Español(Latinoamérica))", "Swedish (Svenska)", "Turkish (Türkçe)",
                "Ukrainian (Українська)"
            });
            comboBoxExtractionMethod.Items.AddRange(new object[]
            {
                "번역 작업에 쓰일 엑셀(.xlsx) 파일",
                "배포 가능한 XML 파일",
                "배포 가능한 XML 파일(주석 포함)"
            });
            comboBoxFileDuplication.Items.AddRange(new object[]
            {
                "멈추고 묻기", "덮어씌우기", "건너뛰기"
            });

            FromPrefabs();
        }

        public void FromPrefabs()
        {
            checkBox1.Checked = Prefabs.EnableTkey; // TODO: REMOVE THIS AFTER

            textBoxPathRimworld.Text = Prefabs.PathRimworld;
            textBoxPathWorkshop.Text = Prefabs.PathWorkshop;
            textBoxVersionPattern.Text = Prefabs.PatternVersion;
            textBoxRimworldVersion.Text = Prefabs.CurrentVersion;

            comboBoxOriginalLanguage.SelectedItem = Prefabs.OriginalLanguage;
            comboBoxTranslationLanguage.SelectedItem = Prefabs.TranslationLanguage;
            comboBoxExtractionMethod.SelectedIndex = (int)Prefabs.Method;
            comboBoxFileDuplication.SelectedIndex = (int)Prefabs.Policy;
            textBoxBaseRefList.Text = Prefabs.PathBaseRefList;

            textBoxExtractableTags.Text = string.Join('/', Prefabs.ExtractableTags);
            textBoxTranslationHandles.Text = string.Join('/', Prefabs.TranslationHandles);
            textBoxNodeReplacement.Text = string.Join("/", Prefabs.NodeReplacement.Select(x => $"{x.Key}|{x.Value}"));
            textBoxFullListTranslation.Text = string.Join('/', Prefabs.FullListTranslationTags);
        }

        public void ToPrefabs()
        {
            Prefabs.EnableTkey = checkBox1.Checked; // TODO: REMOVE THIS AFTER

            Prefabs.PathRimworld = textBoxPathRimworld.Text;
            Prefabs.PathWorkshop = textBoxPathWorkshop.Text;
            Prefabs.PatternVersion = textBoxVersionPattern.Text;
            Prefabs.CurrentVersion = textBoxRimworldVersion.Text;

            Prefabs.OriginalLanguage = (string)comboBoxOriginalLanguage.SelectedItem;
            Prefabs.TranslationLanguage = (string)comboBoxTranslationLanguage.SelectedItem;
            Prefabs.Method = Enum.GetValues<Prefabs.ExtractionMethod>()[comboBoxExtractionMethod.SelectedIndex];
            Prefabs.Policy = Enum.GetValues<Prefabs.DuplicatesPolicy>()[comboBoxFileDuplication.SelectedIndex];
            Prefabs.PathBaseRefList = textBoxBaseRefList.Text;

            Prefabs.ExtractableTags = new HashSet<string>(RemoveSep(textBoxExtractableTags.Text).Split('/'));
            Prefabs.TranslationHandles = new List<string>(RemoveSep(textBoxTranslationHandles.Text).Split('/'));
            Prefabs.NodeReplacement = new Dictionary<string, string>(RemoveSep(textBoxNodeReplacement.Text).Split("/").Select(x =>
            {
                var token = x.Split('|');
                return new KeyValuePair<string, string>(token[0], token[1]);
            }));
            Prefabs.FullListTranslationTags = new HashSet<string>(RemoveSep(textBoxFullListTranslation.Text).Split('/'));
        }

        private void buttonSaveAndClose_Click(object sender, EventArgs e)
        {
            ToPrefabs();
            Prefabs.Save();
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            Prefabs.Init();
            Prefabs.Save();
            FromPrefabs();
        }

        private void buttonAutoDetect_Click(object sender, EventArgs e)
        {
            var version = Prefabs.AutoDetectRimworldVersion();
            textBoxRimworldVersion.Text = version;
        }

        private void buttonHelp1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("추출해야 하는 노드의 태그 목록을 정의합니다. '/' 문자로 구분하여 공백 없이 입력합니다. 특별한 일이 없는 이상 기본 상태로 두세요.");
        }

        private void buttonHelp2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Translation Handle은 노드의 이름이 'li'인 리스트 노드를 추출할 때 특정 태그의 값을 리스트 번호 대신 사용하는 추출 방법입니다. https://ludeon.com/forums/index.php?topic=41942.0 참고\n" +
                            "해당 노드에 Translation Handle 태그와 일치하는 노드가 있으면 그 노드의 값으로 리스트 노드의 이름을 결정합니다.\n" +
                            "예) verbs.2.label => verbs.Verb_Shoot.label\n'/' 문자로 구분하여 공백 없이 입력합니다.\n" +
                            "Translation Handle의 추출 방식은 그 태그의 타입이 Type 타입인지, 그 외인지에 따라 다릅니다. 따라서 Type 타입인 경우 앞에 접두어 '*'를 붙입니다.");
        }

        private void buttonHelp3_Click(object sender, EventArgs e)
        {
            MessageBox.Show("일부 노드는 추출했을 때의 노드와 번역을 적용할 때의 노드가 다른데, 노드 대체 기능은 그러한 경우에 사용됩니다. " +
                            "'(Def 타입)+(원본 노드)|(Def 타입)+(대체 노드)의 형식으로 입력하며, 이때 defName 부분은 생략하여 입력합니다. 여러 개인 경우 '/' 문자로 구분하여 공백 없이 입력합니다. " +
                            "\n주의: 아직 'li' 노드가 포함된 경우는 지원하지 않습니다.");
        }

        private void buttonHelp4_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Full-list Translation은 일부 경우에만 사용되는 리스트 노드 저장 방식입니다. https://ludeon.com/forums/index.php?topic=41942.0 참고\n '/' 문자로 구분하여 공백 없이 입력합니다.");
        }

        private void buttonSelectPathRimworld_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "RimWorldWin64.exe를 지정해주세요";
            dialog.FileName = "";
            dialog.Filter = "림월드 실행 파일|RimWorldWin64.exe";
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = false;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBoxPathRimworld.Text = Path.GetDirectoryName(dialog.FileName);
            }
        }

        private void buttonSelectPathWorkshop_Click(object sender, EventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Title = "림월드 창작마당 경로를 지정해주세요 => Steam\\steamapps\\workshop\\content\\294100";

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                textBoxPathWorkshop.Text = dialog.FileName;
            }
        }

        private void buttonBaseRefList_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "선택한 파일로부터 참조 모드의 목록을 불러옵니다.";
            openFileDialog.InitialDirectory = Assembly.GetExecutingAssembly().Location;
            openFileDialog.Filter = "참조 모드 리스트 파일|*.refMods";
            openFileDialog.DefaultExt = "refMods";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBoxBaseRefList.Text = openFileDialog.FileName;
            }
        }


        private static string RemoveSep(string s) => s.Replace(" ", "").Replace("\r", "").Replace("\n", "");
    }
}
