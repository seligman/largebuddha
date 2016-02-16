using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ScottsUtils;

namespace VisualizeArray
{
    static class Utils
    {
        public static Random Rnd = new Random();

        public static Color Dither(ColorD color)
        {
            double r = color.R;
            double g = color.G;
            double b = color.B;

            r = r * 255.0 + Rnd.NextDouble();
            g = g * 255.0 + Rnd.NextDouble();
            b = b * 255.0 + Rnd.NextDouble();

            return Color.FromArgb(
                Math.Min(255, Math.Max(0, (int)r)),
                Math.Min(255, Math.Max(0, (int)g)),
                Math.Min(255, Math.Max(0, (int)b)));
        }
    }
}
