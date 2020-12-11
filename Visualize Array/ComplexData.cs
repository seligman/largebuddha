using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScottsUtils;
using System.IO;
using System.Diagnostics;
using VisualizeData;

namespace VisualizeArray
{
    public class ComplexData
    {
        public UInt64[,] Level = null;
        public UInt64[,] Level2 = null;
        public UInt64[,] Level3 = null;
        public double[,] Real = null;
        public double[,] Imaginary = null;
        public double[,] Other = null;

        public bool Loaded = false;

        public Helper AsComplex;

        public class Helper
        {
            ComplexData m_parent;

            public Helper(ComplexData parent)
            {
                m_parent = parent;
            }

            public Complex this[int x, int y]
            {
                get
                {
                    if (m_parent.Real == null)
                    {
                        return Complex.Zero;
                    }
                    else
                    {
                        return new Complex(
                            m_parent.Real[x, y],
                            m_parent.Imaginary[x, y]);
                    }
                }
            }
        }

        private ComplexData()
        {
            // Nothing to do
        }

        private void InternalLoadFile(string file, int width, int height)
        {
            Helpers.LoadSave(file, width, height, ref Level, ref Level2, ref Level3, ref Real, ref Imaginary, ref Other);

            Loaded = true;

            AsComplex = new Helper(this);
        }

        public static ComplexData LoadFile(string file, int width, int height)
        {
            ComplexData ret = new ComplexData();

            ret.InternalLoadFile(file, width, height);

            return ret;
        }

        public ComplexData(int x, int y, int level)
        {
            LoadData(x, y, level);

            AsComplex = new Helper(this);
        }

        public ComplexData(double[,] res, double[,] ims, double[,] other, UInt64[,] height, UInt64[,] height2, UInt64[,] height3)
        {
            Real = res;
            Imaginary = ims;
            Other = other;
            Level = height;
            Level2 = height2;
            Level3 = height3;

            Loaded = true;

            AsComplex = new Helper(this);
        }

        void LoadData(int x, int y, int level)
        {
            if (Helpers.SaveExists(Helpers.GetName(x, y, level)))
            {
                MyConsole.WriteLine("Loading " + x + " x " + y + " (" + level + ")");
                int size = 8192;
                Helpers.LoadSave(Helpers.GetName(x, y, level), size, size, ref Level, ref Level2, ref Level3, ref Real, ref Imaginary, ref Other);
                Loaded = true;
            }
        }

        public ColorD GetPoint(int x, int y)
        {
            if (Level == null)
            {
                return new ColorD(0, 0, 0);
            }
            else
            {
                switch (Helpers.Mode)
                {
                    case Helpers.Modes.Buddha:
                        return GetPointBuddha(x, y, 0);
                    case Helpers.Modes.Mandel:
                        return GetPointMandel(x, y);
                    case Helpers.Modes.TriBuddha:
                        return GetPointTriBuddha(x, y);
                    default:
                        throw new Exception();
                }
            }
        }

        ColorD GetPointMandel(int x, int y)
        {
            double r = Real[x, y];
            double g = Imaginary[x, y];
            double b = Other[x, y];

            double lev = Level[x, y];

            if (lev > 0)
            {
                r /= lev;
                g /= lev;
                b /= lev;
            }

            return new ColorD(r / 255.0, g / 255.0, b / 255.0);
        }

        ColorD GetPointBuddha(int x, int y, int level)
        {
            Complex plot = AsComplex[x, y];
            double hue = plot.Arg / Math.PI;

            double r = 0;
            double g = 0;
            double b = 0;

            hue *= 360.0;

            double abs = plot.Abs;
            double lum = abs / Helpers.Limits[level, 0];
            lum = lum * 0.95 + 0.05;
            lum = Math.Min(lum, 1.0);

            double sat = 1.0;
            if (abs >= Helpers.Limits[level, 0])
            {
                if (abs > Helpers.Limits[level, 1])
                {
                    sat = 0;
                }
                else
                {
                    sat = 1.0 - ((abs - Helpers.Limits[level, 0]) / (Helpers.Limits[level, 1] - Helpers.Limits[level, 0]));
                }
            }

            bool lower = true;
            if (lum > 0.2)
            {
                lower = false;
            }

            if (lower)
            {
                lum /= 0.2;
            }
            else
            {
                lum = (lum - 0.2) / (1.0 - 0.2);
            }

            lum = Math.Pow(lum, 0.85);

            if (lower)
            {
                lum *= 0.2;
            }
            else
            {
                lum = (lum * (1.0 - 0.2)) + 0.2;
            }

            Hsv.HsvToRgbPastel(hue, sat, lum, out r, out g, out b);

            return new ColorD(r, g, b);
        }

        double GetBrightBuddha(int x, int y, int level)
        {
            UInt64 val = 0;

            switch (level)
            {
                case 0:
                    val = Level[x, y];
                    break;
                case 1:
                    val = Level2[x, y];
                    break;
                case 2:
                    val = Level3[x, y];
                    break;
            }

            int ret = 0;
            int check = 32768 / 2;
            int i = 0;
            while (check > 0)
            {
                i += check;
                if (i == 32768 || val < Helpers.Limits2[level, i])
                {
                    if (check == 1)
                    {
                        check = 0;
                        ret = i - 1;
                    }
                    else
                    {
                        i -= check;
                        check /= 2;
                    }
                }
            }

            return Math.Pow(ret / 32768.0, 2.2);
        }

        public static void ComplexToPoint(Complex value, out int ptx, out int pty)
        {
            var state_centerX = -0.174563485365959;
            var state_centerY = -0.11673726730093;
            var state_size = 3.8;
            var state_rotate = 326.227841772776;
            var state_width = Settings.Test_Run ? Settings.Test_Width : Settings.Main_Tiles_PerSide * 8192;
            var state_height = Settings.Test_Run ? Settings.Test_Height : Settings.Main_Tiles_PerSide * 8192;

            var re = value.Real;
            var im = value.Imaginary;
            var rot = state_rotate;
            var width = state_width;
            var size = state_size;

            double rotx = re * Math.Cos(0.0174532925199432 * -rot) - im * Math.Sin(0.0174532925199432 * -rot);
            double roty = re * Math.Sin(0.0174532925199432 * -rot) + im * Math.Cos(0.0174532925199432 * -rot);

            double x = (width * (-state_centerX + 0.5 * size + rotx)) / size;
            double y = (0.5 * state_height * size - state_centerY * width + width * roty) / size;

            ptx = (int)Math.Round(x);
            pty = (int)Math.Round(y);
        }

        public static bool PointInPoly(int x, int y, int[] poly)
        {
            bool ret = false;
            int i = 0;
            int j = 0;
            int len = poly.Length;

            for (i = 0, j = len - 2; i < len; j = i, i += 2)
            {
                if ((((poly[i + 1] <= y) && (y < poly[j + 1])) || 
                    ((poly[j + 1] <= y) && (y < poly[i + 1]))) &&
                    (x < (poly[j] - poly[i]) * (y - poly[i + 1]) / (poly[j + 1] - poly[i + 1]) + poly[i]))
                {
                    ret = !ret;
                }
            }

            return ret;
        }

        public static bool PointInPoly(int x, int y)
        {
            if (s_cachedPolygon == null)
            {
                GetDisplayPolygon();
            }

            return PointInPoly(x, y, s_cachedPolygon);
        }

        static int[] s_cachedPolygon = null;

        static int[] GetDisplayPolygon()
        {
            if (s_cachedPolygon == null)
            {
                List<int> pts = new List<int>();

                AddPoint(pts, -2, -0.25);
                AddPoint(pts, -1, -0.75);
                AddPoint(pts, 0.5, -1.5);
                AddPoint(pts, 0.85, -1.0);
                AddPoint(pts, 0.85, 1.0);
                AddPoint(pts, 0.5, 1.5);
                AddPoint(pts, -1, 0.75);
                AddPoint(pts, -2, 0.25);

                s_cachedPolygon = pts.ToArray();
            }

            return s_cachedPolygon;
        }

        static void AddPoint(List<int> pts, double x, double y)
        {
            int ptx = 0;
            int pty = 0;

            ComplexData.ComplexToPoint(new Complex(x, y), out ptx, out pty);

            pts.Add(ptx);
            pts.Add(pty);
        }

        ColorD GetPointTriBuddha(int x, int y)
        {
            double r = GetBrightBuddha(x, y, 0);
            double g = GetBrightBuddha(x, y, 1);
            double b = GetBrightBuddha(x, y, 2);

#if false
            if (PointInPoly(x, y))
            {
                r = 1;
                g = 0;
            }
#endif

            return new ColorD(r, g, b);
        }
    }
}
