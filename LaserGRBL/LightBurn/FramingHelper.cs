using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LaserGRBL.LightBurn
{
    public static class FramingHelper
    {
        public static List<PointF> GetBoundingBox(GrblFile file)
        {
            var pts = new List<PointF>();
            if (file == null || !file.Range.DrawingRange.ValidRange)
                return pts;

            float minX = (float)file.Range.DrawingRange.X.Min;
            float maxX = (float)file.Range.DrawingRange.X.Max;
            float minY = (float)file.Range.DrawingRange.Y.Min;
            float maxY = (float)file.Range.DrawingRange.Y.Max;

            pts.Add(new PointF(minX, minY));
            pts.Add(new PointF(maxX, minY));
            pts.Add(new PointF(maxX, maxY));
            pts.Add(new PointF(minX, maxY));
            pts.Add(new PointF(minX, minY)); // close loop

            return pts;
        }

        public static List<PointF> GetRubberBandHull(GrblFile file)
        {
            if (file == null || file.Commands == null || file.Commands.Count == 0)
                return GetBoundingBox(file);

            List<PointF> points = new List<PointF>();

            // Collect all burning coordinates (G1, G2, G3)
            float curX = 0, curY = 0;
            bool laserOn = false;

            foreach (var cmd in file.Commands)
            {
                if (cmd.S != null)
                    laserOn = cmd.S.Number > 0;

                if (cmd.IsMovement)
                {
                    if (cmd.X != null) curX = (float)cmd.X.Number;
                    if (cmd.Y != null) curY = (float)cmd.Y.Number;

                    if (laserOn)
                    {
                        points.Add(new PointF(curX, curY));
                    }
                }
            }

            if (points.Count < 3)
                return GetBoundingBox(file);

            // Compute 2D Convex Hull using Andrew's Monotone Chain algorithm
            return ComputeConvexHull(points);
        }

        public static List<PointF> ComputeConvexHull(List<PointF> points)
        {
            // Remove duplicates and sort by X then Y
            var sorted = points
                .GroupBy(p => new { X = Math.Round(p.X, 2), Y = Math.Round(p.Y, 2) })
                .Select(g => g.First())
                .OrderBy(p => p.X)
                .ThenBy(p => p.Y)
                .ToList();

            int n = sorted.Count;
            if (n <= 3) return sorted;

            List<PointF> lower = new List<PointF>();
            for (int i = 0; i < n; i++)
            {
                while (lower.Count >= 2 && CrossProduct(lower[lower.Count - 2], lower[lower.Count - 1], sorted[i]) <= 0)
                {
                    lower.RemoveAt(lower.Count - 1);
                }
                lower.Add(sorted[i]);
            }

            List<PointF> upper = new List<PointF>();
            for (int i = n - 1; i >= 0; i--)
            {
                while (upper.Count >= 2 && CrossProduct(upper[upper.Count - 2], upper[upper.Count - 1], sorted[i]) <= 0)
                {
                    upper.RemoveAt(upper.Count - 1);
                }
                upper.Add(sorted[i]);
            }

            // Remove last point of each half because it is repeated
            lower.RemoveAt(lower.Count - 1);
            upper.RemoveAt(upper.Count - 1);

            List<PointF> result = new List<PointF>();
            result.AddRange(lower);
            result.AddRange(upper);
            if (result.Count > 0)
                result.Add(result[0]); // close loop

            return result;
        }

        private static float CrossProduct(PointF a, PointF b, PointF c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }

        public static string GenerateFramingGCode(List<PointF> polygon, int speed, int laserPowerS, bool laserGuide, int loops = 1)
        {
            if (polygon == null || polygon.Count == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("; === LaserGRBL LightBurn Framing ===");
            sb.AppendLine("G90 ; Coordenadas absolutas");
            sb.AppendLine("G21 ; Milímetros");
            sb.AppendLine("M5 S0 ; Garante laser desligado inicialmente");

            PointF start = polygon[0];
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G0 X{0:0.000} Y{1:0.000}", start.X, start.Y));

            if (laserGuide && laserPowerS > 0)
            {
                sb.AppendLine($"M3 S{laserPowerS} ; Ligar feixe guia de baixa potência");
            }

            for (int loop = 1; loop <= Math.Max(1, loops); loop++)
            {
                for (int i = 1; i < polygon.Count; i++)
                {
                    PointF pt = polygon[i];
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000} F{2}", pt.X, pt.Y, speed));
                }
            }

            if (laserGuide)
            {
                sb.AppendLine("M5 S0 ; Desliga feixe laser guia");
            }

            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G0 X{0:0.000} Y{1:0.000}", start.X, start.Y));
            return sb.ToString();
        }
    }
}
