using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using Utils;

namespace VisualizeArray
{
    public static class Helpers
    {
        public static double[,] Limits = new double[3, 3] { 
        { double.MinValue, double.MinValue, double.MinValue }, 
        { double.MinValue, double.MinValue, double.MinValue }, 
        { double.MinValue, double.MinValue, double.MinValue } };

        public static double[,] Limits2 = new double[3, 32768];

        public static Modes Mode = Modes.Invalid;

        public enum Modes
        {
            Buddha,
            TriBuddha,
            Mandel,
            Invalid,
        }

        static Dictionary<string, bool> s_createdDirs = new Dictionary<string, bool>();

        static void CreateDir(string dir)
        {
            if (!s_createdDirs.ContainsKey(dir))
            {
                Directory.CreateDirectory(dir);
                s_createdDirs.Add(dir, true);
            }
        }

        public static string GetFinal(int level, int x, int y)
        {
            string ret = null;
            int type = 0;

            ret = Path.Combine(Settings.Main_DestDir, "tiles");

            switch (Mode)
            {
                case Modes.Buddha:
                    type = 0;
                    break;
                case Modes.Mandel:
                    type = 1;
                    break;
                case Modes.TriBuddha:
                    type = 2;
                    break;
                default:
                    throw new Exception();
            }

            ret = Path.Combine(ret, TilePos.GetTilePos(type, level, x, y, Settings.Main_Tiles_Levels - 1));
            ret += ".png";
            CreateDir(Path.GetDirectoryName(ret));

            return ret;
        }

        public static string GetBig(int x, int y, bool huge)
        {
            string ret = null;
            if (x == -1 || y == -1)
            {
                switch (Mode)
                {
                    case Modes.Buddha:
                        ret = Path.Combine(Settings.Main_DestDir, "preview_buddhabrot");
                        break;
                    case Modes.Mandel:
                        ret = Path.Combine(Settings.Main_DestDir, "preview_mandelbrot");
                        break;
                    case Modes.TriBuddha:
                        ret = Path.Combine(Settings.Main_DestDir, "preview_tribuddha");
                        break;
                    default:
                        throw new Exception();
                }

                if (huge)
                {
                    ret += "_huge.png";
                }
                else
                {
                    ret += ".png";
                }
            }
            else
            {
                switch (Mode)
                {
                    case Modes.Buddha:
                        ret = Path.Combine(Settings.Main_DestDir, "large_buddhabrot_tiles");
                        break;
                    case Modes.Mandel:
                        ret = Path.Combine(Settings.Main_DestDir, "large_mandelbrot_tiles");
                        break;
                    case Modes.TriBuddha:
                        ret = Path.Combine(Settings.Main_DestDir, "large_tribuddha_tiles");
                        break;
                    default:
                        throw new Exception();
                }

                ret = Path.Combine(ret, "tile_" + x.ToString("00") + "_" + y.ToString("00"));

                if (huge)
                {
                    ret += "_huge.png";
                }
                else
                {
                    ret += ".png";
                }
            }

            CreateDir(Path.GetDirectoryName(ret));

            return ret;
        }

        public static string GetName(int a, int b, int level)
        {
            switch (Mode)
            {
                case Modes.TriBuddha:
                    return string.Format(Path.Combine(Settings.Main_SourceTriBuddhaDir, @"{0}x{1}\MandelThreads_0000.dat"), a, b);
                case Modes.Buddha:
                    return string.Format(Path.Combine(Settings.Main_SourceBuddhaDir, @"{0}x{1}\MandelThreads_0000.dat"), a, b);
                case Modes.Mandel:
                    return string.Format(Path.Combine(Settings.Main_SourceMandelDir, @"{0}x{1}\MandelThreads_0000.dat"), a, b);
                default:
                    throw new Exception();
            }
        }

        static Stream OpenDataFile(string file, out bool compressed)
        {
            if (file.ToLower().EndsWith("dgz"))
            {
                compressed = true;
                return new GZipStream(File.OpenRead(file), CompressionMode.Decompress);
            }
            else
            {
                compressed = false;
                return File.OpenRead(file);
            }
        }

        public static void SplitImage()
        {
            bool compressed;
            using (Stream stream = OpenDataFile(Settings.Split_Source, out compressed))
            {
                Stream[,] streams = new Stream[Settings.Split_Tiles_Width, Settings.Split_Tiles_Height];
                BinaryWriter[,] final = new BinaryWriter[Settings.Split_Tiles_Width, Settings.Split_Tiles_Height];

                for (int x = 0; x < Settings.Split_Tiles_Width; x++)
                {
                    for (int y = 0; y < Settings.Split_Tiles_Height; y++)
                    {
                        string file = Path.Combine(Settings.Split_Dest, (x + Settings.Split_Offset_X) + "x" + (y + Settings.Split_Offset_Y));

                        if (!Directory.Exists(file))
                        {
                            Directory.CreateDirectory(file);
                        }

                        if (compressed)
                        {
                            file = Path.Combine(file, "MandelThreads_0000.dgz");
                            streams[x, y] = new GZipStream(File.OpenWrite(file), CompressionMode.Compress);
                        }
                        else
                        {
                            file = Path.Combine(file, "MandelThreads_0000.dat");
                            streams[x, y] = File.OpenWrite(file);
                        }

                        final[x, y] = new BinaryWriter(streams[x, y]);

                        final[x, y].Write((Int32)8192);
                        final[x, y].Write((Int32)8192);
                    }
                }

                using (BinaryReader br = new BinaryReader(stream))
                {
                    br.ReadInt32();
                    br.ReadInt32();

                    int chunk = 0;
                    DateTime next = DateTime.Now;

                    while (true)
                    {
                        if (compressed)
                        {
                            GZipStream temp = (GZipStream)stream;
                            if (temp.BaseStream.Position >= temp.BaseStream.Length)
                            {
                                break;
                            }
                        }
                        else
                        {
                            if (stream.Position >= stream.Length)
                            {
                                break;
                            }
                        }

                        chunk++;

                        MyConsole.WriteLine("Working on chunk " + chunk);

                        for (int y = 0; y < Settings.Split_Tiles_Height * 8192; y++)
                        {
                            for (int x = 0; x < Settings.Split_Tiles_Width * 8192; x++)
                            {
                                final[x / 8192, y / 8192].Write(br.ReadUInt64());

                                if (DateTime.Now >= next)
                                {
                                    double percX = ((double)x) / ((double)(Settings.Split_Tiles_Width * 8192));
                                    percX /= (double)(Settings.Split_Tiles_Height * 8192);
                                    double percY = ((double)y) / ((double)(Settings.Split_Tiles_Height * 8192));

                                    MyConsole.WriteLine("  Working... " + ((percX + percY) * 100.0).ToString("0.000") + "%");

                                    next = next.AddSeconds(60);
                                }
                            }
                        }

                    }
                }

                for (int x = 0; x < Settings.Split_Tiles_Width; x++)
                {
                    for (int y = 0; y < Settings.Split_Tiles_Height; y++)
                    {
                        final[x, y].Close();
                        final[x, y] = null;
                        streams[x, y].Close();
                        streams[x, y].Dispose();
                        streams[x, y] = null;
                    }
                }
            }

            MyConsole.WriteLine("All done!");
        }

        public static string CompressedVersion(string file)
        {
            return Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".dgz");
        }

        public static bool SaveExists(string file)
        {
            if (File.Exists(file))
            {
                return true;
            }
            else if (File.Exists(CompressedVersion(file)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void CompressFile(string file, string dest)
        {
            DateTime next = DateTime.Now;
            MyConsole.WriteLine("Working...");

            using (Stream sourceStream = File.OpenRead(file))
            {
                using (Stream destStream = File.OpenWrite(dest))
                {
                    using (GZipStream comp = new GZipStream(destStream, CompressionMode.Compress))
                    {
                        int len = 134217728;
                        byte[] buffer = new byte[len];
                        while (true)
                        {
                            int read = sourceStream.Read(buffer, 0, len);
                            if (read == 0)
                            {
                                break;
                            }
                            comp.Write(buffer, 0, read);

                            if (DateTime.Now >= next)
                            {
                                next = next.AddSeconds(30);
                                MyConsole.WriteLine("  " + ((((double)sourceStream.Position) / ((double)sourceStream.Length)) * 100).ToString("0.00") + " % complete...");
                            }
                        }
                    }
                }
            }

            MyConsole.WriteLine("All done!");
        }

        public static void DecompressFile(string file, string dest)
        {
            DateTime next = DateTime.Now;
            MyConsole.WriteLine("Working...");

            using (Stream sourceStream = File.OpenRead(file))
            {
                using (GZipStream comp = new GZipStream(sourceStream, CompressionMode.Decompress))
                {
                    using (Stream destStream = File.OpenWrite(dest))
                    {
                        int len = 134217728;
                        byte[] buffer = new byte[len];
                        while (true)
                        {
                            int read = comp.Read(buffer, 0, len);
                            if (read == 0)
                            {
                                break;
                            }
                            destStream.Write(buffer, 0, read);

                            if (DateTime.Now >= next)
                            {
                                next = next.AddSeconds(30);
                                MyConsole.WriteLine("  " + ((((double)sourceStream.Position) / ((double)sourceStream.Length)) * 100).ToString("0.00") + " % complete...");
                            }
                        }
                    }
                }
            }

            MyConsole.WriteLine("All done!");
        }


        public unsafe static void LoadSave(string file, int overWidth, int overHeight, ref UInt64[,] height, ref UInt64[,] height2, ref UInt64[,] height3, ref double[,] res, ref double[,] ims, ref double[,] other)
        {
            int max = 134217728;
            int off = 0;
            byte[] buffer = new byte[max];
            int avail = 0;
            Stream stream;

            if (File.Exists(file))
            {
                stream = File.OpenRead(file);
            }
            else
            {
                stream = new GZipStream(File.OpenRead(CompressedVersion(file)), CompressionMode.Decompress);
            }

            avail = stream.Read(buffer, 0, max);

            int imageWidth = 0;
            int imageHeight = 0;

            fixed (byte* numRef = &(buffer[off]))
            {
                imageWidth = *(((int*)numRef));
                off += 4;
            }

            fixed (byte* numRef = &(buffer[off]))
            {
                imageHeight = *(((int*)numRef));
                off += 4;
            }

            if (overWidth > 0 && overHeight > 0)
            {
                imageWidth = overWidth;
                imageHeight = overHeight;
            }

            height = new UInt64[imageWidth, imageHeight];

            for (int y = 0; y < imageHeight; y++)
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    fixed (byte* numRef = &(buffer[off]))
                    {
                        height[x, y] = *(((UInt64*)numRef));
                    }

                    off += 8;
                    if (off == avail)
                    {
                        avail = stream.Read(buffer, 0, max);
                        off = 0;
                    }
                }
            }

            if (Helpers.Mode == Modes.TriBuddha)
            {
                if (avail > 0)
                {
                    height2 = new UInt64[imageWidth, imageHeight];

                    for (int y = 0; y < imageHeight; y++)
                    {
                        for (int x = 0; x < imageWidth; x++)
                        {
                            fixed (byte* numRef = &(buffer[off]))
                            {
                                height2[x, y] = *(((UInt64*)numRef));
                            }

                            off += 8;
                            if (off == avail)
                            {
                                avail = stream.Read(buffer, 0, max);
                                off = 0;
                            }
                        }
                    }
                }

                if (avail > 0)
                {
                    height3 = new UInt64[imageWidth, imageHeight];

                    for (int y = 0; y < imageHeight; y++)
                    {
                        for (int x = 0; x < imageWidth; x++)
                        {
                            fixed (byte* numRef = &(buffer[off]))
                            {
                                height3[x, y] = *(((UInt64*)numRef));
                            }

                            off += 8;
                            if (off == avail)
                            {
                                avail = stream.Read(buffer, 0, max);
                                off = 0;
                            }
                        }
                    }
                }
            }

            else
            {
                if (avail > 0)
                {
                    res = new double[imageWidth, imageHeight];

                    for (int y = 0; y < imageHeight; y++)
                    {
                        for (int x = 0; x < imageWidth; x++)
                        {
                            fixed (byte* numRef = &(buffer[off]))
                            {
                                res[x, y] = *(((double*)numRef));
                            }

                            off += 8;
                            if (off == avail)
                            {
                                avail = stream.Read(buffer, 0, max);
                                off = 0;
                            }
                        }
                    }
                }

                if (avail > 0)
                {
                    ims = new double[imageWidth, imageHeight];

                    for (int y = 0; y < imageHeight; y++)
                    {
                        for (int x = 0; x < imageWidth; x++)
                        {
                            fixed (byte* numRef = &(buffer[off]))
                            {
                                ims[x, y] = *(((double*)numRef));
                            }

                            off += 8;
                            if (off == avail)
                            {
                                avail = stream.Read(buffer, 0, max);
                                off = 0;
                            }
                        }
                    }
                }

                if (avail > 0)
                {
                    other = new double[imageWidth, imageHeight];

                    for (int y = 0; y < imageHeight; y++)
                    {
                        for (int x = 0; x < imageWidth; x++)
                        {
                            fixed (byte* numRef = &(buffer[off]))
                            {
                                other[x, y] = *(((double*)numRef));
                            }

                            off += 8;
                            if (off == avail)
                            {
                                avail = stream.Read(buffer, 0, max);
                                off = 0;
                            }
                        }
                    }
                }
            }

            stream.Close();
            stream.Dispose();
        }
    }
}
