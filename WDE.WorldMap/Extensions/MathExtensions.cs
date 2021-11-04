using System;
using Avalonia;

namespace WDE.WorldMap.Extensions
{
    public static class MathExtensions
    {
        public static bool LineContains(Point startp, Point endp, Point p, double thickness)
        {
            return LineDistance(p.X, p.Y, startp.X, startp.Y, endp.X, endp.Y) < thickness;
        }
        
        public static double LineDistance(double x, double y, double x1, double y1, double x2, double y2) {

            var A = x - x1;
            var B = y - y1;
            var C = x2 - x1;
            var D = y2 - y1;

            var dot = A * C + B * D;
            var len_sq = C * C + D * D;
            var param = -1.0;
            if (len_sq != 0) //in case of 0 length line
                param = dot / len_sq;

            double xx, yy;

            if (param < 0) {
                xx = x1;
                yy = y1;
            }
            else if (param > 1) {
                xx = x2;
                yy = y2;
            }
            else {
                xx = x1 + param * C;
                yy = y1 + param * D;
            }

            var dx = x - xx;
            var dy = y - yy;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static double Distance(this Point a, Point b)
        {
            var diff = a - b;
            return Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
        }

        public static double Magnitude(this Point a)
        {
            return Distance(new Point(), a);
        }

        public static Point Normalize(this Point a)
        {
            var m = Magnitude(a);
            return new Point(a.X / m, a.Y / m);
        }
    }
}