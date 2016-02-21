using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using ScottsUtils;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace MandelThreads
{
    public class MainWorker
    {
        Levels m_levels;
        Coords m_coords;
        List<Thread> m_threads = new List<Thread>();
        DateTime m_lastSave = DateTime.MinValue;
        DateTime m_closeAt = DateTime.MaxValue;
        double m_lastPerc = 0;

        public static void Run()
        {
            MainWorker self = new MainWorker();
            self.RunInternal();
        }

        public void RunInternal()
        {
            Console.WriteLine("{0:yyyy/MM/dd HH:mm:ss}: Start {1:#,##0} iters, {2:#,##0} x {3:#,##0}",
                DateTime.Now, Settings.Iters, Settings.Width, Settings.Height);

            StarterThread();

            Console.WriteLine("{0:yyyy/MM/dd HH:mm:ss}: Running...", DateTime.Now);

            bool readConsole = true;

            while (true)
            {
                if (readConsole)
                {
                    try
                    {
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true);

                            ConsoleWatcherHandler(key);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // We might be redirected, just ignore console input
                        readConsole = false;
                        Console.WriteLine("{0:yyyy/MM/dd HH:mm:ss}: Ignoring console input", DateTime.Now);
                    }
                }

                if (m_closeAt <= DateTime.Now)
                {
                    break;
                }

                Thread.Sleep(2500);
            }
        }

        private void ConsoleWatcherHandler(ConsoleKeyInfo key)
        {
            switch (key.KeyChar)
            {
                case 'q':
                case 'Q':
                    Console.WriteLine("{0:yyyy/MM/dd HH:mm:ss}: Saving state and closing", DateTime.Now);
                    {
                        Coords temp = m_coords;
                        if (temp != null)
                        {
                            temp.CleanClose();
                        }
                    }
                    break;
                case 's':
                case 'S':
                    Console.WriteLine("{0:yyyy/MM/dd HH:mm:ss}: Sleeping now", DateTime.Now);
                    m_coords.Sleep();
                    break;
                case 'w':
                case 'W':
                    Console.WriteLine("{0:yyyy/MM/dd HH:mm:ss}: Waking up now", DateTime.Now);
                    m_coords.Wakeup();
                    break;
                case '?':
                    Console.WriteLine("Help: ");
                    Console.WriteLine("  ? - This help screen");
                    Console.WriteLine("  Q - Save and quit");
                    Console.WriteLine("  S - Put all worker threads to sleep");
                    Console.WriteLine("  W - Wake up all worker threads");
                    break;
            }
        }

        [DllImport(@"PixelHelper.dll")]
        static extern void PH_InitPixelHelper();
        [DllImport(@"PixelHelper.dll")]
        static extern void PH_CloseAll();
        [DllImport(@"PixelHelper.dll")]
        static extern IntPtr PH_InitCommon(bool levelsPlotReal, bool levelsPlotImaginary, bool levelsPlotOther, bool levelsPlotIters23);
        [DllImport(@"PixelHelper.dll")]
        static extern IntPtr PH_InitThread();
        [DllImport(@"PixelHelper.dll")]
        static extern IntPtr PH_Dump(IntPtr common, IntPtr thread, bool final);
        [DllImport(@"PixelHelper.dll")]
        static extern void PH_WorkerCalcPixel(ref WorkItem item, ref MandelState state, IntPtr common, IntPtr thread);
        [DllImport(@"PixelHelper.dll")]
        static extern void PH_SetSettings(int Mode, int Iters, int Iters2, int Iters3, int Width, int Height, int ViewOffX, int ViewOffY, int ViewWidth, int ViewHeight);

        void StarterThread()
        {
            PH_InitPixelHelper();
            int mode = Utilities.RenderToInt(Settings.Mode);

            PH_SetSettings(mode,
                Settings.Iters,
                Settings.Iters2,
                Settings.Iters3,
                Settings.Width,
                Settings.Height,
                Settings.ViewOffX,
                Settings.ViewOffY,
                Settings.ViewWidth,
                Settings.ViewHeight);

            m_levels = new Levels();
            bool needRe = false;
            bool needIm = false;
            bool needOt = false;
            bool needLevels = false;

            if (Settings.SaveAngleData)
            {
                needRe = true;
                needIm = true;
                if (Settings.Mode == RenderModes.Mandelbrot)
                {
                    needOt = true;
                }
            }
            else
            {
                if (Settings.Mode == RenderModes.Buddhabrot || Settings.Mode == RenderModes.AntiBuddhabrot)
                {
                    needLevels = true;
                }
            }

            m_levels.Common = PH_InitCommon(needRe, needIm, needOt, needLevels);

            m_levels.Max = 0;
            m_levels.Start = DateTime.Now;

            m_coords = new Coords(Settings.Width, Settings.Height);

            for (int thread = 0; thread < Settings.Threads; thread++)
            {
                Thread worker = new Thread(WorkerThread);
                worker.Name = "Worker #" + (thread + 1).ToString();
                worker.Start(thread);
                m_threads.Add(worker);
            }
        }

        static object s_SafeLock = new object();
        static bool s_Safe = true;

        void WorkerThread(object idObj)
        {
            int id = (int)idObj;
            bool wasPaused = false;
            Random rnd = new Random();

            if (System.Diagnostics.Debugger.IsAttached)
            {
                rnd = new Random(1);
            }

            MandelState state = Engine.CreateState();
            bool keepWorking = true;

            IntPtr thread = PH_InitThread();

            if (thread == IntPtr.Zero)
            {
                throw new Exception("Unable to init thread!");
            }

            m_coords.RegisterThread();
            bool registered = true;

            int threadOffset = m_coords.GetThreadOffset(id);

            while (keepWorking)
            {
                WorkItem item = m_coords.GetNext(id, ref threadOffset);

                if (item.WorkType == WorkTypes.Pause ||
                    item.WorkType == WorkTypes.SaveState ||
                    item.WorkType == WorkTypes.SaveStateClose)
                {
                    if (!wasPaused)
                    {
                        m_coords.Pausing();
                        wasPaused = true;
                    }
                }
                else
                {
                    if (wasPaused)
                    {
                        m_coords.Unpausing();
                        wasPaused = false;
                    }
                }

                switch (item.WorkType)
                {
                    case WorkTypes.Sleep:
                    case WorkTypes.Pause:
                        WorkerPause();
                        break;
                    case WorkTypes.CalcPixel:
                        PH_WorkerCalcPixel(ref item, ref state, m_levels.Common, thread);
                        break;
                    case WorkTypes.SaveAndFinish:
                        lock (s_SafeLock)
                        {
                            if (s_Safe)
                            {
                                PH_Dump(m_levels.Common, thread, true);
                                WorkerSaveAndFinish();
                                s_Safe = false;
                            }
                        }
                        break;
                    case WorkTypes.SaveState:
                        lock (s_SafeLock)
                        {
                            if (s_Safe)
                            {
                                PH_Dump(m_levels.Common, thread, false);
                                WorkerSaveState(false);
                            }
                        }
                        break;
                    case WorkTypes.SaveStateClose:
                        lock (s_SafeLock)
                        {
                            if (s_Safe)
                            {
                                PH_Dump(m_levels.Common, thread, true);
                                WorkerSaveState(true);
                                PH_CloseAll();
                                s_Safe = false;
                                m_closeAt = DateTime.Now.AddSeconds(5);
                                m_coords.Abort();
                            }
                        }
                        break;
                    case WorkTypes.Completed:
                        lock (s_SafeLock)
                        {
                            if (s_Safe)
                            {
                                if (registered)
                                {
                                    PH_Dump(m_levels.Common, thread, false);
                                    m_coords.DeregisterThread();
                                    registered = false;
                                }
                            }
                        }
                        break;
                    case WorkTypes.End:
                        keepWorking = false;
                        break;
                }
            }
        }

        void WorkerSaveState(bool quick)
        {
            while (m_coords.GetPaused() != Settings.Threads)
            {
                Thread.Sleep(100);
            }

            double perc = m_coords.GetPerc();

            if (m_lastPerc > 0 && perc > m_lastPerc)
            {
                Console.WriteLine("{0:yyyy/MM/dd HH:mm:ss}: Working... {1:0.0000}%, {2:0.0000}%", DateTime.Now, perc * 100.0, (perc - m_lastPerc) * 100.0);
            }
            else
            {
                Console.WriteLine("{0:yyyy/MM/dd HH:mm:ss}: Working... {1:0.0000}%", DateTime.Now, perc * 100.0);
            }

            m_lastPerc = perc;

            lock (this)
            {
                m_lastSave = DateTime.Now;
            }

            DateTime end = DateTime.Now.AddSeconds(5);
            while (end > DateTime.Now)
            {
                Thread.Sleep(500);

                if (m_coords.GetIsAbort() || quick)
                {
                    break;
                }
            }

            m_coords.DoneSaving();
        }

        void WorkerSaveAndFinish()
        {
            lock (m_levels)
            {
                string dat = "";
                string bmp = "";
                string txt = "";

                for (int i = 0; ; i++)
                {
                    dat = "MandelThreads_" + i.ToString("0000") + ".dat";
                    bmp = "MandelThreads_" + i.ToString("0000") + ".png";
                    txt = "MandelThreads_" + i.ToString("0000") + ".txt";

                    if (!File.Exists(dat) &&
                        !File.Exists(bmp) &&
                        !File.Exists(txt))
                    {
                        break;
                    }
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Time: {0}\r\n", DateTime.Now);
                sb.AppendFormat("Size: {0} x {1}\r\n", Settings.Width, Settings.Height);
                sb.AppendFormat("View: {0} x {1}\r\n", Settings.ViewWidth, Settings.ViewHeight);
                sb.AppendFormat("View Offset: {0} x {1}\r\n", Settings.ViewOffX, Settings.ViewOffY);
                sb.AppendFormat("Iterations: {0}\r\n", Settings.Iters);
                sb.AppendFormat("Alias: {0}\r\n", Settings.Alias);
                sb.AppendFormat("Threads: {0}\r\n", Settings.Threads);
                sb.AppendFormat("Mode: {0}\r\n", Settings.Mode);
                sb.AppendFormat("Save Angle Data: {0}\r\n", Settings.SaveAngleData ? "True" : "False");
                sb.AppendFormat("Run time: {0}\r\n", (DateTime.Now - m_levels.Start));

                File.WriteAllText(txt, sb.ToString());

                int width = Settings.ViewWidth;
                int height = Settings.ViewHeight;

                PH_CloseAll();

                string from = "DataDump.dat";
                string to = dat;

                while (!File.Exists(to) && File.Exists(from))
                {
                    try
                    {
                        File.Move(from, to);
                    }
                    catch { }

                    Thread.Sleep(100);
                }

                Console.WriteLine("{0:yyyy/MM/dd HH:mm:ss}: All done!", DateTime.Now);

                m_closeAt = DateTime.Now.AddSeconds(1);
            }
        }

        static void WriteValue(BinaryWriter bw, double val)
        {
            bw.Write(val);
        }

        static void WriteValue(BinaryWriter bw, Int64 val)
        {
            if (val >= 0)
            {
                WriteValue(bw, ((UInt64)val) * 2);
            }
            else
            {
                WriteValue(bw, ((UInt64)Math.Abs(val)) * 2 + 1);
            }
        }

        static void WriteValue(BinaryWriter bw, UInt64 val)
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

        void WorkerPause()
        {
            Thread.Sleep(1000);
        }
    }
}
