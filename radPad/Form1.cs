using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace radPad
{
    public partial class Form1 : Form
    {
        private string curFileName;   // currently open file name
        private bool fileChanged = false;
        private readonly string fileFilter = "rich/text files (*.rtf; *.txt; *.log)|*.rtf;*.txt; *.log";
        private readonly char[] delimiter = { ',' };
        private readonly string delimiterS = ",";
        private readonly string titleDelimiter = " - ";
        private readonly string rootDrive = "c:\\";
        private readonly string logFile = ".log";
        private readonly string richTextFile = ".rtf";
        private readonly string saveFilePrompt = "file has been changed, do you want to save";
        private readonly string saveFileTitle = "File Changed";
        private readonly float[] fontSizes = { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72 };
        private readonly string driveSep = ":";
        private readonly char pathSep = '\\';
        private float fontSize;  // current font size
        private PrintDocument printDocument = new PrintDocument();
        private int checkPrint;

        public Form1()
        {
            InitializeComponent();
            
            this.printDocument.BeginPrint += new System.Drawing.Printing.PrintEventHandler(this.printDocument_BeginPrint);
            this.printDocument.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(this.printDocument_PrintPage);
            // update the recent files part of the menu with saved data
            UpdateRecentMenu();
            // load up installed fonts
            InstalledFontCollection fonts = new InstalledFontCollection();
            foreach (FontFamily family in fonts.Families)
                comboBoxFonts.Items.Add(family.Name);
            comboBoxFonts.SelectedItem = SystemFonts.DefaultFont.Name;

            // load up some common font sizes
            bool sizeAdded = false;
            foreach (float f in fontSizes)
            {
                // add default font size into the proper place in the list of fonts
                if (!sizeAdded && SystemFonts.DefaultFont.Size < f)
                {
                    sizeAdded = true;
                    comboBoxFontSize.Items.Add(SystemFonts.DefaultFont.Size);
                }
                comboBoxFontSize.Items.Add(f);
            }
            fontSize = SystemFonts.DefaultFont.Size;
            comboBoxFontSize.SelectedItem = SystemFonts.DefaultFont.Size;
            // position the window based on saved data
            string t = Properties.Settings.Default.SavePosInfo;
            if (!String.IsNullOrEmpty(Properties.Settings.Default.SavePosInfo))
            {
                string[] s = Properties.Settings.Default.SavePosInfo.Split(delimiter);
                this.StartPosition = FormStartPosition.Manual;
                try
                {
                    this.Location = new Point(Int32.Parse(s[0]), Int32.Parse(s[1]));
                    this.Size = new Size(Int32.Parse(s[2]), Int32.Parse(s[3]));
                }
                catch
                {
                    // something wrong with saved pos info zap it
                    Properties.Settings.Default.SavePosInfo = null;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private DialogResult CheckSave()
        {
            DialogResult dr;

            // see if the user wants to save file changes
            if (fileChanged == true)
            {
                dr = MessageBox.Show(saveFilePrompt, saveFileTitle, MessageBoxButtons.YesNoCancel);
                if (dr == DialogResult.Yes)
                    if (String.IsNullOrEmpty(curFileName))
                        SaveFile(true);
                    else
                        SaveFile(false);
            }
            else dr = DialogResult.Yes;  // if file has not been change return yes to continue on
            return dr; 
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // save current file if user wants, then reset everything for a new file
            if (CheckSave() != DialogResult.Cancel)
            {
                richTextBox1.Text = String.Empty;
                curFileName = null;
                fileChanged = false;
                RemoveFileFromTitle();
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFile(null);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(String.IsNullOrEmpty(curFileName))  // do we have a filename yet
                SaveFile(true);
            else
                SaveFile(false);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
             SaveFile(true);
        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // code copied from MS print rich text box sample
            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == DialogResult.OK)
                printDocument.Print();
        }

        private void printPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // code copied from MS print rich text box sample
            PrintPreviewDialog ppd = new PrintPreviewDialog();
            ppd.Document = printDocument;
            ppd.ShowDialog();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SaveFile(bool needNewName)
        {
            // see if a filename is needed and prompt the user
            if (needNewName)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                if (String.IsNullOrEmpty(Properties.Settings.Default.LastPath))
                    sfd.InitialDirectory = rootDrive;
                else
                    sfd.InitialDirectory = Properties.Settings.Default.LastPath;
                sfd.Filter = fileFilter;
                sfd.FilterIndex = 1;
                sfd.DefaultExt = richTextFile;
                sfd.AddExtension = true;
                sfd.RestoreDirectory = true;
                DialogResult dr = sfd.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    curFileName = RemoveNetworkPath(sfd.FileName);// save the user selected file name
                    UpdateFileInfo(curFileName);
                }
                else
                    return;  // user changed their mind
            }

            try
            {
                if (Path.GetExtension(curFileName).Equals(richTextFile, StringComparison.OrdinalIgnoreCase))
                    richTextBox1.SaveFile(curFileName);
                else
                    richTextBox1.SaveFile(curFileName, RichTextBoxStreamType.PlainText);  // non rich text files need special flag
                fileChanged = false;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return;
            }
        }

        private void OpenFile(string fn)
        {
            if (CheckSave() == DialogResult.Cancel)
                return;

            // if passed empty strong prompt user for a file name
            if (fn == null)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                if (String.IsNullOrEmpty(Properties.Settings.Default.LastPath))
                    ofd.InitialDirectory = rootDrive;
                else
                    ofd.InitialDirectory = Properties.Settings.Default.LastPath;
                ofd.Filter = fileFilter;
                ofd.FilterIndex = 1;
                ofd.AddExtension = true;
                ofd.RestoreDirectory = true;
                DialogResult dr = ofd.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    curFileName = RemoveNetworkPath(ofd.FileName);// save the open file name
                }
                else
                    return;  // user changed their mind
            }
            else
                curFileName = fn;  // save the open file name

            try
            {
                UpdateFileInfo(curFileName);
                string ext = Path.GetExtension(curFileName);
                if (ext.Equals(richTextFile, StringComparison.OrdinalIgnoreCase)) 
                    richTextBox1.LoadFile(curFileName);
                else
                    richTextBox1.LoadFile(curFileName, RichTextBoxStreamType.PlainText); // non rich text files need special flag
                if (ext.Equals(logFile, StringComparison.OrdinalIgnoreCase))  // make log files read only
                    richTextBox1.ReadOnly = true;
                else
                    richTextBox1.ReadOnly = false;
                fileChanged = false;
            }
            catch (IOException e)
            {
                MessageBox.Show(e.Message);
                return;
            }
        }

        private string RemoveNetworkPath(string fn)
        {
            // depending on how the file is selected it may have a network path as the root instead of the drive
            // this code checks for that and removes the network path so \\localmachine\\c\file.txt = c:\file.txt
            string s = Dns.GetHostName();
            int index = fn.IndexOf(s, StringComparison.OrdinalIgnoreCase);
            if (index > 0)
                return (fn.Remove(index, s.Length).TrimStart(pathSep)).Insert(1, driveSep); // remove network name, leading \ characters, then insert : after drive letter
            else
                return fn;  // no network name return as is
        }

        private void File_Click(object sender, EventArgs e)
        {
            OpenFile(sender.ToString());
        }

        private void AddRecentMenuItem(string fn, EventHandler ev)
        {
            ToolStripMenuItem toolMenuItem = new ToolStripMenuItem(fn);
            toolMenuItem.Click += new EventHandler(ev);
            this.recentFilesToolStripMenuItem.DropDown.Items.Add(toolMenuItem);
        }

        private void UpdateRecentMenu()
        {
            if (!String.IsNullOrEmpty(Properties.Settings.Default.RecentFiles))
            {// delete recent files children menu item, rebuild it with current data and add it back
                this.recentFilesToolStripMenuItem.DropDown.Items.Clear();
                string[] fileNames;
                fileNames = Properties.Settings.Default.RecentFiles.Split(delimiter);
                foreach (string s in fileNames)
                {
                    if(!String.IsNullOrEmpty(s))
                        AddRecentMenuItem(s, File_Click);
                }
            }
        }

        private void UpdateFileInfo(string fn)
        {
            int i;
            if (String.IsNullOrEmpty(Properties.Settings.Default.RecentFiles))
                Properties.Settings.Default.RecentFiles = fn + delimiterS; // no files already saved, just add new one
            else
            {
                i = Properties.Settings.Default.RecentFiles.IndexOf(fn);
                if (i >= 0)
                {   // fn exists in list remove it, it will get added at the front below 
                    Properties.Settings.Default.RecentFiles = Properties.Settings.Default.RecentFiles.Remove(i, fn.Length + 1); // remove delimeter also
                }
                i = Properties.Settings.Default.RecentFiles.Count(x => x == delimiter[0]);
                if (i < 10)  // add at front if not full
                    Properties.Settings.Default.RecentFiles = fn + delimiterS + Properties.Settings.Default.RecentFiles;
                else
                { // string full, remove last member
                    string s;
                    s = Properties.Settings.Default.RecentFiles;
                    // insert new name at the front and remove the last name
                    Properties.Settings.Default.RecentFiles = fn + delimiterS + s.Remove(s.LastIndexOf(delimiter[0], s.Length - 2) + 1);
                }
            }
            Properties.Settings.Default.LastPath = Path.GetDirectoryName(fn);
            Properties.Settings.Default.Save();
            // update title bar
            RemoveFileFromTitle();
            this.Text = this.Text + titleDelimiter + Path.GetFileName(fn);
            UpdateRecentMenu();
        }

        private void RemoveFileFromTitle()
        {
            int i = this.Text.LastIndexOf(titleDelimiter);
            if (i >= 0)
                this.Text = this.Text.Remove(i);
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            fileChanged = true;
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Copy();
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Cut();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Paste();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Undo();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Redo();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.SelectAll();
        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(e.LinkText);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void SetButtonStyle(Button button, bool isOn)
        {
            if (isOn)
                button.FlatStyle = FlatStyle.Flat;
            else
                button.FlatStyle = FlatStyle.Standard;
        }

        private void FlipButtonFlag(Button button, bool isOn, FontStyle fsFlag)
        {
            if (richTextBox1.SelectionFont == null)
                return;
            // Flip the style bit and change the button appearance so the user can see the button took effect
            FontStyle style = richTextBox1.SelectionFont.Style;
            style ^= fsFlag;
            SetButtonStyle(button, !isOn);  // flip isOn flag because style was flipped
            richTextBox1.SelectionFont = new Font(richTextBox1.SelectionFont, style);
            richTextBox1.Focus();
        }

        private void buttonBold_Click(object sender, EventArgs e)
        {
            FlipButtonFlag(sender as Button, richTextBox1.SelectionFont.Bold, FontStyle.Bold);
        }

        private void buttonItalic_Click(object sender, EventArgs e)
        {
            FlipButtonFlag(sender as Button, richTextBox1.SelectionFont.Italic, FontStyle.Italic);
        }

        private void buttonUnderline_Click(object sender, EventArgs e)
        {
            FlipButtonFlag(sender as Button, richTextBox1.SelectionFont.Underline, FontStyle.Underline);
        }

        private void buttonStrikeOut_Click(object sender, EventArgs e)
        {
            FlipButtonFlag(sender as Button, richTextBox1.SelectionFont.Strikeout, FontStyle.Strikeout);
        }

        private void richTextBox1_SelectionChanged(object sender, EventArgs e)
        {
            // make sure button styles reflect what new text will look like when the cursor is moved in the text box
            SetButtonStyle(buttonUnderline, richTextBox1.SelectionFont.Underline);
            SetButtonStyle(buttonBold, richTextBox1.SelectionFont.Bold);
            SetButtonStyle(buttonItalic, richTextBox1.SelectionFont.Italic);
            SetButtonStyle(buttonStrikeOut, richTextBox1.SelectionFont.Strikeout);
            SetTextColorBtn();
            SetBackgroundColorBtn();
            comboBoxFonts.SelectedItem = richTextBox1.SelectionFont.Name;
            fontSize = richTextBox1.SelectionFont.Size;
            comboBoxFontSize.SelectedItem = fontSize;
        }

        private void comboBoxFonts_DropDownClosed(object sender, EventArgs e)
        {
            richTextBox1.SelectionFont = new Font(comboBoxFonts.SelectedItem.ToString(), fontSize,
                richTextBox1.SelectionFont.Style);
            richTextBox1.Focus();
        }

        private void CheckFontSize()
        {
            float size;
            try
            {
                size = float.Parse(comboBoxFontSize.Text);
            }
            catch
            {
                // not a number in fontsize box
                size = 0;
            }
            // ensure entered number is positive and different then current size
            if (size > 0 && fontSize != size)
                 
            {
                fontSize = size;
                richTextBox1.SelectionFont = new Font(richTextBox1.SelectionFont.ToString(), size, richTextBox1.SelectionFont.Style);
            }
            comboBoxFontSize.Text = fontSize.ToString(); // make sure fontsize box has correct number in it
            richTextBox1.Focus();
        }

        private void comboBoxFontSize_Leave(object sender, EventArgs e)
        {
            CheckFontSize();
        }

        private void comboBoxFontSize_DropDownClosed(object sender, EventArgs e)
        {
            comboBoxFontSize.Text = comboBoxFontSize.SelectedItem.ToString();
            CheckFontSize();
        }

        private Color GetColor(Color curColor)
        {
            ColorDialog MyDialog = new ColorDialog();

            MyDialog.AllowFullOpen = false;
            MyDialog.ShowHelp = true;
            MyDialog.Color = curColor;

            // return the new color if the user clicks OK 
            if (MyDialog.ShowDialog() == DialogResult.OK)
                return MyDialog.Color;
            else
                return curColor;
        }

        private void SetTextColorBtn()
        {
            if (richTextBox1.SelectionColor == Color.Black)
                buttonTextColor.FlatStyle = FlatStyle.Standard;
            else
            {
                buttonTextColor.FlatStyle = FlatStyle.Flat;
                buttonTextColor.FlatAppearance.BorderColor = richTextBox1.SelectionColor;
            }
        }

        private void buttonTextColor_Click(object sender, EventArgs e)
        {
            richTextBox1.SelectionColor = GetColor(richTextBox1.SelectionColor);
            SetTextColorBtn();
            richTextBox1.Focus();
        }

        private void SetBackgroundColorBtn()
        {
            if (richTextBox1.SelectionBackColor == Color.White)
                buttonBackgroundColor.FlatStyle = FlatStyle.Standard;
            else
            {
                buttonBackgroundColor.FlatStyle = FlatStyle.Flat;
                buttonBackgroundColor.FlatAppearance.BorderColor = richTextBox1.SelectionBackColor;
            }
        }

        private void buttonBackgroundColor_Click(object sender, EventArgs e)
        {
            richTextBox1.SelectionBackColor = GetColor(richTextBox1.SelectionBackColor);
            SetBackgroundColorBtn();
            richTextBox1.Focus();
        }

        // code copied from MS print rich text field example
        private void printDocument_BeginPrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            checkPrint = 0;
        }

        // code copied from MS print rich text field example
        private void printDocument_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            // Print the content of RichTextBox. Store the last character printed.
            checkPrint = richTextBox1.Print(checkPrint, richTextBox1.TextLength, e);

            // Check for more pages
            if (checkPrint < richTextBox1.TextLength)
                e.HasMorePages = true;
            else
                e.HasMorePages = false;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox1 aboutWindow = new AboutBox1();
            aboutWindow.Show();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            CheckSave();
            Properties.Settings.Default.SavePosInfo = String.Format("{0}{1}{2}{3}{4}{5}{6}", this.Location.X, delimiter[0],
                         this.Location.Y, delimiter[0], this.Size.Width, delimiter[0], this.Size.Height);

            string s = Properties.Settings.Default.SavePosInfo;
            Properties.Settings.Default.Save();
        }
    }
}
