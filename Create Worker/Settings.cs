using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateWorker
{
    class Settings
    {
        public static int TileCount = 16;
        public static bool CallS3Script = true;

        public static bool Worker_DrawBuddhabrot = false;
        public static int Worker_Threads = 6;
        public static int Worker_PixelsPerTile = 8192;
        public static int Worker_Iters = 500000;
        public static int Worker_Alias = 25;

        public static bool Init()
        {
            if (File.Exists("Worker_Settings.txt"))
            {
                foreach (var line in File.ReadAllLines("Worker_Settings.txt"))
                {
                    if (!line.Trim().StartsWith("#"))
                    {
                        string[] split = line.Split('=');

                        foreach (var val in GetFields())
                        {
                            if (val.Name.ToLower().Replace("_", ".") == split[0].Trim().ToLower())
                            {
                                val.Value = split[1].Trim();
                            }
                        }
                    }
                }

                return true;
            }
            else
            {
                StringBuilder sb = new StringBuilder();

                foreach (var cur in GetFields())
                {
                    sb.AppendLine(cur.Name.Replace("_", ".") + " = " + cur.Value);
                }

                File.WriteAllText("Worker_Settings.txt", sb.ToString());

                return false;
            }
        }

        static IEnumerable<SettingValue> GetFields()
        {
            foreach (var fi in typeof(Settings).GetFields())
            {
                SettingValue ret = new SettingValue();
                ret.Name = fi.Name;
                ret.Field = fi;
                yield return ret;
            }
        }
    }
}
