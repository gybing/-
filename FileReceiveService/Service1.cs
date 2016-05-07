using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Net;

namespace FileReceiveService
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        KellFileTransfer.ReceiveListenerArgs rl;

        protected override void OnStart(string[] args)
        {
            Logs.Create("开始启动监听服务...");

            IPAddress LocalIP = IPAddress.Loopback;
            int LocalPort = 8000;
            string dir = KellFileTransfer.Common.GetAppSettingConfig("dir");
            string ip = KellFileTransfer.Common.GetAppSettingConfig("ip");
            string port = KellFileTransfer.Common.GetAppSettingConfig("port");
            if (!string.IsNullOrEmpty(ip))
                LocalIP = IPAddress.Parse(ip);
            if (!string.IsNullOrEmpty(port))
                LocalPort = int.Parse(port);
            KellFileTransfer.FileUploader.ReceiveFinished += new EventHandler<KellFileTransfer.ReceiveFinishArgs>(FileTransfer_ReceiveFinished);
            rl = KellFileTransfer.FileUploader.StartReceiveFile(dir, LocalIP, LocalPort);
            if (rl.Thread.ThreadState == System.Threading.ThreadState.Running && rl.Listener != null)
            {
                Logs.Create("监听成功！");
            }
            else
            {
                Logs.Create("监听失败！");
            }
        }

        void FileTransfer_ReceiveFinished(object sender, KellFileTransfer.ReceiveFinishArgs e)
        {
            Logs.Create("成功接收到文件：" + e.Directory + "\\" + e.Filename);
        }

        protected override void OnStop()
        {
            if (rl != null)
            {
                try
                {
                    bool f = KellFileTransfer.FileUploader.StopReceiveFile(rl);
                    if (!f)
                        Logs.Create("停止监听失败！");
                    else
                        Logs.Create("停止监听成功！");
                }
                catch (Exception e)
                {
                    Logs.Create("停止监听服务时出现异常：" + e.Message);
                }
            }
            else
            {
                Logs.Create("监听线程尚未启动，无需停止监听，监听服务程序直接退出.");
            }
        }
    }
}
