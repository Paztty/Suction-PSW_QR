using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Suction_PSW_QR
{

    // model history
    public class DataTestted
    {
        public int No { get; set; }
        public string Barcode { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public double Suction { get; set; }
        public string Status { get; set; }

        public DataTestted() { }

        public void LoadDemo()
        {

        }
    }

    public class QR_Code
    {
        public string UnitCode { get; set; } = "";
        public string MaterialCode { get; set; } = "";
        public string SupplierCode { get; set; } = "";
        public string ProductionDate { get; set; } = "";
        public string SerialNumber { get; set; } = "";

        public string QRCode { get; set; } = "";
        public string CountryCode { get; set; } = "";
        public string ProductionLine { get; set; } = "";
        public string InspectionEquipment { get; set; } = "";

        public string ThenumberofInspectionitem { get; set; } = "01";
        public string InspectionStart { get; set; } = "";
        public string InspectionEnd { get; set; } = "";
        public string InspectionItem { get; set; } = "";
        public string MeasuredValue { get; set; } = "";
        public string UpperLimitSpecificationValu { get; set; } = "";
        public string LowerLimitSpecificationValue { get; set; } = "";
        public string ClassificationSymbol { get; set; } = "/";
        public string Separator { get; set; } = "-";

        public int Size { get; set; } = 4;
        public int Mode { get; set; } = 2;

        public string TestResult = "";

        public DataTestted DataTest {
            get;
            set;
        }

        public class Label
        {
            public int qr_x { get; set; } = 80;
            public int qr_y { get; set; } = 13;
            public int modelCodeX { get; set; } = 70;
            public int modelCodeY { get; set; } = 150;
            public int serialCodeX { get; set; } = 60;
            public int serialCodeY { get; set; } = 150;

            public int dark { get; set; } = 15;
            public int speed { get; set; } = 1;

            public string QrCodeData = "";
            public string SerialCode = "";
            public string ModelCode = "";
        }

        public Label label { get; set; } = new Label();

        public List<string> parameter = new List<string>();

        public string ConfigPath = Environment.CurrentDirectory + @"\QRformat.config";

        public QR_Code() {
            
        }
            
        // DEVQR = 1 8  D J 9 7 0 2 6 8 3 C  D Y S C  N C Z  0 2 5 1
        //         0 1  2 3 4 5 6 7 8 9 1011 12131415 161718 19202122
        // ";;;A;;02;20210102072634;20210102072641;18DJ9702683D_23;(A045-00204-00300-00200)^OK;(A028-00.00-01000--1000)^OK;;; ^OK"
        //  012 34  5              6              7               8                           9                         101112
        public string getCode(string path, out bool result)
        {
            FileInfo fileInfo = new FileInfo(path);
            string DevQR = fileInfo.Name;
            DevQR = DevQR.Substring(0, DevQR.IndexOf('-'));

            label.SerialCode = DevQR.Substring(12, 11);
            label.ModelCode = DevQR.Substring(0, 12);

            UnitCode = DevQR.Substring(0, 2);
            MaterialCode = DevQR.Substring(2, 10);
            SupplierCode = DevQR.Substring(12, 4);
            ProductionDate = DevQR.Substring(16, 3);
            SerialNumber = DevQR.Substring(19, 4);
            QRCode = QRCode;
            CountryCode = CountryCode;
            ProductionLine = ProductionLine;
            InspectionEquipment = InspectionEquipment;

            parameter = new List<string>();

            string dataTest = File.ReadAllText(path);
            string[] splitDataTest = dataTest.Split(';');

            result = splitDataTest[13].Contains("OK");

            DataTest = new DataTestted()
            {
                Barcode = label.ModelCode + label.SerialCode,
                Date = splitDataTest[7].Substring(0, 4) + "/" + splitDataTest[7].Substring(4, 2) + "/" + splitDataTest[7].Substring(6, 2),
                Time = splitDataTest[7].Substring(8, 2) + ":" + splitDataTest[7].Substring(10, 2) + ":" + splitDataTest[7].Substring(12, 2),
                Suction = Convert.ToDouble(splitDataTest[9].Split('-')[1]),
                Status = splitDataTest[13].Contains("OK") == true ? "PASS" : "FAIL",
            };

            for (int i = 0; i < splitDataTest.Length; i++)
            {
                parameter.Add(splitDataTest[i]);
            }
            string code = UnitCode
                        + MaterialCode
                        + SupplierCode
                        + ProductionDate
                        + SerialNumber
                        + QRCode
                        + CountryCode
                        + ProductionLine
                        + InspectionEquipment
                        + ThenumberofInspectionitem
                        + ClassificationSymbol
                        + parameter[6]
                        + ClassificationSymbol
                        + parameter[7]
                        + ClassificationSymbol;

            for (int itemTest = 9; itemTest < (9 + Convert.ToInt32(ThenumberofInspectionitem)); itemTest++)
            {
                if (parameter[itemTest].Contains("^OK"))
                {
                    code += parameter[itemTest].Replace("^OK", "").Replace("(", "").Replace(")", "");
                }
                if (parameter[itemTest].Contains("^NG"))
                {
                    code += parameter[itemTest].Replace("^NG", "").Replace("(", "").Replace(")", "");
                }
                code += ClassificationSymbol;
            }
            Console.WriteLine(dataTest);
            Console.WriteLine(code);
            return code;
        }


        public string testCode()
        {
            string DevQR = "99AA9999999ADYSCNBS9999-20201125220142.txt";
            DevQR = DevQR.Substring(0, DevQR.IndexOf('-'));

            label.SerialCode = DevQR.Substring(12, 11);
            label.ModelCode = DevQR.Substring(0, 12);

            UnitCode = DevQR.Substring(0, 2);
            MaterialCode = DevQR.Substring(2, 10);
            SupplierCode = DevQR.Substring(12, 4);
            ProductionDate = DevQR.Substring(16, 3);
            SerialNumber = DevQR.Substring(19, 4);

            QRCode = QRCode;
            CountryCode = CountryCode;
            ProductionLine = ProductionLine;
            InspectionEquipment = InspectionEquipment;

            parameter = new List<string>();


            string dataTest = "; ; ; A; ; 02; 20201111111111; 20201111111112; 18DJ99999A_23;(A045 - 99999 - 99999 - 99999) ^ OK; (A028 - 9999 - 9999--9999)^OK; ; ; ^OK";
            dataTest = dataTest.Replace(" ", "");
            string[] splitDataTest = dataTest.Split(';');

            for (int i = 0; i < splitDataTest.Length; i++)
            {
                parameter.Add(splitDataTest[i]);
            }
            string code = UnitCode
                        + MaterialCode
                        + SupplierCode
                        + ProductionDate
                        + SerialNumber
                        + QRCode
                        + CountryCode
                        + ProductionLine
                        + InspectionEquipment
                        + ThenumberofInspectionitem
                        + ClassificationSymbol
                        + parameter[6]
                        + ClassificationSymbol
                        + parameter[7]
                        + ClassificationSymbol;

            for (int itemTest = 9; itemTest < (9 + Convert.ToInt32(ThenumberofInspectionitem)); itemTest++)
            {
                code += parameter[itemTest].Replace("^OK", "").Replace("(", "").Replace(")", "");
                code += parameter[itemTest].Replace("^NG", "");
                code += ClassificationSymbol;
            }
            return code;
        }


        public void print( string pathToGetData, GT800_Printer printer)
        {
            bool reusultTestFile;
            var dataToPrint = getCode(pathToGetData, out reusultTestFile);
            // string print built
            string str = "I8,A,001\n";
            str += "\n";
            str += "Q272,035\n";
            str += "q1228\n";
            str += "rN\n";
            //str += "S1\n";
            str += "SPEED\n";
            //str += "D12\n";
            str += "DARK\n";
            str += "ZT\n";
            str += "JF\n";
            str += "O\n";
            str += "R478,0\n";
            str += "f100\n";
            str += "N\n";
            //str += qr_x + "," + qr_y + ",Q,m2,s3,eQ,iA\"CODE\"\n";
            str += "b" + label.qr_x + "," + label.qr_y + ",Q,m" +  Mode + ",s" + Size + ",eQ,iA\"CODE\"\n"; // Nội dung QR code
            str += "A" + label.modelCodeX + "," + label.modelCodeY + ",4,1,1,1,N,\"12DIGITS\"\n";
            str += "A" + label.serialCodeX + "," + label.serialCodeY + ",3,1,1,1,N,\"11DIGITS\"\n";
            str += "P1\n";

            str = str.Replace("CODE", dataToPrint);
            str = str.Replace("12DIGITS", label.ModelCode);
            str = str.Replace("11DIGITS", label.SerialCode);
            //string replace_11DIGIT = replace_12DIGIT.Replace("11DIGITS", "");
            str = str.Replace("SPEED", "S" + label.speed.ToString());
            str = str.Replace("DARK", "D" + label.dark.ToString());


            Console.WriteLine(str);
            if (reusultTestFile)
            {
                if (printer.IsUseSerial)
                {
                    printer.SendStringToPrinter(str);
                }
                else
                {

                    System.Windows.Forms.PrintDialog pd = new System.Windows.Forms.PrintDialog();
                    pd.PrinterSettings = new PrinterSettings();
                    GT800_Printer.SendStringToPrinter(pd.PrinterSettings.PrinterName, str);
;
                }
                TestResult = "PASS";
            }
            else
            {
                TestResult = "FAIL";
            }
        }

        public void printTest( GT800_Printer printer)
        {
            var dataToPrint = testCode();
            // string print built
            string str = "I8,A,001\n";
            str += "\n";
            str += "Q272,035\n";
            str += "q1228\n";
            str += "rN\n";
            //str += "S1\n";
            str += "SPEED\n";
            //str += "D12\n";
            str += "DARK\n";
            str += "ZT\n";
            str += "JF\n";
            str += "O\n";
            str += "R478,0\n";
            str += "f100\n";
            str += "N\n";
            //str += qr_x + "," + qr_y + ",Q,m2,s3,eQ,iA\"CODE\"\n";
            str += "b" + label.qr_x + "," + label.qr_y + ",Q,m" + Mode + ",s" + Size + ",eQ,iA\"CODE\"\n"; // Nội dung QR code
            str += "A" + label.modelCodeX + "," + label.modelCodeY + ",4,1,1,1,N,\"12DIGITS\"\n";
            str += "A" + label.serialCodeX + "," + label.serialCodeY + ",3,1,1,1,N,\"11DIGITS\"\n";
            str += "P1\n";

            str = str.Replace("CODE", dataToPrint);
            str = str.Replace("12DIGITS", label.ModelCode);
            str = str.Replace("11DIGITS", label.SerialCode);
            //string replace_11DIGIT = replace_12DIGIT.Replace("11DIGITS", "");
            str = str.Replace("SPEED", "S" + label.speed.ToString());
            str = str.Replace("DARK", "D" + label.dark.ToString());

            Console.WriteLine(str);

            if (printer.IsUseSerial)
            {
                printer.SendStringToPrinter(str);
            }
            else
            {
                System.Windows.Forms.PrintDialog pd = new System.Windows.Forms.PrintDialog();
                pd.PrinterSettings = new PrinterSettings();
                GT800_Printer.SendStringToPrinter(pd.PrinterSettings.PrinterName, str);
            }
        }


        public void saveQRFormat()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string QRformat = JsonSerializer.Serialize(this, options);
            //if (!Directory.Exists(ConfigPath)) Directory.CreateDirectory(ConfigPath);
            File.WriteAllText(ConfigPath, QRformat);
        }

    }
}
