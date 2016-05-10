using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Net;
using System.Timers;
using Microsoft.Win32;

namespace FileReceiveService
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        KellFileTransfer.ReceiveListenerArgs rl;
        Timer timer1;

        protected override void OnStart(string[] args)
        {
            ////当用户试图重启或关闭系统时发生，但是有了OnShutdown()的重载，就可以免去该操作！
            //SystemEvents.SessionEnding += new SessionEndingEventHandler(SystemEvents_SessionEnding);
            Logs.Create("开始启动监听服务...");
            timer1 = new Timer(60000);//关机轮询间隔时间为1分钟
            timer1.Elapsed += new ElapsedEventHandler(timer1_Elapsed);
            timer1.Start();
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

        void timer1_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer1.Stop();
            string autoShutdown = KellFileTransfer.Common.GetAppSettingConfig("autoShutdown");
            if (!string.IsNullOrEmpty(autoShutdown) && autoShutdown == "1")
            {
                string defaultShutdownTime = KellFileTransfer.Common.GetAppSettingConfig("defaultShutdownTime");
                if (!string.IsNullOrEmpty(defaultShutdownTime))
                {
                    try
                    {
                        int hour = 0;
                        int minute = 0;
                        string[] hm = defaultShutdownTime.Split(':');
                        if (hm.Length == 2)
                        {
                            hour = Convert.ToInt32(hm[0]);
                            minute = Convert.ToInt32(hm[1]);
                        }
                        else
                        {
                            hour = Convert.ToInt32(hm[0]);
                        }
                        if (DateTime.Now.Hour == hour && DateTime.Now.Minute == minute)
                        {
                            Logs.Create("本服务尝试自动关机...");
                            string shutdownWithCmd = KellFileTransfer.Common.GetAppSettingConfig("shutdownWithCmd");
                            if (!string.IsNullOrEmpty(shutdownWithCmd) && shutdownWithCmd == "1")
                            {
                                using (Process p = new Process())
                                {
                                    p.StartInfo.FileName = "cmd.exe";
                                    p.StartInfo.UseShellExecute = false;
                                    p.StartInfo.RedirectStandardInput = true;
                                    p.StartInfo.RedirectStandardOutput = true;
                                    p.StartInfo.RedirectStandardError = true;
                                    p.StartInfo.CreateNoWindow = true;
                                    p.Start();
                                    p.StandardInput.WriteLine("shutdown -s -f -t 30");//有30秒的时间留待取消关机
                                    p.Close();
                                }
                                Logs.Create("自动关机命令执行中...有30秒的时间留待取消关机（取消关机命令：shutdown -a）。");
                            }
                            else
                            {
                                // 修改 EWX_SHUTDOWN 或者 EWX_LOGOFF, EWX_REBOOT等实现不同得功能。
                                // 在XP下可以看到帮助信息，以得到不同得参数
                                // SHUTDOWN /?
                                Shutdown.DoExitWin(Shutdown.EWX_SHUTDOWN);
                                Logs.Create("自动关机命令执行中...");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logs.Create("定时轮询自动关机时失败：" + ex.Message);
                    }
                }
            }
            timer1.Start();
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
                    if (timer1 != null)
                    {
                        if (timer1.Enabled)
                            timer1.Stop();
                        timer1.Dispose();
                    }
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

        protected override void OnShutdown()
        {
            Logs.Create("系统正在关机或重启...");
            OnStop();
            base.OnShutdown();
        }

        ////下面是系统关闭事件处理程序
        //private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        //{
        //    SessionEndReasons reason = e.Reason;
        //    if (reason == SessionEndReasons.SystemShutdown)
        //    {
        //        this.OnStop();
        //    }
        //}
    }
}
