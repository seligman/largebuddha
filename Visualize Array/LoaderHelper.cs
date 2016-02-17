using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace VisualizeArray
{
    class LoaderHelper
    {
        Thread m_thread;
        AutoResetEvent m_hasOne = new AutoResetEvent(false);
        AutoResetEvent m_gotOne = new AutoResetEvent(false);

        int m_tileX = 0;
        int m_tileY = 0;
        ComplexData m_data = null;
        double[,] m_res = null;
        double[,] m_ims = null;
        double[,] m_other = null;
        UInt64[,] m_height = null;
        UInt64[,] m_height2 = null;
        UInt64[,] m_height3 = null;

        public LoaderHelper()
        {
            m_thread = new Thread(Worker);
            m_thread.Start();
        }

        public ComplexData GetData(ref int x, ref int y, ref double[,] res, ref double[,] ims, ref double[,] other, ref UInt64[,] height, ref UInt64[,] height2, ref UInt64[,] height3)
        {
            m_hasOne.WaitOne();
            x = m_tileX;
            y = m_tileY;
            res = m_res;
            ims = m_ims;
            other = m_other;
            height = m_height;
            height2 = m_height2;
            height3 = m_height3;
            ComplexData ret = m_data;
            m_gotOne.Set();

            if (ret == null)
            {
                m_thread.Join();
                m_thread = null;
            }

            m_tileX = 0;
            m_tileY = 0;
            m_data = null;
            m_res = null;
            m_ims = null;
            m_other = null;
            m_height = null;
            m_height2 = null;
            m_height3 = null;

            return ret;
        }

        public ComplexData GetData()
        {
            int x = 0;
            int y = 0;
            double[,] res = null;
            double[,] ims = null;
            double[,] other = null;
            UInt64[,] height = null;
            UInt64[,] height2 = null;
            UInt64[,] height3 = null;

            return GetData(ref x, ref y, ref res, ref ims, ref other, ref height, ref height2, ref height3);
        }

        void Worker()
        {
            for (int tileX = 0; tileX < Settings.Main_Tiles_PerSide; tileX++)
            {
                for (int tileY = 0; tileY < Settings.Main_Tiles_PerSide; tileY++)
                {
                    string file = Helpers.GetName(tileX, tileY, 0);

                    if (Helpers.SaveExists(file))
                    {
                        MyConsole.WriteLine("Loading " + tileX + " x " + tileY);

                        Helpers.LoadSave(file, 8192, 8192, ref m_height, ref m_height2, ref m_height3, ref m_res, ref m_ims, ref m_other);
                        ComplexData data = null;
                        if (m_height != null)
                        {
                            data = new ComplexData(m_res, m_ims, m_other, m_height, m_height2, m_height3);
                        }

                        if (data.Loaded)
                        {
                            m_tileX = tileX;
                            m_tileY = tileY;
                            m_data = data;
                            m_hasOne.Set();
                            m_gotOne.WaitOne();
                        }
                    }
                }
            }

            m_data = null;
            m_tileX = 0;
            m_tileY = 0;
            m_hasOne.Set();
        }
    }
}
