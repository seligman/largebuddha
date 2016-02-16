using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScottsUtils;
using System.Drawing;

namespace VisualizeArray
{
    abstract class AvgColor
    {
        public abstract void Reset();
        public abstract void Add(ColorD c);
        public abstract Color GetAvg();
        public abstract ColorD GetAvg48();

        public static AvgColor Factory()
        {
            if (Settings.Main_Shrink_UseGamma)
            {
                return new AvgColorGamma();
            }
            else
            {
                return new AvgColorSimple();
            }
        }
    }
}
