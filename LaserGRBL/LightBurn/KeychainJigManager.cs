using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace LaserGRBL.LightBurn
{
    public enum JigShape
    {
        Circle,     // Redondo / Círculo
        Square,     // Quadrado
        Rectangle   // Retangular
    }

    public static class KeychainJigManager
    {
        public static bool Enabled
        {
            get => Settings.GetObject("KeychainJig_Enabled", true);
            set => Settings.SetObject("KeychainJig_Enabled", value);
        }

        public static JigShape Shape
        {
            get => Settings.GetObject("KeychainJig_Shape", JigShape.Circle);
            set => Settings.SetObject("KeychainJig_Shape", value);
        }

        public static float Diameter
        {
            get => Settings.GetObject("KeychainJig_Diameter", 40.0f);
            set => Settings.SetObject("KeychainJig_Diameter", Math.Max(5.0f, value));
        }

        public static float Width
        {
            get => Settings.GetObject("KeychainJig_Width", 40.0f);
            set => Settings.SetObject("KeychainJig_Width", Math.Max(5.0f, value));
        }

        public static float Height
        {
            get => Settings.GetObject("KeychainJig_Height", 30.0f);
            set => Settings.SetObject("KeychainJig_Height", Math.Max(5.0f, value));
        }

        public static float CornerRadius
        {
            get => Settings.GetObject("KeychainJig_CornerRadius", 0.0f);
            set => Settings.SetObject("KeychainJig_CornerRadius", Math.Max(0.0f, value));
        }

        // Multi-peças / Grid
        public static int GridCols
        {
            get => Settings.GetObject("KeychainJig_GridCols", 1);
            set => Settings.SetObject("KeychainJig_GridCols", Math.Max(1, Math.Min(30, value)));
        }

        public static int GridRows
        {
            get => Settings.GetObject("KeychainJig_GridRows", 1);
            set => Settings.SetObject("KeychainJig_GridRows", Math.Max(1, Math.Min(30, value)));
        }

        public static float SpacingX
        {
            get => Settings.GetObject("KeychainJig_SpacingX", 5.0f);
            set => Settings.SetObject("KeychainJig_SpacingX", Math.Max(0.0f, value));
        }

        public static float SpacingY
        {
            get => Settings.GetObject("KeychainJig_SpacingY", 5.0f);
            set => Settings.SetObject("KeychainJig_SpacingY", Math.Max(0.0f, value));
        }

        public static float CenterX
        {
            get => Settings.GetObject("KeychainJig_CenterX", 50.0f);
            set => Settings.SetObject("KeychainJig_CenterX", value);
        }

        public static float CenterY
        {
            get => Settings.GetObject("KeychainJig_CenterY", 50.0f);
            set => Settings.SetObject("KeychainJig_CenterY", value);
        }

        public static int Speed
        {
            get => Settings.GetObject("KeychainJig_Speed", 5000);
            set => Settings.SetObject("KeychainJig_Speed", Math.Max(100, value));
        }

        public static int PowerPercent
        {
            get => Settings.GetObject("KeychainJig_PowerPercent", 15);
            set => Settings.SetObject("KeychainJig_PowerPercent", Math.Max(1, Math.Min(100, value)));
        }

        public static float HoleDiameter
        {
            get => Settings.GetObject("KeychainJig_HoleDiameter", 0.0f);
            set => Settings.SetObject("KeychainJig_HoleDiameter", Math.Max(0.0f, value));
        }

        public static float HoleMargin
        {
            get => Settings.GetObject("KeychainJig_HoleMargin", 3.0f);
            set => Settings.SetObject("KeychainJig_HoleMargin", Math.Max(1.0f, value));
        }

        public static bool ShowCrosshair
        {
            get => Settings.GetObject("KeychainJig_ShowCrosshair", true);
            set => Settings.SetObject("KeychainJig_ShowCrosshair", value);
        }

        public static float EffectiveWidth
        {
            get
            {
                switch (Shape)
                {
                    case JigShape.Circle: return Diameter;
                    case JigShape.Square: return Width;
                    case JigShape.Rectangle: return Width;
                    default: return Diameter;
                }
            }
        }

        public static float EffectiveHeight
        {
            get
            {
                switch (Shape)
                {
                    case JigShape.Circle: return Diameter;
                    case JigShape.Square: return Width;
                    case JigShape.Rectangle: return Height;
                    default: return Diameter;
                }
            }
        }

        public static int TotalItems => Math.Max(1, GridCols) * Math.Max(1, GridRows);

        public static List<PointF> GetItemCenters()
        {
            List<PointF> centers = new List<PointF>();
            int cols = Math.Max(1, GridCols);
            int rows = Math.Max(1, GridRows);
            float stepX = EffectiveWidth + SpacingX;
            float stepY = EffectiveHeight + SpacingY;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    float cx = CenterX + (c * stepX);
                    float cy = CenterY + (r * stepY);
                    centers.Add(new PointF(cx, cy));
                }
            }

            return centers;
        }

        public static string GenerateJigGCode(int maxPwm = 1000)
        {
            int speed = Speed;
            int sVal = (int)Math.Round((PowerPercent / 100.0) * maxPwm);
            if (sVal < 1) sVal = 1;

            float w = EffectiveWidth;
            float h = EffectiveHeight;
            float r = w / 2.0f;
            float cr = Shape == JigShape.Circle ? 0 : Math.Min(CornerRadius, Math.Min(w, h) / 2.0f);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("; === LaserGRBL - Gabarito de Chaveiros / Peças (Ninho no Papelão) ===");
            sb.AppendLine($"; Formato: {Shape} | Medidas: {w:0.0} × {h:0.0} mm | Grade: {GridCols} colunas × {GridRows} linhas ({TotalItems} peças)");
            sb.AppendLine($"; Velocidade: {speed} mm/min | Potência: {PowerPercent}% (S{sVal})");
            sb.AppendLine("G90 ; Coordenadas absolutas");
            sb.AppendLine("G21 ; Milímetros");
            sb.AppendLine("M5 S0 ; Laser desligado");

            var centers = GetItemCenters();
            for (int i = 0; i < centers.Count; i++)
            {
                PointF center = centers[i];
                float cx = center.X;
                float cy = center.Y;

                sb.AppendLine($"; --- Peça {i + 1} de {centers.Count} (Centro: X={cx:0.0} Y={cy:0.0}) ---");

                // Furo de argola (se ativo)
                if (HoleDiameter > 0.5f && HoleDiameter < Math.Min(w, h))
                {
                    float hr = HoleDiameter / 2.0f;
                    float hcx = cx;
                    float hcy = cy + (h / 2.0f) - HoleMargin - hr;
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G0 X{0:0.000} Y{1:0.000}", hcx - hr, hcy));
                    sb.AppendLine($"M3 S{sVal}");
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G2 X{0:0.000} Y{1:0.000} I{2:0.000} J0 F{3}", hcx - hr, hcy, hr, speed));
                    sb.AppendLine("M5 S0");
                }

                // Contorno da peça
                if (Shape == JigShape.Circle)
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G0 X{0:0.000} Y{1:0.000}", cx - r, cy));
                    sb.AppendLine($"M3 S{sVal}");
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G2 X{0:0.000} Y{1:0.000} I{2:0.000} J0 F{3}", cx - r, cy, r, speed));
                    sb.AppendLine("M5 S0");
                }
                else if (cr <= 0.1f)
                {
                    // Retângulo / Quadrado sem cantos arredondados
                    float left = cx - w / 2.0f;
                    float right = cx + w / 2.0f;
                    float bottom = cy - h / 2.0f;
                    float top = cy + h / 2.0f;

                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G0 X{0:0.000} Y{1:0.000}", left, bottom));
                    sb.AppendLine($"M3 S{sVal}");
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000} F{2}", right, bottom, speed));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", right, top));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", left, top));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", left, bottom));
                    sb.AppendLine("M5 S0");
                }
                else
                {
                    // Retângulo / Quadrado com cantos arredondados (G1 + G2 arcs)
                    float left = cx - w / 2.0f;
                    float right = cx + w / 2.0f;
                    float bottom = cy - h / 2.0f;
                    float top = cy + h / 2.0f;

                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G0 X{0:0.000} Y{1:0.000}", left + cr, bottom));
                    sb.AppendLine($"M3 S{sVal}");
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000} F{2}", right - cr, bottom, speed));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G2 X{0:0.000} Y{1:0.000} I0 J{2:0.000}", right, bottom + cr, cr));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", right, top - cr));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G2 X{0:0.000} Y{1:0.000} I{2:0.000} J0", right - cr, top, -cr));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", left + cr, top));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G2 X{0:0.000} Y{1:0.000} I0 J{2:0.000}", left, top - cr, -cr));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", left, bottom + cr));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G2 X{0:0.000} Y{1:0.000} I{2:0.000} J0", left + cr, bottom, cr));
                    sb.AppendLine("M5 S0");
                }
            }

            sb.AppendLine("G0 X0 Y0 ; Retorna a origem");
            sb.AppendLine("M2 ; Fim de programa");

            return sb.ToString();
        }

        public static void ExecuteBurn(GrblCore core, IWin32Window parent)
        {
            if (core == null || !core.IsConnected)
            {
                var dr = MessageBox.Show(parent,
                    "A máquina não está conectada ao LaserGRBL no momento.\n\nDeseja carregar o G-Code do gabarito como programa no LaserGRBL para visualizar?",
                    "Gabarito de Peças / Chaveiros",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (dr == DialogResult.Yes)
                {
                    LoadJigAsJob(core);
                }
                return;
            }

            int maxPwm = 1000;
            if (GrblCore.Configuration != null && GrblCore.Configuration.MaxPWM > 0)
                maxPwm = (int)GrblCore.Configuration.MaxPWM;

            int sVal = (int)Math.Round((PowerPercent / 100.0) * maxPwm);
            string shapeName = Shape == JigShape.Circle ? $"Redondo Ø {Diameter:0.0} mm" :
                               Shape == JigShape.Square ? $"Quadrado {Width:0.0}×{Width:0.0} mm" :
                               $"Retângulo {Width:0.0}×{Height:0.0} mm";

            string msg = $"Deseja gravar o ninho do gabarito no papelão agora?\n\n" +
                         $"• Formato: {shapeName}\n" +
                         $"• Grade: {GridCols} colunas × {GridRows} linhas ({TotalItems} peças)\n" +
                         $"• Origem Inicial: X = {CenterX:0.0} mm, Y = {CenterY:0.0} mm\n" +
                         $"• Velocidade: {Speed} mm/min\n" +
                         $"• Potência: {PowerPercent}% (S{sVal})\n" +
                         $"\nCertifique-se de que o papelão está na mesa e o foco do laser ajustado.";

            if (MessageBox.Show(parent, msg, $"Gravar Gabarito no Papelão ({TotalItems} peças)", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                string gcode = GenerateJigGCode(maxPwm);
                core.ExecuteCustomCode(gcode);
                Logger.LogMessage("Jig", "Gravou gabarito {0} peças ({1}x{2}) a 15% F5000", TotalItems, GridCols, GridRows);
            }
        }

        public static void LoadJigAsJob(GrblCore core)
        {
            if (core == null) return;
            try
            {
                int maxPwm = 1000;
                if (GrblCore.Configuration != null && GrblCore.Configuration.MaxPWM > 0)
                    maxPwm = (int)GrblCore.Configuration.MaxPWM;

                string gcode = GenerateJigGCode(maxPwm);
                string tempFile = Path.Combine(Path.GetTempPath(), $"Gabarito_{Shape}_{TotalItems}pecas_{DateTime.Now:yyyyMMdd_HHmmss}.nc");
                File.WriteAllText(tempFile, gcode);
                core.OpenFile(tempFile, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar gabarito: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void CenterArtwork(GrblCore core)
        {
            if (core?.LoadedFile?.Range?.DrawingRange?.ValidRange == true)
            {
                core.MoveLoadedFileTo((decimal)CenterX, (decimal)CenterY, true);
            }
        }

        public static void DuplicateArtworkAcrossGrid(GrblCore core, IWin32Window parent)
        {
            if (core?.LoadedFile == null || !core.LoadedFile.Range.DrawingRange.ValidRange)
            {
                MessageBox.Show(parent, "Nenhuma arte carregada no LaserGRBL para duplicar.", "Duplicar Arte", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (GridCols == 1 && GridRows == 1)
            {
                CenterArtwork(core);
                MessageBox.Show(parent, "A arte foi centralizada no gabarito (1 peça).", "Centralizado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // 1. Centraliza a arte na primeira peça (X=CenterX, Y=CenterY)
                core.MoveLoadedFileTo((decimal)CenterX, (decimal)CenterY, true);

                // 2. Extrai as linhas de comando da arte centralizada
                List<string> baseLines = new List<string>();
                foreach (var cmd in core.LoadedFile.Commands)
                {
                    if (cmd != null && !string.IsNullOrWhiteSpace(cmd.Command))
                        baseLines.Add(cmd.Command);
                }

                if (baseLines.Count == 0) return;

                int cols = Math.Max(1, GridCols);
                int rows = Math.Max(1, GridRows);
                float stepX = EffectiveWidth + SpacingX;
                float stepY = EffectiveHeight + SpacingY;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"; === LaserGRBL - Gravação Múltipla ({TotalItems} Peças / Grade {cols}x{rows}) ===");
                sb.AppendLine("G90 ; Coordenadas absolutas");
                sb.AppendLine("G21 ; Milímetros");

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        decimal dx = (decimal)(c * stepX);
                        decimal dy = (decimal)(r * stepY);

                        sb.AppendLine($"; --- Peça Coluna {c + 1}, Linha {r + 1} (Offset DX={dx:0.0} DY={dy:0.0}) ---");

                        foreach (string line in baseLines)
                        {
                            string shifted = (dx == 0 && dy == 0) ? line : GrblFile.TranslateGCodeLine(line, dx, dy);
                            sb.AppendLine(shifted);
                        }

                        sb.AppendLine("M5 S0 ; Laser off entre peças");
                    }
                }

                sb.AppendLine("G0 X0 Y0 ; Retorna a origem final");
                sb.AppendLine("M2 ; Fim de gravação múltipla");

                string tempFile = Path.Combine(Path.GetTempPath(), $"GravacaoMultipla_{TotalItems}pecas_{DateTime.Now:yyyyMMdd_HHmmss}.nc");
                File.WriteAllText(tempFile, sb.ToString());
                core.OpenFile(tempFile, false);

                MessageBox.Show(parent, $"A arte foi duplicada com sucesso para todas as {TotalItems} peças da grade ({cols}×{rows})!\nVocê já pode visualizar e gravar todas de uma só vez.", "Gravação Múltipla Gerada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(parent, $"Erro ao duplicar arte: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
