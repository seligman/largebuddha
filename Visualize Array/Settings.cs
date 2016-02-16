using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace VisualizeArray
{
    class Settings
    {
        public static bool Test_Run = false;
        public static int Test_Width = 512;
        public static int Test_Height = 512;
        public static string Test_Source = @"C:\Path\To\File";

        public static bool Split_Run = false;
        public static string Split_Source = @"C:\Path\To\Split\Source.dat";
        public static string Split_Dest = @"C:\Path\To\Split\Out";
        public static int Split_Offset_X = 0;
        public static int Split_Offset_Y = 0;

        public static int Main_Tiles_PerSide = 1;
        public static int Main_Tiles_Levels = 1;
        public static int Main_Tiles_LevelLimit = 1000;
        public static bool Main_Action_DrawBuddha = true;
        public static bool Main_Action_DrawTriBuddha = true;
        public static bool Main_Action_DrawMandel = true;
        public static bool Main_Action_LargeImageOnly = false;
        public static bool Main_Action_DrawFullSizeTiles = true;
        public static string Main_SourceBuddhaDir = @"C:\Path\To\Buddha";
        public static string Main_SourceMandelDir = @"C:\Path\To\Mandel";
        public static string Main_DestDir = @"C:\Path\To\Out";
        public static bool Main_Shrink_UseGamma = false;
        public static bool Main_Shrink_AveragePixel = true;
        public static bool Main_Action_FakeTiles = false;

        public static bool Init()
        {
            return Init("Settings.txt");
        }

        public static bool Init(string file)
        {
            if (File.Exists(file))
            {
                foreach (var line in File.ReadAllLines(file))
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

                File.WriteAllText(file, sb.ToString());

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
