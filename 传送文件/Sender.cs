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
    public partial class Sender : Form
    {
        public Sender()
        {
            InitializeComponent();
        }

        //创建Socket 实例 
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox3.Text.Trim() == "")
            {
                MessageBox.Show("请输入要传送的文件！");
                return;
            }
            //取得预发送的文件名 
            string SendFileName = textBox3.Text.Trim();
            //远程主机 
            string RemoteHostIP = textBox1.Text.Trim();
            //远程端口 
            int RemotePort = Int32.Parse(textBox2.Text.Trim());
            //得到主机信息 
            IPHostEntry ipInfo = Dns.GetHostEntry(RemoteHostIP);
            //取得IPAddress[] 
            IPAddress[] ipAddr = ipInfo.AddressList;
            //得到远程接收IP 
            IPAddress ip = ipAddr[0];
            //组合出远程终结点 
            IPEndPoint hostEP = new IPEndPoint(ip, RemotePort);
            
            try
            {
                //尝试连接 
                socket.Connect(hostEP);
                //socket.BeginConnect(hostEP, new AsyncCallback(Send), socket);
            }
            catch (Exception se)
            {
                MessageBox.Show("连接远程IP错误" + se.Message, "提示信息", MessageBoxButtons.RetryCancel, MessageBoxIcon.Information);
            }
            FileInfo fi = new FileInfo(SendFileName);
            byte[] fileName = new byte[256];
            byte[] bs = Encoding.Default.GetBytes(fi.Name);
            for (int i = 0; i < bs.Length; i++)
            {
                fileName[i] = bs[i];
            }
            socket.SendFile(SendFileName, fileName, null, TransmitFileOptions.UseDefaultWorkerThread);
            socket.Disconnect(true);
            /*
            //发送文件流代码

            NetworkStream tcpStream = new NetworkStream(socket);
            FileStream fs = new FileStream(SendFileName, FileMode.Open, FileAccess.Read);
            BinaryReader fileReader = new BinaryReader(fs, Encoding.Default);
            long Total = fs.Length;
            byte[] bytes = new byte[Total];
            int hasRead = 0;
            int len;
            //获取要传输文件的总长度
            //读取文件写入到数据流中
            while ((hasRead <= fs.Length) && tcpStream.CanWrite && fileReader.Read(bytes, 0, bytes.Length) != 0)
            //while((hasRead  =  fileReader.Read(bytes,0,bytes.Length)) !=  0)   
            {
                //从要传输的文件读取指定长度的数据
                len = fileReader.Read(bytes, 0, bytes.Length);
                //将读取的数据发送到对应的计算机
                tcpStream.Write(bytes, 0, hasRead);
                try
                {
                    //向主机发送请求 
                    socket.Send(bytes, bytes.Length, 0);                    
                    this.toolStripStatusLabel1.Text += " " + hasRead;

                }
                catch (Exception ce)
                {
                    MessageBox.Show("发送错误:" + ce.Message, "提示信息", MessageBoxButtons.RetryCancel, MessageBoxIcon.Information);
                }
                //增加已经发送的长度
                hasRead += len;
            }
            fileReader.Close();
            fs.Close();*/
            //禁用Socket 
            //socket.Shutdown(SocketShutdown.Send);
            //关闭Socket 
            //socket.Close();
            
            
            this.toolStripStatusLabel1.Text = "文件成功发送!"; 

        }

        private void Send(IAsyncResult e)
        {
            Socket socket = e.AsyncState as Socket;
            string SendFileName = textBox3.Text.Trim();
            FileInfo fi = new FileInfo(SendFileName);
            byte[] fileName = new byte[256];
            byte[] bs = Encoding.Default.GetBytes(fi.Name);
            for (int i = 0; i < bs.Length; i++)
            {
                fileName[i] = bs[i];
            }
            socket.SendFile(SendFileName, fileName, null, TransmitFileOptions.UseDefaultWorkerThread);
            socket.EndDisconnect(e);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox3.Text = openFileDialog1.FileName;
            }
            openFileDialog1.Dispose();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //禁用Socket 
            socket.Shutdown(SocketShutdown.Send);
            //关闭Socket 
            socket.Close();
        }
    }
}