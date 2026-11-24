using ClipperLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace LaserGRBL.LightBurn
{
    public class EngravingGeometry
    {
        public bool IsValid { get; set; }
        public double MinX { get; set; }
        public double MaxX { get; set; }
        public double MinY { get; set; }
        public double MaxY { get; set; }
        public double Width => IsValid ? MaxX - MinX : 0.0;
        public double Height => IsValid ? MaxY - MinY : 0.0;
        public double CenterX => IsValid ? (MinX + MaxX) / 2.0 : 0.0;
        public double CenterY => IsValid ? (MinY + MaxY) / 2.0 : 0.0;
        public List<PointF> BurningPoints { get; set; } = new List<PointF>();
    }

    public class CutAroundBuildResult
    {
        public EngravingGeometry Engraving { get; set; } = new EngravingGeometry();
        public List<PointF> CutPolygon { get; set; } = new List<PointF>();
        public List<GrblCommand> CutCommands { get; set; } = new List<GrblCommand>();
        public double CutMinX { get; set; }
        public double CutMaxX { get; set; }
        public double CutMinY { get; set; }
        public double CutMaxY { get; set; }
        public double CutWidth => CutPolygon.Count > 0 ? CutMaxX - CutMinX : 0.0;
        public double CutHeight => CutPolygon.Count > 0 ? CutMaxY - CutMinY : 0.0;
        public bool IsValid => Engraving != null && Engraving.IsValid && CutPolygon != null && CutPolygon.Count >= 2;
    }

    public static class CutAroundGenerator
    {
        private const double ClipperResolution = 1000.0;

        public static EngravingGeometry ComputeEngravingGeometry(IReadOnlyList<GrblCommand> engravingCommands, bool hardwarePwm = true)
        {
            var geom = new EngravingGeometry();
            if (engravingCommands == null || engravingCommands.Count == 0)
                return geom;

            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;

            var spb = new GrblCommand.StatePositionBuilder();
            var dummyConf = new GrblConfST();

            for (int i = 0; i < engravingCommands.Count; i++)
            {
                GrblCommand cmd = engravingCommands[i];
                if (cmd == null) continue;

                bool deleteHelper = !cmd.JustBuilt;
                if (deleteHelper) cmd.BuildHelper();
                try
                {
                    spb.LastArcHelperResult = null;
                    spb.AnalyzeCommand(cmd, true, dummyConf);

                    if (cmd.IsMovement && spb.G2G3 && spb.LastArcHelperResult == null)
                    {
                        spb.GetArcHelper(cmd);
                    }

                    bool isBurning = spb.IsLaserBurning(hardwarePwm);

                    if (isBurning && cmd.IsMovement)
                    {
                        double sx = (double)spb.X.Previous;
                        double sy = (double)spb.Y.Previous;
                        double ex = (double)spb.X.Number;
                        double ey = (double)spb.Y.Number;

                        AddBurningPoint(geom.BurningPoints, sx, sy, ref minX, ref maxX, ref minY, ref maxY);

                        if (spb.G2G3 && spb.LastArcHelperResult != null)
                        {
                            var arc = spb.LastArcHelperResult;
                            if (arc.Ray > 0 && Math.Abs(arc.AngularWidth) > 1e-6)
                            {
                                const double maxSagitta = 0.005;
                                double maxAngle = arc.Ray > maxSagitta
                                    ? 2.0 * Math.Acos(Math.Max(-1.0, 1.0 - maxSagitta / arc.Ray))
                                    : Math.PI / 36.0;
                                double step = Math.Min(Math.PI / 36.0, Math.Max(1e-4, maxAngle));
                                int steps = Math.Max(8, Math.Min(4096, (int)Math.Ceiling(Math.Abs(arc.AngularWidth) / step)));
                                for (int s = 1; s < steps; s++)
                                {
                                    double angle = arc.StartAngle + arc.AngularWidth * (s / (double)steps);
                                    double ax = arc.CenterX + arc.Ray * Math.Cos(angle);
                                    double ay = arc.CenterY + arc.Ray * Math.Sin(angle);
                                    AddBurningPoint(geom.BurningPoints, ax, ay, ref minX, ref maxX, ref minY, ref maxY);
                                }
                            }

                            minX = Math.Min(minX, arc.BBox.X);
                            maxX = Math.Max(maxX, arc.BBox.X + arc.BBox.Width);
                            minY = Math.Min(minY, arc.BBox.Y);
                            maxY = Math.Max(maxY, arc.BBox.Y + arc.BBox.Height);
                        }

                        AddBurningPoint(geom.BurningPoints, ex, ey, ref minX, ref maxX, ref minY, ref maxY);
                    }
                }
                finally
                {
                    if (deleteHelper) cmd.DeleteHelper();
                }
            }

            if (geom.BurningPoints.Count > 0 && minX <= maxX && minY <= maxY)
            {
                geom.IsValid = true;
                geom.MinX = minX;
                geom.MaxX = maxX;
                geom.MinY = minY;
                geom.MaxY = maxY;
            }

            return geom;
        }

        private static void AddBurningPoint(List<PointF> points, double x, double y, ref double minX, ref double maxX, ref double minY, ref double maxY)
        {
            points.Add(new PointF((float)x, (float)y));
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        public static ProgramRange.XYRange ComputeEngravingBounds(IReadOnlyList<GrblCommand> engravingCommands, bool hardwarePwm = true)
        {
            var range = new ProgramRange.XYRange();
            var geom = ComputeEngravingGeometry(engravingCommands, hardwarePwm);
            if (geom.IsValid)
            {
                range.X.UpdateRange((decimal)geom.MinX);
                range.X.UpdateRange((decimal)geom.MaxX);
                range.Y.UpdateRange((decimal)geom.MinY);
                range.Y.UpdateRange((decimal)geom.MaxY);
            }
            return range;
        }

        public static List<PointF> ComputeCutPolygon(IReadOnlyList<GrblCommand> engravingCommands, CutAroundConfig config, CutMachineOptions machineOptions = null)
        {
            var res = BuildCutAround(engravingCommands, config, machineOptions);
            return res.CutPolygon;
        }

        public static List<GrblCommand> BuildCutCommands(IReadOnlyList<GrblCommand> engravingCommands, CutAroundConfig config, CutMachineOptions machineOptions = null)
        {
            var res = BuildCutAround(engravingCommands, config, machineOptions);
            return res.CutCommands;
        }

        public static List<GrblCommand> ComposeEngravingAndCut(IReadOnlyList<GrblCommand> engravingCommands, CutAroundConfig config, CutMachineOptions machineOptions = null)
        {
            var composed = new List<GrblCommand>();
            if (engravingCommands != null)
            {
                foreach (var cmd in engravingCommands)
                {
                    if (cmd != null)
                        composed.Add(new GrblCommand(cmd.Command));
                }
            }

            if (config != null && config.Enabled)
            {
                var cutCmds = BuildCutCommands(engravingCommands, config, machineOptions);
                composed.AddRange(cutCmds);
            }

            return composed;
        }

        public static CutAroundBuildResult BuildCutAround(IReadOnlyList<GrblCommand> engravingCommands, CutAroundConfig config, CutMachineOptions machineOptions = null)
        {
            if (engravingCommands == null || engravingCommands.Count == 0 || config == null)
                return new CutAroundBuildResult();

            var opts = machineOptions != null ? machineOptions.Clone() : new CutMachineOptions();
            opts.ValidateAndClamp();

            EngravingGeometry geom = ComputeEngravingGeometry(engravingCommands, opts.HardwarePwm);
            return BuildCutAround(geom, config, opts);
        }

        public static CutAroundBuildResult BuildCutAround(EngravingGeometry geom, CutAroundConfig config, CutMachineOptions machineOptions = null)
        {
            var result = new CutAroundBuildResult();
            if (geom == null || config == null)
                return result;

            var cfg = config.Clone();
            cfg.ValidateAndClamp();

            var opts = machineOptions != null ? machineOptions.Clone() : new CutMachineOptions();
            opts.ValidateAndClamp();

            result.Engraving = geom;
            if (!geom.IsValid)
                return result;

            // Compute shape geometry
            ShapeBuildData shapeData = BuildShapeData(geom, cfg);
            result.CutPolygon = shapeData.Polygon;

            if (result.CutPolygon.Count > 0)
            {
                double minX = double.MaxValue, maxX = double.MinValue;
                double minY = double.MaxValue, maxY = double.MinValue;
                foreach (var pt in result.CutPolygon)
                {
                    if (pt.X < minX) minX = pt.X;
                    if (pt.X > maxX) maxX = pt.X;
                    if (pt.Y < minY) minY = pt.Y;
                    if (pt.Y > maxY) maxY = pt.Y;
                }
                result.CutMinX = minX;
                result.CutMaxX = maxX;
                result.CutMinY = minY;
                result.CutMaxY = maxY;
            }

            result.CutCommands = GenerateCommandsForShape(shapeData, cfg, opts);
            return result;
        }

        private class ShapeBuildData
        {
            public CutAroundShape EffectiveShape { get; set; }
            public List<PointF> Polygon { get; set; } = new List<PointF>();
            public double X0 { get; set; }
            public double Y0 { get; set; }
            public double X1 { get; set; }
            public double Y1 { get; set; }
            public double CornerRadius { get; set; }
            public double CenterX { get; set; }
            public double CenterY { get; set; }
            public double Radius { get; set; }
        }

        private static ShapeBuildData BuildShapeData(EngravingGeometry geom, CutAroundConfig cfg)
        {
            double margin = cfg.MarginMm;

            switch (cfg.Shape)
            {
                case CutAroundShape.RoundedRectangle:
                    return BuildRoundedRectShape(geom, margin, cfg.CornerRadiusMm);

                case CutAroundShape.Circle:
                    return BuildCircleShape(geom, margin);

                case CutAroundShape.Ellipse:
                    return BuildEllipseShape(geom, margin);

                case CutAroundShape.Contour:
                    return BuildContourShape(geom, margin);

                case CutAroundShape.Rectangle:
                default:
                    return BuildRectangleShape(geom, margin);
            }
        }

        private static ShapeBuildData BuildRectangleShape(EngravingGeometry geom, double margin)
        {
            double x0 = geom.MinX - margin;
            double x1 = geom.MaxX + margin;
            double y0 = geom.MinY - margin;
            double y1 = geom.MaxY + margin;

            var poly = new List<PointF>
            {
                new PointF((float)x0, (float)y0),
                new PointF((float)x1, (float)y0),
                new PointF((float)x1, (float)y1),
                new PointF((float)x0, (float)y1),
                new PointF((float)x0, (float)y0)
            };

            return new ShapeBuildData
            {
                EffectiveShape = CutAroundShape.Rectangle,
                Polygon = poly,
                X0 = x0,
                Y0 = y0,
                X1 = x1,
                Y1 = y1
            };
        }

        private static ShapeBuildData BuildRoundedRectShape(EngravingGeometry geom, double margin, double cornerRadius)
        {
            double wBase = Math.Max(0.0, geom.MaxX - geom.MinX);
            double hBase = Math.Max(0.0, geom.MaxY - geom.MinY);
            double minBase = Math.Min(wBase, hBase);
            double sqrt2 = Math.Sqrt(2.0);

            double rMax = margin + minBase / sqrt2;
            double r = Math.Max(0.0, Math.Min(cornerRadius, rMax));

            if (r <= 1e-4)
                return BuildRectangleShape(geom, margin);

            // When r > margin, expand outer offset so corner arc center (x0+r, y0+r) keeps
            // Euclidean clearance >= margin from engraving corner (MinX, MinY), plus 0.002mm for F3 rounding.
            double offset = (r <= margin)
                ? margin
                : (margin / sqrt2 + r * (1.0 - 1.0 / sqrt2) + 0.002);

            double x0 = geom.MinX - offset;
            double x1 = geom.MaxX + offset;
            double y0 = geom.MinY - offset;
            double y1 = geom.MaxY + offset;

            double minDim = Math.Min(x1 - x0, y1 - y0);
            if (2.0 * r > minDim)
                r = minDim * 0.5;

            if (r <= 1e-4)
                return BuildRectangleShape(geom, margin);

            var poly = new List<PointF>();
            const int arcSegs = 16;

            // Start bottom-left straight start: (x0 + r, y0)
            poly.Add(new PointF((float)(x0 + r), (float)y0));
            if ((x1 - r) - (x0 + r) > 1e-5)
                poly.Add(new PointF((float)(x1 - r), (float)y0));

            // Bottom-right arc: center (x1 - r, y0 + r), -90 deg to 0 deg
            AddArcPoints(poly, x1 - r, y0 + r, r, -Math.PI / 2.0, 0.0, arcSegs);
            if ((y1 - r) - (y0 + r) > 1e-5)
                poly.Add(new PointF((float)x1, (float)(y1 - r)));

            // Top-right arc: center (x1 - r, y1 - r), 0 deg to 90 deg
            AddArcPoints(poly, x1 - r, y1 - r, r, 0.0, Math.PI / 2.0, arcSegs);
            if ((x1 - r) - (x0 + r) > 1e-5)
                poly.Add(new PointF((float)(x0 + r), (float)y1));

            // Top-left arc: center (x0 + r, y1 - r), 90 deg to 180 deg
            AddArcPoints(poly, x0 + r, y1 - r, r, Math.PI / 2.0, Math.PI, arcSegs);
            if ((y1 - r) - (y0 + r) > 1e-5)
                poly.Add(new PointF((float)x0, (float)(y0 + r)));

            // Bottom-left arc: center (x0 + r, y0 + r), 180 deg to 270 deg
            AddArcPoints(poly, x0 + r, y0 + r, r, Math.PI, 1.5 * Math.PI, arcSegs);

            if (poly.Count > 0)
                poly[poly.Count - 1] = poly[0];

            return new ShapeBuildData
            {
                EffectiveShape = CutAroundShape.RoundedRectangle,
                Polygon = poly,
                X0 = x0,
                Y0 = y0,
                X1 = x1,
                Y1 = y1,
                CornerRadius = r
            };
        }

        private static void AddArcPoints(List<PointF> poly, double cx, double cy, double r, double startAngle, double endAngle, int segments)
        {
            for (int i = 1; i <= segments; i++)
            {
                double a = startAngle + (endAngle - startAngle) * (i / (double)segments);
                poly.Add(new PointF((float)(cx + r * Math.Cos(a)), (float)(cy + r * Math.Sin(a))));
            }
        }

        private static ShapeBuildData BuildCircleShape(EngravingGeometry geom, double margin)
        {
            ComputeMinimumEnclosingCircle(geom.BurningPoints, out double cx, out double cy, out double mecRadius);
            double r = Math.Max(0.01, mecRadius + margin);

            var poly = new List<PointF>();
            const int segments = 72;
            for (int i = 0; i <= segments; i++)
            {
                // Start at leftmost point (cx - r, cy) to match G2 start
                double a = Math.PI - (2.0 * Math.PI * i / segments);
                poly.Add(new PointF((float)(cx + r * Math.Cos(a)), (float)(cy + r * Math.Sin(a))));
            }
            poly[poly.Count - 1] = poly[0];

            return new ShapeBuildData
            {
                EffectiveShape = CutAroundShape.Circle,
                Polygon = poly,
                CenterX = cx,
                CenterY = cy,
                Radius = r
            };
        }

        private static ShapeBuildData BuildEllipseShape(EngravingGeometry geom, double margin)
        {
            double cx = geom.CenterX;
            double cy = geom.CenterY;
            double hw = geom.Width / 2.0;
            double hh = geom.Height / 2.0;

            // Circumscribing ellipse around the margin-expanded bounding box:
            // rx = (hw + margin) * sqrt(2), ry = (hh + margin) * sqrt(2).
            // Since the expanded box [-hw-margin, hw+margin] x [-hh-margin, hh+margin]
            // is strictly inscribed inside this ellipse, every point of the engraving box
            // (including all 4 corners) is at least `margin` mm inside the ellipse.
            double sqrt2 = Math.Sqrt(2.0);
            double rx = Math.Max(0.01, (hw + margin) * sqrt2);
            double ry = Math.Max(0.01, (hh + margin) * sqrt2);

            double approxPerimeter = Math.PI * (3.0 * (rx + ry) - Math.Sqrt((3.0 * rx + ry) * (rx + 3.0 * ry)));
            int segments = Math.Max(64, Math.Min(240, (int)Math.Ceiling(approxPerimeter / 0.5)));

            var poly = new List<PointF>(segments + 1);
            for (int i = 0; i <= segments; i++)
            {
                double a = 2.0 * Math.PI * i / segments;
                poly.Add(new PointF((float)(cx + rx * Math.Cos(a)), (float)(cy + ry * Math.Sin(a))));
            }
            poly[poly.Count - 1] = poly[0];

            return new ShapeBuildData
            {
                EffectiveShape = CutAroundShape.Ellipse,
                Polygon = poly,
                CenterX = cx,
                CenterY = cy
            };
        }

        private static ShapeBuildData BuildContourShape(EngravingGeometry geom, double margin)
        {
            if (geom.BurningPoints == null || geom.BurningPoints.Count < 3)
                return BuildRectangleShape(geom, margin);

            List<PointF> hull = FramingHelper.ComputeConvexHull(geom.BurningPoints);
            if (hull == null || hull.Count < 3)
                return BuildRectangleShape(geom, margin);

            // Strip duplicate closing vertex if present
            var openHull = new List<PointF>(hull);
            if (openHull.Count > 1 && Distance(openHull[0], openHull[openHull.Count - 1]) < 1e-4)
                openHull.RemoveAt(openHull.Count - 1);

            if (openHull.Count < 3 || Math.Abs(SignedPolygonArea(openHull)) <= 1e-4)
                return BuildRectangleShape(geom, margin);

            List<PointF> contourPoly = null;

            if (margin > 1e-6)
            {
                var path = new List<IntPoint>(openHull.Count);
                foreach (var pt in openHull)
                {
                    path.Add(new IntPoint((long)Math.Round(pt.X * ClipperResolution), (long)Math.Round(pt.Y * ClipperResolution)));
                }

                var offset = new ClipperOffset(2.0, 0.005 * ClipperResolution);
                offset.AddPath(path, JoinType.jtRound, EndType.etClosedPolygon);

                var solution = new List<List<IntPoint>>();
                offset.Execute(ref solution, margin * ClipperResolution);

                if (solution != null && solution.Count > 0)
                {
                    // Choose largest polygon
                    List<IntPoint> best = solution[0];
                    double bestArea = Math.Abs(Clipper.Area(best));
                    for (int i = 1; i < solution.Count; i++)
                    {
                        double a = Math.Abs(Clipper.Area(solution[i]));
                        if (a > bestArea)
                        {
                            bestArea = a;
                            best = solution[i];
                        }
                    }

                    if (best.Count >= 3)
                    {
                        contourPoly = new List<PointF>(best.Count + 1);
                        foreach (var ip in best)
                        {
                            contourPoly.Add(new PointF((float)(ip.X / ClipperResolution), (float)(ip.Y / ClipperResolution)));
                        }
                        contourPoly.Add(contourPoly[0]);
                    }
                }
            }
            else
            {
                contourPoly = new List<PointF>(openHull.Count + 1);
                contourPoly.AddRange(openHull);
                contourPoly.Add(openHull[0]);
            }

            if (contourPoly == null || contourPoly.Count < 4)
                return BuildRectangleShape(geom, margin);

            return new ShapeBuildData
            {
                EffectiveShape = CutAroundShape.Contour,
                Polygon = contourPoly
            };
        }

        private static List<GrblCommand> GenerateCommandsForShape(ShapeBuildData shape, CutAroundConfig cfg, CutMachineOptions opts)
        {
            var cmds = new List<GrblCommand>();
            if (shape == null || shape.Polygon == null || shape.Polygon.Count < 2)
                return cmds;

            string skipCmd = opts.DisableG0FastSkip ? "G1" : "G0";
            string skipFeed = opts.DisableG0FastSkip
                ? string.Format(CultureInfo.InvariantCulture, " F{0}", cfg.SpeedMmMin)
                : string.Empty;
            string laserMode = cfg.GetNormalizedLaserMode();
            string laserOnCmd = FormatLaserOnCommand(laserMode, cfg, opts);
            string laserOffCmd = (opts.HardwarePwm || opts.IsSmoothie) ? "M5 S0" : "M5";

            // Ensure absolute coordinates and laser safely off after engraving
            cmds.Add(new GrblCommand("G90"));
            cmds.Add(new GrblCommand(laserOffCmd));

            if (cfg.AirAssist)
                cmds.Add(new GrblCommand("M8"));

            for (int pass = 1; pass <= cfg.Passes; pass++)
            {
                if (pass > 1 && cfg.ZStepMm > 0.0)
                {
                    double zOffset = -(cfg.ZStepMm * (pass - 1));
                    cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "{0} Z{1:0.000}{2}", skipCmd, zOffset, skipFeed)));
                }

                if (shape.EffectiveShape == CutAroundShape.Circle)
                {
                    double startX = shape.CenterX - shape.Radius;
                    double startY = shape.CenterY;

                    cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "{0} X{1:0.000} Y{2:0.000}{3}", skipCmd, startX, startY, skipFeed)));
                    cmds.Add(new GrblCommand(laserOnCmd));
                    cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "G2 X{0:0.000} Y{1:0.000} I{2:0.000} J0 F{3}", startX, startY, shape.Radius, cfg.SpeedMmMin)));
                    cmds.Add(new GrblCommand(laserOffCmd));
                }
                else if (shape.EffectiveShape == CutAroundShape.RoundedRectangle)
                {
                    double gx0 = Math.Round(shape.X0, 3);
                    double gy0 = Math.Round(shape.Y0, 3);
                    double gx1 = Math.Round(shape.X1, 3);
                    double gy1 = Math.Round(shape.Y1, 3);
                    double gr = Math.Round(shape.CornerRadius, 3);

                    double maxRoundedR = Math.Floor((Math.Min(gx1 - gx0, gy1 - gy0) * 0.5) * 1000.0) / 1000.0;
                    if (gr > maxRoundedR)
                        gr = maxRoundedR;

                    if (gr <= 1e-4)
                    {
                        cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "{0} X{1:0.000} Y{2:0.000}{3}", skipCmd, gx0, gy0, skipFeed)));
                        cmds.Add(new GrblCommand(laserOnCmd));
                        cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000} F{2}", gx1, gy0, cfg.SpeedMmMin)));
                        cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", gx1, gy1)));
                        cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", gx0, gy1)));
                        cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", gx0, gy0)));
                        cmds.Add(new GrblCommand(laserOffCmd));
                    }
                    else
                    {
                        cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "{0} X{1:0.000} Y{2:0.000}{3}", skipCmd, gx0 + gr, gy0, skipFeed)));
                        cmds.Add(new GrblCommand(laserOnCmd));

                        string cutFeed = string.Format(CultureInfo.InvariantCulture, " F{0}", cfg.SpeedMmMin);
                        bool feedEmitted = false;

                        if (Math.Abs((gx1 - gr) - (gx0 + gr)) >= 1e-4)
                        {
                            cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}{2}", gx1 - gr, gy0, feedEmitted ? string.Empty : cutFeed)));
                            feedEmitted = true;
                        }
                        cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "G3 X{0:0.000} Y{1:0.000} I0 J{2:0.000}{3}", gx1, gy0 + gr, gr, feedEmitted ? string.Empty : cutFeed)));
                        feedEmitted = true;

                        if (Math.Abs((gy1 - gr) - (gy0 + gr)) >= 1e-4)
                            cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", gx1, gy1 - gr)));
                        cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "G3 X{0:0.000} Y{1:0.000} I{2:0.000} J0", gx1 - gr, gy1, -gr)));

                        if (Math.Abs((gx1 - gr) - (gx0 + gr)) >= 1e-4)
                            cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", gx0 + gr, gy1)));
                        cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "G3 X{0:0.000} Y{1:0.000} I0 J{2:0.000}", gx0, gy1 - gr, -gr)));

                        if (Math.Abs((gy1 - gr) - (gy0 + gr)) >= 1e-4)
                            cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", gx0, gy0 + gr)));
                        cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "G3 X{0:0.000} Y{1:0.000} I{2:0.000} J0", gx0 + gr, gy0, gr)));

                        cmds.Add(new GrblCommand(laserOffCmd));
                    }
                }
                else
                {
                    PointF start = shape.Polygon[0];
                    cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "{0} X{1:0.000} Y{2:0.000}{3}", skipCmd, start.X, start.Y, skipFeed)));
                    cmds.Add(new GrblCommand(laserOnCmd));

                    for (int i = 1; i < shape.Polygon.Count; i++)
                    {
                        PointF pt = shape.Polygon[i];
                        if (i == 1)
                            cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000} F{2}", pt.X, pt.Y, cfg.SpeedMmMin)));
                        else
                            cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "G1 X{0:0.000} Y{1:0.000}", pt.X, pt.Y)));
                    }

                    cmds.Add(new GrblCommand(laserOffCmd));
                }
            }

            if (cfg.ZStepMm > 0.0 && cfg.Passes > 1)
                cmds.Add(new GrblCommand(string.Format(CultureInfo.InvariantCulture, "{0} Z0{1}", skipCmd, skipFeed)));

            if (cfg.AirAssist)
                cmds.Add(new GrblCommand("M9"));

            if (cmds[cmds.Count - 1].Command != laserOffCmd)
                cmds.Add(new GrblCommand(laserOffCmd));

            return cmds;
        }

        public static string FormatLaserOnCommand(string laserMode, CutAroundConfig cfg, CutMachineOptions opts)
        {
            if (opts.IsSmoothie)
            {
                double norm = cfg.PowerPercent / 100.0;
                return string.Format(CultureInfo.InvariantCulture, "{0} S{1:0.00}", laserMode, norm);
            }

            if (opts.HardwarePwm)
            {
                int sVal = CalculateSValue(cfg.PowerPercent, opts.MaxPWM);
                return string.Format(CultureInfo.InvariantCulture, "{0} S{1}", laserMode, sVal);
            }

            return laserMode;
        }

        public static int CalculateSValue(int powerPercent, int maxPwm)
        {
            int clampedPct = Math.Max(1, Math.Min(100, powerPercent));
            int clampedPwm = Math.Max(1, maxPwm);
            return (int)Math.Round(clampedPct / 100.0 * clampedPwm);
        }

        private static double Distance(PointF a, PointF b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static double SignedPolygonArea(IList<PointF> poly)
        {
            double area = 0.0;
            for (int i = 0; i < poly.Count; i++)
            {
                PointF p1 = poly[i];
                PointF p2 = poly[(i + 1) % poly.Count];
                area += ((double)p1.X * p2.Y - (double)p2.X * p1.Y);
            }
            return area * 0.5;
        }

        /// <summary>
        /// Exact iterative O(N) expected-time Minimum Enclosing Circle (Welzl's incremental algorithm).
        /// </summary>
        private static void ComputeMinimumEnclosingCircle(IReadOnlyList<PointF> points, out double cx, out double cy, out double radius)
        {
            cx = 0;
            cy = 0;
            radius = 0;
            if (points == null || points.Count == 0)
                return;

            // Filter duplicates to keep point set compact
            var pts = points
                .GroupBy(p => new { X = Math.Round(p.X, 4), Y = Math.Round(p.Y, 4) })
                .Select(g => new PointD(g.First().X, g.First().Y))
                .ToList();

            // Deterministic shuffle using fixed seed for O(N) performance
            var rng = new Random(42);
            for (int i = pts.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var tmp = pts[i];
                pts[i] = pts[j];
                pts[j] = tmp;
            }

            PointD center = pts[0];
            double r = 0.0;
            const double eps = 1e-7;

            for (int i = 1; i < pts.Count; i++)
            {
                if (Dist(center, pts[i]) > r + eps)
                {
                    center = pts[i];
                    r = 0.0;
                    for (int j = 0; j < i; j++)
                    {
                        if (Dist(center, pts[j]) > r + eps)
                        {
                            center = new PointD((pts[i].X + pts[j].X) * 0.5, (pts[i].Y + pts[j].Y) * 0.5);
                            r = Dist(center, pts[i]);
                            for (int k = 0; k < j; k++)
                            {
                                if (Dist(center, pts[k]) > r + eps)
                                {
                                    CircleFrom3Points(pts[i], pts[j], pts[k], out center, out r);
                                }
                            }
                        }
                    }
                }
            }

            // Safety pass over all original points to guarantee enclosure even with float rounding
            for (int i = 0; i < points.Count; i++)
            {
                double d = Math.Sqrt(Math.Pow(points[i].X - center.X, 2) + Math.Pow(points[i].Y - center.Y, 2));
                if (d > r)
                    r = d;
            }

            cx = center.X;
            cy = center.Y;
            radius = r;
        }

        private struct PointD
        {
            public double X;
            public double Y;
            public PointD(double x, double y) { X = x; Y = y; }
        }

        private static double Dist(PointD a, PointD b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static void CircleFrom3Points(PointD a, PointD b, PointD c, out PointD center, out double radius)
        {
            double bx = b.X - a.X;
            double by = b.Y - a.Y;
            double cx = c.X - a.X;
            double cy = c.Y - a.Y;

            double d = 2.0 * (bx * cy - by * cx);
            if (Math.Abs(d) < 1e-12)
            {
                // Collinear fallback: circle from the farthest pair
                double dab = Dist(a, b);
                double dac = Dist(a, c);
                double dbc = Dist(b, c);
                if (dab >= dac && dab >= dbc)
                {
                    center = new PointD((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5);
                    radius = dab * 0.5;
                }
                else if (dac >= dbc)
                {
                    center = new PointD((a.X + c.X) * 0.5, (a.Y + c.Y) * 0.5);
                    radius = dac * 0.5;
                }
                else
                {
                    center = new PointD((b.X + c.X) * 0.5, (b.Y + c.Y) * 0.5);
                    radius = dbc * 0.5;
                }
                return;
            }

            double b2 = bx * bx + by * by;
            double c2 = cx * cx + cy * cy;
            double ux = (cy * b2 - by * c2) / d;
            double uy = (bx * c2 - cx * b2) / d;
            center = new PointD(a.X + ux, a.Y + uy);
            radius = Math.Sqrt(ux * ux + uy * uy);
        }
    }
}
