using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace VisualizeArray
{
    class TopImageStep
    {
        int m_level;
        int m_perTile;
        int m_tileX;
        int m_tileY;
        BitBits m_bit;

        public TopImageStep(int level, int perTile, int tileX, int tileY)
        {
            m_level = level;
            m_perTile = perTile;
            m_tileX = tileX;
            m_tileY = tileY;

            m_bit = new BitBits(256, 256);
            m_bit.Save(Helpers.GetFinal(m_level, m_tileX, m_tileY), false);
        }

        public void RunStep(ComplexData data, int a, int b, string msg)
        {
            if (a >= m_tileX * m_perTile &&
                b >= m_tileY * m_perTile &&
                a < (m_tileX + 1) * m_perTile &&
                b < (m_tileY + 1) * m_perTile)
            {
                MyConsole.WriteLine(msg);

                VisualizeWorker.CopyRect(
                    new Rectangle(0, 0, 8192, 8192),
                    new Rectangle(
                        (a - m_tileX * m_perTile) * (256 / m_perTile),
                        (b - m_tileY * m_perTile) * (256 / m_perTile),
                        (256 / m_perTile),
                        (256 / m_perTile)),
                    data,
                    m_bit, false);

                m_bit.Save(Helpers.GetFinal(m_level, m_tileX, m_tileY), false);
            }
        }

        public void Finish()
        {
            m_bit.Save(Helpers.GetFinal(m_level, m_tileX, m_tileY));
        }
    }
}
