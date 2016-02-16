using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScottsUtils;
using System.Drawing;
using System.Runtime.InteropServices;

namespace MandelThreads
{
    [StructLayout(LayoutKind.Sequential)]
    struct MyPoint
    {
        public int X;
        public int Y;

        public MyPoint(int X, int Y)
        {
            this.X = X;
            this.Y = Y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MandelState
    {
        public int alias;
        public int pointCount;
        public int curAnti;
        public int width;
        public int height;
        public double centerX;
        public double centerY;
        public double size;
        public double rotate;

        public double addX;
        public double addY;
    }
}
