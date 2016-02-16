using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MandelThreads
{
    public enum WorkTypes
    {
        CalcPixel,
        DrawOutput,
        DrawOutputFinal,
        SaveAndFinish,
        SaveState,
        SaveStateClose,
        Pause,
        Sleep,
        Completed,
        End,
    }
}
