using FeedingFrenzyExplorer.Models;
using FeedingFrenzyExplorer.Struct;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FeedingFrenzyExplorer
{
    public partial class Form1 : Form
    {
        string formTitle;

        string file_Path;
        byte[] file_Bin;

        string selPath;

        byte[] uniqueBytes;
        byte[] total_file;

        CSifHeader pHeader = new CSifHeader();
        List<SifFile> pFiles = new List<SifFile>();

        BackgroundWorker bwAnalyze = new BackgroundWorker();
        BackgroundWorker bwExtract = new BackgroundWorker();
        BackgroundWorker bwSave = new BackgroundWorker();

        ContextMenuStrip rightClickNonFileMenu = new ContextMenuStrip();

        bool Logging = false;
        public Form1()
        {
            InitializeComponent();

            Init();
        }

        private void WriteLog(string log)
        {
            if (Logging)
            {
                if (String.IsNullOrEmpty(RTBLogs.Text))
                    RTBLogs.Text += "[" + DateTime.Now + "] " + log;
                else
                    RTBLogs.Text += "\r\n[" + DateTime.Now + "] " + log;
            }
        }
        void AddListViewItems(int offset, int size, /*string path,*/ string name, int id, bool marked = false)
        {
            var items = new ListViewItem(new[] { name, offset.ToString("X8"), size.ToString("X8"), id.ToString("X8") });
            if (marked)
                items.BackColor = Color.Orange;
            listFile.Items.Add(items);
        }

        #region View file
        private void ViewImage(ListViewItem item)
        {
            var fileId = item.SubItems[3];

            //Get file in list by Id
            var file = pFiles[int.Parse(fileId.Text, System.Globalization.NumberStyles.HexNumber)];

            Form frmImage = new Form();
            frmImage.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            frmImage.Text = file.Path + "/" + file.FileName;

            Panel panelImage = new Panel();
            panelImage.Dock = DockStyle.Fill;
            panelImage.AutoScroll = true;
            panelImage.BackColor = Color.Lime;

            PictureBox picBox = new PictureBox();
            picBox.BackColor = Color.Transparent;
            picBox.SizeMode = PictureBoxSizeMode.AutoSize;

            picBox.Image = Functions.ByteToImage(file.Binary.ToArray());

            panelImage.Controls.Add(picBox);
            frmImage.Controls.Add(panelImage);
            frmImage.Show();
        }

        private void ViewText(ListViewItem item)
        {
            var fileId = item.SubItems[3];

            //Get file in list by Id
            var file = pFiles[int.Parse(fileId.Text, System.Globalization.NumberStyles.HexNumber)];

            byte[] data = file.Binary.ToArray();

            Form frmText = new Form();
            frmText.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            frmText.Text = file.Path + "/" + file.FileName;

            RichTextBox rtb = new RichTextBox();
            rtb.Dock = DockStyle.Fill;
            rtb.WordWrap = false;
            rtb.Text = Encoding.UTF8.GetString(data);

            frmText.Controls.Add(rtb);

            frmText.Show();
        }
        #endregion

        private void Init()
        {
            formTitle = this.Text;
            this.Load += (s, e) =>
            {
                var cmdArgs = Environment.GetCommandLineArgs();
                if (cmdArgs.Length > 0)
                {
                    foreach(var arg in cmdArgs)
                    {
                        if (arg == "debug") // ANALYSIS PURPOSE ONLY. IT WILL CAUSE LAG IF THIS ACTIVE
                            Logging = true;
                            viewToolStripMenuItem.Visible = true;
                    }
                }
            };

            rightClickNonFileMenu.Items.Add("Add");

            #region Background Worker

            bwAnalyze.WorkerReportsProgress = true;
            bwAnalyze.DoWork += (s, e) =>
            {
                Invoke(
                        (MethodInvoker)delegate
                        {
                            openToolStripMenuItem.Enabled = false;
                            saveAsToolStripMenuItem.Enabled = false;
                            treePath.Nodes.Clear();
                            listFile.Items.Clear();
                        }
                    );
                GetFiles();
                GetPath();
                Invoke(
                        (MethodInvoker)delegate
                        {
                            extractToolStripMenuItem.Enabled = true;
                            saveAsToolStripMenuItem.Enabled = true;
                        }
                    );
            };
            bwAnalyze.ProgressChanged += (s, e) => { ProgBar.Value = e.ProgressPercentage; };
            bwAnalyze.RunWorkerCompleted += (s, e) => { ProgBar.Value = 0; openToolStripMenuItem.Enabled = true; saveAsToolStripMenuItem.Enabled = true; };

            bwExtract.WorkerReportsProgress = true;
            bwExtract.DoWork += (s, e) =>
            {
                Invoke(
                        (MethodInvoker)delegate
                        {
                            extractToolStripMenuItem.Enabled = false;
                            openToolStripMenuItem.Enabled = false;
                            saveAsToolStripMenuItem.Enabled = false;
                        }
                    );
                ExtractToPath(selPath);
                Invoke(
                        (MethodInvoker)delegate
                        {
                            extractToolStripMenuItem.Enabled = true;
                            openToolStripMenuItem.Enabled = true;
                            saveAsToolStripMenuItem.Enabled = true;
                        }
                    );
            };
            bwExtract.ProgressChanged += (s, e) => { ProgBar.Value = e.ProgressPercentage; };
            bwExtract.RunWorkerCompleted += (s, e) => { ProgBar.Value = 0; openToolStripMenuItem.Enabled = true; saveAsToolStripMenuItem.Enabled = true; };

            bwSave.WorkerReportsProgress = true;
            bwSave.DoWork += (s, e) =>
            {
                Invoke(
                        (MethodInvoker)delegate
                        {
                            extractToolStripMenuItem.Enabled = false;
                            openToolStripMenuItem.Enabled = false;
                            saveAsToolStripMenuItem.Enabled = false;
                        }
                    );
                SaveFile();
                Invoke(
                        (MethodInvoker)delegate
                        {
                            extractToolStripMenuItem.Enabled = true;
                            openToolStripMenuItem.Enabled = true;
                            saveAsToolStripMenuItem.Enabled = true;
                        }
                    );
            };
            bwSave.ProgressChanged += (s, e) => { ProgBar.Value = e.ProgressPercentage; };
            bwSave.RunWorkerCompleted += (s, e) => { ProgBar.Value = 0; openToolStripMenuItem.Enabled = true; saveAsToolStripMenuItem.Enabled = true; };

            #endregion

            MainContainer.Panel2Collapsed = true;

            viewToolStripMenuItem.Visible = false;

            #region Toolstrip

            logsToolStripMenuItem.CheckedChanged += (s, e) => { MainContainer.Panel2Collapsed = !logsToolStripMenuItem.Checked; Logging = logsToolStripMenuItem.Checked; };
            clearLogsToolStripMenuItem.Click += (s, e) => RTBLogs.Clear();
            aboutToolStripMenuItem.Click += (s, e) => MessageBox.Show("@FarisFreak\r\n\r\ngithub.com/farisfreak", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
            selectFolderToolStripMenuItem.Click += (s, e) =>
            {
                FolderBrowserDialog open = new FolderBrowserDialog();
                open.ShowNewFolderButton = true;
                if (open.ShowDialog() == DialogResult.OK)
                {
                    selPath = open.SelectedPath;
                    try
                    {
                        if (Directory.Exists(selPath))
                        {
                            // Extracting process
                            bwExtract.RunWorkerAsync();
                        } else
                            MessageBox.Show("Path does not exist!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    catch (Exception ex)
                    {
                        WriteLog(ex.Message);
                        MessageBox.Show(ex.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            automaticalyCreateFolderToolStripMenuItem.Click += (s, e) =>
            {
                selPath = Path.GetDirectoryName(file_Path) + "\\" + Path.GetFileNameWithoutExtension(file_Path) + "_Extract";
                try
                {
                    bwExtract.RunWorkerAsync();
                }
                catch (Exception ex)
                {
                    WriteLog(ex.Message);
                    MessageBox.Show(ex.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            openToolStripMenuItem.Click += (s, e) =>
            {
                OpenFileDialog open = new OpenFileDialog();
                open.Filter = "saf files (*.saf)|*.saf";
                if (open.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        file_Path = open.FileName;
                        this.Text = formTitle + " - " + file_Path;
                        file_Bin = File.ReadAllBytes(file_Path);

                        WriteLog("File Name : " + Path.GetFileName(file_Path));
                        WriteLog("File Path : " + Path.GetDirectoryName(file_Path));
                        WriteLog("File Size : " + file_Bin.Length.ToString() + " bytes");

                        ReadBinary();
                    }
                    catch (Exception ex)
                    {
                        WriteLog(ex.Message);
                        MessageBox.Show(ex.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            saveAsToolStripMenuItem.Click += (s, e) =>
            {
                SaveFileDialog save = new SaveFileDialog();
                save.Filter = "saf files (*.saf)|*.saf";
                if (save.ShowDialog() == DialogResult.OK)
                {
                    selPath = save.FileName;
                    try
                    {
                        bwSave.RunWorkerAsync();
                    }
                    catch (Exception ex)
                    {
                        WriteLog(ex.Message);
                        MessageBox.Show(ex.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            #endregion
            #region List & Tree View

            listFile.Columns.Add("File Name", 225);
            listFile.Columns.Add("Offset", 70);
            listFile.Columns.Add("Size", 70);
            listFile.Columns.Add("Id", 70);

            listFile.MouseDoubleClick += (s, e) =>
            {
                try
                {
                    ListViewHitTestInfo info = listFile.HitTest(e.X, e.Y);
                    ListViewItem selectedItems = info.Item;
                    if (selectedItems != null)
                    {
                        if (Path.GetExtension(selectedItems.Text) == ".jpg" || Path.GetExtension(selectedItems.Text) == ".png")
                            ViewImage(selectedItems);
                        else if (Path.GetExtension(selectedItems.Text) == ".xml")
                            ViewText(selectedItems);
                    }
                }
                catch (Exception ex)
                {
                    WriteLog(ex.Message);
                    MessageBox.Show(ex.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            listFile.MouseClick += (s, e) =>
            {
                try
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        ListViewHitTestInfo info = listFile.HitTest(e.X, e.Y);
                        ListViewItem selectedItems = info.Item;
                        //catch
                        var catchdata = selectedItems;
                        var nx = 0;
                        if (selectedItems != null)
                        {
                            rightClickFileMenu.Show(listFile, new Point(e.X, e.Y));
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLog(ex.Message);
                    MessageBox.Show(ex.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            treePath.AfterSelect += (s, e) =>
            {
                listFile.Items.Clear();

                string selpath = treePath.SelectedNode.FullPath;

                IEnumerable<int> allIndices = pFiles.Select(x => x.Path).Select((str, idx) => new { Str = str, Index = idx })
                    .Where(x => x.Str == selpath)
                    .Select(x => x.Index);

                foreach (int matchingIndex in allIndices)
                {
                    SifFile curSF = pFiles[matchingIndex];
                    AddListViewItems(curSF.Offset, curSF.Size, curSF.FileName, matchingIndex, curSF.Marked);
                }
            };

            #endregion
            #region Right click list view
            rightClickFileMenu.Opening += (s, e) =>
            {
                try
                {
                    ListViewItem selectedItems = listFile.SelectedItems[0];
                    if (selectedItems != null)
                    {
                        var fileId = selectedItems.SubItems[3];

                        //Get file in list by Id
                        var file = pFiles[int.Parse(fileId.Text, System.Globalization.NumberStyles.HexNumber)];

                        revertToOriginalToolStripMenuItem.Enabled = file.Marked;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            revertToOriginalToolStripMenuItem.Click += (s, e) =>
            {
                try
                {
                    ListViewItem selectedItems = listFile.SelectedItems[0];
                    if (selectedItems != null)
                    {
                        var fileId = selectedItems.SubItems[3];

                        //Get file in list by Id
                        var file = pFiles[int.Parse(fileId.Text, System.Globalization.NumberStyles.HexNumber)];

                        file.Binary = file._oriBinary;
                        file._oriBinary = null;
                        file.Marked = false;
                        listFile.SelectedItems[0].BackColor = Color.Transparent;

                        pFiles[int.Parse(fileId.Text, System.Globalization.NumberStyles.HexNumber)] = file;

                        revertToOriginalToolStripMenuItem.Enabled = file.Marked;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            extractToolStripMenuItem1.Click += (s, e) =>
            {
                try
                {
                    ListViewItem selectedItems = listFile.SelectedItems[0];
                    if (selectedItems != null)
                    {
                        var fileId = selectedItems.SubItems[3];

                        //Get file in list by Id
                        var file = pFiles[int.Parse(fileId.Text, System.Globalization.NumberStyles.HexNumber)];

                        string extFile = Path.GetExtension(file.FileName).Replace(".", string.Empty);

                        SaveFileDialog saveFileDialog = new SaveFileDialog();
                        saveFileDialog.FileName = file.FileName;
                        saveFileDialog.Filter = extFile + " files (*." + extFile + ")|*." + extFile;

                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            byte[] data = file.Binary.ToArray();
                            File.WriteAllBytes(saveFileDialog.FileName, data.ToArray());

                            MessageBox.Show("File saved to " + saveFileDialog.FileName + " successfully!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            replaceToolStripMenuItem.Click += (s, e) =>
            {
                try
                {
                    ListViewItem selectedItems = listFile.SelectedItems[0];
                    if (selectedItems != null)
                    {
                        var fileId = selectedItems.SubItems[3];

                        OpenFileDialog openFileDialog = new OpenFileDialog();
                        openFileDialog.Multiselect = false;
                        if (openFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            //temp
                            byte[] data = File.ReadAllBytes(openFileDialog.FileName);

                            //Get file in list by Id
                            var file = pFiles[int.Parse(fileId.Text, System.Globalization.NumberStyles.HexNumber)];
                            file._oriBinary = file.Binary;
                            file.Binary = data.ToList();
                            file.Marked = true;

                            listFile.SelectedItems[0].BackColor = Color.Orange;

                            pFiles[int.Parse(fileId.Text, System.Globalization.NumberStyles.HexNumber)] = file;

                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            #endregion
        }
        #region Analyze BW
        private void GetFiles()
        {
            try
            {
                //Clear list
                pFiles.Clear();

                Invoke((MethodInvoker)delegate { WriteLog("offset\tsize\t\tname"); WriteLog("-----------------------------------------------"); });

                CSifFile curFile = new CSifFile();

                byte[] currentBinary = file_Bin.Skip(pHeader.Offset + 0x0004).ToArray();
                // Take unique bytes
                uniqueBytes = currentBinary.Take(0x0010).ToArray();
                currentBinary = currentBinary.Skip(0x0010).ToArray();
                // Take file count
                total_file = currentBinary.Take(0x0004).ToArray();
                currentBinary = currentBinary.Skip(0x0004).ToArray();

                Invoke((MethodInvoker)delegate { totalFileStripStatusLabel1.Text = "Total File : " + BitConverter.ToInt32(total_file, 0); });

                for (int i = 0; i < BitConverter.ToInt32(total_file, 0); i++)
                {
                    curFile = Functions.RawDataToObject<CSifFile>(currentBinary);

                    byte[] fileName = new byte[curFile.NameSize];

                    currentBinary = currentBinary.Skip(Marshal.SizeOf(typeof(CSifFile)) - 2).ToArray(); // Adjusted length

                    Functions.getArray(currentBinary, 0, fileName);

                    currentBinary = currentBinary.Skip(curFile.NameSize).ToArray(); // Adjusted length end

                    //fileBin
                    byte[] assetFileBin = new byte[curFile.Size];
                    Functions.getArray(file_Bin, curFile.Offset, assetFileBin);

                    SifFile sf = new SifFile();
                    sf.Path = Path.GetDirectoryName(Encoding.Default.GetString(fileName).ToString().Replace('\\', '/').Replace("\0", string.Empty)).Replace('\\', '/');
                    sf.FileName = (Path.GetFileName(Encoding.Default.GetString(fileName).Replace("\0", string.Empty)));
                    sf.Binary = assetFileBin.ToList();
                    sf.Offset = curFile.Offset;
                    sf.Size = curFile.Size;
                    sf._Dummy = curFile._sDummy.ToList();
                    sf.Marked = false;

                    pFiles.Add(sf);


                    Invoke(
                        (MethodInvoker)delegate
                        {
                            WriteLog(
                                curFile.Offset.ToString("X8") + "\t" +
                                curFile.Size.ToString("X8") + "\t" +
                                Encoding.Default.GetString(fileName));
                        }
                    );

                    bwAnalyze.ReportProgress((i + 1) * 100 / BitConverter.ToInt32(total_file, 0));
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Invoke((MethodInvoker)delegate { WriteLog(ex.Message); });
            }
        }

        private void GetPath()
        {
            Invoke((MethodInvoker)delegate
            {
                treePath.PathSeparator = @"/";

                Functions.PopulateTreeView(treePath, pFiles.Select(x => x.Path).ToArray(), '/');
                treePath.Sort();
            });
        }
        #endregion
        #region Extract BW
        private void ExtractToPath(string selPath)
        {
            try
            {
                int count = 0;
                //Create path first
                List<string> path = pFiles.Select(x => x.Path).Distinct().ToList();

                int maxCount = path.Count + pFiles.Count;

                foreach (var _p in path)
                {
                    Invoke(
                           (MethodInvoker)delegate
                           {
                               WriteLog("Create directory to " + selPath + "\\" + _p);
                           }
                       );
                    count++;
                    Directory.CreateDirectory(selPath + "\\" + _p);
                    bwExtract.ReportProgress((count + 1) * 100 / maxCount);
                }
                //Write file to path
                foreach (var _f in pFiles)
                {
                    Invoke(
                           (MethodInvoker)delegate
                           {
                               WriteLog("Write file to " + selPath + "\\" + _f.Path + "\\" + _f.FileName);
                           }
                       );
                    count++;
                    File.WriteAllBytes(selPath + "\\" + _f.Path + "\\" + _f.FileName, _f.Binary.ToArray());
                    bwExtract.ReportProgress((count + 1) * 100 / maxCount);
                }
                Invoke(
                        (MethodInvoker)delegate
                        {
                            MessageBox.Show("Extracted successfully", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                Arguments = selPath,
                                FileName = "explorer.exe"
                            };
                            Process.Start(startInfo);
                        }
                    );
            }
            catch (Exception ex)
            {
                Invoke(
                        (MethodInvoker)delegate
                        {
                            WriteLog(ex.Message);
                            MessageBox.Show(ex.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            extractToolStripMenuItem.Enabled = true;
                            openToolStripMenuItem.Enabled = true;
                        }
                    );
            }

        }
        #endregion
        #region Save BW
        private void SaveFile()
        {
            try
            {
                List<byte> allBinary = new List<byte>();
                var fcount = pFiles.Count();
                int _fcount = fcount;
                foreach (var binFile in pFiles.Select(x => x.Binary).ToList())
                {
                    foreach (var byteBin in binFile)
                    {
                        allBinary.Add(byteBin);
                    }
                }

                using (FileStream fs = new FileStream(selPath, FileMode.Create))
                {
                    fs.FlushAsync();
                    int startOffset = 0;
                    fs.Write(pHeader.Header, 0, pHeader.Header.Length); // FFAS
                    fs.Write(Functions.INT32ToLittleEndian(pHeader.Dummy), 0, Functions.INT32ToLittleEndian(pHeader.Dummy).Length); //DUMMY
                    startOffset = (int)(fs.Length + Functions.INT32ToLittleEndian(pHeader.Dummy).Length);
                    fs.Write(Functions.INT32ToLittleEndian((int)(fs.Length + allBinary.Count() + 4)), 0, 4); //OFFSET
                    fs.Write(allBinary.ToArray(), 0, allBinary.ToArray().Length); // File
                    fs.Write(Functions.INT32ToLittleEndian(1), 0, 4);
                    fs.Write(uniqueBytes, 0, uniqueBytes.Length);
                    fs.Write(total_file, 0, total_file.Length);

                    int offsetFile = startOffset;
                    foreach (SifFile _file in pFiles)
                    {
                        byte[] _offstFile = Functions.INT32ToLittleEndian(offsetFile);
                        fs.Write(_offstFile, 0, _offstFile.Length);
                        byte[] _fileSize = Functions.INT32ToLittleEndian(_file.Binary.Count);
                        fs.Write(_fileSize, 0, _fileSize.Length);
                        //byte[] _fileHash = Functions.GetMD5checksum(_file.Binary.ToArray());
                        //fs.Write(_fileHash, 0, _fileHash.Count());
                        fs.Write(_file._Dummy.ToArray(), 0, _file._Dummy.Count());
                        //fileName
                        byte[] _fileName = Encoding.UTF8.GetBytes(_file.Path + "/" + _file.FileName + "\x0");
                        byte[] _fileNameSize = Functions.INT16ToLittleEndian((Int16)_fileName.Length);
                        fs.Write(_fileNameSize, 0, _fileNameSize.Length);
                        fs.Write(_fileName, 0, _fileName.Length);

                        offsetFile += _file.Binary.Count();
                    }
                
                }

                MessageBox.Show("File saved successfully", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion
        private void ReadBinary()
        {
            try
            {
                pHeader = Functions.RawDataToObject<CSifHeader>(file_Bin);
                if (Encoding.Default.GetString(pHeader.Header) != "FFAS")
                {
                    MessageBox.Show("Corrupt file!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    extractToolStripMenuItem.Enabled = false;
                }
                else
                    bwAnalyze.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}