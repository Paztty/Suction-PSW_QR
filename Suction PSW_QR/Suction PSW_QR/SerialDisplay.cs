using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Suction_PSW_QR
{
    public class SerialDisplay
    {
        public Rectangle IsOpenRect;
        public Rectangle TX;
        public Rectangle RX;

        private bool blinkRX = true;
        private bool blinkTX = true;

        public void BlinkTX()
        {
            TX.Fill = blinkTX == true ? new SolidColorBrush(Colors.Yellow) : new SolidColorBrush(Colors.DarkOrange);
            blinkTX = !blinkTX;
        }

        public void BlinkRX()
        {
           RX.Fill = blinkRX == true ? new SolidColorBrush(Colors.Yellow) : new SolidColorBrush(Colors.DarkGreen);
           blinkRX = !blinkRX;
        }

        public void ShowCOMStatus(bool IsOpen)
        {
            if (IsOpenRect != null)
            {
                IsOpenRect.Fill = IsOpen ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Gray);
            }
        }
    }
}
