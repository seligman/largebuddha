using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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

            for (int off = 0; off < Settings.TileCount; off++)
            {
                for (int flip = -1; flip <= 1; flip += 2)
                {
                    for (int x = 0; x < Settings.TileCount; x++)
                    {
                        CreateStep(x + flip * off, x);
                    }
                }
            }

            for (int y = 0; y < Settings.TileCount; y++)
            {
                for (int x = 0; x < Settings.TileCount; x++)
                {
                    CreateStep(x, y);
                }
            }

            Console.WriteLine("All done!");
        }

        static void CreateStep(int x, int y)
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

            s_created.Add(id);

            Console.WriteLine("Working on " + x + " x " + y);

            int stepNo = s_created.Count();
            int total = Settings.TileCount * Settings.TileCount;
            string perc = (((double)stepNo) / ((double)total) * 100.0).ToString("0.00");

            using (var zip = ZipFile.Open("step_" + stepNo.ToString("0000") + "_" + id + ".zip", ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile("MandelThreads.exe", "MandelThreads.exe");
                zip.CreateEntryFromFile("PixelHelper.dll", "PixelHelper.dll");
                zip.CreateEntryFromFile("VisualizeArray.exe", "VisualizeArray.exe");

                var entry = zip.CreateEntry("Settings.txt");
                using (StreamWriter set = new StreamWriter(entry.Open()))
                {
                    set.WriteLine("Width = " + (Settings.Worker_PixelsPerTile * Settings.TileCount));
                    set.WriteLine("Height = " + (Settings.Worker_PixelsPerTile * Settings.TileCount));
                    set.WriteLine("ViewOffX = " + (Settings.Worker_PixelsPerTile * x));
                    set.WriteLine("ViewOffY = " + (Settings.Worker_PixelsPerTile * y));
                    set.WriteLine("ViewWidth = " + Settings.Worker_PixelsPerTile);
                    set.WriteLine("ViewHeight = " + Settings.Worker_PixelsPerTile);
                    set.WriteLine("Scale = 8");
                    set.WriteLine("Iters = " + Settings.Worker_Iters);
                    set.WriteLine("Iters2 = " + Settings.Worker_Iters2);
                    set.WriteLine("Iters3 = " + Settings.Worker_Iters3);
                    set.WriteLine("Alias = " + Settings.Worker_Alias);
                    set.WriteLine("Threads = " + Settings.Worker_Threads);
                    set.WriteLine("SaveAngleData = " + Settings.Worker_SaveAngleData);
                    set.WriteLine("SaveTriLimits = " + Settings.Worker_SaveTriLimits);

                    if (Settings.Worker_DrawBuddhabrot)
                    {
                        set.WriteLine("Mode = 1");
                    }
                    else
                    {
                        set.WriteLine("Mode = 4");
                    }
                }

                entry = zip.CreateEntry("RunIt.cmd");
                using (StreamWriter set = new StreamWriter(entry.Open()))
                {
                    set.WriteLine("title Tile: " + x + " x " + y + "");
                    set.WriteLine("echo Working on Tile: " + x + " x " + y);
                    set.WriteLine("echo Step " + stepNo + " of " + total + ", " + perc + " perc");
                    set.WriteLine("MandelThreads.exe");
                    set.WriteLine("VisualizeArray.exe compress MandelThreads_0000.dat MandelThreads_0000.dgz");
                    if (Settings.Worker_S3.Length > 0)
                    {
                        set.WriteLine("aws s3 cp MandelThreads_0000.dgz " + Settings.Worker_S3 + id + "/MandelThreads_0000.dgz");
                    }
                }
            }
        }
    }
}
