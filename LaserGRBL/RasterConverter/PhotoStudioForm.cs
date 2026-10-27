using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using LaserGRBL.Icons;
using LaserGRBL.UserControls;

namespace LaserGRBL.RasterConverter
{
    public partial class PhotoStudioForm : Form
    {
        private Bitmap mSourceOriginal;       // Full-res original bitmap
        private Bitmap mDisplaySource;        // Resized working copy for fast preview
        private Bitmap mEnhancedDisplay;
        private Bitmap mDitheredDisplay;
        private Bitmap mWoodSimDisplay;
        private Bitmap mSlateSimDisplay;

        public Bitmap EnhancedFullResult { get; private set; }
        public PhotoEnhanceParams CurrentParams { get; private set; }
        public decimal SelectedQuality { get; private set; } = 10.0m;

        private int mViewMode = 0; // 0 = Wood Burn, 1 = Dither Laser Dots, 2 = Slate, 3 = Side-by-Side
        private float mZoom = 1.0f;
        private PointF mPan = new PointF(0, 0);
        private bool mIsDragging = false;
        private Point mLastMousePos;
        private Timer mDebounceTimer;
        private bool mIsUpdatingUI = false;

        public PhotoStudioForm(Bitmap sourceOriginal, decimal currentQuality = 10.0m)
        {
            InitializeComponent();
            mSourceOriginal = sourceOriginal != null ? (Bitmap)sourceOriginal.Clone() : new Bitmap(400, 400);
            SelectedQuality = currentQuality > 0 ? currentQuality : 10.0m;

            // Generate a sensible preview size (max 800px on longest side for super-fast interactive UI)
            int maxDim = 800;
            int w = mSourceOriginal.Width;
            int h = mSourceOriginal.Height;
            if (w > maxDim || h > maxDim)
            {
                float scale = Math.Min((float)maxDim / w, (float)maxDim / h);
                mDisplaySource = ImageTransform.ResizeImage(mSourceOriginal, new Size((int)(w * scale), (int)(h * scale)), false, InterpolationMode.HighQualityBicubic);
            }
            else
            {
                mDisplaySource = (Bitmap)mSourceOriginal.Clone();
            }

            CurrentParams = PhotoEnhanceParams.CreatePortraitPreset();

            mDebounceTimer = new Timer();
            mDebounceTimer.Interval = 120; // 120ms debounce for smooth dragging
            mDebounceTimer.Tick += (s, e) =>
            {
                mDebounceTimer.Stop();
                RecalculatePipeline();
            };

            SetupInitialUI();
            RecalculatePipeline();
            CenterAndFitImage();
        }

        private void SetupInitialUI()
        {
            BackColor = ColorScheme.FormBackColor;
            ForeColor = ColorScheme.FormForeColor;

            CbDitherAlgo.Items.Clear();
            foreach (ImageTransform.DitheringMode mode in Enum.GetValues(typeof(ImageTransform.DitheringMode)))
            {
                CbDitherAlgo.Items.Add(mode);
            }

            SyncControlsFromParams();
            UpdatePresetSelection(0); // Portrait preset default
        }

        private void SyncControlsFromParams()
        {
            mIsUpdatingUI = true;
            try
            {
                TbGamma.Value = Math.Max(50, Math.Min(250, (int)Math.Round(CurrentParams.Gamma * 100f)));
                LblGammaVal.Text = $"{CurrentParams.Gamma:0.00}x";

                TbSharpness.Value = Math.Max(0, Math.Min(100, (int)Math.Round(CurrentParams.Sharpness * 100f)));
                LblSharpVal.Text = $"{TbSharpness.Value}%";

                TbLocalContrast.Value = Math.Max(0, Math.Min(100, (int)Math.Round(CurrentParams.LocalContrast * 100f)));
                LblLocalVal.Text = $"{TbLocalContrast.Value}%";

                TbBrightness.Value = Math.Max(-100, Math.Min(100, CurrentParams.Brightness));
                LblBrightVal.Text = $"{TbBrightness.Value}";

                TbContrast.Value = Math.Max(-100, Math.Min(100, CurrentParams.Contrast));
                LblContrastVal.Text = $"{TbContrast.Value}";

                TbBgClean.Value = Math.Max(0, Math.Min(60, CurrentParams.BackgroundClean));
                LblBgCleanVal.Text = $"{TbBgClean.Value}";

                CbDitherAlgo.SelectedItem = CurrentParams.Dithering;
                ChkInvert.Checked = CurrentParams.Invert;
            }
            finally
            {
                mIsUpdatingUI = false;
            }
        }

        private void SyncParamsFromControls()
        {
            if (mIsUpdatingUI) return;

            CurrentParams.Gamma = TbGamma.Value / 100f;
            CurrentParams.Sharpness = TbSharpness.Value / 100f;
            CurrentParams.LocalContrast = TbLocalContrast.Value / 100f;
            CurrentParams.Brightness = TbBrightness.Value;
            CurrentParams.Contrast = TbContrast.Value;
            CurrentParams.BackgroundClean = TbBgClean.Value;
            if (CbDitherAlgo.SelectedItem is ImageTransform.DitheringMode mode)
            {
                CurrentParams.Dithering = mode;
            }
            CurrentParams.Invert = ChkInvert.Checked;

            LblGammaVal.Text = $"{CurrentParams.Gamma:0.00}x";
            LblSharpVal.Text = $"{TbSharpness.Value}%";
            LblLocalVal.Text = $"{TbLocalContrast.Value}%";
            LblBrightVal.Text = $"{TbBrightness.Value}";
            LblContrastVal.Text = $"{TbContrast.Value}";
            LblBgCleanVal.Text = $"{TbBgClean.Value}";

            mDebounceTimer.Stop();
            mDebounceTimer.Start();
        }

        private System.Threading.CancellationTokenSource mPipelineCts;

        private async void RecalculatePipeline()
        {
            mPipelineCts?.Cancel();
            mPipelineCts?.Dispose();
            mPipelineCts = new System.Threading.CancellationTokenSource();
            System.Threading.CancellationToken token = mPipelineCts.Token;

            PhotoEnhanceParams paramCopy = CurrentParams.Clone();
            Bitmap source = mDisplaySource;
            if (source == null) return;

            try
            {
                var pack = await System.Threading.Tasks.Task.Run(() =>
                {
                    if (token.IsCancellationRequested) return null;

                    Bitmap enhanced = PhotoEnhancer.Enhance(source, paramCopy);
                    if (token.IsCancellationRequested) { enhanced?.Dispose(); return null; }

                    Bitmap dithered = ImageTransform.DitherImage(enhanced, paramCopy.Dithering);
                    if (token.IsCancellationRequested) { enhanced?.Dispose(); dithered?.Dispose(); return null; }

                    Bitmap wood = PhotoEnhancer.GenerateWoodBurnSimulation(dithered);
                    Bitmap slate = PhotoEnhancer.GenerateSlateSimulation(dithered);

                    return new { Enhanced = enhanced, Dithered = dithered, Wood = wood, Slate = slate };
                }, token);

                if (token.IsCancellationRequested || pack == null) return;

                mEnhancedDisplay?.Dispose();
                mDitheredDisplay?.Dispose();
                mWoodSimDisplay?.Dispose();
                mSlateSimDisplay?.Dispose();

                mEnhancedDisplay = pack.Enhanced;
                mDitheredDisplay = pack.Dithered;
                mWoodSimDisplay = pack.Wood;
                mSlateSimDisplay = pack.Slate;

                PnlCanvas.Invalidate();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        private void CenterAndFitImage()
        {
            if (mDisplaySource == null || PnlCanvas.Width <= 0 || PnlCanvas.Height <= 0) return;

            float pad = 20f;
            float availW = Math.Max(50f, PnlCanvas.Width - pad * 2);
            float availH = Math.Max(50f, PnlCanvas.Height - pad * 2);

            float scale = Math.Min(availW / mDisplaySource.Width, availH / mDisplaySource.Height);
            mZoom = Math.Max(0.1f, Math.Min(5.0f, scale));

            float imgW = mDisplaySource.Width * mZoom;
            float imgH = mDisplaySource.Height * mZoom;

            mPan = new PointF((PnlCanvas.Width - imgW) / 2f, (PnlCanvas.Height - imgH) / 2f);
            LblZoomInfo.Text = $"{Math.Round(mZoom * 100)}%";
            PnlCanvas.Invalidate();
        }

        private void PnlCanvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.FromArgb(32, 33, 36));

            if (mDisplaySource == null) return;

            g.InterpolationMode = InterpolationMode.NearestNeighbor; // Keep dither pixel crispness
            g.PixelOffsetMode = PixelOffsetMode.Half;

            int canvasW = PnlCanvas.Width;
            int canvasH = PnlCanvas.Height;

            if (mViewMode == 3) // Side-by-Side Comparison
            {
                int halfW = canvasW / 2;

                // Left: Original Source
                Rectangle leftRect = new Rectangle((int)mPan.X, (int)mPan.Y, (int)(mDisplaySource.Width * mZoom), (int)(mDisplaySource.Height * mZoom));
                
                // Set clip to left half
                g.SetClip(new Rectangle(0, 0, halfW - 2, canvasH));
                g.DrawImage(mDisplaySource, leftRect);
                g.ResetClip();

                // Right: Laser Result
                Bitmap rightImg = mWoodSimDisplay ?? mDitheredDisplay;
                if (rightImg != null)
                {
                    Rectangle rightRect = new Rectangle((int)(mPan.X + halfW), (int)mPan.Y, (int)(rightImg.Width * mZoom), (int)(rightImg.Height * mZoom));
                    g.SetClip(new Rectangle(halfW + 2, 0, canvasW - halfW - 2, canvasH));
                    g.DrawImage(rightImg, rightRect);
                    g.ResetClip();
                }

                // Split separator
                using (Pen sepPen = new Pen(Color.FromArgb(120, 255, 255, 255), 2))
                {
                    g.DrawLine(sepPen, halfW, 0, halfW, canvasH);
                }

                // Badges
                DrawBadge(g, "FOTO ORIGINAL", 15, 15, Color.FromArgb(180, 20, 20, 20), Color.White);
                DrawBadge(g, "SIMULAÇÃO LASER (MADEIRA)", halfW + 15, 15, Color.FromArgb(180, 46, 27, 16), Color.FromArgb(240, 220, 190));
            }
            else
            {
                Bitmap imgToDraw = null;
                string badgeText = "";
                Color badgeBg = Color.FromArgb(180, 20, 20, 20);
                Color badgeFg = Color.White;

                switch (mViewMode)
                {
                    case 0:
                        imgToDraw = mWoodSimDisplay;
                        badgeText = "🪵 SIMULAÇÃO REAL: MADEIRA QUEIMADA (PINUS / COMPENSADO)";
                        badgeBg = Color.FromArgb(200, 46, 27, 16);
                        badgeFg = Color.FromArgb(250, 230, 200);
                        break;
                    case 1:
                        imgToDraw = mDitheredDisplay;
                        badgeText = "⚫ MATRIZ P&B: PONTOS REAIS DO LASER (1-BIT DITHER)";
                        badgeBg = Color.FromArgb(200, 15, 15, 15);
                        badgeFg = Color.LightGreen;
                        break;
                    case 2:
                        imgToDraw = mSlateSimDisplay;
                        badgeText = "🪨 SIMULAÇÃO ARDÓSIA / ACRÍLICO PRETO";
                        badgeBg = Color.FromArgb(200, 25, 28, 32);
                        badgeFg = Color.FromArgb(230, 235, 240);
                        break;
                }

                if (imgToDraw != null)
                {
                    Rectangle destRect = new Rectangle((int)mPan.X, (int)mPan.Y, (int)(imgToDraw.Width * mZoom), (int)(imgToDraw.Height * mZoom));
                    g.DrawImage(imgToDraw, destRect);
                }

                DrawBadge(g, badgeText, 15, 15, badgeBg, badgeFg);
            }
        }

        private void DrawBadge(Graphics g, string text, int x, int y, Color bg, Color fg)
        {
            using (Font f = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                SizeF size = g.MeasureString(text, f);
                Rectangle badgeRect = new Rectangle(x, y, (int)size.Width + 16, (int)size.Height + 8);

                using (GraphicsPath path = CreateRoundRect(badgeRect, 6))
                using (SolidBrush b = new SolidBrush(bg))
                using (Pen p = new Pen(Color.FromArgb(80, 255, 255, 255), 1))
                {
                    g.FillPath(b, path);
                    g.DrawPath(p, path);
                }

                using (SolidBrush textBrush = new SolidBrush(fg))
                {
                    g.DrawString(text, f, textBrush, x + 8, y + 4);
                }
            }
        }

        private GraphicsPath CreateRoundRect(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void PnlCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle)
            {
                mIsDragging = true;
                mLastMousePos = e.Location;
                PnlCanvas.Cursor = Cursors.SizeAll;
            }
        }

        private void PnlCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (mIsDragging)
            {
                float dx = e.X - mLastMousePos.X;
                float dy = e.Y - mLastMousePos.Y;
                mPan.X += dx;
                mPan.Y += dy;
                mLastMousePos = e.Location;
                PnlCanvas.Invalidate();
            }
        }

        private void PnlCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            mIsDragging = false;
            PnlCanvas.Cursor = Cursors.Default;
        }

        private void PnlCanvas_MouseWheel(object sender, MouseEventArgs e)
        {
            float oldZoom = mZoom;
            if (e.Delta > 0)
                mZoom = Math.Min(6.0f, mZoom * 1.2f);
            else if (e.Delta < 0)
                mZoom = Math.Max(0.1f, mZoom / 1.2f);

            // Zoom centered on cursor
            float factor = mZoom / oldZoom;
            mPan.X = e.X - (e.X - mPan.X) * factor;
            mPan.Y = e.Y - (e.Y - mPan.Y) * factor;

            LblZoomInfo.Text = $"{Math.Round(mZoom * 100)}%";
            PnlCanvas.Invalidate();
        }

        private void BtnZoomIn_Click(object sender, EventArgs e)
        {
            mZoom = Math.Min(6.0f, mZoom * 1.25f);
            LblZoomInfo.Text = $"{Math.Round(mZoom * 100)}%";
            PnlCanvas.Invalidate();
        }

        private void BtnZoomOut_Click(object sender, EventArgs e)
        {
            mZoom = Math.Max(0.1f, mZoom / 1.25f);
            LblZoomInfo.Text = $"{Math.Round(mZoom * 100)}%";
            PnlCanvas.Invalidate();
        }

        private void BtnZoomFit_Click(object sender, EventArgs e)
        {
            CenterAndFitImage();
        }

        private void BtnZoom100_Click(object sender, EventArgs e)
        {
            mZoom = 1.0f;
            if (mDisplaySource != null)
            {
                mPan = new PointF((PnlCanvas.Width - mDisplaySource.Width) / 2f, (PnlCanvas.Height - mDisplaySource.Height) / 2f);
            }
            LblZoomInfo.Text = "100%";
            PnlCanvas.Invalidate();
        }

        private void UpdatePresetSelection(int presetIdx)
        {
            BtnPresetPortrait.BackColor = (presetIdx == 0) ? Color.FromArgb(0, 122, 217) : ColorScheme.FormButtonsColor;
            BtnPresetHighDetail.BackColor = (presetIdx == 1) ? Color.FromArgb(0, 122, 217) : ColorScheme.FormButtonsColor;
            BtnPresetPet.BackColor = (presetIdx == 2) ? Color.FromArgb(0, 122, 217) : ColorScheme.FormButtonsColor;
            BtnPresetSketch.BackColor = (presetIdx == 3) ? Color.FromArgb(0, 122, 217) : ColorScheme.FormButtonsColor;
            BtnPresetSlate.BackColor = (presetIdx == 4) ? Color.FromArgb(0, 122, 217) : ColorScheme.FormButtonsColor;
        }

        private void BtnPresetPortrait_Click(object sender, EventArgs e)
        {
            CurrentParams = PhotoEnhanceParams.CreatePortraitPreset();
            UpdatePresetSelection(0);
            SyncControlsFromParams();
            RecalculatePipeline();
        }

        private void BtnPresetHighDetail_Click(object sender, EventArgs e)
        {
            CurrentParams = PhotoEnhanceParams.CreateHighDetailPreset();
            UpdatePresetSelection(1);
            SyncControlsFromParams();
            RecalculatePipeline();
        }

        private void BtnPresetPet_Click(object sender, EventArgs e)
        {
            CurrentParams = PhotoEnhanceParams.CreatePetPreset();
            UpdatePresetSelection(2);
            SyncControlsFromParams();
            RecalculatePipeline();
        }

        private void BtnPresetSketch_Click(object sender, EventArgs e)
        {
            CurrentParams = PhotoEnhanceParams.CreateSketchPreset();
            UpdatePresetSelection(3);
            SyncControlsFromParams();
            RecalculatePipeline();
        }

        private void BtnPresetSlate_Click(object sender, EventArgs e)
        {
            CurrentParams = PhotoEnhanceParams.CreateSlatePreset();
            UpdatePresetSelection(4);
            SyncControlsFromParams();
            RecalculatePipeline();
        }

        private void Slider_Scroll(object sender, EventArgs e)
        {
            SyncParamsFromControls();
        }

        private void CbDitherAlgo_SelectedIndexChanged(object sender, EventArgs e)
        {
            SyncParamsFromControls();
        }

        private void ChkInvert_CheckedChanged(object sender, EventArgs e)
        {
            SyncParamsFromControls();
        }

        private void RbViewMode_CheckedChanged(object sender, EventArgs e)
        {
            if (RbViewWood.Checked) mViewMode = 0;
            else if (RbViewDots.Checked) mViewMode = 1;
            else if (RbViewSlate.Checked) mViewMode = 2;
            else if (RbViewSplit.Checked) mViewMode = 3;

            PnlCanvas.Invalidate();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            CurrentParams = PhotoEnhanceParams.CreatePortraitPreset();
            UpdatePresetSelection(0);
            SyncControlsFromParams();
            RecalculatePipeline();
            CenterAndFitImage();
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                // Generate the full-resolution enhanced result using the full source image
                EnhancedFullResult = PhotoEnhancer.Enhance(mSourceOriginal, CurrentParams);
                DialogResult = DialogResult.OK;
                Close();
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                mPipelineCts?.Cancel();
                mPipelineCts?.Dispose();
                mDebounceTimer?.Dispose();
                mSourceOriginal?.Dispose();
                mDisplaySource?.Dispose();
                mEnhancedDisplay?.Dispose();
                mDitheredDisplay?.Dispose();
                mWoodSimDisplay?.Dispose();
                mSlateSimDisplay?.Dispose();
                EnhancedFullResult?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
