﻿using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using PawnPlus.Core.Events;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Xml;
using WeifenLuo.WinFormsUI.Docking;

namespace PawnPlus.Core.Forms
{
    public partial class Editor : DockContent
    {
        public TextEditor codeEditor { get; private set; }

        /// <summary>
        /// Path to the file which is edited.
        /// Empty by default.
        /// </summary>
        public string FilePath
        {
            get
            {
                return this.filePath;
            }

            set
            {
                this.Text = Path.GetFileName(value);
                this.filePath = value;
            }
        }

        /// <summary>
        /// Get the 'modified' flag.
        /// </summary>
        public bool IsModified { get { return codeEditor.IsModified; } }

        public bool HasProject { get; set; }

        private ElementHost elementHost = new ElementHost();

        private string filePath = string.Empty;

        public Editor()
        {
            InitializeComponent();

            this.codeEditor = new TextEditor();
        }

        private void CodeEditor_Load(object sender, EventArgs e)
        {
            this.elementHost.Dock = DockStyle.Fill;
            this.elementHost.Child = this.codeEditor;

            this.Controls.Add(this.elementHost);

            this.codeEditor.ShowLineNumbers = true;
            this.codeEditor.FontFamily = new System.Windows.Media.FontFamily("Consolas");
            this.codeEditor.FontSize = 12;

            this.codeEditor.TextArea.Caret.PositionChanged += codeEditor_Caret_PositionChanged;
            this.codeEditor.Document.UpdateFinished += codeEditor_UpdateFinished;

            // TODO: Create folding and indentation strategy.

            Stream stream = null;

            try
            {
                // Let's load the syntax hightlight.
                stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PawnPlus.Core.Resources.PAWNSyntax.xml");

                using (XmlReader xmlReader = XmlReader.Create(stream))
                {
                    stream = null;
                    this.codeEditor.SyntaxHighlighting = HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        private void Editor_FormClosing(object sender, FormClosingEventArgs e)
        {
            Workspace.CloseFile(this.FilePath);

            this.codeEditor.Document.UpdateFinished -= codeEditor_UpdateFinished;
            this.elementHost.Dispose();
        }

        private void codeEditor_UpdateFinished(object sender, EventArgs e)
        {
            if (this.IsModified == false && this.Text[this.Text.Length - 1] == '*')
            {
                this.Text = this.Text.Remove(this.Text.Length - 1);
            }
            else if (this.IsModified == true && this.Text[this.Text.Length - 1] != '*')
            {
                this.Text += '*';
            }
        }

        private void codeEditor_Caret_PositionChanged(object sender, EventArgs e)
        {
            EventStorage.Fire(EventKey.CaretPositionChanged, this, new CaretPositionChangedArgs(this.codeEditor.TextArea.Caret.Line, this.codeEditor.TextArea.Caret.Column));
        }

        /// <summary>
        /// Open a file.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        public void Open(string fileName)
        {
            this.FilePath = fileName;
            this.codeEditor.Load(fileName);

            IntPtr hIcon;

            if (fileName.Contains(".inc"))
            {
                hIcon = Properties.Resources.gear_32xLG.GetHicon();
            }
            else
            {
                hIcon = Properties.Resources.FileGroup_10135_32x.GetHicon();
            }

            this.Icon = Icon.FromHandle(hIcon);
        }

        /// <summary>
        /// Save file.
        /// </summary>
        /// <param name="fileName">Path where file should be saved.</param>
        public void Save(string fileName)
        {
            // A work-around for Cyrillic.
            byte[] bytes = new byte[this.codeEditor.Text.Length * sizeof(char)];
            Buffer.BlockCopy(this.codeEditor.Text.ToCharArray(), 0, bytes, 0, bytes.Length);

            using (StreamReader reader = new StreamReader(new MemoryStream(bytes), Encoding.Default))
            {
                this.codeEditor.Encoding = reader.CurrentEncoding;
            }

            this.FilePath = fileName;
            this.Text = Path.GetFileName(fileName);

            this.codeEditor.Save(this.FilePath);
        }

        /// <summary>
        /// Save file.
        /// </summary>
        public void Save()
        {
            this.Save(this.FilePath);
        }
    }
}