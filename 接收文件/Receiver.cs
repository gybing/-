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
using System.Threading;

namespace 接收文件
{
    public partial class Receiver : Form
    {
        public Receiver()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        private Thread thThreadRead;
        //创建线程，用以侦听端口号，接收信息
        private TcpListener tlTcpListen;
        //侦听端口号
        private bool blistener = true;
        //设定标示位，判断侦听状态
        private Socket stRead;
        string ReceiveDir = "";
        int cnt = 0;
        string filename = "";

        private void button1_Click(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                if (!Directory.Exists(textBox3.Text))
                {
                    MessageBox.Show("目录路径有误，请检查您指定保存的路径！");
                    return;
                }
            }
            thThreadRead = new Thread(new ThreadStart(Listen));
            //以Listen过程来初始化Thread实例
            thThreadRead.Start();
            //启动线程
            button1.Enabled = false;
            button3.Enabled = true;
        }

        private void Listen()
        {
            try
            {
                IPHostEntry ieh = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress LocalIP = IPAddress.Loopback;
                for (int i = 0; i < ieh.AddressList.Length; i++)
                {
                    if (ieh.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        LocalIP = ieh.AddressList[i];
                        break;
                    }
                }
                int LocalPort = Int32.Parse(textBox2.Text);
                this.textBox1.Text = LocalIP.ToString();
                ReceiveDir = textBox3.Text.Trim();
                //if (Directory.Exists(ReceiveDir))
                //{
                //    filename = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                //}
                tlTcpListen = new TcpListener(LocalIP, LocalPort);
                //以8000端口号来初始化TcpListener实例
                tlTcpListen.Start();
                //开始监听网络的连接请求
                toolStripStatusLabel1.Text = "程序正在监听...";
                stRead = tlTcpListen.AcceptSocket();
                //通过连接请求，并获得接收数据时使用的Socket实例
                EndPoint tempRemoteEP = stRead.RemoteEndPoint;
                IPEndPoint tempRemoteIP = (IPEndPoint)tempRemoteEP;
                //获取请求的远程计算机名称
                IPHostEntry host = Dns.GetHostEntry(tempRemoteIP.Address);
                string sHostName = host.HostName;
                toolStripStatusLabel1.Text = "远程电脑: " + sHostName + " 已经与本机通过端口 " + LocalPort.ToString() + " 成功建立连接！";
                //同意和发送端计算机建立连接
                string ReceiveContent = "";
                string sTime = DateTime.Now.ToShortTimeString();
                int iRead;
                const int readOneTime = 4096;
                //循环侦听
                while (blistener)
                {
                    //获取接收数据时的时间
                    Byte[] byRead = new Byte[readOneTime];
                    iRead = stRead.ReceiveFrom(byRead, ref tempRemoteEP);
                    cnt++;
                    //读取完成后退出循环 
                    if (iRead <= 0)
                    {
                        this.button1.Enabled = true;
                        //保存数据流为本地文件
                        byte[] content = Encoding.Default.GetBytes(ReceiveContent);
                        try
                        {
                            if (filename == "")
                                filename = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                            if (!Directory.Exists(ReceiveDir))
                                Directory.CreateDirectory(ReceiveDir);
                            //创建文件流对象实例 
                            FileStream fs = new FileStream(ReceiveDir + "\\" + filename, FileMode.OpenOrCreate, FileAccess.Write);
                            //写入文件
                            fs.Write(content, 0, content.Length);
                            fs.Close();
                            /*会有乱码！
                            using (FileStream ffs = new FileStream(ReceiveFileName, FileMode.Open, FileAccess.Read))
                            {
                                using (TextReader rd = new StreamReader(ffs))
                                {
                                    ReceiveContent = rd.ReadToEnd();
                                }
                            }*/
                        }
                        catch (Exception fe)
                        {
                            MessageBox.Show("文件创建/写入错误:" + fe.Message, "提示信息", MessageBoxButtons.RetryCancel, MessageBoxIcon.Information);
                        }
                        //richTextBox1.Text ="\n\r接收时间 "+ sTime + "\n\r接收文件内容 " +ReceiveContent ;
                        richTextBox1.Text = ReceiveContent;
                        stRead.Close();
                        //tlTcpListen.Stop();
                        break;
                    }
                    else
                    {
                        if (cnt == 1)
                        {
                                filename = TrimTheNullByte(Encoding.Default.GetString(byRead, 0, 256));
                            if (!checkBox1.Checked)//指定保存位置
                            {
                                textBox3.Text = ReceiveDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                            }
                            else
                            {
                                ReceiveDir = textBox3.Text;
                                if (!Directory.Exists(ReceiveDir))
                                    Directory.CreateDirectory(ReceiveDir);
                            }
                            ReceiveContent += Encoding.Default.GetString(byRead, 256, readOneTime - 256);
                            //MessageBox.Show(cnt.ToString()+":\n"+ReceiveContent);
                        }
                        else
                        {
                            //将读取的字节数转换为字符串 
                            ReceiveContent += Encoding.Default.GetString(byRead);
                            //MessageBox.Show(cnt.ToString()+":\n"+ReceiveContent);
                        }
                    }
                }
            }
            catch (System.Security.SecurityException)
            {
                MessageBox.Show("侦听失败！", "错误");
                this.button1.Enabled = true;
            }
        }

        private string TrimTheNullByte(string p)
        {
            string ret = "";
            int i = 0;
            byte[] b = new byte[1];
            while (i < p.Length)
            {
                string s = p.Substring(i, 1);
                if (s != Encoding.Default.GetString(b))
                    ret += s;
                i++;
            }
            return ret;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            stRead.Close();
            tlTcpListen.Stop();
            //关闭侦听
            toolStripStatusLabel1.Text = "连接已经关闭！";
            thThreadRead.Abort();
            //中止线程
            this.button1.Enabled = true;
            this.button3.Enabled = false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox3.Enabled = checkBox1.Checked;
            if (checkBox1.Checked)
            {
                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                    textBox3.Text = folderBrowserDialog1.SelectedPath;
                }
                folderBrowserDialog1.Dispose();
            }
        }

    }
}