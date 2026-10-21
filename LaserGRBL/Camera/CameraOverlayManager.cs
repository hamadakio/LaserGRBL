using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace LaserGRBL.Camera
{
    public static class CameraOverlayManager
    {
        private static readonly object mLock = new object();
        private static Bitmap mRawFrame;
        private static Bitmap mWarpedFrame;

        public static event Action OnFrameUpdated;

        public static bool Enabled { get; set; } = true;
        public static float Opacity { get; set; } = 0.65f;
        public static float BedWidthMm { get; set; } = 400.0f;
        public static float BedHeightMm { get; set; } = 400.0f;

        public static float OffsetX_Mm { get; set; } = 0.0f;
        public static float OffsetY_Mm { get; set; } = 0.0f;
        public static float ScaleX { get; set; } = 1.0f;
        public static float ScaleY { get; set; } = 1.0f;
        public static int CalibMode { get; set; } = 0; // 0 = Bed Corners (0..BedW, 0..BedH), 1 = Burned Targets with Margin
        public static float TargetMarginMm { get; set; } = 30.0f;
        public static int RotationAngle { get; set; } = 0; // 0, 90, 180, 270

        // 4 Calibration corners in normalized camera coordinates [0..1]:
        // [0] = Top-Left (0, BedHeight)
        // [1] = Top-Right (BedWidth, BedHeight)
        // [2] = Bottom-Right (BedWidth, 0)
        // [3] = Bottom-Left (0, 0)
        public static PointF[] CalibrationPoints { get; set; } = new PointF[]
        {
            new PointF(0.05f, 0.05f),
            new PointF(0.95f, 0.05f),
            new PointF(0.95f, 0.95f),
            new PointF(0.05f, 0.95f)
        };

        public static bool HasCalibratedPoints { get; set; } = false;

        public static Bitmap RawFrame
        {
            get
            {
                lock (mLock)
                {
                    if (mRawFrame == null) return null;
                    try
                    {
                        Bitmap copy = new Bitmap(mRawFrame.Width, mRawFrame.Height, PixelFormat.Format32bppArgb);
                        using (Graphics g = Graphics.FromImage(copy))
                        {
                            g.DrawImage(mRawFrame, 0, 0, mRawFrame.Width, mRawFrame.Height);
                        }
                        return copy;
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
        }

        public static Bitmap WarpedFrame
        {
            get
            {
                lock (mLock)
                {
                    if (mWarpedFrame == null) return null;
                    try
                    {
                        Bitmap copy = new Bitmap(mWarpedFrame.Width, mWarpedFrame.Height, PixelFormat.Format32bppArgb);
                        using (Graphics g = Graphics.FromImage(copy))
                        {
                            g.DrawImage(mWarpedFrame, 0, 0, mWarpedFrame.Width, mWarpedFrame.Height);
                        }
                        return copy;
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
        }

        static CameraOverlayManager()
        {
            LoadSettings();
        }

        public static void LoadSettings()
        {
            try
            {
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                Enabled = Settings.GetObject("Camera_Enabled", true);
                Opacity = (float)Settings.GetObject("Camera_Opacity", 0.65);
                BedWidthMm = (float)Settings.GetObject("Camera_BedWidth", 300.0);
                BedHeightMm = (float)Settings.GetObject("Camera_BedHeight", 300.0);
                OffsetX_Mm = (float)Settings.GetObject("Camera_OffsetX", 0.0);
                OffsetY_Mm = (float)Settings.GetObject("Camera_OffsetY", 0.0);
                ScaleX = (float)Settings.GetObject("Camera_ScaleX", 1.0);
                ScaleY = (float)Settings.GetObject("Camera_ScaleY", 1.0);
                CalibMode = Settings.GetObject("Camera_CalibMode", 0);
                TargetMarginMm = (float)Settings.GetObject("Camera_TargetMargin", 30.0);
                RotationAngle = Settings.GetObject("Camera_RotationAngle", 0);

                string ptsStr = Settings.GetObject("Camera_CalibPoints", "");
                if (!string.IsNullOrEmpty(ptsStr))
                {
                    string[] parts = ptsStr.Split(';');
                    if (parts.Length == 8)
                    {
                        CalibrationPoints = new PointF[]
                        {
                            new PointF(float.Parse(parts[0], ci), float.Parse(parts[1], ci)),
                            new PointF(float.Parse(parts[2], ci), float.Parse(parts[3], ci)),
                            new PointF(float.Parse(parts[4], ci), float.Parse(parts[5], ci)),
                            new PointF(float.Parse(parts[6], ci), float.Parse(parts[7], ci))
                        };
                        HasCalibratedPoints = true;
                    }
                }
            }
            catch { }
        }

        public static void SaveSettings()
        {
            try
            {
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                Settings.SetObject("Camera_Enabled", Enabled);
                Settings.SetObject("Camera_Opacity", (double)Opacity);
                Settings.SetObject("Camera_BedWidth", (double)BedWidthMm);
                Settings.SetObject("Camera_BedHeight", (double)BedHeightMm);
                Settings.SetObject("Camera_OffsetX", (double)OffsetX_Mm);
                Settings.SetObject("Camera_OffsetY", (double)OffsetY_Mm);
                Settings.SetObject("Camera_ScaleX", (double)ScaleX);
                Settings.SetObject("Camera_ScaleY", (double)ScaleY);
                Settings.SetObject("Camera_CalibMode", CalibMode);
                Settings.SetObject("Camera_TargetMargin", (double)TargetMarginMm);
                Settings.SetObject("Camera_RotationAngle", RotationAngle);

                string ptsStr = $"{CalibrationPoints[0].X.ToString(ci)};{CalibrationPoints[0].Y.ToString(ci)};" +
                               $"{CalibrationPoints[1].X.ToString(ci)};{CalibrationPoints[1].Y.ToString(ci)};" +
                               $"{CalibrationPoints[2].X.ToString(ci)};{CalibrationPoints[2].Y.ToString(ci)};" +
                               $"{CalibrationPoints[3].X.ToString(ci)};{CalibrationPoints[3].Y.ToString(ci)}";
                Settings.SetObject("Camera_CalibPoints", ptsStr);
            }
            catch { }
        }

        public static void RotatePhoto(int angleDelta = 90)
        {
            RotationAngle = ((RotationAngle + angleDelta) % 360 + 360) % 360;
            SaveSettings();

            lock (mLock)
            {
                if (mRawFrame != null)
                {
                    RotateFlipType rft = RotateFlipType.Rotate90FlipNone;
                    int normDelta = (angleDelta % 360 + 360) % 360;
                    if (normDelta == 90) rft = RotateFlipType.Rotate90FlipNone;
                    else if (normDelta == 180) rft = RotateFlipType.Rotate180FlipNone;
                    else if (normDelta == 270) rft = RotateFlipType.Rotate270FlipNone;

                    mRawFrame.RotateFlip(rft);

                    // Rotate calibration points
                    if (normDelta == 90)
                    {
                        for (int i = 0; i < CalibrationPoints.Length; i++)
                        {
                            float oldU = CalibrationPoints[i].X;
                            float oldV = CalibrationPoints[i].Y;
                            CalibrationPoints[i] = new PointF(1f - oldV, oldU);
                        }
                    }
                    else if (normDelta == 270)
                    {
                        for (int i = 0; i < CalibrationPoints.Length; i++)
                        {
                            float oldU = CalibrationPoints[i].X;
                            float oldV = CalibrationPoints[i].Y;
                            CalibrationPoints[i] = new PointF(oldV, 1f - oldU);
                        }
                    }
                    else if (normDelta == 180)
                    {
                        for (int i = 0; i < CalibrationPoints.Length; i++)
                        {
                            CalibrationPoints[i] = new PointF(1f - CalibrationPoints[i].X, 1f - CalibrationPoints[i].Y);
                        }
                    }

                    mWarpedFrame?.Dispose();
                    mWarpedFrame = WarpPerspective(mRawFrame, CalibrationPoints, (int)BedWidthMm * 2, (int)BedHeightMm * 2);
                }
            }

            OnFrameUpdated?.Invoke();
        }

        public static void UpdateFrame(Bitmap newFrame)
        {
            if (newFrame == null) return;

            lock (mLock)
            {
                try
                {
                    mRawFrame?.Dispose();
                    // Create an unmanaged standalone 32bppArgb bitmap to avoid GDI+ stream dependencies
                    mRawFrame = new Bitmap(newFrame.Width, newFrame.Height, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(mRawFrame))
                    {
                        g.DrawImage(newFrame, 0, 0, newFrame.Width, newFrame.Height);
                    }

                    if (RotationAngle == 90)
                        mRawFrame.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    else if (RotationAngle == 180)
                        mRawFrame.RotateFlip(RotateFlipType.Rotate180FlipNone);
                    else if (RotationAngle == 270)
                        mRawFrame.RotateFlip(RotateFlipType.Rotate270FlipNone);

                    // Compute perspective warped image
                    mWarpedFrame?.Dispose();
                    mWarpedFrame = WarpPerspective(mRawFrame, CalibrationPoints, (int)BedWidthMm * 2, (int)BedHeightMm * 2);
                }
                catch (Exception ex)
                {
                    LaserCameraServer.Log($"⚠️ Erro em UpdateFrame: {ex.Message}");
                }
            }

            OnFrameUpdated?.Invoke();
        }

        /// <summary>
        /// Warps the quadrilateral defined by 4 normalized points into a rectified rectangular image.
        /// </summary>
        public static Bitmap WarpPerspective(Bitmap src, PointF[] corners, int dstWidth, int dstHeight)
        {
            if (src == null || corners == null || corners.Length != 4)
                return null;

            dstWidth = Math.Max(200, Math.Min(1600, dstWidth));
            dstHeight = Math.Max(200, Math.Min(1600, dstHeight));

            Bitmap dst = new Bitmap(dstWidth, dstHeight, PixelFormat.Format32bppArgb);

            Bitmap safeSrc = null;
            BitmapData srcData = null;
            BitmapData dstData = null;

            try
            {
                // Always create an independent Format32bppArgb clone so caller's src is NEVER locked
                safeSrc = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(safeSrc))
                {
                    g.DrawImage(src, 0, 0, src.Width, src.Height);
                }

                // Source pixel coordinates
                PointF p0 = new PointF(corners[0].X * safeSrc.Width, corners[0].Y * safeSrc.Height);
                PointF p1 = new PointF(corners[1].X * safeSrc.Width, corners[1].Y * safeSrc.Height);
                PointF p2 = new PointF(corners[2].X * safeSrc.Width, corners[2].Y * safeSrc.Height);
                PointF p3 = new PointF(corners[3].X * safeSrc.Width, corners[3].Y * safeSrc.Height);

                // Compute 3x3 Homography Matrix (mapping destination (x, y) -> source (u, v))
                float uMin = 0f;
                float uMax = dstWidth;
                float vMin = 0f;
                float vMax = dstHeight;

                if (CalibMode == 1 && TargetMarginMm > 0 && BedWidthMm > TargetMarginMm * 2 && BedHeightMm > TargetMarginMm * 2)
                {
                    uMin = (TargetMarginMm / BedWidthMm) * dstWidth;
                    uMax = ((BedWidthMm - TargetMarginMm) / BedWidthMm) * dstWidth;
                    vMin = (TargetMarginMm / BedHeightMm) * dstHeight;
                    vMax = ((BedHeightMm - TargetMarginMm) / BedHeightMm) * dstHeight;
                }

                PointF[] dstPoints = new PointF[]
                {
                    new PointF(uMin, vMin),
                    new PointF(uMax, vMin),
                    new PointF(uMax, vMax),
                    new PointF(uMin, vMax)
                };

                double[] H = ComputeHomography(dstPoints, new PointF[] { p0, p1, p2, p3 });
                if (H == null)
                {
                    using (Graphics g = Graphics.FromImage(dst))
                    {
                        g.DrawImage(safeSrc, 0, 0, dstWidth, dstHeight);
                    }
                    return dst;
                }

                // Fast pixel manipulation with LockBits
                srcData = safeSrc.LockBits(new Rectangle(0, 0, safeSrc.Width, safeSrc.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                dstData = dst.LockBits(new Rectangle(0, 0, dstWidth, dstHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                int srcStride = srcData.Stride;
                int dstStride = dstData.Stride;
                int srcW = safeSrc.Width;
                int srcH = safeSrc.Height;

                unsafe
                {
                    byte* srcPtr = (byte*)srcData.Scan0;
                    byte* dstPtr = (byte*)dstData.Scan0;

                    Parallel.For(0, dstHeight, y =>
                    {
                        byte* dstRow = dstPtr + (y * dstStride);

                        for (int x = 0; x < dstWidth; x++)
                        {
                            // Homography project: (u, v) = H * (x, y, 1)
                            double d = H[6] * x + H[7] * y + H[8];
                            if (Math.Abs(d) < 1e-9) d = 1e-9;

                            int u = (int)((H[0] * x + H[1] * y + H[2]) / d);
                            int v = (int)((H[3] * x + H[4] * y + H[5]) / d);

                            int dstIdx = x * 4;

                            if (u >= 0 && u < srcW && v >= 0 && v < srcH)
                            {
                                byte* srcPixel = srcPtr + (v * srcStride) + (u * 4);
                                dstRow[dstIdx + 0] = srcPixel[0]; // B
                                dstRow[dstIdx + 1] = srcPixel[1]; // G
                                dstRow[dstIdx + 2] = srcPixel[2]; // R
                                dstRow[dstIdx + 3] = 255;          // A
                            }
                            else
                            {
                                dstRow[dstIdx + 0] = 0;
                                dstRow[dstIdx + 1] = 0;
                                dstRow[dstIdx + 2] = 0;
                                dstRow[dstIdx + 3] = 0;
                            }
                        }
                    });
                }

                return dst;
            }
            catch (Exception ex)
            {
                LaserCameraServer.Log($"⚠️ Erro em WarpPerspective: {ex.Message}");
                try
                {
                    using (Graphics g = Graphics.FromImage(dst))
                    {
                        g.DrawImage(src, 0, 0, dstWidth, dstHeight);
                    }
                }
                catch { }
                return dst;
            }
            finally
            {
                if (srcData != null && safeSrc != null)
                {
                    try { safeSrc.UnlockBits(srcData); } catch { }
                }
                if (dstData != null && dst != null)
                {
                    try { dst.UnlockBits(dstData); } catch { }
                }
                if (safeSrc != null)
                {
                    try { safeSrc.Dispose(); } catch { }
                }
            }
        }

        /// <summary>
        /// Solves 3x3 Homography matrix mapping 4 points from src to dst.
        /// </summary>
        public static double[] ComputeHomography(PointF[] src, PointF[] dst)
        {
            if (src.Length < 4 || dst.Length < 4) return null;

            double[,] A = new double[8, 8];
            double[] B = new double[8];

            for (int i = 0; i < 4; i++)
            {
                double sx = src[i].X;
                double sy = src[i].Y;
                double dx = dst[i].X;
                double dy = dst[i].Y;

                A[i * 2, 0] = sx;
                A[i * 2, 1] = sy;
                A[i * 2, 2] = 1.0;
                A[i * 2, 3] = 0.0;
                A[i * 2, 4] = 0.0;
                A[i * 2, 5] = 0.0;
                A[i * 2, 6] = -sx * dx;
                A[i * 2, 7] = -sy * dx;
                B[i * 2] = dx;

                A[i * 2 + 1, 0] = 0.0;
                A[i * 2 + 1, 1] = 0.0;
                A[i * 2 + 1, 2] = 0.0;
                A[i * 2 + 1, 3] = sx;
                A[i * 2 + 1, 4] = sy;
                A[i * 2 + 1, 5] = 1.0;
                A[i * 2 + 1, 6] = -sx * dy;
                A[i * 2 + 1, 7] = -sy * dy;
                B[i * 2 + 1] = dy;
            }

            double[] h8 = GaussianElimination(A, B);
            if (h8 == null) return null;

            return new double[]
            {
                h8[0], h8[1], h8[2],
                h8[3], h8[4], h8[5],
                h8[6], h8[7], 1.0
            };
        }

        private static double[] GaussianElimination(double[,] A, double[] B)
        {
            int n = B.Length;
            for (int i = 0; i < n; i++)
            {
                int maxRow = i;
                double maxVal = Math.Abs(A[i, i]);
                for (int k = i + 1; k < n; k++)
                {
                    if (Math.Abs(A[k, i]) > maxVal)
                    {
                        maxVal = Math.Abs(A[k, i]);
                        maxRow = k;
                    }
                }

                if (maxVal < 1e-12) return null;

                for (int k = i; k < n; k++)
                {
                    double tmp = A[maxRow, k];
                    A[maxRow, k] = A[i, k];
                    A[i, k] = tmp;
                }
                double tmpB = B[maxRow];
                B[maxRow] = B[i];
                B[i] = tmpB;

                for (int k = i + 1; k < n; k++)
                {
                    double factor = A[k, i] / A[i, i];
                    B[k] -= factor * B[i];
                    for (int j = i; j < n; j++)
                    {
                        A[k, j] -= factor * A[i, j];
                    }
                }
            }

            double[] x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = 0.0;
                for (int j = i + 1; j < n; j++) sum += A[i, j] * x[j];
                x[i] = (B[i] - sum) / A[i, i];
            }
            return x;
        }
    }
}
