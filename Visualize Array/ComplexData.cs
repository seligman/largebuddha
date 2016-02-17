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
            double bright = val  / Helpers.Limits[level, 1];
            bright = Math.Min(bright, 1.0);
            return bright;
        }


        ColorD GetPointTriBuddha(int x, int y)
        {
            Complex plot = AsComplex[x, y];

            double r = GetBrightBuddha(x, y, 0);
            double g = GetBrightBuddha(x, y, 1);
            double b = GetBrightBuddha(x, y, 2);

            return new ColorD(r, g, b);
        }
    }
}
