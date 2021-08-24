using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Suction_PSW_QR
{



    public class PSW
    {

        public SerialPort serialPort = new SerialPort();
        public SerialDisplay Display = new SerialDisplay();

        public int ScanTimeOut = 500;
        private bool IsPSWport = false;
        private string dataReciverBuffer { get; set; } = "";

        public bool IsOutputON = true;
        public string IP { get; set; }
        public bool IPcontrol { get; set; } = true;

        public double Voltage { get; set; } = 0.000;
        public double Ampere { get; set; } = 0.000;

        public bool IsSocketConnected = false;

        private enum Query
        { 
            Measure = 0,
            Other = 1,
            Set = 2,
            WaitQuery = 999,
        }

        //private Query query = Query.WaitQuery;

        public event EventHandler SerialReciver;
        public event EventHandler SerialSend;

        public PSW()
        {

        }

        public bool PortScant()
        {
            var PortList = SerialPort.GetPortNames();
            foreach (var port in PortList)
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
                serialPort.PortName = port;
                serialPort.BaudRate = 9600;
                serialPort.DataBits = 8;
                serialPort.StopBits = StopBits.One;
                serialPort.Parity = Parity.None;

                serialPort.DataReceived -= SerialPort_DataReceived;
                serialPort.DataReceived += SerialPort_DataReceived;

                dataReciverBuffer = "";
                try
                {
                    serialPort.Open();
                }
                catch (Exception err)
                {

                }

                if (serialPort.IsOpen)
                {
                    Display.ShowCOMStatus(serialPort.IsOpen);
                    serialPort.Write("*IDN?\r\n");
                    SerialSend?.Invoke(null, null);
                    Console.WriteLine(serialPort.PortName + ": *IDN?\r\n");
                    var startScanTime = DateTime.Now;
                    while (true)
                    {
                        if (dataReciverBuffer.Contains("GW-INSTEK,PSW80-40.5"))
                        {
                            IsPSWport = true;
                            break;
                        }
                        else
                        {
                            IsPSWport = false;
                        }

                        if (DateTime.Now.Subtract(startScanTime).TotalMilliseconds > ScanTimeOut)
                        {
                            break;
                        }

                    }
                }
                if (IsPSWport)
                {
                    break;
                }
                else
                {
                    serialPort.Close();
                }
            }
            return IsPSWport;
        }

        public void Measure_Volt()
        {
            
            dataReciverBuffer = "";
            if (IPcontrol)
            {
                if(Command.Count < 100) Command.Add("MEASure:VOLTage:DC?\r\n");
                if (Command.Count < 100) Command.Add("MEASure:CURRent:DC?\r\n");
            }
            else
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Write("MEASure:VOLTage:DC?\r\n");
                    SerialSend?.Invoke(null, null);
                    var startScanTime = DateTime.Now;
                    while (true)
                    {
                        if (Double.TryParse(dataReciverBuffer, out _) && dataReciverBuffer != "")
                        {
                            SerialReciver?.Invoke(null, null);
                            Voltage = Convert.ToDouble(dataReciverBuffer.Replace("\n", "").Replace("\r", ""));
                        }

                        if (DateTime.Now.Subtract(startScanTime).TotalMilliseconds > 100)
                        {
                            break;
                        }
                    }
                }
            }
        }

        public void SetParam(ProductConfig.Model model)
        {
            
            dataReciverBuffer = "";
            if (IPcontrol)
            {
                //while (query != Query.WaitQuery) ;
                if (Command.Count < 100) Command.Add("MEASure:VOLTage:DC?\r\n");
                if (Command.Count < 100) Command.Add("MEASure:CURRent:DC?\r\n");
                Command.Insert(1,string.Format("APPL {0},{1}\r\n", model.Volt.ToString("F4"), model.Ampe.ToString("F4")));
                //OUTPut:DELay:ON <NRf>
                Command.Insert(1, string.Format("OUTPut:DELay:ON {0}\r\n", model.OutputOnDelay.ToString("F1")));

                Command.Insert(1, string.Format("OUTPut:DELay:OFF {0}\r\n", model.OutputOffDelay.ToString("F1")));

                Command.Insert(1, string.Format("SOURce:VOLTage:PROTection:LEVel {0}\r\n", model.OverVolt.ToString("F2")));

                Command.Insert(1, string.Format("SOURce:CURRent:PROTection:LEVel {0}\r\n", model.OverAmpe.ToString("F2")));

            }
            else
            if (serialPort.IsOpen)
            {
                dataReciverBuffer = "";
                serialPort.Write(string.Format("APPL {0},{1}\r\n", model.Volt.ToString("F4"), model.Ampe.ToString("F4")));
            }
        }
        public void SetVolt(ProductConfig.Model model, bool LowLevel)
        {

            dataReciverBuffer = "";
            if (IPcontrol)
            {
                //while (query != Query.WaitQuery) ;
                if (Command.Count < 100) Command.Add("MEASure:VOLTage:DC?\r\n");
                if (Command.Count < 100) Command.Add("MEASure:CURRent:DC?\r\n");
                if (LowLevel)
                {
                    Command.Insert(1, string.Format("APPL {0},{1}\r\n", 5, model.Ampe.ToString("F4")));
                }
                else
                {
                    Command.Insert(1, string.Format("APPL {0},{1}\r\n", model.Volt.ToString("F4"), model.Ampe.ToString("F4")));
                }
                

            }
            else
            if (serialPort.IsOpen)
            {
                dataReciverBuffer = "";
                serialPort.Write(string.Format("APPL {0},{1}\r\n", model.Volt.ToString("F4"), model.Ampe.ToString("F4")));
            }
        }
        public bool ON()
        {
            var startScanTime = DateTime.Now;
            dataReciverBuffer = "";
            if (IPcontrol)
            {
                if (Command.Count < 100) Command.Add("MEASure:VOLTage:DC?\r\n");
                if (Command.Count < 100) Command.Add("MEASure:CURRent:DC?\r\n");
                Command.Add("MEASure:VOLTage:DC?\r\n");
                Command.Insert(1,"OUTPut:IMMediate ON\n");
                IsOutputON = true;
            }
            else
            if (serialPort.IsOpen)
            {
                dataReciverBuffer = "";
                serialPort.Write("OUTPut:IMMediate ON\r\n");
                SerialSend?.Invoke(null, null);
                Task.Delay(100);
                serialPort.Write("OUTPut:IMMediate?\r\n");
                SerialSend?.Invoke(null, null);
                while (true)
                {
                    if (dataReciverBuffer.Replace("\n", "").Replace("\r", "") == "0")
                    {
                        SerialReciver?.Invoke(null, null);
                        IsOutputON = false;
                        break;
                    }

                    if (dataReciverBuffer.Replace("\n", "").Replace("\r", "") == "1")
                    {
                        SerialReciver?.Invoke(null, null);
                        IsOutputON = true;
                        break;
                    }

                    if (DateTime.Now.Subtract(startScanTime).TotalMilliseconds > 200)
                    {
                        IsOutputON = false;
                        break;
                    }
                }
            }
            else
            {
                IsOutputON = false;
            }
            return IsOutputON;
        }

        public bool OFF()
        {
            dataReciverBuffer = "";
            if (IPcontrol)
            {
                if (Command.Count < 100) Command.Add("MEASure:VOLTage:DC?\r\n");
                if (Command.Count < 100) Command.Add("MEASure:CURRent:DC?\r\n");
                Command.Add("MEASure:VOLTage:DC?\r\n");
                Command.Insert(1,"OUTPut:IMMediate OFF\r\n");
                IsOutputON = false;
            }
            else
            if (serialPort.IsOpen)
            {
                dataReciverBuffer = "";
                serialPort.Write("OUTPut:IMMediate OFF\r\n");
                Task.Delay(100);
                serialPort.Write("OUTPut:IMMediate?\r\n");
                var startScanTime = DateTime.Now;
                while (true)
                {
                    if (dataReciverBuffer.Replace("\n", "").Replace("\r", "") == "0")
                    {
                        SerialReciver?.Invoke(null, null);
                        IsOutputON = false;
                        break;
                    }

                    if (dataReciverBuffer.Replace("\n", "").Replace("\r", "") == "1")
                    {
                        SerialReciver?.Invoke(null, null);
                        IsOutputON = true;
                        break;
                    }

                    if (DateTime.Now.Subtract(startScanTime).TotalMilliseconds > 500)
                    {
                        IsOutputON = false;
                        break;
                    }
                }
            }
            else
            {
                IsOutputON = false;
            }
            return IsOutputON;
        }


            private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            dataReciverBuffer = serialPort.ReadExisting();
            SerialReciver?.Invoke(null, null);
        }

        public void saveConfig(string Path)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string str = JsonSerializer.Serialize(this, options);
            //if (!Directory.Exists(ConfigPath)) Directory.CreateDirectory(ConfigPath);
            File.WriteAllText(Path, str);
        }

        private List<string> Command = new List<string>();
        public void PSW_Client_Comunicate()
        {
            try
            {
                Console.WriteLine("Try connect to GWINTEK POWER SUPPLY. {0}", IP);
                TcpClient client = new TcpClient()
                {
                    ReceiveTimeout = 500
                };
                // 1. connect
                IAsyncResult resultConnect = client.BeginConnect(IP, 2268, null, null);
                bool success = resultConnect.AsyncWaitHandle.WaitOne(5000, true);
                //client.Connect(PSW_SocketIP, 2268);
                if (success)
                {
                    Stream stream = client.GetStream();
                    Console.WriteLine("Connected to GWINTEK POWER SUPPLY.");
                    IsSocketConnected = true;
                    while (true)
                    {
                        if (!IPcontrol)
                        {
                            stream.Close();
                            client.Close();
                            break;
                        }
                        var reader = new StreamReader(stream);
                        var writer = new StreamWriter(stream);
                        writer.AutoFlush = true;
                        string cmd = "";
                        if (Command.Count >= 1)
                        {
                            if (Command[0] != null)
                            {
                                cmd = Command[0];
                                cmd = cmd.Replace("\r\n", "\n");
                                writer.WriteLine(cmd);
                                Console.WriteLine(cmd);
                                //SerialSend?.Invoke(null, null);
                            }
                        }
                        // 2. send
                        Thread.Sleep(100);
                        // 3. receive
                        try
                        {
                            string str = reader.ReadLine();
                            if (str != null)
                            {
                                dataReciverBuffer = str;
                                Console.WriteLine(dataReciverBuffer);
                                //SerialReciver?.Invoke(null, null);
                                if (cmd.Contains("MEASure:VOLTage:DC"))
                                {
                                    Voltage = Convert.ToDouble(dataReciverBuffer.Replace("\n", "").Replace("\r", ""));
                                }
                                else if (cmd.Contains("MEASure:CURRent:DC"))
                                {
                                    Ampere = Convert.ToDouble(dataReciverBuffer.Replace("\n", "").Replace("\r", ""));
                                }
                            }
                        }
                        catch (Exception)
                        {
                            
                        }
                        if (Command.Count >= 2) Command.RemoveAt(0);
                    }
                    // 4. close
                    stream.Close();
                    client.Close();
                }
                else
                {
                    Console.WriteLine("Connect time out.");
                    IsSocketConnected = false;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("Lost connect to GWINTEK POWER SUPPLY. {0}", err.Message);
                IsSocketConnected = false;
            }
        }
    }
}
