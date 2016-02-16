using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace MandelThreads
{
    static class Utilities
    {
        static public Rectangle GetRect(Bitmap bitmap)
        {
            return new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        }

        static public RenderModes IntToRender(int value)
        {
            switch (value)
            {
                case 1:
                    return RenderModes.Buddhabrot;
                case 2:
                    return RenderModes.AntiBuddhabrot;
                case 3:
                    return RenderModes.StarField;
                case 4:
                    return RenderModes.Mandelbrot;
                default:
                    throw new Exception();
            }
        }

        static public int RenderToInt(RenderModes value)
        {
            switch (value)
            {
                case RenderModes.Buddhabrot:
                    return 1;
                case RenderModes.AntiBuddhabrot:
                    return 2;
                case RenderModes.StarField:
                    return 3;
                case RenderModes.Mandelbrot:
                    return 4;
                default:
                    throw new Exception();
            }
        }
    }
}
