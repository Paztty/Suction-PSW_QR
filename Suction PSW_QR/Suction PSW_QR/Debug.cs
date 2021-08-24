using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Suction_PSW_QR
{

    public static class Debug
    {
        public enum ContentType
        { 
            Error = 0,
            Notify = 1,
            Log = 2,
            Warning = 3,
        }

        public static RichTextBox richTextBox;

        public static void Write(string content, ContentType type)
        {

            BrushConverter textBrush = new BrushConverter();

            if (richTextBox != null)
            {
                var paragraph = new Paragraph();

                paragraph.Inlines.Add(new Run(DateTime.Now.ToString("HH:mm:ss ")+ content));
                switch (type)
                {
                    case ContentType.Error:
                        paragraph.Foreground = new SolidColorBrush(Colors.Red);
                        break;
                    case ContentType.Notify:
                        paragraph.Foreground = new SolidColorBrush(Colors.LightGreen);
                        break;
                    case ContentType.Log:
                        paragraph.Foreground = new SolidColorBrush(Colors.White);
                        break;
                    case ContentType.Warning:
                        paragraph.Foreground = new SolidColorBrush(Colors.Yellow);
                        break;
                    default:
                        break;
                }

                richTextBox.Document.Blocks.Add(paragraph);
                richTextBox.ScrollToEnd();
            }
        }
    }
}
