using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using Microsoft.Win32;
using System.Xml;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Configuration;
using System.Text.RegularExpressions;

namespace KellFileTransfer
{
    public class ReceiveListenerArgs
    {
        string dir;

        public string Directory
        {
            get { return dir; }
            set { dir = value; }
        }
        TcpListener listener;

        public TcpListener Listener
        {
            get { return listener; }
            set { listener = value; }
        }
        Thread thr;

        public Thread Thread
        {
            get { return thr; }
            set { thr = value; }
        }

        public ReceiveListenerArgs(string dir, TcpListener listener, Thread thr)
        {
            this.dir = dir;
            this.listener = listener;
            this.thr = thr;
        }
    }

    public class ReceiveFinishArgs : EventArgs
    {
        string filename;

        public string Filename
        {
            get { return filename; }
        }

        string dir;

        public string Directory
        {
            get { return dir; }
        }

        public ReceiveFinishArgs(string dir, string filename)
        {
            this.dir = dir;
            this.filename = filename;
        }
    }
    /// <summary>
    /// 文件上传类
    /// </summary>
    public static class FileUploader
    {
        public static bool SendFile(string sendFile, IPEndPoint hostEP, string dir = null)
        {
            if (!File.Exists(sendFile))
                return false;

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(hostEP);
                FileInfo fi = new FileInfo(sendFile);
                byte[] fileName = new byte[256];
                string filepath = fi.Name;
                if (!string.IsNullOrEmpty(dir))
                    filepath = dir + "\\" + fi.Name;
                byte[] bs = Encoding.Default.GetBytes(filepath);
                for (int i = 0; i < bs.Length; i++)
                {
                    fileName[i] = bs[i];
                }
                socket.SendFile(sendFile, fileName, null, TransmitFileOptions.UseDefaultWorkerThread);
                return true;
            }
            catch (Exception e)
            {
                //return false;
                throw e;
            }
            finally
            {
                socket.Close();
            }
        }

        public static bool SendFile(string sendFile, string host, int port, string dir = null)
        {
            if (!File.Exists(sendFile))
                return false;

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(host, port);
                FileInfo fi = new FileInfo(sendFile);
                byte[] fileName = new byte[256];
                string filepath = fi.Name;
                if (!string.IsNullOrEmpty(dir))
                    filepath = dir + "\\" + fi.Name;
                byte[] bs = Encoding.Default.GetBytes(filepath);
                for (int i = 0; i < bs.Length; i++)
                {
                    fileName[i] = bs[i];
                }
                socket.SendFile(sendFile, fileName, null, TransmitFileOptions.UseDefaultWorkerThread);
                return true;
            }
            catch (Exception e)
            {
                //return false;
                throw e;
            }
            finally
            {
                socket.Close();
            }
        }

        public static ReceiveListenerArgs StartReceiveFile(string receivePath, IPAddress LocalIP, int LocalPort)
        {
            Thread thThreadRead = new Thread(new ParameterizedThreadStart(Listen));
            TcpListener tcpListen = new TcpListener(LocalIP, LocalPort);
            ReceiveListenerArgs rl = new ReceiveListenerArgs(receivePath, tcpListen, thThreadRead);
            thThreadRead.Start(rl);
            return rl;
        }

        public static event EventHandler<ReceiveFinishArgs> ReceiveFinished;

        const int ReadOneTime = 4096;
        static volatile bool stop;
        private static void Listen(object o)
        {
            stop = false;
            ReceiveListenerArgs rl = o as ReceiveListenerArgs;
            if (rl != null)
            {
                string receivePath = rl.Directory;
                if (string.IsNullOrEmpty(receivePath))
                    receivePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                
                TcpListener tcpListen = rl.Listener;
                string filename = "";
                try
                {
                    //以8000端口号来初始化TcpListener实例
                    tcpListen.Start();
                    //循环侦听
                    while (!stop)
                    {
                        //开始监听网络的连接请求
                        if (tcpListen.Pending())
                        {
                            Socket stRead = tcpListen.AcceptSocket();//在停止监听的时候可能会引发“一个封锁操作被对 WSACancelBlockingCall 的调用中断”异常
                            //通过连接请求，并获得接收数据时使用的Socket实例
                            EndPoint tempRemoteEP = stRead.RemoteEndPoint;
                            int iRead;
                            List<byte> buffer = new List<byte>();
                            int cnt = 0;
                            //循环读取数据
                            while (!stop)
                            {
                                //获取接收数据时的时间
                                Byte[] byRead = new Byte[ReadOneTime];
                                iRead = stRead.ReceiveFrom(byRead, ref tempRemoteEP);
                                cnt++;
                                //读取完成后退出循环 
                                if (iRead <= 0)
                                {
                                    try
                                    {
                                        if (filename == "")
                                            filename = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                                        if (!Directory.Exists(receivePath))
                                            Directory.CreateDirectory(receivePath);
                                        string fullpath = receivePath + "\\" + filename;
                                        string fileName = Path.GetFileName(fullpath);
                                        string FilePath = fullpath.Substring(0, fullpath.Length - fileName.Length);
                                        if (FilePath.EndsWith("\\"))
                                            FilePath = FilePath.Substring(0, FilePath.Length-1);
                                        if (!Directory.Exists(FilePath))
                                            Directory.CreateDirectory(FilePath);
                                        using (FileStream fs = new FileStream(fullpath, FileMode.OpenOrCreate, FileAccess.Write))
                                        {
                                            fs.Write(buffer.ToArray(), 0, buffer.Count);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        throw e;
                                    }
                                    finally
                                    {
                                        stRead.Close();
                                        if (ReceiveFinished != null)
                                            ReceiveFinished(null, new ReceiveFinishArgs(receivePath, filename));
                                    }
                                    break;
                                }
                                else
                                {
                                    if (cnt == 1)
                                    {
                                        byte[] fileN = new byte[256];
                                        for (int i = 0; i < 256; i++)
                                        {
                                            fileN[i] = byRead[i];
                                        }
                                        filename = Common.TrimTheNullByte(Encoding.Default.GetString(fileN));
                                        if (!Common.IsValidFileName(filename))//过滤而已攻击（过滤非文件）
                                            break;
                                        if (!Directory.Exists(receivePath))
                                            Directory.CreateDirectory(receivePath);
                                        for (int i = 0; i < ReadOneTime - 256; i++)
                                        {
                                            buffer.Add(byRead[256 + i]);
                                        }
                                    }
                                    else
                                    {
                                        for (int i = 0; i < iRead; i++)
                                            buffer.Add(byRead[i]);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (System.Security.SecurityException ex)
                {
                    throw ex;
                }
            }
        }

        public static bool StopReceiveFile(ReceiveListenerArgs listener)
        {
            stop = true;
            Thread.Sleep(100);
            try
            {
                if (listener != null)
                {
                    if (listener.Listener != null)
                    {
                        listener.Listener.Stop();
                    }
                    if (listener.Thread.ThreadState != System.Threading.ThreadState.Aborted)
                    {
                        listener.Thread.Abort();
                        listener.Thread.Join(1000);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    /// <summary>
    /// 公用类
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// 读取注册表
        /// </summary>
        /// <returns></returns>
        public static string ReadRegistry(string name)
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey softWare = rk.OpenSubKey("Software");
                RegistryKey microsoft = softWare.OpenSubKey("Microsoft");
                RegistryKey windows = microsoft.OpenSubKey("Windows");
                RegistryKey current = windows.OpenSubKey("CurrentVersion");
                RegistryKey run = current.OpenSubKey(@"Run", true);
                return run.GetValue(name, "", RegistryValueOptions.None).ToString();
            }
            catch// (Exception ex)
            {
                return "";
            }
        }

        /// <summary>
        /// 设置开机自启动-写入注册表
        /// </summary>
        /// <param name="exePath">可执行文件的完整路径</param>
        /// <param name="name">注册表项的名字</param>
        /// <returns></returns>
        public static bool SetSelfStarting(string exePath, string name)
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey softWare = rk.OpenSubKey("SOFTWARE");
                RegistryKey microsoft = softWare.OpenSubKey("Microsoft");
                RegistryKey windows = microsoft.OpenSubKey("Windows");
                RegistryKey current = windows.OpenSubKey("CurrentVersion");
                RegistryKey run = current.OpenSubKey(@"Run", true);//这里必须加true就是得到写入权限 
                run.SetValue(name, exePath);
                run.Close();
                return true;
            }
            catch// (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 取消开机自启-删除注册表
        /// </summary>
        /// <param name="name">注册表项的名字</param>
        /// <returns></returns>
        public static bool CancelSelfStarting(string name)
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey softWare = rk.OpenSubKey("Software");
                RegistryKey microsoft = softWare.OpenSubKey("Microsoft");
                RegistryKey windows = microsoft.OpenSubKey("Windows");
                RegistryKey current = windows.OpenSubKey("CurrentVersion");
                RegistryKey run = current.OpenSubKey(@"Run", true);
                run.DeleteValue(name);//删除注册表的值
                run.Close();
                return true;
            }
            catch// (Exception ex)
            {
                return false;
            }
        }

        internal static string TrimTheNullByte(string input)
        {
            string ret = "";
            int i = 0;
            byte[] b = new byte[1];
            while (i < input.Length)
            {
                string s = input.Substring(i, 1);
                if (s != Encoding.Default.GetString(b))
                    ret += s;
                i++;
            }
            return ret;
        }

        public static bool IsValidFileName(string filename)
        {
            if (!string.IsNullOrEmpty(filename) && filename.Length <= 260)
            {
                if (filename.Contains("\r\n"))
                    return false;
                Regex regex = new Regex(@"(?<fpath>([a-zA-Z]:\\){0,1}([\s\.\-\w]+\\)*)(?<fname>[\w]+)(?<namext>(\.[\w]+)*)");
                return regex.IsMatch(filename);
            }
            return false;
        }

        public static bool SaveAppSettingConfig(string key, string value, string configPath = null)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName + ".config";
            if (!string.IsNullOrEmpty(configPath))
                path = configPath;
            if (File.Exists(path))
            {
                try
                {
                    XmlDocument xDoc = new XmlDocument();
                    xDoc.Load(path);
                    XmlNode xNode;
                    XmlElement xElem;
                    xNode = xDoc.SelectSingleNode("//appSettings");
                    xElem = (XmlElement)xNode.SelectSingleNode("//add[@key='" + key + "']");
                    xElem.SetAttribute("value", value);
                    xDoc.Save(path);
                    return true;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            return false;
        }

        public static string GetAppSettingConfig(string key, string configPath = null)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName + ".config";
            if (!string.IsNullOrEmpty(configPath))
                path = configPath;
            if (File.Exists(path))
            {
                try
                {
                    XmlDocument xDoc = new XmlDocument();
                    xDoc.Load(path);
                    XmlNode xNode;
                    XmlElement xElem;
                    xNode = xDoc.SelectSingleNode("//appSettings");
                    if (xNode != null)
                    {
                        xElem = (XmlElement)xNode.SelectSingleNode("//add[@key='" + key + "']");
                        if (xElem != null)
                        {
                            string s = xElem.GetAttribute("value");
                            return s;
                        }
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            return "";
        }

        public static bool IsValidHostOrAddress(string hostNameOrAddress, out bool isHostName)
        {
            isHostName = false;
            if (hostNameOrAddress != null)
            {
                try
                {
                    IPHostEntry host = Dns.GetHostEntry(hostNameOrAddress);
                    isHostName = host.HostName.Equals(hostNameOrAddress, StringComparison.InvariantCultureIgnoreCase);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        public static List<IPAddress> GetIPv4(string hostNameOrAddress)
        {
            List<IPAddress> ips = new List<IPAddress>();
            IPHostEntry host = Dns.GetHostEntry(hostNameOrAddress);
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ips.Add(ip);
                }
            }
            return ips;
        }

        public static List<IPAddress> GetIPv6(string hostNameOrAddress)
        {
            List<IPAddress> ips = new List<IPAddress>();
            IPHostEntry host = Dns.GetHostEntry(hostNameOrAddress);
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    ips.Add(ip);
                }
            }
            return ips;
        }

        public static List<FILELIST> GetFILELIST(string xmlFile)
        {
            List<FILELIST> list = new List<FILELIST>();
            if (File.Exists(xmlFile))
            {
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(xmlFile);
                XmlNodeList xnlist = xmldoc.SelectNodes("//LIST//FileList");
                foreach (XmlNode xn in xnlist)
                {
                    XmlElement xefile = xn as XmlElement;
                    if (xefile != null)
                    {
                        string filepath = xefile.GetAttribute("文件路径");
                        string FileLength = xefile.GetAttribute("文件大小");
                        FILELIST file = new FILELIST();
                        file.文件路径 = filepath;
                        file.文件大小 = long.Parse(FileLength);
                        list.Add(file);
                    }
                }
            }
            return list;
        }

        public static FILELIST GetFILE(string fileFullPath, string dir = null)
        {
            FileInfo fi = new FileInfo(fileFullPath);
            FILELIST file = new FILELIST();
            if (!string.IsNullOrEmpty(dir))
                file.文件路径 = dir + "\\" + fi.Name;
            else
                file.文件路径 = fi.Name;
            file.文件大小 = fi.Length;
            return file;
        }

        public static bool SetFILELIST(string xmlFile, List<KellFileTransfer.FILELIST> list)
        {
            try
            {
                string lastTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                if (File.Exists(xmlFile))
                {
                    XmlDocument xDoc = new XmlDocument();
                    xDoc.Load(xmlFile);
                    XmlElement xNode;
                    XmlNodeList xNodes = xDoc.SelectNodes("//LIST//FileList");
                    if (xNodes == null || xNodes.Count == 0)
                    {
                        xNode = xDoc.SelectSingleNode("//LIST") as XmlElement;
                        if (xNode == null)
                        {
                            xNode = xDoc.CreateElement("LIST");
                            xDoc.AppendChild(xNode);
                        }
                        foreach (KellFileTransfer.FILELIST file in list)
                        {
                            XmlElement xElem = xDoc.CreateElement("FileList");
                            XmlAttribute attr = xDoc.CreateAttribute("文件路径");
                            attr.Value = file.文件路径;
                            xElem.Attributes.Append(attr);
                            XmlAttribute attr2 = xDoc.CreateAttribute("文件大小");
                            attr2.Value = file.文件大小.ToString();
                            xElem.Attributes.Append(attr2);
                            xNode.AppendChild(xElem);
                        }
                    }
                    else
                    {
                        xNode = xDoc.SelectSingleNode("//LIST") as XmlElement;
                        if (xNode != null)
                        {
                            xNode.RemoveAll();
                            foreach (KellFileTransfer.FILELIST file in list)
                            {
                                XmlElement xElem = xDoc.CreateElement("FileList");
                                XmlAttribute attr = xDoc.CreateAttribute("文件路径");
                                attr.Value = file.文件路径;
                                xElem.Attributes.Append(attr);
                                XmlAttribute attr2 = xDoc.CreateAttribute("文件大小");
                                attr2.Value = file.文件大小.ToString();
                                xElem.Attributes.Append(attr2);
                                xNode.AppendChild(xElem);
                            }
                        }
                    }
                    xDoc.Save(xmlFile);
                }
                else
                {
                    XmlDocument xDoc = new XmlDocument();
                    XmlNode node = xDoc.CreateXmlDeclaration("1.0", "utf-8", "");
                    xDoc.AppendChild(node);
                    XmlElement xNode = xDoc.CreateElement("LIST");
                    xDoc.AppendChild(xNode);
                    foreach (KellFileTransfer.FILELIST file in list)
                    {
                        XmlElement xElem = xDoc.CreateElement("FileList");
                        XmlAttribute attr = xDoc.CreateAttribute("文件路径");
                        attr.Value = file.文件路径;
                        xElem.Attributes.Append(attr);
                        XmlAttribute attr2 = xDoc.CreateAttribute("文件大小");
                        attr2.Value = file.文件大小.ToString();
                        xElem.Attributes.Append(attr2);
                        xNode.AppendChild(xElem);
                    }
                    xDoc.Save(xmlFile);
                }
                return true;
            }
            catch// (Exception e)
            {
                return false;
            }
        }

        public static DateTime GetUpgradeTime(string xmlFile)
        {
            DateTime time = DateTime.MinValue;
            if (File.Exists(xmlFile))
            {
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(xmlFile);
                XmlElement xefile = xmldoc.SelectSingleNode("//UPGRADE") as XmlElement;
                if (xefile != null)
                {
                    string lastTime = xefile.GetAttribute("LastTime");
                    DateTime t;
                    if (!string.IsNullOrEmpty(lastTime) && DateTime.TryParse(lastTime, out t))
                        time = t;
                }
            }
            return time;
        }

        public static bool SetUpgradeTime(string xmlFile)
        {
            try
            {
                string lastTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                if (File.Exists(xmlFile))
                {
                    XmlDocument xDoc = new XmlDocument();
                    xDoc.Load(xmlFile);
                    XmlElement xNode = xDoc.SelectSingleNode("//UPGRADE") as XmlElement;
                    if (xNode == null)
                    {
                        xNode = xDoc.CreateElement("UPGRADE");
                        XmlAttribute attr = xDoc.CreateAttribute("LastTime");
                        attr.Value = lastTime;
                        xNode.Attributes.Append(attr);
                        xDoc.AppendChild(xNode);
                    }
                    else
                    {
                        xNode.SetAttribute("LastTime", lastTime);
                    }
                    xDoc.Save(xmlFile);
                }
                else
                {
                    XmlDocument xDoc = new XmlDocument();
                    XmlNode node = xDoc.CreateXmlDeclaration("1.0", "utf-8", "");
                    xDoc.AppendChild(node);
                    XmlElement xNode = xDoc.CreateElement("UPGRADE");
                    XmlAttribute attr = xDoc.CreateAttribute("LastTime");
                    attr.Value = lastTime;
                    xNode.Attributes.Append(attr);
                    xDoc.AppendChild(xNode);
                    xDoc.Save(xmlFile);
                }
                return true;
            }
            catch// (Exception e)
            {
                return false;
            }
        }

        public static bool CheckMainFormIsRun(string exeFilePath, out int procId)
        {
            procId = 0;
            if (File.Exists(exeFilePath))
            {
                string mainFileName = Path.GetFileNameWithoutExtension(exeFilePath);
                Process[] ps = Process.GetProcessesByName(mainFileName);
                if (ps.Length > 0 && ps[0].MainModule.FileName.Equals(exeFilePath, StringComparison.InvariantCultureIgnoreCase))
                {
                    procId = ps[0].Id;
                    return true;
                }
            }
            return false;
        }

        public static bool KillProcess(int porcId)
        {
            Process proc = Process.GetProcessById(porcId);
            if (proc != null)
            {
                try
                {
                    if (proc.CloseMainWindow())
                    {
                        if (proc.HasExited)
                        {
                            return true;
                        }
                        else
                        {
                            proc.Kill();
                        }
                    }
                    //else
                    //{
                    //    proc.Kill();
                    //}
                }
                catch// (Exception e)
                {
                    return false;
                }
                finally
                {
                    proc.Close();
                }
            }
            return true;
        }

        public static string LocalHostName
        {
            get
            {
                return Dns.GetHostName();
            }
        }
        /// <summary>
        /// 获取默认的文件下载终结点(downip,downport=9000)
        /// </summary>
        /// <returns></returns>
        public static IPEndPoint GetDownloadIPEndPoint()
        {
            try
            {
                string ip = Common.GetAppSettingConfig("downip");
                string port = Common.GetAppSettingConfig("downport");
                IPAddress downip = Common.GetIPv4(Common.LocalHostName)[0];
                if (!string.IsNullOrEmpty(ip))
                {
                    List<IPAddress> ips = Common.GetIPv4(ip);
                    if (ips.Count > 0)
                    {
                        downip = ips[0];
                    }
                }
                int Port = 9000;
                int p;
                if (!string.IsNullOrEmpty(port) && int.TryParse(port, out p))
                    Port = p;
                IPEndPoint ipep = new IPEndPoint(downip, Port);
                return ipep;
            }
            catch// (Exception e)
            {
            }
            return null;
        }
        /// <summary>
        /// 获取默认的文件上传终结点(ip,port=8000)
        /// </summary>
        /// <returns></returns>
        public static IPEndPoint GetUploadIPEndPoint()
        {
            try
            {
                string ip = Common.GetAppSettingConfig("ip");
                string port = Common.GetAppSettingConfig("port");
                IPAddress upip = Common.GetIPv4(Common.LocalHostName)[0];
                if (!string.IsNullOrEmpty(ip))
                {
                    List<IPAddress> ips = Common.GetIPv4(ip);
                    if (ips.Count > 0)
                    {
                        upip = ips[0];
                    }
                }
                int Port = 8000;
                int p;
                if (!string.IsNullOrEmpty(port) && int.TryParse(port, out p))
                    Port = p;
                IPEndPoint ipep = new IPEndPoint(upip, Port);
                return ipep;
            }
            catch// (Exception e)
            {
            }
            return null;
        }
        /// <summary>
        /// 一键升级(ipep.port=9000)
        /// </summary>
        /// <param name="exeFileFullPath"></param>
        /// <param name="ipep"></param>
        /// <param name="xmlMaxLength"></param>
        /// <returns></returns>
        public static bool AutoUpgrade(string exeFileFullPath, IPEndPoint ipep, int xmlMaxLength = 4096)
        {
            FileDownloadClient client = new FileDownloadClient(xmlMaxLength);
            client.DownloadingError += new DownloadErrorHandler(client_DownloadingError);
            client.DownloadAllFinished += new EventHandler(client_DownloadAllFinished);
            if (client.HasUpgrade(ipep.Address, ipep.Port))
            {
                int procId;
                if (KellFileTransfer.Common.CheckMainFormIsRun(exeFileFullPath, out procId))
                {
                    if (MessageBox.Show("是否让升级程序强制关闭主程序？", "关闭提醒", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
                    {//强制退出主程序
                        if (!KellFileTransfer.Common.KillProcess(procId))
                        {
                            string mainName = Path.GetFileName(exeFileFullPath);
                            MessageBox.Show("无法关闭主程序，请手动终止[" + mainName + "]进程！");
                        }
                    }
                }
                if (client.DownloadRemoteFileListXml(ipep.Address, ipep.Port))
                {
                    List<KellFileTransfer.FILELIST> list = client.GetLocalFileList();
                    if (list.Count > 0)
                    {
                        if (!client.StartDownloadFilesFromServer(list, ipep.Address, ipep.Port))
                        {
                            MessageBox.Show("无法下载更新的文件！");
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("当前程序已经是最新版本！");
            }
            return false;
        }

        static void client_DownloadingError(FileDownloadClient sender, Exception e)
        {
            MessageBox.Show("升级过程中出错：" + e.Message);
        }

        static void client_DownloadAllFinished(object sender, EventArgs e)
        {
            MessageBox.Show("升级完毕！");
        }
    }

    internal enum CMDD : int
    {
        /// <summary>
        /// 客户端接收到OK的意思是服务器传过来的文件信息，形式如下“OK|文件长度|文件路径|文件名|”
        //  如果是服务端接收到OK的意思是，客户端收到文件时为了响应给服务器让服务器继续发文件的信号
        /// </summary>
        OK = 1,
        /// <summary>
        /// 客户端请求服务器一个文件时就是发这个命令“FILE|路径|文件名|文件大小|修改时间|”
        /// </summary>
        FILE = 2,
        /// <summary>
        /// 下载的文件列表XML文档发这个命令“FILELIST”
        /// </summary>
        FILELIST = 3,
        /// <summary>
        /// 最近的更新XML文档发这个命令“UPGRADE”
        /// </summary>
        UPGRADE = 4
    }

    internal class SocketAndCMDD
    {
        Socket socket;

        public Socket Socket
        {
            get { return socket; }
            set { socket = value; }
        }
        CMDD cmdd;

        public CMDD Cmdd
        {
            get { return cmdd; }
            set { cmdd = value; }
        }

        public SocketAndCMDD(Socket socket, CMDD cmdd)
        {
            this.socket = socket;
            this.cmdd = cmdd;
        }
    }
    /// <summary>
    /// 文件下载客户端类（多用于程序的更新升级和附件的下载客户端程序中）
    /// </summary>
    public class FileDownloadClient
    {
        private Socket connSocket;
        private static ArrayList files = new ArrayList();
        private ArrayList syncFiles = ArrayList.Synchronized(files);
        private static ManualResetEvent receivefileDone = new ManualResetEvent(false);
        //private static ManualResetEvent sendfilecmdDone = new ManualResetEvent(false);
        private static ManualResetEvent connDone = new ManualResetEvent(false);
        private Exception except;
        private string filename;
        public string FileName//文件名
        {
            get { return filename; }
            set { filename = value; }
        }
        private string filepath;//文件路径
        public string FilePath
        {
            get { return filepath; }
            set { filepath = value; }
        }
        private string filelen;//文件长度
        public string FileLength
        {
            get { return filelen; }
            set { filelen = value; }
        }
        private int xmlMaxLength;

        public int XmlMaxLength
        {
            get { return xmlMaxLength; }
            set { xmlMaxLength = value; }
        }

        private string xmlFile = Directory.GetCurrentDirectory() + "\\UPGRADE.XML";
        private string newXmlFile = Directory.GetCurrentDirectory() + "\\UPGRADE_NEW.XML";

        public FileDownloadClient(int xmlMaxLength = 4096)
        {
            this.xmlMaxLength = xmlMaxLength;
        }

        public bool DownloadRemoteFileListXml(IPAddress serverip, int port = 9000)
        {
            try
            {
                return StartClientXml(serverip, port);
            }
            catch// (Exception e)
            {
                return false;
            }
        }

        public bool DownloadRemoteFileListXml(string host, int port = 9000)
        {
            try
            {
                return StartClientXml(host, port);
            }
            catch// (Exception e)
            {
                return false;
            }
        }

        public List<FILELIST> GetLocalFileList()
        {
            List<FILELIST> list = new List<FILELIST>();
            string xmlFile = Directory.GetCurrentDirectory() + "\\FILELIST.XML";
            if (File.Exists(xmlFile))
            {
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(xmlFile);
                XmlNodeList xnlist = xmldoc.SelectNodes("//LIST//FileList");
                foreach (XmlNode xn in xnlist)//xml文档里是要下的文件，这里遍历xml节点
                {
                    XmlElement xefile = xn as XmlElement;
                    if (xefile != null)
                        syncFiles.Add(xefile);//SyncFile数组里存的是要下的每个文件
                }
                for (int k = 0; k < syncFiles.Count; k++)
                {
                    XmlElement xefile = (XmlElement)syncFiles[k];
                    FILELIST file = new FILELIST();
                    string filepath = xefile.GetAttribute("文件路径");
                    FileName = Path.GetFileName(filepath);
                    FilePath = filepath.Substring(0, filepath.Length - FileName.Length);
                    FileLength = xefile.GetAttribute("文件大小");
                    file.文件路径 = filepath;
                    file.文件大小 = long.Parse(FileLength);
                    list.Add(file);
                }
            }
            return list;
        }

        public bool HasUpgrade(IPAddress serverip, int port = 9000)
        {
            //先下载UPGRADE_NEW.XML
            if (!StartClientXml(serverip, port, CMDD.UPGRADE))
            {
                return false;
            }

            //这里一定存在UPGRADE_NEW.XML，而没有UPGRADE.XML文档将无法一键升级主程序(除非分步执行更新的步骤，调用DownloadRemoteFileListXml和StartDownloadFilesFromServer方法，才可以升级主程序！该策略可用于那些不提供持续升级服务的试用版程序当中)
            if (File.Exists(newXmlFile) && File.Exists(xmlFile))//存在UPGRADE_NEW.XML和UPGRADE.XML文档
            {
                //比较UPGRADE.XML和UPGRADE_NEW.XML文档中的LastTime：
                DateTime local = DateTime.MinValue;
                DateTime remote = DateTime.MinValue;

                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(xmlFile);
                XmlElement xefile = xmldoc.SelectSingleNode("//UPGRADE") as XmlElement;
                if (xefile != null)
                {
                    string lastTime = xefile.GetAttribute("LastTime");
                    DateTime t;
                    if (!string.IsNullOrEmpty(lastTime) && DateTime.TryParse(lastTime, out t))
                        local = t;
                }
                XmlDocument xmldoc2 = new XmlDocument();
                xmldoc2.Load(newXmlFile);
                XmlElement xefile2 = xmldoc2.SelectSingleNode("//UPGRADE") as XmlElement;
                if (xefile2 != null)
                {
                    ;
                    string lastTime2 = xefile2.GetAttribute("LastTime");
                    DateTime t2;
                    if (!string.IsNullOrEmpty(lastTime2) && DateTime.TryParse(lastTime2, out t2))
                        remote = t2;
                }
                return remote > local;
            }
            return false;
        }

        public bool HasUpgrade(string host, int port = 9000)
        {
            //先下载UPGRADE_NEW.XML
            if (!StartClientXml(host, port, CMDD.UPGRADE))
            {
                return false;
            }

            //这里一定存在UPGRADE_NEW.XML，而没有UPGRADE.XML文档将无法一键升级主程序(除非分步执行更新的步骤，调用DownloadRemoteFileListXml和StartDownloadFilesFromServer方法，才可以升级主程序！该策略可用于那些不提供持续升级服务的试用版程序当中)
            if (File.Exists(newXmlFile) && File.Exists(xmlFile))//存在UPGRADE_NEW.XML和UPGRADE.XML文档
            {
                //比较UPGRADE.XML和UPGRADE_NEW.XML文档中的LastTime：
                DateTime local = DateTime.MinValue;
                DateTime remote = DateTime.MinValue;

                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(xmlFile);
                XmlElement xefile = xmldoc.SelectSingleNode("//UPGRADE") as XmlElement;
                if (xefile != null)
                {
                    string lastTime = xefile.GetAttribute("LastTime");
                    DateTime t;
                    if (!string.IsNullOrEmpty(lastTime) && DateTime.TryParse(lastTime, out t))
                        local = t;
                }
                XmlDocument xmldoc2 = new XmlDocument();
                xmldoc2.Load(newXmlFile);
                XmlElement xefile2 = xmldoc2.SelectSingleNode("//UPGRADE") as XmlElement;
                if (xefile2 != null)
                {
                    ;
                    string lastTime2 = xefile2.GetAttribute("LastTime");
                    DateTime t2;
                    if (!string.IsNullOrEmpty(lastTime2) && DateTime.TryParse(lastTime2, out t2))
                        remote = t2;
                }
                return remote > local;
            }
            return false;
        }

        private bool StartClientXml(IPAddress serverip, int port = 9000, CMDD cmdd = CMDD.FILELIST)
        {
            Socket connSocketXml = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                IPEndPoint server = new IPEndPoint(serverip, port);
                connSocketXml.Connect(server);
                ClientXmlInfo cs = new ClientXmlInfo(connSocketXml);
                //FILELIST或者UPGRADE
                SendToUpServer(cs.ClientSocket, ((int)cmdd).ToString());
                byte[] tmp = new byte[xmlMaxLength];
                int len = cs.ClientSocket.Receive(tmp);
                cs.Buffer = new byte[len];
                for (int i = 0; i < len; i++)
                    cs.Buffer[i] = tmp[i];
                if (len != 0)
                {
                    string filepath = Directory.GetCurrentDirectory() + "\\FILELIST.XML";

                    string token = Common.TrimTheNullByte(Encoding.Default.GetString(cs.Buffer, 0, 10));

                    if (token == ((int)CMDD.UPGRADE).ToString())
                    {
                        filepath = Directory.GetCurrentDirectory() + "\\UPGRADE_NEW.XML";
                    }
                    using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Write))
                    {
                        fs.Write(cs.Buffer, 10, cs.Buffer.Length - 10);
                        fs.Flush();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                OnDownloadingError(ex);
            }
            finally
            {
                connSocketXml.Close();
            }
            return false;
        }

        private bool StartClientXml(string host, int port = 9000, CMDD cmdd = CMDD.FILELIST)
        {
            Socket connSocketXml = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                connSocketXml.Connect(host, port);
                ClientXmlInfo cs = new ClientXmlInfo(connSocketXml);
                //FILELIST或者UPGRADE
                SendToUpServer(cs.ClientSocket, ((int)cmdd).ToString());
                byte[] tmp = new byte[xmlMaxLength];
                int len = cs.ClientSocket.Receive(tmp);
                cs.Buffer = new byte[len];
                for (int i = 0; i < len; i++)
                    cs.Buffer[i] = tmp[i];
                if (len != 0)
                {
                    string filepath = Directory.GetCurrentDirectory() + "\\FILELIST.XML";

                    string token = Common.TrimTheNullByte(Encoding.Default.GetString(cs.Buffer, 0, 10));

                    if (token == ((int)CMDD.UPGRADE).ToString())
                    {
                        filepath = Directory.GetCurrentDirectory() + "\\UPGRADE_NEW.XML";
                    }
                    using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Write))
                    {
                        fs.Write(cs.Buffer, 10, cs.Buffer.Length - 10);
                        fs.Flush();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                OnDownloadingError(ex);
            }
            finally
            {
                connSocketXml.Close();
            }
            return false;
        }
        /// <summary>
        /// 异步进行批量文件下载
        /// </summary>
        /// <param name="fileList"></param>
        /// <param name="serverip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool StartDownloadFilesFromServer(List<FILELIST> fileList, IPAddress serverip, int port = 9000)
        {
            if (fileList != null)
            {
                try
                {
                    IPEndPoint ipep = new IPEndPoint(serverip, port);
                    DownloadArgs da = new DownloadArgs(fileList, ipep);
                    Thread th = new Thread(new ParameterizedThreadStart(DownloadFilesInList));
                    th.Start(da);
                    return true;
                }
                catch// (Exception e)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 异步进行批量文件下载
        /// </summary>
        /// <param name="fileList"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool StartDownloadFilesFromServer(List<FILELIST> fileList, string host, int port = 9000)
        {
            if (fileList != null)
            {
                try
                {
                    DownloadArgs da = new DownloadArgs(fileList, host, port);
                    Thread th = new Thread(new ParameterizedThreadStart(DownloadFilesInList));
                    th.Start(da);
                    return true;
                }
                catch// (Exception e)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void DownloadFilesInList(object o)
        {
            DownloadArgs da = o as DownloadArgs;
            if (da.EndPoint != null)
            {
                IPEndPoint ipep = da.EndPoint;
                for (int k = 0; k < da.FileList.Count; k++)
                {
                    FILELIST file = da.FileList[k];
                    string filepath = file.文件路径;
                    FileName = Path.GetFileName(filepath);
                    FilePath = filepath.Substring(0, filepath.Length - FileName.Length);
                    FileLength = file.文件大小.ToString();
                    connDone.Reset();
                    StartClient(ipep.Address, ipep.Port);
                    connDone.WaitOne();//保证异步线程的同步
                }
            }
            else if (da.Host != null)
            {
                for (int k = 0; k < da.FileList.Count; k++)
                {
                    FILELIST file = da.FileList[k];
                    string filepath = file.文件路径;
                    FileName = Path.GetFileName(filepath);
                    FilePath = filepath.Substring(0, filepath.Length - FileName.Length);
                    FileLength = file.文件大小.ToString();
                    connDone.Reset();
                    StartClient(da.Host, da.Port);
                    connDone.WaitOne();//保证异步线程的同步
                }
            }
            OnDownloadAllFinished();
        }

        /// <summary>
        /// 同步进行文件下载（注意调用该方法之前要订阅DownloadFinishedSingle事件，才能在事件处理代码中从异步的处理结果里获得文件流）
        /// </summary>
        /// <param name="file"></param>
        /// <param name="serverip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool DownloadFileFromServer(FILELIST file, IPAddress serverip, int port = 9000)
        {
            try
            {
                IPEndPoint ipep = new IPEndPoint(serverip, port);
                string filepath = file.文件路径;
                FileName = Path.GetFileName(filepath);
                FilePath = filepath.Substring(0, filepath.Length - FileName.Length);
                FileLength = file.文件大小.ToString();
                connDone.Reset();
                DownloadFile(ipep.Address, ipep.Port);
                connDone.WaitOne();
                if (except == null)
                    return true;
                else
                    return false;
            }
            catch// (Exception ex)
            {
                return false;
            }
        }

        private void DownloadFile(IPAddress serverip, int port = 9000)
        {
            try
            {
                IPEndPoint server = new IPEndPoint(serverip, port);
                connSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                connSocket.BeginConnect(server, new AsyncCallback(ConnectCallBackSingle), connSocket);
            }
            catch (Exception ex)
            {
                except = ex;
                connDone.Set();
                throw new Exception(ex.ToString());
            }
        }

        private void ConnectCallBackSingle(IAsyncResult ar)
        {
            try
            {
                Socket ClientSocket = (Socket)ar.AsyncState;
                ClientSocket.EndConnect(ar);
                EndPoint ep = ClientSocket.RemoteEndPoint;
                ClientInfo cs = new ClientInfo(ClientSocket, ep);
                //FILE  |  路径  |  文件名  |  文件大小  | 
                SendToUpServer(cs.ClientSocket, ((int)CMDD.FILE).ToString() + "|" + FilePath + "|" + FileName + "|" + FileLength + "|");
                cs.ClientSocket.BeginReceive(cs.Buffer, 0, cs.Buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBackSingle), cs);
            }
            catch (Exception ex)
            {
                except = ex;
                connDone.Set();
                connSocket.Close();
            }
        }

        private void ReceiveCallBackSingle(IAsyncResult ar)
        {
            try
            {
                ClientInfo cs = (ClientInfo)ar.AsyncState;
                int len = cs.ClientSocket.EndReceive(ar);//len是接收到文件的字节数
                if (len != 0)
                {
                    string[] token = System.Text.Encoding.Default.GetString(cs.Buffer, 0, len).Split('|');
                    //如果命令等于OK   接收的命令形式为:   OK       | 文件长度 | 文件路径 | 文件名
                    //对应:   token[0] |token[1]  | token[2] | token[3]
                    if (token[0] == ((int)CMDD.OK).ToString())
                    {
                        OnDownloadBegined(token[3]);
                        string filepath = Directory.GetCurrentDirectory() + "\\" + token[2] + token[3];//这是本程序的路径加上下载文件的路径，也就是文件的接收路径
                        //这是一个包的大小，数据大小 + 命令大小
                        int packlen = 512 * 1024 + token[0].Length + token[1].Length + token[2].Length + token[3].Length + 4;
                        //文件夹名
                        if (token[2].Length > 1)
                        {
                            string gpath = token[2].Substring(0, token[2].Length - 1);
                            //如果有这个文件夹就不新建
                            if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\" + gpath))
                            {
                                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\" + gpath);
                            }
                        }
                        FileStream fs = new FileStream(filepath, FileMode.Append, FileAccess.Write, FileShare.Write);
                        //命令的文本形式
                        string cmd = token[0] + "|" + token[1] + "|" + token[2] + "|" + token[3] + "|";
                        //响应服务器的OK命令
                        string strok = token[0] + "|";
                        byte[] receivebuf = System.Text.Encoding.Default.GetBytes(cmd.ToCharArray());
                        //将纯文件数据复制到2M缓冲再写入硬盘
                        Array.Copy(cs.Buffer, receivebuf.Length, cs.AllBuffer, cs.FileCount, len - receivebuf.Length);
                        cs.FileCount += (len - receivebuf.Length);
                        cs.BagCount = Convert.ToInt32(token[1]) / (512 * 1024);//这个地方应该是 （文件+命令*包数）/（命令+512*1024），但是经过数学公式成此代码
                        //只要得到余数，那么就是这个包有个零头，所以必须再加一个包才能接收完
                        if (Convert.ToInt32(token[1]) % (512 * 1024) != 0)
                        {
                            cs.BagCount += 1;
                        }
                        //如果接收一次没收到一个包的大小，那么就再接收一次，知道接收到一个包的大小
                        while (len < packlen)
                        {
                            if (cs.BagIndex == cs.BagCount || token[1] == cs.FileCount.ToString())
                            {
                                break;
                            }
                            int TcpFile = cs.ClientSocket.Receive(cs.Buffer, 0, cs.Buffer.Length - len, SocketFlags.None);
                            Array.Copy(cs.Buffer, 0, cs.AllBuffer, cs.FileCount, TcpFile);
                            cs.FileCount += TcpFile;//接收的长度
                            len += TcpFile;//便于while做出正确判断
                        }
                        //将2M缓冲写入硬盘当一个文件小与或等于2M时 || 当接收缓冲区allbuf满时 || 当最后一个包发送过来时 || 发送的时候2M的缓冲区只剩下不到512*1024的容量了，这时是不会写入数据的，但是又发来一个数据包，缓冲区溢出
                        if (token[1] == cs.FileCount.ToString() || cs.FileCount == (2048 * 1024) || cs.BagIndex == cs.BagCount || cs.FileCount > 1572864)
                        {
                            receivefileDone.Reset();
                            fs.BeginWrite(cs.AllBuffer, 0, cs.FileCount, new AsyncCallback(WriteFileCallback), fs);
                            receivefileDone.WaitOne();
                            cs.FileCount = 0;
                            fs.Flush();
                            connDone.Set();
                            OnDownloadFinishedSingle(fs.Name);
                        }
                        cs.BagIndex++;
                        //当传过来的文件长度不等于我硬盘文件长度就是没传完，发送OK让服务器继续传
                        if (Convert.ToInt32(token[1]) > fs.Length)
                        {
                            SendToUpServer(cs.ClientSocket, strok);
                        }
                        //如果接收到的长度等于我实际文件的长度，那么就是接收完了这个文件
                        if (fs.Length.ToString() == token[1])
                        {
                            connDone.Set();//这里发送信号给download()方法里的ConnDone.WaitOne(),让他继续运行，这样就
                            //               使程序变成   一个文件下载完后，再set()  for循环继续下载第2个文件
                            cs.ClientSocket.Close();
                            cs.BagIndex = 1;
                            fs.Close();
                            return;
                        }
                        fs.Close();
                    }
                    cs.ClientSocket.BeginReceive(cs.Buffer, 0, cs.Buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBackSingle), cs);
                }
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 10054)
                {
                    connSocket.Close();
                    //MessageBox.Show("与服务器连接中断，请重新连接!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                except = ex;
                OnDownloadingError(ex);
                receivefileDone.Set();
                connDone.Set();
            }
        }

        private void StartClient(IPAddress serverip, int port = 9000)
        {
            try
            {
                IPEndPoint server = new IPEndPoint(serverip, port);
                connSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                connSocket.BeginConnect(server, new AsyncCallback(ConnectCallBack), connSocket);
            }
            catch (Exception ex)
            {
                except = ex;
                throw new Exception(ex.ToString());
            }
        }

        private void StartClient(string host, int port = 9000)
        {
            try
            {
                connSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                connSocket.BeginConnect(host, port, new AsyncCallback(ConnectCallBack), connSocket);
            }
            catch (Exception ex)
            {
                except = ex;
                throw new Exception(ex.ToString());
            }
        }

        private void ConnectCallBack(IAsyncResult ar)
        {
            try
            {
                Socket ClientSocket = (Socket)ar.AsyncState;
                ClientSocket.EndConnect(ar);
                EndPoint ep = ClientSocket.RemoteEndPoint;
                ClientInfo cs = new ClientInfo(ClientSocket, ep);
                //FILE  |  路径  |  文件名  |  文件大小  | 
                SendToUpServer(cs.ClientSocket, ((int)CMDD.FILE).ToString() + "|" + FilePath + "|" + FileName + "|" + FileLength + "|");
                cs.ClientSocket.BeginReceive(cs.Buffer, 0, cs.Buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), cs);
            }
            catch (Exception ex)
            {
                except = ex;
                connSocket.Close();
            }
        }

        private void ReceiveCallBack(IAsyncResult ar)
        {
            try
            {
                ClientInfo cs = (ClientInfo)ar.AsyncState;
                int len = cs.ClientSocket.EndReceive(ar);//len是接收到文件的字节数
                if (len != 0)
                {
                    string[] token = System.Text.Encoding.Default.GetString(cs.Buffer, 0, len).Split('|');
                    //如果命令等于OK   接收的命令形式为:   OK       | 文件长度 | 文件路径 | 文件名
                    //对应:   token[0] |token[1]  | token[2] | token[3]
                    if (token[0] == ((int)CMDD.OK).ToString())
                    {
                        OnDownloadBegined(token[3]);
                        string filepath = Directory.GetCurrentDirectory() + "\\" + token[2] + token[3];//这是本程序的路径加上下载文件的路径，也就是文件的接收路径
                        //这是一个包的大小，数据大小 + 命令大小
                        int packlen = 512 * 1024 + token[0].Length + token[1].Length + token[2].Length + token[3].Length + 4;
                        //文件夹名
                        if (token[2].Length > 1)
                        {
                            string gpath = token[2].Substring(0, token[2].Length - 1);
                            //如果有这个文件夹就不新建
                            if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\" + gpath))
                            {
                                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\" + gpath);
                            }
                        }
                        FileStream fs = new FileStream(filepath, FileMode.Append, FileAccess.Write, FileShare.Write);
                        //命令的文本形式
                        string cmd = token[0] + "|" + token[1] + "|" + token[2] + "|" + token[3] + "|";
                        //响应服务器的OK命令
                        string strok = token[0] + "|";
                        byte[] receivebuf = System.Text.Encoding.Default.GetBytes(cmd.ToCharArray());
                        //将纯文件数据复制到2M缓冲再写入硬盘
                        Array.Copy(cs.Buffer, receivebuf.Length, cs.AllBuffer, cs.FileCount, len - receivebuf.Length);
                        cs.FileCount += (len - receivebuf.Length);
                        cs.BagCount = Convert.ToInt32(token[1]) / (512 * 1024);//这个地方应该是 （文件+命令*包数）/（命令+512*1024），但是经过数学公式成此代码
                        //只要得到余数，那么就是这个包有个零头，所以必须再加一个包才能接收完
                        if (Convert.ToInt32(token[1]) % (512 * 1024) != 0)
                        {
                            cs.BagCount += 1;
                        }
                        //如果接收一次没收到一个包的大小，那么就再接收一次，知道接收到一个包的大小
                        while (len < packlen)
                        {
                            if (cs.BagIndex == cs.BagCount || token[1] == cs.FileCount.ToString())
                            {
                                break;
                            }
                            int TcpFile = cs.ClientSocket.Receive(cs.Buffer, 0, cs.Buffer.Length - len, SocketFlags.None);
                            Array.Copy(cs.Buffer, 0, cs.AllBuffer, cs.FileCount, TcpFile);
                            cs.FileCount += TcpFile;//接收的长度
                            len += TcpFile;//便于while做出正确判断
                        }
                        //将2M缓冲写入硬盘当一个文件小与或等于2M时 || 当接收缓冲区allbuf满时 || 当最后一个包发送过来时 || 发送的时候2M的缓冲区只剩下不到512*1024的容量了，这时是不会写入数据的，但是又发来一个数据包，缓冲区溢出
                        if (token[1] == cs.FileCount.ToString() || cs.FileCount == (2048 * 1024) || cs.BagIndex == cs.BagCount || cs.FileCount > 1572864)
                        {
                            receivefileDone.Reset();
                            fs.BeginWrite(cs.AllBuffer, 0, cs.FileCount, new AsyncCallback(WriteFileCallback), fs);
                            receivefileDone.WaitOne();
                            cs.FileCount = 0;
                            fs.Flush();
                        }
                        cs.BagIndex++;
                        //当传过来的文件长度不等于我硬盘文件长度就是没传完，发送OK让服务器继续传
                        if (Convert.ToInt32(token[1]) > fs.Length)
                        {
                            SendToUpServer(cs.ClientSocket, strok);
                        }
                        //如果接收到的长度等于我实际文件的长度，那么就是接收完了这个文件
                        if (fs.Length.ToString() == token[1])
                        {
                            connDone.Set();//这里发送信号给download()方法里的ConnDone.WaitOne(),让他继续运行，这样就
                            //               使程序变成   一个文件下载完后，再set()  for循环继续下载第2个文件
                            cs.ClientSocket.Close();
                            cs.BagIndex = 1;
                            string filename = Path.GetFileName(fs.Name);
                            fs.Close();
                            OnDownloadFinished(filename);
                            return;
                        }
                        fs.Close();
                    }
                    cs.ClientSocket.BeginReceive(cs.Buffer, 0, cs.Buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), cs);
                }
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 10054)
                {
                    connSocket.Close();
                    //MessageBox.Show("与服务器连接中断，请重新连接!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                except = ex;
                OnDownloadingError(ex);
                receivefileDone.Set();
                connDone.Set();
            }
        }

        public event DownloadHandler DownloadBegined;

        public event DownloadHandler DownloadFinished;

        public event DownloadSingleHandler DownloadFinishedSingle;

        public event EventHandler DownloadAllFinished;

        public event DownloadErrorHandler DownloadingError;

        private void OnDownloadFinishedSingle(string fileFullPath)
        {
            if (DownloadFinishedSingle != null)
                DownloadFinishedSingle(this, fileFullPath);
        }

        private void OnDownloadFinished(string filename)
        {
            if (DownloadFinished != null)
                DownloadFinished(this, filename);
        }

        private void OnDownloadAllFinished()
        {
            File.Copy(newXmlFile, xmlFile, true);//升级完毕，覆盖更新配置文件
            if (DownloadAllFinished != null)
                DownloadAllFinished(this, EventArgs.Empty);
        }

        private void OnDownloadBegined(string filename)
        {
            if (DownloadBegined != null)
                DownloadBegined(this, filename);
        }

        private void OnDownloadingError(Exception e)
        {
            if (DownloadingError != null)
                DownloadingError(this, e);
        }

        private void WriteFileCallback(IAsyncResult ar)
        {
            if (ar.AsyncState is FileStream)
            {
                FileStream fs = ar.AsyncState as FileStream;
                try
                {
                    fs.EndWrite(ar);
                    receivefileDone.Set();
                    //如有客户端本地数据库更新（存在SQL文件），就异步执行以下程序...
                    string ext = Path.GetExtension(fs.Name);
                    if (ext.Equals(".sql", StringComparison.InvariantCultureIgnoreCase))
                    {
                        List<string> sqls = GetSqlText(fs);
                        ThreadPool.QueueUserWorkItem(
                        delegate
                        {
                            SqlConnection conn = null;
                            SqlCommand cmd = null;
                            try
                            {
                                string connStr = ConfigurationManager.ConnectionStrings["connStr"].ConnectionString;
                                if (!string.IsNullOrEmpty(connStr))
                                {
                                    conn = new SqlConnection(connStr);
                                    cmd = conn.CreateCommand();
                                    conn.Open();
                                    foreach (string sql in sqls)
                                    {
                                        if (!string.IsNullOrEmpty(sql))
                                        {
                                            cmd.CommandText = sql;
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show("安装数据库脚本时出错：" + e.Message);
                            }
                            finally
                            {
                                if (cmd != null)
                                    cmd.Dispose();
                                if (conn != null)
                                {
                                    if (conn.State != System.Data.ConnectionState.Closed)
                                        conn.Close();
                                    conn.Dispose();
                                }
                            }
                        });
                    }
                    //异步执行客户端本地数据库脚本结束
                }
                catch (Exception ex)
                {
                    except = ex;
                    OnDownloadingError(ex);
                    receivefileDone.Set();
                }
                finally
                {
                    fs.Close();
                    fs.Dispose();
                }
            }
        }

        private List<string> GetSqlText(FileStream fs)
        {
            List<string> sqls = new List<string>();

            using (StreamReader reader = new StreamReader(fs))
            {
                string sqlText = reader.ReadToEnd();
                List<string> tmps = new List<string>();
                System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(@"^(/s*)GO(/s*)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.ExplicitCapture);
                tmps.AddRange(reg.Split(sqlText));
                foreach (string tmp in tmps)
                {
                    if (tmp != null)
                    {
                        string sql = tmp.Trim();
                        if (sql != "")
                        {
                            sqls.Add(sql);
                        }
                    }
                }
            }

            return sqls;
        }

        private void SendToUpServer(Socket s, string msg)//发消息到服务器
        {
            byte[] sendbuff = Encoding.Default.GetBytes(msg.ToCharArray());
            s.BeginSend(sendbuff, 0, sendbuff.Length, SocketFlags.None, new AsyncCallback(SendUpCallBack), s);
        }

        private void SendUpCallBack(IAsyncResult ar)
        {
            if (ar.AsyncState is Socket)
            {
                try
                {
                    Socket s = ar.AsyncState as Socket;
                    s.EndSend(ar);
                }
                catch { }
            }
        }
    }

    public delegate void DownloadSingleHandler(FileDownloadClient sender, string fileFullPath);

    public delegate void DownloadHandler(FileDownloadClient sender, string filename);

    public delegate void DownloadErrorHandler(FileDownloadClient sender, Exception e);
    /// <summary>
    /// 文件下载服务类（多用于程序的更新升级和附件的下载服务程序中）
    /// </summary>
    public class FileDownloadServer
    {
        private static ManualResetEvent sendfileDone = new ManualResetEvent(false);
        //private static ManualResetEvent readfileDone = new ManualResetEvent(false);
        private Socket serverSocket;
        private bool listening;
        private string downloadDir;

        public string DownloadDir
        {
            get { return downloadDir; }
        }

        public FileDownloadServer(string downloadDir)
        {
            this.downloadDir = downloadDir;
        }

        public bool IsListening
        {
            get { return listening; }
        }

        public bool StartDownloadListen(IPAddress localip, int port = 9000)
        {
            try
            {
                if (BindServerSocket(localip, port))
                {
                    AsyncAccept();
                    return true;
                }
            }
            catch// (Exception e)
            { }
            return false;
        }

        private bool BindServerSocket(IPAddress localip, int port = 9000)
        {
            try
            {
                if (!listening)
                {
                    IPEndPoint localep = new IPEndPoint(localip, port);
                    serverSocket = new Socket(localep.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    serverSocket.Bind(localep);
                    serverSocket.Listen(100);
                    listening = true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            return listening;
        }

        private void AsyncAccept()
        {
            try
            {
                serverSocket.BeginAccept(new AsyncCallback(AcceptCallBack), serverSocket);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        private void AcceptCallBack(IAsyncResult ar)
        {
            serverSocket = (Socket)ar.AsyncState;
            try
            {
                Socket clientSocket = serverSocket.EndAccept(ar);
                ClientServerInfo cl = new ClientServerInfo(clientSocket);
                cl.Buffer = new byte[1024];
                cl.ClientSocket.BeginReceive(cl.Buffer, 0, cl.Buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), cl);
                serverSocket.BeginAccept(new AsyncCallback(AcceptCallBack), serverSocket);
            }
            catch (ObjectDisposedException)
            {
                serverSocket.Close();
            }
        }

        private void SendToClient(Socket sock, string msg)
        {
            byte[] sendbuff = Encoding.Default.GetBytes(msg.ToCharArray());
            sock.BeginSend(sendbuff, 0, sendbuff.Length, SocketFlags.None, new AsyncCallback(SendCallBack), sock);
        }

        private void SendCallBack(IAsyncResult ar)
        {
            if (ar.AsyncState is Socket)
            {
                Socket sock = ar.AsyncState as Socket;
                sock.EndSend(ar);
            }
        }

        private void ReceiveCallBack(IAsyncResult ar)
        {
            ClientServerInfo cl = (ClientServerInfo)ar.AsyncState;
            try
            {
                int len = cl.ClientSocket.EndReceive(ar);
                if (len != 0)
                {
                    string msg = Encoding.Default.GetString(cl.Buffer, 0, len);
                    string[] token = msg.Split(new Char[] { '|' });
                    //如果接受到OK  那么就sendfileDone.Set()给sendfileDone.Wait()发送信号，让线程继续，就是让他继续发送游戏
                    if (token[0] == ((int)CMDD.OK).ToString())
                    {
                        sendfileDone.Set();
                    }
                    //如果接受到FILE   那么是客户端请求下载文件
                    else if (token[0] == ((int)CMDD.FILE).ToString())
                    {
                        string path = DownloadDir + "\\" + token[1] + token[2];//这个DownloadDir是文件服务器的源文件所在处
                        FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                        long filelen = fs.Length;
                        //OK | 文件长度 | 文件路径  | 文件名 | 
                        string ok = ((int)CMDD.OK).ToString() + "|" + filelen.ToString() + "|" + token[1] + "|" + token[2] + "|";
                        byte[] okbuf = System.Text.Encoding.Default.GetBytes(ok.ToCharArray());
                        byte[] buf = new byte[512 * 1024];
                        byte[] allbuf = new byte[okbuf.Length + buf.Length];
                        try
                        {
                            okbuf.CopyTo(allbuf, 0);
                            while (filelen > 0)
                            {
                                int readlen = fs.Read(buf, 0, buf.Length);
                                buf.CopyTo(allbuf, okbuf.Length);
                                sendfileDone.Reset();
                                //每次发送一个包都是这样形式   OK | 文件长度 | 文件路径  | 文件名 | 文件纯数据
                                cl.ClientSocket.BeginSend(allbuf, 0, readlen + okbuf.Length, SocketFlags.None, new AsyncCallback(SendFileCallback), cl);
                                filelen -= readlen;
                                sendfileDone.WaitOne();
                            }
                            fs.Close();
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.ToString());
                        }
                    }
                    else if (token[0] == ((int)CMDD.FILELIST).ToString())
                    {
                        string path = DownloadDir + "\\FILELIST.XML";//这个DownloadDir是文件服务器的源文件所在处
                        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                        {
                            long filelen = fs.Length;
                            //FILELIST
                            string filelist = ((int)CMDD.FILELIST).ToString();
                            byte[] cmdbuf = System.Text.Encoding.Default.GetBytes(filelist.ToCharArray());
                            byte[] allbuf = new byte[10 + filelen];
                            try
                            {
                                cmdbuf.CopyTo(allbuf, 0);
                                if (filelen > 0)
                                {
                                    byte[] buf = new byte[filelen];
                                    int readlen = fs.Read(buf, 0, (int)filelen);
                                    buf.CopyTo(allbuf, 10);
                                    //每次发送一个包都是这样形式：FILELIST|文件纯数据
                                    cl.ClientSocket.Send(allbuf, allbuf.Length, SocketFlags.None);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception(ex.ToString());
                            }
                        }
                    }
                    else if (token[0] == ((int)CMDD.UPGRADE).ToString())
                    {
                        string path = DownloadDir + "\\UPGRADE.XML";//这个DownloadDir是文件服务器的源文件所在处
                        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                        {
                            long filelen = fs.Length;
                            //UPGRADE
                            string upgrade = ((int)CMDD.UPGRADE).ToString();
                            byte[] cmdbuf = System.Text.Encoding.Default.GetBytes(upgrade.ToCharArray());
                            byte[] allbuf = new byte[10 + filelen];
                            try
                            {
                                cmdbuf.CopyTo(allbuf, 0);
                                if (filelen > 0)
                                {
                                    byte[] buf = new byte[filelen];
                                    int readlen = fs.Read(buf, 0, (int)filelen);
                                    buf.CopyTo(allbuf, 10);
                                    //每次发送一个包都是这样形式：UPGRADE|文件纯数据
                                    cl.ClientSocket.Send(allbuf, allbuf.Length, SocketFlags.None);
                                }
                                fs.Close();
                            }
                            catch (Exception ex)
                            {
                                throw new Exception(ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == 10054)
                {
                    CloseSocket(cl);//如果跟哪个失去连接，那么断开此SOCKET
                }
            }
            catch (Exception)
            {
                sendfileDone.Set();
            }
        }

        private void SendFileCallback(IAsyncResult ar)
        {
            try
            {
                ClientServerInfo cf = (ClientServerInfo)ar.AsyncState;
                cf.ClientSocket.EndSend(ar);
                cf.ClientSocket.BeginReceive(cf.Buffer, 0, cf.Buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), cf);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        private void CloseSocket(ClientServerInfo c)
        {
            c.ClientSocket.Close();
        }
    }

    internal class ClientServerInfo
    {
        private Socket clientSocket;
        private byte[] buffer;
        public byte[] Buffer
        {
            get { return buffer; }
            set { buffer = value; }
        }
        public Socket ClientSocket
        {
            get { return clientSocket; }
            set { clientSocket = value; }
        }
        private string path;
        public string Path
        {
            get { return path; }
            set { path = value; }
        }
        private string ok;
        public string OK
        {
            get { return ok; }
            set { ok = value; }
        }
        public ClientServerInfo(Socket sock)
        {
            ClientSocket = sock;
        }
    }

    internal class ClientInfo
    {
        public ClientInfo(Socket sock, EndPoint ip)
        {
            ClientSocket = sock;
            ClientIP = ip;
            FileCount = 0;
            BagIndex = 1;
            Buffer = new byte[513 * 1024];
            AllBuffer = new byte[2048 * 1024];
        }
        private Socket _ClientSocket;
        private EndPoint _ClientIP;
        /// <summary>
        /// 接收文件的缓冲区
        /// </summary>
        private byte[] buffer;
        public byte[] Buffer
        {
            get { return buffer; }
            set { buffer = value; }
        }
        public Socket ClientSocket
        {
            get { return _ClientSocket; }
            set { _ClientSocket = value; }
        }
        public EndPoint ClientIP
        {
            get { return _ClientIP; }
            set { _ClientIP = value; }
        }
        /// <summary>
        /// 已经下到了多少文件来了
        /// </summary>
        private int filecount;
        public int FileCount
        {
            get { return filecount; }
            set { filecount = value; }
        }
        private int bagIndex;
        /// <summary>
        /// 正在下的是第几个包，从1开始
        /// </summary>
        public int BagIndex
        {
            get { return bagIndex; }
            set { bagIndex = value; }
        }
        /// <summary>
        /// 2M的缓冲区
        /// </summary>
        private byte[] allbuffer;
        public byte[] AllBuffer
        {
            get { return allbuffer; }
            set { allbuffer = value; }
        }
        /// <summary>
        /// 一个文件的包数，就是比如要下一个文件，我就重传过来的命令分析这个文件要分正几个 512*1024 字节的包
        /// </summary>
        private int bagCount;
        public int BagCount
        {
            get { return bagCount; }
            set { bagCount = value; }
        }
    }

    internal class ClientXmlInfo
    {
        private Socket clientSocket;
        private byte[] buffer;
        public byte[] Buffer
        {
            get { return buffer; }
            set { buffer = value; }
        }
        public Socket ClientSocket
        {
            get { return clientSocket; }
            set { clientSocket = value; }
        }
        public ClientXmlInfo(Socket sock)
        {
            ClientSocket = sock;
        }
    }

    public class FILELIST
    {
        string filepath = "";

        public string 文件路径
        {
            get { return filepath; }
            set { filepath = value; }
        }
        long filesize = 0;

        public long 文件大小
        {
            get { return filesize; }
            set { filesize = value; }
        }

        public override string ToString()
        {
            return "文件路径:" + 文件路径 + " | 文件大小:" + 文件大小;
        }
    }

    public class DownloadArgs
    {
        List<FILELIST> fileList;

        public List<FILELIST> FileList
        {
            get { return fileList; }
            set { fileList = value; }
        }
        IPEndPoint ipep;

        string host;

        public string Host
        {
            get { return host; }
            set { host = value; }
        }
        int port;

        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        public IPEndPoint EndPoint
        {
            get { return ipep; }
            set { ipep = value; }
        }

        public DownloadArgs(List<FILELIST> fileList, IPEndPoint ipep)
        {
            if (fileList == null || fileList.Count == 0)
                throw new ArgumentException("参数为空", "fileList");

            this.fileList = fileList;
            this.ipep = ipep;
        }

        public DownloadArgs(List<FILELIST> fileList, string host, int port)
        {
            if (fileList == null || fileList.Count == 0)
                throw new ArgumentException("参数为空", "fileList");
            if (host == null)
                throw new ArgumentException("参数为空", "host");

            this.fileList = fileList;
            this.host = host;
            this.port = port;
        }
    }
}
