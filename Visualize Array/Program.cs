using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using ScottsUtils;

namespace VisualizeArray
{
    static class Program
    {
        [STAThread]
        static void Main(params string[] args)
        {
            bool showHelp = true;

            if (args.Length == 3)
            {
                if (args[0].ToLower() == "compress")
                {
                    Helpers.CompressFile(args[1], args[2]);
                    showHelp = false;
                }
                if (args[0].ToLower() == "decompress")
                {
                    Helpers.DecompressFile(args[1], args[2]);
                    showHelp = false;
                }
            }
            
            if (args.Length == 1 && File.Exists(args[0]))
            {
                if (Settings.Init(args[0]))
                {
                    VisualizeWorker.MainWorker();
                }
                showHelp = false;
            }
            else if (args.Length == 0)
            { 
                if (Settings.Init())
                {
                    VisualizeWorker.MainWorker();
                }
                else
                {
                    Console.WriteLine("Settings file created");
                }
                showHelp = false;
            }

            if (showHelp)
            {
                Console.WriteLine("compress <source> <dest>   - Compress file");
                Console.WriteLine("decompress <source> <dest> - Decompress file");
                Console.WriteLine("<file>                     - Run from specific settings file");
                Console.WriteLine("<no args>                  - Run from settings file");
            }

            if (!Settings.Test_Run)
            {
                if (Debugger.IsAttached)
                {
                    Console.Write("Press any key to continue . . . ");
                    Console.ReadKey();
                    Console.WriteLine();
                }
            }
        }
    }
}
