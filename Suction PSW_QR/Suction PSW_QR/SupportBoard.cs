using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Suction_PSW_QR
{
    public class SupportBoard
    {
        public SerialPort serialPort = new SerialPort();
        public SerialDisplay Display = new SerialDisplay();

        public int ScanTimeOut = 1000;
        private bool IsBoardPort = false;
        private string dataReciverBuffer { get; set; } = "";

        public event EventHandler SerialReciver;
        public event EventHandler SerialSend;

        public SupportBoard()
        {

        }

        public bool PortScant()
        {
            var PortList = SerialPort.GetPortNames();
            foreach (var port in PortList)
            {
                if (serialPort.IsOpen) serialPort.Close();

                serialPort = new SerialPort()
                {
                    PortName = port,
                    BaudRate = 9600,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Parity = Parity.None,
                };
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
                    serialPort.Write("*Arduino?\r\n");
                    SerialSend?.Invoke(null, null);
                    Console.WriteLine(serialPort.PortName + ": *is support?\r\n");
                    var startScanTime = DateTime.Now;
                    while (true)
                    {
                        if (dataReciverBuffer.Contains("Arduino"))
                        {
                            IsBoardPort = true;
                            break;
                        }
                        else
                        {
                            IsBoardPort = false;
                        }

                        if (DateTime.Now.Subtract(startScanTime).TotalMilliseconds > ScanTimeOut)
                        {
                            break;
                        }
                    }
                }
                if (IsBoardPort)
                {
                    break;
                }
                else
                {
                    serialPort.Close();
                }
            }
            return IsBoardPort;
        }

        public void Send_Data(string dataToSend)
        {
            if (serialPort.IsOpen)
            {
                Display.ShowCOMStatus(serialPort.IsOpen);
                serialPort.Write(dataToSend);
                SerialSend?.Invoke(null, null);
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                dataReciverBuffer = serialPort.ReadLine();
                SerialReciver?.Invoke(dataReciverBuffer, null);
                Console.WriteLine(dataReciverBuffer);
            }
            catch (Exception)
            {
            }
        }
    }
}
