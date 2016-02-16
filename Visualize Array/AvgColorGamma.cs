using ScottsUtils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace VisualizeArray
{
    class AvgColorGamma : AvgColor
    {
        double m_r = 0;
        double m_g = 0;
        double m_b = 0;

        int m_count = 0;

        public override void Reset()
        {
            m_r = 0;
            m_g = 0;
            m_b = 0;

            m_count = 0;
        }

        public override void Add(ColorD c)
        {
            c = c.FromGamma(2.2);

            m_r += c.R;
            m_g += c.G;
            m_b += c.B;

            m_count++;
        }

        public override Color GetAvg()
        {
            if (m_count == 0)
            {
                return Color.FromArgb(0, 0, 0);
            }
            else
            {
                ColorD ret = new ColorD(m_r / ((double)m_count), m_g / ((double)m_count), m_b / ((double)m_count));
                ret = ret.ToGamma(2.2);
                return Utils.Dither(ret);
            }
        }

        public override ColorD GetAvg48()
        {
            if (m_count == 0)
            {
                return new ColorD();
            }
            else
            {
                ColorD ret = new ColorD(m_r / ((double)m_count), m_g / ((double)m_count), m_b / ((double)m_count));
                ret = ret.ToGamma(2.2);
                return ret;
            }
        }
    }
}
