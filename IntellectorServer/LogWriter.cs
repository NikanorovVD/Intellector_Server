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
        public static void WriteLine(string mes)
        {
            try
            {
                Console.WriteLine(mes);
                using (StreamWriter writer = new StreamWriter(LogFilePath, true))
                {
                    writer.WriteLine(mes);
                }
            }
            catch { }
        }
    }
}
