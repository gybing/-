using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace 接收文件
{
    static class Program
    {
        [DllImport("user32.dll")]
        static extern bool FlashWindow(IntPtr hWnd, bool bInvert);
        /// <summary>
        /// 该函数设置由不同线程产生的窗口的显示状态
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="cmdShow">指定窗口如何显示。查看允许值列表，请查阅ShowWlndow函数的说明部分</param>
        /// <returns>如果函数原来可见，返回值为非零；如果函数原来被隐藏，返回值为零</returns>
        [DllImport("User32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);
        /// <summary>
        ///  该函数将创建指定窗口的线程设置到前台，并且激活该窗口。键盘输入转向该窗口，并为用户改各种可视的记号。
        ///  系统给创建前台窗口的线程分配的权限稍高于其他线程。 
        /// </summary>
        /// <param name="hWnd">将被激活并被调入前台的窗口句柄</param>
        /// <returns>如果窗口设入了前台，返回值为非零；如果窗口未被设入前台，返回值为零</returns>
        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int SW_SHOWNOMAL = 1;

        private static void HandleRunningInstance(Process instance)
        {
            ShowWindowAsync(instance.MainWindowHandle, SW_SHOWNOMAL);//显示
            SetForegroundWindow(instance.MainWindowHandle);//推到最前端
        }

        private static Process RuningInstance()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] Processes = Process.GetProcessesByName(currentProcess.ProcessName);
            foreach (Process process in Processes)
            {
                if (process.Id != currentProcess.Id)
                {//允许同一个应用程序在多个地方允许（但是，记得如果是服务程序，要配置不一样的监听端口！）
                    if (process.MainModule.FileName.Equals(currentProcess.MainModule.FileName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return process;
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Process process = RuningInstance();
            if (process == null)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
                Application.Run(new Receiver2());
            }
            else
            {
                MessageBox.Show("该应用程序已经在运行中，请留意任务栏或托盘区！");
                HandleRunningInstance(process);
                FlashWindow(process.MainWindowHandle, true);
                //System.Threading.Thread.Sleep(1000);
                //System.Environment.Exit(1);
            }
            //bool createNew;
            //using (System.Threading.Mutex mutex = new System.Threading.Mutex(true, Application.ProductName, out createNew))
            //{
            //    if (createNew)
            //    {
            //        Application.EnableVisualStyles();
            //        Application.SetCompatibleTextRenderingDefault(false);
            //        Application.Run(Form1.GetInstance());
            //    }
            //    else
            //    {
            //        MessageBox.Show("应用程序已经在运行中...");
            //        System.Threading.Thread.Sleep(1000);
            //        System.Environment.Exit(1);
            //    }
            //}
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string str = string.Format("应用程序错误:{0},应用程序状态：{1}", e.ExceptionObject.ToString(), (e.IsTerminating ? "终止" : "未终止"));

            StackTrace st = new StackTrace(true);
            StackFrame sf = st.GetFrame(0);
            string fileName = sf.GetFileName();
            Type type = sf.GetMethod().ReflectedType;
            string assName = type.Assembly.FullName;
            string typeName = type.FullName;
            string methodName = sf.GetMethod().Name;
            int lineNo = sf.GetFileLineNumber();
            int colNo = sf.GetFileColumnNumber();
            Logs.Create(str, fileName + " : " + assName + "." + typeName + "=>" + methodName + "(" + lineNo + "行" + colNo + "列)");
            MessageBox.Show("发生应用程序致命错误，请及时联系系统管理员！", "应用程序错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            string str = "";
            Exception error = e.Exception as Exception;
            if (error != null)
            {
                string strDateInfo = "出现应用程序未处理的线程异常：" + DateTime.Now.ToString() + "/r/n";
                str = string.Format(strDateInfo + "异常类型：{0}/r/n异常消息：{1}/r/n异常信息：{2}/r/n",
                     error.GetType().Name, error.Message, error.StackTrace);
            }
            else
            {
                str = string.Format("应用程序线程错误:{0}", e);
            }

            StackTrace st = new StackTrace(true);
            StackFrame sf = st.GetFrame(0);
            string fileName = sf.GetFileName();
            Type type = sf.GetMethod().ReflectedType;
            string assName = type.Assembly.FullName;
            string typeName = type.FullName;
            string methodName = sf.GetMethod().Name;
            int lineNo = sf.GetFileLineNumber();
            int colNo = sf.GetFileColumnNumber();
            Logs.Create(str, fileName + " : " + assName + "." + typeName + "=>" + methodName + "(" + lineNo + "行" + colNo + "列)");
            MessageBox.Show("发生应用程序线程致命错误，请及时联系系统管理员！", "线程错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}