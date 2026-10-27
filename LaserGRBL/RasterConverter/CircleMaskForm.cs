using LaserGRBL.Icons;
using LaserGRBL.LightBurn;
using LaserGRBL.UserControls;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace LaserGRBL.RasterConverter
{
    public class CircleMaskForm : Form
    {
        private Bitmap mSource;
        public Bitmap ResultBitmap { get; private set; }

        public JigShape SelectedShape { get; private set; } = JigShape.Circle;
        public double SelectedDiameter { get; private set; } = 40.0;
        public double SelectedWidth { get; private set; } = 40.0;
        public double SelectedHeight { get; private set; } = 30.0;
        public double SelectedCornerRadius { get; private set; } = 0.0;

        public double EffectiveWidth => SelectedShape == JigShape.Circle ? SelectedDiameter : SelectedWidth;
        public double EffectiveHeight => SelectedShape == JigShape.Circle ? SelectedDiameter : (SelectedShape == JigShape.Square ? SelectedWidth : SelectedHeight);

        private float mZoom = 1.0f;
        private PointF mPan = new PointF(0, 0);
        private bool mIsDragging = false;
        private Point mLastMouse;

        private Panel pnlCanvas;
        private ComboBox cboShape;
        private Label lblDiameter;
        private NumericUpDown numDiameter;
        private Label lblWidth;
        private NumericUpDown numWidth;
        private Label lblHeight;
        private NumericUpDown numHeight;
        private Label lblCornerRadius;
        private NumericUpDown numCornerRadius;
        private FlowLayoutPanel flpSizes;

        private TrackBar tbZoom;
        private Label lblZoomVal;
        private CheckBox chkBorder;

        public CircleMaskForm(Bitmap source, double initialDiameter = 40.0)
        {
            mSource = source != null ? (Bitmap)source.Clone() : new Bitmap(200, 200);

            // Initialize defaults from KeychainJigManager if available
            SelectedShape = KeychainJigManager.Shape;
            SelectedDiameter = initialDiameter > 0 ? initialDiameter : KeychainJigManager.Diameter;
            SelectedWidth = KeychainJigManager.Width;
            SelectedHeight = KeychainJigManager.Height;
            SelectedCornerRadius = KeychainJigManager.CornerRadius;

            InitializeComponent();
            ApplyTheme();
            UpdateShapeFields();
            FitToShape();
        }

        private void InitializeComponent()
        {
            this.Text = "Molde de Peça / Chaveiro - Encaixar Foto no Formato Desejado";
            this.Size = new Size(780, 640);
            this.MinimumSize = new Size(720, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.ShowIcon = false;

            // Header instructions
            Label lblHeader = new Label()
            {
                Text = "Arraste com o mouse para posicionar a foto dentro do molde. Use a roda do mouse ou a barra lateral para zoom.",
                Location = new Point(14, 10),
                Size = new Size(740, 22),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.DarkGray
            };

            // Canvas for interactive shape framing
            pnlCanvas = new Panel()
            {
                Location = new Point(14, 36),
                Size = new Size(515, 545),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(22, 28, 38)
            };
            pnlCanvas.DoubleBuffered(true);
            pnlCanvas.Paint += PnlCanvas_Paint;
            pnlCanvas.MouseDown += PnlCanvas_MouseDown;
            pnlCanvas.MouseMove += PnlCanvas_MouseMove;
            pnlCanvas.MouseUp += PnlCanvas_MouseUp;
            pnlCanvas.MouseWheel += PnlCanvas_MouseWheel;
            pnlCanvas.Resize += (s, e) => pnlCanvas.Invalidate();

            // Right side Controls panel
            Panel pnlRight = new Panel()
            {
                Location = new Point(538, 36),
                Size = new Size(215, 545),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right
            };

            // Shape Selector
            Label lblShape = new Label() { Text = "Formato do Molde:", Location = new Point(0, 4), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cboShape = new ComboBox()
            {
                Location = new Point(0, 24),
                Size = new Size(205, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                DrawMode = DrawMode.OwnerDrawFixed
            };
            cboShape.Items.Add("⭕ Redondo (Círculo)");
            cboShape.Items.Add("⬛ Quadrado");
            cboShape.Items.Add("▭ Retangular");
            cboShape.SelectedIndex = (int)SelectedShape;
            cboShape.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                bool isDark = ColorScheme.DarkScheme;
                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color bg = isSelected ? (isDark ? Color.FromArgb(37, 99, 235) : SystemColors.Highlight)
                                      : (isDark ? Color.FromArgb(30, 41, 59) : SystemColors.Window);
                Color fg = isSelected ? Color.White
                                      : (isDark ? Color.FromArgb(248, 250, 252) : SystemColors.WindowText);

                using (SolidBrush bgBrush = new SolidBrush(bg))
                using (SolidBrush fgBrush = new SolidBrush(fg))
                {
                    e.Graphics.FillRectangle(bgBrush, e.Bounds);
                    string text = cboShape.Items[e.Index].ToString();
                    e.Graphics.DrawString(text, cboShape.Font, fgBrush, e.Bounds.X + 2, e.Bounds.Y + 3);
                }
            };
            cboShape.SelectedIndexChanged += (s, e) =>
            {
                SelectedShape = (JigShape)cboShape.SelectedIndex;
                UpdateShapeFields();
                FitToShape();
            };

            // Diameter (Circle)
            lblDiameter = new Label() { Text = "Diâmetro (mm):", Location = new Point(0, 56), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            numDiameter = new NumericUpDown()
            {
                Location = new Point(0, 76),
                Size = new Size(95, 23),
                Minimum = 5,
                Maximum = 500,
                DecimalPlaces = 1,
                Value = (decimal)SelectedDiameter,
                Font = new Font("Segoe UI", 9f)
            };
            numDiameter.ValueChanged += (s, e) =>
            {
                SelectedDiameter = (double)numDiameter.Value;
                pnlCanvas.Invalidate();
            };

            // Width (Square & Rectangle)
            lblWidth = new Label() { Text = "Largura (mm):", Location = new Point(0, 56), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            numWidth = new NumericUpDown()
            {
                Location = new Point(0, 76),
                Size = new Size(95, 23),
                Minimum = 5,
                Maximum = 500,
                DecimalPlaces = 1,
                Value = (decimal)SelectedWidth,
                Font = new Font("Segoe UI", 9f)
            };
            numWidth.ValueChanged += (s, e) =>
            {
                SelectedWidth = (double)numWidth.Value;
                if (SelectedShape == JigShape.Square) SelectedHeight = SelectedWidth;
                pnlCanvas.Invalidate();
            };

            // Height (Rectangle)
            lblHeight = new Label() { Text = "Altura (mm):", Location = new Point(108, 56), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            numHeight = new NumericUpDown()
            {
                Location = new Point(108, 76),
                Size = new Size(95, 23),
                Minimum = 5,
                Maximum = 500,
                DecimalPlaces = 1,
                Value = (decimal)SelectedHeight,
                Font = new Font("Segoe UI", 9f)
            };
            numHeight.ValueChanged += (s, e) =>
            {
                SelectedHeight = (double)numHeight.Value;
                pnlCanvas.Invalidate();
            };

            // Corner Radius (Square & Rectangle)
            lblCornerRadius = new Label() { Text = "Cantos Arred. (mm):", Location = new Point(0, 106), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numCornerRadius = new NumericUpDown()
            {
                Location = new Point(115, 104),
                Size = new Size(88, 23),
                Minimum = 0,
                Maximum = 100,
                DecimalPlaces = 1,
                Value = (decimal)SelectedCornerRadius,
                Font = new Font("Segoe UI", 9f)
            };
            numCornerRadius.ValueChanged += (s, e) =>
            {
                SelectedCornerRadius = (double)numCornerRadius.Value;
                pnlCanvas.Invalidate();
            };

            // Quick size buttons
            flpSizes = new FlowLayoutPanel()
            {
                Location = new Point(0, 134),
                Size = new Size(205, 34),
                WrapContents = false
            };
            int[] sizes = new int[] { 30, 35, 40, 45, 50 };
            foreach (int sz in sizes)
            {
                Button btnSz = new Button()
                {
                    Text = $"{sz}mm",
                    Size = new Size(38, 26),
                    Font = new Font("Segoe UI", 8f),
                    Margin = new Padding(1)
                };
                int target = sz;
                btnSz.Click += (s, e) =>
                {
                    if (SelectedShape == JigShape.Circle)
                        numDiameter.Value = target;
                    else
                    {
                        numWidth.Value = target;
                        if (SelectedShape == JigShape.Square) numHeight.Value = target;
                    }
                    FitToShape();
                };
                flpSizes.Controls.Add(btnSz);
            }

            // Zoom controls
            Label lblZoom = new Label() { Text = "Zoom da Imagem:", Location = new Point(0, 175), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            lblZoomVal = new Label() { Text = "100%", Location = new Point(140, 175), AutoSize = true, Font = new Font("Segoe UI", 9f), ForeColor = Color.LightSkyBlue };

            tbZoom = new TrackBar()
            {
                Location = new Point(0, 195),
                Size = new Size(205, 36),
                Minimum = 10,
                Maximum = 400,
                Value = 100,
                TickFrequency = 50
            };
            tbZoom.ValueChanged += (s, e) =>
            {
                mZoom = tbZoom.Value / 100.0f;
                lblZoomVal.Text = $"{tbZoom.Value}%";
                pnlCanvas.Invalidate();
            };

            // Alignment buttons
            Button btnCenter = new Button()
            {
                Text = "🎯 Centralizar",
                Location = new Point(0, 238),
                Size = new Size(205, 28),
                Font = new Font("Segoe UI", 8.8f),
                Cursor = Cursors.Hand
            };
            btnCenter.Click += (s, e) => { mPan = PointF.Empty; pnlCanvas.Invalidate(); };

            Button btnFit = new Button()
            {
                Text = "📐 Ajustar ao Molde",
                Location = new Point(0, 272),
                Size = new Size(205, 28),
                Font = new Font("Segoe UI", 8.8f),
                Cursor = Cursors.Hand
            };
            btnFit.Click += (s, e) => FitToShape();

            Button btnFill = new Button()
            {
                Text = "🔍 Preencher Molde",
                Location = new Point(0, 306),
                Size = new Size(205, 28),
                Font = new Font("Segoe UI", 8.8f),
                Cursor = Cursors.Hand
            };
            btnFill.Click += (s, e) => FillShape();

            chkBorder = new CheckBox()
            {
                Text = "Contorno fino guia (1px)",
                Location = new Point(0, 344),
                Size = new Size(205, 24),
                Font = new Font("Segoe UI", 8.5f),
                Checked = false
            };
            chkBorder.CheckedChanged += (s, e) => pnlCanvas.Invalidate();

            // Big Action buttons
            Button btnApply = new Button()
            {
                Text = "✅ Aplicar ao Chaveiro",
                Location = new Point(0, 385),
                Size = new Size(205, 44),
                BackColor = Color.FromArgb(30, 150, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += BtnApply_Click;

            Button btnCancel = new Button()
            {
                Text = "Cancelar",
                Location = new Point(0, 438),
                Size = new Size(205, 32),
                DialogResult = DialogResult.Cancel,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };

            pnlRight.Controls.AddRange(new Control[] {
                lblShape, cboShape,
                lblDiameter, numDiameter,
                lblWidth, numWidth,
                lblHeight, numHeight,
                lblCornerRadius, numCornerRadius,
                flpSizes,
                lblZoom, lblZoomVal, tbZoom,
                btnCenter, btnFit, btnFill, chkBorder,
                btnApply, btnCancel
            });

            this.Controls.AddRange(new Control[] { lblHeader, pnlCanvas, pnlRight });
        }

        private void UpdateShapeFields()
        {
            bool isCircle = SelectedShape == JigShape.Circle;
            bool isSquare = SelectedShape == JigShape.Square;
            bool isRect = SelectedShape == JigShape.Rectangle;

            lblDiameter.Visible = isCircle;
            numDiameter.Visible = isCircle;

            lblWidth.Visible = isSquare || isRect;
            numWidth.Visible = isSquare || isRect;
            lblWidth.Text = isSquare ? "Lado (mm):" : "Largura (mm):";

            lblHeight.Visible = isRect;
            numHeight.Visible = isRect;

            lblCornerRadius.Visible = isSquare || isRect;
            numCornerRadius.Visible = isSquare || isRect;

            pnlCanvas.Invalidate();
        }

        private void ApplyTheme()
        {
            bool isDark = ColorScheme.DarkScheme;
            Color backColor = isDark ? Color.FromArgb(24, 30, 42) : ColorScheme.FormBackColor;
            Color foreColor = isDark ? Color.FromArgb(241, 245, 249) : ColorScheme.FormForeColor;
            Color panelBack = isDark ? Color.FromArgb(30, 41, 59) : Color.FromArgb(245, 245, 245);
            Color inputBack = isDark ? Color.FromArgb(15, 23, 42) : Color.White;
            Color inputFore = isDark ? Color.FromArgb(248, 250, 252) : Color.Black;
            Color borderCol = isDark ? Color.FromArgb(71, 85, 105) : Color.LightGray;

            this.BackColor = backColor;
            this.ForeColor = foreColor;

            ApplyThemeRecursive(this, isDark, backColor, foreColor, panelBack, inputBack, inputFore, borderCol);

            ThemeMgr.SetTheme(this, true);
        }

        private void ApplyThemeRecursive(Control parent, bool isDark, Color backColor, Color foreColor, Color panelBack, Color inputBack, Color inputFore, Color borderCol)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Label lbl)
                {
                    lbl.ForeColor = isDark ? Color.FromArgb(226, 232, 240) : Color.FromArgb(30, 30, 30);
                }
                else if (c is CheckBox chk)
                {
                    chk.ForeColor = isDark ? Color.FromArgb(241, 245, 249) : Color.FromArgb(30, 30, 30);
                }
                else if (c is NumericUpDown num)
                {
                    num.BackColor = inputBack;
                    num.ForeColor = inputFore;
                }
                else if (c is ComboBox cb)
                {
                    cb.BackColor = inputBack;
                    cb.ForeColor = inputFore;
                }
                else if (c is Button btn)
                {
                    if (btn.Text.Contains("Aplicar"))
                    {
                        // Keep distinct green styling
                    }
                    else
                    {
                        btn.BackColor = isDark ? Color.FromArgb(51, 65, 85) : Color.FromArgb(235, 235, 235);
                        btn.ForeColor = isDark ? Color.White : Color.Black;
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = borderCol;
                    }
                }
                else if (c is Panel p && p != pnlCanvas)
                {
                    ApplyThemeRecursive(p, isDark, backColor, foreColor, panelBack, inputBack, inputFore, borderCol);
                }
            }
        }

        private SizeF GetCanvasShapeSize()
        {
            float maxCanvasW = pnlCanvas.Width * 0.85f;
            float maxCanvasH = pnlCanvas.Height * 0.82f;

            float wMm = (float)EffectiveWidth;
            float hMm = (float)EffectiveHeight;

            float scale = Math.Min(maxCanvasW / wMm, maxCanvasH / hMm);
            return new SizeF(wMm * scale, hMm * scale);
        }

        private RectangleF GetCanvasShapeRect()
        {
            SizeF sz = GetCanvasShapeSize();
            float cx = pnlCanvas.Width / 2f;
            float cy = pnlCanvas.Height / 2f;
            return new RectangleF(cx - sz.Width / 2f, cy - sz.Height / 2f, sz.Width, sz.Height);
        }

        private void FitToShape()
        {
            if (mSource == null) return;
            SizeF shapeSz = GetCanvasShapeSize();
            float maxDim = Math.Max(mSource.Width, mSource.Height);
            if (maxDim <= 0) return;

            float targetDim = Math.Min(shapeSz.Width, shapeSz.Height);
            mZoom = targetDim / maxDim;
            mPan = PointF.Empty;
            UpdateZoomSlider();
            pnlCanvas.Invalidate();
        }

        private void FillShape()
        {
            if (mSource == null) return;
            SizeF shapeSz = GetCanvasShapeSize();
            if (mSource.Width <= 0 || mSource.Height <= 0) return;

            float scaleX = shapeSz.Width / (float)mSource.Width;
            float scaleY = shapeSz.Height / (float)mSource.Height;

            mZoom = Math.Max(scaleX, scaleY);
            mPan = PointF.Empty;
            UpdateZoomSlider();
            pnlCanvas.Invalidate();
        }

        private void UpdateZoomSlider()
        {
            int val = (int)Math.Round(mZoom * 100);
            val = Math.Max(tbZoom.Minimum, Math.Min(tbZoom.Maximum, val));
            tbZoom.Value = val;
            lblZoomVal.Text = $"{val}%";
        }

        private static GraphicsPath CreateRoundedPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0.1f)
            {
                path.AddRectangle(rect);
                return path;
            }
            float d = radius * 2.0f;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void PnlCanvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            PointF center = new PointF(pnlCanvas.Width / 2f, pnlCanvas.Height / 2f);
            RectangleF shapeRect = GetCanvasShapeRect();
            float crMm = SelectedShape == JigShape.Circle ? 0 : (float)Math.Min(SelectedCornerRadius, Math.Min(EffectiveWidth, EffectiveHeight) / 2.0);
            float screenCr = crMm * (shapeRect.Width / (float)EffectiveWidth);

            // 1. Draw source image transformed with Pan and Zoom
            if (mSource != null)
            {
                float imgW = mSource.Width * mZoom;
                float imgH = mSource.Height * mZoom;
                float imgX = center.X + mPan.X - imgW / 2f;
                float imgY = center.Y + mPan.Y - imgH / 2f;

                g.DrawImage(mSource, new RectangleF(imgX, imgY, imgW, imgH));
            }

            // 2. Dark Mask Outside the Shape (Vignette)
            using (GraphicsPath outerPath = new GraphicsPath())
            {
                outerPath.AddRectangle(new RectangleF(0, 0, pnlCanvas.Width, pnlCanvas.Height));

                if (SelectedShape == JigShape.Circle)
                {
                    outerPath.AddEllipse(shapeRect);
                }
                else
                {
                    using (GraphicsPath sp = CreateRoundedPath(shapeRect, screenCr))
                    {
                        outerPath.AddPath(sp, false);
                    }
                }

                using (SolidBrush maskBrush = new SolidBrush(Color.FromArgb(215, 15, 18, 25)))
                {
                    g.FillPath(maskBrush, outerPath);
                }
            }

            // 3. Crisp Outline Border of the Shape
            using (Pen borderPen = new Pen(Color.FromArgb(255, 200, 40), 2.5f))
            {
                if (SelectedShape == JigShape.Circle)
                {
                    g.DrawEllipse(borderPen, shapeRect);
                }
                else
                {
                    using (GraphicsPath sp = CreateRoundedPath(shapeRect, screenCr))
                    {
                        g.DrawPath(borderPen, sp);
                    }
                }
            }

            // 4. Center Crosshair
            using (Pen crossPen = new Pen(Color.FromArgb(160, 255, 220, 80), 1.2f))
            {
                g.DrawLine(crossPen, center.X - 10, center.Y, center.X + 10, center.Y);
                g.DrawLine(crossPen, center.X, center.Y - 10, center.X, center.Y + 10);
            }

            // 5. Dimension info label
            using (Font dimFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                string shapeIcon = SelectedShape == JigShape.Circle ? "⭕" : (SelectedShape == JigShape.Square ? "⬛" : "▭");
                string info = SelectedShape == JigShape.Circle
                    ? $"{shapeIcon} Chaveiro Ø {SelectedDiameter:0.0} mm"
                    : $"{shapeIcon} Peça {EffectiveWidth:0.0}×{EffectiveHeight:0.0} mm";

                SizeF sz = g.MeasureString(info, dimFont);
                g.DrawString(info, dimFont, textBrush, (pnlCanvas.Width - sz.Width) / 2f, shapeRect.Bottom + 10);
            }
        }

        private void PnlCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mIsDragging = true;
                mLastMouse = e.Location;
                pnlCanvas.Cursor = Cursors.SizeAll;
            }
        }

        private void PnlCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (mIsDragging)
            {
                float dx = e.X - mLastMouse.X;
                float dy = e.Y - mLastMouse.Y;
                mPan.X += dx;
                mPan.Y += dy;
                mLastMouse = e.Location;
                pnlCanvas.Invalidate();
            }
        }

        private void PnlCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            mIsDragging = false;
            pnlCanvas.Cursor = Cursors.Default;
        }

        private void PnlCanvas_MouseWheel(object sender, MouseEventArgs e)
        {
            float zoomFactor = e.Delta > 0 ? 1.1f : 0.9f;
            mZoom = Math.Max(0.1f, Math.Min(4.0f, mZoom * zoomFactor));
            UpdateZoomSlider();
            pnlCanvas.Invalidate();
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            if (mSource == null) return;

            try
            {
                float wMm = (float)EffectiveWidth;
                float hMm = (float)EffectiveHeight;
                float crMm = SelectedShape == JigShape.Circle ? 0 : (float)Math.Min(SelectedCornerRadius, Math.Min(wMm, hMm) / 2.0);

                // Determine high-resolution output aspect ratio
                float maxOutputDim = 1600f;
                float ratioW = wMm >= hMm ? 1.0f : wMm / hMm;
                float ratioH = hMm >= wMm ? 1.0f : hMm / wMm;

                int outW = (int)Math.Round(maxOutputDim * ratioW);
                int outH = (int)Math.Round(maxOutputDim * ratioH);

                Bitmap result = new Bitmap(outW, outH, PixelFormat.Format32bppArgb);

                using (Graphics g = Graphics.FromImage(result))
                {
                    g.Clear(Color.White);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    RectangleF shapeCanvasRect = GetCanvasShapeRect();
                    float scaleRatio = (float)outW / shapeCanvasRect.Width;
                    float outCr = crMm * (outW / wMm);

                    RectangleF outShapeRect = new RectangleF(0, 0, outW, outH);

                    // Compute transformed image position relative to output bitmap
                    float imgW = mSource.Width * mZoom * scaleRatio;
                    float imgH = mSource.Height * mZoom * scaleRatio;
                    PointF outCenter = new PointF(outW / 2f, outH / 2f);
                    float imgX = outCenter.X + (mPan.X * scaleRatio) - imgW / 2f;
                    float imgY = outCenter.Y + (mPan.Y * scaleRatio) - imgH / 2f;

                    // Clip to chosen shape and draw image
                    using (GraphicsPath clipPath = new GraphicsPath())
                    {
                        if (SelectedShape == JigShape.Circle)
                        {
                            clipPath.AddEllipse(outShapeRect);
                        }
                        else
                        {
                            using (GraphicsPath p = CreateRoundedPath(outShapeRect, outCr))
                            {
                                clipPath.AddPath(p, false);
                            }
                        }

                        g.SetClip(clipPath);
                        g.DrawImage(mSource, new RectangleF(imgX, imgY, imgW, imgH));
                        g.ResetClip();
                    }

                    // Optional hairline border
                    if (chkBorder.Checked)
                    {
                        using (Pen bPen = new Pen(Color.Black, Math.Max(1.5f, outW / 500f)))
                        {
                            if (SelectedShape == JigShape.Circle)
                                g.DrawEllipse(bPen, 1, 1, outW - 2, outH - 2);
                            else
                            {
                                using (GraphicsPath p = CreateRoundedPath(new RectangleF(1, 1, outW - 2, outH - 2), outCr))
                                {
                                    g.DrawPath(bPen, p);
                                }
                            }
                        }
                    }
                }

                ResultBitmap = result;
                SelectedDiameter = (double)numDiameter.Value;
                SelectedWidth = (double)numWidth.Value;
                SelectedHeight = (double)numHeight.Value;
                SelectedCornerRadius = (double)numCornerRadius.Value;

                // Sync with KeychainJigManager
                KeychainJigManager.Shape = SelectedShape;
                KeychainJigManager.Diameter = (float)SelectedDiameter;
                KeychainJigManager.Width = (float)SelectedWidth;
                KeychainJigManager.Height = (float)SelectedHeight;
                KeychainJigManager.CornerRadius = (float)SelectedCornerRadius;

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Erro ao gerar imagem recortada: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public static class ControlExtensions
    {
        public static void DoubleBuffered(this Control control, bool enable)
        {
            var doubleBufferPropertyInfo = control.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            doubleBufferPropertyInfo?.SetValue(control, enable, null);
        }
    }
}
