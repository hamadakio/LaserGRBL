using LaserGRBL.Hershey;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace LaserGRBL.LightBurn
{
    public enum ShapeType
    {
        Rectangle,
        Circle,
        Polygon,
        Star,
        Text
    }

    public class QuickShapeConfig
    {
        public ShapeType Type { get; set; } = ShapeType.Rectangle;
        public double Width { get; set; } = 50.0;
        public double Height { get; set; } = 30.0;
        public double CornerRadius { get; set; } = 0.0;
        public double Diameter { get; set; } = 40.0;
        public double InnerDiameter { get; set; } = 0.0; // Hole
        public int Sides { get; set; } = 6; // Hexagon
        public double Radius { get; set; } = 25.0;
        public double InnerRadius { get; set; } = 12.0; // For star
        public string TextString { get; set; } = "LASER";
        public double TextSize { get; set; } = 10.0;

        // Array duplicate
        public int GridCols { get; set; } = 1;
        public int GridRows { get; set; } = 1;
        public double GridSpacingX { get; set; } = 5.0;
        public double GridSpacingY { get; set; } = 5.0;

        // Laser parameters
        public int Speed { get; set; } = 800; // mm/min
        public int PowerPct { get; set; } = 80; // %
        public int Passes { get; set; } = 1;
        public double ZStep { get; set; } = 0.0; // mm per pass
        public bool AirAssist { get; set; } = true;
        public int MaxPwm { get; set; } = 1000;
        public string LaserMode { get; set; } = "M3";
    }

    public static class QuickShapesGenerator
    {
        public static string GenerateGCode(QuickShapeConfig cfg)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("; === LaserGRBL LightBurn Quick Shapes Generator ===");
            sb.AppendLine($"; Forma: {cfg.Type}");
            sb.AppendLine($"; Velocidade: {cfg.Speed} mm/min | Potência: {cfg.PowerPct}% | Passes: {cfg.Passes}");
            sb.AppendLine("G90 ; Coordenadas absolutas");
            sb.AppendLine("G21 ; Milímetros");
            sb.AppendLine("M5 S0 ; Laser desligado");

            if (cfg.AirAssist)
                sb.AppendLine("M8 ; Air Assist Ligado");

            int sVal = (int)Math.Round((cfg.PowerPct / 100.0) * cfg.MaxPwm);

            int cols = Math.Max(1, cfg.GridCols);
            int rows = Math.Max(1, cfg.GridRows);

            double shapeW = GetBoundingWidth(cfg);
            double shapeH = GetBoundingHeight(cfg);

            double stepX = shapeW + cfg.GridSpacingX;
            double stepY = shapeH + cfg.GridSpacingY;

            double currentZ = 0.0;

            for (int pass = 1; pass <= cfg.Passes; pass++)
            {
                if (cfg.Passes > 1)
                {
                    sb.AppendLine($"; --- Passe {pass} de {cfg.Passes} ---");
                    if (cfg.ZStep != 0.0)
                    {
                        currentZ += cfg.ZStep;
                        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G0 Z{0:0.000}", currentZ));
                    }
                }

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        double ox = c * stepX;
                        double oy = r * stepY;

                        switch (cfg.Type)
                        {
                            case ShapeType.Rectangle:
                                GenerateRectangle(sb, ox, oy, cfg.Width, cfg.Height, cfg.CornerRadius, cfg.Speed, sVal, cfg.LaserMode);
                                break;
                            case ShapeType.Circle:
                                GenerateCircle(sb, ox, oy, cfg.Diameter, cfg.InnerDiameter, cfg.Speed, sVal, cfg.LaserMode);
                                break;
                            case ShapeType.Polygon:
                                GeneratePolygon(sb, ox, oy, cfg.Sides, cfg.Radius, cfg.Speed, sVal, cfg.LaserMode);
                                break;
                            case ShapeType.Star:
                                GenerateStar(sb, ox, oy, cfg.Sides, cfg.Radius, cfg.InnerRadius, cfg.Speed, sVal, cfg.LaserMode);
                                break;
                            case ShapeType.Text:
                                GenerateText(sb, ox, oy, cfg.TextString, cfg.Speed, sVal, cfg.LaserMode);
                                break;
                        }
                    }
                }
            }

            if (cfg.ZStep != 0.0 && cfg.Passes > 1)
            {
                sb.AppendLine("G0 Z0 ; Retorna Z");
            }

            sb.AppendLine("M5 S0 ; Desliga laser");
            if (cfg.AirAssist)
                sb.AppendLine("M9 ; Desliga Air Assist");
            sb.AppendLine("G0 X0 Y0 ; Retorna a origem");
            sb.AppendLine("M2 ; Fim de programa");

            return sb.ToString();
        }

        private static double GetBoundingWidth(QuickShapeConfig cfg)
        {
            switch (cfg.Type)
            {
                case ShapeType.Rectangle: return cfg.Width;
                case ShapeType.Circle: return cfg.Diameter;
                case ShapeType.Polygon:
                case ShapeType.Star: return cfg.Radius * 2.0;
                case ShapeType.Text: return cfg.TextString.Length * cfg.TextSize * 0.8;
                default: return 50.0;
            }
        }

        private static double GetBoundingHeight(QuickShapeConfig cfg)
        {
            switch (cfg.Type)
            {
                case ShapeType.Rectangle: return cfg.Height;
                case ShapeType.Circle: return cfg.Diameter;
                case ShapeType.Polygon:
                case ShapeType.Star: return cfg.Radius * 2.0;
                case ShapeType.Text: return cfg.TextSize;
                default: return 50.0;
            }
        }

        private static void GenerateRectangle(StringBuilder sb, double ox, double oy, double w, double h, double r, int speed, int sVal, string laserMode)
        {
            r = Math.Min(r, Math.Min(w, h) / 2.0);

            if (r <= 0.01)
            {
                // Simple sharp box
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G0 X{0:0.000} Y{1:0.000}", ox, oy));
                sb.AppendLine($"{laserMode} S{sVal}");
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000} F{2}", ox + w, oy, speed));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", ox + w, oy + h));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", ox, oy + h));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", ox, oy));
                sb.AppendLine("M5 S0");
            }
            else
            {
                // Rounded rectangle with G2 arcs
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G0 X{0:0.000} Y{1:0.000}", ox + r, oy));
                sb.AppendLine($"{laserMode} S{sVal}");
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000} F{2}", ox + w - r, oy, speed));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G2 X{0:0.000} Y{1:0.000} I0 J{2:0.000}", ox + w, oy + r, r));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", ox + w, oy + h - r));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G2 X{0:0.000} Y{1:0.000} I{2:0.000} J0", ox + w - r, oy + h, -r));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", ox + r, oy + h));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G2 X{0:0.000} Y{1:0.000} I0 J{2:0.000}", ox, oy + h - r, -r));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", ox, oy + r));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G2 X{0:0.000} Y{1:0.000} I{2:0.000} J0", ox + r, oy, r));
                sb.AppendLine("M5 S0");
            }
        }

        private static void GenerateCircle(StringBuilder sb, double ox, double oy, double d, double innerD, int speed, int sVal, string laserMode)
        {
            double r = d / 2.0;
            double cx = ox + r;
            double cy = oy + r;

            // If inner hole exists, cut inner hole first!
            if (innerD > 0.1 && innerD < d)
            {
                double ir = innerD / 2.0;
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G0 X{0:0.000} Y{1:0.000}", cx - ir, cy));
                sb.AppendLine($"{laserMode} S{sVal}");
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G2 X{0:0.000} Y{1:0.000} I{2:0.000} J0 F{3}", cx - ir, cy, ir, speed));
                sb.AppendLine("M5 S0");
            }

            // Cut outer circle
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G0 X{0:0.000} Y{1:0.000}", cx - r, cy));
            sb.AppendLine($"{laserMode} S{sVal}");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G2 X{0:0.000} Y{1:0.000} I{2:0.000} J0 F{3}", cx - r, cy, r, speed));
            sb.AppendLine("M5 S0");
        }

        private static void GeneratePolygon(StringBuilder sb, double ox, double oy, int sides, double radius, int speed, int sVal, string laserMode)
        {
            sides = Math.Max(3, sides);
            double cx = ox + radius;
            double cy = oy + radius;

            List<PointF> pts = new List<PointF>();
            for (int i = 0; i < sides; i++)
            {
                double angle = (2.0 * Math.PI * i / sides) - (Math.PI / 2.0); // start at top
                float x = (float)(cx + radius * Math.Cos(angle));
                float y = (float)(cy + radius * Math.Sin(angle));
                pts.Add(new PointF(x, y));
            }
            pts.Add(pts[0]); // close loop

            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G0 X{0:0.000} Y{1:0.000}", pts[0].X, pts[0].Y));
            sb.AppendLine($"{laserMode} S{sVal}");
            for (int i = 1; i < pts.Count; i++)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000} F{2}", pts[i].X, pts[i].Y, speed));
            }
            sb.AppendLine("M5 S0");
        }

        private static void GenerateStar(StringBuilder sb, double ox, double oy, int points, double rOuter, double rInner, int speed, int sVal, string laserMode)
        {
            points = Math.Max(3, points);
            double cx = ox + rOuter;
            double cy = oy + rOuter;

            int totalVertices = points * 2;
            List<PointF> pts = new List<PointF>();

            for (int i = 0; i < totalVertices; i++)
            {
                double angle = (2.0 * Math.PI * i / totalVertices) - (Math.PI / 2.0);
                double r = (i % 2 == 0) ? rOuter : rInner;
                float x = (float)(cx + r * Math.Cos(angle));
                float y = (float)(cy + r * Math.Sin(angle));
                pts.Add(new PointF(x, y));
            }
            pts.Add(pts[0]); // close loop

            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G0 X{0:0.000} Y{1:0.000}", pts[0].X, pts[0].Y));
            sb.AppendLine($"{laserMode} S{sVal}");
            for (int i = 1; i < pts.Count; i++)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000} F{2}", pts[i].X, pts[i].Y, speed));
            }
            sb.AppendLine("M5 S0");
        }

        private static void GenerateText(StringBuilder sb, double ox, double oy, string text, int speed, int sVal, string laserMode)
        {
            try
            {
                var cmds = Hershey.Hershey.CreateString(text, ox, oy, speed, sVal, true, laserMode);
                if (cmds != null)
                {
                    foreach (var c in cmds)
                        sb.AppendLine(c.Command);
                }
            }
            catch { }
        }
    }
}
