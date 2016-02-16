using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using ScottsUtils;
using System.IO;

namespace VisualizeArray
{
    public class BitBits
    {
        Bitmap m_bitmap;
        BitmapData m_data;
        UInt16[,] m_r;
        UInt16[,] m_g;
        UInt16[,] m_b;
        int m_width;
        int m_height;

        public BitBits(int width, int height)
        {
            Init(width, height, false);
        }

        public BitBits(int width, int height, bool use48)
        {
            Init(width, height, use48);
        }

        void Init(int width, int height, bool use48)
        {
            m_width = width;
            m_height = height;
            m_bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            m_data = m_bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            if (use48)
            {
                m_r = new UInt16[width, height];
                m_g = new UInt16[width, height];
                m_b = new UInt16[width, height];
            }
        }

        unsafe public void SaveBits(int x, int y, int width, int height, BinaryWriter bw)
        {
            for (int curY = y; curY < height + y; curY++)
            {
                for (int curX = x; curX < width + x; curX++)
                {
                    bw.Write(((int*)m_data.Scan0)[curX + curY * m_data.Stride / 4]);
                }
            }
        }

        public void SaveBits(int x, int y, int width, int height, string file)
        {
            using (Stream stream = File.OpenWrite(file))
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                SaveBits(x, y, width, height, bw);
            }
        }

        public void LoadBits(int x, int y, int width, int height, string file)
        {
            using (Stream stream = File.OpenRead(file))
            using (BinaryReader br = new BinaryReader(stream))
            {
                LoadBits(x, y, width, height, br);
            }
        }

        unsafe public void LoadBits(int x, int y, int width, int height, BinaryReader br)
        {
            for (int curY = y; curY < height + y; curY++)
            {
                for (int curX = x; curX < width + x; curX++)
                {
                    ((int*)m_data.Scan0)[curX + curY * m_data.Stride / 4] = br.ReadInt32();
                }
            }
        }

        unsafe public void Set(int x, int y, Color c)
        {
            if (x < 0 || x >= m_width || y < 0 || y >= m_height || m_r != null)
            {
                throw new Exception();
            }
            ((int*)m_data.Scan0)[x + y * m_data.Stride / 4] = c.ToArgb();
        }

        unsafe public void Set(int x, int y, int c)
        {
            if (x < 0 || x >= m_width || y < 0 || y >= m_height || m_r != null)
            {
                throw new Exception();
            }
            ((int*)m_data.Scan0)[x + y * m_data.Stride / 4] = c;
        }

        unsafe public void Set(int x, int y, Color c, ColorD for48)
        {
            if (x < 0 || x >= m_width || y < 0 || y >= m_height || m_r == null)
            {
                throw new Exception();
            }
            ((int*)m_data.Scan0)[x + y * m_data.Stride / 4] = c.ToArgb();

            m_r[x, y] = (UInt16)(Math.Max(0.0, Math.Min(1.0, for48.R)) * ((double)UInt16.MaxValue));
            m_g[x, y] = (UInt16)(Math.Max(0.0, Math.Min(1.0, for48.G)) * ((double)UInt16.MaxValue));
            m_b[x, y] = (UInt16)(Math.Max(0.0, Math.Min(1.0, for48.B)) * ((double)UInt16.MaxValue));
        }

        public void Save(string file)
        {
            Save(file, true);
        }

        public void Save(string file, bool close)
        {
            m_bitmap.UnlockBits(m_data);
            m_data = null;

            ImageCodecInfo pngCodec = ImageCodecInfo.GetImageEncoders().Where(codec => codec.FormatID.Equals(ImageFormat.Png.Guid)).FirstOrDefault();
            if (pngCodec != null)
            {
                EncoderParameters parameters = new EncoderParameters();
                parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.ColorDepth, 24);
                m_bitmap.Save(file, pngCodec, parameters);
            }
            else
            {
                throw new Exception();
            }

            if (m_r != null)
            {
                using (Stream stream = File.OpenWrite(file + ".rgb"))
                using (BinaryWriter bw = new BinaryWriter(stream))
                {
                    for (int y = 0; y < m_height; y++)
                    {
                        for (int x = 0; x < m_width; x++)
                        {
                            bw.Write(m_r[x, y]);
                            bw.Write(m_g[x, y]);
                            bw.Write(m_b[x, y]);
                        }
                    }
                }
            }

            if (close)
            {
                Close();
            }
            else
            {
                m_data = m_bitmap.LockBits(new Rectangle(0, 0, m_width, m_height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            }
        }

        public void Close()
        {
            if (m_data != null)
            {
                m_bitmap.UnlockBits(m_data);
                m_data = null;
            }

            m_bitmap.Dispose();
            m_bitmap = null;

            m_r = null;
            m_g = null;
            m_b = null;
        }
    }
}
