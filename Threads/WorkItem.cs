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

        [MarshalAs(UnmanagedType.I4)]
        public WorkTypes WorkType;

        public WorkItem(WorkTypes type)
        {
            this.WorkType = type;
            this.X = 0;
            this.Y = 0;
        }

        public WorkItem(WorkTypes type, int x, int y)
        {
            this.WorkType = type;
            this.X = x;
            this.Y = y;
        }
    }
}
