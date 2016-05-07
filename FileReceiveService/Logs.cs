using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FileReceiveService
{
    public static class Logs
    {
        private static string logPath = AppDomain.CurrentDomain.BaseDirectory + "Logs";
        public static void Create(string msg, string errPoint = null)
        {
            if (!string.IsNullOrEmpty(errPoint))
                msg += Environment.NewLine + "【出错位置】：" + errPoint;
            DateTime now = DateTime.Now;
            try
            {
                if (!Directory.Exists(logPath))
                    Directory.CreateDirectory(logPath);
                string path = logPath + "\\" + now.ToString("yyyy-MM-dd") + ".log";
                File.AppendAllText(path, "[" + now.TimeOfDay.Hours.ToString().PadLeft(2, '0') + ":" + now.TimeOfDay.Minutes.ToString().PadLeft(2, '0') + ":" + now.TimeOfDay.Seconds.ToString().PadLeft(2, '0') + "]" + msg + Environment.NewLine + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                if (!Directory.Exists("d:\\Logs"))
                    Directory.CreateDirectory("d:\\Logs");
                string path = "d:\\Logs\\" + now.ToString("yyyy-MM-dd") + ".log";
                File.AppendAllText(path, "[" + now.TimeOfDay.Hours.ToString().PadLeft(2, '0') + ":" + now.TimeOfDay.Minutes.ToString().PadLeft(2, '0') + ":" + now.TimeOfDay.Seconds.ToString().PadLeft(2, '0') + "]" + msg + Environment.NewLine + Environment.NewLine, Encoding.UTF8);
            }
        }
    }
}
