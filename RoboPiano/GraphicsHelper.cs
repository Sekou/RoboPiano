using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace RoboPiano
{
    //содержит вспомогательные функции для прорисовки различных объектов
    public class GraphicsHelper
    {
        public Graphics G;//переменная доступа к низкоуровневым графическим функциям в C#
        PictureBox pb;//элемент формы, на который будет выводится графика

        public void Init(PictureBox pb_)
        {
            pb = pb_;
            pb.Image = new Bitmap(pb.Size.Width, pb.Size.Height);
            G = Graphics.FromImage(pb.Image);
            G.SmoothingMode = SmoothingMode.AntiAlias;
            UpdateTransform();
        }

        Bitmap bmp; //альтернативно графика может выводиться во внутреннее изображение

        public void InitInnerImage(int W, int H)
        {
            bmp = new Bitmap(W, H);
            G = Graphics.FromImage(bmp);
            G.SmoothingMode = SmoothingMode.AntiAlias;
            UpdateTransform();
        }

        public Bitmap GetBitmap()
        {
            if (pb != null) return (Bitmap)pb.Image;
            else return bmp;
        }

        public Transform tr = new Transform();
        #region view transform
        public class Transform
        {
            public float Scale = 10;//показывает сколько пикселей приходится на 1 метр.
            public float Tx = 0, Ty = 0;
            public float Angle_d = 0;

            public void UpdateTransform(Graphics G)
            {
                G.ResetTransform();
                G.TranslateTransform(Tx, Ty);
                G.ScaleTransform(Scale, Scale);
                G.RotateTransform(Angle_d);
            }
        }

        public void UpdateTransform()
        {
            Helper.NormalizeAngle(ref tr.Angle_d, 360);

            tr.UpdateTransform(G);
        }

        public float thickness = 2;

        public PointF ConvertMousePointToWorld(PointF pmouse)
        {
            var pts = new PointF[] { pmouse };
            var t = G.Transform.Clone(); t.Invert();
            t.TransformPoints(pts);

            return pts[0];
        }

        public PointF ConvertMouseVecToWorld(PointF pmouse)
        {
            var pts = new PointF[] { pmouse };
            var t = G.Transform.Clone(); t.Invert();
            t.TransformVectors(pts);

            return pts[0];
        }

        public PointF ConvertWorldPointToMouse(PointF pworld)
        {
            var pts = new PointF[] { pworld };
            G.Transform.TransformPoints(pts);

            return pts[0];
        }
        public PointF ConvertWorldVecToMouse(PointF pworld)
        {
            var pts = new PointF[] { pworld };
            G.Transform.TransformVectors(pts);

            return pts[0];
        }

        public void ShiftScreen(PointF delta)
        {
            tr.Tx += delta.X;
            tr.Ty += delta.Y;
            UpdateTransform();
        }
        public void ShiftWorld(PointF delta)
        {
            var d = ConvertWorldVecToMouse(delta);
            ShiftScreen(d);
        }

        public void ScaleAtMousePoint(PointF mouse, float scale, bool update_pens)
        {
            var p1 = new SizeF(ConvertMousePointToWorld(mouse));
            tr.Scale = scale;
            UpdateTransform();
            var p2 = ConvertMousePointToWorld(mouse);

            var d = p2 - p1;

            ShiftWorld(d);

            UpdateTransform();
        }

        public void RotateAtMousePoint(PointF mouse, float angle_d)
        {
            var p1 = new SizeF(ConvertMousePointToWorld(mouse));
            tr.Angle_d = angle_d;
            UpdateTransform();
            var p2 = ConvertMousePointToWorld(mouse);

            var d = p2 - p1;

            ShiftWorld(d);

            UpdateTransform();
        }
        #endregion

        public Pen GetPen(Color c)
        {
            return new Pen(c, thickness / tr.Scale);
        }

        //отображает нарисованную графику
        public void UpdateGraphics()
        {
            if(pb!=null) pb.Invalidate();
        }

        //очищает область прорисовки
        public void Clear()
        {
            G.Clear(System.Drawing.Color.White);
        }

        //рисует точку 
        public void DrawCircle(float R, float x, float y, Pen p, bool solid)
        {
            if (solid) G.FillEllipse(p.Brush, x - R, y - R, 2 * R, 2 * R);
            else G.DrawEllipse(p, x - R, y - R, 2 * R, 2 * R);
        }

        //рисует прямоугольник с левым верхним углом в точке {r.X, r.Y}, но повернутый вокруг него на угол alpha против часовой стрелки
        public void DrawRectangleA(float x0, float y0, float width, float height, float alpha_d, Pen p, bool reset_transform, bool solid)
        {
            if (reset_transform) Push();

            G.TranslateTransform(x0, y0);
            G.RotateTransform(alpha_d);

            if (solid) G.FillRectangle(new SolidBrush(p.Color), 0, 0, width, height);
            else G.DrawRectangle(p, 0, 0, width, height);

            if (reset_transform) Pop();
        }

        //рисует прямоугольник с геометрическим центром в точке {xc, yc}, но повернутый вокруг него на угол alpha против часовой стрелки
        public void DrawRectangleB(float xc, float yc, float width, float height, float alpha_d, Pen p, bool reset_transform, bool solid)
        {
            if (reset_transform) Push();

            var dx = width / 2;
            var dy = height / 2;

            G.TranslateTransform(xc, yc);
            G.RotateTransform(alpha_d);

            if (solid) G.FillRectangle(new SolidBrush(p.Color), -dx, -dy, width, height);
            else G.DrawRectangle(p, -dx, -dy, width, height);

            if (reset_transform) Pop();
        }

        Stack<System.Drawing.Drawing2D.Matrix> transformations = new Stack<System.Drawing.Drawing2D.Matrix>();
        //сохраняет текущюю трансформацию графики
        public void Push()
        {
            transformations.Push(G.Transform);
        }

        //восстанавливает сохраненную ранее трансформацию графики
        public void Pop()
        {
            G.Transform = transformations.Pop();
        }

        //отрисовка текста
        public void DrawText(string s, float xc, float yc, float sz, Brush b)
        {
            sz /= tr.Scale;
            float x = xc - s.Length * sz / 2, y = yc - sz / 2;
            var font = new Font(FontFamily.GenericMonospace, sz);
            G.DrawString(s, font, b, x, y);
        }


        /// <summary> 
        /// Creates a Color from alpha, hue, saturation and brightness.
        /// </summary>
        /// <param name="alpha">The alpha channel value.</param>
        /// <param name="hue">The hue value.</param>
        /// <param name="saturation">The saturation value.</param>
        /// <param name="brightness">The brightness value.</param>
        /// <returns>A Color with the given values.</returns>
        public static Color FromAhsb(int alpha, float hue, float saturation, float brightness)
        {
            if (0 > alpha || 255 < alpha) throw new ArgumentOutOfRangeException("alpha", alpha, "Value must be within a range of 0 - 255.");

            if (0f > hue || 360f < hue) throw new ArgumentOutOfRangeException("hue", hue, "Value must be within a range of 0 - 360.");

            if (0f > saturation || 1f < saturation) throw new ArgumentOutOfRangeException("saturation", saturation, "Value must be within a range of 0 - 1.");

            if (0f > brightness || 1f < brightness) throw new ArgumentOutOfRangeException("brightness", brightness, "Value must be within a range of 0 - 1.");

            if (0 == saturation) return Color.FromArgb(alpha, Convert.ToInt32(brightness * 255), Convert.ToInt32(brightness * 255), Convert.ToInt32(brightness * 255));

            float fMax, fMid, fMin;
            int iSextant, iMax, iMid, iMin;

            if (0.5 < brightness)
            {
                fMax = brightness - (brightness * saturation) + saturation;
                fMin = brightness + (brightness * saturation) - saturation;
            }
            else
            {
                fMax = brightness + (brightness * saturation);
                fMin = brightness - (brightness * saturation);
            }

            iSextant = (int)Math.Floor(hue / 60f);
            if (300f <= hue) hue -= 360f;

            hue /= 60f;
            hue -= 2f * (float)Math.Floor(((iSextant + 1f) % 6f) / 2f);
            if (0 == iSextant % 2)
            {
                fMid = (hue * (fMax - fMin)) + fMin;
            }
            else
            {
                fMid = fMin - (hue * (fMax - fMin));
            }

            iMax = Convert.ToInt32(fMax * 255); iMid = Convert.ToInt32(fMid * 255); iMin = Convert.ToInt32(fMin * 255);

            switch (iSextant)
            {
                case 1: return Color.FromArgb(alpha, iMid, iMax, iMin);
                case 2: return Color.FromArgb(alpha, iMin, iMax, iMid);
                case 3: return Color.FromArgb(alpha, iMin, iMid, iMax);
                case 4: return Color.FromArgb(alpha, iMid, iMin, iMax);
                case 5: return Color.FromArgb(alpha, iMax, iMin, iMid);
                default: return Color.FromArgb(alpha, iMax, iMid, iMin);
            }
        }

        public static Color IntToColor(int k, int num_types)
        {
            return FromAhsb(255, (k * 360 / num_types) % 360, 0.5f, 0.5f);
        }

        public int W { get { if (pb != null) return pb.Width; else return bmp.Width; } }
        public int H { get { if (pb != null) return pb.Height; else return bmp.Height; } }

        //рисует изображение (фоновое, например)
        public void DrawImage(Image img)
        {
            Push();
            G.ResetTransform();

            G.DrawImage(img, 0, 0);

            Pop();
        }

          //зтуманивает изображение
        public void FadeImage(Color c)
        {
            Push();
            G.ResetTransform();

            G.FillRectangle(new SolidBrush(c), 0, 0, W, H);

            Pop();
        }
    }
}