using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections;

namespace 传送文件
{
    public partial class Sender2 : Form
    {
        public Sender2()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox3.Text.Trim() == "")
            {
                MessageBox.Show("请输入要传送的文件！");
                return;
            }
            textBox3.Enabled = button1.Enabled = button2.Enabled = false;
            try
            {
                //取得预发送的文件名 
                string SendFileName = textBox3.Text.Trim();
                //远程主机 
                string RemoteHostIP = textBox1.Text.Trim();
                //远程端口 
                int RemotePort = Int32.Parse(textBox2.Text.Trim());
                //得到主机信息
                bool isHostName;
                if (KellFileTransfer.Common.IsValidHostOrAddress(RemoteHostIP, out isHostName))
                {
                    if (isHostName)
                    {
                        toolStripStatusLabel1.Text = "文件发送中...";
                        statusStrip1.Refresh();
                        if (KellFileTransfer.FileUploader.SendFile(SendFileName, RemoteHostIP, RemotePort))
                            toolStripStatusLabel1.Text = "文件发送成功!";
                        else
                            toolStripStatusLabel1.Text = "远程服务器可能已经下线，文件发送失败!";
                    }
                    else
                    {
                        //组合出远程终结点
                        IPEndPoint hostEP = new IPEndPoint(IPAddress.Parse(RemoteHostIP), RemotePort);
                        toolStripStatusLabel1.Text = "文件发送中...";
                        statusStrip1.Refresh();
                        if (KellFileTransfer.FileUploader.SendFile(SendFileName, hostEP))
                            toolStripStatusLabel1.Text = "文件发送成功!";
                        else
                            toolStripStatusLabel1.Text = "远程服务器可能已经下线，文件发送失败!";
                    }
                }
                else
                {
                    MessageBox.Show("无效的IP地址或者主机名！");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                textBox3.Enabled = button1.Enabled = button2.Enabled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox3.Text = openFileDialog1.FileName;
                toolStripStatusLabel1.Text = "Ready...";
            }
            openFileDialog1.Dispose();
        }

        private void Sender2_Load(object sender, EventArgs e)
        {
            //IPEndPoint ipep = KellFileTransfer.Common.GetUploadIPEndPoint();
            string ip = KellFileTransfer.Common.GetAppSettingConfig("ip");
            string port = KellFileTransfer.Common.GetAppSettingConfig("port");
            textBox1.Text = ip;
            textBox2.Text = port;
        }
    }
}