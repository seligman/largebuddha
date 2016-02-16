using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace VisualizeArray
{
    class CachedStream : Stream, IDisposable
    {
#if DEBUG
        public static void Test_RunTest()
        {
            string file = @"a31033a5049d3d3c4b6bfe6ed4535142";
            Random rnd = new Random(42);

            using (Stream good = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (CachedStream test = new CachedStream(file))
            {
                long scan = 0;
                for (int i = 0; i < 100000000; i++)
                {
                    if (i % 100000 == 0)
                    {
                        Console.WriteLine("Byte: " + i + ", " + scan);
                    }

                    if (scan > 10 && rnd.Next(0, 5) == 0)
                    {
                        scan -= rnd.Next(0, 10);
                    }
                    else
                    {
                        scan += rnd.Next(0, 10);
                    }

                    if (Test_GetStreamByte(good, scan) != test.GetByte(scan))
                    {
                        Debugger.Break();
                        throw new Exception();
                    }
                }

                for (int i = 0; i < 100; i++)
                {
                    int len = rnd.Next(1000, 10000000);
                    int off = rnd.Next(0, (int)good.Length - len);

                    if (rnd.Next(0, 10) != 0)
                    {
                        len = rnd.Next(1000, 5000);
                        off = rnd.Next(0, (int)good.Length - len);
                    }

                    Console.WriteLine(i + ": " + len + ", " + off);

                    good.Seek(off, SeekOrigin.Begin);
                    test.Seek(off, SeekOrigin.Begin);

                    byte[] buffer = new byte[len];
                    int read = good.Read(buffer, 0, len);
                    if (read != len)
                    {
                        Debugger.Break();
                        throw new Exception();
                    }

                    for (int j = 0; j < len; j++)
                    {
                        if (test.GetByte(off + j) != buffer[j])
                        {
                            Debugger.Break();
                            throw new Exception();
                        }
                    }
                }
            }

            Console.WriteLine("Done");
            Console.Write("Press any key to continue . . . ");
            Console.ReadKey(true);
            Console.WriteLine();
        }

        static byte Test_GetStreamByte(Stream stream, long index)
        {
            byte[] buffer = new byte[1];
            if (stream.Read(buffer, 0, 1) == 1)
            {
                return buffer[0];
            }
            else
            {
                throw new Exception();
            }
        }
#endif
        Stream m_base;
        long m_pos = 0;
        const int AvailBufferSize = 16777216;
        byte[] m_lastBuffer = new byte[AvailBufferSize];
        byte[] m_buffer = new byte[AvailBufferSize];
        long m_lastBufferLen = 0;
        long m_bufferLen = 0;
        long m_bufferPos = -AvailBufferSize;
        long m_lastBufferPos = -AvailBufferSize;

        public CachedStream(string target)
        {
            m_base = File.Open(target, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override void Flush()
        {
            // Nothing to do
        }

        public override long Length
        {
            get
            {
                return m_base.Length;
            }
        }

        public override long Position
        {
            get
            {
                return m_pos;
            }
            set
            {
                m_pos = value;
            }
        }

        byte[] m_byteBuffer = new byte[1];
        public byte GetByte(long index)
        {
            m_pos = index;
            if (Read(m_byteBuffer, 0, 1) == 1)
            {
                return m_byteBuffer[0];
            }
            else
            {
                throw new Exception();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int ret = 0;
            while (count > 0)
            {
                if (!(m_pos >= m_bufferPos && m_pos < m_bufferPos + AvailBufferSize))
                {
                    byte[] temp = m_lastBuffer;
                    long tempPos = m_lastBufferPos;
                    long tempLen = m_lastBufferLen;
                    m_lastBuffer = m_buffer;
                    m_lastBufferPos = m_bufferPos;
                    m_lastBufferLen = m_bufferLen;
                    m_buffer = temp;
                    m_lastBufferPos = tempPos;
                    m_lastBufferLen = tempLen;

                    if (!(m_pos >= m_bufferPos && m_pos < m_bufferPos + AvailBufferSize))
                    {
                        m_bufferPos = m_pos - (m_pos % AvailBufferSize);
                        m_base.Seek(m_bufferPos, SeekOrigin.Begin);
                        m_bufferLen = m_base.Read(m_buffer, 0, AvailBufferSize);
                    }
                }

                long start = (m_pos % AvailBufferSize);
                long end = start + count;

                if (end > m_bufferLen)
                {
                    end = m_bufferLen;
                }

                Array.Copy(m_buffer, start, buffer, offset, end - start);

                count -= (int)(end - start);
                ret += (int)(end - start);
                offset += (int)(end - start);
                m_pos += (end - start);

                if (m_bufferLen < AvailBufferSize)
                {
                    break;
                }
            }

            return ret;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    m_pos = offset;
                    break;
                case SeekOrigin.End:
                    m_pos = Length + offset;
                    break;
                case SeekOrigin.Current:
                    m_pos += offset;
                    break;
            }

            return m_pos;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        void IDisposable.Dispose()
        {
            m_base.Dispose();
        }
    }
}
