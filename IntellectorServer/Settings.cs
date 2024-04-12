using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IntellectorServer
{
    public class Settings
    {
        private static string settings_file_path = @"../../../appsettings.json";
        public  static Settings Instance { get; private set; }
        public string Password {  get; set; }
        public int Version { get; set; }

        static Settings()
        {
            Instance = Load();
        }

        private static Settings Load()
        {
            return JsonSerializer.Deserialize<Settings>(File.ReadAllText(settings_file_path));
        }
    }
}
