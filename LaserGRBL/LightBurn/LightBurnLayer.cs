using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;

namespace LaserGRBL.LightBurn
{
    [Serializable]
    public class LightBurnLayer
    {
        public int Index { get; set; }
        public string Code { get; set; } // e.g. "C00", "C01"
        public string Name { get; set; }
        public Color Color { get; set; }
        public string Mode { get; set; } // "Line" (Cut) or "Fill" (Engrave)
        public int Speed { get; set; } // mm/min
        public int PowerMin { get; set; } // % 0..100
        public int PowerMax { get; set; } // % 0..100
        public int Passes { get; set; } // 1..20
        public double ZStep { get; set; } // mm per pass (negative to go down)
        public bool AirAssist { get; set; } // M8 / M9
        public bool Output { get; set; } // enabled

        public LightBurnLayer()
        {
            Output = true;
            Mode = "Line";
            Passes = 1;
            ZStep = 0.0;
            AirAssist = true;
        }

        public LightBurnLayer(int index, string code, string name, Color color, string mode, int speed, int minP, int maxP, int passes, double zStep, bool air)
        {
            Index = index;
            Code = code;
            Name = name;
            Color = color;
            Mode = mode;
            Speed = speed;
            PowerMin = minP;
            PowerMax = maxP;
            Passes = passes;
            ZStep = zStep;
            AirAssist = air;
            Output = true;
        }
    }

    public static class LightBurnLayerManager
    {
        private static List<LightBurnLayer> mCurrentLayers;

        static LightBurnLayerManager()
        {
            ResetToDefaults();
        }

        public static List<LightBurnLayer> CurrentLayers
        {
            get
            {
                if (mCurrentLayers == null)
                    ResetToDefaults();
                return mCurrentLayers;
            }
        }

        public static void ResetToDefaults()
        {
            mCurrentLayers = new List<LightBurnLayer>()
            {
                new LightBurnLayer(0, "C00", "Corte Principal", Color.FromArgb(33, 33, 33), "Line", 350, 0, 95, 2, -0.5, true),
                new LightBurnLayer(1, "C01", "Gravação / Fill", Color.FromArgb(41, 121, 255), "Fill", 2500, 10, 45, 1, 0.0, false),
                new LightBurnLayer(2, "C02", "Marcação / Score", Color.FromArgb(255, 23, 68), "Line", 1800, 0, 25, 1, 0.0, false),
                new LightBurnLayer(3, "C03", "Corte Fino", Color.FromArgb(0, 230, 118), "Line", 600, 0, 80, 1, 0.0, true),
                new LightBurnLayer(4, "C04", "Gravação Profunda", Color.FromArgb(255, 214, 0), "Fill", 1200, 20, 65, 2, 0.0, true),
                new LightBurnLayer(5, "C05", "Contorno Vetorial", Color.FromArgb(213, 0, 249), "Line", 900, 0, 70, 1, 0.0, true),
                new LightBurnLayer(6, "C06", "Superfície Suave", Color.FromArgb(0, 229, 255), "Fill", 3200, 5, 20, 1, 0.0, false),
                new LightBurnLayer(7, "C07", "Furação / Perfurar", Color.FromArgb(255, 109, 0), "Line", 200, 0, 100, 3, -0.8, true)
            };
        }

        public static Dictionary<string, List<LightBurnLayer>> GetMaterialPresets()
        {
            var presets = new Dictionary<string, List<LightBurnLayer>>();

            // Preset: MDF 3mm
            presets["MDF 3mm (Corte & Gravação)"] = new List<LightBurnLayer>()
            {
                new LightBurnLayer(0, "C00", "Corte MDF 3mm", Color.FromArgb(33, 33, 33), "Line", 300, 0, 90, 2, -0.5, true),
                new LightBurnLayer(1, "C01", "Gravação MDF", Color.FromArgb(41, 121, 255), "Fill", 2200, 10, 40, 1, 0.0, false),
                new LightBurnLayer(2, "C02", "Linha / Detalhe", Color.FromArgb(255, 23, 68), "Line", 1500, 0, 20, 1, 0.0, false)
            };

            // Preset: Compensado 3mm / Plywood
            presets["Compensado 3mm (Plywood)"] = new List<LightBurnLayer>()
            {
                new LightBurnLayer(0, "C00", "Corte Compensado", Color.FromArgb(33, 33, 33), "Line", 280, 0, 95, 2, -0.6, true),
                new LightBurnLayer(1, "C01", "Gravação Escura", Color.FromArgb(41, 121, 255), "Fill", 2000, 15, 50, 1, 0.0, false),
                new LightBurnLayer(2, "C02", "Gravação Clara", Color.FromArgb(0, 229, 255), "Fill", 3000, 5, 25, 1, 0.0, false)
            };

            // Preset: Acrílico 2mm / 3mm
            presets["Acrílico 2mm / 3mm"] = new List<LightBurnLayer>()
            {
                new LightBurnLayer(0, "C00", "Corte Acrílico", Color.FromArgb(33, 33, 33), "Line", 220, 0, 90, 3, -0.4, true),
                new LightBurnLayer(1, "C01", "Gravação Fosca", Color.FromArgb(41, 121, 255), "Fill", 2400, 10, 35, 1, 0.0, false),
                new LightBurnLayer(2, "C02", "Contorno / Borda", Color.FromArgb(255, 23, 68), "Line", 1200, 0, 30, 1, 0.0, true)
            };

            // Preset: Couro Legítimo
            presets["Couro Legítimo"] = new List<LightBurnLayer>()
            {
                new LightBurnLayer(0, "C00", "Corte Couro", Color.FromArgb(33, 33, 33), "Line", 450, 0, 85, 1, 0.0, true),
                new LightBurnLayer(1, "C01", "Gravação / Relevo", Color.FromArgb(41, 121, 255), "Fill", 2800, 5, 30, 1, 0.0, false)
            };

            // Preset: Papelão Kraft / Cartolina
            presets["Papelão Kraft / Cartolina"] = new List<LightBurnLayer>()
            {
                new LightBurnLayer(0, "C00", "Corte Rápido", Color.FromArgb(33, 33, 33), "Line", 1200, 0, 60, 1, 0.0, true),
                new LightBurnLayer(1, "C01", "Vincos / Dobras", Color.FromArgb(255, 23, 68), "Line", 2500, 0, 15, 1, 0.0, false)
            };

            return presets;
        }

        public static string ApplyLayersToGCode(GrblFile file, List<LightBurnLayer> layers, int maxPwm = 1000)
        {
            if (file == null || file.Commands == null || file.Commands.Count == 0)
                return null;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("; === LaserGRBL LightBurn Layer Processing ===");
            sb.AppendLine($"; Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("G90 ; Coordenadas absolutas");
            sb.AppendLine("G21 ; Unidades em milímetros");
            sb.AppendLine("M5 S0 ; Laser desligado");

            // Extract segments from current program
            List<GrblCommand> originalCmds = file.Commands;

            // Group into active layers: For now, if user configured passes or multi-layer,
            // we apply the active layer parameters (Passes, Z-step, Speed, Power, Air Assist)
            foreach (var layer in layers)
            {
                if (!layer.Output) continue;

                int targetS = (int)((layer.PowerMax / 100.0) * maxPwm);
                sb.AppendLine($"; --- Camada [{layer.Code}]: {layer.Name} (Passes: {layer.Passes}, Vel: {layer.Speed}mm/min, S: {targetS}) ---");

                if (layer.AirAssist)
                    sb.AppendLine("M8 ; Air Assist LIGADO");
                else
                    sb.AppendLine("M9 ; Air Assist DESLIGADO");

                double currentZ = 0.0;

                for (int pass = 1; pass <= layer.Passes; pass++)
                {
                    if (layer.Passes > 1)
                    {
                        sb.AppendLine($"; Passe {pass} de {layer.Passes}");
                        if (layer.ZStep != 0.0)
                        {
                            currentZ += layer.ZStep;
                            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "G0 Z{0:0.000}", currentZ));
                        }
                    }

                    // Feed lines
                    foreach (var cmd in originalCmds)
                    {
                        string line = cmd.Command.Trim();
                        if (string.IsNullOrEmpty(line)) continue;

                        // Replace F with layer speed if it is cutting move
                        if (line.StartsWith("G1", StringComparison.OrdinalIgnoreCase) ||
                            line.StartsWith("G2", StringComparison.OrdinalIgnoreCase) ||
                            line.StartsWith("G3", StringComparison.OrdinalIgnoreCase))
                        {
                            // If has S parameter, update power
                            if (line.Contains("S") && targetS > 0)
                            {
                                line = System.Text.RegularExpressions.Regex.Replace(line, @"S\d+", $"S{targetS}");
                            }
                            // If has F parameter, update speed
                            if (line.Contains("F") && layer.Speed > 0)
                            {
                                line = System.Text.RegularExpressions.Regex.Replace(line, @"F\d+", $"F{layer.Speed}");
                            }
                            sb.AppendLine(line);
                        }
                        else if (!line.StartsWith("M8") && !line.StartsWith("M9") && !line.StartsWith("M2") && !line.StartsWith("M30"))
                        {
                            sb.AppendLine(line);
                        }
                    }
                }

                if (layer.ZStep != 0.0 && layer.Passes > 1)
                {
                    // Restore Z to 0
                    sb.AppendLine("G0 Z0 ; Retorna Z para altura original");
                }
            }

            sb.AppendLine("M5 S0 ; Desliga laser");
            sb.AppendLine("M9 ; Desliga Air Assist");
            sb.AppendLine("G0 X0 Y0 ; Retorna a origem");
            sb.AppendLine("M2 ; Fim de programa");

            return sb.ToString();
        }
    }
}
