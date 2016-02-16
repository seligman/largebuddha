using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ScottsUtils;
using System.Drawing;
using System.Diagnostics;

namespace MandelThreads
{
    // This class is responsible for creating WorkItems for each thread
    class Coords
    {
        // The size of the work area
        int m_width;
        int m_height;
        // Dates for when these events will occur
        DateTime m_nextRefresh;
        DateTime m_nextSave;

        // The total size of the image to work on
        int m_minX = 0;
        int m_minY = 0;
        int m_maxX = 0;
        int m_maxY = 0;

        // How many pixels have bene completed
        Int64 m_completed = 0;
        // How many pixels are there to do
        Int64 m_total = 0;

        // How many worker threads are done?
        int m_paused = 0;

        // This causes certain workitems to be triggered
        bool m_done = false;
        bool m_abort = false;
        bool m_saving = false;
        bool m_saveAndAbort = false;
        bool m_sleep = false;

        // Stripes of work items for the threads
        ThreadPixels[] m_points = null;
        int m_pointsCount = 0;

        // How many active worker threads are there?
        int m_registered = 0;

        // Have we sent along the final save message?
        bool m_sentFinalSave = false;

        public Coords(int width, int height)
        {
            m_width = width;
            m_height = height;
            m_nextRefresh = DateTime.Now.AddSeconds(0);

            // Get a save event to occur at 0 minutes or 30 minutes after the hour, but make sure
            // it occurs at least a minute from now
            m_nextSave = DateTime.Now.AddSeconds(60);
            m_nextSave = m_nextSave.AddMilliseconds(1000 - m_nextSave.Millisecond);
            m_nextSave = m_nextSave.AddSeconds(60 - m_nextSave.Second);
            m_nextSave = m_nextSave.AddMinutes(30 - (m_nextSave.Minute % 30));

            // Calculate how many pixels there are
            m_total = ((Int64)width) * ((Int64)height);

            // Calculate the bounding box of work items
            m_minX = 0;
            m_minY = 0;
            m_maxX = m_width - 1;
            m_maxY = m_height - 1;

            if (Settings.Mode == RenderModes.Mandelbrot)
            {
                // We can shrink that box for Mandelbrot
                m_minX = Settings.ViewOffX;
                m_minY = Settings.ViewOffY;
                m_maxX = Settings.ViewOffX + Settings.ViewWidth - 1;
                m_maxY = Settings.ViewOffY + Settings.ViewHeight - 1;
            }

            MandelState state = Engine.CreateState();

            if (Settings.Mode == RenderModes.Buddhabrot ||
                Settings.Mode == RenderModes.AntiBuddhabrot)
            {
                // For the buddhabrot fractals, it actually needs to grow a bit sometimes
                for (double ang = 0; ang < 360; ang += 1)
                {
                    Complex cur = new Complex(-0.5, 0) + Complex.RotatePoint(1.6, ang);
                    int x;
                    int y;
                    Engine.ComplexToPoint(cur, state, out x, out y);

                    m_minX = Math.Min(m_minX, x);
                    m_minY = Math.Min(m_minY, y);
                    m_maxX = Math.Max(m_maxX, x);
                    m_maxY = Math.Max(m_maxY, y);
                }
            }

            // Recalc the total number of pixels to calculate
            m_total = ((Int64)((m_maxX - m_minX) + 1)) * ((Int64)((m_maxY - m_minY) + 1));

            // And finally, create several 'clusters' of work items so each thread
            // is working on a different area of the fractal to prevent memory
            // collisions
            GeneratePoints();
        }

        // Abort now!
        public void Abort()
        {
            lock (this)
            {
                m_abort = true;
            }
        }

        // Called when we're done saving
        public void DoneSaving()
        {
            lock (this)
            {
                m_saving = false;
            }
        }

        // Close, but only after saving
        public void CleanClose()
        {
            lock (this)
            {
                m_saveAndAbort = true;
            }
        }

        // Save as soon as possible
        public void RequestSave()
        {
            lock (this)
            {
                while (DateTime.Now < m_nextSave)
                {
                    m_nextSave = m_nextSave.AddSeconds(-Settings.SaveSecs);
                }
            }
        }

        // Refresh the UI as soon as possible
        public void ReqestRefresh()
        {
            lock (this)
            {
                while (DateTime.Now < m_nextRefresh)
                {
                    m_nextRefresh = m_nextRefresh.AddSeconds(-Settings.DrawSecs);
                }
            }
        }

        // Get the next work item for a given thread
        public WorkItem GetNext(int thread, ref int threadOffset)
        {
            lock (this)
            {
                if (m_saveAndAbort)
                {
                    // Time to save and close
                    m_saving = true;
                    m_saveAndAbort = false;
                    return new WorkItem(WorkTypes.SaveStateClose);
                }
                else if (m_sleep)
                {
                    // We're just sleeping for a bit
                    return new WorkItem(WorkTypes.Sleep);
                }
                else if (m_abort)
                {
                    // Ah!  Abort!
                    return new WorkItem(WorkTypes.Completed);
                }
                else if (m_saving)
                {
                    // Someone's saving, just sleep for a bit
                    return new WorkItem(WorkTypes.Pause);
                }
                else if (!m_done && DateTime.Now > m_nextSave)
                {
                    // Hey!  It's time to save, note that, and return a save work item
                    while (DateTime.Now > m_nextSave)
                    {
                        m_nextSave = m_nextSave.AddSeconds(Settings.SaveSecs);
                    }

                    m_saving = true;

                    return new WorkItem(WorkTypes.SaveState);
                }
                else if (!m_done && DateTime.Now > m_nextRefresh)
                {
                    // Time to refresh the UI, note it, and return the work item
                    while (DateTime.Now > m_nextRefresh)
                    {
                        m_nextRefresh = m_nextRefresh.AddSeconds(Settings.DrawSecs);
                    }

                    return new WorkItem(WorkTypes.DrawOutput, ((double)m_completed) / ((double)m_total));
                }
                else
                {
                    // No other meta-work to do, so see if we're done
                    if (m_done)
                    {
                        if (m_registered == 0)
                        {
                            if (!m_sentFinalSave)
                            {
                                m_sentFinalSave = true;
                                return new WorkItem(WorkTypes.SaveAndFinish);
                            }
                            else
                            {
                                return new WorkItem(WorkTypes.End);
                            }
                        }
                        else
                        {
                            return new WorkItem(WorkTypes.Completed);
                        }
                    }
                    else
                    {
                        // Ok, find the next 'stripe' that has work items in and pick it to work from
                        for (int i = threadOffset; ; i++)
                        {
                            var pt = m_points[i % m_pointsCount];

                            if (pt != null)
                            {
                                // This one has something, so calculate the work item
                                threadOffset = i % m_pointsCount;
                                var ret = new WorkItem(WorkTypes.CalcPixel, pt.X, pt.Y);
                                m_completed++;
                                pt.Left--;

                                if (pt.Left == 0)
                                {
                                    m_points[i % m_pointsCount] = null;
                                }
                                else
                                {
                                    // Move the X/Y along the chain
                                    pt.Y++;
                                    if (pt.Y > m_maxY)
                                    {
                                        pt.Y = m_minY;
                                        pt.X++;
                                    }
                                }

                                // If we actually finished up, the mark the fact we're all done
                                if (m_completed == m_total)
                                {
                                    m_done = true;
                                }

                                return ret;
                            }
                        }
                    }
                }
            }
        }

        // Helper to return a starting thread offset.  This is the stripe we'll work on
        public int GetThreadOffset(int thread)
        {
            if (Settings.Mode == RenderModes.Mandelbrot)
            {
                return thread;
            }
            else
            {
                return thread * 25;
            }
        }

        // Helper to generate all the stripes for the workers
        void GeneratePoints()
        {
            LinkedList<ThreadPixels> temp = new LinkedList<ThreadPixels>();
            double size = ((double)m_total) / ((double)Settings.Threads * 25);
            if (Settings.Mode == RenderModes.Mandelbrot)
            {
                // No need to be clever for mandelbrot, just a subset will do
                size = ((double)m_total) / ((double)Settings.Threads);
            }
            double end = 0;
            double cur = 0;

            // Just run through the X/Y, and make a new one when the last one is full enough
            for (int x = m_minX; x <= m_maxX; x++)
            {
                for (int y = m_minY; y <= m_maxY; y++)
                {
                    cur++;
                    if (cur > end)
                    {
                        temp.AddLast(new ThreadPixels());

                        temp.Last.Value.X = x;
                        temp.Last.Value.Y = y;
                        temp.Last.Value.Left = 0;

                        end += size;
                    }

                    temp.Last.Value.Left++;
                }
            }

            // And sort them randomly
            List<ThreadPixels> temp2 = new List<ThreadPixels>();
            Random rnd = new Random(1234);
            foreach (var pt in temp)
            {
                temp2.Add(pt);
                pt.Sort = rnd.NextDouble();
            }

            if (Settings.Mode != RenderModes.Mandelbrot)
            {
                // Really no need to do this for the Mandelbrot fractal
                temp2.Sort((a, b) =>
                {
                    return a.Sort.CompareTo(b.Sort);
                });
            }

            // All done, save our results
            m_points = temp2.ToArray();
            m_pointsCount = m_points.Length;
        }

        public void RegisterThread()
        {
            lock (this)
            {
                m_registered++;
            }
        }

        public void DeregisterThread()
        {
            lock (this)
            {
                m_registered--;
            }
        }

        // This thread is pausing
        public void Pausing()
        {
            lock (this)
            {
                m_paused++;
            }
        }

        // This thread is done pausing
        public void Unpausing()
        {
            lock (this)
            {
                m_paused--;
            }
        }

        // How many threads are paused?
        public int GetPaused()
        {
            lock (this)
            {
                return m_paused;
            }
        }

        // Tell this thread to sleep
        public void Sleep()
        {
            lock (this)
            {
                m_sleep = true;
            }
        }

        // Wake up this thread
        public void Wakeup()
        {
            lock (this)
            {
                m_sleep = false;
            }
        }

        // Serialize and Unserialize is very broken, it can't be used to recover state anymore
        //public void Serialize(BinaryWriter bw)
        //{
        //    // TODO: Fix up serialization
        //    //bw.Write(m_x);
        //    //bw.Write(m_y);
        //    bw.Write(m_completed);
        //}

        //public void Deserialize(BinaryReader br)
        //{
        //    // TODO: Fix this up too
        //    //m_x = br.ReadInt32();
        //    //m_y = br.ReadInt32();
        //    m_completed = br.ReadInt64();
        //}

        // Have we aborted?
        public bool GetIsAbort()
        {
            lock (this)
            {
                return m_abort;
            }
        }

        // How done are things?
        public double GetPerc()
        {
            lock (this)
            {
                return ((double)m_completed) / ((double)m_total);
            }
        }
    }
}
