using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Xml;

namespace KAT_Translator
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        OpenFileDialog openFileDialog1 = new OpenFileDialog();
        SaveFileDialog saveFileDialog1 = new SaveFileDialog();
        public static bool isListViewChecked = false;
        public static bool isTreeViewChecked = false;
        Hashtable _treeNodesHashtable = new Hashtable();
        Hashtable _listViewItemHashtable = new Hashtable();
        XmlDocument _originalDocument;
        string _secondLanguageFileName;
        bool _isChanged;

        private void listView1_Click(object sender, EventArgs e)
        {
            if ((!isListViewChecked && isTreeViewChecked) || (!isListViewChecked && !isTreeViewChecked) || (isListViewChecked))
            {
                isListViewChecked = true;
                isTreeViewChecked = false;
                if (String.IsNullOrEmpty(textBox1.Text) || textBox1.Text == "Search in Tags")
                {
                    textBox1.Text = "Search in Text";
                }
            }
        }

        private void treeView1_Click(object sender, EventArgs e)
        {
            if ((isListViewChecked && !isTreeViewChecked) || (!isListViewChecked && !isTreeViewChecked) || (isTreeViewChecked))
            {
                isListViewChecked = false;
                isTreeViewChecked = true;
                if (String.IsNullOrEmpty(textBox1.Text) || textBox1.Text == "Search in Text")
                {
                    textBox1.Text = "Search in Tags";
                }
            }
        }

        private void Clear()
        {
            _treeNodesHashtable = new Hashtable();
            _listViewItemHashtable = new Hashtable();
            treeView1.Nodes.Clear();
            listView1.Items.Clear();
            listView1.Clear();

            _secondLanguageFileName = string.Empty;

            _isChanged = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_isChanged && listView1.Columns.Count == 3 &&
                MessageBox.Show("Are you sure? You haven't saved the document yet!", "Changes are not saved!", MessageBoxButtons.YesNo) == DialogResult.No)
                return;

            if (treeView1.Nodes.Count == 0)
            {
                openFileDialog1.FileName = string.Empty;
                openFileDialog1.DefaultExt = ".xml";
                openFileDialog1.Filter = "Xml files|*.xml" + "|All files|*.*";
                openFileDialog1.Title = "Open your first file";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    Clear();
                    if (OpenFirstFile(openFileDialog1.FileName))
                        if (MessageBox.Show("Do you want to open the second file? It is used when you want to updating a document.", "Asking a question~", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            OpenSecondFile();
                        }
                        else
                        {
                            listView1.Columns.Add("Seconnd language", 300);
                            CreateEmptyLanguage();
                        }

                }
            }
            else
                OpenSecondFile();         
        }

        private void ExpandNode(TreeNode parentNode, XmlNode node)
        {
            if (listView1.Columns.Count == 2)
            {
                AddAttributes(node);
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    var treeNode = new TreeNode(childNode.Name);
                    if (parentNode == null)
                        treeView1.Nodes.Add(treeNode);
                    else
                        parentNode.Nodes.Add(treeNode);
                    if (childNode.ChildNodes.Count > 0 && !IsTextNode(childNode) && childNode.NodeType != XmlNodeType.Comment && childNode.NodeType != XmlNodeType.CDATA)
                    {
                        ExpandNode(treeNode, childNode);
                    }
                    else
                    {
                        _treeNodesHashtable.Add(treeNode, childNode);
                        AddListViewItem(childNode);
                        AddAttributes(childNode);
                    }
                }
            }
            else if (listView1.Columns.Count == 3)
            {
                AddAttributes(node);
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    if (childNode.ChildNodes.Count > 0 && !IsTextNode(childNode) && childNode.NodeType != XmlNodeType.Comment && childNode.NodeType != XmlNodeType.CDATA)
                    {
                        ExpandNode(null, childNode);
                    }
                    else
                    {
                        AddListViewItem(childNode);
                        AddAttributes(childNode);
                    }
                }
            }
        }


        private void TreeView1AfterSelect(object sender, TreeViewEventArgs e)
        {
            var node = _treeNodesHashtable[e.Node] as XmlNode;
            if (node != null)
            {
                DeSelectListViewItems();

                var item = _listViewItemHashtable[BuildNodePath(node)] as ListViewItem;
                if (item != null)
                {
                    item.Selected = true;
                    listView1.EnsureVisible(item.Index);
                }
            }
        }

        private void DeSelectListViewItems()
        {
            var selectedItems = new List<ListViewItem>();
            foreach (ListViewItem lvi in listView1.SelectedItems)
            {
                selectedItems.Add(lvi);
            }
            foreach (ListViewItem lvi in selectedItems)
            {
                lvi.Selected = false;
            }
        }
        private void AddAttributes(XmlNode node)
        {
            if (node.NodeType == XmlNodeType.Comment || node.NodeType == XmlNodeType.CDATA)
                return;

            if (node.Attributes != null)
            {
                foreach (XmlNode childNode in node.Attributes)
                {
                    AddListViewItem(childNode);
                }
            }
        }

        private void AddListViewItem(XmlNode node)
        {
            if (listView1.Columns.Count == 2)
            {
                if (node.NodeType != XmlNodeType.Comment && node.NodeType != XmlNodeType.CDATA)
                {

                    ListViewItem item;
                    if (node.NodeType == XmlNodeType.Attribute)
                        item = new ListViewItem("@" + node.Name);
                    else
                        item = new ListViewItem(node.Name);
                    item.Tag = node;

                    var subItem = new ListViewItem.ListViewSubItem(item, node.InnerText);
                    item.SubItems.Add(subItem);
                    listView1.Items.Add(item);
                    _listViewItemHashtable.Add(BuildNodePath(node), item); // fails on some attributes!!
                }
            }
            else if (listView1.Columns.Count == 3)
            {
                var item = _listViewItemHashtable[BuildNodePath(node)] as ListViewItem;
                if (item != null)
                {
                    var subItem = new ListViewItem.ListViewSubItem(item, node.InnerText);
                    item.SubItems.Add(subItem);
                }
            }
        }

        private bool IsTextNode(XmlNode childNode)
        {
            if (childNode.ChildNodes.Count == 1 && childNode.ChildNodes[0].NodeType == XmlNodeType.Text)
                return true;
            return false;
        }

        private bool OpenFirstFile(string fileName)
        {
            label6.Text = "Opening " + fileName + "... ";
            var doc = new XmlDocument();
            try
            {
                doc.Load(fileName);
            }
            catch
            {
                MessageBox.Show("Not a valid xml file: " + fileName);
                return false;
            }

            return OpenFirstXmlDocument(doc);
        }

        private void OpenSecondFile()
        {
            _secondLanguageFileName = string.Empty;
            openFileDialog1.Title = "Open file to translate/correct";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                OpenSecondFile(openFileDialog1.FileName);
            }
            else
            {
                listView1.Columns.Add("Second language", 300);
                SetLanguage(comboBox2, null);
                CreateEmptyLanguage();
            }
            HighLightLinesWithSameText();

            button2.Enabled = true;
            button3.Enabled = true;
            treeView1.BackColor = Color.White;
            listView1.BackColor = Color.White;
            button4.BringToFront();
        }

        private void HighLightLinesWithSameText()
        {
            foreach (ListViewItem item in listView1.Items)
            {
                if (item.SubItems.Count == 3)
                {
                    if (item.SubItems[1].Text.Trim() == item.SubItems[2].Text.Trim())
                    {
                        item.BackColor = Color.LightYellow;
                        item.UseItemStyleForSubItems = true;
                    }
                    else if (item.SubItems[2].Text.Trim().Length == 0)
                    {
                        item.BackColor = Color.LightPink;
                        item.UseItemStyleForSubItems = true;
                    }
                    else
                    {
                        item.BackColor = listView1.BackColor;
                        item.UseItemStyleForSubItems = true;
                    }
                }
            }
        }

        private void OpenSecondFile(string fileName)
        {
            label6.Text = "Opening " + fileName + "...";
            _secondLanguageFileName = fileName;

            Cursor = Cursors.WaitCursor;
            listView1.BeginUpdate();
            var doc = new XmlDocument();
            try
            {
                doc.Load(_secondLanguageFileName);
            }
            catch
            {
                MessageBox.Show("Not a valid xml file: " + _secondLanguageFileName);
            }

            if (doc.DocumentElement != null && doc.DocumentElement.Attributes["Name"] != null)
            {
                listView1.Columns.Add(doc.DocumentElement.Attributes["Name"].InnerText, 300);
            }
            else if (doc.DocumentElement != null && doc.DocumentElement.Attributes["name"] != null)
            {
                listView1.Columns.Add(doc.DocumentElement.Attributes["name"].InnerText, 300);
            }
            else
            {
                listView1.Columns.Add("Second language", 300);
            }

            SetLanguage(comboBox2, doc);

            AddAttributes(doc.DocumentElement);
            if (doc.DocumentElement != null)
            {
                foreach (XmlNode childNode in doc.DocumentElement.ChildNodes)
                {
                    if (childNode.ChildNodes.Count > 0 && !IsTextNode(childNode))
                    {
                        ExpandNode(null, childNode);
                    }
                    else
                    {
                        AddListViewItem(childNode);
                        AddAttributes(doc.DocumentElement);
                    }
                }
            }

            CreateEmptyLanguage();

            listView1.EndUpdate();
            Cursor = Cursors.Default;
            label6.Text = "Done reading!"; // + _secondLanguageFileName;

            button2.Enabled = true;
            button3.Enabled = true;
            treeView1.BackColor = Color.White;
            listView1.BackColor = Color.White;

            button4.BringToFront();
        }

        private void CreateEmptyLanguage()
        {
            foreach (ListViewItem lvi in listView1.Items)
            {
                if (lvi.SubItems.Count == 2)
                {
                    var subItem = new ListViewItem.ListViewSubItem(lvi, string.Empty);
                    lvi.SubItems.Add(subItem);
                }
            }
        }

        private void FillOriginalDocumentFromSecondLanguage()
        {
            FillAttributes(_originalDocument.DocumentElement);
            if (_originalDocument.DocumentElement != null)
            {
                foreach (XmlNode childNode in _originalDocument.DocumentElement.ChildNodes)
                {
                    if (childNode.ChildNodes.Count > 0 && !IsTextNode(childNode))
                    {
                        FillOriginalDocumentExpandNode(childNode);
                    }
                    else
                    {
                        var item = _listViewItemHashtable[BuildNodePath(childNode)] as ListViewItem;
                        if (item != null)
                        {
                            childNode.InnerText = item.SubItems[2].Text;
                        }
                        FillAttributes(_originalDocument.DocumentElement);
                    }
                }
            }
        }

        static string BuildNodePath(XmlNode node)
        {
            var sb = new StringBuilder();
            sb.Append(node.Name);
            if (node.NodeType == XmlNodeType.Attribute)
            {
                XmlNode old = node;
                node = (node as XmlAttribute).OwnerElement; // use OwnerElement for attributes as ParentNode is null
                sb.Insert(0, node.Name + "@" + GetAttributeIndex(node, old));
                //  node = node.ParentNode;
            }
            while (node.ParentNode != null)
            {
                sb.Insert(0, node.ParentNode.Name + GetNodeIndex(node) + "/");
                node = node.ParentNode;
            }
            return sb.ToString();
        }

        private static string GetNodeIndex(XmlNode node)
        {
            int i = 0;
            if (node.NodeType == XmlNodeType.Comment || node.NodeType == XmlNodeType.CDATA)
                return string.Empty;

            if (!string.IsNullOrEmpty(node.NamespaceURI))
            {
                var man = new XmlNamespaceManager(node.OwnerDocument.NameTable);
                man.AddNamespace(node.Prefix, node.NamespaceURI);

                foreach (var x in node.ParentNode.SelectNodes(node.Name, man))
                {
                    if (x == node)
                        break;
                    i++;
                }

            }
            else
            {
                foreach (var x in node.ParentNode.SelectNodes(node.Name))
                {
                    if (x == node)
                        break;
                    i++;
                }
            }

            if (i == 0)
                return string.Empty;
            return string.Format("[{0}]", i);
        }

        private static string GetAttributeIndex(XmlNode node, XmlNode child)
        {
            if (node.Attributes == null)
                return string.Empty;

            int nameCount = 0;
            foreach (XmlAttribute x in node.Attributes)
            {
                if (x.Name == child.Name)
                    nameCount++;
            }

            int i = 0;
            foreach (XmlAttribute x in node.Attributes)
            {
                if (x == node)
                    break;
                i++;
            }
            if (i == 0 || nameCount < 2)
                return string.Empty;
            return string.Format("[{0}]", i);
        }

        private void FillOriginalDocumentExpandNode(XmlNode node)
        {
            FillAttributes(node);
            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.ChildNodes.Count > 0 && !IsTextNode(childNode))
                {
                    FillOriginalDocumentExpandNode(childNode);
                }
                else
                {
                    var item = _listViewItemHashtable[BuildNodePath(childNode)] as ListViewItem;
                    if (item != null)
                    {
                        childNode.InnerText = item.SubItems[2].Text;
                    }
                    FillAttributes(childNode);
                }
            }
        }

        private void FillAttributes(XmlNode node)
        {
            if (node.Attributes == null)
                return;

            foreach (XmlNode attribute in node.Attributes)
            {
                var item = _listViewItemHashtable[BuildNodePath(attribute)] as ListViewItem;
                if (item != null)
                {
                    attribute.InnerText = item.SubItems[2].Text;
                }
            }
        }

        private bool OpenFirstXmlDocument(XmlDocument doc)
        {
            listView1.Columns.Add("Tag", 150);
            if (doc.DocumentElement != null && doc.DocumentElement.Attributes["Name"] != null)
            {
                listView1.Columns.Add(doc.DocumentElement.Attributes["Name"].InnerText, 200);
            }
            else if (doc.DocumentElement != null && doc.DocumentElement.Attributes["name"] != null)
            {
                listView1.Columns.Add(doc.DocumentElement.Attributes["name"].InnerText, 200);
            }
            else
            {
                listView1.Columns.Add("Master language", 200);
            }

            SetLanguage(comboBox1, doc);

            AddAttributes(doc.DocumentElement);
            if (doc.DocumentElement != null)
            {
                foreach (XmlNode childNode in doc.DocumentElement.ChildNodes)
                {
                    if (childNode.NodeType != XmlNodeType.Attribute)
                    {
                        var treeNode = new TreeNode(childNode.Name);
                        treeView1.Nodes.Add(treeNode);
                        if (childNode.ChildNodes.Count > 0 && !IsTextNode(childNode))
                        {
                            ExpandNode(treeNode, childNode);
                        }
                        else
                        {
                            _treeNodesHashtable.Add(treeNode, childNode);
                            AddListViewItem(childNode);
                            AddAttributes(childNode);
                        }
                    }
                }
            }
            _originalDocument = doc;

            timer1.Enabled = true;
            label6.Text = "Done reading!"; // + openFileDialog1.FileName;


            return true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            timer1.Interval = 10;
            PopulateComboBox(comboBox1);
            PopulateComboBox(comboBox2);
            comboBox1.SelectedIndex = comboBox1.FindStringExact("English");
            comboBox2.SelectedIndex = comboBox1.FindStringExact("English");


            label5.Text = string.Empty;
        }

        private void SetLanguage(ComboBox comboBox, XmlDocument doc)
        {
            int index = 0;
            comboBox.SelectedIndex = -1;
            if (doc != null && doc.DocumentElement != null && doc.DocumentElement.SelectSingleNode("General/CultureName") != null)
            {
                string culture = doc.DocumentElement.SelectSingleNode("General/CultureName").InnerText;
                foreach (Language item in comboBox.Items)
                {
                    if (item.Value == culture)
                    {
                        comboBox.SelectedIndex = index;
                        return;
                    }
                    index++;
                }

                culture = culture.Substring(0, 2);
                index = 0;
                foreach (Language item in comboBox.Items)
                {
                    if (item.Value == culture)
                    {
                        comboBox.SelectedIndex = index;
                        return;
                    }
                    index++;
                }
            }
            if (comboBox.SelectedIndex == -1)
            {
                index = 0;
                foreach (Language item in comboBox.Items)
                {
                    if (item.Value == "en")
                    {
                        comboBox.SelectedIndex = index;
                        return;
                    }
                    index++;
                }
            }
        }



        class Language
        {
            private string Name { get; set; }
            public string Value { get; set; }

            public Language(string text, string value)
            {
                if (text.Length > 1)
                    text = text.Substring(0, 1).ToUpper() + text.Substring(1).ToLower();
                Name = text;
                Value = value;
            }

            public override string ToString() { return this.Name; } 
        }

        public static void PopulateComboBox(ComboBox comboBox)
        {
            comboBox.Items.Add(new Language("AFRIKAANS", "af"));
            comboBox.Items.Add(new Language("ALBANIAN", "sq"));
            comboBox.Items.Add(new Language("AMHARIC", "am"));
            comboBox.Items.Add(new Language("ARABIC", "ar"));
            comboBox.Items.Add(new Language("ARMENIAN", "hy"));
            comboBox.Items.Add(new Language("AZERBAIJANI", "az"));
            comboBox.Items.Add(new Language("BASQUE", "eu"));
            comboBox.Items.Add(new Language("BELARUSIAN", "be"));
            comboBox.Items.Add(new Language("BENGALI", "bn"));
            comboBox.Items.Add(new Language("BIHARI", "bh"));
            comboBox.Items.Add(new Language("BULGARIAN", "bg"));
            comboBox.Items.Add(new Language("BURMESE", "my"));
            comboBox.Items.Add(new Language("CATALAN", "ca"));
            comboBox.Items.Add(new Language("CHEROKEE", "chr"));
            comboBox.Items.Add(new Language("CHINESE", "zh"));
            comboBox.Items.Add(new Language("CHINESE_SIMPLIFIED", "zh-CN"));
            comboBox.Items.Add(new Language("CHINESE_TRADITIONAL", "zh-TW"));
            comboBox.Items.Add(new Language("CROATIAN", "hr"));
            comboBox.Items.Add(new Language("CZECH", "cs"));
            comboBox.Items.Add(new Language("DANISH", "da"));
            comboBox.Items.Add(new Language("DHIVEHI", "dv"));
            comboBox.Items.Add(new Language("DUTCH", "nl"));
            comboBox.Items.Add(new Language("ENGLISH", "en"));
            comboBox.Items.Add(new Language("ESPERANTO", "eo"));
            comboBox.Items.Add(new Language("ESTONIAN", "et"));
            comboBox.Items.Add(new Language("FILIPINO", "tl"));
            comboBox.Items.Add(new Language("FINNISH", "fi"));
            comboBox.Items.Add(new Language("FRENCH", "fr"));
            comboBox.Items.Add(new Language("GALICIAN", "gl"));
            comboBox.Items.Add(new Language("GEORGIAN", "ka"));
            comboBox.Items.Add(new Language("GERMAN", "de"));
            comboBox.Items.Add(new Language("GREEK", "el"));
            comboBox.Items.Add(new Language("GUARANI", "gn"));
            comboBox.Items.Add(new Language("GUJARATI", "gu"));
            comboBox.Items.Add(new Language("HEBREW", "iw"));
            comboBox.Items.Add(new Language("HINDI", "hi"));
            comboBox.Items.Add(new Language("HUNGARIAN", "hu"));
            comboBox.Items.Add(new Language("ICELANDIC", "is"));
            comboBox.Items.Add(new Language("IRISH", "ga"));
            comboBox.Items.Add(new Language("INDONESIAN", "id"));
            comboBox.Items.Add(new Language("INUKTITUT", "iu"));
            comboBox.Items.Add(new Language("ITALIAN", "it"));
            comboBox.Items.Add(new Language("JAPANESE", "ja"));
            comboBox.Items.Add(new Language("KANNADA", "kn"));
            comboBox.Items.Add(new Language("KAZAKH", "kk"));
            comboBox.Items.Add(new Language("KHMER", "km"));
            comboBox.Items.Add(new Language("KOREAN", "ko"));
            comboBox.Items.Add(new Language("KURDISH", "ku"));
            comboBox.Items.Add(new Language("KYRGYZ", "ky"));
            comboBox.Items.Add(new Language("LAOTHIAN", "lo"));
            comboBox.Items.Add(new Language("LATVIAN", "lv"));
            comboBox.Items.Add(new Language("LITHUANIAN", "lt"));
            comboBox.Items.Add(new Language("MACEDONIAN", "mk"));
            comboBox.Items.Add(new Language("MALAY", "ms"));
            comboBox.Items.Add(new Language("MALAYALAM", "ml"));
            comboBox.Items.Add(new Language("MALTESE", "mt"));
            comboBox.Items.Add(new Language("MARATHI", "mr"));
            comboBox.Items.Add(new Language("MONGOLIAN", "mn"));
            comboBox.Items.Add(new Language("NEPALI", "ne"));
            comboBox.Items.Add(new Language("NORWEGIAN", "no"));
            comboBox.Items.Add(new Language("ORIYA", "or"));
            comboBox.Items.Add(new Language("PASHTO", "ps"));
            comboBox.Items.Add(new Language("PERSIAN", "fa"));
            comboBox.Items.Add(new Language("POLISH", "pl"));
            comboBox.Items.Add(new Language("PORTUGUESE", "pt-PT"));
            comboBox.Items.Add(new Language("PUNJABI", "pa"));
            comboBox.Items.Add(new Language("ROMANIAN", "ro"));
            comboBox.Items.Add(new Language("RUSSIAN", "ru"));
            comboBox.Items.Add(new Language("SANSKRIT", "sa"));
            comboBox.Items.Add(new Language("SERBIAN", "sr"));
            comboBox.Items.Add(new Language("SINDHI", "sd"));
            comboBox.Items.Add(new Language("SINHALESE", "si"));
            comboBox.Items.Add(new Language("SLOVAK", "sk"));
            comboBox.Items.Add(new Language("SLOVENIAN", "sl"));
            comboBox.Items.Add(new Language("SPANISH", "es"));
            comboBox.Items.Add(new Language("SWAHILI", "sw"));
            comboBox.Items.Add(new Language("SWEDISH", "sv"));
            comboBox.Items.Add(new Language("TAJIK", "tg"));
            comboBox.Items.Add(new Language("TAMIL", "ta"));
            comboBox.Items.Add(new Language("TAGALOG", "tl"));
            comboBox.Items.Add(new Language("TELUGU", "te"));
            comboBox.Items.Add(new Language("THAI", "th"));
            comboBox.Items.Add(new Language("TIBETAN", "bo"));
            comboBox.Items.Add(new Language("TURKISH", "tr"));
            comboBox.Items.Add(new Language("UKRAINIAN", "uk"));
            comboBox.Items.Add(new Language("URDU", "ur"));
            comboBox.Items.Add(new Language("UZBEK", "uz"));
            comboBox.Items.Add(new Language("UIGHUR", "ug"));
            comboBox.Items.Add(new Language("VIETNAMESE", "vi"));
            comboBox.Items.Add(new Language("WELSH", "cy"));
            comboBox.Items.Add(new Language("YIDDISH", "yi"));
        }


        public static string TranslateTextViaScreenScraping(string input, string languagePair)
        {
            input = input.Replace(Environment.NewLine, "<br/>").Trim();
            input = input.Replace("'", "&apos;");

            //string url = String.Format("https://www.google.com/translate_t?hl=en&ie=UTF8&text={0}&langpair={1}", HttpUtility.UrlEncode(input), languagePair);
            string url = String.Format("https://translate.googleapis.com/translate_a/single?client=gtx&sl={0}&tl={1}&dt=t&q={2}", languagePair.Substring(0, 2), languagePair.Substring(3), Uri.EscapeUriString(input));

            HttpClient httpClient = new HttpClient();
            string result = httpClient.GetStringAsync(url).Result;
            //var jsonData = new Deserialize<List<dynamic>>(result);
            var jsonData = JsonConvert.DeserializeObject<List<dynamic>>(result);

            var translationItems = jsonData[0];
            string translation = "";
            foreach (object item in translationItems)
            {
                IEnumerable translationLineObject = item as IEnumerable;
                IEnumerator translationLineString = translationLineObject.GetEnumerator();
                translationLineString.MoveNext();
                translation += string.Format(" {0}", Convert.ToString(translationLineString.Current));
            }
            if (translation.Length > 1) { translation = translation.Substring(1); };
            return translation;
        }

        private void GoogleTranslateSelectedLines()
        {
            if (string.IsNullOrEmpty(_secondLanguageFileName))
                return;

            if (comboBox1.SelectedItem == null || comboBox2.SelectedItem == null)
            {
                MessageBox.Show("From/to language not selected");
                return;
            }

            int skipped = 0;
            int translated = 0;
            string oldText = string.Empty;
            string newText = string.Empty;

            if (listView1.SelectedItems.Count > 10)
            {
                label5.Text = "Translating via Google Translate. Please wait...";
                Refresh();
            }

            Cursor = Cursors.WaitCursor;
            var sb = new StringBuilder();
            var res = new StringBuilder();
            var oldLines = new List<string>();
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                oldText = item.SubItems[1].Text;
                oldLines.Add(oldText);
                var urlEncode = HttpUtility.UrlEncode(sb + newText);
                if (urlEncode != null && urlEncode.Length >= 1000)
                {
                    res.Append(TranslateTextViaScreenScraping(sb.ToString(), (comboBox1.SelectedItem as Language).Value + "|" + (comboBox2.SelectedItem as Language).Value));
                    sb = new StringBuilder();
                }
                sb.Append("== " + oldText + " ");
            }
            res.Append(TranslateTextViaScreenScraping(sb.ToString(), (comboBox1.SelectedItem as Language).Value + "|" + (comboBox2.SelectedItem as Language).Value));

            var lines = new List<string>();
            foreach (string s in res.ToString().Split(new string[] { "==" }, StringSplitOptions.None))
                lines.Add(s.Trim());
            lines.RemoveAt(0);

            if (listView1.SelectedItems.Count != lines.Count)
            {
                MessageBox.Show("Error getting/decoding translation from google!");
                Cursor = Cursors.Default;
                return;
            }

            int index = 0;
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                string s = lines[index];
                string cleanText = s.Replace("</p>", string.Empty).Trim();
                cleanText = cleanText.Replace(" ...", "...");
                cleanText = cleanText.Replace("<br>", Environment.NewLine);
                cleanText = cleanText.Replace("<br/>", Environment.NewLine);
                cleanText = cleanText.Replace("<br />", Environment.NewLine);
                cleanText = cleanText.Replace(Environment.NewLine + " ", Environment.NewLine);
                newText = cleanText;

                oldText = oldLines[index];
                if (oldText.Contains("{0:"))
                {
                    newText = oldText;
                }
                else
                {
                    if (!oldText.Contains(" / "))
                        newText = newText.Replace(" / ", "/");

                    if (!oldText.Contains(" ..."))
                        newText = newText.Replace(" ...", "...");

                    if (!oldText.Contains("& "))
                        newText = newText.Replace("& ", "&");

                    if (!oldText.Contains("# "))
                        newText = newText.Replace("# ", "#");

                    if (!oldText.Contains("@ "))
                        newText = newText.Replace("@ ", "@");

                    if (oldText.Contains("{0}"))
                    {
                        for (int i = 0; i < 50; i++)
                            newText = newText.Replace("(" + i + ")", "{" + i + "}");
                    }
                    translated++;
                }
                item.SubItems[2].Text = newText;
                _isChanged = true;
                index++;
            }


            Cursor = Cursors.Default;
            if (translated == 1 && skipped == 0)
            {
                label5.Text = "One line translated: '" + Max50(oldText) + "' => '" + Max50(newText) + "'";
            }
            else
            {
                if (translated == 1)
                    label5.Text = "Oneline translated";
                else
                    label5.Text = translated + " lines translated";
                if (skipped > 0)
                    label5.Text += ", " + skipped + " line(s) skipped";
            }
            listView1_SelectedIndexChanged(null, null);
        }

        private string Max50(string text)
        {
            if (text.Length > 50)
                return text.Substring(0, 49).Trim() + "...";
            return text;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1 && listView1.SelectedItems[0].SubItems.Count > 2)
            {
                textBox3.Enabled = true;
                //textBox3.Text = listView1.SelectedItems[0].SubItems[2].Text;
                textBox3.Text = "";

                var node = listView1.SelectedItems[0].Tag as XmlNode;
                if (node != null)
                    label5.Text = string.Format("{0}     {1} / {2}", BuildNodePath(node).Replace("#document/", ""), listView1.SelectedItems[0].Index + 1, listView1.Items.Count);
                else
                    label5.Text = string.Format("{0} / {1}", listView1.SelectedItems[0].Index + 1, listView1.Items.Count);


                textBox3.Focus();
            }
            else
            {
                textBox3.Text = string.Empty;
                textBox3.Enabled = false;
                label5.Text = string.Format("{0} items selected", listView1.SelectedItems.Count);
            }

            HighLightLinesWithSameText();
        }

        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            // make sure they're actually dropping files (not text or anything else)
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
                e.Effect = DragDropEffects.All;
        }

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (!string.IsNullOrEmpty(_secondLanguageFileName))
            {
                MessageBox.Show("Two files already loaded");
                return;
            }

            if (files.Length == 1)
            {

                string fileName = files[0];
                var fi = new FileInfo(fileName);
                if (fi.Length < 1024 * 1024 * 20) // max 20 mb
                {
                    if (treeView1.Nodes.Count == 0)
                        OpenFirstFile(fileName);
                    else
                        OpenSecondFile(fileName);
                }
                else
                {
                    MessageBox.Show(fileName + " is too large (max 20 mb)");
                }
            }
            else
            {
                MessageBox.Show("Only file drop supported");
            }
        }

        private void translateTheLineUsingGoogleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GoogleTranslateSelectedLines();
        }

        private void transferValueFromMasterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_secondLanguageFileName))
                return;

            int transfered = 0;
            string oldText = string.Empty;
            string newText = string.Empty;
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                oldText = item.SubItems[2].Text;
                newText = item.SubItems[1].Text;
                transfered++;
                item.SubItems[2].Text = newText;
                _isChanged = true;
            }
            if (transfered == 1)
                label5.Text = "One line transfered from master: '" + oldText + "' => '" + newText + "'";
            else
                label5.Text = transfered + " line(s) transfered from master";
            listView1_SelectedIndexChanged(null, null);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show($"Are you sure you wanna close the program?", "☆ｏ(＞＜；)○", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }
        }

        public static bool isExpanded = true;

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (isExpanded)
            {
                panel4.Height -= 3;
                //listView1.Height -= 2;
                if (panel4.Height <= 32)
                {
                    label2.SendToBack();
                    timer1.Enabled = false;
                    panel4.Height = 32;
                    listView1.Height = listView1.Height - textBox3.Height - 5;
                    isExpanded = false;
                }
            }
            else
            {
                panel4.Height += 3;
                //listView1.Height += 2;
                if (panel4.Height >= 78)
                {
                    label2.BringToFront();
                    timer1.Enabled = false;
                    panel4.Height = 78;
                    listView1.Height = listView1.Height + textBox3.Height + 5;
                    isExpanded = true;
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (_isChanged && listView1.Columns.Count == 3 &&
                MessageBox.Show("Changes will be lost. Continue?", "Continue", MessageBoxButtons.YesNo) == DialogResult.No)
                return;

            Clear();
            label5.Text = "";
            timer1.Enabled = true;
            button4.SendToBack();
            button2.Enabled = false;
            button3.Enabled = false;

            treeView1.BackColor = Color.Gainsboro;
            listView1.BackColor = Color.Gainsboro;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_secondLanguageFileName))
            {
                FillOriginalDocumentFromSecondLanguage();

                var settings = new XmlWriterSettings { Indent = true };
                using (var writer = XmlWriter.Create(_secondLanguageFileName, settings))
                {
                    _originalDocument.Save(writer);
                }
                _isChanged = false;
                label5.Text = "File saved - " + _secondLanguageFileName;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (_originalDocument == null)
                return;

            saveFileDialog1.Title = "Save language file as...";
            saveFileDialog1.DefaultExt = ".xml";
            saveFileDialog1.Filter = "Xml files|*.xml" + "|All files|*.*";
            saveFileDialog1.Title = "Open language master file";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _secondLanguageFileName = saveFileDialog1.FileName;
                FillOriginalDocumentFromSecondLanguage();
                _originalDocument.Save(saveFileDialog1.FileName);
                _isChanged = false;
                label5.Text = "File saved as " + _secondLanguageFileName;
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems.Count == 1)
                {
                    listView1.SelectedItems[0].SubItems[2].Text = textBox3.Text;
                }
            }
            catch { }
        }

        private void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Control && !e.Alt)
                _isChanged = true;

            if (e.KeyCode == Keys.Up)
            {
                int next = Convert.ToInt32(listView1.FocusedItem.Index) - 1;
                //add this line            
                //this.listView1.Focus();
                this.listView1.Items[next].Selected = true;
            }
            if (e.KeyCode == Keys.Down)
            {
                int next = Convert.ToInt32(listView1.FocusedItem.Index) + 1;
                //add this line            
                //this.listView1.Focus();
                this.listView1.Items[next].Selected = true;
            }
        }

        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Up && e.KeyCode != Keys.Down && e.KeyCode != Keys.Left && e.KeyCode != Keys.Right)
            {
                if (string.IsNullOrEmpty(textBox3.Text))
                {
                    if (e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
                    {
                        textBox3.Text = e.KeyCode.ToString();
                        textBox3.Focus();
                        textBox3.DeselectAll();
                        textBox3.Select(1, 1);
                    }                
                }
                e.SuppressKeyPress = true;
            }
        }
    }
}
