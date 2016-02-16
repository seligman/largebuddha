using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScottsUtils;
using System.Drawing;

namespace MandelThreads
{
    // Helper class to convert "screen" point to Complex and vice versa
    static class Engine
    {
        static public MandelState CreateState()
        {
            MandelState state = new MandelState();

            state.alias = Settings.Alias;
            state.curAnti = 0;
            state.width = Settings.Width;
            state.height = Settings.Height;

            state.centerX = -0.174563485365959;
            state.centerY = -0.11673726730093;
            state.size = 3.8;
            state.rotate = 326.227841772776;

            state.addX = 0;
            state.addY = 0;

            state.pointCount = state.alias * state.alias;

            return state;
        }

        static public void PointToComplex(Point value, MandelState state, out double real, out double imaginary)
        {
            double x = value.X;
            double y = value.Y;

            var statealias = state.alias;
            var statewidth = state.width;
            var statesize = state.size;
            var statePointX = state.curAnti % state.alias;
            var statePointY = state.curAnti / state.alias;

            double re = 2d * state.addX * statesize +
                -statealias * statesize * statewidth +
                2d * statesize * statePointX +
                2d * statealias * statesize * x;
            double im = 2d * state.addY * statesize +
                -statealias * state.height * statesize +
                2d * statesize * statePointY +
                2d * statealias * statesize * y;

            double temp = 2d * statealias * statewidth;

            re = temp * state.centerX + re;
            im = temp * state.centerY + im;

            temp = (state.rotate * 3.141592653589) / (180d);

            double re2 = Math.Cos(temp);
            double im2 = Math.Sin(temp);

            temp = (1d / (2d * statealias * statewidth));

            real = (re * re2 - im * im2) * temp;
            imaginary = (im * re2 + re * im2) * temp;
        }

        static public void ComplexToPoint(Complex value, MandelState state, out int ptx, out int pty)
        {
            var re = value.Real;
            var im = value.Imaginary;
            var rot = state.rotate;
            var width = state.width;
            var size = state.size;

            double rotx = re * Math.Cos(0.0174532925199432 * -rot) - im * Math.Sin(0.0174532925199432 * -rot);
            double roty = re * Math.Sin(0.0174532925199432 * -rot) + im * Math.Cos(0.0174532925199432 * -rot);

            double x = (width * (-state.centerX + 0.5 * size + rotx)) / size;
            double y = (0.5 * state.height * size - state.centerY * width + width * roty) / size;

            ptx = (int)Math.Round(x);
            pty = (int)Math.Round(y);
        }
    }
}
