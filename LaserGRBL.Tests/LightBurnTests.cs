using System;
using System.Collections.Generic;
using System.Drawing;
using LaserGRBL.Camera;
using LaserGRBL.LightBurn;
using Xunit;

namespace LaserGRBL.Tests
{
    public class LightBurnTests
    {
        [Fact]
        public void ConvexHull_CalculatesCorrectOuterContour()
        {
            // Given a square with interior points
            List<PointF> points = new List<PointF>
            {
                new PointF(0, 0),
                new PointF(10, 0),
                new PointF(10, 10),
                new PointF(0, 10),
                new PointF(5, 5),   // interior point
                new PointF(2, 8),   // interior point
                new PointF(8, 2)    // interior point
            };

            List<PointF> hull = FramingHelper.ComputeConvexHull(points);

            // Hull closes loop, so 4 corners + 1 closing point = 5 points
            Assert.True(hull.Count >= 4);
            Assert.Contains(hull, p => p.X == 0 && p.Y == 0);
            Assert.Contains(hull, p => p.X == 10 && p.Y == 0);
            Assert.Contains(hull, p => p.X == 10 && p.Y == 10);
            Assert.Contains(hull, p => p.X == 0 && p.Y == 10);
        }

        [Fact]
        public void ConvexHull_HandlesDegenerateCasesGracefully()
        {
            List<PointF> empty = new List<PointF>();
            Assert.Empty(FramingHelper.ComputeConvexHull(empty));

            List<PointF> single = new List<PointF> { new PointF(5, 5) };
            Assert.Single(FramingHelper.ComputeConvexHull(single));
        }

        [Fact]
        public void LightBurnLayers_InitializesEightDistinctLayers()
        {
            List<LightBurnLayer> layers = LightBurnLayerManager.CurrentLayers;

            Assert.Equal(8, layers.Count);
            Assert.Equal("C00", layers[0].Code);
            Assert.Equal("C07", layers[7].Code);

            // Verify each layer has unique color
            HashSet<Color> colors = new HashSet<Color>();
            foreach (var l in layers)
            {
                Assert.True(colors.Add(l.Color), $"Duplicate color found in layer {l.Code}");
            }
        }

        [Fact]
        public void LightBurnLayers_MaterialPresetsAvailable()
        {
            var presets = LightBurnLayerManager.GetMaterialPresets();

            Assert.NotEmpty(presets);
            Assert.True(presets.ContainsKey("MDF 3mm (Corte & Gravação)"));
            Assert.True(presets.ContainsKey("Compensado 3mm (Plywood)"));
            Assert.True(presets.ContainsKey("Acrílico 2mm / 3mm"));
            Assert.True(presets.ContainsKey("Couro Legítimo"));
            Assert.True(presets.ContainsKey("Papelão Kraft / Cartolina"));
        }

        [Fact]
        public void MaterialTestGenerator_GeneratesValidGridGCode()
        {
            MaterialTestConfig cfg = new MaterialTestConfig
            {
                Title = "TESTE MDF 3MM",
                Mode = "Fill",
                Columns = 4,
                Rows = 4,
                MinSpeed = 600,
                MaxSpeed = 2400,
                MinPowerPct = 20,
                MaxPowerPct = 90,
                BoxSize = 7.0,
                Spacing = 2.0,
                FillInterval = 0.2,
                Passes = 1,
                MaxPwm = 1000
            };

            string gcode = MaterialTestGenerator.GenerateGCode(cfg);

            Assert.False(string.IsNullOrEmpty(gcode));
            // Verify header and G-code commands
            Assert.Contains("=== LaserGRBL LightBurn Material Test Matrix ===", gcode);
            Assert.Contains("TESTE MDF 3MM", gcode);
            Assert.Contains("G90", gcode);
            Assert.Contains("G21", gcode);
            Assert.Contains("M5 S0", gcode);
        }

        [Fact]
        public void QuickShapesGenerator_GeneratesRectangleCorrectly()
        {
            QuickShapeConfig cfg = new QuickShapeConfig
            {
                Type = ShapeType.Rectangle,
                Width = 50.0,
                Height = 30.0,
                Speed = 1200,
                PowerPct = 80,
                Passes = 2,
                CornerRadius = 0,
                GridCols = 1,
                GridRows = 1,
                AirAssist = true,
                MaxPwm = 1000
            };

            string gcode = QuickShapesGenerator.GenerateGCode(cfg);

            Assert.False(string.IsNullOrEmpty(gcode));
            Assert.Contains("Forma: Rectangle", gcode);
            Assert.Contains("F1200", gcode);
            Assert.Contains("S800", gcode); // 80% of 1000
            Assert.Contains("X50.000", gcode);
            Assert.Contains("Y30.000", gcode);
            Assert.Contains("M8 ; Air Assist Ligado", gcode);
            Assert.Contains("M9 ; Desliga Air Assist", gcode);
        }

        [Fact]
        public void QuickShapesGenerator_GeneratesCircleWithArcs()
        {
            QuickShapeConfig cfg = new QuickShapeConfig
            {
                Type = ShapeType.Circle,
                Diameter = 40.0,
                Speed = 1000,
                PowerPct = 75,
                Passes = 1,
                GridCols = 1,
                GridRows = 1
            };

            string gcode = QuickShapesGenerator.GenerateGCode(cfg);

            Assert.False(string.IsNullOrEmpty(gcode));
            Assert.Contains("Forma: Circle", gcode);
            Assert.Contains("G2", gcode); // G2 clockwise arcs
        }

        [Fact]
        public void QuickShapesGenerator_GeneratesPolygonsAndStars()
        {
            QuickShapeConfig polyCfg = new QuickShapeConfig
            {
                Type = ShapeType.Polygon,
                Sides = 6,
                Radius = 25.0,
                Speed = 800,
                PowerPct = 60
            };

            string polyGCode = QuickShapesGenerator.GenerateGCode(polyCfg);
            Assert.Contains("Forma: Polygon", polyGCode);

            QuickShapeConfig starCfg = new QuickShapeConfig
            {
                Type = ShapeType.Star,
                Sides = 5,
                Radius = 30.0,
                InnerRadius = 12.0,
                Speed = 800,
                PowerPct = 60
            };

            string starGCode = QuickShapesGenerator.GenerateGCode(starCfg);
            Assert.Contains("Forma: Star", starGCode);
        }

        [Fact]
        public void RotaryMath_CalculatesCorrectAdjustedStepsPerMm()
        {
            // Roller rotary test:
            // Formula: newSteps = originalSteps * (rollerDiameter / objectDiameter)
            decimal originalSteps = 80.0m;
            decimal rollerDiameter = 20.0m;
            decimal objectDiameter = 60.0m;

            decimal calculatedRollerSteps = originalSteps * (rollerDiameter / objectDiameter);
            Assert.Equal(26.6667m, Math.Round(calculatedRollerSteps, 4));

            // Chuck rotary test:
            // Formula: stepsPerMm = (stepsPerRev * microsteps * gearRatio) / (PI * diameter)
            int stepsPerRev = 200;
            int microsteps = 16;
            decimal gearRatio = 2.5m;
            decimal chuckDiameter = 50.0m;

            decimal totalMotorStepsPerRev = (decimal)(stepsPerRev * microsteps) * gearRatio; // 8000
            decimal circumference = (decimal)Math.PI * chuckDiameter; // ~157.0796
            decimal calculatedChuckSteps = totalMotorStepsPerRev / circumference;

            Assert.True(calculatedChuckSteps > 50.0m && calculatedChuckSteps < 52.0m);
        }

        [Fact]
        public void CameraOverlay_HomographyCalculatesMappingCorrectly()
        {
            // Simple 2x scaling homography test
            PointF[] src = new PointF[]
            {
                new PointF(0, 0),
                new PointF(100, 0),
                new PointF(100, 100),
                new PointF(0, 100)
            };
            PointF[] dst = new PointF[]
            {
                new PointF(0, 0),
                new PointF(200, 0),
                new PointF(200, 200),
                new PointF(0, 200)
            };

            double[] H = CameraOverlayManager.ComputeHomography(src, dst);

            Assert.NotNull(H);
            Assert.Equal(9, H.Length);

            // Test mapping of center point (50, 50) -> should map to (100, 100)
            double x = 50.0;
            double y = 50.0;
            double d = H[6] * x + H[7] * y + H[8];
            double u = (H[0] * x + H[1] * y + H[2]) / d;
            double v = (H[3] * x + H[4] * y + H[5]) / d;

            Assert.InRange(u, 99.0, 101.0);
            Assert.InRange(v, 99.0, 101.0);
        }

        [Fact]
        public void CameraOverlay_WarpPerspectiveGeneratesBitmap()
        {
            using (Bitmap bmp = new Bitmap(100, 100))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.CornflowerBlue);
                }

                PointF[] corners = new PointF[]
                {
                    new PointF(0.1f, 0.1f),
                    new PointF(0.9f, 0.1f),
                    new PointF(0.9f, 0.9f),
                    new PointF(0.1f, 0.9f)
                };

                using (Bitmap warped = CameraOverlayManager.WarpPerspective(bmp, corners, 200, 200))
                {
                    Assert.NotNull(warped);
                    Assert.Equal(200, warped.Width);
                    Assert.Equal(200, warped.Height);
                }
            }
        }

        [Fact]
        public void LaserCameraServer_GetLocalUrl_GeneratesValidHttpEndpoint()
        {
            string url = LaserCameraServer.GetLocalUrl();

            Assert.False(string.IsNullOrEmpty(url));
            Assert.StartsWith("http://", url);
            Assert.Contains($":{LaserCameraServer.Port}/", url);
        }

        [Fact]
        public void LaserCameraServer_GenerateQRCode_ProducesValidBitmap()
        {
            using (Bitmap qr = LaserCameraServer.GenerateQRCode("http://192.168.1.100:8088/", 220))
            {
                Assert.NotNull(qr);
                Assert.Equal(220, qr.Width);
                Assert.Equal(220, qr.Height);
            }
        }

        [Fact]
        public void GrblFile_TranslateGCodeLine_TranslatesCoordinatesCorrectly()
        {
            // Case 1: Standard G0 move with positive coordinates
            string res1 = GrblFile.TranslateGCodeLine("G0 X10 Y20", 5m, -2m);
            Assert.Equal("G0 X15 Y18", res1);

            // Case 2: G1 move with decimals, speed, and spindle power
            string res2 = GrblFile.TranslateGCodeLine("G1 X-17.5 Y0.0 S1000 F800", 20m, 10.5m);
            Assert.Equal("G1 X2.5 Y10.5 S1000 F800", res2);

            // Case 3: Arc motion with I and J vector offsets (I and J must not change)
            string res3 = GrblFile.TranslateGCodeLine("G2 X25 Y30 I5 J0", 10m, 10m);
            Assert.Equal("G2 X35 Y40 I5 J0", res3);

            // Case 4: Machine coordinates (G53) and coordinate offset reset (G92) must not be translated
            string res4 = GrblFile.TranslateGCodeLine("G53 G0 X0 Y0", 50m, 50m);
            Assert.Equal("G53 G0 X0 Y0", res4);

            string res5 = GrblFile.TranslateGCodeLine("G92 X0 Y0", 50m, 50m);
            Assert.Equal("G92 X0 Y0", res5);

            // Case 5: Comments must be preserved
            string res6 = GrblFile.TranslateGCodeLine("G1 X10 Y20 ; Test comment", 5m, 5m);
            Assert.Equal("G1 X15 Y25 ; Test comment", res6);

            string res7 = GrblFile.TranslateGCodeLine("; Full line comment with X10 Y20", 5m, 5m);
            Assert.Equal("; Full line comment with X10 Y20", res7);
        }

        [Fact]
        public void CustomButtons_EnsureFocusButton_CreatesLaserThreePercentButton()
        {
            CustomButtons.EnsureFocusButton();
            Assert.True(CustomButtons.Count > 0);

            bool found = false;
            foreach (CustomButton b in CustomButtons.Buttons)
            {
                if (b.Caption == "Focus")
                {
                    found = true;
                    Assert.Contains("M3 S30", b.GCode);
                    Assert.Contains("G1 F1", b.GCode);
                    Assert.Contains("M5", b.GCode2);
                    Assert.Equal(CustomButton.ButtonTypes.TwoStateButton, b.ButtonType);
                    break;
                }
            }
            Assert.True(found, "Focus button was not found");
        }

        [Fact]
        public void CustomButtons_EnsureZeroButtons_CreatesZeroAndGoZeroButtons()
        {
            CustomButtons.EnsureZeroButtons();
            Assert.True(CustomButtons.Count > 0);

            bool foundZero = false;
            bool foundGoZero = false;
            foreach (CustomButton b in CustomButtons.Buttons)
            {
                if (b.Caption == "Zero")
                {
                    foundZero = true;
                    Assert.Contains("G92 X0 Y0", b.GCode);
                    Assert.Equal(CustomButton.ButtonTypes.Button, b.ButtonType);
                    Assert.Equal(CustomButton.EnableStyles.Connected, b.EnableStyle);
                }
                if (b.Caption == "GoZero")
                {
                    foundGoZero = true;
                    Assert.Contains("G0 X0 Y0", b.GCode);
                    Assert.Equal(CustomButton.ButtonTypes.Button, b.ButtonType);
                    Assert.Equal(CustomButton.EnableStyles.Connected, b.EnableStyle);
                }
            }
            Assert.True(foundZero, "Zero button was not found");
            Assert.True(foundGoZero, "GoZero button was not found");

            // Calling it again should not duplicate
            int countBefore = CustomButtons.Count;
            CustomButtons.EnsureZeroButtons();
            Assert.Equal(countBefore, CustomButtons.Count);
        }

        [Fact]
        public void RotateGCodeLine_RotatesCoordinatesAroundCenter()
        {
            // Point (10, 0) rotated 90 degrees around center (0, 0) becomes (0, 10)
            string rotated1 = GrblFile.RotateGCodeLine("G1 X10 Y0 F500", 90m, 0m, 0m);
            Assert.Equal("G1 X0 Y10 F500", rotated1);

            // Point (0, 10) rotated 90 degrees around center (0, 0) becomes (-10, 0)
            string rotated2 = GrblFile.RotateGCodeLine("G1 X0 Y10 F500", 90m, 0m, 0m);
            Assert.Equal("G1 X-10 Y0 F500", rotated2);

            // Arc vectors I5 J0 rotated 90 degrees becomes I0 J5
            string rotatedArc = GrblFile.RotateGCodeLine("G2 X10 Y0 I5 J0", 90m, 0m, 0m);
            Assert.Equal("G2 X0 Y10 I0 J5", rotatedArc);

            // Comments preserved
            string rotatedComment = GrblFile.RotateGCodeLine("; Comment line X10 Y10", 90m, 0m, 0m);
            Assert.Equal("; Comment line X10 Y10", rotatedComment);
        }

        [Fact]
        public void CameraOverlayManager_RotatePhoto_UpdatesAngle()
        {
            int startAngle = CameraOverlayManager.RotationAngle;
            CameraOverlayManager.RotatePhoto(90);
            Assert.Equal((startAngle + 90) % 360, CameraOverlayManager.RotationAngle);

            CameraOverlayManager.RotatePhoto(90);
            Assert.Equal((startAngle + 180) % 360, CameraOverlayManager.RotationAngle);

            CameraOverlayManager.RotatePhoto(180);
            Assert.Equal(startAngle, CameraOverlayManager.RotationAngle);
        }

        [Fact]
        public void CustomButtons_EnsureFrameButton_CreatesFramingButtonWith3PercentLaser()
        {
            CustomButtons.EnsureFrameButton();
            CustomButton frameBtn = null;
            foreach (CustomButton b in CustomButtons.Buttons)
            {
                if (b != null && b.Caption == "Frame")
                {
                    frameBtn = b;
                    break;
                }
            }

            Assert.NotNull(frameBtn);
            Assert.Contains("[left]", frameBtn.GCode);
            Assert.Contains("[right]", frameBtn.GCode);
            Assert.Contains("[top]", frameBtn.GCode);
            Assert.Contains("[bottom]", frameBtn.GCode);
            Assert.Contains("M3 S30", frameBtn.GCode);
            Assert.Contains("M5 S0", frameBtn.GCode);
            Assert.Contains("G0 X0 Y0", frameBtn.GCode);
            Assert.Equal(CustomButton.ButtonTypes.Button, frameBtn.ButtonType);
            Assert.Equal(CustomButton.EnableStyles.IdleProgram, frameBtn.EnableStyle);
        }

        [Fact]
        public void CustomButtons_EnsureBedButton_CreatesBed300Button()
        {
            CustomButtons.EnsureBedButton();
            CustomButton bedBtn = null;
            foreach (CustomButton b in CustomButtons.Buttons)
            {
                if (b != null && b.Caption == "Bed300")
                {
                    bedBtn = b;
                    break;
                }
            }

            Assert.NotNull(bedBtn);
            Assert.Contains("$130=300", bedBtn.GCode);
            Assert.Contains("$131=300", bedBtn.GCode);
            Assert.Equal(CustomButton.ButtonTypes.Button, bedBtn.ButtonType);
            Assert.Equal(CustomButton.EnableStyles.Connected, bedBtn.EnableStyle);
        }

        [Fact]
        public void MachineBed_BoundsVerification_DetectsOutOfBoundsCorrectly()
        {
            float bedWidth = 300f;
            float bedHeight = 300f;

            // Inside bounds
            float job1MinX = 10f, job1MaxX = 250f, job1MinY = 10f, job1MaxY = 280f;
            bool outOfBounds1 = job1MinX < 0 || job1MaxX > bedWidth || job1MinY < 0 || job1MaxY > bedHeight;
            Assert.False(outOfBounds1);

            // Exceeds X max (320 > 300)
            float job2MinX = 10f, job2MaxX = 320f, job2MinY = 10f, job2MaxY = 200f;
            bool outOfBounds2 = job2MinX < 0 || job2MaxX > bedWidth || job2MinY < 0 || job2MaxY > bedHeight;
            Assert.True(outOfBounds2);

            // Negative X (< 0)
            float job3MinX = -5f, job3MaxX = 150f, job3MinY = 0f, job3MaxY = 150f;
            bool outOfBounds3 = job3MinX < 0 || job3MaxX > bedWidth || job3MinY < 0 || job3MaxY > bedHeight;
            Assert.True(outOfBounds3);
        }

        [Fact]
        public void PhotoEnhanceParams_Presets_HaveValidAndDistinctValues()
        {
            var portrait = LaserGRBL.RasterConverter.PhotoEnhanceParams.CreatePortraitPreset();
            var highDetail = LaserGRBL.RasterConverter.PhotoEnhanceParams.CreateHighDetailPreset();
            var pet = LaserGRBL.RasterConverter.PhotoEnhanceParams.CreatePetPreset();
            var sketch = LaserGRBL.RasterConverter.PhotoEnhanceParams.CreateSketchPreset();
            var slate = LaserGRBL.RasterConverter.PhotoEnhanceParams.CreateSlatePreset();

            // Portrait: High gamma to prevent burned skin tones
            Assert.True(portrait.Gamma >= 1.5f);
            Assert.Equal(LaserGRBL.RasterConverter.ImageTransform.DitheringMode.Atkinson, portrait.Dithering);
            Assert.False(portrait.Invert);

            // Slate: Invert must be true
            Assert.True(slate.Invert);

            // Sketch: High sharpness and contrast
            Assert.True(sketch.Sharpness > portrait.Sharpness);
            Assert.True(sketch.Contrast > portrait.Contrast);
        }

        [Fact]
        public void PhotoEnhancer_Enhance_LiftsMidtonesWithGamma()
        {
            // Create a test bitmap with flat midtone gray (100) representing dark skin or shadows
            using (Bitmap bmp = new Bitmap(30, 30, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(100, 100, 100));
                }

                var p = new LaserGRBL.RasterConverter.PhotoEnhanceParams
                {
                    Gamma = 1.6f,
                    Sharpness = 0f,
                    LocalContrast = 0f,
                    Brightness = 0,
                    Contrast = 0,
                    BackgroundClean = 0,
                    Invert = false
                };

                using (Bitmap enhanced = LaserGRBL.RasterConverter.PhotoEnhancer.Enhance(bmp, p))
                {
                    Color px = enhanced.GetPixel(15, 15);
                    // With gamma 1.6, pixel (100/255)^0.625 * 255 ~= 142 -> significantly lifted from 100
                    Assert.True(px.R > 130, $"Expected lifted midtone > 130, got {px.R}");
                }
            }
        }

        [Fact]
        public void PhotoEnhancer_Enhance_CleansBackgroundNoise()
        {
            // Create a test bitmap with off-white background noise (245)
            using (Bitmap bmp = new Bitmap(20, 20, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(245, 245, 245));
                }

                var p = new LaserGRBL.RasterConverter.PhotoEnhanceParams
                {
                    Gamma = 1.0f,
                    Sharpness = 0f,
                    LocalContrast = 0f,
                    Brightness = 0,
                    Contrast = 0,
                    BackgroundClean = 15, // Cleans pixels >= 232.5 up to 255
                    Invert = false
                };

                using (Bitmap enhanced = LaserGRBL.RasterConverter.PhotoEnhancer.Enhance(bmp, p))
                {
                    Color px = enhanced.GetPixel(10, 10);
                    // Near-white noise should be cleaned to pure white (255)
                    Assert.True(px.R >= 253, $"Expected background noise cleaned to >= 253, got {px.R}");
                }
            }
        }

        [Fact]
        public void PhotoEnhancer_GenerateWoodBurnSimulation_ProducesBurnedTones()
        {
            using (Bitmap dithered = new Bitmap(10, 10, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                // Top half black (burned dots), bottom half white (unburned wood)
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        dithered.SetPixel(x, y, y < 5 ? Color.Black : Color.White);
                    }
                }

                using (Bitmap woodSim = LaserGRBL.RasterConverter.PhotoEnhancer.GenerateWoodBurnSimulation(dithered))
                {
                    Color burnedPx = woodSim.GetPixel(5, 2);
                    Color woodPx = woodSim.GetPixel(5, 7);

                    // Burned pixel should have warm deep charcoal tone (#2E1B10 => R=46, G=27, B=16)
                    Assert.InRange(burnedPx.R, 40, 55);
                    Assert.InRange(burnedPx.G, 20, 35);
                    Assert.InRange(burnedPx.B, 10, 25);

                    // Wood background should have warm birch/pine wood tone (> 180 for all channels)
                    Assert.True(woodPx.R > 200, $"Expected wood R > 200, got {woodPx.R}");
                    Assert.True(woodPx.G > 190, $"Expected wood G > 190, got {woodPx.G}");
                }
            }
        }

        [Fact]
        public void PhotoEnhancer_GenerateSlateSimulation_ProducesInvertedAshTones()
        {
            using (Bitmap dithered = new Bitmap(10, 10, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                // Pixel (0,0) is black (laser etched)
                dithered.SetPixel(0, 0, Color.Black);
                // Pixel (5,5) is white (dark stone background)
                dithered.SetPixel(5, 5, Color.White);

                using (Bitmap slateSim = LaserGRBL.RasterConverter.PhotoEnhancer.GenerateSlateSimulation(dithered))
                {
                    Color etchedPx = slateSim.GetPixel(0, 0);
                    Color stonePx = slateSim.GetPixel(5, 5);

                    // Laser etched mark should be light ash / white (> 200)
                    Assert.True(etchedPx.R > 210, $"Expected etched R > 210, got {etchedPx.R}");

                    // Dark stone background should be dark charcoal (< 50)
                    Assert.True(stonePx.R < 50, $"Expected stone R < 50, got {stonePx.R}");
                }
            }
        }
    }
}

