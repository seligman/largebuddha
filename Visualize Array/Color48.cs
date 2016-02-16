using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Mandelbrot
{
    public struct Color48
    {
        public UInt16 R;
        public UInt16 G;
        public UInt16 B;

        public static Color48 FromColor(Color color)
        {
            Color48 ret = new Color48();
            ret.R = (UInt16)(((UInt16)color.R) << 8);
            ret.G = (UInt16)(((UInt16)color.G) << 8);
            ret.B = (UInt16)(((UInt16)color.B) << 8);
            return ret;
        }

        public static Color48 FromDouble(double R, double G, double B)
        {
            Color48 ret = new Color48();
            ret.R = (UInt16)((R / 255.0) * 65535.0);
            ret.G = (UInt16)((G / 255.0) * 65535.0);
            ret.B = (UInt16)((B / 255.0) * 65535.0);
            return ret;
        }

        public static Color48 FromRGB(double R, double G, double B)
        {
            Color48 ret = new Color48();
            ret.R = (UInt16)R;
            ret.G = (UInt16)G;
            ret.B = (UInt16)B;
            return ret;
        }

        public static Color48 FromRGB(UInt16 R, UInt16 G, UInt16 B)
        {
            Color48 ret = new Color48();
            ret.R = R;
            ret.G = G;
            ret.B = B;
            return ret;
        }
    }
}
