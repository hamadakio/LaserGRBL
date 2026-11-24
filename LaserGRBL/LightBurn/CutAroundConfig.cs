using System;

namespace LaserGRBL.LightBurn
{
    public enum CutAroundShape
    {
        Rectangle = 0,
        RoundedRectangle = 1,
        Circle = 2,
        Ellipse = 3,
        Contour = 4
    }

    public class CutMachineOptions
    {
        public int MaxPWM { get; set; } = 1000;
        public bool HardwarePwm { get; set; } = true;
        public bool DisableG0FastSkip { get; set; } = false;
        public bool IsSmoothie { get; set; } = false;

        public CutMachineOptions Clone()
        {
            return new CutMachineOptions
            {
                MaxPWM = MaxPWM,
                HardwarePwm = HardwarePwm,
                DisableG0FastSkip = DisableG0FastSkip,
                IsSmoothie = IsSmoothie
            };
        }

        public void ValidateAndClamp()
        {
            if (MaxPWM < 1) MaxPWM = 1;
        }
    }

    public class CutAroundConfig
    {
        public bool Enabled { get; set; } = false;
        public CutAroundShape Shape { get; set; } = CutAroundShape.Rectangle;
        public double MarginMm { get; set; } = 2.0;
        public double CornerRadiusMm { get; set; } = 3.0;
        public int SpeedMmMin { get; set; } = 300;
        public int PowerPercent { get; set; } = 80;
        public int Passes { get; set; } = 1;
        public string LaserMode { get; set; } = "M3";
        public double ZStepMm { get; set; } = 0.0;
        public bool AirAssist { get; set; } = false;

        public CutAroundConfig Clone()
        {
            return new CutAroundConfig
            {
                Enabled = Enabled,
                Shape = Shape,
                MarginMm = MarginMm,
                CornerRadiusMm = CornerRadiusMm,
                SpeedMmMin = SpeedMmMin,
                PowerPercent = PowerPercent,
                Passes = Passes,
                LaserMode = LaserMode,
                ZStepMm = ZStepMm,
                AirAssist = AirAssist
            };
        }

        public string GetNormalizedLaserMode()
        {
            if (!string.IsNullOrWhiteSpace(LaserMode) &&
                LaserMode.Trim().Equals("M4", StringComparison.OrdinalIgnoreCase))
            {
                return "M4";
            }
            return "M3";
        }

        public void ValidateAndClamp()
        {
            if (!Enum.IsDefined(typeof(CutAroundShape), Shape))
                Shape = CutAroundShape.Rectangle;

            if (double.IsNaN(MarginMm) || double.IsInfinity(MarginMm) || MarginMm < 0.0)
                MarginMm = 0.0;

            if (double.IsNaN(CornerRadiusMm) || double.IsInfinity(CornerRadiusMm) || CornerRadiusMm < 0.0)
                CornerRadiusMm = 0.0;

            if (SpeedMmMin < 1)
                SpeedMmMin = 1;

            if (PowerPercent < 1)
                PowerPercent = 1;
            else if (PowerPercent > 100)
                PowerPercent = 100;

            if (Passes < 1)
                Passes = 1;

            LaserMode = GetNormalizedLaserMode();

            if (double.IsNaN(ZStepMm) || double.IsInfinity(ZStepMm) || ZStepMm < 0.0)
                ZStepMm = 0.0;
        }
    }
}
