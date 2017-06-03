using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO;
using Sanford.Multimedia.Midi;

namespace RoboPiano
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public PictureBox pb { get { return pictureBox1; } }

        AppCore ac;


        void clear_temp()
        {
            var di = new DirectoryInfo(temp);
            if(!di.Exists)Helper.CorrectPath(temp);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            clear_temp();

            ac = new AppCore();
            ac.Init(this);

            resize();
        }
           
        private void button1_Click(object sender, EventArgs e)
        {
            ac.reset();

#warning magic
            ac.RunLua("coeffs_mode=\"generate\"");

            ac.RunLua(richTextBox1.Text);
            timer1.Enabled = true;
        }

        public void SetTimerState(bool x)
        {
            timer1.Enabled = x;
        }
        public float TimerIntervalSec()
        {
            return timer1.Interval / 1000f;
        }

        float t111 = 0;
        bool need_resize = true;

        private void timer1_Tick(object sender, EventArgs e)
        {
            ac.paint();

            if(ac.m!=null) draw_piano(ac.gh, ac.m.SumLengths());

            if (need_resize && Helper.is_time(1000, timer1.Interval, ref t111))
            {
                need_resize = false;
                resize();
            }
        }

        public static string temp = "..\\Files\\Temp\\";
        public static string scripts = "..\\Files\\Scripts\\";
        public static string demo = "..\\Files\\Demo\\";


        string[] FilesInTempFolder()
        {
            DirectoryInfo di = new DirectoryInfo(temp);
            return Array.ConvertAll(di.GetFiles(), x => x.FullName);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            var sfd = new SaveFileDialog();
            sfd.Filter = szip;
            sfd.RestoreDirectory = true;

            using (var w = new StreamWriter(temp + "script.lua", false))
            {
                w.Write(richTextBox1.Text);
            }
            pictureBox1.Image.Save(temp + "image.jpg", ImageFormat.Jpeg);

            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var files = FilesInTempFolder();
                Helper.ZipFiles(files, sfd.FileName, true);

                toolStripStatusLabel1.Text = "File saved: " + sfd.FileName;
            }

            timer1.Enabled = true;
        }

        string szip = "Zip archives (*.zip)|*.zip";
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            var ofd = new OpenFileDialog();
            ofd.Filter = szip;
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                clear_temp();

                open_file(ofd.FileName);
            }

            ac.reset();

#warning magic
            ac.RunLua("coeffs_mode=\"load\"");

            ac.RunLua(richTextBox1.Text);

            timer1.Enabled = true;
        }

        private void open_file(string name)
        {
            using (var r = new StreamReader(name, true))
            {
                Helper.UnZipFile(name, temp, true);

                using (var r1 = new StreamReader(temp + "script.lua", true))
                {
                    richTextBox1.Text = r1.ReadToEnd();
                }
            }

            toolStripStatusLabel1.Text="File opened: "+name;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var f2 = new Form2About();
            f2.ShowDialog();
        }
        
        private void Form1_Resize(object sender, EventArgs e)
        {
            need_resize = true;
        }

        public void resize()
        {
            pictureBox1.Width=this.Width-richTextBox1.Width-40;
            pictureBox1.Height = this.Height - menuStrip1.Height - 67;

            richTextBox1.Height = pictureBox1.Height - 60;

            var p=pictureBox1.Location;
            var sz=pictureBox1.Size;
            richTextBox1.Location=new Point(p.X+sz.Width+10, p.Y+20);

            var p1=richTextBox1.Location;

            button1.Location = new Point(p1.X + 5, p1.Y + richTextBox1.Height + 10);

            label1.Location = new Point(p1.X + 5, p1.Y - 16);

            ac.InitGraphics();
        }

        void load_example(int i)
        {
            ac.reset();

            open_file(demo + i + ".zip");

#warning magic
            ac.RunLua("coeffs_mode=\"load\"");

            ac.RunLua(richTextBox1.Text);

            timer1.Enabled = true;
        }

        private void ex1(object sender, EventArgs e)
        {
            load_example(1);
        }

        private void ex2(object sender, EventArgs e)
        {
            load_example(2);
        }

        private void ex3(object sender, EventArgs e)
        {
            load_example(3);
        }


        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            ac = new AppCore();
            ac.Init(this);
            timer1.Enabled = true;
        }

        private void example9randomizableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            load_example(9);
        }

        private OutputDevice outDevice= new OutputDevice(0);

        public class Note
        {
            public int id;
            public float life_time;
            public Note next;
        }

        List<Note> notes = new List<Note>();

        float time = 0.5f;
        void play(int note)  
        {
            var n = new Note { id = note, life_time = time };
            play(n);
        }

        void play(Note n) 
        {
            var ind = 21 + n.id % 88;
            outDevice.Send(new ChannelMessage(ChannelCommand.NoteOn, 0, ind, 127));
            notes.Add(n);
            timer2.Enabled = true;
        }

        void play(int[] notes) 
        {
            Note first = new Note { id = notes[0], life_time = time };
            Note prev = first;
            for (int i = 1; i < notes.Length; i++)
            {
                var curr = new Note { id = notes[i], life_time = time };
                prev.next = curr;
                prev = curr;
            }
            play(first);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            float dt = timer2.Interval / 1000f;
            for (int i = notes.Count - 1; i >= 0; i--)
            {
                var n = notes[i];
                n.life_time -= dt;
                if (n.life_time <= 0)
                {
                    var ind = 21 + n.id % 88;
                    outDevice.Send(new ChannelMessage(ChannelCommand.NoteOff, 0, ind, 0));
                    if (n.next != null) play(n.next);
                    notes.RemoveAt(i);
                }
            }
            if (notes.Count == 0)
            {
                timer2.Enabled = false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (outDevice != null)
            {
                outDevice.Dispose();
            }

        }

        float2 prev_p=null;
        float lastS = 0;
        float S = 0;

        int last_note=0;

        private void timer3_Tick(object sender, EventArgs e)
        {
            if (ac.m == null) return;

            var p = ac.m.getEndPoint();

            float dS = 0;
            if (prev_p != null) dS = (p - prev_p).Length();

            S += dS;

            var ang = Math.Atan2(p.Y, p.X);
            if(ang<0) ang+=2*Math.PI;
            var r = (float)Math.Sqrt(p.X * p.X + p.Y * p.Y);

            var maxR = ac.m.SumLengths();

            //if (S - lastS > 2)            {

                int note = (int)(12 * ang / Math.PI / 2.0001);

                int oct = (int)(8 * r / maxR/ 1.0001);

                var notes = new List<int>(new[] { 1, 3, 6, 8, 10 }); //skip black keys
                //if (notes.Contains(note)) return;

                int note_ind = oct * 12 + note;
                note_ind += 3; //skip first non-complete octave
                note_ind%=87;

            if(last_note!=note_ind){

                last_note = note_ind;

                play(note_ind);

                var np = note_discreete_pos(note, oct, maxR);
                ac.gh_back.DrawCircle(0.5f, np.X, np.Y, new Pen(Color.Green), true);

                lastS = S;
            }

            prev_p = p;

            _(ang, r);
        }
        void _(params object[] p) { }


        void draw_piano(GraphicsHelper gh, float maxR)
        {
            Pen p=new Pen(Color.Blue, 0.1f);
            for (float r = maxR*0.999f; r > 0; r -= maxR / 8)
            {
                gh.DrawCircle(r, 0, 0, p, false);
            }

            for (float a = 0; a<360; a+=360/12f)
            {
                var a1 = a /180* (float)Math.PI;
                float sin = (float)Math.Sin(a1), cos = (float)Math.Cos(a1);
                gh.G.DrawLine(p, 0, 0, maxR*cos, maxR*sin);
            }
        }

        float2 note_discreete_pos(int note, int octave, float maxR)
        {
            float a = 2 * (note+0.5f) * (float)Math.PI / 12f;
            float r = maxR/8*(octave+0.5f);
            float sin = (float)Math.Sin(a), cos = (float)Math.Cos(a);
            return new float2(r*cos, r*sin);

        }

    }
}
