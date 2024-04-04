using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntellectorServer
{
    class LogWriter
    {
        private static string LogFilePath = "log.txt";
        private static Queue<string> LogQueue = new Queue<string>();
        Thread LogManager;

        public void Start()
        {
            LogManager = new Thread(WriteLogs);
            LogManager.Start();
        }

        public void Write(string mes) => LogQueue.Enqueue(mes);

        public static void StaticWrite(string mes) => LogQueue.Enqueue(mes);

        public void WriteLogs()
        {
            try
            {
                while (true)
                {
                    if (LogQueue.Count != 0)
                    {
                        WriteInFile(LogQueue.Dequeue());
                    }
                }
            }
            catch (Exception) { }
        }
        private void WriteInFile(string messeage)
        {
            Console.WriteLine(messeage);
            using (StreamWriter writer = new StreamWriter(LogFilePath, true))
            {
                writer.WriteLine(messeage);
            }
        }

        public static void WriteImmideate(string mes)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(LogFilePath, true))
                {
                    writer.WriteLine(mes);
                }
            }
            catch { }
        }
    }
}
