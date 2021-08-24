using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Suction_PSW_QR
{
    public class ProductConfig
    {
        //public string ConfigPath = Environment.CurrentDirectory + @"\Product.config";

        public class Model
        {
            public double Volt { get; set; } = 20.0;
            public double Ampe { get; set; } = 10.0;
            public double OverVolt { get; set; } = 40.0;
            public double OverAmpe { get; set; } = 20.0;
            public double OutputOnDelay { get; set; } = 0.5;
            public double OutputOffDelay { get; set; } = 0.5;
            public bool HardwareTest { get; set; } = false;

            public bool HardwareTestPass = false;
        }

        public Model VS9000 { get; set; } = new Model();
        public Model VS9500 { get; set; } = new Model();

        public void saveConfig( string ConfigPath)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string str = JsonSerializer.Serialize(this, options);
            //if (!Directory.Exists(ConfigPath)) Directory.CreateDirectory(ConfigPath);
            File.WriteAllText(ConfigPath, str);
        }
    }
}
