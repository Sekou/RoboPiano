
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace RoboPiano
{
    //универсальная точка или вектор
    public class float2
    {
        public float X, Y;
        public float2(float x_, float y_)
        { X = x_; Y = y_; }
        public void init(float x_, float y_)
        { X = x_; Y = y_; }
        public float2() : this(0, 0) { }

        public static float2 operator +(float2 v1, float2 v2)//сумма векторов
        { return new float2(v1.X + v2.X, v1.Y + v2.Y); }
        public static float2 operator -(float2 v1, float2 v2)//разность векторов
        { return new float2(v1.X - v2.X, v1.Y - v2.Y); }
        public static float2 operator *(float2 v1, float k)//умножение вектора на число
        { return new float2(v1.X * k, v1.Y * k); }
        public static float2 operator *(float k, float2 v1)//умножение числа на вектор
        { return v1 * k; }
        public static float2 operator /(float2 v1, float k)//деление вектора на число
        { return new float2(v1.X / k, v1.Y / k); }

        public float Length()
        { return (float)Math.Sqrt(X * X + Y * Y); }

        public bool Coinsides(float2 other)
        {
            return (X == other.X && Y == other.Y);
        }

        public static implicit operator PointF(float2 p)
        {
            return new PointF(p.X, p.Y);
        }

        public static implicit operator float2(PointF p)
        {
            return new float2(p.X, p.Y);
        }

    }

    public class Link
    {
        public float L; //длина звена
        public float2 P0_local; //вектор локального смещения
        public float2 P0_abs_cache; //вектор абсолютного смещения начальной точки
        public float2 P1_abs_cache; //вектор абсолютного смещения конечной точки
        public float ang_d; //поворот в градусах
        public Link prev; //предыдущее звено или null
        public Color color;

        public Link(float L_, float2 P0_local_, Color color_, Link prev_)
        {
            L = L_;
            P0_local = P0_local_;
            color = color_;
            prev = prev_; 
        }

        public float getAbsAng()
        {
            float res = ang_d;
            if (prev != null) res+=prev.getAbsAng();
            return res;
        }

        public float2 getAbsP0()
        {
            if (P0_abs_cache != null) return P0_abs_cache;

            if (prev != null) { P0_abs_cache = prev.getAbsP1(); return P0_abs_cache; }

            return P0_local;
        }

        float k_pi = (float)Math.PI / 180;
        public float2 getAbsP1()
        {
            if (P1_abs_cache != null) return P1_abs_cache;

            var p = getAbsP0();

            var a = getAbsAng();
            var sin = (float)Math.Sin(a * k_pi);
            var cos = (float)Math.Cos(a * k_pi);

            P1_abs_cache = new float2(p.X + cos * L, p.Y + sin * L);

            return P1_abs_cache;
        }

        public void Simulate()
        {
            getAbsP0();
            getAbsP1();

            Helper.NormalizeAngle(ref ang_d, 360);

        }

        public void Draw(GraphicsHelper gh)
        {
            var p0 = getAbsP0();
            var p1 = getAbsP1();
            gh.G.DrawLine(gh.GetPen(color), p0, p1);
        }
    }

    public class Manipulator
    {
        public List<Link> links = new List<Link>();

        public Link LastLink { get { return links[links.Count - 1]; } }

        public void Simulate()
        {
            ClearCache();

            for (int i = 0, c = links.Count; i < c; i++)
            {
                var L=links[i];
                L.Simulate();
            }
        }

        public void Draw(GraphicsHelper gh)
        {
            for (int i = 0, c = links.Count; i < c; i++)
            {
                var L = links[i];
                L.Draw(gh);
            }
        }

        public void InitDefault()
        {
            var l1 = new Link(5, new float2(25, 25), Color.Red, null); links.Add(l1);
            var l2 = new Link(4, new float2(0, 0), Color.Green, l1); links.Add(l2);
            var l3 = new Link(3, new float2(0, 0), Color.Blue, l2); links.Add(l3);
            var l4 = new Link(2, new float2(0, 0), Color.Yellow, l3); links.Add(l4);

        }

        void ClearCache()
        {
            for (int i = 0, c = links.Count; i < c; i++)
            {
                links[i].P0_abs_cache = null;
                links[i].P1_abs_cache = null;
            }
        }

        public float2 getEndPoint()
        {
            return LastLink.getAbsP1();
        }
        public float SumLengths()
        {
            float s = links[0].L;
            for (int i = 1; i < links.Count; i++) s += links[i].L;
            return s;
        }
    }
}
