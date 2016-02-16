using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace Utils
{
    public class PreviewForm : Form
    {
        #region WinAPI P/Invokes
#if !UselWinAPI
        static class WinAPI
        {
            [DllImport("user32.dll")]
            public static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);
            [DllImport("user32.dll")]
            public static extern IntPtr CreateIconIndirect(ref ICONINFO piconinfo);

            public const int HTBOTTOMRIGHT = 17;

            public enum WindowMessage : uint
            {
                WM_LBUTTONDOWN = 0x201,
                WM_NCLBUTTONDOWN = 0xA1,
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct ICONINFO
            {
                public bool fIcon;
                public Int32 xHotspot;
                public Int32 yHotspot;
                public IntPtr hbmMask;
                public IntPtr hbmColor;
            }
        }
#endif
        #endregion
        #region Hole PictureBox
        public class HolePictureBox : PictureBox
        {
            public IntPtr ParentHandle;

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == (int)WinAPI.WindowMessage.WM_LBUTTONDOWN)
                {
                    Message msg = Message.Create(ParentHandle,
                        (int)WinAPI.WindowMessage.WM_NCLBUTTONDOWN,
                        (IntPtr)WinAPI.HTBOTTOMRIGHT,
                        IntPtr.Zero);

                    DefWndProc(ref msg);
                }
                else
                {
                    base.WndProc(ref m);
                }
            }
        }
        #endregion
        #region Hand Grab Cursors
#if DEBUG && LoadCursorAndTestCode
        public static void DebugShowForm(string file)
        {
            PreviewForm form = new PreviewForm(null, (Bitmap)Image.FromFile(file));
            form.ShowDialog();
        }

        public static string LoadCursor(string file)
        {
            using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read))
            {
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, bytes.Length);
                string temp = Convert.ToBase64String(bytes);

                if (temp.Contains("{") || temp.Contains("}"))
                {
                    System.Diagnostics.Debugger.Break();
                }

                temp = Regex.Replace(
                    temp,
                    "(.)\\1*",
                    delegate(Match match)
                    {
                        string repl = "{" + 
                            match.Value.Substring(0,1) +
                            match.Value.Length.ToString("x") + 
                            "}";
                        if (repl.Length < match.Value.Length)
                        {
                            return repl;
                        }
                        else
                        {
                            return match.Value;
                        }
                    });

                StringBuilder sb = new StringBuilder();
                while (temp.Length > 0)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(" +");
                        sb.AppendLine();
                    }
                    sb.Append("\"");
                    if (temp.Length > 70)
                    {
                        sb.Append(temp.Substring(0, 70));
                        temp = temp.Substring(70);
                    }
                    else
                    {
                        sb.Append(temp);
                        temp = "";
                    }
                    sb.Append("\"");
                }

                return sb.ToString();
            }
        }
#endif
        static Cursor m_HandGrabCursor = null;
        public static Cursor HandGrabCursor
        {
            get
            {
                if (m_HandGrabCursor == null)
                {
                    m_HandGrabCursor = CursorFromString(
                        "AAACAAEAICAAABAAFACoEAAAFgAAACgAAAAgAAAAQ{A5}EAI{A62}aBgM3GgYD/xoGA/8a" +
                        "BgP/GgYD/xoGA/8aBgP/GgYD/xoGA/8aBgO6{A70}GgYDNxoGA73Pzc3/4eHh/97e3v/b2" +
                        "9v/2dnZ/9bW1v/U1NT/0tLS/xoGA94{A6a}BoGAzcaBgO919XV/+rq6v/n5+f/4+Pj/+Dg" +
                        "4P/d3d3/29vb/9jY2P/W1tb/GgYD3hoGAz{A61}aBgM3GgYDvd/d3P/y8vL/7+/v/+zs7P" +
                        "/p6en/5ubm/+Pj4//g4OD/3d3d/9ra2v8aBgOHGgYDhw{A5f}BoGA73l4+P/+vr6//f39/" +
                        "/19fX/8vLy/+/v7//r6+v/6Ojo/+Xl5f/i4uL/39/f/7Ovrv8aBgPV{A5b}aBgNjGgYDtP" +
                        "{/5}Nycj/+/v7//n5+f/39/f/9PT0//Hx8f/u7u7/6+vr/+fn5//k5OT/4eHh/xoGA94aB" +
                        "gMw{A50}GgYDNhoGA7ro5uX{/6}4Z7ev/+/v7//f39//v7+//5+fn/9vb2//Pz8//w8PD/" +
                        "7e3t/+rq6v/n5+f/GgYDhxoGA4c{A50}aBgO66Obl{/c}GgYD{/d}v7+//z8/P/6+vr/+P" +
                        "j4//X19f/y8vL/7+/v/+zs7P+9ubj/GgYD1Q{A4f}BoGA//o5uX/6Obl/xoGA7oaBgP{/1" +
                        "7}7+/v/8/Pz/+vr6//f39//19fX/8vLy/+/v7/8aBgP/{A50}GgYDuhoGA/8aBgO6GgYDN" +
                        "hoGA{/22}9/f3/+/v7//n5+f/39/f/9PT0/xoGA/8{A65}GgYD{/27}+/v7/GgYD//v7+/" +
                        "/5+fn/GgYD/w{A65}aBgP{/b}8aBgP{/b}8aBgP{/b}8aBgP//v7+/8zIx/8aBgO6{A65}" +
                        "BoGA{/c}xoGA{/c}xoGA{/c}xoGA//Pysr/GgYDuhoGAxo{A65}GgYD/8/Kyv{/6}GgYD{" +
                        "/c}GgYD{/7}Pysr/GgYD/xoGA7oaBgMa{A6b}aBgM2GgYDuhoGA/8aBgO6{/a}8aBgO6Gg" +
                        "YD/xoGA7oaBgM2{A85}BoGAzYaBgO6GgYDuhoGAzY{Aaf5}//AD///gA///wAH//4AB//+" +
                        "AAf//AAD//gAA//4AAP/+AAD//gAA///gAP//4AD//+AA///gAf//4Af///w{/57}8=");
                }

                return m_HandGrabCursor;
            }
        }

        static Cursor m_HandCursor = null;
        public static Cursor HandCursor
        {
            get
            {
                if (m_HandCursor == null)
                {
                    m_HandCursor = CursorFromString(
                        "AAACAAEAICAAABAAFACoEAAAFgAAACgAAAAgAAAAQ{A5}EAI{A62}aBgM3GgYD/xoGA/8a" +
                        "BgP/GgYD/xoGA/8aBgP/GgYD/xoGA/8aBgO6{A70}GgYDNxoGA73Ny8r/39/f/93d3f/a2" +
                        "tr/2NjY/9bW1v/V1dX/09PT/xoGA94{A6a}BoGAzcaBgO909HR/+Xl5f/j4+P/4eHh/97e" +
                        "3v/c3Nz/2tra/9jY2P/W1tb/GgYD3hoGAz{A61}aBgM3GgYDvdnX1//s7Oz/6urq/+fn5/" +
                        "/l5eX/4+Pj/+Dg4P/e3t7/3Nzc/9ra2v8aBgOHGgYDhw{A5f}BoGA73f3dz/8/Pz//Hx8f" +
                        "/u7u7/7Ozs/+np6f/n5+f/5OTk/+Li4v/g4OD/3d3d/7Kurf8aBgPV{A5b}aBgNjGgYDtP" +
                        "n5+f/IxMP/9fX1//Ly8v/w8PD/7u7u/+vr6//p6en/5ubm/+Tk5P/i4uL/39/f/xoGA94a" +
                        "BgMw{A50}GgYDNhoGA7rm5OT//Pz8/4N5d//4+Pj/9vb2//T09P/y8vL/8PDw/+3t7f/r6" +
                        "+v/6Ojo/+bm5v/j4+P/GgYDhxoGA4c{A50}aBgO66Obl{/7}+/v7/GgYD//v7+//6+vr/+" +
                        "Pj4//b29v/09PT/8vLy/+/v7//t7e3/6urq/+jo6P+6trX/GgYD1Q{A4f}BoGA//o5uX/6" +
                        "Obl/xoGA7oaBgP//v7+//39/f/7+/v/+fn5//j4+P/19fX/8/Pz//Hx8f/v7+//7Ozs/+r" +
                        "q6v8aBgP/{A50}GgYDuhoGA/8aBgO6GgYDNhoGA{/d}7+/v/8/Pz/+/v7//n5+f/39/f/9" +
                        "fX1//Pz8//x8fH/7u7u/xoGA/8{A65}GgYD{/17}9/f3//Pz8//r6+v/5+fn/GgYD//X19" +
                        "f/y8vL/GgYD/w{A65}aBgP{/c}Pysr{/b}8aBgP//f39//z8/P8aBgP/+Pj4//b29v8aBg" +
                        "P/{A60}GgYDMDgmJP{/b}4Z7ev{/b}xoGA{/8}v7+/xoGA//7+/v/+vr6/xoGA/8{A60}a" +
                        "BgOHhnt6{/c}QC8t{/c}GgYD{/c}GgYD//7+/v/9/f3/GgYD/w{A5f}BoGA9XPysr{/6}8" +
                        "/Kyv8aBgP{/b}8aBgP{/b}8aBgP{/6}8/Kyv8aBgO6{A60}GgYD{/c}GgYDhxoGA{/c}xo" +
                        "GA{/c}xoGA//Pysr/GgYDuhoGAxo{A60}aBgP{/b}8aBgOHGgYD{/c}GgYD{/7}Pysr/Gg" +
                        "YD/xoGA7oaBgMa{A65}BoGA7r{/a}xoGA4caBgP{/b}8aBgP/GgYD/xoGA7oaBgM2{A70}" +
                        "GgYDNhoGA7oaBgO6GgYDNhoGA7r{/a}xoGA7o{A95}GgYDNhoGA7oaBgO6GgYDNg{A84a}" +
                        "//AD///gA///wAH//4AB//+AAf//AAD//gAA//4AAP/+AAD//gAA///gAP//4AD//8AA//" +
                        "/AAP//wAD//8AA///AAf//wAf//8A////8P{/41}8=");
                }

                return m_HandCursor;
            }
        }

        static Cursor m_ZoomInCursor = null;
        public static Cursor ZoomInCursor
        {
            get
            {
                if (m_ZoomInCursor == null)
                {
                    m_ZoomInCursor = CursorFromString(
                        "AAACAAEAEBAAAAYABgBoBAAAFgAAACgAAAAQAAAAI{A5}EAI{A67}CQkJTh4ZHjQ{A49}a" +
                        "DhtqPxCU2n6AAAAJ{A3f}aDpuqfwiVo3/RlRn6wAAACQ{A39}aDduqfwnWpH/P1Vw8SMuO" +
                        "1Q{A15}lpGNHZaRjXCWkY23lpGN6paRjeqVkIy4SmqIzDZtqfosXpb/Rl14+SMuO1Q{A15" +
                        "}l5KOPpaRjbChnZn/yse//83Jw//Oy8j/0MzH/6ymo/9mg6P8Rl14+Sw2Q1g{A15}lZGNI" +
                        "ZaRjbCwqqL/1NHD8efj2N348+rT+PLr0+nk3N/X0srxv7Wy/3B4hNGXkoxv{A10}mZmZA5" +
                        "aRjXCfmpT/18678fPl1dT/8+jTwWUQ/7NWDP//9OrM9Ord1trPx/KupKL/lpGNoQ{A14}C" +
                        "WkY23wLOj/+jZw97/8OHV//Ll1NuAF//PdBT///Llzf/z5s3q4NPg1cK8/5aRjbs{A15}l" +
                        "pGN6s+6oP/55tDVwmcT/890Fv/afxj/34QZ/9uAF//PdBT/+e3e1tnKv/+WkY3q{A15}Ja" +
                        "RjerQuJr/+uTL2K1SD/+1WhH/wmcT/890Fv/afxj/34QZ//ns3Nnayrz/lpGN6g{A14}CW" +
                        "kY23vqiQ/+/ZvOb/5MnZ/+PI161SD/+1WhH//+bO0//oz9Lr2sTj1sOz/5aRjbc{A15}lp" +
                        "GNcKGVif/q0rD4+u/h7f/s2OetUg//rVIP///r1uH56NXm6N3M97GooP+WkY1w{A15}JaR" +
                        "jR2WkY2wr56M/+nYwPv15tf1/e7d7/3u3O336trx7eDR+b+0qv+WkY2wlpGNHQ{A1a}lpG" +
                        "NOZaRjbCil43/vK+h/+HNuP/o18P/y8C0/6yjmv+WkY2wlpGNOQ{A24}CWkY0dlpGNcJaR" +
                        "jbeWkY3qlpGN6paRjbeWkY1wlpGNHQ{A1a}//kAAP/wAAD/4AAA/8EAAOADAADABwAAgAc" +
                        "AAAAHAACABwAAgAcAAIAHAACABwAAgAcAAIAHAADADwAA4B8AAA==");
                }

                return m_ZoomInCursor;
            }
        }

        static Cursor m_ZoomOutCursor = null;
        public static Cursor ZoomOutCursor
        {
            get
            {
                if (m_ZoomOutCursor == null)
                {
                    m_ZoomOutCursor = CursorFromString(
                        "AAACAAEAEBAAAAYABgBoBAAAFgAAACgAAAAQAAAAI{A5}EAI{A67}CQkJTh4ZHjQ{A49}a" +
                        "DhtqPxCU2n6AAAAJ{A3f}aDpuqfwiVo3/RlRn6wAAACQ{A39}aDduqfwnWpH/P1Vw8SMuO" +
                        "1Q{A15}lpGNHZaRjXCWkY23lpGN6paRjeqVkIy4SmqIzDZtqfosXpb/Rl14+SMuO1Q{A15" +
                        "}l5KOPpaRjbChnZn/yse//83Jw//Oy8j/0MzH/6ymo/9mg6P8Rl14+Sw2Q1g{A15}lZGNI" +
                        "ZaRjbCwqqL/1NHD8efj2N348+rT+PLr0+nk3N/X0srxv7Wy/3B4hNGXkoxv{A10}mZmZA5" +
                        "aRjXCfmpT/18678fPl1dT/8+jT//Po///z6P//9OrM9Ord1trPx/KupKL/lpGNoQ{A14}C" +
                        "WkY23wLOj/+jZw97/8OHV//Ll1P/y5f//8uX///Llzf/z5s3q4NPg1cK8/5aRjbs{A15}l" +
                        "pGN6s+6oP/55tDVwmcT/890Fv/afxj/34QZ/9uAF//PdBT/+e3e1tnKv/+WkY3q{A15}Ja" +
                        "RjerQuJr/+uTL2K1SD/+1WhH/wmcT/890Fv/afxj/34QZ//ns3Nnayrz/lpGN6g{A14}CW" +
                        "kY23vqiQ/+/ZvOb/5MnZ/+PI1//jyP//48j//+bO0//oz9Lr2sTj1sOz/5aRjbc{A15}lp" +
                        "GNcKGVif/q0rD4+u/h7f/s2Of/7Nj//+zY///r1uH56NXm6N3M97GooP+WkY1w{A15}JaR" +
                        "jR2WkY2wr56M/+nYwPv15tf1/e7d7/3u3O336trx7eDR+b+0qv+WkY2wlpGNHQ{A1a}lpG" +
                        "NOZaRjbCil43/vK+h/+HNuP/o18P/y8C0/6yjmv+WkY2wlpGNOQ{A24}CWkY0dlpGNcJaR" +
                        "jbeWkY3qlpGN6paRjbeWkY1wlpGNHQ{A1a}//kAAP/wAAD/4AAA/8EAAOADAADABwAAgAc" +
                        "AAAAHAACABwAAgAcAAIAHAACABwAAgAcAAIAHAADADwAA4B8AAA==");
                }

                return m_ZoomOutCursor;
            }
        }

        static Cursor CursorFromString(string data)
        {
            byte[] bits = Convert.FromBase64String(
                        Regex.Replace(data,
                            "\\{(.)([0-9a-f]+)\\}",
                            delegate(Match m)
                            {
                                return new string(
                                    m.Groups[1].Value[0],
                                    int.Parse(m.Groups[2].Value,
                                        System.Globalization.NumberStyles.HexNumber));
                            }
                        )
                    );

            bits[2] = 1;

            using (MemoryStream stream = new MemoryStream(bits))
            {
                using (Icon icon = new Icon(stream))
                {
                    WinAPI.ICONINFO info = new WinAPI.ICONINFO();
                    WinAPI.GetIconInfo(icon.Handle, out info);

                    info.fIcon = false;
                    info.xHotspot = bits[10];
                    info.yHotspot = bits[12];

                    IntPtr hCursor = WinAPI.CreateIconIndirect(ref info);

                    Cursor ret = new Cursor(hCursor);

                    return ret;
                }
            }
        }

        #endregion
        #region Base form stuff
        VScrollBar m_vscroll;
        HScrollBar m_hscroll;
        HolePictureBox m_hole;
        ContextMenuStrip m_context;
        ToolStripMenuItem m_actualPixelsMenuItem;
        ToolStripMenuItem m_saveImageMenuItem;
        PictureBox m_preview;
        RectangleF m_scaledRect;

        private void InitializeComponent()
        {
            m_vscroll = new VScrollBar();
            m_hscroll = new HScrollBar();
            m_hole = new HolePictureBox();
            m_context = new ContextMenuStrip();
            m_actualPixelsMenuItem = new ToolStripMenuItem();
            m_saveImageMenuItem = new ToolStripMenuItem();
            m_preview = new PictureBox();

            ((ISupportInitialize)(m_hole)).BeginInit();
            m_context.SuspendLayout();
            ((ISupportInitialize)(m_preview)).BeginInit();
            SuspendLayout();

            m_vscroll.TabIndex = 0;
            m_vscroll.Scroll += new ScrollEventHandler(VScroll_Scroll);

            m_hscroll.TabIndex = 1;
            m_hscroll.Scroll += new ScrollEventHandler(HScroll_Scroll);

            m_hole.Cursor = Cursors.SizeNWSE;
            m_hole.TabIndex = 2;
            m_hole.TabStop = false;
            m_hole.Paint += new PaintEventHandler(Hole_Paint);

            m_context.Items.AddRange(new ToolStripItem[] 
            {
                m_actualPixelsMenuItem,
                new ToolStripSeparator(),                
                m_saveImageMenuItem,
            });

            m_actualPixelsMenuItem.Text = "Actual pixels";
            m_actualPixelsMenuItem.Click += new EventHandler(ActualPixelsMenuItem_Click);

            m_saveImageMenuItem.Text = "Save image...";
            m_saveImageMenuItem.Click += new EventHandler(SaveImageMenuItem_Click);

            m_showActualPixels = false;

            m_preview.Dock = DockStyle.Fill;
            m_preview.TabIndex = 2;
            m_preview.TabStop = false;
            m_preview.MouseMove += new MouseEventHandler(Preview_MouseMove);
            m_preview.MouseDown += new MouseEventHandler(Preview_MouseDown);
            m_preview.Paint += new PaintEventHandler(Preview_Paint);
            m_preview.MouseUp += new MouseEventHandler(Preview_MouseUp);

            ClientSize = new Size(340, 279);
            ContextMenuStrip = m_context;
            Controls.Add(m_hole);
            Controls.Add(m_hscroll);
            Controls.Add(m_vscroll);
            Controls.Add(m_preview);
            DoubleBuffered = true;
            KeyPreview = true;
            Text = "Preview";

            Load += new EventHandler(PreviewForm_Load);
            ResizeBegin += new EventHandler(PreviewForm_ResizeBegin);
            Resize += new EventHandler(PreviewForm_Resize);
            KeyDown += new KeyEventHandler(PreviewForm_KeyDown);
            ResizeEnd += new EventHandler(PreviewForm_ResizeEnd);
            ((ISupportInitialize)(m_hole)).EndInit();
            m_context.ResumeLayout(false);
            ((ISupportInitialize)(m_preview)).EndInit();
            ResumeLayout(false);
        }
        #endregion

        Bitmap m_image;
        bool m_mouseDown = false;
        bool m_inResize = false;
        bool m_showActualPixels = false;
        Point m_lastPoint;

        public Bitmap Bitmap
        {
            get
            {
                return m_image;
            }
            set
            {
                m_image = value;
                Invalidate();
            }
        }

        public PreviewForm(Icon icon, Bitmap image)
        {
            m_image = image;

            InitializeComponent();

            try
            {
                Icon = icon;
            }
            catch { }
        }

        void PreviewForm_Resize(object sender, EventArgs e)
        {
            SetupScrollBars(null);

            m_preview.Invalidate();

            m_hole.Invalidate();

            if (WindowState == FormWindowState.Maximized)
            {
                m_hole.Cursor = Cursors.Default;
            }
            else
            {
                m_hole.Cursor = Cursors.SizeNWSE;
            }
        }

        void SetupScrollBars(object sender)
        {
            if (m_showActualPixels)
            {
                if ((ClientSize.Width - m_vscroll.Width) > 1 &&
                    (ClientSize.Height - m_hscroll.Height) > 1)
                {
                    m_hscroll.Width = ClientSize.Width - m_vscroll.Width;
                    m_hscroll.Left = 0;
                    m_hscroll.Top = ClientSize.Height - m_hscroll.Height;

                    m_vscroll.Height = ClientSize.Height - m_hscroll.Height;
                    m_vscroll.Top = 0;
                    m_vscroll.Left = ClientSize.Width - m_vscroll.Width;

                    m_hole.Left = m_vscroll.Left;
                    m_hole.Width = m_vscroll.Width;
                    m_hole.Top = m_hscroll.Top;
                    m_hole.Height = m_hscroll.Height;

                    bool showHScroll = true;
                    bool showVScroll = true;

                    while (true)
                    {
                        if (showHScroll)
                        {
                            m_hscroll.Minimum = 0;
                            m_hscroll.LargeChange = ClientSize.Width - (showVScroll ? m_vscroll.Width : 0);
                            m_hscroll.SmallChange = 1;
                            if (m_image.Width - (ClientSize.Width - (showVScroll ? m_vscroll.Width : 0) - 1) < 0)
                            {
                                m_hscroll.Maximum = 0;
                                m_hscroll.Enabled = false;
                                showHScroll = false;
                                continue;
                            }
                            else
                            {
                                m_hscroll.Maximum = m_image.Width - m_hscroll.SmallChange;
                                m_hscroll.Enabled = true;
                            }
                        }

                        if (showVScroll)
                        {
                            m_vscroll.Minimum = 0;
                            m_vscroll.LargeChange = ClientSize.Height - (showHScroll ? m_hscroll.Height : 0);
                            m_vscroll.SmallChange = 1;

                            if (m_image.Height - (ClientSize.Height - (showHScroll ? m_hscroll.Height : 0)) < 0)
                            {
                                m_vscroll.Maximum = 0;
                                m_vscroll.Enabled = false;
                                showVScroll = false;
                                continue;
                            }
                            else
                            {
                                m_vscroll.Maximum = m_image.Height - m_vscroll.SmallChange;
                                m_vscroll.Enabled = true;
                            }
                        }

                        break;
                    }

                    if (showHScroll)
                    {
                        int value = m_hscroll.Value;

                        if (sender != null && sender is Point)
                        {
                            Point temp = (Point)sender;
                            value = (int)(((float)(temp.X - m_scaledRect.X) / (float)m_scaledRect.Width) * (float)m_image.Width);
                            value -= ClientSize.Width / 2;
                        }

                        value = Math.Min(value, m_hscroll.Maximum - m_hscroll.LargeChange + m_hscroll.SmallChange);
                        m_hscroll.Value = Math.Max(0, value);
                    }

                    if (showVScroll)
                    {
                        int value = m_vscroll.Value;

                        if (sender != null && sender is Point)
                        {
                            Point temp = (Point)sender;
                            value = (int)(((float)(temp.Y - m_scaledRect.Y) / (float)m_scaledRect.Height) * (float)m_image.Height);
                            value -= ClientSize.Height / 2;
                        }

                        value = Math.Min(value, m_vscroll.Maximum - m_vscroll.LargeChange + m_vscroll.SmallChange);
                        m_vscroll.Value = Math.Max(0, value);
                    }

                    if (!showHScroll)
                    {
                        m_vscroll.Height = ClientSize.Height;
                    }

                    if (!showVScroll)
                    {
                        m_hscroll.Width = ClientSize.Width;
                    }

                    m_vscroll.Visible = showVScroll;
                    m_hscroll.Visible = showHScroll;

                    m_hole.Visible = (showVScroll & showHScroll);

                    if (!m_inResize)
                    {
                        if (showVScroll | showHScroll)
                        {
                            m_preview.Cursor = HandCursor;
                        }
                        else
                        {
                            m_preview.Cursor = Cursors.Default;
                        }
                    }

                    m_preview.Visible = true;
                }
                else
                {
                    m_vscroll.Visible = false;
                    m_hscroll.Visible = false;
                    m_hole.Visible = false;
                    m_preview.Visible = false;
                }
            }
            else
            {
                if (!m_inResize)
                {
                    m_preview.Cursor = ZoomInCursor;
                }

                m_vscroll.Visible = false;
                m_hscroll.Visible = false;
                m_hole.Visible = false;
            }
        }

        void PreviewForm_Load(object sender, EventArgs e)
        {
            SetupScrollBars(null);

            m_hole.ParentHandle = Handle;
        }

        void VScroll_Scroll(object sender, ScrollEventArgs e)
        {
            m_preview.Invalidate();
        }

        void HScroll_Scroll(object sender, ScrollEventArgs e)
        {
            m_preview.Invalidate();
        }

        void SaveImageMenuItem_Click(object sender, EventArgs e)
        {
            string file = "";
            int index = 1;

            do
            {
                file = string.Format("Preview_{0}.png", index);
                index++;
            } while (File.Exists(file));

            m_image.Save(file);

            MessageBox.Show(
                "Image saved to \"" + file + "\".",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        void ActualPixelsMenuItem_Click(object sender, EventArgs e)
        {
            m_showActualPixels = !m_showActualPixels;

            if (m_showActualPixels)
            {
                m_actualPixelsMenuItem.Text = "Fit to window";
            }
            else
            {
                m_actualPixelsMenuItem.Text = "Actual pixels";
            }

            SetupScrollBars(sender);

            m_preview.Invalidate();
        }

        void MoveImage(MouseEventArgs e)
        {
            int newV = m_vscroll.Value + m_lastPoint.Y - e.Y;
            int newH = m_hscroll.Value + m_lastPoint.X - e.X;

            newV = Math.Min(newV, m_vscroll.Maximum - m_vscroll.LargeChange + m_vscroll.SmallChange);
            newH = Math.Min(newH, m_hscroll.Maximum - m_hscroll.LargeChange + m_hscroll.SmallChange);

            newV = Math.Max(0, newV);
            newH = Math.Max(0, newH);

            m_vscroll.Value = newV;
            m_hscroll.Value = newH;

            m_lastPoint = e.Location;

            m_preview.Invalidate();
        }

        void PreviewForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control == true)
            {
                switch (e.KeyCode)
                {
                    case Keys.Down:
                        m_vscroll.Value = Math.Min(m_vscroll.Value + m_vscroll.SmallChange, m_vscroll.Maximum - m_vscroll.LargeChange);
                        e.Handled = true;
                        break;
                    case Keys.Up:
                        m_vscroll.Value = Math.Max(m_vscroll.Value - m_vscroll.SmallChange, m_vscroll.Minimum);
                        e.Handled = true;
                        break;
                    case Keys.Left:
                        m_hscroll.Value = Math.Max(m_hscroll.Value - m_hscroll.SmallChange, m_hscroll.Minimum);
                        e.Handled = true;
                        break;
                    case Keys.Right:
                        m_hscroll.Value = Math.Min(m_hscroll.Value + m_hscroll.SmallChange, m_hscroll.Maximum - m_hscroll.LargeChange);
                        e.Handled = true;
                        break;
                }
            }
            else
            {
                switch (e.KeyCode)
                {
                    case Keys.Down:
                        m_vscroll.Value = Math.Min(m_vscroll.Value + m_vscroll.LargeChange / 2, m_vscroll.Maximum - m_vscroll.LargeChange + 1);
                        e.Handled = true;
                        break;
                    case Keys.Up:
                        m_vscroll.Value = Math.Max(m_vscroll.Value - m_vscroll.LargeChange / 2, m_vscroll.Minimum);
                        e.Handled = true;
                        break;
                    case Keys.Left:
                        m_hscroll.Value = Math.Max(m_hscroll.Value - m_hscroll.LargeChange / 2, m_hscroll.Minimum);
                        e.Handled = true;
                        break;
                    case Keys.Right:
                        m_hscroll.Value = Math.Min(m_hscroll.Value + m_hscroll.LargeChange / 2, m_hscroll.Maximum - m_hscroll.LargeChange + 1);
                        e.Handled = true;
                        break;
                }
            }
        }

        void Preview_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                Graphics g = e.Graphics;

                g.Clear(SystemColors.Control);

                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                if (m_showActualPixels)
                {
                    int nOffX = 0;
                    int nOffY = 0;

                    if (ClientSize.Width - (m_vscroll.Visible ? m_vscroll.Width : 0) > m_image.Width)
                    {
                        nOffX = (ClientSize.Width - (m_vscroll.Visible ? m_vscroll.Width : 0) - m_image.Width) / 2;
                    }

                    if (ClientSize.Height - (m_hscroll.Visible ? m_hscroll.Height : 0) > m_image.Height)
                    {
                        nOffY = (ClientSize.Height - (m_hscroll.Visible ? m_hscroll.Height : 0) - m_image.Height) / 2;
                    }

                    if (nOffX >= 4 && nOffY >= 4)
                    {
                        RectangleF rectBig = new RectangleF(
                            -m_hscroll.Value + nOffX, -m_vscroll.Value + nOffY,
                            m_image.Width, m_image.Height);

                        rectBig.Inflate(2.0f, 2.0f);

                        g.FillRectangle(SystemBrushes.ControlDark, rectBig);

                        rectBig.Inflate(-2.0f, -2.0f);

                        using (Bitmap bitmap = new Bitmap(16, 16, PixelFormat.Format24bppRgb))
                        {
                            using (Brush brush = new SolidBrush(Color.FromArgb(204, 204, 204)))
                            using (Graphics graphics = Graphics.FromImage(bitmap))
                            {
                                graphics.Clear(Color.FromArgb(255, 255, 255));
                                graphics.FillRectangle(brush, 8, 0, 8, 8);
                                graphics.FillRectangle(brush, 0, 8, 8, 8);
                            }

                            using (Brush brush = new TextureBrush(bitmap))
                            {
                                e.Graphics.FillRectangle(brush, rectBig);
                            }
                        }
                    }
                    else
                    {
                        using (Bitmap bitmap = new Bitmap(16, 16, PixelFormat.Format24bppRgb))
                        {
                            using (Brush brush = new SolidBrush(Color.FromArgb(204, 204, 204)))
                            using (Graphics graphics = Graphics.FromImage(bitmap))
                            {
                                graphics.Clear(Color.FromArgb(255, 255, 255));
                                graphics.FillRectangle(brush, 8, 0, 8, 8);
                                graphics.FillRectangle(brush, 0, 8, 8, 8);
                            }

                            using (Brush brush = new TextureBrush(bitmap))
                            {
                                Rectangle temp = new Rectangle(
                                    -m_hscroll.Value + nOffX, -m_vscroll.Value + nOffY,
                                    m_image.Width, m_image.Height);

                                if (temp.X < 0)
                                {
                                    temp.Width += temp.X;
                                    temp.X = 0;
                                }

                                if (temp.Y < 0)
                                {
                                    temp.Height += temp.Y;
                                    temp.Y = 0;
                                }

                                e.Graphics.FillRectangle(brush, temp);
                            }
                        }
                    }

                    g.DrawImage(m_image, -m_hscroll.Value + nOffX, -m_vscroll.Value + nOffY);
                }
                else
                {
                    if (ClientSize.Width >= 25 &&
                        ClientSize.Height >= 25)
                    {
                        double scale = Math.Min(
                            ((double)(ClientSize.Width - 8)) / ((double)m_image.Width),
                            ((double)(ClientSize.Height - 8)) / ((double)m_image.Height));

                        if (scale > 1.0)
                        {
                            scale = 1.0;
                        }

                        SizeF newSize = new SizeF(
                            (float)(((double)m_image.Width) * scale),
                            (float)(((double)m_image.Height) * scale));

                        RectangleF rect = new RectangleF(
                            ((float)ClientSize.Width - newSize.Width) / 2f,
                            ((float)ClientSize.Height - newSize.Height) / 2f,
                            newSize.Width, newSize.Height);

                        RectangleF rectBig = rect;
                        rectBig.Inflate(2.0f, 2.0f);

                        g.FillRectangle(SystemBrushes.ControlDark, rectBig);

                        rectBig.Inflate(-2.5f, -2.5f);

                        using (Bitmap bitmap = new Bitmap(16, 16, PixelFormat.Format24bppRgb))
                        {
                            using (Brush brush = new SolidBrush(Color.FromArgb(204, 204, 204)))
                            using (Graphics graphics = Graphics.FromImage(bitmap))
                            {
                                graphics.Clear(Color.FromArgb(255, 255, 255));
                                graphics.FillRectangle(brush, 8, 0, 8, 8);
                                graphics.FillRectangle(brush, 0, 8, 8, 8);
                            }

                            using (Brush brush = new TextureBrush(bitmap))
                            {
                                e.Graphics.FillRectangle(brush, rectBig);
                            }
                        }

                        m_scaledRect = rect;

                        g.DrawImage(m_image, rect);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
            }
        }

        void Preview_MouseUp(object sender, MouseEventArgs e)
        {
            if (m_mouseDown)
            {
                m_preview.Cursor = HandCursor;

                MoveImage(e);
                m_mouseDown = false;
            }
        }

        void Preview_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_mouseDown)
            {
                MoveImage(e);
            }
        }

        void Preview_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (m_showActualPixels)
                {
                    if (m_vscroll.Visible | m_hscroll.Visible)
                    {
                        m_preview.Cursor = HandGrabCursor;

                        m_lastPoint = e.Location;
                        m_mouseDown = true;
                    }
                }
                else
                {
                    ActualPixelsMenuItem_Click(e.Location, EventArgs.Empty);
                }
            }
        }

        void PreviewForm_ResizeBegin(object sender, EventArgs e)
        {
            m_inResize = true;
        }

        void PreviewForm_ResizeEnd(object sender, EventArgs e)
        {
            m_inResize = false;

            SetupScrollBars(null);

            m_preview.Invalidate();
        }

        void Hole_Paint(object sender, PaintEventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                e.Graphics.Clear(BackColor);
            }
            else
            {
                int count = 3;
                int y = 0;

                using (Brush one = new SolidBrush(Color.FromArgb(
                    Math.Max(BackColor.R - 16, 0),
                    Math.Max(BackColor.G - 16, 0),
                    Math.Max(BackColor.B - 16, 0))))
                using (Brush two = new SolidBrush(Color.FromArgb(
                    Math.Min(BackColor.R + 25, 255),
                    Math.Min(BackColor.G + 25, 255),
                    Math.Min(BackColor.B + 25, 255))))
                {
                    while (count > 0)
                    {
                        for (int x = 0; x < count; x++)
                        {
                            e.Graphics.FillRectangle(one,
                                m_vscroll.Width - 4 * x - 4,
                                m_hscroll.Height - 4 * y - 4,
                                2,
                                2);

                            e.Graphics.FillRectangle(two,
                                m_vscroll.Width - 4 * x - 3,
                                m_hscroll.Height - 4 * y - 3,
                                2,
                                2);
                        }

                        count--;
                        y++;
                    }
                }
            }
        }
    }
}