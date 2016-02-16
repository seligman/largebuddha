using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualizeData
{
    public static class Hsv
    {
        static double HsvMaxPv = Math.Sqrt(0.691 + 0.241);
        static double HsvMinPv = Math.Sqrt(0.068);
        static Random DitherRand = new Random();

        //cimg -size 8000x8000 -depth 16 image0.rgb image0.tiff
        public static UInt16 SmoothUInt16(double val)
        {
            if (double.IsNaN(val) || double.IsNegativeInfinity(val))
            {
                return 0;
            }
            else if (double.IsPositiveInfinity(val) || double.IsInfinity(val))
            {
                return UInt16.MaxValue;
            }

            val *= UInt16.MaxValue;

            val = Math.Round(val);

            if (val <= 0)
            {
                return 0;
            }
            else if (val >= UInt16.MaxValue)
            {
                return UInt16.MaxValue;
            }
            else
            {
                return (UInt16)val;
            }
        }

        public static int DitherColor(double val)
        {
            if (double.IsNaN(val) || double.IsNegativeInfinity(val))
            {
                return 0;
            }
            else if (double.IsPositiveInfinity(val) || double.IsInfinity(val))
            {
                return 255;
            }

            val *= 255.0;
            val += DitherRand.NextDouble();
            val = Math.Floor(val);

            if (val <= 0)
            {
                return 0;
            }
            else if (val >= 255)
            {
                return 255;
            }

            return (int)val;
        }

        public static void HsvToRgbPastel(double h, double S, double V, out double r, out double g, out double b)
        {
            double perc = h / 360.0;

            r = ((Math.Sin(perc * Math.PI * 2 + Math.PI / 2) + 1) / 2) * 104 + 69;
            g = ((Math.Sin((perc + 0.5) * Math.PI * 2 + Math.PI / 2) + 1) / 2) * 41 + 113;
            b = ((Math.Sin((perc + 0.3) * Math.PI * 2 + Math.PI / 2) + 1) / 2) * 99 + 71;

            r /= 255.0;
            g /= 255.0;
            b /= 255.0;

            r = r * S + (1 - S);
            g = g * S + (1 - S);
            b = b * S + (1 - S);

            r = r * V;
            g = g * V;
            b = b * V;
        }

        public static void HsvToRgbNormal(double h, double S, double V, out double r, out double g, out double b)
        {
            HsvToRgb(h, 1, 1, out r, out g, out b);

            double pv = Math.Sqrt(r * r * 0.241 + g * g * 0.691 + b * b * 0.068);

            r = (r / (pv / HsvMaxPv)) * HsvMinPv;
            g = (g / (pv / HsvMaxPv)) * HsvMinPv;
            b = (b / (pv / HsvMaxPv)) * HsvMinPv;

            r = r * S + (1 - S);
            g = g * S + (1 - S);
            b = b * S + (1 - S);

            r = r * V;
            g = g * V;
            b = b * V;
        }

        public static void HsvToRgb(double h, double S, double V, out double r, out double g, out double b)
        {
            r = 0;
            g = 0;
            b = 0;

            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            {
                R = G = B = 0;
            }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));

                switch (i)
                {
                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;
                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;
                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;
                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;
                    default:
                        R = G = B = V;
                        break;
                }
            }

            r = Math.Min(1.0, Math.Max(0.0, R));
            g = Math.Min(1.0, Math.Max(0.0, G));
            b = Math.Min(1.0, Math.Max(0.0, B));
        }
    }
}
