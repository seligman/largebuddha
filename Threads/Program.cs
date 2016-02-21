using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MandelThreads
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            if (Settings.Init())
            {
                MainWorker.Run();
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
