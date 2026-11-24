using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using LaserGRBL.LightBurn;
using LaserGRBL.UserControls;
using Xunit;

namespace LaserGRBL.Tests
{
    public class CutAroundTests
    {
        private static GrblFile CreateSampleRectangleEngravingFile(double x0 = 10, double y0 = 10, double x1 = 40, double y1 = 25)
        {
            var file = new GrblFile();
            file.LoadCommands(new[]
            {
                "G0 X0 Y0", // travel at origin (laser off, must NOT inflate engraving bbox)
                string.Format(CultureInfo.InvariantCulture, "G0 X{0:0.###} Y{1:0.###}", x0, y0),
                "M3 S500",
                string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.###} Y{1:0.###} F1200", x1, y0),
                string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.###} Y{1:0.###}", x1, y1),
                string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.###} Y{1:0.###}", x0, y1),
                string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.###} Y{1:0.###}", x0, y0),
                "M5 S0",
                "G0 X200 Y200" // trailing rapid with laser off (must NOT inflate engraving bbox)
            }, "test-engraving.nc");
            return file;
        }

        private static ProgramRange.XYRange MeasureBurningRange(IEnumerable<GrblCommand> commands, bool hardwarePwm = true)
        {
            var range = new ProgramRange.XYRange();
            var spb = new GrblCommand.StatePositionBuilder();
            var conf = new GrblConfST();

            foreach (var cmd in commands)
            {
                bool deleteHelper = !cmd.JustBuilt;
                if (deleteHelper) cmd.BuildHelper();
                try
                {
                    spb.LastArcHelperResult = null;
                    spb.AnalyzeCommand(cmd, true, conf);
                    if (cmd.IsMovement && spb.G2G3 && spb.LastArcHelperResult == null)
                        spb.GetArcHelper(cmd);

                    bool isBurning = spb.IsLaserBurning(hardwarePwm);
                    if (isBurning && cmd.IsMovement)
                    {
                        range.X.UpdateRange(spb.X.Previous);
                        range.Y.UpdateRange(spb.Y.Previous);
                        if (spb.G2G3 && spb.LastArcHelperResult != null)
                        {
                            var b = spb.LastArcHelperResult.BBox;
                            range.UpdateRange(b.X, b.Y, b.Width, b.Height);
                        }
                        else
                        {
                            range.X.UpdateRange(spb.X.Number);
                            range.Y.UpdateRange(spb.Y.Number);
                        }
                    }
                }
                finally
                {
                    if (deleteHelper) cmd.DeleteHelper();
                }
            }
            return range;
        }

        [Fact]
        public void DisabledByDefault_EngravingRemainsUntouched_AndRoundTripRestoresExactCommands()
        {
            var file = CreateSampleRectangleEngravingFile(10, 10, 40, 25);
            Assert.False(file.CutAroundConfig.Enabled);
            Assert.False(file.HasCutAroundEnabled);

            var originalCmds = file.Commands.Select(c => c.Command).ToList();

            // ComposeEngravingAndCut with disabled config returns exact engraving only
            var composedDisabled = CutAroundGenerator.ComposeEngravingAndCut(file.Commands, new CutAroundConfig { Enabled = false });
            Assert.Equal(originalCmds, composedDisabled.Select(c => c.Command).ToList());

            // Enable then disable -> restores exact original engraving commands
            file.ApplyCutAround(new CutAroundConfig
            {
                Enabled = true,
                Shape = CutAroundShape.Rectangle,
                MarginMm = 3.0,
                SpeedMmMin = 400,
                PowerPercent = 85,
                Passes = 2
            });

            Assert.True(file.HasCutAroundEnabled);
            Assert.True(file.Commands.Count > originalCmds.Count);

            file.DisableCutAround();

            Assert.False(file.HasCutAroundEnabled);
            Assert.False(file.CutAroundConfig.Enabled);
            Assert.Equal(originalCmds, file.Commands.Select(c => c.Command).ToList());
            Assert.Equal(10m, file.Range.DrawingRange.X.Min);
            Assert.Equal(40m, file.Range.DrawingRange.X.Max);
            Assert.Equal(10m, file.Range.DrawingRange.Y.Min);
            Assert.Equal(25m, file.Range.DrawingRange.Y.Max);
        }

        [Fact]
        public void EnabledCutAround_ExecutesEngravingFirst_ThenSafetyOff_ThenCutAround()
        {
            var file = CreateSampleRectangleEngravingFile(10, 10, 40, 25);
            var originalCmds = file.Commands.Select(c => c.Command).ToList();

            var cfg = new CutAroundConfig
            {
                Enabled = true,
                Shape = CutAroundShape.Rectangle,
                MarginMm = 2.5,
                SpeedMmMin = 350,
                PowerPercent = 75,
                Passes = 1,
                LaserMode = "M3"
            };
            file.ApplyCutAround(cfg, new CutMachineOptions { MaxPWM = 1000, HardwarePwm = true });

            var allCmds = file.Commands.Select(c => c.Command).ToList();
            Assert.True(allCmds.Count > originalCmds.Count);

            // Exact engraving commands must appear FIRST in original order
            for (int i = 0; i < originalCmds.Count; i++)
            {
                Assert.Equal(originalCmds[i], allCmds[i]);
            }

            // Cut section starts after engraving with safety G90 + M5 S0 before G0 travel
            var cutSlice = file.Commands.Skip(originalCmds.Count).ToList();
            Assert.Equal("G90", cutSlice[0].Command);
            Assert.Equal("M5 S0", cutSlice[1].Command);
            Assert.StartsWith("G0 ", cutSlice[2].Command);

            // Engraving bounds ignored G0 X0 Y0 and G0 X200 Y200 -> [10..40] x [10..25]
            var cutBurnRange = MeasureBurningRange(cutSlice);
            Assert.Equal(7.5m, Math.Round(cutBurnRange.X.Min, 3));
            Assert.Equal(42.5m, Math.Round(cutBurnRange.X.Max, 3));
            Assert.Equal(7.5m, Math.Round(cutBurnRange.Y.Min, 3));
            Assert.Equal(27.5m, Math.Round(cutBurnRange.Y.Max, 3));
        }

        [Fact]
        public void CutAroundShapes_AllShapesEncloseEngravingWithConfiguredMargin()
        {
            // Asymmetric polygon engraving inside [20..60] x [15..45]
            var engraving = new List<GrblCommand>
            {
                new GrblCommand("G0 X20 Y15"),
                new GrblCommand("M3 S600"),
                new GrblCommand("G1 X60 Y15 F1000"),
                new GrblCommand("G1 X45 Y45"),
                new GrblCommand("G1 X20 Y30"),
                new GrblCommand("G1 X20 Y15"),
                new GrblCommand("M5 S0")
            };

            double margin = 4.0;
            var geom = CutAroundGenerator.ComputeEngravingGeometry(engraving);
            Assert.True(geom.IsValid);
            Assert.Equal(20.0, geom.MinX, 3);
            Assert.Equal(60.0, geom.MaxX, 3);
            Assert.Equal(15.0, geom.MinY, 3);
            Assert.Equal(45.0, geom.MaxY, 3);

            // 1. Rectangle
            var rectRes = CutAroundGenerator.BuildCutAround(engraving, new CutAroundConfig
            {
                Enabled = true,
                Shape = CutAroundShape.Rectangle,
                MarginMm = margin
            });
            Assert.Equal(16.0, rectRes.CutMinX, 3);
            Assert.Equal(64.0, rectRes.CutMaxX, 3);
            Assert.Equal(11.0, rectRes.CutMinY, 3);
            Assert.Equal(49.0, rectRes.CutMaxY, 3);

            // 2. RoundedRectangle (with G3 arcs and safe radius clamping)
            var rrRes = CutAroundGenerator.BuildCutAround(engraving, new CutAroundConfig
            {
                Enabled = true,
                Shape = CutAroundShape.RoundedRectangle,
                MarginMm = margin,
                CornerRadiusMm = 3.0
            });
            Assert.Contains(rrRes.CutCommands, c => c.Command.StartsWith("G3 "));
            var rrRange = MeasureBurningRange(rrRes.CutCommands);
            Assert.Equal(16.0m, Math.Round(rrRange.X.Min, 3));
            Assert.Equal(64.0m, Math.Round(rrRange.X.Max, 3));
            Assert.Equal(11.0m, Math.Round(rrRange.Y.Min, 3));
            Assert.Equal(49.0m, Math.Round(rrRange.Y.Max, 3));

            // Extreme corner radius clamping must not invert or fail
            var rrClamped = CutAroundGenerator.BuildCutAround(engraving, new CutAroundConfig
            {
                Enabled = true,
                Shape = CutAroundShape.RoundedRectangle,
                MarginMm = margin,
                CornerRadiusMm = 999.0
            });
            Assert.True(rrClamped.IsValid);
            var rrClampedRange = MeasureBurningRange(rrClamped.CutCommands);
            Assert.True(rrClampedRange.X.Min <= 16.01m && rrClampedRange.X.Max >= 63.99m);

            // 3. Circle (minimum circumscribing circle + margin, never cuts across corners)
            var circleRes = CutAroundGenerator.BuildCutAround(engraving, new CutAroundConfig
            {
                Enabled = true,
                Shape = CutAroundShape.Circle,
                MarginMm = margin
            });
            var g2Cmd = circleRes.CutCommands.FirstOrDefault(c => c.Command.StartsWith("G2 "));
            Assert.NotNull(g2Cmd);
            g2Cmd.BuildHelper();
            double cutR = (double)g2Cmd.I.Number;
            double cutCx = (double)g2Cmd.X.Number + cutR;
            double cutCy = (double)g2Cmd.Y.Number;
            g2Cmd.DeleteHelper();

            foreach (var pt in geom.BurningPoints)
            {
                double distToCenter = Math.Sqrt(Math.Pow(pt.X - cutCx, 2) + Math.Pow(pt.Y - cutCy, 2));
                Assert.True(distToCenter <= cutR - margin + 1e-2,
                    $"Point ({pt.X},{pt.Y}) at dist {distToCenter:0.000} violates circle margin {margin} (R={cutR:0.000})");
            }

            // 4. Ellipse (circumscribes all 4 engraving corners strictly by at least MarginMm)
            var ellipseRes = CutAroundGenerator.BuildCutAround(engraving, new CutAroundConfig
            {
                Enabled = true,
                Shape = CutAroundShape.Ellipse,
                MarginMm = margin
            });
            Assert.True(ellipseRes.CutPolygon.Count >= 64);

            // Check distance from all 4 engraving corners to every segment of the ellipse polygon
            PointF[] engCorners = new[]
            {
                new PointF((float)geom.MinX, (float)geom.MinY),
                new PointF((float)geom.MaxX, (float)geom.MinY),
                new PointF((float)geom.MaxX, (float)geom.MaxY),
                new PointF((float)geom.MinX, (float)geom.MaxY)
            };
            foreach (var corner in engCorners)
            {
                Assert.True(IsPointInsidePolygon(corner, ellipseRes.CutPolygon), $"Corner ({corner.X},{corner.Y}) not inside ellipse");
                double minDist = MinDistanceToPolygonEdges(corner, ellipseRes.CutPolygon);
                Assert.True(minDist >= margin - 0.05,
                    $"Corner ({corner.X},{corner.Y}) distance {minDist:0.000} < margin {margin}");
            }

            // 5. Contour (Convex Hull + ClipperOffset jtRound)
            var contourRes = CutAroundGenerator.BuildCutAround(engraving, new CutAroundConfig
            {
                Enabled = true,
                Shape = CutAroundShape.Contour,
                MarginMm = margin
            });
            Assert.True(contourRes.CutPolygon.Count > 6);
            foreach (var pt in geom.BurningPoints)
            {
                Assert.True(IsPointInsidePolygon(pt, contourRes.CutPolygon));
                double minDist = MinDistanceToPolygonEdges(pt, contourRes.CutPolygon);
                Assert.True(minDist >= margin - 0.08,
                    $"Burning point ({pt.X},{pt.Y}) distance to contour {minDist:0.000} < margin {margin}");
            }
        }

        [Fact]
        public void ContourShape_FallsBackToRectangle_WhenFewerThanThreeNonCollinearPoints()
        {
            // Collinear horizontal burning line
            var collinear = new List<GrblCommand>
            {
                new GrblCommand("G0 X10 Y20"),
                new GrblCommand("M3 S500"),
                new GrblCommand("G1 X30 Y20 F1000"),
                new GrblCommand("G1 X50 Y20"),
                new GrblCommand("M5 S0")
            };

            var res = CutAroundGenerator.BuildCutAround(collinear, new CutAroundConfig
            {
                Enabled = true,
                Shape = CutAroundShape.Contour,
                MarginMm = 3.0
            });

            Assert.True(res.IsValid);
            Assert.Equal(5, res.CutPolygon.Count); // Closed 4-corner rectangle
            Assert.Equal(7.0, res.CutMinX, 3);
            Assert.Equal(53.0, res.CutMaxX, 3);
            Assert.Equal(17.0, res.CutMinY, 3);
            Assert.Equal(23.0, res.CutMaxY, 3);
        }

        [Fact]
        public void EngravingWithG2Arc_IsEnclosedByCutAround()
        {
            // Half-circle / full-circle arc at center (30,30) radius 10 -> bbox [20..40] x [20..40]
            var arcEngraving = new List<GrblCommand>
            {
                new GrblCommand("G0 X20 Y30"),
                new GrblCommand("M3 S400"),
                new GrblCommand("G2 X20 Y30 I10 J0 F800"),
                new GrblCommand("M5 S0")
            };

            var res = CutAroundGenerator.BuildCutAround(arcEngraving, new CutAroundConfig
            {
                Enabled = true,
                Shape = CutAroundShape.Rectangle,
                MarginMm = 2.0
            });

            Assert.Equal(18.0, res.CutMinX, 3);
            Assert.Equal(42.0, res.CutMaxX, 3);
            Assert.Equal(18.0, res.CutMinY, 3);
            Assert.Equal(42.0, res.CutMaxY, 3);
        }

        [Fact]
        public void ParametersAppliedAccurately_SpeedPowerPassesLaserModeZStepAirAssist()
        {
            var file = CreateSampleRectangleEngravingFile(10, 10, 30, 20);
            var originalCount = file.Commands.Count;

            var cfg = new CutAroundConfig
            {
                Enabled = true,
                Shape = CutAroundShape.Rectangle,
                MarginMm = 2.0,
                SpeedMmMin = 420,
                PowerPercent = 65,
                Passes = 3,
                LaserMode = "M4",
                ZStepMm = 0.5,
                AirAssist = true
            };
            var opts = new CutMachineOptions
            {
                MaxPWM = 1000,
                HardwarePwm = true,
                DisableG0FastSkip = false,
                IsSmoothie = false
            };

            file.ApplyCutAround(cfg, opts);
            var cutCmds = file.Commands.Skip(originalCount).Select(c => c.Command).ToList();

            // Air assist M8 before passes, M9 after passes
            int m8Index = cutCmds.IndexOf("M8");
            int m9Index = cutCmds.IndexOf("M9");
            Assert.True(m8Index >= 0);
            Assert.True(m9Index > m8Index);

            // Exactly 3 passes of M4 S650
            int m4Count = cutCmds.Count(c => c == "M4 S650");
            Assert.Equal(3, m4Count);

            // Speed F420 applied on all 3 passes
            int f420Count = cutCmds.Count(c => c.Contains("F420"));
            Assert.Equal(3, f420Count);

            // Z-step: pass 2 -> Z-0.500, pass 3 -> Z-1.000, restore -> G0 Z0
            Assert.Contains("G0 Z-0.500", cutCmds);
            Assert.Contains("G0 Z-1.000", cutCmds);
            Assert.Contains("G0 Z0", cutCmds);

            // Final command turns laser off
            Assert.Equal("M5 S0", cutCmds.Last());
        }

        [Fact]
        public void MachineOptions_SupportCustomMaxPwm_NonPwm_Smoothie_AndDisableG0FastSkip()
        {
            var engraving = CreateSampleRectangleEngravingFile(10, 10, 20, 20).Commands;
            var cfg = new CutAroundConfig
            {
                Enabled = true,
                Shape = CutAroundShape.Rectangle,
                MarginMm = 1.0,
                SpeedMmMin = 300,
                PowerPercent = 50,
                Passes = 1,
                LaserMode = "M3"
            };

            // Custom MaxPWM = 255 -> 50% = S128
            var pwm255 = CutAroundGenerator.BuildCutCommands(engraving, cfg, new CutMachineOptions { MaxPWM = 255, HardwarePwm = true });
            Assert.Contains(pwm255, c => c.Command == "M3 S128");

            // Non-PWM relay mode -> M3 without S, M5 without S0
            var nonPwm = CutAroundGenerator.BuildCutCommands(engraving, cfg, new CutMachineOptions { HardwarePwm = false });
            Assert.Contains(nonPwm, c => c.Command == "M3");
            Assert.DoesNotContain(nonPwm, c => c.Command.StartsWith("M3 S"));
            Assert.Contains(nonPwm, c => c.Command == "M5");

            // Smoothie mode -> S0.50
            var smoothie = CutAroundGenerator.BuildCutCommands(engraving, cfg, new CutMachineOptions { IsSmoothie = true });
            Assert.Contains(smoothie, c => c.Command == "M3 S0.50");

            // DisableG0FastSkip -> no G0 in cut commands, uses G1
            var noG0 = CutAroundGenerator.BuildCutCommands(engraving, cfg, new CutMachineOptions { DisableG0FastSkip = true });
            Assert.DoesNotContain(noG0, c => c.Command.StartsWith("G0 "));
            Assert.Contains(noG0, c => c.Command.StartsWith("G1 X9.000 Y9.000"));
        }

        [Fact]
        public void ReEditingCutConfig_DoesNotCompoundMargins_AndNewFileResetsCutState()
        {
            var file = CreateSampleRectangleEngravingFile(10, 10, 30, 20);

            // First edit: margin 2mm -> cut X [8..32]
            file.ApplyCutAround(new CutAroundConfig { Enabled = true, Shape = CutAroundShape.Rectangle, MarginMm = 2.0 });
            Assert.Equal(8m, Math.Round(file.Range.DrawingRange.X.Min, 3));
            Assert.Equal(32m, Math.Round(file.Range.DrawingRange.X.Max, 3));

            // Second edit: margin 5mm -> cut X must be [5..35], NOT [3..37]!
            file.ApplyCutAround(new CutAroundConfig { Enabled = true, Shape = CutAroundShape.Rectangle, MarginMm = 5.0 });
            Assert.Equal(5m, Math.Round(file.Range.DrawingRange.X.Min, 3));
            Assert.Equal(35m, Math.Round(file.Range.DrawingRange.X.Max, 3));
            Assert.Equal(5m, Math.Round(file.Range.DrawingRange.Y.Min, 3));
            Assert.Equal(25m, Math.Round(file.Range.DrawingRange.Y.Max, 3));

            // Third edit: switch shape Rectangle -> Circle -> still wraps base engraving [10..30]x[10..20]
            file.ApplyCutAround(new CutAroundConfig { Enabled = true, Shape = CutAroundShape.Circle, MarginMm = 1.0 });
            Assert.Equal(1, file.Commands.Count(c => c.Command.StartsWith("G2 ")));
            Assert.True(file.BaseEngravingCommands != null && file.BaseEngravingCommands.Count == 9);

            // Loading a new file resets CutAround state
            file.LoadCommands(new[]
            {
                "G0 X0 Y0",
                "M3 S300",
                "G1 X15 Y15 F800",
                "M5 S0"
            }, "newfile.nc", append: false);

            Assert.False(file.HasCutAroundEnabled);
            Assert.False(file.CutAroundConfig.Enabled);
            Assert.Null(file.BaseEngravingCommands);
            Assert.Equal(4, file.Commands.Count);
        }

        [Fact]
        public void BedTransforms_TranslateMoveResetRotate_KeepEngravingAndCutSynchronized()
        {
            var file = CreateSampleRectangleEngravingFile(10, 10, 40, 20); // 30x10 engraving
            file.ApplyCutAround(new CutAroundConfig
            {
                Enabled = true,
                Shape = CutAroundShape.Rectangle,
                MarginMm = 2.0,
                SpeedMmMin = 300,
                PowerPercent = 80,
                Passes = 1
            });

            // 1. Translate (+15, +25) -> cut [23..57] x [33..47], base engraving [25..55] x [35..45]
            file.Translate(15m, 25m);
            Assert.Equal(23m, Math.Round(file.Range.DrawingRange.X.Min, 3));
            Assert.Equal(57m, Math.Round(file.Range.DrawingRange.X.Max, 3));
            Assert.Equal(33m, Math.Round(file.Range.DrawingRange.Y.Min, 3));
            Assert.Equal(47m, Math.Round(file.Range.DrawingRange.Y.Max, 3));
            var baseRange1 = MeasureBurningRange(file.BaseEngravingCommands);
            Assert.Equal(25m, Math.Round(baseRange1.X.Min, 3));
            Assert.Equal(55m, Math.Round(baseRange1.X.Max, 3));

            // 2. MoveTo (100, 100, center: true) -> combined center at (100, 100) AND engraving center at (100, 100)
            file.MoveTo(100m, 100m, center: true);
            Assert.Equal(100f, file.Range.DrawingRange.Center.X, 2);
            Assert.Equal(100f, file.Range.DrawingRange.Center.Y, 2);
            var baseRange2 = MeasureBurningRange(file.BaseEngravingCommands);
            Assert.Equal(100f, baseRange2.Center.X, 2);
            Assert.Equal(100f, baseRange2.Center.Y, 2);

            // 3. ResetToOrigin -> outer cut corner at (0, 0), base engraving at (2, 2)
            file.ResetToOrigin();
            Assert.Equal(0m, Math.Round(file.Range.DrawingRange.X.Min, 3));
            Assert.Equal(0m, Math.Round(file.Range.DrawingRange.Y.Min, 3));
            var baseRange3 = MeasureBurningRange(file.BaseEngravingCommands);
            Assert.Equal(2m, Math.Round(baseRange3.X.Min, 3));
            Assert.Equal(2m, Math.Round(baseRange3.Y.Min, 3));

            // 4. Rotate 90 deg -> 30x10 engraving becomes 10x30, cut becomes 14x34
            file.Rotate(90m);
            Assert.Equal(14m, Math.Round(file.Range.DrawingRange.Width, 2));
            Assert.Equal(34m, Math.Round(file.Range.DrawingRange.Height, 2));
            var baseRange4 = MeasureBurningRange(file.BaseEngravingCommands);
            Assert.Equal(10m, Math.Round(baseRange4.Width, 2));
            Assert.Equal(30m, Math.Round(baseRange4.Height, 2));
        }

        [Fact]
        public void CutAroundForm_BindsControlsAndProducesExpectedConfig()
        {
            var engraving = CreateSampleRectangleEngravingFile(10, 10, 40, 30).Commands;
            var initial = new CutAroundConfig
            {
                Enabled = false,
                Shape = CutAroundShape.RoundedRectangle,
                MarginMm = 3.5,
                CornerRadiusMm = 4.0,
                SpeedMmMin = 450,
                PowerPercent = 70,
                Passes = 2,
                LaserMode = "M4",
                ZStepMm = 0.4,
                AirAssist = true
            };

            using (var form = new CutAroundForm(initial, engraving, new CutMachineOptions { MaxPWM = 1000, HardwarePwm = true }))
            {
                Assert.Equal((int)CutAroundShape.RoundedRectangle, form.ShapeComboBox.SelectedIndex);
                Assert.True(form.CornerRadiusInput.Enabled);
                Assert.Equal(3.5m, form.MarginInput.Value);
                Assert.Equal(4.0m, form.CornerRadiusInput.Value);
                Assert.Equal(450m, form.SpeedInput.Value);
                Assert.Equal(70m, form.PowerInput.Value);
                Assert.Contains("S700", form.PowerSLabel.Text);
                Assert.Equal(2m, form.PassesInput.Value);
                Assert.Equal(1, form.LaserModeComboBox.SelectedIndex); // M4
                Assert.Equal(0.4m, form.ZStepInput.Value);
                Assert.True(form.AirAssistCheckBox.Checked);

                // Switching to Circle disables CornerRadiusInput
                form.ShapeComboBox.SelectedIndex = (int)CutAroundShape.Circle;
                Assert.False(form.CornerRadiusInput.Enabled);

                // Updating values and building config
                form.MarginInput.Value = 5.0m;
                form.SpeedInput.Value = 600m;
                form.PowerInput.Value = 90m;
                form.PassesInput.Value = 4m;
                Assert.Contains("S900", form.PowerSLabel.Text);

                var appliedCfg = form.BuildConfigFromControls(true);
                Assert.True(appliedCfg.Enabled);
                Assert.Equal(CutAroundShape.Circle, appliedCfg.Shape);
                Assert.Equal(5.0, appliedCfg.MarginMm);
                Assert.Equal(600, appliedCfg.SpeedMmMin);
                Assert.Equal(90, appliedCfg.PowerPercent);
                Assert.Equal(4, appliedCfg.Passes);
                Assert.Equal("M4", appliedCfg.LaserMode);

                // Trigger Apply -> sets DialogResult.OK and Enabled = true
                form.CommitApply();
                Assert.Equal(DialogResult.OK, form.DialogResult);
                Assert.True(form.ResultConfig.Enabled);
                Assert.Equal(CutAroundShape.Circle, form.ResultConfig.Shape);

                // Trigger Disable -> sets DialogResult.OK and Enabled = false
                form.CommitDisable();
                Assert.Equal(DialogResult.OK, form.DialogResult);
                Assert.False(form.ResultConfig.Enabled);
            }
        }

        [Fact]
        public void GrblPanel3D_ExposesCutAroundControlsAndSyncsWithLoadedItem()
        {
            using (var sync = new Control())
            using (var preview = new PreviewForm())
            using (var jog = new JogForm())
            {
                var core = new GrblCore(sync, preview, jog);
                var panel = preview.GrblPanel as GrblPanel3D;
                if (panel == null)
                    return; // GDI fallback mode

                panel.SetCore(core);
                panel.UpdateCutAroundUiState();

                // Before loading an image/drawing, cut-around bed controls are disabled
                Assert.NotNull(panel.CutAroundButton);
                Assert.NotNull(panel.CutAroundCheckBox);
                Assert.NotNull(panel.CutAroundConfigButton);
                Assert.False(panel.CutAroundButton.Enabled);
                Assert.False(panel.CutAroundCheckBox.Enabled);

                // Load engraving into core
                core.LoadedFile.LoadCommands(new[]
                {
                    "G0 X10 Y10",
                    "M3 S500",
                    "G1 X30 Y10 F1000",
                    "G1 X30 Y30",
                    "G1 X10 Y30",
                    "G1 X10 Y10",
                    "M5 S0"
                }, "bed-item.nc");

                panel.UpdateCutAroundUiState();
                Assert.True(panel.CutAroundButton.Enabled);
                Assert.True(panel.CutAroundCheckBox.Enabled);
                Assert.False(panel.CutAroundCheckBox.Checked);

                // Enable cut-around via GrblCore
                core.ApplyCutAround(new CutAroundConfig
                {
                    Enabled = true,
                    Shape = CutAroundShape.Rectangle,
                    MarginMm = 3.0,
                    SpeedMmMin = 350,
                    PowerPercent = 80,
                    Passes = 1
                }, new CutMachineOptions { MaxPWM = 1000, HardwarePwm = true });

                panel.UpdateCutAroundUiState();
                Assert.True(core.HasCutAroundEnabled);
                Assert.True(panel.CutAroundCheckBox.Checked);
                Assert.Contains("Ativo", panel.CutAroundButton.Text);

                // Disable cut-around via GrblCore
                core.DisableCutAround();
                panel.UpdateCutAroundUiState();
                Assert.False(core.HasCutAroundEnabled);
                Assert.False(panel.CutAroundCheckBox.Checked);
            }
        }

        [Fact]
        public void M4DynamicModeEngraving_IgnoresG0Travel_AndAppendResetsCutAround()
        {
            var file = new GrblFile();
            file.LoadCommands(new[]
            {
                "M4 S500",
                "G0 X0 Y0",
                "G0 X15 Y15", // under M4, G0 must NOT burn
                "G1 X35 Y15 F1000",
                "G1 X35 Y25",
                "G0 X100 Y100", // trailing G0 under M4 must NOT burn
                "M5 S0"
            }, "m4-engraving.nc");

            var geom = CutAroundGenerator.ComputeEngravingGeometry(file.Commands);
            Assert.True(geom.IsValid);
            Assert.Equal(15.0, geom.MinX, 3);
            Assert.Equal(35.0, geom.MaxX, 3);
            Assert.Equal(15.0, geom.MinY, 3);
            Assert.Equal(25.0, geom.MaxY, 3);

            file.ApplyCutAround(new CutAroundConfig { Enabled = true, Shape = CutAroundShape.Rectangle, MarginMm = 2.0 });
            Assert.True(file.HasCutAroundEnabled);

            // Append additional engraving -> strips old cut and resets CutAroundConfig.Enabled
            file.LoadCommands(new[]
            {
                "G0 X50 Y50",
                "M3 S400",
                "G1 X60 Y50 F1000",
                "M5 S0"
            }, "appended.nc", append: true);

            Assert.False(file.HasCutAroundEnabled);
            Assert.False(file.CutAroundConfig.Enabled);
            Assert.Null(file.BaseEngravingCommands);
            Assert.Equal(11, file.Commands.Count); // 7 original + 4 appended, no stale cut commands
        }

        [Fact]
        public void CutAroundForm_PreviewPaintAndOutOfBedWarning_WorkCleanly()
        {
            // Engraving near bed edge (295..299) with 5mm margin exceeds 300mm bed
            var nearEdge = CreateSampleRectangleEngravingFile(290, 290, 298, 298).Commands;
            using (var form = new CutAroundForm(
                new CutAroundConfig { Enabled = true, Shape = CutAroundShape.Contour, MarginMm = 5.0 },
                nearEdge,
                new CutMachineOptions { MaxPWM = 1000, HardwarePwm = true },
                bedWidthMm: 300.0,
                bedHeightMm: 300.0))
            {
                Assert.Contains("ultrapassa", form.BedWarningLabel.Text);

                using (var bmp = new Bitmap(form.PreviewPanel.Width, form.PreviewPanel.Height))
                {
                    form.PreviewPanel.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                    Assert.True(bmp.Width > 0 && bmp.Height > 0);
                }
            }
        }

        [Fact]
        public void RoundedRectangle_AlwaysEnclosesEngravingCornersWithConfiguredMargin()
        {
            var cases = new[]
            {
                new { MinX = 10.0, MinY = 10.0, MaxX = 50.0, MaxY = 60.0, Margin = 0.0, Radius = 3.0 },
                new { MinX = 10.0, MinY = 10.0, MaxX = 50.0, MaxY = 60.0, Margin = 0.5, Radius = 3.0 },
                new { MinX = 10.0, MinY = 10.0, MaxX = 50.0, MaxY = 60.0, Margin = 2.0, Radius = 3.0 },
                new { MinX = 10.0, MinY = 10.0, MaxX = 50.0, MaxY = 60.0, Margin = 1.0, Radius = 25.0 },
                new { MinX = 20.0, MinY = 15.0, MaxX = 60.0, MaxY = 45.0, Margin = 4.0, Radius = 999.0 },
                new { MinX = 0.0,  MinY = 0.0,  MaxX = 200.0, MaxY = 0.5, Margin = 0.5, Radius = 3.0 },
                new { MinX = 0.0,  MinY = 0.0,  MaxX = 200.0, MaxY = 0.5, Margin = 2.0, Radius = 3.0 },
            };

            foreach (var tc in cases)
            {
                var engraving = CreateSampleRectangleEngravingFile(tc.MinX, tc.MinY, tc.MaxX, tc.MaxY).Commands;
                var res = CutAroundGenerator.BuildCutAround(engraving, new CutAroundConfig
                {
                    Enabled = true,
                    Shape = CutAroundShape.RoundedRectangle,
                    MarginMm = tc.Margin,
                    CornerRadiusMm = tc.Radius,
                    SpeedMmMin = 300
                });

                Assert.True(res.IsValid);
                List<PointF> gcodeLoop = ExtractBurningLoopPointsFromCommands(res.CutCommands);
                Assert.True(gcodeLoop.Count >= 16);

                PointF[] corners = new[]
                {
                    new PointF((float)tc.MinX, (float)tc.MinY),
                    new PointF((float)tc.MaxX, (float)tc.MinY),
                    new PointF((float)tc.MaxX, (float)tc.MaxY),
                    new PointF((float)tc.MinX, (float)tc.MaxY)
                };

                foreach (var corner in corners)
                {
                    Assert.True(IsPointInsidePolygon(corner, gcodeLoop),
                        $"Corner ({corner.X},{corner.Y}) outside RoundedRect G-code loop for Margin={tc.Margin}, R={tc.Radius}");
                    double dist = MinDistanceToPolygonEdges(corner, gcodeLoop);
                    Assert.True(dist >= tc.Margin - 0.02,
                        $"Corner ({corner.X},{corner.Y}) dist={dist:0.000} < Margin={tc.Margin} for R={tc.Radius}");
                }
            }

            // Degenerate single burning point with large corner radius still emits valid G3 arcs with F
            var singlePoint = new List<GrblCommand>
            {
                new GrblCommand("G0 X25 Y25"),
                new GrblCommand("M3 S500"),
                new GrblCommand("G1 X25 Y25 F1000"),
                new GrblCommand("M5 S0")
            };
            var spRes = CutAroundGenerator.BuildCutAround(singlePoint, new CutAroundConfig
            {
                Enabled = true,
                Shape = CutAroundShape.RoundedRectangle,
                MarginMm = 2.0,
                CornerRadiusMm = 5.0,
                SpeedMmMin = 320
            });
            Assert.True(spRes.IsValid);
            Assert.Contains(spRes.CutCommands, c => c.Command.Contains("F320"));
            var spLoop = ExtractBurningLoopPointsFromCommands(spRes.CutCommands);
            Assert.True(IsPointInsidePolygon(new PointF(25f, 25f), spLoop));
            Assert.True(MinDistanceToPolygonEdges(new PointF(25f, 25f), spLoop) >= 1.98);
        }

        [Fact]
        public void Contour_LargeRadiusG2AndG3Arcs_AdaptivelySampledAndEnclosedWithMargin()
        {
            var arcCases = new[]
            {
                new { StartX = 0.0, StartY = 200.0, Cmd = "G3 X-200 Y0 I0 J-200 F1000", StartAngle = Math.PI / 2.0, Sweep = Math.PI / 2.0 },
                new { StartX = 0.0, StartY = 200.0, Cmd = "G2 X200 Y0 I0 J-200 F1000", StartAngle = Math.PI / 2.0, Sweep = -Math.PI / 2.0 },
                new { StartX = 0.0, StartY = 200.0, Cmd = "G3 X200 Y0 I0 J-200 F1000", StartAngle = Math.PI / 2.0, Sweep = 1.5 * Math.PI },
            };

            double[] margins = new[] { 0.5, 2.0 };

            foreach (var ac in arcCases)
            {
                var engraving = new List<GrblCommand>
                {
                    new GrblCommand(string.Format(CultureInfo.InvariantCulture, "G0 X{0:0.###} Y{1:0.###}", ac.StartX, ac.StartY)),
                    new GrblCommand("M3 S500"),
                    new GrblCommand(ac.Cmd),
                    new GrblCommand("M5 S0")
                };

                foreach (double margin in margins)
                {
                    var res = CutAroundGenerator.BuildCutAround(engraving, new CutAroundConfig
                    {
                        Enabled = true,
                        Shape = CutAroundShape.Contour,
                        MarginMm = margin
                    });

                    Assert.True(res.IsValid);

                    // Check dense 0.5-degree analytical points along the true R=200 arc
                    int sampleCount = (int)Math.Ceiling(Math.Abs(ac.Sweep) / (Math.PI / 360.0));
                    for (int s = 0; s <= sampleCount; s++)
                    {
                        double angle = ac.StartAngle + ac.Sweep * (s / (double)sampleCount);
                        PointF truePt = new PointF((float)(200.0 * Math.Cos(angle)), (float)(200.0 * Math.Sin(angle)));
                        Assert.True(IsPointInsidePolygon(truePt, res.CutPolygon),
                            $"Arc point ({truePt.X:0.000},{truePt.Y:0.000}) outside Contour for {ac.Cmd}, Margin={margin}");
                        double dist = MinDistanceToPolygonEdges(truePt, res.CutPolygon);
                        Assert.True(dist >= margin - 0.02,
                            $"Arc point ({truePt.X:0.000},{truePt.Y:0.000}) dist={dist:0.000} < Margin={margin} for {ac.Cmd}");
                    }
                }
            }
        }

        [Fact]
        public void DisableG0FastSkip_AlwaysEmitsFeedrateOnTravelAndZMoves()
        {
            var engravings = new[]
            {
                // No prior F anywhere in engraving
                new List<GrblCommand>
                {
                    new GrblCommand("G0 X10 Y10"),
                    new GrblCommand("M3 S500"),
                    new GrblCommand("G1 X30 Y30"),
                    new GrblCommand("M5 S0")
                },
                // Slow F10 at end of engraving
                new List<GrblCommand>
                {
                    new GrblCommand("G0 X10 Y10"),
                    new GrblCommand("M3 S500"),
                    new GrblCommand("G1 X30 Y30 F10"),
                    new GrblCommand("M5 S0")
                }
            };

            CutAroundShape[] shapes = new[]
            {
                CutAroundShape.Rectangle,
                CutAroundShape.RoundedRectangle,
                CutAroundShape.Circle
            };

            foreach (var eng in engravings)
            {
                foreach (var shape in shapes)
                {
                    var res = CutAroundGenerator.BuildCutAround(eng, new CutAroundConfig
                    {
                        Enabled = true,
                        Shape = shape,
                        MarginMm = 2.0,
                        CornerRadiusMm = 3.0,
                        SpeedMmMin = 350,
                        Passes = 2,
                        ZStepMm = 0.5
                    }, new CutMachineOptions { DisableG0FastSkip = true });

                    Assert.True(res.IsValid);
                    Assert.DoesNotContain(res.CutCommands, c => c.Command.StartsWith("G0 "));

                    // Verify using StatePositionBuilder that EVERY G1/G2/G3 movement in CutCommands has F == 350 (> 0)
                    var spb = new GrblCommand.StatePositionBuilder();
                    var conf = new GrblConfST();
                    foreach (var cmd in res.CutCommands)
                    {
                        bool del = !cmd.JustBuilt;
                        if (del) cmd.BuildHelper();
                        try
                        {
                            spb.AnalyzeCommand(cmd, true, conf);
                            if (cmd.IsMovement)
                            {
                                Assert.False(spb.G0);
                                Assert.Equal(350m, spb.F.Number);
                            }
                        }
                        finally
                        {
                            if (del) cmd.DeleteHelper();
                        }
                    }
                }
            }
        }

        [Fact]
        public void GrblFile_ApplyCutAround_OnNonBurningGCode_DoesNotEnableCutAround()
        {
            var file = new GrblFile();
            file.LoadCommands(new[]
            {
                "G0 X10 Y10",
                "G0 X30 Y30"
            }, "no-burn.nc");

            file.ApplyCutAround(new CutAroundConfig
            {
                Enabled = true,
                Shape = CutAroundShape.Rectangle,
                MarginMm = 2.0,
                SpeedMmMin = 300,
                PowerPercent = 80
            });

            Assert.False(file.HasCutAroundEnabled);
            Assert.False(file.CutAroundConfig.Enabled);
            Assert.Null(file.BaseEngravingCommands);
            Assert.Null(file.CutAroundResult);
            Assert.Equal(new[] { "G0 X10 Y10", "G0 X30 Y30" }, file.Commands.Select(c => c.Command).ToArray());
        }

        private static List<PointF> ExtractBurningLoopPointsFromCommands(IEnumerable<GrblCommand> commands, bool hardwarePwm = true)
        {
            var pts = new List<PointF>();
            var spb = new GrblCommand.StatePositionBuilder();
            var conf = new GrblConfST();

            foreach (var cmd in commands)
            {
                bool del = !cmd.JustBuilt;
                if (del) cmd.BuildHelper();
                try
                {
                    spb.LastArcHelperResult = null;
                    spb.AnalyzeCommand(cmd, true, conf);
                    if (cmd.IsMovement && spb.G2G3 && spb.LastArcHelperResult == null)
                        spb.GetArcHelper(cmd);

                    if (spb.IsLaserBurning(hardwarePwm) && cmd.IsMovement)
                    {
                        if (pts.Count == 0)
                            pts.Add(new PointF((float)spb.X.Previous, (float)spb.Y.Previous));

                        if (spb.G2G3 && spb.LastArcHelperResult != null)
                        {
                            var arc = spb.LastArcHelperResult;
                            double rStart = Math.Sqrt(Math.Pow((double)spb.X.Previous - arc.CenterX, 2) + Math.Pow((double)spb.Y.Previous - arc.CenterY, 2));
                            double rEnd = Math.Sqrt(Math.Pow((double)spb.X.Number - arc.CenterX, 2) + Math.Pow((double)spb.Y.Number - arc.CenterY, 2));
                            Assert.True(Math.Abs(rStart - rEnd) <= 0.005, $"Arc radius mismatch in {cmd.Command}: rStart={rStart:0.0000}, rEnd={rEnd:0.0000}");

                            int steps = Math.Max(16, (int)Math.Ceiling(Math.Abs(arc.AngularWidth) / (Math.PI / 180.0)));
                            for (int s = 1; s <= steps; s++)
                            {
                                double angle = arc.StartAngle + arc.AngularWidth * (s / (double)steps);
                                pts.Add(new PointF((float)(arc.CenterX + arc.Ray * Math.Cos(angle)), (float)(arc.CenterY + arc.Ray * Math.Sin(angle))));
                            }
                        }
                        else
                        {
                            pts.Add(new PointF((float)spb.X.Number, (float)spb.Y.Number));
                        }
                    }
                }
                finally
                {
                    if (del) cmd.DeleteHelper();
                }
            }
            return pts;
        }

        private static bool IsPointInsidePolygon(PointF pt, IList<PointF> poly)
        {
            bool inside = false;
            int count = poly.Count;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                float xi = poly[i].X, yi = poly[i].Y;
                float xj = poly[j].X, yj = poly[j].Y;
                bool intersect = ((yi > pt.Y) != (yj > pt.Y)) &&
                                 (pt.X < (xj - xi) * (pt.Y - yi) / (double)(yj - yi) + xi);
                if (intersect) inside = !inside;
            }
            return inside;
        }

        private static double MinDistanceToPolygonEdges(PointF p, IList<PointF> poly)
        {
            double minDist = double.MaxValue;
            for (int i = 0; i < poly.Count - 1; i++)
            {
                double d = PointToSegmentDistance(p, poly[i], poly[i + 1]);
                if (d < minDist) minDist = d;
            }
            return minDist;
        }

        private static double PointToSegmentDistance(PointF p, PointF a, PointF b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double lenSq = dx * dx + dy * dy;
            if (lenSq < 1e-12)
                return Math.Sqrt(Math.Pow(p.X - a.X, 2) + Math.Pow(p.Y - a.Y, 2));

            double t = Math.Max(0.0, Math.Min(1.0, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq));
            double projX = a.X + t * dx;
            double projY = a.Y + t * dy;
            return Math.Sqrt(Math.Pow(p.X - projX, 2) + Math.Pow(p.Y - projY, 2));
        }
    }
}
