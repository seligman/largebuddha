using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateWorker
{
    class Program
    {
        static HashSet<string> s_created = new HashSet<string>();

        static void Main(string[] args)
        {
            if (!Settings.Init())
            {
                Console.WriteLine("Worker_Settings.txt created.");
                return;
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("@echo off");

            for (int x = 0; x < Settings.TileCount; x++)
            {
                CreateStep(sb, x, x);
            }

            for (int x = 0; x < Settings.TileCount; x++)
            {
                CreateStep(sb, x + 1, x);
            }

            for (int x = 0; x < Settings.TileCount; x++)
            {
                CreateStep(sb, x, 0);
            }

            for (int x = 0; x < Settings.TileCount; x++)
            {
                CreateStep(sb, Settings.TileCount - 1, x);
            }

            for (int x = 0; x < Settings.TileCount; x++)
            {
                CreateStep(sb, (Settings.TileCount - 1) - x, Settings.TileCount - 1);
            }

            for (int x = 0; x < Settings.TileCount; x++)
            {
                CreateStep(sb, 0, (Settings.TileCount - 1) - x);
            }

            for (int y = 0; y < Settings.TileCount; y++)
            {
                for (int x = 0; x < Settings.TileCount; x++)
                {
                    CreateStep(sb, x, y);
                }
            }

            File.WriteAllText("RunThreads.cmd", sb.ToString());

            Console.WriteLine("All done!");
        }

        static void CreateStep(StringBuilder sb, int x, int y)
        {
            if (x < 0 || y < 0 || x >= Settings.TileCount || y >= Settings.TileCount)
            {
                return;
            }

            string id = x + "x" + y;

            if (s_created.Contains(id))
            {
                return;
            }

            Console.WriteLine("Working on " + x + " x " + y);

            s_created.Add(id);

            Directory.CreateDirectory(id);

            File.Copy("MandelThreads.exe", Path.Combine(id, "MandelThreads.exe"), true);
            File.Copy("PixelHelper.dll", Path.Combine(id, "PixelHelper.dll"), true);

            StringBuilder set = new StringBuilder();

            set.AppendLine("Width = " + (Settings.Worker_PixelsPerTile * Settings.TileCount));
            set.AppendLine("Height = " + (Settings.Worker_PixelsPerTile * Settings.TileCount));
            set.AppendLine("ViewOffX = " + (Settings.Worker_PixelsPerTile * x));
            set.AppendLine("ViewOffY = " + (Settings.Worker_PixelsPerTile * y));
            set.AppendLine("ViewWidth = " + Settings.Worker_PixelsPerTile);
            set.AppendLine("ViewHeight = " + Settings.Worker_PixelsPerTile);
            set.AppendLine("Scale = 8");
            set.AppendLine("Iters = " + Settings.Worker_Iters);
            set.AppendLine("Alias = " + Settings.Worker_Alias);
            set.AppendLine("Threads = " + Settings.Worker_Threads);
            set.AppendLine("SaveAngleData = True");
            if (Settings.Worker_DrawBuddhabrot)
            {
                set.AppendLine("Mode = 1");
            }
            else
            {
                set.AppendLine("Mode = 4");
            }

            File.WriteAllText(Path.Combine(id, "Settings.txt"), set.ToString());

            sb.AppendLine("");
            sb.AppendLine("if exist \"" + id + "\" (");
            sb.AppendLine("    cd " + id + "");
            sb.AppendLine("    if not exist \"MandelThreads_0000.dat\" (");
            sb.AppendLine("        echo Working on Tile: " + x + " x " + y + "");
            sb.AppendLine("        MandelThreads.exe");
            sb.AppendLine("    )");
            sb.AppendLine("    cd ..");
            if (Settings.CallS3Script)
            {
                sb.AppendLine("    call SendToS3.cmd " + id);
            }
            sb.AppendLine(")");
            sb.AppendLine("");
            sb.AppendLine("if exist \"Abort.txt\" goto :EOF");
        }
    }
}
