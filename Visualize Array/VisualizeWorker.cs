using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using ScottsUtils;
using VisualizeData;
using System.Diagnostics;
using Mandelbrot;
using Utils;

namespace VisualizeArray
{
    public partial class VisualizeWorker
    {
        static unsafe public void MainWorker()
        {
            if (Settings.Test_Run)
            {
                int w = Settings.Test_Width;
                int h = Settings.Test_Height;
                Helpers.Mode = Helpers.Modes.TriBuddha;
                // FindLimitTriBuddha(Settings.Test_Source, w, h);
                FindLimit(Settings.Test_Source, w, h);
                BitBits bit = new BitBits(w, h, true);
                ComplexData data = ComplexData.LoadFile(Settings.Test_Source, w, h);
                CopyRect48(new Rectangle(0, 0, w, h), new Rectangle(0, 0, w, h), data, bit);

                bit.Save(Settings.Test_Source + "_test_.png");

                Image image = Image.FromFile(Settings.Test_Source + "_test_.png");
                PreviewForm form = new PreviewForm(null, (Bitmap)image);
                Application.Run(form);
            }

            if (Settings.Split_Run)
            {
                Helpers.SplitImage(Settings.Split_Source, Settings.Split_Dest, Settings.Split_Offset_X, Settings.Split_Offset_Y);
            }

            if (Settings.Main_Action_DrawBuddha)
            {
                using (var indent = MyConsole.Indent("Buddhabrot"))
                {
                    Helpers.Mode = Helpers.Modes.Buddha;
                    FindLimit(null, 8192, 8192);
                    MainWorkerInternal();
                }
            }

            if (Settings.Main_Action_DrawTriBuddha)
            {
                using (var indent = MyConsole.Indent("TriBuddha"))
                {
                    Helpers.Mode = Helpers.Modes.TriBuddha;
                    FindLimit(null, 8192, 8192);
                    MainWorkerInternal();
                }
            }

            if (Settings.Main_Action_DrawMandel)
            {
                using (var indent = MyConsole.Indent("Mandelbrot"))
                {
                    Helpers.Mode = Helpers.Modes.Mandel;
                    MainWorkerInternal();
                }
            }
        }

        static void MainWorkerInternal()
        {
            using (var indent = MyConsole.Indent("Creating large image"))
            {
                CreateLargeImage();
            }

            if (Settings.Main_Action_FakeTiles)
            {
                int level = 0;
                int side = 1;
                using (StringFormat format = (StringFormat)StringFormat.GenericDefault.Clone())
                using (Font font = new Font("Tahoma", 15, FontStyle.Regular))
                {
                    format.LineAlignment = StringAlignment.Center;
                    format.Alignment = StringAlignment.Center;

                    while (level < Settings.Main_Tiles_Levels && level <= Settings.Main_Tiles_LevelLimit)
                    {
                        for (int a = 0; a < side; a++)
                        {
                            for (int b = 0; b < side; b++)
                            {
                                MyConsole.WriteLine(level + " - " + a + " x " + b);
                                string file = Helpers.GetFinal(level, a, b);

                                using (Bitmap bitmap = new Bitmap(256, 256, PixelFormat.Format24bppRgb))
                                {
                                    using (Graphics g = Graphics.FromImage(bitmap))
                                    {
                                        g.Clear(Color.White);
                                        g.DrawString(
                                            "Level: " + (level + 1) + "\n" +
                                            a + "x" + b,
                                            font,
                                            Brushes.Black,
                                            new Rectangle(0, 0, 256, 256), format);
                                    }
                                    bitmap.Save(file);
                                }
                            }
                        }

                        level++;
                        side *= 2;
                    }
                }
            }
            else if (!Settings.Main_Action_LargeImageOnly)
            {
                int dim = Settings.Main_Tiles_PerSide * 8192;
                int levels = 1;
                int temp = dim;
                while (temp >= 256)
                {
                    levels++;
                    temp /= 2;
                }

                if (Settings.Main_Tiles_Levels != levels)
                {
                    Console.WriteLine("I calced a different number of levels at " + levels);
                    return;
                }

                int tiles = 1;
                int perTile = Settings.Main_Tiles_PerSide;
                int targetLevel = 0;
                List<TopImageStep> steps = new List<TopImageStep>();
                for (int level = 0; level < levels; level++)
                {
                    foreach (var cur in CreateTopImages(level, tiles, perTile))
                    {
                        if (level < Settings.Main_Tiles_LevelLimit)
                        {
                            steps.Add(cur);
                        }
                    }

                    if (perTile == 2)
                    {
                        targetLevel = level + 1;
                        break;
                    }
                    else
                    {
                        perTile /= 2;
                    }
                    tiles *= 2;
                }

                List<Point> todo = new List<Point>();
                for (int x = 0; x < Settings.Main_Tiles_PerSide; x++)
                {
                    Add(todo, x, x);
                }
                for (int x = 0; x < Settings.Main_Tiles_PerSide; x++)
                {
                    Add(todo, x + 1, x);
                }
                for (int x = 0; x < Settings.Main_Tiles_PerSide; x++)
                {
                    Add(todo, x, 0);
                }
                for (int x = 0; x < Settings.Main_Tiles_PerSide; x++)
                {
                    Add(todo, Settings.Main_Tiles_PerSide - 1, x);
                }
                for (int x = 0; x < Settings.Main_Tiles_PerSide; x++)
                {
                    Add(todo, (Settings.Main_Tiles_PerSide - 1) - x, Settings.Main_Tiles_PerSide - 1);
                }
                for (int x = 0; x < Settings.Main_Tiles_PerSide; x++)
                {
                    Add(todo, 0, (Settings.Main_Tiles_PerSide - 1) - x);
                }
                for (int a = 0; a < Settings.Main_Tiles_PerSide; a++)
                {
                    for (int b = 0; b < Settings.Main_Tiles_PerSide; b++)
                    {
                        Add(todo, a, b);
                    }
                }

                foreach (var pt in todo)
                {
                    ComplexData data = new ComplexData(pt.X, pt.Y, 0);
                    int idx = 1;
                    foreach (var cur in steps)
                    {
                        cur.RunStep(data, pt.X, pt.Y, "Shrinking " + pt.X + " x " + pt.Y + ": " + idx + " / " + steps.Count);
                        idx++;
                    }

                    perTile = 1;
                    int size = 8192;
                    for (int cur = targetLevel; cur < levels; cur++)
                    {
                        if (cur < Settings.Main_Tiles_LevelLimit)
                        {
                            MyConsole.WriteLine("Level " + cur + " for " + pt.X + " x " + pt.Y);
                            CreateImages(pt.X, pt.Y, data, cur, size, perTile);
                        }

                        perTile *= 2;
                        size /= 2;
                    }

                    if (Settings.Main_Action_DrawFullSizeTiles)
                    {
                        BitBits bit = new BitBits(8192, 8192, true);
                        CopyRect48(new Rectangle(0, 0, 8192, 8192), new Rectangle(0, 0, 8192, 8192), data, bit);
                        bit.Save(Helpers.GetBig(pt.X, pt.Y));
                    }
                }

                foreach (var cur in steps)
                {
                    cur.Finish();
                }
            }
        }

        static void Add(List<Point> todo, int a, int b)
        {
            if (a >= 0 &&
                b >= 0 &&
                a < Settings.Main_Tiles_PerSide &&
                b < Settings.Main_Tiles_PerSide)
            {
                foreach (var cur in todo)
                {
                    if (cur.X == a && cur.Y == b)
                    {
                        return;
                    }
                }

                todo.Add(new Point(a, b));
            }
        }

        public static void CopyRect(Rectangle src, Rectangle dest, ComplexData from, BitBits to, bool use48)
        {
            if (from != null && from.Loaded)
            {
                if (src.Width > dest.Width)
                {
                    int xScale = src.Width / dest.Width;
                    int yScale = src.Height / dest.Height;

                    if (Settings.Main_Shrink_AveragePixel)
                    {
                        for (int x = 0; x < src.Width; x += xScale)
                        {
                            for (int y = 0; y < src.Height; y += yScale)
                            {
                                ColorD cd = AvgRange(from, x + src.X, y + src.Y, xScale);
                                Color c = Utils.Dither(cd);
                                if (use48)
                                {
                                    to.Set(x / xScale + dest.X, y / yScale + dest.Y, c, cd);
                                }
                                else
                                {
                                    to.Set(x / xScale + dest.X, y / yScale + dest.Y, c);
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int x = 0; x < src.Width; x += xScale)
                        {
                            for (int y = 0; y < src.Height; y += yScale)
                            {
                                ColorD cd = from.GetPoint(x + src.X, y + src.Y);
                                Color c = Utils.Dither(cd);
                                if (use48)
                                {
                                    to.Set(x / xScale + dest.X, y / yScale + dest.Y, c, cd);
                                }
                                else
                                {
                                    to.Set(x / xScale + dest.X, y / yScale + dest.Y, c);
                                }
                            }
                        }
                    }
                }
                else if (src.Width == dest.Width)
                {
                    for (int x = 0; x < src.Width; x++)
                    {
                        for (int y = 0; y < src.Height; y++)
                        {
                            ColorD cd = from.GetPoint(x + src.X, y + src.Y);
                            Color c = Utils.Dither(cd);
                            if (use48)
                            {
                                to.Set(x + dest.X, y + dest.Y, c, cd);
                            }
                            else
                            {
                                to.Set(x + dest.X, y + dest.Y, c);
                            }
                        }
                    }
                }
                else if (src.Width < dest.Width)
                {
                    int xScale = dest.Width / src.Width;
                    int yScale = dest.Height / src.Height;

                    for (int x = 0; x < src.Width; x++)
                    {
                        for (int y = 0; y < src.Height; y++)
                        {
                            ColorD cd = from.GetPoint(x + src.X, y + src.Y);
                            Color c = Utils.Dither(cd);

                            for (int xOff = 0; xOff < xScale; xOff++)
                            {
                                for (int yOff = 0; yOff < yScale; yOff++)
                                {
                                    if (use48)
                                    {
                                        to.Set(x * xScale + dest.X + xOff, y * yScale + dest.Y + yOff, c, cd);
                                    }
                                    else
                                    {
                                        to.Set(x * xScale + dest.X + xOff, y * yScale + dest.Y + yOff, c);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Color c1 = Color.FromArgb(204, 204, 204);
                Color c2 = Color.FromArgb(255, 255, 255);
                ColorD cd1 = (ColorD)c1;
                ColorD cd2 = (ColorD)c2;

                for (int x = 0; x < dest.Width; x++)
                {
                    for (int y = 0; y < dest.Height; y++)
                    {
                        Color temp;
                        ColorD tempd;

                        if ((((x / 8) % 2) == 0) ^ (((y / 8) % 2) == 0))
                        {
                            temp = c1;
                            tempd = cd1;
                        }
                        else
                        {
                            temp = c2;
                            tempd = cd2;
                        }

                        if (use48)
                        {
                            to.Set(x + dest.X, y + dest.Y, temp, tempd);
                        }
                        else
                        {
                            to.Set(x + dest.X, y + dest.Y, temp);
                        }
                    }
                }
            }
        }

        static void CopyRect48(Rectangle src, Rectangle dest, ComplexData from, BitBits to)
        {
            if (from.Loaded)
            {
                if (src.Width > dest.Width)
                {
                    int xScale = src.Width / dest.Width;
                    int yScale = src.Height / dest.Height;

                    for (int x = 0; x < src.Width; x += xScale)
                    {
                        for (int y = 0; y < src.Height; y += yScale)
                        {
                            ColorD c = AvgRange48(from, x + src.X, y + src.Y, xScale);
                            to.Set(x / xScale + dest.X, y / yScale + dest.Y, Utils.Dither(c), c);
                        }
                    }
                }
                else if (src.Width == dest.Width)
                {
                    for (int x = 0; x < src.Width; x++)
                    {
                        for (int y = 0; y < src.Height; y++)
                        {
                            ColorD cd = from.GetPoint(x + src.X, y + src.Y);
                            Color c = Utils.Dither(cd);
                            to.Set(x + dest.X, y + dest.Y, c, cd);
                        }
                    }
                }
                else if (src.Width < dest.Width)
                {
                    int xScale = dest.Width / src.Width;
                    int yScale = dest.Height / src.Height;

                    for (int x = 0; x < src.Width; x++)
                    {
                        for (int y = 0; y < src.Height; y++)
                        {
                            ColorD cd = from.GetPoint(x + src.X, y + src.Y);

                            for (int xOff = 0; xOff < xScale; xOff++)
                            {
                                for (int yOff = 0; yOff < yScale; yOff++)
                                {
                                    Color c = Utils.Dither(cd);
                                    to.Set(x * xScale + dest.X + xOff, y * yScale + dest.Y + yOff, c, cd);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Color c1 = Color.FromArgb(204, 204, 204);
                Color c2 = Color.FromArgb(255, 255, 255);

                for (int x = 0; x < dest.Width; x++)
                {
                    for (int y = 0; y < dest.Height; y++)
                    {
                        if ((((x / 8) % 2) == 0) ^ (((y / 8) % 2) == 0))
                        {
                            to.Set(x + dest.X, y + dest.Y, c1, c1);
                        }
                        else
                        {
                            to.Set(x + dest.X, y + dest.Y, c2, c2);
                        }
                    }
                }
            }
        }

        static void CreateImages(int a, int b, ComplexData data, int level, int size, int tileOff)
        {
            using (var indent = MyConsole.Indent("Creating level " + (level + 1) + " for " + a + " x " + b))
            {
                for (int tileX = 0; tileX < tileOff; tileX++)
                {
                    for (int tileY = 0; tileY < tileOff; tileY++)
                    {
                        MyConsole.WriteLine("Level " + (level + 1) + " for " + a + " x " + b + ": " + tileX + " x " + tileY);

                        Rectangle src = new Rectangle(tileX * size, tileY * size, size, size);
                        Rectangle dest = new Rectangle(0, 0, 256, 256);

                        BitBits bit = new BitBits(256, 256);

                        CopyRect(src, dest, data, bit, false);

                        bit.Save(Helpers.GetFinal(level, tileX + tileOff * a, tileY + tileOff * b));
                    }
                }
            }
        }

        static void CreateLargeImage()
        {
            BitBits bit = new BitBits(2048, 2048, false);

            int size = 2048 / Settings.Main_Tiles_PerSide;

            CopyRect(
                new Rectangle(0, 0, 2048, 2048),
                new Rectangle(0, 0, 2048, 2048),
                null, bit, false);

            for (int a = 0; a < Settings.Main_Tiles_PerSide; a++)
            {
                for (int b = 0; b < Settings.Main_Tiles_PerSide; b++)
                {
                    string bitsFile = Helpers.GetBig(a, b) + ".bits";
                    if (File.Exists(bitsFile))
                    {
                        MyConsole.WriteLine("Using cache for " + a + " x " + b + " (" + 0 + ")");
                        bit.LoadBits(a * size, b * size, size, size, bitsFile);
                    }
                    else
                    {
                        ComplexData data = new ComplexData(a, b, 0);

                        if (data.Loaded)
                        {
                            BitBits bitBig = null;

                            if (!File.Exists(Helpers.GetBig(a, b)))
                            {
                                bitBig = new BitBits(2048, 2048, false);
                            }

                            CopyRect(
                                new Rectangle(0, 0, 8192, 8192),
                                new Rectangle(a * size, b * size, size, size),
                                data,
                                bit, false);

                            bit.SaveBits(a * size, b * size, size, size, bitsFile);

                            if (bitBig != null)
                            {
                                CopyRect(
                                    new Rectangle(0, 0, 8192, 8192),
                                    new Rectangle(0, 0, 2048, 2048),
                                    data,
                                    bitBig, false);

                                bitBig.Save(Helpers.GetBig(a, b));
                            }

                            bit.Save(Helpers.GetBig(-1, -1), false);
                        }
                    }
                }
            }

            bit.Save(Helpers.GetBig(-1, -1));
        }

        static List<TopImageStep> CreateTopImages(int level, int tiles, int perTile)
        {
            List<TopImageStep> ret = new List<TopImageStep>();

            for (int tx = 0; tx < tiles; tx++)
            {
                for (int ty = 0; ty < tiles; ty++)
                {
                    ret.Add(new TopImageStep(level, perTile, tx, ty));
                }
            }

            return ret;
        }

        public static Color AvgRange(ComplexData data, int x, int y, int size)
        {
            AvgColor avg = AvgColor.Factory();

            for (int xOff = 0; xOff < size; xOff++)
            {
                for (int yOff = 0; yOff < size; yOff++)
                {
                    avg.Add(data.GetPoint(x + xOff, y + yOff));
                }
            }

            return avg.GetAvg();
        }

        public static ColorD AvgRange48(ComplexData data, int x, int y, int size)
        {
            AvgColor avg = AvgColor.Factory();

            for (int xOff = 0; xOff < size; xOff++)
            {
                for (int yOff = 0; yOff < size; yOff++)
                {
                    avg.Add(data.GetPoint(x + xOff, y + yOff));
                }
            }

            return avg.GetAvg48();
        }

        static string NormalizeFilename(string value)
        {
            if (value.Length > 3 && value[1] == ':')
            {
                value = value.Substring(2);
            }

            value = value.ToLower();

            return value;
        }

        static void FindLimit(string file, int w, int h)
        {
            string cache = "limits.cache";

            if (File.Exists(cache) && !Settings.Test_Run)
            {
                string[] lines = File.ReadAllLines(cache);
                int i = 0;
                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        Helpers.Limits[x, y] = double.Parse(lines[i]);
                        i++;
                    }
                }
                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 32768; y++)
                    {
                        Helpers.Limits2[x, y] = double.Parse(lines[i]);
                        i++;
                    }
                }
            }
            else
            {
                using (var indent = MyConsole.Indent("Finding limits"))
                {
                    Dictionary<long, long>[] counts = new Dictionary<long, long>[3];
                    long[] totalPixels = new long[3];
                    LoaderHelper helper = null;
                    int levels = 0;

                    for (int i = 0; i < 3; i++)
                    {
                        counts[i] = new Dictionary<long, long>();
                        totalPixels[i] = 0;
                    }

                    if (file == null)
                    {
                        helper = new LoaderHelper();
                    }

                    while (true)
                    {
                        double[,] res = null;
                        double[,] ims = null;
                        double[,] other = null;
                        UInt64[,] height = null;
                        UInt64[,] height2 = null;
                        UInt64[,] height3 = null;
                        int tileX = 0;
                        int tileY = 0;

                        ComplexData data = null;

                        if (helper != null)
                        {
                            data = helper.GetData(ref tileX, ref tileY, ref res, ref ims, ref other, ref height, ref height2, ref height3);
                        }
                        else
                        {
                            if (file != null)
                            {
                                data = ComplexData.LoadFile(file, w, h);

                                res = data.Real;
                                ims = data.Imaginary;
                                other = data.Other;
                                height = data.Level;
                                height2 = data.Level2;
                                height3 = data.Level3;

                                file = null;
                            }
                        }

                        if (data == null)
                        {
                            break;
                        }

                        if (height2 != null)
                        {
                            levels = 3;
                            for (int i = 0; i < 3; i++)
                            {
                                ulong[,] cur = null;
                                switch (i)
                                {
                                    case 0:
                                        cur = height;
                                        break;
                                    case 1:
                                        cur = height2;
                                        break;
                                    case 2:
                                        cur = height3;
                                        break;
                                }

                                for (int x = 0; x < w; x++)
                                {
                                    for (int y = 0; y < h; y++)
                                    {
                                        if (UsePixel(x, y, tileX, tileY))
                                        {
                                            totalPixels[i]++;

                                            long abs = (long)cur[x, y];
                                            long temp = 0;
                                            if (counts[i].TryGetValue(abs, out temp))
                                            {
                                                counts[i][abs] = temp + 1;
                                            }
                                            else
                                            {
                                                counts[i].Add(abs, 1);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            levels = 1;
                            for (int x = 0; x < w; x++)
                            {
                                for (int y = 0; y < h; y++)
                                {
                                    totalPixels[0]++;

                                    Complex cur = new Complex(res[x, y], ims[x, y]);

                                    long abs = (long)cur.Abs;
                                    if (counts[0].ContainsKey(abs))
                                    {
                                        counts[0][abs]++;
                                    }
                                    else
                                    {
                                        counts[0].Add(abs, 1);
                                    }
                                }
                            }
                        }
                    }

                    for (int i = 0; i < levels; i++)
                    {
                        List<long> keys = new List<long>(counts[i].Keys);
                        keys.Sort();

                        long curPixels = 0;
                        long limit1 = -1;
                        long limit2 = -1;
                        long limit3 = -1;
                        foreach (long key in keys)
                        {
                            curPixels += counts[i][key];
                            double perc = ((double)curPixels) / ((double)totalPixels[i]);

                            if (limit1 == -1)
                            {
                                if (perc >= 0.995)
                                {
                                    limit1 = key;
                                }
                            }

                            if (limit2 == -1)
                            {
                                if (perc >= 0.9999)
                                {
                                    limit2 = key;
                                }
                            }

                            if (limit3 == -1)
                            {
                                if (perc >= 0.9999)
                                {
                                    limit3 = key;
                                }
                            }
                        }

                        double tot = (0.9) * (((double)totalPixels[i]) * 1.0);
                        double stepPixels = (1.0 / (32768.0)) * (tot);
                        double checkPixels = stepPixels + (totalPixels[i] - tot);
                        curPixels = 0;
                        int x = 0;

                        foreach (long key in keys)
                        {
                            curPixels += counts[i][key];

                            while (curPixels >= checkPixels)
                            {
                                Helpers.Limits2[i, x] = key;
                                checkPixels += stepPixels;
                                x++;
                                if (x == 32768)
                                {
                                    break;
                                }
                            }
                            if (x == 32768)
                            {
                                break;
                            }
                        }

                        Helpers.Limits[i, 0] = limit1;
                        Helpers.Limits[i, 1] = limit2;
                        Helpers.Limits[i, 2] = limit3;
                    }

                    StringBuilder sb2 = new StringBuilder();
                    for (int x = 0; x < 3; x++)
                    {
                        for (int y = 0; y < 3; y++)
                        {
                            sb2.AppendLine(Helpers.Limits[x, y].ToString());
                        }
                    }
                    for (int x = 0; x < 3; x++)
                    {
                        for (int y = 0; y < 32768; y++)
                        {
                            sb2.AppendLine(Helpers.Limits2[x, y].ToString());
                        }
                    }
                    File.WriteAllText(cache, sb2.ToString());
                }
            }
        }

        static bool UsePixel(int x, int y, int tileX, int tileY)
        {
            if (ComplexData.PointInPoly(x + tileX * 8192, y + tileY * 8192))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
