using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MandelThreads
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (Settings.Init())
            {
                Application.Run(new MainForm());
            }
            else
            {
                Console.WriteLine("Settings file created.");
            }
        }

        [DllImport(@"PixelHelper.dll")]
        static extern void PH_Test(IntPtr temp, int count);

        static void Test()
        {
            PH_Test(IntPtr.Zero, 0);
        }
    }
}
