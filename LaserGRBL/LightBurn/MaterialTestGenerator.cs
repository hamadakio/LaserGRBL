using LaserGRBL.Hershey;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LaserGRBL.LightBurn
{
    public class MaterialTestConfig
    {
        public string Title { get; set; } = "TESTE DE MATERIAL";
        public string Mode { get; set; } = "Fill"; // "Fill" (Engrave) or "Line" (Cut)
        public int Columns { get; set; } = 6; // Speed columns
        public int Rows { get; set; } = 6; // Power rows
        public int MinSpeed { get; set; } = 500;
        public int MaxSpeed { get; set; } = 3000;
        public int MinPowerPct { get; set; } = 10;
        public int MaxPowerPct { get; set; } = 100;
        public double BoxSize { get; set; } = 6.0; // mm
        public double Spacing { get; set; } = 2.5; // mm
        public double FillInterval { get; set; } = 0.15; // mm line pitch for fill
        public int Passes { get; set; } = 1;
        public int LabelSpeed { get; set; } = 1500;
        public int LabelPowerS { get; set; } = 250;
        public int MaxPwm { get; set; } = 1000;
        public string LaserMode { get; set; } = "M3"; // M3 or M4
    }

    public static class MaterialTestGenerator
    {
        public static string GenerateGCode(MaterialTestConfig cfg)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("; === LaserGRBL LightBurn Material Test Matrix ===");
            sb.AppendLine($"; Título: {cfg.Title}");
            sb.AppendLine($"; Modo: {cfg.Mode} ({cfg.Columns}x{cfg.Rows} Matriz)");
            sb.AppendLine($"; Velocidades: {cfg.MinSpeed} a {cfg.MaxSpeed} mm/min");
            sb.AppendLine($"; Potências: {cfg.MinPowerPct}% a {cfg.MaxPowerPct}%");
            sb.AppendLine("G90 ; Absoluto");
            sb.AppendLine("G21 ; Milímetros");
            sb.AppendLine("M5 S0 ; Laser desligado");

            double originX = 16.0; // Margin for left Y-axis power labels
            double originY = 12.0; // Margin for bottom X-axis speed labels

            // Calculate speeds per column
            int cols = Math.Max(2, cfg.Columns);
            int rows = Math.Max(2, cfg.Rows);

            double speedStep = (cfg.MaxSpeed - cfg.MinSpeed) / (double)(cols - 1);
            double powerStep = (cfg.MaxPowerPct - cfg.MinPowerPct) / (double)(rows - 1);

            // 1. Draw Title Header
            double totalWidth = cols * (cfg.BoxSize + cfg.Spacing);
            double totalHeight = rows * (cfg.BoxSize + cfg.Spacing);
            double headerY = originY + totalHeight + 4.0;

            AppendText(sb, cfg.Title.ToUpper(), originX, headerY, cfg.LabelSpeed, cfg.LabelPowerS, cfg.LaserMode);

            // 2. Draw Column Speed Labels along bottom
            for (int c = 0; c < cols; c++)
            {
                int curSpeed = (int)Math.Round(cfg.MinSpeed + c * speedStep);
                double boxX = originX + c * (cfg.BoxSize + cfg.Spacing);
                AppendText(sb, curSpeed.ToString(), boxX, originY - 6.0, cfg.LabelSpeed, cfg.LabelPowerS, cfg.LaserMode);
            }

            // 3. Draw Row Power Labels along left side
            for (int r = 0; r < rows; r++)
            {
                int curPct = (int)Math.Round(cfg.MinPowerPct + r * powerStep);
                double boxY = originY + r * (cfg.BoxSize + cfg.Spacing);
                AppendText(sb, $"{curPct}%", originX - 12.0, boxY + (cfg.BoxSize / 4), cfg.LabelSpeed, cfg.LabelPowerS, cfg.LaserMode);
            }

            // 4. Draw Test Grid Squares
            for (int pass = 1; pass <= cfg.Passes; pass++)
            {
                for (int r = 0; r < rows; r++)
                {
                    int curPct = (int)Math.Round(cfg.MinPowerPct + r * powerStep);
                    int sVal = (int)Math.Round((curPct / 100.0) * cfg.MaxPwm);
                    double y0 = originY + r * (cfg.BoxSize + cfg.Spacing);

                    for (int c = 0; c < cols; c++)
                    {
                        int curSpeed = (int)Math.Round(cfg.MinSpeed + c * speedStep);
                        double x0 = originX + c * (cfg.BoxSize + cfg.Spacing);

                        if (cfg.Mode.Equals("Fill", StringComparison.OrdinalIgnoreCase))
                        {
                            DrawFilledSquare(sb, x0, y0, cfg.BoxSize, curSpeed, sVal, cfg.FillInterval, cfg.LaserMode);
                        }
                        else
                        {
                            DrawVectorSquare(sb, x0, y0, cfg.BoxSize, curSpeed, sVal, cfg.LaserMode);
                        }
                    }
                }
            }

            sb.AppendLine("M5 S0 ; Desliga laser");
            sb.AppendLine("G0 X0 Y0 ; Retorna origem");
            sb.AppendLine("M2 ; Fim de programa");

            return sb.ToString();
        }

        private static void DrawVectorSquare(StringBuilder sb, double x0, double y0, double size, int speed, int sVal, string laserMode)
        {
            double x1 = x0 + size;
            double y1 = y0 + size;

            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G0 X{0:0.000} Y{1:0.000}", x0, y0));
            sb.AppendLine($"{laserMode} S{sVal}");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000} F{2}", x1, y0, speed));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", x1, y1));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", x0, y1));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", x0, y0));
            sb.AppendLine("M5 S0");
        }

        private static void DrawFilledSquare(StringBuilder sb, double x0, double y0, double size, int speed, int sVal, double interval, string laserMode)
        {
            double x1 = x0 + size;
            double curY = y0;
            bool forward = true;

            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G0 X{0:0.000} Y{1:0.000}", x0, y0));
            sb.AppendLine($"{laserMode} S{sVal}");

            while (curY <= y0 + size + 0.001)
            {
                if (forward)
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000} F{2}", x1, curY, speed));
                }
                else
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000} F{2}", x0, curY, speed));
                }

                forward = !forward;
                curY += interval;
                if (curY <= y0 + size + 0.001)
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 Y{0:0.000}", curY));
                }
            }

            sb.AppendLine("M5 S0");
        }

        private static void AppendText(StringBuilder sb, string text, double x, double y, int speed, int power, string laserMode)
        {
            try
            {
                var cmds = Hershey.Hershey.CreateString(text, x, y, speed, power, true, laserMode);
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
