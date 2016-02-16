using ShowProgress.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShowProgress
{
    public partial class ProgressForm : Form
    {
        public ProgressForm()
        {
            InitializeComponent();

            ClientSize = new Size(512 + m_progress.Left * 2, 512 + m_progress.Top * 3 + m_progress.Height);

            try
            {
                Icon = Resources.Mandelbrot;
            }
            catch { }
        }

        void ProgressForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            double value = (double)m_progress.Value;

            using (Bitmap current = CreateProgress(value))
            {
                g.DrawImage(current, m_progress.Left, m_progress.Height + m_progress.Top * 2);
            }

        }

        Bitmap CreateProgress(double progress)
        {
            Bitmap ret = new Bitmap(512, 512, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(ret))
            {
                if (m_buddha.Checked)
                {
                    g.DrawImage(Resources.Tile1, 0, 0);
                    g.DrawImage(Resources.Tile2, 256, 0);
                    g.DrawImage(Resources.Tile3, 0, 256);
                    g.DrawImage(Resources.Tile4, 256, 256);
                }
                else
                {
                    g.DrawImage(Resources.TileMand1, 0, 0);
                    g.DrawImage(Resources.TileMand2, 256, 0);
                    g.DrawImage(Resources.TileMand3, 0, 256);
                    g.DrawImage(Resources.TileMand4, 256, 256);
                }
            }

            BitmapData data = ret.LockBits(new Rectangle(0, 0, 512, 512), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int[] bits = new int[data.Stride / 4 * data.Height];
            Marshal.Copy(data.Scan0, bits, 0, bits.Length);

            long pixel = (long)((131072L * 131072L) * (progress / 100.0));
            Point target = new Point((int)((pixel / 131072L) / 256), (int)((pixel % 131072L) / 256));

            for (int x = target.X; x < 512; x++)
            {
                int start = 0;
                if (x == target.X)
                {
                    start = target.Y;
                }

                for (int y = start; y < 512; y++)
                {
                    int blend = ((((x / 8) + (y / 8)) % 2 == 1) ? 204 : 255);
                    Color c = Color.FromArgb(bits[x + y * data.Stride / 4]);
                    bits[x + y * data.Stride / 4] = Color.FromArgb(
                        (c.R + blend * 2) / 3,
                        (c.G + blend * 2) / 3,
                        (c.B + blend * 2) / 3).ToArgb();
                }
            }

            int size = 7;
            for (int x = target.X - (size + 1); x <= target.X + (size + 1); x++)
            {
                for (int y = target.Y - (size + 1); y <= target.Y + (size + 1); y++)
                {
                    if (x >= 0 && y >= 0 && x < 512 && y < 512)
                    {
                        double dist = Math.Sqrt((x - target.X) * (x - target.X) + (y - target.Y) * (y - target.Y));
                        if (dist <= size)
                        {
                            double mix = 1.0;
                            if (dist > size - 1)
                            {
                                mix = 1.0 - (dist - (size - 1));
                            }
                            Color c = Color.FromArgb(bits[x + y * data.Stride / 4]);
                            bits[x + y * data.Stride / 4] = Color.FromArgb(
                                (int)((((c.R + 255 * 4) / 5) * mix) + (c.R * (1.0 - mix))),
                                (int)((((c.G + 0 * 4) / 5) * mix) + (c.G * (1.0 - mix))),
                                (int)((((c.B + 0 * 4) / 5) * mix) + (c.B * (1.0 - mix)))).ToArgb();
                        }
                    }
                }

            }

            Marshal.Copy(bits, 0, data.Scan0, bits.Length);

            ret.UnlockBits(data);

            return ret;
        }

        void Buddha_CheckedChanged(object sender, EventArgs e)
        {
            Invalidate();
        }

        void Progress_ValueChanged(object sender, EventArgs e)
        {
            Invalidate();
        }
    }
}
