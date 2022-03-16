using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Xml;
using System.Globalization;
using System.Net.Mail;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Drawing;


namespace xml_Test
{
    public partial class Form1 : Form
    {

        #region Fileds

        private XmlNamespaceManager _ns;

        private XmlDocument _document;
        public XmlDocument Document
        {
            get { return _document; }
            set { _document = value; }
        }

        private XmlNode _rootNode;
        public XmlNode RootNode
        {
            get { return _rootNode; }
            set { _rootNode = value; }
        }

        private XmlNode _node;

        public int keyvalue; // TEST

        public XmlNode Node
        {
            get { return _node; }
            set { _node = value; }
        }

        
        #endregion


        #region Variables

        //private IDictionary<string, string> instancneDictionary = new Dictionary<string, string>();


        //private List<string> names = new List<string>();

        private List<(string Component, string InstanceType, string BlockName)> names = new List<(string Component, string InstanceType, string BlockName)>();

        private string result = "";

        private string PartUId = "";

        private string ComponentNameValue = "";

        private string TagNameValue = "";

        private List<Colisions> completeListOfColisions = new List<Colisions>();

        private List<Colisions> localVarListOfColisions = new List<Colisions>();

        private List<Colisions> globalVarListOfColisions = new List<Colisions>();

        private int LocalColisionsCount = 0;

        private int GlobalColisionsCount = 0;

        private bool SearchInLocalVar = true;

        #endregion


        public Form1()
        {
            InitializeComponent();

            this.Text = "Vyhladavac viacnasobnych zapisov V4";

            tabPage1.Text = "Kolizie";
            tabPage2.Text = "Dvojite zapisy";
            tabPage3.Text = "Vsetky zapisy";
            
        }


        private void button1_Click(object sender, EventArgs e)
        {
            names.Clear();
            completeListOfColisions.Clear();
            localVarListOfColisions.Clear();
            globalVarListOfColisions.Clear();

            richTextBox1.Clear();
            richTextBox2.Clear();
            richTextBox3.Clear();
            label1.Text = "Projekt : ";

            checkBox_SearchInLocal.Enabled = false;


            var openFileDialog = new OpenFileDialog();

            //openFileDialog.InitialDirectory = @"D:\Excel code generator for TIA Portal Openness\xml Export ProjectCheck debug";
            //openFileDialog.InitialDirectory = @"D:\Excel code generator for TIA Portal Openness\xml Export Openes\TEMP\26-E10-90C-367604-001-1330-SPS.ap15_1\20_Station_1320";
            openFileDialog.Filter = "TIA XML (*.xml)|*.xml";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                
                string Path = openFileDialog.FileName;
                string FolderName = System.IO.Path.GetDirectoryName(Path);
                System.IO.DirectoryInfo Dirinfo = new System.IO.DirectoryInfo(FolderName);
                label1.Text = "Projekt : " + Dirinfo.Name + " --- " + openFileDialog.FileNames.Count() + ". suborov.";

                foreach (var file in openFileDialog.FileNames)
                {

                    //richTextBox1.AppendText(file + "\n");
                    //richTextBox1.AppendText(Environment.NewLine);
                    
                    ProcessFile(file);
                }


                GetColisions();

                checkBox_SearchInLocal.Enabled = true;
            }
            else
            {
                checkBox_SearchInLocal.Enabled = true;
            }

        }

        
        void ProcessFile(string Filename)
        {
            Document = new XmlDocument();

            _ns = new XmlNamespaceManager(Document.NameTable);
            _ns.AddNamespace("SI", "http://www.siemens.com/automation/Openness/SW/Interface/v3");
            _ns.AddNamespace("siemensNetworks", "http://www.siemens.com/automation/Openness/SW/NetworkSource/FlgNet/v3");

            //Load Xml File with fileName into memory
            Document.Load(Filename);
            //get root node of xml file
            RootNode = Document.DocumentElement;

            
            var AktualFile = System.IO.Path.GetFileNameWithoutExtension(Filename);

            var listOfNetworks = RootNode.SelectNodes("//SW.Blocks.CompileUnit");


            if (listOfNetworks != null)
            {
                foreach (XmlNode network in listOfNetworks)
                {

                    // initializacia novy network
                    PartUId = "";

                    var listOfAccess = network.SelectNodes(".//siemensNetworks:Access", _ns);

                    //najdi spulku - zapis zo spulky
                    var listOfPart = network.SelectNodes(".//siemensNetworks:Part", _ns);

                    foreach (XmlNode nodePart in listOfPart)
                    {
                        if (nodePart.Attributes["Name"].Value != null)
                        {
                            if (nodePart.Attributes["Name"].Value == "Coil")
                            {
                                // Spulka najdena a k nej prisluchajuce ID
                                PartUId = nodePart.Attributes["UId"].Value;

                                // Najdi wire ID prisluchajuce spulke
                                var listOfWire = network.SelectNodes(".//siemensNetworks:Wire", _ns);

                                foreach (XmlNode nodeWire in listOfWire)
                                {

                                    var IdentCon = nodeWire.SelectSingleNode(".//siemensNetworks:IdentCon", _ns);
                                    var NameCon = nodeWire.SelectSingleNode(".//siemensNetworks:NameCon", _ns);

                                    if (NameCon.Attributes["UId"].Value != null)
                                    {
                                        if (NameCon.Attributes["Name"].Value == "operand" &&
                                            NameCon.Attributes["UId"].Value == PartUId)
                                        {
                                            if (!string.IsNullOrEmpty(IdentCon.Attributes["UId"].Value))
                                            {
                                                // Wire ID najdene k spulke
                                                var PartUId = IdentCon.Attributes["UId"].Value;


                                                // Najdi meno symbolu k wide ID
                                                foreach (XmlNode nodeAcess in listOfAccess)
                                                {

                                                    if (nodeAcess.Attributes.GetNamedItem("UId") != null)
                                                    {
                                                        var nodeAcessUId = nodeAcess.Attributes["UId"].Value;

                                                        if (nodeAcessUId == PartUId)
                                                        {

                                                            //init
                                                            ComponentNameValue = "";

                                                            // Meno spulky
                                                            var Scope = nodeAcess.Attributes["Scope"].Value;
                                                            var listOfComponentName =
                                                                nodeAcess.SelectNodes(".//siemensNetworks:Component",
                                                                    _ns);

                                                            foreach (XmlNode ComponentName in listOfComponentName)
                                                            {

                                                                if (ComponentName.Attributes.GetNamedItem(
                                                                        "AccessModifier") != null)
                                                                {
                                                                    var ConstantValue = ComponentName
                                                                        .SelectSingleNode(
                                                                            ".//siemensNetworks:ConstantValue", _ns)
                                                                        .InnerText;

                                                                    if (ComponentNameValue == "")
                                                                    {
                                                                        ComponentNameValue =
                                                                            ComponentName.Attributes["Name"].Value +
                                                                            "[" + ConstantValue + "]";
                                                                    }
                                                                    else
                                                                    {
                                                                        ComponentNameValue = ComponentNameValue + "." +
                                                                            ComponentName.Attributes["Name"].Value +
                                                                            "[" + ConstantValue + "]";
                                                                    }
                                                                }
                                                                else if (ComponentName.Attributes.GetNamedItem(
                                                                             "SliceAccessModifier") != null)
                                                                {
                                                                    if (ComponentNameValue == "")
                                                                    {
                                                                        ComponentNameValue =
                                                                            ComponentName.Attributes["Name"].Value +
                                                                            "." + ComponentName
                                                                                .Attributes["SliceAccessModifier"]
                                                                                .Value;
                                                                    }
                                                                    else
                                                                    {
                                                                        ComponentNameValue = ComponentNameValue + "." +
                                                                            ComponentName.Attributes["Name"].Value +
                                                                            "." + ComponentName
                                                                                .Attributes["SliceAccessModifier"]
                                                                                .Value;
                                                                    }
                                                                }
                                                                else
                                                                {

                                                                    if (ComponentNameValue == "")
                                                                    {
                                                                        ComponentNameValue =
                                                                            ComponentName.Attributes["Name"].Value;
                                                                    }
                                                                    else
                                                                    {
                                                                        ComponentNameValue = ComponentNameValue + "." +
                                                                            ComponentName.Attributes["Name"].Value;
                                                                    }
                                                                }

                                                            }

                                                            //richTextBox2.AppendText($"Spulka: {ComponentNameValue}  Scope: {Scope} \n");
                                                            //richTextBox2.AppendText(Environment.NewLine);


                                                            if (ComponentNameValue != "")
                                                            {
                                                                //instancneDictionary.Add(Component, Component);

                                                                names.Add((ComponentNameValue, Scope, AktualFile));

                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }


                    // najdi call - zapis z bloku
                    var listOfCallRef = network.SelectNodes(".//siemensNetworks:Call", _ns);
                    foreach (XmlNode nodeCallref in listOfCallRef)
                    {

                        var listOfCallParameterOut = nodeCallref.SelectNodes(".//siemensNetworks:Parameter[@Section='Output']", _ns);

                        var actualCallUid = nodeCallref.Attributes["UId"].Value;

                        foreach (XmlNode CallParameterOut in listOfCallParameterOut)
                        {
                            var CallParameterOutName = CallParameterOut.Attributes["Name"].Value;


                            // Najdi wire ID prisluchajuce callu
                            var listOfWire = network.SelectNodes(".//siemensNetworks:Wire", _ns);
                            foreach (XmlNode nodeWire in listOfWire)
                            {

                                var NameCon = nodeWire.SelectSingleNode(".//siemensNetworks:NameCon[@Name='"+ CallParameterOutName + "' and @UId='" + actualCallUid + "']", _ns);

                                
                                if (NameCon != null)
                                {
                                    var IdentCon = nodeWire.SelectSingleNode(".//siemensNetworks:IdentCon", _ns);

                                    if (IdentCon != null)
                                    {
                                        var ConnUid = IdentCon.Attributes["UId"].Value;

                                        var listOfAcess = network.SelectSingleNode(".//siemensNetworks:Access[@UId='" + ConnUid + "']", _ns);

                                        var Scope = network.SelectSingleNode(".//siemensNetworks:Access[@UId='" + ConnUid + "']", _ns).Attributes["Scope"].Value;

                                        var listOfTagNames = listOfAcess.SelectNodes(".//siemensNetworks:Component", _ns);

                                        TagNameValue = "";

                                        foreach (XmlNode listOfTagName in listOfTagNames)
                                        {

                                            if (listOfTagName.Attributes.GetNamedItem("AccessModifier") != null)
                                            {
                                                var ConstantValue = listOfTagName.SelectSingleNode(".//siemensNetworks:ConstantValue", _ns).InnerText;

                                                if (ComponentNameValue == "")
                                                {
                                                    ComponentNameValue = listOfTagName.Attributes["Name"].Value + "[" + ConstantValue + "]";
                                                }
                                                else
                                                {
                                                    ComponentNameValue = ComponentNameValue + "." + listOfTagName.Attributes["Name"].Value + "[" + ConstantValue + "]";
                                                }
                                            }
                                            else if (listOfTagName.Attributes.GetNamedItem("SliceAccessModifier") != null)
                                            {
                                                if (TagNameValue == "")
                                                {
                                                    TagNameValue = listOfTagName.Attributes["Name"].Value + "." + listOfTagName.Attributes["SliceAccessModifier"].Value;
                                                }
                                                else
                                                {
                                                    TagNameValue = TagNameValue + "." + listOfTagName.Attributes["Name"].Value + "." + listOfTagName.Attributes["SliceAccessModifier"].Value;
                                                }
                                            }
                                            else
                                            {
                                                if (TagNameValue == "")
                                                {
                                                    TagNameValue = listOfTagName.Attributes["Name"].Value;
                                                }
                                                else
                                                {
                                                    TagNameValue = TagNameValue + "." + listOfTagName.Attributes["Name"].Value;
                                                }
                                            }

                                        }

                                        names.Add((TagNameValue, Scope, AktualFile));

                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        void GetColisions()
        {

            var colisions = names.GroupBy(x => x.Component)
                    .Where(w => w.Count() > 1)
                    .Select(s => s);



            //var colisions2 = from col in names2
            //                     //group col by col.Component
            //                 where col.Component.Count() > 1
            //                 where col.BlockName.Count() > 1
            //                 select col;


            //names2.Add(new Colisions() { Component = ComponentNameValue, InstanceType = Scope, BlockName = AktualFile});

            //.Select(s => new { Component = s.Key, InstanceType = s.Select(x => x.InstanceType)});
            //.Select(s => new{Component = s.Key, InstanceType = s.Select(x => x.InstanceType)}).ToList();
            //.Select(s => new{ Component = s.Key, InstanceType = s.Select(x => x.InstanceType);


            foreach (var colision in colisions)
            {
                
                result = "";

                foreach (var xx in colision)
                {

                    result = xx.Component + " -> " + xx.InstanceType + " -> " + xx.BlockName;

                    completeListOfColisions.Add(new Colisions() { Component = xx.Component, InstanceType = xx.InstanceType, BlockName = xx.BlockName });


                    if (xx.InstanceType == "GlobalVariable")

                    {
                        globalVarListOfColisions.Add(new Colisions() { Component = xx.Component, InstanceType = xx.InstanceType, BlockName = xx.BlockName });
                    }
                    else
                    {
                        localVarListOfColisions.Add(new Colisions() { Component = xx.Component, InstanceType = xx.InstanceType, BlockName = xx.BlockName });
                    }
                }
            }

            #region Vypis Globalne Kolizie

            result = "";
            GlobalColisionsCount = 0;

            var oldName = "";
            var newName = "";

            RichTextBoxExtensions.AppendText(richTextBox1, " -- Kolizie globalnych variable -- \n", Color.Blue);

            foreach (var globalVarListOfColision in globalVarListOfColisions)
            {

                oldName = globalVarListOfColision.Component;
                if (oldName != newName)
                {
                    newName = globalVarListOfColision.Component;
                    GlobalColisionsCount++;
                }


                result = globalVarListOfColision.Component + " -> " + globalVarListOfColision.InstanceType + " -> " + globalVarListOfColision.BlockName;


                RichTextBoxExtensions.AppendText(richTextBox1, GlobalColisionsCount + ". " + result, Color.Red);
                richTextBox1.AppendText(Environment.NewLine);

            }

            if (GlobalColisionsCount == 0)
            {
                RichTextBoxExtensions.AppendText(richTextBox1, "Nenasiel som.", Color.Green);
                richTextBox1.AppendText(Environment.NewLine);
            }

            #endregion


            #region Vypis Lokalne Kolizie

            LocalColisionsCount = 0;

            if (SearchInLocalVar)
            {
                var localVarColisionsByComponent = localVarListOfColisions.GroupBy(x => x.Component)
                    .Where(w => w.Count() > 1)
                    .Select(s => s);


                result = "";

                oldName = "";
                newName = "";

                RichTextBoxExtensions.AppendText(richTextBox1, " -- Kolizie lokalnych variable -- \n", Color.Blue);

                foreach (var localVarColisionByComponent in localVarColisionsByComponent)
                {

                    var localVarColisionsByBlockName = localVarColisionByComponent.GroupBy(x => x.BlockName)
                        .Where(w => w.Count() > 1)
                        .Select(s => s);


                    foreach (var localVarColisionByBlockName in localVarColisionsByBlockName)
                    {

                        foreach (var localVarListOfColision in localVarColisionByBlockName)
                        {

                            oldName = localVarListOfColision.Component;
                            if (oldName != newName)
                            {
                                newName = localVarListOfColision.Component;
                                LocalColisionsCount++;
                            }

                            result = localVarListOfColision.Component + " -> " + localVarListOfColision.InstanceType + " -> " + localVarListOfColision.BlockName;


                            RichTextBoxExtensions.AppendText(richTextBox1, LocalColisionsCount + ". " + result, Color.Black);
                            richTextBox1.AppendText(Environment.NewLine);

                        }
                    }
                }

                if (LocalColisionsCount == 0)
                {
                    RichTextBoxExtensions.AppendText(richTextBox1, "Nenasiel som.", Color.Green);
                    richTextBox1.AppendText(Environment.NewLine);
                }
            }

            #endregion


            #region Vypis Sumar + dvojite zapisy + vsetky zapisy

            if (LocalColisionsCount + GlobalColisionsCount == 0)
            {
                richTextBox1.AppendText(Environment.NewLine);
                RichTextBoxExtensions.AppendText(richTextBox1, $"Hotovo, nenasiel som ziadne kolizie.", Color.Green);
            }
            else
            {
                richTextBox1.AppendText(Environment.NewLine);
                RichTextBoxExtensions.AppendText(richTextBox1, $"Hotovo, spolu {LocalColisionsCount + GlobalColisionsCount} kolizii najdenich.", Color.Red);
            }




            for (int i = 0; i < completeListOfColisions.Count; i++)
            {
                richTextBox2.AppendText(i + 1 + " - " + completeListOfColisions[i].Component + " - " + completeListOfColisions[i].InstanceType + " - " + completeListOfColisions[i].BlockName + "\n");
            }


            for (int i = 0; i < names.Count; i++)
            {
                richTextBox3.AppendText(i + 1 + ";" + names[i].Component + ";" + names[i].InstanceType + ";" + names[i].BlockName + "\n");
            }

            #endregion



        }


        private void checkBox_SearchInLocal_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_SearchInLocal.Checked)
            {
                SearchInLocalVar = true;
            }
            else
            {
                SearchInLocalVar = false;
            }
            
        }
    }
}
