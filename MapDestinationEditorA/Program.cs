using System;
using System.Text;
using System.Windows.Forms;

namespace MapDestinationEditorA
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Enable legacy code pages (GBK/936, Big5, etc.)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}
