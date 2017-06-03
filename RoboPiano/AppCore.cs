using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using LuaInterface;
using System.IO;
using System.Drawing;
using System.Globalization;

namespace RoboPiano
{
    class AppCore
    {
        Form1 f1;

        public GraphicsHelper gh;
        public Manipulator m;
        List<PointF> pts = new List<PointF>();

       public GraphicsHelper gh_back;


        public void XPTN(string msg)
        {
            f1.SetTimerState(false);
            MessageBox.Show(msg);
        }

        #region Lua
        public void RunLua(string str)
        {
            try
            {
                lua.DoString(str);
            }
            catch (Exception e1)
            {
                var e = e1 as LuaException;
                if (e == null) return;
                var s = e.Message;
                if (e.InnerException != null) s += "\r\n" + e.InnerException;
                if (e.StackTrace != null) s += "\r\n" + e.StackTrace;
                XPTN(s);
            }
        }
        Lua lua;//basic lua functionality
        internal void ResetLua()
        {
            lua = new Lua();
            RegisterFunctions();
            load_scripts();
        }
        public void load_scripts()
        {
            try
            {
                foreach (string st in Directory.GetFiles(Form1.scripts, "*.lua", SearchOption.TopDirectoryOnly))
                {
                    lua.DoFile(st);
                }
            }
            catch (Exception ex)
            {
                XPTN("Lua Error: " + ex.Message);
            }
        }
        void rf(string function_name)
        {
            lua.RegisterFunction(function_name, this, this.GetType().GetMethod(function_name));
        }
        void RegisterFunctions()
        {
            rf("paint");
            rf("reset");
            rf("set_params");
            rf("init_manipulator");

            rf("getA");
            rf("getL");
            rf("getT");
            rf("setA");
            rf("setL");

            rf("debug");

        }
        #endregion

        public void Init(Form1 f1)
        {
            this.f1 = f1;
            ResetLua();

            InitGraphics();
        }

        public void InitGraphics()
        {
            gh = new GraphicsHelper();
            gh.Init(f1.pb);
            gh.Clear();

            gh_back = new GraphicsHelper();
            gh_back.InitInnerImage(f1.pb.Width, f1.pb.Height);
            gh_back.Clear();

            CenterScreen();
        }

        public float t = 0;
       
        #region Methods

        void xptn(Exception e)
        {
            var s = e.Message;
            if (e.InnerException != null) s += "\r\n" + e.InnerException;
            if (e.StackTrace != null) s += "\r\n" + e.StackTrace;
            MessageBox.Show(s);
        }

        public class SimParams
        {
            public bool draw_manipulator = true;
            public bool draw_path = false;
            public bool clear_manipulator = false;
            public int speed = 3;
            public int fade = 0;
            public int thickness = 2;
        }

        SimParams sp=new SimParams();

        public void paint()
        {
            gh.thickness = gh_back.thickness = sp.thickness;

            if (m == null) return;

            if (sp.clear_manipulator) gh.Clear();          

            for (int i = 0; i < sp.speed; i++)
            {
                bool last = i == sp.speed - 1;

                var str_t = t.ToString(CultureInfo.InvariantCulture);
                RunLua(motion_func+"("+str_t+")");

                m.Simulate();

                if (sp.draw_path)
                {
                    pts.Add(m.LastLink.getAbsP1());
                    if (pts.Count > 1)
                    {
                        gh_back.G.DrawLines(gh.GetPen(Color.Black), pts.ToArray());
                        pts.RemoveAt(0);
                    }
                }
              
                if (last)
                {
                    gh.DrawImage(gh_back.GetBitmap());

                    if (sp.fade > 0) gh_back.FadeImage(Color.FromArgb(sp.fade, 255, 255, 255));
                }

                if (sp.draw_manipulator)
                {
                    if (sp.clear_manipulator && last)
                    {
                        m.Draw(gh);
                    }
                    else if (!sp.clear_manipulator)
                    {
                        m.Draw(gh_back);
                    }
                }

                t += f1.TimerIntervalSec();
            }

            gh.UpdateGraphics();
        }

        public void reset()
        {
            t = 0;
            gh.Clear();
            gh_back.Clear();
            pts.Clear();

            ResetLua();
        }

        public void set_params(LuaTable p0)
        {
            var p = new SimParams();
            var p_obj = (object)p;
            Helper.InitParamsFromLuaTable(p_obj, p0);
            sp = (SimParams)p_obj;
        }

        public void CenterScreen()
        {
            gh.ShiftScreen(new PointF(f1.pb.Width / 2, f1.pb.Height / 2));
            gh_back.ShiftScreen(new PointF(f1.pb.Width / 2, f1.pb.Height / 2));
        }
        string motion_func;
        public void init_manipulator(LuaTable lengths, string motion_func_, LuaTable draw_params)
        {
            m = new Manipulator();
            //var pos0 = gh.ConvertMousePointToWorld(new PointF(f1.pb.Width / 2, f1.pb.Height / 2));
            var pos0 = new PointF(0, 0);

            Link L_prev = null;
            for (int i = 1; i <= lengths.Keys.Count; i++)
            {
                var pos = new float2();
                if (i == 1) pos = pos0;

                var L = new Link(Convert.ToSingle(lengths[i]), pos, GraphicsHelper.IntToColor(i-1, 7), L_prev);
                m.links.Add(L);

                L_prev = L;
            }
            //----------
            motion_func = motion_func_;
            //----------
            var p = new SimParams();
            var p_obj = (object)p;
            Helper.InitParamsFromLuaTable(p_obj, draw_params);
            sp = (SimParams)p_obj;
        }

        public float getA(int ind)
        {
            return m.links[ind].ang_d;
        }
        public float getL(int ind)
        {
            return m.links[ind].L;
        }
        public float getT()
        {
            return t;
        }
        public void setA(int ind, float x)
        {
            m.links[ind].ang_d=x;
        }
        public void setL(int ind, float x)
        {
            m.links[ind].L=x;
        }

        public void clear_graphics()
        {
            gh.Clear();
        }

        public void update_graphics()
        {
            gh.UpdateGraphics();
            gh_back.UpdateGraphics();
        }

        public void debug(string x)
        {
            MessageBox.Show(x);
        }

        #endregion

    }
}
