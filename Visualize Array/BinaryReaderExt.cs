using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace VisualizeArray
{
    static class BinaryReaderExt
    {
        public static UInt64 ReadCompressedUInt64(this BinaryReader br)
        {
            UInt64 val = 0;
            int off = 0;
            while (true)
            {
                var temp = br.ReadByte();
                val += (UInt64)((temp & 0x7F) << off);
                off += 7;
                if ((temp & 0x80) == 0)
                {
                    break;
                }
            }

            return val;
        }

        public static Int64 ReadCompressedInt64(this BinaryReader br)
        {
            UInt64 val = br.ReadCompressedUInt64();

            if ((val & 1UL) == 1UL)
            {
                return -(Int64)((val - 1) / 2);
            }
            else
            {
                return (Int64)(val / 2);
            }
        }

        static void WriteCompressed(this BinaryWriter bw, Int64 val)
        {
            if (val >= 0)
            {
                bw.WriteCompressed(((UInt64)val) * 2);
            }
            else
            {
                bw.WriteCompressed(((UInt64)Math.Abs(val)) * 2 + 1);
            }
        }

        public static void WriteCompressed(this BinaryWriter bw, UInt64 val)
        {
            if (val == 0)
            {
                bw.Write((byte)(0));
            }
            else
            {
                while (val > 0)
                {
                    if (val > 0x7F)
                    {
                        bw.Write((byte)((val & 0x7F) | 0x80));
                        val >>= 7;
                    }
                    else
                    {
                        bw.Write((byte)(val));
                        val = 0;
                    }
                }
            }
        }

        public static double ReadReal(this BinaryReader br, long off, int x, int y)
        {
            br.BaseStream.Position = off + ((x * 8192) + y) * 8;

            return br.ReadDouble();
        }

        public static double ReadImag(this BinaryReader br, long off, int x, int y)
        {
            br.BaseStream.Position = off + ((x * 8192) + y) * 8 + (8192 * 8192 * 8);

            return br.ReadDouble();
        }
    }
}
