using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Diagnostics;

namespace FileReceiveService
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
			{ 
				new Service1() 
			};
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            ServiceBase.Run(ServicesToRun);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string str = string.Format("应用程序错误，请及时联系系统管理员:{0},应用程序状态：{1}", e.ExceptionObject.ToString(), (e.IsTerminating ? "终止" : "未终止"));

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
        }
    }
}
