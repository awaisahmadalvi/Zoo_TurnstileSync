using System;
using System.IO;
using ZooTurnstileSync;

namespace zooTurnstileSync
{
    class Logs
    {
        private static readonly object locker = new object();

        MainUI ui;

        public Logs(MainUI UI)
        {
            this.ui = UI;
        }

        private string getPath()
        {
            // Log file named after date
            return String.Format("{0}_{1:yyyy-MM-dd}.txt", "log", DateTime.Now);
        }

        public void LogText(string logMessage)
        {
            lock (locker)
            {
                DateTime time = new DateTime();
                time = DateTime.Now;

                logMessage = time.ToString("hh:mm:ss tt") + "    " + logMessage + "\r\n";
                if (ui.isLoggingUI)
                {
                    ui.WriteTextSafe(logMessage);
                }
                if (ui.isLoggingFile)
                {
                    using (var str = new StreamWriter(getPath(), append: true))
                    {
                        str.WriteLine(logMessage);
                        str.Flush();
                        str.Close();
                    }
                }
            }
        }
    }
}
