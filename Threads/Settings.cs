using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace MandelThreads
{
    static class Settings
    {
        //public static string SavedState = "SavedState.dat";

        public static int DrawSecs = GetDrawSecs(15, 2);
        public static int SaveSecs = 60 * 30;

        public static bool SaveAngleData = true;
        public static bool SaveTriLimits = false;
        public static int Width = 8192;
        public static int Height = 8192;
        public static int ViewOffX = 0;
        public static int ViewOffY = 0;
        public static int ViewWidth = 8192;
        public static int ViewHeight = 8192;
        public static int Iters = 500000;
        public static int Iters2 = 50000;
        public static int Iters3 = 5000;
        public static int Alias = 25;
        public static int Threads = 6;

        public static RenderModes Mode = RenderModes.Buddhabrot;

        static int GetDrawSecs(int normal, int debugger)
        {
            if (Debugger.IsAttached)
            {
                return debugger;
            }
            else
            {
                return normal;
            }
        }

        public static int ParseNum(string val)
        {
            return ScottsUtils.Equation.Equation<int>.Static.Evaluate(val);
        }

        public static bool Init()
        {
            if (File.Exists("Settings.txt"))
            {
                foreach (var line in File.ReadAllLines("Settings.txt"))
                {
                    if (!line.Trim().StartsWith("#"))
                    {
                        string[] split = line.Split('=');

                        switch (split[0].ToLower().Trim())
                        {
                            case "viewoffx":
                                ViewOffX = ParseNum(split[1].Trim());
                                break;
                            case "viewoffy":
                                ViewOffY = ParseNum(split[1].Trim());
                                break;
                            case "viewwidth":
                                ViewWidth = ParseNum(split[1].Trim());
                                break;
                            case "viewheight":
                                ViewHeight = ParseNum(split[1].Trim());
                                break;
                            case "width":
                                Width = ParseNum(split[1].Trim());
                                break;
                            case "height":
                                Height = ParseNum(split[1].Trim());
                                break;
                            case "iters":
                                Iters = ParseNum(split[1].Trim());
                                break;
                            case "iters2":
                                Iters2 = ParseNum(split[1].Trim());
                                break;
                            case "iters3":
                                Iters3 = ParseNum(split[1].Trim());
                                break;
                            case "alias":
                                Alias = ParseNum(split[1].Trim());
                                break;
                            case "threads":
                                Threads = ParseNum(split[1].Trim());
                                break;
                            case "mode":
                                Mode = Utilities.IntToRender(int.Parse(split[1].Trim()));
                                break;
                            case "saveangledata":
                                SaveAngleData = bool.Parse(split[1].Trim());
                                break;
                            case "savetrilimits":
                                SaveTriLimits = bool.Parse(split[1].Trim());
                                break;
                        }
                    }
                }

                return true;
            }
            else
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendFormat("Width = {0}\r\n", Width);
                sb.AppendFormat("Height = {0}\r\n", Height);

                sb.AppendFormat("ViewOffX = {0}\r\n", ViewOffX);
                sb.AppendFormat("ViewOffY = {0}\r\n", ViewOffY);
                sb.AppendFormat("ViewWidth = {0}\r\n", ViewWidth);
                sb.AppendFormat("ViewHeight = {0}\r\n", ViewHeight);

                sb.AppendFormat("Iters = {0}\r\n", Iters);
                sb.AppendFormat("Iters2 = {0}\r\n", Iters2);
                sb.AppendFormat("Iters3 = {0}\r\n", Iters3);
                sb.AppendFormat("Alias = {0}\r\n", Alias);
                sb.AppendFormat("Threads = {0}\r\n", Threads);

                sb.AppendFormat("SaveAngleData = {0}\r\n", SaveAngleData);
                sb.AppendFormat("SaveTriLimits = {0}\r\n", SaveTriLimits);

                sb.AppendFormat("Mode = {0}\r\n", Utilities.RenderToInt(Mode));
                sb.Append("# 1 = Buddhabrot\r\n");
                sb.Append("# 2 = Anti-Buddhabrot\r\n");
                sb.Append("# 3 = Black Mandelbrot\r\n");
                sb.Append("# 4 = Mandelbrot\r\n");

                File.WriteAllText("Settings.txt", sb.ToString());

                return false;
            }
        }
    }
}
