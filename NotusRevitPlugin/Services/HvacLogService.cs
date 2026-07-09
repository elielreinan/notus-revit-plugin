using System;
using System.Diagnostics;
using System.IO;

namespace NotusRevitPlugin.Services
{
    public static class HvacLogService
    {
        public static string GetLogFolder()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Notus", "Logs");
            Directory.CreateDirectory(folder);
            return folder;
        }

        public static string GetLogPath()
        {
            return Path.Combine(GetLogFolder(), "notus.log");
        }

        public static void Info(string message)
        {
            Write("INFO", message, null);
        }

        public static void Error(string message, Exception ex)
        {
            Write("ERRO", message, ex);
        }

        private static void Write(string level, string message, Exception ex)
        {
            try
            {
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [" + level + "] " + message;
                if (ex != null) line += " | " + ex.GetType().Name + ": " + ex.Message + "\r\n" + ex.StackTrace;
                File.AppendAllText(GetLogPath(), line + "\r\n");
            }
            catch { }
        }

        public static void OpenLogFolder()
        {
            Directory.CreateDirectory(GetLogFolder());
            Process.Start(new ProcessStartInfo(GetLogFolder()) { UseShellExecute = true });
        }
    }
}


