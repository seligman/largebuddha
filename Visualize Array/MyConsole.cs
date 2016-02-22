using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace VisualizeArray
{
    public static class MyConsole
    {
        public class IndentClass : IDisposable
        {
            public IndentClass(string value)
            {
                if (value.Length > 0)
                {
                    MyConsole.WriteLine(value);
                }

                IncrementIndent(1);
            }

            public void Dispose()
            {
                IncrementIndent(-1);
            }
        }

        static int s_indent = 0;

        public static IndentClass Indent(string value)
        {
            return new IndentClass(value);
        }

        public static void IncrementIndent(int value)
        {
            s_indent += value;

            if (s_indent < 0)
            {
                s_indent = 0;
            }
        }

        static bool ConsoleAvailable = true;

        public static void WriteLine(string value)
        {
            value = new string(' ', s_indent * 2) + value;

            try
            {
                File.AppendAllText("Log.txt", value + "\r\n");
            }
            catch { }

            if (ConsoleAvailable)
            {
                try
                {
                    if (value.Length >= Console.BufferWidth)
                    {
                        value = value.Substring(0, Console.BufferWidth - 4) + "...";
                    }

                    Console.WriteLine(value);
                }
                catch
                {
                    ConsoleAvailable = false;
                }
            }

            if (!ConsoleAvailable)
            {
                Debug.WriteLine(value);
                Console.WriteLine(value);
            }
        }
    }
}
