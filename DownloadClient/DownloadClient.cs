using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;

namespace DownloadClientApp
{
    /*
    UPGRADE.XML文件的内容固定如下：
    <?xml version="1.0" encoding="utf-8"?>
    <UPGRADE LastTime="2016-05-06 19:03:37" />
    */
    public partial class DownloadClient : Form
    {
        public DownloadClient()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        KellFileTransfer.FileDownloadClient client;
        List<KellFileTransfer.FILELIST> list;

        private void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            string host = IPAddress.Loopback.ToString();
            int Port = 9000;
            string ip = KellFileTransfer.Common.GetAppSettingConfig("downip");
            string port = KellFileTransfer.Common.GetAppSettingConfig("downport");
            if (!string.IsNullOrEmpty(ip))
                host = ip;
            int r;
            if (!string.IsNullOrEmpty(port) && int.TryParse(port, out r))
                Port = r;
            bool isHostName;
            if (KellFileTransfer.Common.IsValidHostOrAddress(host, out isHostName))
            {
                if (isHostName)
                {
                    if (client.DownloadRemoteFileListXml(host, Port))
                    {
                        list = client.GetLocalFileList();
                        foreach (KellFileTransfer.FILELIST file in list)
                        {
                            listBox1.Items.Add(file);
                        }
                    }
                }
                else
                {
                    if (client.DownloadRemoteFileListXml(IPAddress.Parse(host), Port))
                    {
                        list = client.GetLocalFileList();
                        foreach (KellFileTransfer.FILELIST file in list)
                        {
                            listBox1.Items.Add(file);
                        }
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (list != null && list.Count > 0)
            {
                string host = IPAddress.Loopback.ToString();
                int Port = 9000;
                string ip = KellFileTransfer.Common.GetAppSettingConfig("downip");
                string port = KellFileTransfer.Common.GetAppSettingConfig("downport");
                if (!string.IsNullOrEmpty(ip))
                    host = ip;
                int r;
                if (!string.IsNullOrEmpty(port) && int.TryParse(port, out r))
                    Port = r;
                bool isHostName;
                if (KellFileTransfer.Common.IsValidHostOrAddress(host, out isHostName))
                {
                    if (isHostName)
                    {
                        if (client.StartDownloadFilesFromServer(list, host, Port))
                        {
                            button2.Text = "正在下载...";
                            button2.Enabled = false;
                        }
                        else
                        {
                            MessageBox.Show("无法下载！");
                        }
                    }
                    else
                    {
                        if (client.StartDownloadFilesFromServer(list, IPAddress.Parse(host), Port))
                        {
                            button2.Text = "正在下载...";
                            button2.Enabled = false;
                        }
                        else
                        {
                            MessageBox.Show("无法下载！");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("本地目录中找不到更新文件列表XML文档，或者下载到的更新文件列表XML文档中没有需要更新的文件！");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
                string host = IPAddress.Loopback.ToString();
                int Port = 9000;
                string ip = KellFileTransfer.Common.GetAppSettingConfig("downip");
                string port = KellFileTransfer.Common.GetAppSettingConfig("downport");
                if (!string.IsNullOrEmpty(ip))
                    host = ip;
                int r;
                if (!string.IsNullOrEmpty(port) && int.TryParse(port, out r))
                    Port = r;
                bool isHostName;
                if (KellFileTransfer.Common.IsValidHostOrAddress(host, out isHostName))
                {
                    if (isHostName)
                    {
                        bool f = client.HasUpgrade(host, Port);
                        if (f)
                            MessageBox.Show("发现更新！");
                        else
                            MessageBox.Show("没有更新！");
                    }
                    else
                    {
                        bool f = client.HasUpgrade(IPAddress.Parse(host), Port);
                        if (f)
                            MessageBox.Show("发现更新！");
                        else
                            MessageBox.Show("没有更新！");
                    }
                }
        }

        private void DownloadClient_Load(object sender, EventArgs e)
        {
            //ipep = KellFileTransfer.Common.GetDownloadIPEndPoint();
            client = new KellFileTransfer.FileDownloadClient();
            client.DownloadBegined += new KellFileTransfer.DownloadHandler(client_DownloadBegined);
            client.DownloadAllFinished += new EventHandler(client_DownloadAllFinished);
            client.DownloadFinished += new KellFileTransfer.DownloadHandler(client_DownloadFinished);
            client.DownloadingError += new KellFileTransfer.DownloadErrorHandler(client_DownloadingError);
        }

        void client_DownloadAllFinished(object sender, EventArgs e)
        {
            button2.Text = "开始下载";
            button2.Enabled = true;
            button4.Text = "一键升级";
            button4.Enabled = true;
            MessageBox.Show("更新完毕！");
        }

        void client_DownloadBegined(KellFileTransfer.FileDownloadClient sender, string filename)
        {
            toolStripStatusLabel1.Text = "正在下载文件[" + filename + "]...";
        }

        void client_DownloadingError(KellFileTransfer.FileDownloadClient sender, Exception e)
        {
            toolStripStatusLabel1.Text += "出错：" + e.Message;
            MessageBox.Show("下载时出错：" + e.Message);
        }

        void client_DownloadFinished(KellFileTransfer.FileDownloadClient sender, string filename)
        {
            toolStripStatusLabel1.Text = "文件[" + filename + "]下载完毕！";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string host = IPAddress.Loopback.ToString();
            int Port = 9000;
            string ip = KellFileTransfer.Common.GetAppSettingConfig("downip");
            string port = KellFileTransfer.Common.GetAppSettingConfig("downport");
            if (!string.IsNullOrEmpty(ip))
                host = ip;
            int r;
            if (!string.IsNullOrEmpty(port) && int.TryParse(port, out r))
                Port = r;
            bool isHostName;
            if (KellFileTransfer.Common.IsValidHostOrAddress(host, out isHostName))
            {
                if (isHostName)
                {
                    Publish(host, Port);
                }
                else
                {
                    Publish(IPAddress.Parse(host), Port);
                }
            }
        }

        private void Publish(string host, int port)
        {
            if (client.HasUpgrade(host, port))
            {
                string mainName = textBox1.Text.Trim();
                if (!mainName.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase))
                    mainName += ".exe";
                string exeFileFullPath = Directory.GetCurrentDirectory() + "\\" + mainName;
                int procId;
                if (KellFileTransfer.Common.CheckMainFormIsRun(exeFileFullPath, out procId))
                {
                    if (MessageBox.Show("是否让升级程序强制关闭主程序？", "关闭提醒", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
                    {//强制退出主程序
                        if (!KellFileTransfer.Common.KillProcess(procId))
                            MessageBox.Show("无法关闭主程序，请手动终止[" + mainName + "]进程！");
                    }
                }
                if (client.DownloadRemoteFileListXml(host, port))
                {
                    List<KellFileTransfer.FILELIST> list = client.GetLocalFileList();
                    if (list.Count > 0)
                    {
                        listBox1.Items.Clear();
                        foreach (KellFileTransfer.FILELIST file in list)
                        {
                            listBox1.Items.Add(file);
                        }
                        if (client.StartDownloadFilesFromServer(list, host, port))
                        {
                            button4.Text = "正在升级...";
                            button4.Enabled = false;
                        }
                        else
                        {
                            MessageBox.Show("无法下载！");
                        }
                    }
                    else
                    {
                        MessageBox.Show("下载到的更新文件列表XML文档中没有需要更新的文件！");
                    }
                }
            }
            else
            {
                MessageBox.Show("当前程序已经是最新版本！");
            }
        }

        private void Publish(IPAddress address, int port)
        {
            if (client.HasUpgrade(address, port))
            {
                string mainName = textBox1.Text.Trim();
                if (!mainName.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase))
                    mainName += ".exe";
                string exeFileFullPath = Directory.GetCurrentDirectory() + "\\" + mainName;
                int procId;
                if (KellFileTransfer.Common.CheckMainFormIsRun(exeFileFullPath, out procId))
                {
                    if (MessageBox.Show("是否让升级程序强制关闭主程序？", "关闭提醒", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
                    {//强制退出主程序
                        if (!KellFileTransfer.Common.KillProcess(procId))
                            MessageBox.Show("无法关闭主程序，请手动终止[" + mainName + "]进程！");
                    }
                }
                if (client.DownloadRemoteFileListXml(address, port))
                {
                    List<KellFileTransfer.FILELIST> list = client.GetLocalFileList();
                    if (list.Count > 0)
                    {
                        listBox1.Items.Clear();
                        foreach (KellFileTransfer.FILELIST file in list)
                        {
                            listBox1.Items.Add(file);
                        }
                        if (client.StartDownloadFilesFromServer(list, address, port))
                        {
                            button4.Text = "正在升级...";
                            button4.Enabled = false;
                        }
                        else
                        {
                            MessageBox.Show("无法下载！");
                        }
                    }
                    else
                    {
                        MessageBox.Show("下载到的更新文件列表XML文档中没有需要更新的文件！");
                    }
                }
            }
            else
            {
                MessageBox.Show("当前程序已经是最新版本！");
            }
        }
    }
}
