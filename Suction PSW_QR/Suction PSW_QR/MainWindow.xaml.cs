using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Suction_PSW_QR
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string QRconfigPath = Environment.CurrentDirectory + @"\QRformat.config";
        public string ProductConfigPath = Environment.CurrentDirectory + @"\Product.config";
        public string PSWConfigPath = Environment.CurrentDirectory + @"\PSW.config";
        public string PrinterConfigPath = Environment.CurrentDirectory + @"\Printer.Config";
        public string FolderConfigPath = Environment.CurrentDirectory + @"\path.txt";
        public string HistoryPath = Environment.CurrentDirectory + @"\History\";
        public string CNS_Path = @"D:\AutoQR\CNS";


        enum TestProgress
        {
            Ready = 0,
            Start = 1,
            SuctionTest = 2,
            HardwareTest = 3,
            EndTest = 4,
            Wait = 5,
        }
        TestProgress GetTestProgress = TestProgress.Ready;


        QR_Code QR = new QR_Code();

        GT800_Printer GT800_Printer = new GT800_Printer()
        {
            IsUseSerial = true,
        };

        PSW PSW = new PSW();
        System.Timers.Timer updateTimer = new System.Timers.Timer()
        {
            Interval = 200
        };

        System.Windows.Forms.Timer checkFileTimer = new System.Windows.Forms.Timer()
        {
            Interval = 500
        };


        SupportBoard SupportBoard = new SupportBoard();

        List<DataTestted> DataTestteds { get; set; } = new List<DataTestted>();

        ProductConfig product = new ProductConfig();

        public enum ModelUser
        {
            VS9500 = 1,
            VS9000 = 2,
        }
        public ModelUser modelUser { get; set; } = ModelUser.VS9000;

        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.richTextBox = ProgramLog;
            System.Windows.Forms.Timer InitDelayTimer = new System.Windows.Forms.Timer()
            {
                Interval = 200
            };
            InitDelayTimer.Tick += InitDelayTimer_Tick;
            InitDelayTimer.Start();
        }

        private void InitDelayTimer_Tick(object sender, EventArgs e)
        {
            (sender as System.Windows.Forms.Timer).Stop();

            Debug.Write("Check file and folder ........ ", Debug.ContentType.Notify);
            FolderCheck();
            Debug.Write("Load config........ ", Debug.ContentType.Notify);
            Config_check();
            Debug.Write("Load history data ........ ", Debug.ContentType.Notify);
            LoadDataTest(DataTest);
            CommunicationUiInit();
            Debug.Write("Scaning Port ........ ", Debug.ContentType.Notify);
            CommunicationPortScan();

            modelUser = ModelUser.VS9500;
            btModelToggle.IsChecked = true;
            btModelToggle.IsChecked = false;

            updateTimer.Elapsed += UpdateTimer_Elapsed;
            updateTimer.Start();

            checkFileTimer.Tick += CheckFileTimer_Tick;
            checkFileTimer.Start();
        }

        private void CheckFileTimer_Tick(object sender, EventArgs e)
        {
            ProgressTest();
            checkFileTimer.Start();
        }

        public void CommunicationUiInit()
        {
            PSW.Display = new SerialDisplay()
            {
                IsOpenRect = CONNECTED_RECT_COM2,
                TX = TX_RECT_COM2,
                RX = RX_RECT_COM2,
            };


            SupportBoard.Display = new SerialDisplay()
            {
                IsOpenRect = CONNECTED_RECT_COM1,
                TX = TX_RECT_COM1,
                RX = RX_RECT_COM1,
            };

            GT800_Printer.Display = new SerialDisplay()
            {
                IsOpenRect = CONNECTED_RECT_COM3,
                TX = TX_RECT_COM3,
                RX = RX_RECT_COM3,
            };

        }

        private void GT800_Printer_SerialSend(object sender, EventArgs e)
        {
            GT800_Printer.Display.BlinkTX();
        }

        private void SupportBoard_SerialReciver(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                SupportBoard.Display.BlinkRX();
                if ((sender as string).Contains(":POWER_ON"))
                {
                    PSW.ON();
                    testBrushResult.Text = "ON";
                }
                if ((sender as string).Contains(":POWER_OFF"))
                {
                    PSW.OFF();
                    testBrushResult.Text = "OFF";
                }
            });

            if (GetTestProgress == TestProgress.HardwareTest)
            {
                switch (modelUser)
                {
                    case ModelUser.VS9500:
                        if ((sender as string).Contains("TEST:RESULT:PASS"))
                        {
                            product.VS9500.HardwareTestPass = true;
                        }
                        if ((sender as string).Contains("TEST:RESULT:FAIL"))
                        {
                            product.VS9500.HardwareTestPass = false;
                        }
                        break;
                    case ModelUser.VS9000:
                        break;
                    default:
                        break;
                }
            }

        }

        private void SupportBoard_SerialSend(object sender, EventArgs e)
        {
            SupportBoard.Display.BlinkTX();
        }

        private void PSW_SerialSend(object sender, EventArgs e)
        {
            PSW.Display.BlinkTX();
        }

        private void PSW_SerialReciver(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                PSW.Display.BlinkRX();
            });
        }

        public void CommunicationPortScan()
        {
            PSW.SerialReciver -= PSW_SerialReciver;
            PSW.SerialSend -= PSW_SerialSend;
            SupportBoard.SerialReciver -= SupportBoard_SerialReciver;
            SupportBoard.SerialSend -= SupportBoard_SerialSend;
            GT800_Printer.SerialSend -= GT800_Printer_SerialSend;

            if (PSW.IPcontrol)
            {
                PSW.Display.ShowCOMStatus(true);
                Debug.Write("PSW IP:" + PSW.IP, Debug.ContentType.Notify);
            }
            else if (PSW.PortScant())
            {
                PSW.Display.ShowCOMStatus(PSW.serialPort.IsOpen);
                Debug.Write("PSW COM:" + PSW.serialPort.PortName, Debug.ContentType.Notify);
            }
            else
            {
                PSW.Display.ShowCOMStatus(PSW.serialPort.IsOpen);
                Debug.Write("PSW COM: Can't find", Debug.ContentType.Error);
            }

            if (SupportBoard.PortScant())
            {
                SupportBoard.Display.ShowCOMStatus(SupportBoard.serialPort.IsOpen);
                Debug.Write("Battery JIG:" + SupportBoard.serialPort.PortName, Debug.ContentType.Notify);
            }
            else
            {
                SupportBoard.Display.ShowCOMStatus(SupportBoard.serialPort.IsOpen);
                Debug.Write("Battery JIG: Can't find", Debug.ContentType.Error);
            }

            cbbPrinterCOM.ItemsSource = SerialPort.GetPortNames();
            Debug.Write("Printer comport: " + GT800_Printer.serialPortName, Debug.ContentType.Notify);
            if (GT800_Printer.IsUseSerial)
            {
                for (int i = 0; i < cbbPrinterCOM.Items.Count; i++)
                {
                    if (GT800_Printer.serialPortName == SerialPort.GetPortNames()[i])
                    {
                        cbbPrinterCOM.SelectedIndex = i;
                        break;
                    }
                }
            }

            PSW.SerialReciver += PSW_SerialReciver;
            PSW.SerialSend += PSW_SerialSend;
            SupportBoard.SerialReciver += SupportBoard_SerialReciver;
            SupportBoard.SerialSend += SupportBoard_SerialSend;
            GT800_Printer.SerialSend += GT800_Printer_SerialSend;

        }
        private void cbbPrinterCOM_DropDownClosed(object sender, EventArgs e)
        {
            GT800_Printer.PortChange((sender as ComboBox).SelectedItem.ToString());
        }


        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (PSW.IPcontrol)
            {
                if (!PSW.IsSocketConnected)
                {
                    Thread IPCONTROL_PSW = new Thread(PSW.PSW_Client_Comunicate);
                    IPCONTROL_PSW.Start();
                }
            }
            this.Dispatcher.Invoke(() =>
            {
                PSW.Measure_Volt();
                PSW_Voltage.Text = PSW.Voltage.ToString("F2") + " V";
                PSW_Ampere.Text = PSW.Ampere.ToString("F2") + " A";
                //PSW.Display.BlinkRX();
            });
            updateTimer.Start();
        }




        public void FolderCheck()
        {
            if (!Directory.Exists("History")) Directory.CreateDirectory("History");
            if (File.Exists(FolderConfigPath))
            {
                CNS_Path = File.ReadAllText(FolderConfigPath);
            }
            else
            {
                File.WriteAllText(FolderConfigPath, "");
            }

        }
        // timer update value


        // load data
        bool dataLoaded = false;
        public void LoadDataTest(DataGrid viewer)
        {
            if (!dataLoaded)
            {
                string todayFile = "History/" + DateTime.Now.Year + "/" + DateTime.Now.ToString("MM_dd") + ".txt";
                //string todayFile = Environment.CurrentDirectory + "Text.txt";
                Console.WriteLine(todayFile);
                if (File.Exists(todayFile))
                {
                    var lines = File.ReadAllLines(todayFile);
                    foreach (var line in lines)
                    {
                        var dataItem = line.Split(' ');
                        DataTestteds.Insert(0, new DataTestted()
                        {
                            No = Convert.ToInt32(dataItem[0]),
                            Barcode = dataItem[1],
                            Date = dataItem[2],
                            Time = dataItem[3],
                            Suction = Convert.ToDouble(dataItem[4]),
                            Status = dataItem[5]
                        });
                    }
                }
                viewer.ItemsSource = DataTestteds;
            }
        }






        //file result check
        DateTime lastModified = DateTime.Now;
        DateTime StartTime = DateTime.Now;
        bool TestPass = true;
        FileInfo theLastFile;
        public void ProgressTest()
        {
            switch (GetTestProgress)
            {
                case TestProgress.Ready:

                    if (PSW.Ampere > 0.02)
                    {
                        switch (modelUser)
                        {
                            case ModelUser.VS9500:
                                SupportBoard.Send_Data("*MODEL:VS9500\r\n");
                                PSW.SetVolt(product.VS9500, false);
                                product.VS9500.HardwareTestPass = false;
                                break;
                            case ModelUser.VS9000:
                                SupportBoard.Send_Data("*MODEL:VS9000\r\n");
                                PSW.SetVolt(product.VS9000, false);
                                product.VS9000.HardwareTestPass = false;
                                break;
                            default:
                                break;
                        }
                        StartTime = DateTime.Now;
                        TestPass = true;
                        GetTestProgress = TestProgress.Start;
                        Debug.Write("Test start", Debug.ContentType.Log);
                    }
                    break;
                case TestProgress.Start:
                    SupportBoard.Send_Data("*TEST:StartProgress\r\n");
                    GetTestProgress = TestProgress.SuctionTest;
                    Debug.Write("Suction test", Debug.ContentType.Log);
                    break;
                case TestProgress.SuctionTest:
                    Console.WriteLine(DateTime.Now.Subtract(StartTime).TotalSeconds);
                    if (PSW.Ampere < 0.01)
                    {
                        SupportBoard.Send_Data("*TEST:EndProgress:FAIL\r\n");
                        GetTestProgress = TestProgress.EndTest;
                        Debug.Write("Fail suction or not scan barcode.", Debug.ContentType.Error);
                        TestPass = false;
                    }
                    else if (DateTime.Now.Subtract(StartTime).TotalSeconds > 45)
                    {
                        SupportBoard.Send_Data("*TEST:EndProgress:FAIL\r\n");
                        GetTestProgress = TestProgress.EndTest;
                        Debug.Write("Fail suction", Debug.ContentType.Error);
                        TestPass = false;
                    }
                    else if (CNS_Path != "")
                    {
                        if (Directory.Exists(CNS_Path))
                        {
                            string Folder = CNS_Path;
                            var files = new DirectoryInfo(Folder).GetFiles();
                            theLastFile = null;
                            foreach (FileInfo file in files)
                            {
                                if (file.LastWriteTime > lastModified)
                                {
                                    lastModified = file.LastWriteTime;
                                    theLastFile = file;
                                }
                            }
                            if (theLastFile != null)
                            {
                                Console.WriteLine("Latest File Name: " + theLastFile.Name);
                                bool suctionResult = false;
                                QR.getCode(theLastFile.FullName, out suctionResult);

                                if (!suctionResult)
                                {
                                    GetTestProgress = TestProgress.EndTest;
                                    PSW.SetVolt(product.VS9000, true);
                                    Debug.Write("Fail suction", Debug.ContentType.Error);
                                    TestPass = false;
                                }
                                else
                                {
                                    switch (modelUser)
                                    {
                                        case ModelUser.VS9500:
                                            if (product.VS9500.HardwareTest)
                                            {
                                                GetTestProgress = TestProgress.HardwareTest;
                                                StartTime = DateTime.Now;
                                            }
                                            else
                                            {
                                                QR.print(theLastFile.FullName, GT800_Printer);
                                                GetTestProgress = TestProgress.EndTest;
                                                Debug.Write("Pass", Debug.ContentType.Notify);
                                            }
                                            break;
                                        case ModelUser.VS9000:
                                            if (product.VS9500.HardwareTest)
                                            {
                                                GetTestProgress = TestProgress.HardwareTest;
                                            }
                                            else
                                            {
                                                QR.print(theLastFile.FullName, GT800_Printer);
                                                GetTestProgress = TestProgress.EndTest;
                                                Debug.Write("Pass", Debug.ContentType.Notify);
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                Debug.Write("Suction test finish", Debug.ContentType.Log);
                            }
                        }
                    }
                    else
                    {
                        GetTestProgress = TestProgress.EndTest;
                    }
                    break;
                case TestProgress.HardwareTest:
                    switch (modelUser)
                    {
                        case ModelUser.VS9500:
                            if (product.VS9500.HardwareTest != null)
                            {
                                if (product.VS9500.HardwareTestPass)
                                {
                                    GetTestProgress = TestProgress.EndTest;
                                    QR.print(theLastFile.FullName, GT800_Printer);
                                }
                                else
                                {
                                    GetTestProgress = TestProgress.EndTest;
                                    TestPass = false;
                                    Debug.Write("Charger wire inverted or not connected", Debug.ContentType.Error);
                                }
                            }
                            else
                            {
                                if (DateTime.Now.Subtract(StartTime).TotalSeconds > 2)
                                {
                                    GetTestProgress = TestProgress.EndTest;
                                    TestPass = false;
                                    Debug.Write("Charger wire inverted or not connected", Debug.ContentType.Error);
                                }
                            }
                            break;
                        case ModelUser.VS9000:
                            SupportBoard.Send_Data("*TEST:SHORT\r\n");
                            Thread.Sleep(500);
                            if (PSW.Ampere < 1)
                            {
                                QR.print(theLastFile.FullName, GT800_Printer);
                                GetTestProgress = TestProgress.EndTest;
                                Debug.Write("Brush test pass", Debug.ContentType.Notify);
                            }
                            else
                            {
                                TestPass = false;
                                GetTestProgress = TestProgress.EndTest;
                                PSW.SetVolt(product.VS9000, true);
                                Debug.Write("Brush wire not connected", Debug.ContentType.Error);
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case TestProgress.EndTest:
                    if (theLastFile != null)
                    {
                        AddHistory(TestPass);
                        theLastFile.Delete();   
                    }
                    GetTestProgress = TestProgress.Wait;
                    PSW.SetVolt(product.VS9000, true);
                    //PSW.OFF();
                    if (TestPass)
                    {
                        SupportBoard.Send_Data("*TEST:EndProgress:PASS\r\n");
                        Debug.Write("Pass", Debug.ContentType.Notify);
                    }
                    else
                    {
                        SupportBoard.Send_Data("*TEST:EndProgress:FAIL\r\n");
                        Debug.Write("Fail", Debug.ContentType.Error);
                    }
                    break;
                case TestProgress.Wait:
                    if (PSW.Ampere < 0.01)
                    {
                        GetTestProgress = TestProgress.Ready;
                    }
                    break;
                default:
                    break;
            }
            //LoadDataTest(DataTest);
        }

        public void AddHistory(bool resutl)
        {
            QR.DataTest.No = DataTestteds.Count + 1;
            QR.DataTest.Status = resutl == true ? "PASS" : "FAIL";
            DataTestteds.Insert(0, QR.DataTest);
            DataTest.Items.Refresh();

            DataTestted data = QR.DataTest;

            string todayFile = "History/" + DateTime.Now.Year;   // + "/" + DateTime.Now.ToString("MMMM_dd") + ".txt";
            if (!Directory.Exists(todayFile)) Directory.CreateDirectory(todayFile);
            todayFile += "/" + DateTime.Now.ToString("MM_dd") + ".txt";
            string strAdd = data.No.ToString() + " " + data.Barcode + " " + data.Date + " " + data.Time + " " + data.Suction + " " + data.Status + Environment.NewLine;
            File.AppendAllText(todayFile, strAdd);
        }





        // printer 
        public void Testprinter()
        {
            //Ngôn ngữ máy in EPL

            var _12digitx = "A80"; // Lưu các thông số vào các biến
            var _12digity = "150"; // Lưu các thông số vào các biến
            var _11digitx = "A80"; // Lưu các thông số vào các biến
            var _11digity = "150"; // Lưu các thông số vào các biến
            var qr_x = "b80"; // Lưu các thông số vào các biến
            var qr_y = "13"; // Lưu các thông số vào các biến

            string str = "I8,A,001\n";
            str += "\n";
            str += "Q272,035\n";
            str += "q1228\n";
            str += "rN\n";
            str += "S5\n"; // Tốc độ
            str += "D10\n"; // Độ đậm nhạt
            str += "ZT\n";
            str += "JF\n";
            str += "O\n";
            str += "R478,0\n";
            str += "f100\n";
            str += "N\n";
            str += qr_x + "," + qr_y + ",Q," + QR.Mode + "," + QR.Size + ",eQ,iA\"CODE\"\n"; // Nội dung QR code
            str += _12digitx + "," + _12digity + ",4,1,1,1,N,\"18DJ99999999\"\n"; // Nội dung 12 ký tự
            str += _11digitx + "," + _11digity + ",3,1,1,1,N,\"DYSCCNC0123\"\n";
            //str += _12digitx + "," + _12digity + ",4,a,1,1,N,\"12DIGITS\"\n"; // Nội dung 12 ký tự
            str += "P1\n";

            string code = str.Replace("CODE", QR.testCode());

            System.Windows.Forms.PrintDialog pd = new System.Windows.Forms.PrintDialog();
            pd.PrinterSettings = new PrinterSettings();
            GT800_Printer.SendStringToPrinter(pd.PrinterSettings.PrinterName, code); // Gửi dữ liệu cho máy in
        }

        public void LoadPrintToView(QR_Code qR_Code)
        {
            tbQrX.Text = qR_Code.label.qr_x.ToString();
            tbQrY.Text = qR_Code.label.qr_y.ToString();
            tbQrSize.Text = qR_Code.Size.ToString();
            tbQrMode.Text = qR_Code.Mode.ToString();

            tbQR_QRcode.Text = qR_Code.QRCode;
            tbQR_CountryCode.Text = qR_Code.CountryCode;
            tbQR_Line.Text = qR_Code.ProductionLine;
            tbQR_EQP.Text = qR_Code.InspectionEquipment;

            tbQrModelX.Text = qR_Code.label.modelCodeX.ToString();
            tbQrModelY.Text = qR_Code.label.modelCodeY.ToString();

            tbQrSerialX.Text = qR_Code.label.serialCodeX.ToString();
            tbQrSerialY.Text = qR_Code.label.serialCodeY.ToString();

            tbQrDark.Text = qR_Code.label.dark.ToString();
            tbQrSpeed.Text = qR_Code.label.speed.ToString();
        }

        public void GetPrintFromView(QR_Code qR_Code)
        {
            qR_Code.label.qr_x = Convert.ToInt32(tbQrX.Text);
            qR_Code.label.qr_y = Convert.ToInt32(tbQrY.Text);
            qR_Code.Size = Convert.ToInt32(tbQrSize.Text);
            qR_Code.Mode = Convert.ToInt32(tbQrMode.Text);

            qR_Code.QRCode = tbQR_QRcode.Text;
            qR_Code.CountryCode = tbQR_CountryCode.Text;
            qR_Code.ProductionLine = tbQR_Line.Text;
            qR_Code.InspectionEquipment = tbQR_EQP.Text;

            qR_Code.label.modelCodeX = Convert.ToInt32(tbQrModelX.Text);
            qR_Code.label.modelCodeY = Convert.ToInt32(tbQrModelY.Text);
            qR_Code.label.serialCodeX = Convert.ToInt32(tbQrSerialX.Text);
            qR_Code.label.serialCodeY = Convert.ToInt32(tbQrSerialY.Text);

            qR_Code.label.dark = Convert.ToInt32(tbQrDark.Text);
            qR_Code.label.speed = Convert.ToInt32(tbQrSpeed.Text);

            GT800_Printer.IsUseSerial = !(bool)cbUSBPrinter.IsChecked;
            if (GT800_Printer.IsUseSerial)
            {
                GT800_Printer.SerialInit();
            }
        }

        public void LoadProductConfigToView()
        {
            tbVS9000_Volt.Text = product.VS9000.Volt.ToString();
            tbVS9000_VoltProtect.Text = product.VS9000.OverVolt.ToString();
            tbVS9000_Ampe.Text = product.VS9000.Ampe.ToString();
            tbVS9000_AmpeProtect.Text = product.VS9000.OverAmpe.ToString();
            tbVS9000_Outdelay.Text = product.VS9000.OutputOnDelay.ToString("F1");
            tbVS9000_OutOffdelay.Text = product.VS9000.OutputOffDelay.ToString("F1");
            cbVS9000_HardwareTest.IsChecked = product.VS9000.HardwareTest;

            tbVS9500_Volt.Text = product.VS9500.Volt.ToString();
            tbVS9500_VoltProtect.Text = product.VS9500.OverVolt.ToString();
            tbVS9500_Ampe.Text = product.VS9500.Ampe.ToString();
            tbVS9500_AmpeProtect.Text = product.VS9500.OverAmpe.ToString();
            tbVS9500_Outdelay.Text = product.VS9500.OutputOnDelay.ToString("F1");
            tbVS9500_OutOffdelay.Text = product.VS9500.OutputOffDelay.ToString("F1");
            cbVS9500_HardwareTest.IsChecked = product.VS9500.HardwareTest;
        }
        public void GetProductConfigFromView()
        {
            product.VS9000.Volt = Convert.ToDouble(tbVS9000_Volt.Text);
            product.VS9000.OverVolt = Convert.ToDouble(tbVS9000_VoltProtect.Text);
            product.VS9000.Ampe = Convert.ToDouble(tbVS9000_Ampe.Text);
            product.VS9000.OverAmpe = Convert.ToDouble(tbVS9000_AmpeProtect.Text);
            product.VS9000.OutputOnDelay = Convert.ToDouble(tbVS9000_Outdelay.Text);
            product.VS9000.OutputOffDelay = Convert.ToDouble(tbVS9000_OutOffdelay.Text);
            product.VS9000.HardwareTest = (bool)cbVS9000_HardwareTest.IsChecked;


            product.VS9500.Volt = Convert.ToDouble(tbVS9500_Volt.Text);
            product.VS9500.OverVolt = Convert.ToDouble(tbVS9500_VoltProtect.Text);
            product.VS9500.Ampe = Convert.ToDouble(tbVS9500_Ampe.Text);
            product.VS9500.OverAmpe = Convert.ToDouble(tbVS9500_AmpeProtect.Text);
            product.VS9500.OutputOnDelay = Convert.ToDouble(tbVS9500_Outdelay.Text);
            product.VS9500.OutputOffDelay = Convert.ToDouble(tbVS9500_OutOffdelay.Text);
            product.VS9500.HardwareTest = (bool)cbVS9500_HardwareTest.IsChecked;
        }






        // check file config
        private void Config_check()
        {
            if (File.Exists(QRconfigPath))
            {
                string QRJson = File.ReadAllText(QRconfigPath);
                Debug.Write(QRJson, Debug.ContentType.Log);
                QR = JsonSerializer.Deserialize<QR_Code>(QRJson);
                //MessageBox.Show(
                //    "Unit Code: " + QR.UnitCode + Environment.NewLine +
                //    "Material Code: " + QR.MaterialCode + Environment.NewLine +
                //    "Supplier Code: " + QR.SupplierCode + Environment.NewLine +
                //    "Production Date(YY / MM / DD): " + QR.ProductionDate + Environment.NewLine +
                //    "Serial Number: " + QR.SerialNumber + Environment.NewLine +
                //    "QR Code: " + QR.QRCode + Environment.NewLine +
                //    "Country Code: " + QR.CountryCode + Environment.NewLine +
                //    "Production Line: " + QR.ProductionLine + Environment.NewLine +
                //    "Inspection Equipment Number(Serial): " + QR.InspectionEquipment + Environment.NewLine +
                //    "Classification Symbol: " + QR.ClassificationSymbol + Environment.NewLine +
                //    "Separator(or delimiter): " + QR.Separator + Environment.NewLine
                //    );

            }
            else
            {
                QR.saveQRFormat();
                MessageBox.Show("QR parametter not set. All are default");
            }
            LoadPrintToView(QR);

            if (File.Exists(ProductConfigPath))
            {
                string strJson = File.ReadAllText(ProductConfigPath);
                Debug.Write(strJson, Debug.ContentType.Log);
                product = JsonSerializer.Deserialize<ProductConfig>(strJson);
            }
            else
            {
                product.saveConfig(ProductConfigPath);
                MessageBox.Show("produc parametter not set. All are default");
            }
            LoadProductConfigToView();

            if (File.Exists(PSWConfigPath))
            {
                string strJson = File.ReadAllText(PSWConfigPath);
                Debug.Write(strJson, Debug.ContentType.Log);
                PSW = JsonSerializer.Deserialize<PSW>(strJson);
            }
            else
            {
                PSW.saveConfig(PSWConfigPath);
                MessageBox.Show("PSW parametter not set. All are default");
            }

            PSW_ControlType.IsChecked = !PSW.IPcontrol;
            PSW_ControlEnthernet.IsChecked = PSW.IPcontrol;
            PSWIP.Text = PSW.IP;
            PSWIP.IsEnabled = PSW.IPcontrol;

            if (PSW.IPcontrol)
            {

                Thread IPCONTROL_PSW = new Thread(PSW.PSW_Client_Comunicate);
                IPCONTROL_PSW.Start();
            }

            if (File.Exists(PrinterConfigPath))
            {
                string strJson = File.ReadAllText(PrinterConfigPath);
                Debug.Write(strJson, Debug.ContentType.Log);
                GT800_Printer = JsonSerializer.Deserialize<GT800_Printer>(strJson);
            }
            else
            {
                GT800_Printer.saveConfig(PrinterConfigPath);
                MessageBox.Show("Printer parametter not set. All are default");
            }
            cbUSBPrinter.IsChecked = !GT800_Printer.IsUseSerial;
            cbbPrinterCOM.IsEnabled = GT800_Printer.IsUseSerial;
            cbbPrinterCOM.DropDownClosed += cbbPrinterCOM_DropDownClosed;

            if (File.Exists(FolderConfigPath))
            {
                string str = File.ReadAllText(FolderConfigPath);
                CNS_Path = str;
                tbCNS_Path.Text = str;

            }
        }



        private void QR_TextChange_Param(object sender, TextChangedEventArgs e)
        {
            int n = 0;
            if (!int.TryParse((sender as TextBox).Text, out n))
            {
                if ((sender as TextBox).Text.Length > 0)
                {
                    (sender as TextBox).Text = (sender as TextBox).Text.Remove((sender as TextBox).Text.Length - 1, 1);
                }
                else
                {
                    (sender as TextBox).Text = "0";
                }

            }
        }

        private void PrintTest_Click(object sender, RoutedEventArgs e)
        {
            GetPrintFromView(QR);
            QR.printTest(GT800_Printer);
            QR.saveQRFormat();
            GT800_Printer.saveConfig(PrinterConfigPath);
        }

        private void tbModelParam_Changed(object sender, TextChangedEventArgs e)
        {
            double n = 0;
            if (!double.TryParse((sender as TextBox).Text, out n))
            {
                if ((sender as TextBox).Text.Length > 0)
                {
                    (sender as TextBox).Text = (sender as TextBox).Text.Remove((sender as TextBox).Text.Length - 1, 1);
                }
                else
                {
                    (sender as TextBox).Text = "0.0";
                }

            }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            GetProductConfigFromView();
            product.saveConfig(ProductConfigPath);
            GetTestProgress = TestProgress.Ready;
            switch (modelUser)
            {
                case ModelUser.VS9500:
                    PSW.SetParam(product.VS9500);
                    Debug.Write(string.Format("Power VS9500 change to {0} V  {1} A", product.VS9500.Volt, product.VS9500.Ampe), Debug.ContentType.Warning);
                    break;
                case ModelUser.VS9000:
                    PSW.SetParam(product.VS9000);
                    Debug.Write(string.Format("Power VS9000 change to {0} V  {1} A", product.VS9500.Volt, product.VS9500.Ampe), Debug.ContentType.Warning);
                    break;
                default:
                    break;
            }

        }


        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            (sender as ToggleButton).Content = "VS9500";
            modelUser = ModelUser.VS9500;
            GetTestProgress = TestProgress.Ready;
            switch (modelUser)
            {
                case ModelUser.VS9500:
                    PSW.SetParam(product.VS9500);
                    break;
                case ModelUser.VS9000:
                    PSW.SetParam(product.VS9000);
                    break;
                default:
                    break;
            }
            if (VS9000_set != null)
            {
                BrushColumn.Width = new GridLength(100);
                Debug.Write("Use VS9500 models", Debug.ContentType.Log);
            }
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            (sender as ToggleButton).Content = "VS9000";
            modelUser = ModelUser.VS9000;
            GetTestProgress = TestProgress.Ready;
            Thread.Sleep(50);
            switch (modelUser)
            {
                case ModelUser.VS9500:
                    PSW.SetParam(product.VS9500);
                    break;
                case ModelUser.VS9000:
                    PSW.ON();
                    PSW.SetParam(product.VS9000);
                    break;
                default:
                    break;
            }
            if (VS9000_set != null)
            {
                BrushColumn.Width = new GridLength(0);
                Debug.Write("Use VS9000 models", Debug.ContentType.Log);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            QR.saveQRFormat();
            // stop timer
            updateTimer.Stop();
            updateTimer.Dispose();

            checkFileTimer.Stop();
            checkFileTimer.Dispose();




            var serialPort = SerialPort.GetPortNames();
            foreach (var item in serialPort)
            {
                SerialPort serial = new SerialPort()
                {
                    PortName = item
                };
                if (serial.IsOpen)
                {
                    serial.Close();
                }
            }
        }

        private void cbUSBPrinter_Checked(object sender, RoutedEventArgs e)
        {
            cbbPrinterCOM.IsEnabled = !(bool)((sender as CheckBox).IsChecked);
            GT800_Printer.IsUseSerial = !(bool)((sender as CheckBox).IsChecked);
        }


        private void PSW_ControlType_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox) == PSW_ControlType)
            {
                PSW_ControlEnthernet.IsChecked = !(bool)((sender as CheckBox).IsChecked);
                PSWIP.IsEnabled = !(bool)((sender as CheckBox).IsChecked);
            }
            else if ((sender as CheckBox) == PSW_ControlEnthernet)
            {
                PSW_ControlType.IsChecked = !(bool)((sender as CheckBox).IsChecked);
                PSWIP.IsEnabled = (bool)((sender as CheckBox).IsChecked);
            }
        }

        private void btPSW_test_Click(object sender, RoutedEventArgs e)
        {
            if (!PSW.IsOutputON) PSW.ON(); else PSW.OFF();
        }

        private void btPSW_Control_Apply_Click(object sender, RoutedEventArgs e)
        {
            PSW.IP = PSWIP.Text;
            PSW.IPcontrol = (bool)PSW_ControlEnthernet.IsChecked;
            if (PSW.IPcontrol)
            {
                Thread IPCONTROL_PSW = new Thread(PSW.PSW_Client_Comunicate);
                IPCONTROL_PSW.Start();
            }
            PSW.saveConfig(PSWConfigPath);
        }

        private void btCheckComunications_Click(object sender, RoutedEventArgs e)
        {
            CommunicationPortScan();
        }

        private void btCNS_Path_Browser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();
            tbCNS_Path.Text = dialog.FileName;
            CNS_Path = dialog.FileName;
            File.WriteAllText(FolderConfigPath, CNS_Path);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }
    }
}
