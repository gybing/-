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
using System.Diagnostics;

namespace 接收文件
{
    public partial class Receiver2 : Form
    {
        public Receiver2()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        KellFileTransfer.ReceiveListenerArgs rl;

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
            StartListen();
        }

        private void StartListen()
        {
            if (textBox1.Text.Trim() == "")
            {
                string ip = KellFileTransfer.Common.GetAppSettingConfig("ip");
                if (string.IsNullOrEmpty(ip))
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
                    textBox1.Text = LocalIP.ToString();
                }
                else
                {
                    textBox1.Text = ip;
                }
            }
            if (textBox2.Text.Trim() == "")
            {
                string port = KellFileTransfer.Common.GetAppSettingConfig("port");
                if (string.IsNullOrEmpty(port))
                {
                    textBox2.Text = port;
                }
                else
                {
                    textBox2.Text = "8000";
                }
            }

            Logs.Create("开始监听...");

            KellFileTransfer.FileUploader.ReceiveFinished += new EventHandler<KellFileTransfer.ReceiveFinishArgs>(FileTransfer_ReceiveFinished);
            rl = KellFileTransfer.FileUploader.StartReceiveFile(textBox3.Text, IPAddress.Parse(textBox1.Text), int.Parse(textBox2.Text.Trim()));
            if (rl.Thread.ThreadState == System.Threading.ThreadState.Running && rl.Listener != null)
            {
                toolStripStatusLabel1.Text = "正在监听端口[" + textBox2.Text.Trim() + "]上的客户端连接...";
                checkBox1.Enabled = button1.Enabled = textBox1.Enabled = textBox2.Enabled = textBox3.Enabled = false;
                button3.Enabled = true;

                Logs.Create("监听成功！");
            }
            else
            {
                Logs.Create("监听失败！");
            }
        }

        void FileTransfer_ReceiveFinished(object sender, KellFileTransfer.ReceiveFinishArgs e)
        {
            DateTime time = DateTime.Now;
            //string dir = textBox3.Text.Trim();
            //if (dir == "")
            //    dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            listBox1.Items.Add(time.ToString("yyyy-MM-dd HH:mm:ss") + " -- " + e.Directory + "\\" + e.Filename);

            Logs.Create("成功接收到文件：" + e.Directory + "\\" + e.Filename);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (KellFileTransfer.FileUploader.StopReceiveFile(rl))
            {
                toolStripStatusLabel1.Text = "连接已经关闭，并已停止监听！";
                checkBox1.Enabled = button1.Enabled = textBox1.Enabled = textBox2.Enabled = textBox3.Enabled = true;
                button3.Enabled = false;
            }
            else
            {
                MessageBox.Show("无法完成操作！");
            }
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

        private void button2_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(textBox3.Text))
                Process.Start(textBox3.Text);
            else
                Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }

        bool loadOver;
        private bool sure;
        private void Receiver2_Load(object sender, EventArgs e)
        {
            Logs.Create("服务程序已启动.");

            textBox3.Text = KellFileTransfer.Common.GetAppSettingConfig("dir");
            textBox1.Text = KellFileTransfer.Common.GetAppSettingConfig("ip");
            textBox2.Text = KellFileTransfer.Common.GetAppSettingConfig("port");
            string filename = Path.GetFileName(Application.ExecutablePath);
            //string path = KellFileTransfer.Common.ReadRegistry("ReceiveFile");
            //if (path != "" && path.Equals(Application.ExecutablePath, StringComparison.InvariantCultureIgnoreCase))
            checkBox2.Checked = IsAutoStartupAllUsers(filename) || IsAutoStartupCurrentUser(filename);// true;
            checkBox3.Checked = KellFileTransfer.Common.GetAppSettingConfig("autoShutdown") == "1";
            if (checkBox3.Checked)
                timer1.Start();
            int defaultHour = 23;
            int defaultMin = 10;
            string defaultShutdownTime = KellFileTransfer.Common.GetAppSettingConfig("defaultShutdownTime");
            if (!string.IsNullOrEmpty(defaultShutdownTime))
            {
                string[] hourMin = defaultShutdownTime.Split(":".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (hourMin.Length == 2)
                {
                    int H;
                    if (int.TryParse(hourMin[0], out H))
                        defaultHour = H;
                    int M;
                    if (int.TryParse(hourMin[1], out M))
                        defaultMin = M;
                }
            }
            if (defaultHour > -1 && defaultHour < 24)
                comboBox1.SelectedIndex = defaultHour;
            LoadMinutes(defaultMin);
            loadOver = true;
        }

        private void LoadMinutes(int defaultMin)
        {
            comboBox2.Items.Clear();
            for (int i = 0; i < 60; i++)
            {
                string min = i.ToString().PadLeft(2, '0');
                comboBox2.Items.Add(min);
            }
            if (defaultMin > -1 && defaultMin < 60)
                comboBox2.SelectedIndex = defaultMin;
        }

        private void Receiver2_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (sure)
            {
                if (MessageBox.Show("确定要退出服务吗？", "退出提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
                {
                    SaveAndEnd();
                    notifyIcon1.Dispose();
                    Environment.Exit(0);
                }
                else
                {
                    e.Cancel = true;
                    sure = false;
                }
            }
            else
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void Exit()
        {
            sure = true;
            this.Close();
        }

        private void SaveAndEnd()
        {
            string defaultShutdownTime = comboBox1.Text + ":" + comboBox2.Text;
            if (rl != null)
            {
                KellFileTransfer.Common.SaveAppSettingConfig("dir", rl.Directory);
                IPEndPoint ipep = rl.Listener.LocalEndpoint as IPEndPoint;
                if (ipep != null)
                {
                    KellFileTransfer.Common.SaveAppSettingConfig("ip", ipep.Address.ToString());
                    KellFileTransfer.Common.SaveAppSettingConfig("port", ipep.Port.ToString());
                }
                try
                {
                    bool f = KellFileTransfer.FileUploader.StopReceiveFile(rl);
                    if (!f)
                        MessageBox.Show("停止监听失败！");
                }
                catch (Exception e)
                {
                    MessageBox.Show("停止监听服务出现异常：" + e.Message);
                }
                finally
                {
                    KellFileTransfer.Common.SaveAppSettingConfig("defaultShutdownTime", defaultShutdownTime);
                }
            }
            else
            {
                KellFileTransfer.Common.SaveAppSettingConfig("dir", textBox3.Text.Trim());
                KellFileTransfer.Common.SaveAppSettingConfig("ip", textBox1.Text);
                KellFileTransfer.Common.SaveAppSettingConfig("port", textBox2.Text.Trim());
                KellFileTransfer.Common.SaveAppSettingConfig("defaultShutdownTime", defaultShutdownTime);
            }
            Logs.Create("服务程序退出.");
        }

        private void ShowUI()
        {
            this.WindowState = FormWindowState.Normal;
            this.Show();
            this.BringToFront();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (loadOver)
            {
                AutoStartup(Application.ExecutablePath, checkBox2.Checked);
                //if (checkBox2.Checked)
                //    KellFileTransfer.Common.SetSelfStarting(Application.ExecutablePath, "ReceiveFile");
                //else
                //    KellFileTransfer.Common.CancelSelfStarting("ReceiveFile");
            }
        }

        private void AutoStartup(string executablePath, bool flag)
        {
            string filename = Path.GetFileName(executablePath);
            //string path = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string path = Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 1) + @":\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup";
            if (!Directory.Exists(path))
                path = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string file = path + "\\" + filename + ".lnk";
            if (flag)
            {
                if (!IsAutoStartupAllUsers(filename))
                {                    
                    try
                    {
                        CreateShortcut(executablePath, path);
                    }
                    catch (Exception e)
                    {
                        CreateShortcut(executablePath, Environment.GetFolderPath(Environment.SpecialFolder.Startup));
                    }
                }
                else if (!IsAutoStartupCurrentUser(filename))
                {
                    CreateShortcut(executablePath, Environment.GetFolderPath(Environment.SpecialFolder.Startup));
                }
            }
            else
            {
                if (IsAutoStartupAllUsers(filename))
                {
                    File.Delete(file);
                }
                if (IsAutoStartupCurrentUser(filename))
                {
                    string file2 = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\" + filename + ".lnk";
                    File.Delete(file2);
                }
            }
        }

        private static void CreateShortcut(string executablePath, string path)
        {
            // 声明操作对象
            IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShellClass();
            string filename = Path.GetFileName(executablePath);
            string file = path + "\\" + filename + ".lnk";
            // 创建一个快捷方式
            IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(file);
            // 关联的程序
            shortcut.TargetPath = executablePath;
            // 参数
            //shortcut.Arguments = "";
            // 快捷方式描述，鼠标放到快捷方式上会显示出来哦
            shortcut.Description = filename + "应用程序";
            // 全局热键
            //shortcut.Hotkey = "CTRL+SHIFT+N";
            // 设置快捷方式的图标，这里是取程序图标，如果希望指定一个ico文件，那么请写路径。
            //shortcut.IconLocation = "notepad.exe, 0";
            // 保存，创建就成功了。
            shortcut.Save();
        }

        private bool IsAutoStartupAllUsers(string filename)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 1) + @":\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup";
            if (Directory.Exists(path))
            {
                string lnk = path + "\\" + filename + ".lnk";
                string[] files = Directory.GetFiles(path, "*.lnk", SearchOption.TopDirectoryOnly);
                foreach (string file in files)
                {
                    if (file.Equals(lnk, StringComparison.InvariantCultureIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private bool IsAutoStartupCurrentUser(string filename)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string lnk = path + "\\" + filename + ".lnk";
            string[] files = Directory.GetFiles(path, "*.lnk", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                if (file.Equals(lnk, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }


        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (listBox1.SelectedItem != null)
                {
                    try
                    {
                        Process.Start(listBox1.SelectedItem.ToString());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowUI();
        }

        private void 显示界面ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowUI();
        }

        private void 退出服务ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Exit();
        }

        private void Receiver2_Shown(object sender, EventArgs e)
        {
            this.Hide();
            StartListen();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (loadOver)
            {
                if (checkBox3.Checked)
                {
                    if (!timer1.Enabled)
                        timer1.Start();
                }
                else
                {
                    if (timer1.Enabled)
                        timer1.Stop();
                }
                KellFileTransfer.Common.SaveAppSettingConfig("autoShutdown", checkBox3.Checked ? "1" : "0");
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            if (DateTime.Now.Hour.ToString().PadLeft(2, '0') == comboBox1.Text && DateTime.Now.Minute.ToString().PadLeft(2, '0') == comboBox2.Text)
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
                    p.StandardInput.WriteLine("shutdown -s -f -t 30");//有30秒的时候留待取消关机
                    p.Close();
                }
            }
            timer1.Start();
        }
    }
}