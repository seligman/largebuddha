using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ScottsUtils;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace MandelThreads
{
    public partial class MainForm : Form
    {
        Bitmap m_display;
        Levels m_levels;
        Coords m_coords;
        List<Thread> m_threads = new List<Thread>();
        DateTime m_lastSave = DateTime.MinValue;
        bool m_wasMinimized = false;
        DateTime m_closeAt;
        bool m_wasResting = true;
        double m_perc = 0;
        double m_lastPerc = 0;
        bool m_quitDuringBreather = false;

        Thread m_consoleThread;

        public MainForm()
        {
            m_consoleThread = new Thread(ConsoleWatcher);
            m_consoleThread.Start();

            InitializeComponent();

            Console.WriteLine("{0:yyyy/MM/dd HH:mm:ss}: Start {1:#,##0} iters, {2:#,##0} x {3:#,##0}",
                DateTime.Now, Settings.Iters, Settings.Width, Settings.Height);

            try
            {
                Icon = MandelThreads.Properties.Resources.Mandelbrot;
            }
            catch { }
        }

        void ConsoleWatcher()
        {
            while (true)
            {
                var key = Console.ReadKey(true);

                BeginInvoke(new MethodInvoker(delegate()
                    {
                        ConsoleWatcherHandler(key);
                    }));
            }
        }

        private void ConsoleWatcherHandler(ConsoleKeyInfo key)
        {
            switch (key.KeyChar)
            {
                case ' ':
                    this.Visible = !this.Visible;

                    if (this.Visible)
                    {
                        WindowState = FormWindowState.Normal;
                        Location = new Point(10, 10);
                        ClientSize = new Size(500, 500);
                    }
                    break;
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
                    Console.WriteLine("  <Space> - Show or hide the preview form");
                    Console.WriteLine("  Q - Save and quit");
                    Console.WriteLine("  S - Put all worker threads to sleep");
                    Console.WriteLine("  W - Wake up all worker threads");
                    break;
            }
        }

        void MainForm_Load(object sender, EventArgs e)
        {
            int width = Settings.ViewWidth;
            int height = Settings.ViewHeight;
            int scale = Settings.Scale;
            m_display = new Bitmap(width / scale, height / scale, PixelFormat.Format32bppArgb);
            ClientSize = m_display.Size;

            BitmapData data = m_display.LockBits(Utilities.GetRect(m_display), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int[] bits = new int[data.Stride / 4 * data.Height];
            int dark = Color.FromArgb(204, 204, 204).ToArgb();
            int white = Color.FromArgb(255, 255, 255).ToArgb();
            for (int x = 0; x < width / scale; x++)
            {
                for (int y = 0; y < height / scale; y++)
                {
                    bits[x + y * data.Stride / 4] = ((((x / 8) + (y / 8)) % 2 == 1) ? dark : white);
                }
            }
            Marshal.Copy(bits, 0, data.Scan0, bits.Length);
            m_display.UnlockBits(data);

            Thread starter = new Thread(StarterThread);
            starter.Name = "Starter Thread";
            m_threads.Add(starter);
            starter.Start();
        }

        [DllImport(@"PixelHelper.dll")]
        static extern void PH_InitPixelHelper();
        [DllImport(@"PixelHelper.dll")]
        static extern void PH_CloseAll();
        [DllImport(@"PixelHelper.dll")]
        static extern IntPtr PH_InitCommon(bool levelsPlotReal, bool levelsPlotImaginary, bool levelsPlotOther);
        [DllImport(@"PixelHelper.dll")]
        static extern IntPtr PH_InitThread();
        [DllImport(@"PixelHelper.dll")]
        static extern IntPtr PH_Dump(IntPtr common, IntPtr thread, bool final);
        [DllImport(@"PixelHelper.dll")]
        static extern void PH_WorkerCalcPixel(ref WorkItem item, ref MandelState state, IntPtr common, IntPtr thread);
        [DllImport(@"PixelHelper.dll")]
        static extern void PH_SetSettings(int Mode, int Iters, int Width, int Height, int ViewOffX, int ViewOffY, int ViewWidth, int ViewHeight);

        void StarterThread()
        {
            BeginInvoke(new MethodInvoker(delegate()
            {
                Visible = false;
            }));

            PH_InitPixelHelper();
            int mode = Utilities.RenderToInt(Settings.Mode);

            PH_SetSettings(mode, Settings.Iters, Settings.Width, Settings.Height, Settings.ViewOffX, Settings.ViewOffY, Settings.ViewWidth, Settings.ViewHeight);

            m_levels = new Levels();
            bool needRe = false;
            bool needIm = false;
            bool needOt = false;

            if (Settings.SaveAngleData)
            {
                needRe = true;
                needIm = true;
                if (Settings.Mode == RenderModes.Mandelbrot)
                {
                    needOt = true;
                }
            }

            m_levels.Common = PH_InitCommon(needRe, needIm, needOt);

            m_levels.Max = 0;
            m_levels.Start = DateTime.Now;

            m_coords = new Coords(Settings.Width, Settings.Height);

            //if (File.Exists(Settings.SavedState))
            //{
            //    BeginInvoke(new MethodInvoker(delegate()
            //    {
            //        Text = "Loading saved state...";
            //    }));

            //    using (Stream stream = File.OpenRead(Settings.SavedState))
            //    using (BinaryReader br = new BinaryReader(stream))
            //    {
            //        if (br.ReadInt32() == Settings.Width &&
            //            br.ReadInt32() == Settings.Height &&
            //            br.ReadInt32() == Settings.Scale &&
            //            br.ReadInt32() == Settings.Iters &&
            //            br.ReadInt32() == Settings.Alias &&
            //            br.ReadInt32() >= 0 /* == Settings.Threads */)
            //        {
            //            BeginInvoke(new MethodInvoker(delegate()
            //            {
            //                Console.WriteLine("{0:yyyy/MM/dd HH:mm:ss}: Loading previous save state...", DateTime.Now);
            //            }));

            //            m_coords.Deserialize(br);

            //            m_perc = m_coords.GetPerc();

            //            BeginInvoke(new MethodInvoker(delegate()
            //            {
            //                Console.WriteLine("{0:yyyy/MM/dd HH:mm:ss}: Loaded... {1:0.0000}%", DateTime.Now, m_perc * 100.0);
            //            }));
            //        }
            //    }
            //}

            for (int thread = 0; thread < Settings.Threads; thread++)
            {
                Thread worker = new Thread(WorkerThread);
                worker.Name = "Worker #" + (thread + 1).ToString();
                worker.Start(thread);
                m_threads.Add(worker);
            }

            if (Debugger.IsAttached)
            {
                BeginInvoke(new MethodInvoker(delegate()
                    {
                        WindowState = FormWindowState.Normal;
                    }));
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
                    case WorkTypes.DrawOutput:
                    case WorkTypes.DrawOutputFinal:
                        lock (s_SafeLock)
                        {
                            if (s_Safe)
                            {
                                WorkerDrawOutput(item);
                            }
                        }
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
                                BeginInvoke(new MethodInvoker(delegate()
                                {
                                    m_closeAt = DateTime.Now.AddSeconds(5);
                                    m_timer.Enabled = true;
                                }));
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
            BeginInvoke(new MethodInvoker(delegate()
            {
                Text = "Saving: Waiting for worker threads...";
            }));

            while (m_coords.GetPaused() != Settings.Threads)
            {
                Thread.Sleep(100);
            }

            //if (true)
            //{
            //    string data = Settings.SavedState;
            //    string dataBack = Settings.SavedState + "_back";

            //    if (File.Exists(dataBack))
            //    {
            //        File.Delete(dataBack);
            //    }

            //    if (File.Exists(data))
            //    {
            //        File.Move(data, dataBack);
            //    }

            //    BeginInvoke(new MethodInvoker(delegate()
            //    {
            //        Text = "Saving: Writing data...";
            //    }));

            //    using (Stream stream = File.OpenWrite(data))
            //    using (BinaryWriter bw = new BinaryWriter(stream))
            //    {
            //        lock (m_coords)
            //        {
            //            lock (m_levels)
            //            {
            //                bw.Write((Int32)Settings.Width);
            //                bw.Write((Int32)Settings.Height);
            //                bw.Write((Int32)Settings.Scale);
            //                bw.Write((Int32)Settings.Iters);
            //                bw.Write((Int32)Settings.Alias);
            //                bw.Write((Int32)Settings.Threads);

            //                m_coords.Serialize(bw);
            //            }
            //        }
            //    }

            //    if (File.Exists(dataBack))
            //    {
            //        File.Delete(dataBack);
            //    }
            //}

            BeginInvoke(new MethodInvoker(delegate()
            {
                if (m_lastPerc > 0 && m_perc > m_lastPerc)
                {
                    Console.WriteLine("{0:yyyy/MM/dd HH:mm:ss}: Working... {1:0.0000}%, {2:0.0000}%", DateTime.Now, m_perc * 100.0, (m_perc - m_lastPerc) * 100.0);
                }
                else
                {
                    Console.WriteLine("{0:yyyy/MM/dd HH:mm:ss}: Working... {1:0.0000}%", DateTime.Now, m_perc * 100.0);
                }
                m_lastPerc = m_perc;
                Text = "Saving: Taking a breather...";

                if (m_quitDuringBreather)
                {
                    Close();
                }
            }));

            lock (this)
            {
                m_lastSave = DateTime.Now;
                m_wasResting = true;
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
                sb.AppendFormat("Scale: {0}\r\n", Settings.Scale);
                sb.AppendFormat("Iterations: {0}\r\n", Settings.Iters);
                sb.AppendFormat("Alias: {0}\r\n", Settings.Alias);
                sb.AppendFormat("Threads: {0}\r\n", Settings.Threads);
                sb.AppendFormat("Mode: {0}\r\n", Settings.Mode);
                sb.AppendFormat("Save Angle Data: {0}\r\n", Settings.SaveAngleData ? "True" : "False");
                sb.AppendFormat("Run time: {0}\r\n", (DateTime.Now - m_levels.Start));

                File.WriteAllText(txt, sb.ToString());

                int width = Settings.ViewWidth;
                int height = Settings.ViewHeight;

                //if (File.Exists(Settings.SavedState))
                //{
                //    File.Delete(Settings.SavedState);
                //}

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

                BeginInvoke(new MethodInvoker(delegate()
                {
                    Console.WriteLine("{0:yyyy/MM/dd HH:mm:ss}: All done!", DateTime.Now);
                    Text = "All done!";

                    if (WindowState == FormWindowState.Minimized)
                    {
                        m_closeAt = DateTime.Now.AddSeconds(1);
                    }
                    else
                    {
                        m_closeAt = DateTime.Now.AddSeconds(30);
                    }
                    m_timer.Enabled = true;
                }));
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

        void WorkerDrawOutput(WorkItem item)
        {
            bool skipUI = false;

            lock (this)
            {
                if (item.WorkType == WorkTypes.DrawOutput)
                {
                    if (WindowState == FormWindowState.Minimized)
                    {
                        if (m_wasResting)
                        {
                            m_wasResting = false;
                        }
                        else
                        {
                            skipUI = true;
                        }
                    }
                }
            }

            if (item.WorkType == WorkTypes.DrawOutput)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    lock (this)
                    {
                        StringBuilder sb = new StringBuilder();

                        m_perc = item.Perc;

                        sb.Append("Mandel Threads");
                        sb.Append(" \u2502 ");
                        sb.Append(Settings.Iters);
                        sb.Append(" \u2502 ");
                        sb.AppendFormat("{0:0.000}%", item.Perc * 100.0);
                        sb.Append(" \u2502 ");
                        sb.Append("Last save: ");

                        if (m_lastSave == DateTime.MinValue)
                        {
                            sb.Append("(never)");
                        }
                        else
                        {
                            sb.AppendFormat("{0:h:mm}", m_lastSave);
                        }

                        Text = sb.ToString();
                    }
                }));
            }

            if (!skipUI)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    Invalidate();
                }));
            }
        }

        void WorkerPause()
        {
            Thread.Sleep(1000);
        }

        void MainForm_Paint(object sender, PaintEventArgs e)
        {
            lock (m_display)
            {
                e.Graphics.DrawImage(m_display, 0, 0);
            }
        }

        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_consoleThread.Abort();

            m_coords.Abort();

            foreach (var cur in m_threads)
            {
                cur.Join();
            }

            Environment.Exit(0);
        }

        void SaveDataMenuItem_Click(object sender, EventArgs e)
        {
            m_coords.RequestSave();
        }

        void SaveAndExitMenuItem_Click(object sender, EventArgs e)
        {
            m_quitDuringBreather = true;
            m_coords.RequestSave();

        }

        void ExitMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                m_wasMinimized = true;
            }
            else
            {
                if (m_wasMinimized)
                {
                    m_wasMinimized = false;
                    m_coords.ReqestRefresh();
                }
            }
        }

        void Timer_Tick(object sender, EventArgs e)
        {
            if (m_closeAt < DateTime.Now)
            {
                Close();
            }
        }
    }
}
