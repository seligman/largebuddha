using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace MandelThreads
{
    [StructLayout(LayoutKind.Sequential)]
    public struct WorkItem
    {
        public int X;
        public int Y;

        public double Perc;

        [MarshalAs(UnmanagedType.I4)]
        public WorkTypes WorkType;

        public WorkItem(WorkTypes type)
        {
            this.WorkType = type;
            this.X = 0;
            this.Y = 0;
            this.Perc = 0;
        }

        public WorkItem(WorkTypes type, double perc)
        {
            this.WorkType = type;
            this.X = 0;
            this.Y = 0;
            this.Perc = perc;
        }

        public WorkItem(WorkTypes type, int x, int y)
        {
            this.WorkType = type;
            this.X = x;
            this.Y = y;
            this.Perc = 0;
        }
    }
}
