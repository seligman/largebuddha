using System;
#if SILVERLIGHT
#if FAKEGDI
using SilverGraphics;
#else
using System.Windows;
#endif
#else
using System.Drawing;
#endif
using System.Diagnostics;
#if XNA
using Xna = Microsoft.Xna.Framework;
#endif

namespace ScottsUtils
{
#if !SILVERLIGHT
    [Serializable]
#endif
    public struct Complex
    {
        public double Real;
        public double Imaginary;
        public object Tag;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public double X
        {
            get
            {
                return Real;
            }
            set
            {
                Real = value;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public double Y
        {
            get
            {
                return Imaginary;
            }
            set
            {
                Imaginary = value;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsReal
        {
            get
            {
                if (Imaginary == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsImaginary
        {
            get
            {
                if (Real == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsValid
        {
            get
            {
                if (double.IsInfinity(Real) ||
                    double.IsNaN(Real) ||
                    double.IsInfinity(Imaginary) ||
                    double.IsNaN(Imaginary))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public Complex(double Real, double Imaginary)
        {
            this.Real = Real;
            this.Imaginary = Imaginary;
            this.Tag = null;
        }

        public Complex(Complex other)
        {
            this.Real = other.Real;
            this.Imaginary = other.Imaginary;
            this.Tag = null;
        }

        public static Complex WithTag(Complex value, object tag)
        {
            Complex ret = new Complex();
            ret.Real = value.Real;
            ret.Imaginary = value.Imaginary;
            ret.Tag = tag;
            return ret;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Complex Conjugate
        {
            get
            {
                return new Complex(Real, -Imaginary);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Complex Sqrt
        {
            get
            {
                double abs = Abs;

                Complex ret = new Complex(
                    Math.Sqrt(abs + Real),
                    Math.Abs(Math.Sqrt(abs - Real)));

                ret *= new Complex(1.0 / Math.Sqrt(2.0), 0.0);

                return ret;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Complex Sin
        {
            get
            {
                return new Complex(
                    Math.Sin(Real) * Math.Cosh(Imaginary),
                    Math.Cos(Real) * Math.Sinh(Imaginary));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Complex Tan
        {
            get
            {
                double rsd = Real * 2.0;
                double isd = Imaginary * 2.0;

                return new Complex(
                    (Math.Sin(rsd)) / (Math.Cos(rsd) + Math.Cosh(isd)),
                    (Math.Sinh(isd)) / (Math.Cos(rsd) + Math.Cosh(isd)));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Complex Cos
        {
            get
            {
                return new Complex(
                    Math.Cos(Real) * Math.Cosh(Imaginary),
                    Math.Sin(Real) * Math.Sinh(Imaginary));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Complex Cosh
        {
            get
            {
                Complex c1 = Exp;
                Complex c2 = (new Complex(-Real, -Imaginary)).Exp;

                return new Complex(
                    0.5 * (c1.Real + c2.Real),
                    0.5 * (c1.Imaginary + c2.Imaginary));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Complex Sinh
        {
            get
            {
                Complex c1 = Exp;
                Complex c2 = (new Complex(-Real, -Imaginary)).Exp;

                return new Complex(
                    0.5 * (c1.Real - c2.Real),
                    0.5 * (c1.Imaginary - c2.Imaginary));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Complex Tanh
        {
            get
            {
                return Sinh / Cosh;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Complex Sgn
        {
            get
            {
                double div = Real * Real + Imaginary * Imaginary;
                if (div == 0)
                {
                    return Zero;
                }
                else
                {
                    return this / new Complex(Math.Sqrt(div), 0.0);
                }
            }
        }

        public static Complex Power(Complex value, Complex exp)
        {
            if (value.Imaginary == 0 && value.Real == 0)
            {
                return 0;
            }
            else
            {
                double r = value.Abs;
                double t = value.Argument;
                double c = exp.Real;
                double d = exp.Imaginary;

                return new Complex(
                    Math.Pow(r, c) * Math.Exp(-d * t) * Math.Cos(c * t + d * Math.Log(r)),
                    Math.Pow(r, c) * Math.Exp(-d * t) * Math.Sin(c * t + d * Math.Log(r)));
            }
        }

        public static Complex Power(Complex value, double exp)
        {
            if (value.Imaginary == 0 && 0 <= value.Real)
            {
                return Math.Pow(value.Real, exp);
            }
            else
            {
                return (value.Log * exp).Exp;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Complex Log
        {
            get
            {
                return new Complex(Math.Log(Modulus), Argument);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public double Modulus
        {
            get
            {
                return Math.Sqrt(Real * Real + Imaginary * Imaginary);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public double Argument
        {
            get
            {
                return Math.Atan2(Imaginary, Real);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Complex Exp
        {
            get
            {
                double value = Math.Exp(Real);

                return new Complex(
                   value * Math.Cos(Imaginary),
                   value * Math.Sin(Imaginary));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Complex AbsOfParts
        {
            get
            {
                return new Complex(Math.Abs(Real), Math.Abs(Imaginary));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public double Arg
        {
            get
            {
                return Sgn.Log.Imaginary;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public double Abs
        {
            get
            {
                return Math.Sqrt(Real * Real + Imaginary * Imaginary);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public double AbsSquared
        {
            get
            {
                return Real * Real + Imaginary * Imaginary;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public static Complex I
        {
            get
            {
                return new Complex(0, 1);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public static Complex NegI
        {
            get
            {
                return new Complex(0, -1);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public static Complex Zero
        {
            get
            {
                return new Complex(0, 0);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public static Complex One
        {
            get
            {
                return new Complex(1, 0);
            }
        }

        public static implicit operator Complex(double d)
        {
            return new Complex(d, 0.0);
        }

#if SILVERLIGHT
#if !FAKEGDI
        public static implicit operator Point(Complex c)
        {
            return new Point(
                c.Real,
                c.Imaginary);
        }
#endif
#else
        public static implicit operator Point(Complex c)
        {
            return new Point(
                (int)c.Real,
                (int)c.Imaginary);
        }
#endif

#if !SILVERLIGHT
        public static implicit operator PointF(Complex c)
        {
            return new PointF(
                (float)c.Real,
                (float)c.Imaginary);
        }

        public static implicit operator SizeF(Complex c)
        {
            return new SizeF(
                (float)c.Real,
                (float)c.Imaginary);
        }

        public static implicit operator Size(Complex c)
        {
            return new Size(
                (int)c.Real,
                (int)c.Imaginary);
        }
#endif

#if XNA
        public static implicit operator Xna.Vector2(Complex c)
        {
            return new Xna.Vector2(
                (float)c.Real,
                (float)c.Imaginary);
        }
#endif

#if !FAKEGDI
        public static implicit operator Complex(Point p)
        {
            return new Complex((double)p.X, (double)p.Y);
        }
#endif

#if !SILVERLIGHT
        public static implicit operator Complex(PointF p)
        {
            return new Complex((double)p.X, (double)p.Y);
        }

        public static implicit operator Complex(SizeF p)
        {
            return new Complex((double)p.Width, (double)p.Height);
        }
#endif

        public static implicit operator Complex(Size p)
        {
            return new Complex((double)p.Width, (double)p.Height);
        }
#if XNA
        public static implicit operator Complex(Xna.Vector2 p)
        {
            return new Complex((double)p.X, (double)p.Y);
        }
#endif

#if !XNA
#if !SILVERLIGHT || FAKEGDI
        public static RectangleF CreateRect(Complex center, double size)
        {
            return new RectangleF(
                center - new Complex(size / 2, size / 2),
                new Complex(size, size));
        }

        public RectangleF CreateRect(double size)
        {
            return CreateRect(this, size);
        }

        public static RectangleF CreateRect(Complex center, Complex size)
        {
            return new RectangleF(
                center - size / 2,
                size);
        }

        public RectangleF CreateRect(Complex size)
        {
            return CreateRect(this, size);
        }
#endif
#endif

        public static Complex operator -(Complex c1)
        {
            return new Complex(
                -c1.Real,
                -c1.Imaginary);
        }

        public static Complex operator +(Complex c1, Complex c2)
        {
            return new Complex(
                c1.Real + c2.Real,
                c1.Imaginary + c2.Imaginary);
        }

        public static Complex operator +(Complex c1, double d2)
        {
            return new Complex(
                c1.Real + d2,
                c1.Imaginary);
        }

        public static Complex operator +(double d1, Complex c2)
        {
            return new Complex(
                d1 + c2.Real,
                c2.Imaginary);
        }

        public static Complex operator -(Complex c1, Complex c2)
        {
            return new Complex(
                c1.Real - c2.Real,
                c1.Imaginary - c2.Imaginary);
        }

        public static Complex operator -(Complex c1, double d2)
        {
            return new Complex(
                c1.Real - d2,
                c1.Imaginary - 0.0);
        }

        public static Complex operator -(double d1, Complex c2)
        {
            return new Complex(
                d1 - c2.Real,
                0.0 - c2.Imaginary);
        }

        public static Complex operator *(Complex c1, Complex c2)
        {
            return new Complex(
                c1.Real * c2.Real - c1.Imaginary * c2.Imaginary,
                c1.Imaginary * c2.Real + c1.Real * c2.Imaginary);
        }

        public static Complex operator *(double d1, Complex c2)
        {
            return new Complex(
                d1 * c2.Real,
                d1 * c2.Imaginary);
        }

        public static Complex operator *(Complex c1, double d2)
        {
            return new Complex(
                c1.Real * d2,
                c1.Imaginary * d2);
        }

        public static Complex operator /(Complex c1, Complex c2)
        {
            return new Complex(
                (c1.Real * c2.Real + c1.Imaginary * c2.Imaginary) /
                (c2.Real * c2.Real + c2.Imaginary * c2.Imaginary),
                (c1.Imaginary * c2.Real - c1.Real * c2.Imaginary) /
                (c2.Real * c2.Real + c2.Imaginary * c2.Imaginary));
        }

        public static Complex operator /(Complex c1, double d2)
        {
            return new Complex(
                (c1.Real * d2) /
                (d2 * d2),
                (c1.Imaginary * d2) /
                (d2 * d2));
        }

        public static Complex operator /(double d1, Complex c2)
        {
            return new Complex(
                (d1 * c2.Real) /
                (c2.Real * c2.Real + c2.Imaginary * c2.Imaginary),
                (0.0 - d1 * c2.Imaginary) /
                (c2.Real * c2.Real + c2.Imaginary * c2.Imaginary));
        }

        public static bool operator >(Complex c1, double d2)
        {
            return c1.Imaginary * c1.Imaginary +
                c1.Real * c1.Real >
                d2 * d2;
        }

        public static bool operator <(Complex c1, double d2)
        {
            return c1.Imaginary * c1.Imaginary +
                c1.Real * c1.Real <
                d2 * d2;
        }

        public static bool operator >=(Complex c1, double d2)
        {
            return c1.Imaginary * c1.Imaginary +
                c1.Real * c1.Real >=
                d2 * d2;
        }

        public static bool operator <=(Complex c1, double d2)
        {
            return c1.Imaginary * c1.Imaginary +
                c1.Real * c1.Real <=
                d2 * d2;
        }

        public static bool operator ==(Complex c1, Complex c2)
        {
            return (c1.Imaginary == c2.Imaginary) &&
                (c1.Real == c2.Real);
        }

        public static bool operator !=(Complex c1, Complex c2)
        {
            return (c1.Imaginary != c2.Imaginary) ||
                (c1.Real != c2.Real);
        }

        public static double Distance(Complex c1, Complex c2)
        {
            return Math.Sqrt((c1.Real - c2.Real) *
                (c1.Real - c2.Real) +
                (c1.Imaginary - c2.Imaginary) *
                (c1.Imaginary - c2.Imaginary));
        }

        public static double DistanceSquared(Complex c1, Complex c2)
        {
            return (c1.Real - c2.Real) *
                (c1.Real - c2.Real) +
                (c1.Imaginary - c2.Imaginary) *
                (c1.Imaginary - c2.Imaginary);
        }

        public static double DegToRad(double value)
        {
            return value * (3.141592653589 / 180.0);
        }

        public static double RadToDeg(double value)
        {
            return value * (180.0 / 3.141592653589);
        }

        public static bool TryParse(string value, out Complex result)
        {
            result = Complex.Zero;

            string[] split = value.Split(',');

            if (split.Length == 1)
            {
                double real = 0;
                if (double.TryParse(split[0], out real))
                {
                    result = new Complex(real, 0);
                    return true;
                }
            }
            else if (split.Length == 2)
            {
                double real = 0;
                double img = 0;

                if (double.TryParse(split[0], out real))
                {
                    if (double.TryParse(split[1], out img))
                    {
                        result = new Complex(real, img);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Calculates the angle. 
        /// 0 = Bottom, 
        /// 45 = Bottom Right, 
        /// 90 = Right, 
        /// 135 = Top Right, 
        /// 180 = Top, 
        /// 225 = Top Left, 
        /// 270 = Left, 
        /// 315 = Bottom Left
        /// </summary>
        /// <param name="center">The center point</param>
        /// <param name="outside">The outside point</param>
        /// <returns>The calculated angle in degrees</returns>
        public static double CalcAngle(Complex center, Complex outside)
        {
            double xa = (outside.X - center.X);
            double ya = (outside.Y - center.Y);

            double ratio = xa / Math.Sqrt(ya * ya + xa * xa);
            double asin = 0d;

            if (double.IsInfinity(ratio) || double.IsNaN(ratio))
            {
                ratio = 0.0;
            }

            if (Math.Abs(ratio) == 1)
            {
                asin = 90d * ratio;
            }
            else
            {
                asin = Math.Atan(ratio /
                    Math.Sqrt(-ratio * ratio + 1d)) * (180d / 3.141592653589);
            }

            if (ya < 0)
            {
                asin = 90d - asin + 90d;
            }
            else if (xa < 0)
            {
                asin = asin + 360d;
            }

            return asin;
        }

        public static Complex GetIntersection(Complex Line1Pt1, Complex Line1Pt2, Complex Line2Pt1, Complex Line2Pt2)
        {
            double d = 
                (Line1Pt1.X - Line1Pt2.X) * 
                (Line2Pt1.Y - Line2Pt2.Y) - 
                (Line1Pt1.Y - Line1Pt2.Y) * 
                (Line2Pt1.X - Line2Pt2.X);

            if (d == 0)
            {
                return Complex.Zero;
            }

            return new Complex(
                ((Line2Pt1.X - Line2Pt2.X) * (Line1Pt1.X * Line1Pt2.Y - Line1Pt1.Y * Line1Pt2.X) - 
                (Line1Pt1.X - Line1Pt2.X) * (Line2Pt1.X * Line2Pt2.Y - Line2Pt1.Y * Line2Pt2.X)) / d,
                ((Line2Pt1.Y - Line2Pt2.Y) * (Line1Pt1.X * Line1Pt2.Y - Line1Pt1.Y * Line1Pt2.X) - 
                (Line1Pt1.Y - Line1Pt2.Y) * (Line2Pt1.X * Line2Pt2.Y - Line2Pt1.Y * Line2Pt2.X)) / d);
        }

        /// <summary>
        /// Rotate one point around another point.
        /// 0 = Bottom, 
        /// 45 = Bottom Right, 
        /// 90 = Right, 
        /// 135 = Top Right, 
        /// 180 = Top, 
        /// 225 = Top Left, 
        /// 270 = Left, 
        /// 315 = Bottom Left
        /// </summary>
        /// <param name="point">The point to rotate</param>
        /// <param name="center">The center point</param>
        /// <param name="angle">Angle in degrees of rotation</param>
        /// <returns>The newly rotated point</returns>
        public static Complex Rotate(Complex point, Complex center, double angle)
        {
            return 
                RotatePoint(
                    (point - center).Abs, 
                    CalcAngle(center, point) + angle) 
                + center;
        }

        /// <summary>
        /// Calculates a new point rotated around Complex.Zero
        /// 0 = Bottom, 
        /// 45 = Bottom Right, 
        /// 90 = Right, 
        /// 135 = Top Right, 
        /// 180 = Top, 
        /// 225 = Top Left, 
        /// 270 = Left, 
        /// 315 = Bottom Left
        /// </summary>
        /// <param name="distance">The distance from 0</param>
        /// <param name="angle">The angle in degrees to rotate</param>
        /// <returns>The rotated point</returns>
        public static Complex RotatePoint(double distance, double angle)
        {
            return new Complex(
                distance * Math.Sin(3.141592653589 / 180d * angle),
                distance * Math.Cos(3.141592653589 / 180d * angle));
        }

        public override bool Equals(object obj)
        {
            return ((Complex)obj) == this;
        }

        public override int GetHashCode()
        {
            return Real.GetHashCode() ^ Imaginary.GetHashCode();
        }

        public override string ToString()
        {
            if (Imaginary < 0)
            {
                return string.Format("{0} - {1}i", 
                    Real, Math.Abs(Imaginary));
            }
            else
            {
                return string.Format("{0} + {1}i", 
                    Real, Imaginary);
            }
        }
    }
}
