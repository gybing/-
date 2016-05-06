using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;

namespace DownloadServerApp
{
    public partial class DownloadServer : Form
    {
        public DownloadServer()
        {
            InitializeComponent();
        }

        string dir = @"F:\Backup";
        List<KellFileTransfer.FILELIST> list;
        bool showErrMsg = true;

        private void button1_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "指定供客户端下载的升级目录";
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
                dir = textBox1.Text;
                if (string.IsNullOrEmpty(dir))
                {
                    dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            folderBrowserDialog1.Dispose();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            dir = textBox1.Text;
            if (string.IsNullOrEmpty(dir))
            {
                dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            KellFileTransfer.FileDownloadServer server = new KellFileTransfer.FileDownloadServer(dir);
            IPEndPoint ipep = KellFileTransfer.Common.GetDownloadIPEndPoint();
            if (server.StartDownloadListen(ipep.Address, ipep.Port))
            {
                button2.Text = "正在监听...";
                button2.Enabled = false;
            }
            else
            {
                MessageBox.Show("监听失败！");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "从指定最新程序所在目录复制...";
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox2.Text = folderBrowserDialog1.SelectedPath;
                list.Clear();
                GetFiles(folderBrowserDialog1.SelectedPath);
                if (list.Count > 0)
                {
                    listBox1.Items.Clear();
                    foreach (KellFileTransfer.FILELIST file in list)
                    {
                        listBox1.Items.Add("文件路径:" + file.文件路径 + " | 文件大小:" + file.文件大小);
                    }
                }
                else
                {
                    MessageBox.Show("该目录下没有任何可用的文件！");
                }
            }
            folderBrowserDialog1.Dispose();
        }

        private void GetFiles(string dir)
        {
            int dirLen = dir.Length;
            string[] files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                KellFileTransfer.FILELIST f = new KellFileTransfer.FILELIST();
                f.文件路径 = fi.FullName.Substring(dirLen + 1);
                f.文件大小 = fi.Length;
                try
                {
                    string fullname = this.dir + "\\" + f.文件路径;
                    string newDir = Path.GetDirectoryName(fullname);
                    if (!Directory.Exists(newDir))
                        Directory.CreateDirectory(newDir);
                    fi.CopyTo(fullname, true);
                    list.Add(f);
                }
                catch (Exception e)
                {
                    if (showErrMsg && MessageBox.Show("该文件[" + file + "]可能正被其他进程占用！无法添加。" + Environment.NewLine + e.Message, "是否继续显示此错误信息？", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != System.Windows.Forms.DialogResult.OK)
                        showErrMsg = false;
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
            {
                int index = listBox1.SelectedIndex;
                KellFileTransfer.FILELIST file = list[listBox1.SelectedIndex] as KellFileTransfer.FILELIST;
                if (file != null)
                {
                    try
                    {
                        File.Delete(this.dir + "\\" + file.文件路径);
                    }
                    catch { }
                    listBox1.Items.RemoveAt(index);
                    list.RemoveAt(index);
                }
            }
        }

        private void DownloadServer_Load(object sender, EventArgs e)
        {
            list = new List<KellFileTransfer.FILELIST>();
            listBox1.Tag = list;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            CreateConfigs();
        }

        private void CreateConfigs()
        {
            if (string.IsNullOrEmpty(dir))
            {
                MessageBox.Show("请先指定下载目录！");
                button1.Focus();
                return;
            }

            if (list.Count == 0)
            {
                MessageBox.Show("请先添加要给客户端更新的文件！");
                button3.Focus();
                return;
            }

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (Directory.Exists(dir))
            {
                string filelist = dir + "\\FILELIST.XML";
                KellFileTransfer.Common.SetFILELIST(filelist, list);
                string upgrade = dir + "\\UPGRADE.XML";
                KellFileTransfer.Common.SetUpgradeTime(upgrade);
                MessageBox.Show("生成升级配置文档成功！");
            }
            else
            {
                MessageBox.Show("指定的下载目录不存在！");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "添加更新文件...";
            openFileDialog1.Filter = "所有文件(*.*)|*.*";
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FileInfo fi = new FileInfo(openFileDialog1.FileName);
                KellFileTransfer.FILELIST file = new KellFileTransfer.FILELIST();
                file.文件路径 = fi.Name;
                file.文件大小 = fi.Length;
                try
                {
                    string fullname = this.dir + "\\" + file.文件路径;
                    string newDir = Path.GetDirectoryName(fullname);
                    if (!Directory.Exists(newDir))
                        Directory.CreateDirectory(newDir);
                    fi.CopyTo(fullname, true);
                    list.Add(file);
                    listBox1.Items.Add("文件路径:" + file.文件路径 + " | 文件大小:" + file.文件大小);
                }
                catch { }
            }
            openFileDialog1.Dispose();
        }
    }
}
