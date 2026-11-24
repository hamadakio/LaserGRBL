using LaserGRBL.UserControls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace LaserGRBL.LightBurn
{
    public class CutAroundForm : Form
    {
        private readonly GrblCore mCore;
        private readonly EngravingGeometry mCachedEngravingGeometry;
        private readonly bool mIsIllustrativePreview;
        private readonly CutMachineOptions mMachineOptions;
        private readonly double mBedWidthMm;
        private readonly double mBedHeightMm;
        private CutAroundBuildResult mCachedPreviewBuild;

        private ComboBox cboShape;
        private NumericUpDown numMargin;
        private Label lblCornerRadius;
        private NumericUpDown numCornerRadius;
        private NumericUpDown numSpeed;
        private NumericUpDown numPower;
        private Label lblPowerS;
        private NumericUpDown numPasses;
        private ComboBox cboLaserMode;
        private NumericUpDown numZStep;
        private CheckBox chkAirAssist;

        private Panel pnlPreview;
        private Label lblDimensions;
        private Label lblBedWarning;

        private Button btnApply;
        private Button btnDisable;
        private Button btnCancel;

        private bool mSuppressEvents = false;

        public CutAroundConfig ResultConfig { get; private set; }

        internal ComboBox ShapeComboBox => cboShape;
        internal NumericUpDown MarginInput => numMargin;
        internal NumericUpDown CornerRadiusInput => numCornerRadius;
        internal NumericUpDown SpeedInput => numSpeed;
        internal NumericUpDown PowerInput => numPower;
        internal NumericUpDown PassesInput => numPasses;
        internal ComboBox LaserModeComboBox => cboLaserMode;
        internal NumericUpDown ZStepInput => numZStep;
        internal CheckBox AirAssistCheckBox => chkAirAssist;
        internal Label PowerSLabel => lblPowerS;
        internal Label DimensionsLabel => lblDimensions;
        internal Label BedWarningLabel => lblBedWarning;
        internal Button ApplyButton => btnApply;
        internal Button DisableButton => btnDisable;
        internal Button CancelButtonControl => btnCancel;
        internal Panel PreviewPanel => pnlPreview;

        public CutAroundForm(GrblCore core)
            : this(
                core?.GetCutAroundConfig() ?? new CutAroundConfig(),
                core?.LoadedFile?.GetEngravingCommandsSnapshot(),
                core?.BuildCutMachineOptions() ?? new CutMachineOptions(),
                core != null ? core.MachineBedWidth.Value : 300.0,
                core != null ? core.MachineBedHeight.Value : 300.0)
        {
            mCore = core;
        }

        public CutAroundForm(
            CutAroundConfig initialConfig,
            IReadOnlyList<GrblCommand> engravingCommands = null,
            CutMachineOptions machineOptions = null,
            double bedWidthMm = 300.0,
            double bedHeightMm = 300.0)
        {
            mMachineOptions = machineOptions != null ? machineOptions.Clone() : new CutMachineOptions();
            mMachineOptions.ValidateAndClamp();
            mBedWidthMm = bedWidthMm > 0 ? bedWidthMm : 300.0;
            mBedHeightMm = bedHeightMm > 0 ? bedHeightMm : 300.0;

            EngravingGeometry geom = (engravingCommands != null && engravingCommands.Count > 0)
                ? CutAroundGenerator.ComputeEngravingGeometry(engravingCommands, mMachineOptions.HardwarePwm)
                : null;

            if (geom != null && geom.IsValid)
            {
                mCachedEngravingGeometry = geom;
                mIsIllustrativePreview = false;
            }
            else
            {
                mIsIllustrativePreview = true;
                var sampleCmds = new List<GrblCommand>
                {
                    new GrblCommand("G0 X10 Y10"),
                    new GrblCommand("M3 S500"),
                    new GrblCommand("G1 X50 Y10 F1000"),
                    new GrblCommand("G1 X50 Y40"),
                    new GrblCommand("G1 X10 Y40"),
                    new GrblCommand("G1 X10 Y10"),
                    new GrblCommand("M5 S0")
                };
                mCachedEngravingGeometry = CutAroundGenerator.ComputeEngravingGeometry(sampleCmds, mMachineOptions.HardwarePwm);
            }

            CutAroundConfig cfg = initialConfig != null ? initialConfig.Clone() : new CutAroundConfig();
            cfg.ValidateAndClamp();
            ResultConfig = cfg.Clone();

            InitializeComponent();
            ApplyTheme();
            LoadConfigIntoControls(cfg);
            UpdateCornerRadiusState();
            UpdatePowerSLabel();
            UpdatePreviewSummary();
        }

        public static DialogResult CreateAndShowDialog(GrblCore core)
        {
            using (CutAroundForm form = new CutAroundForm(core))
            {
                DialogResult dr = form.ShowDialog(FormsHelper.MainForm);
                if (dr == DialogResult.OK && core != null && form.ResultConfig != null)
                {
                    if (form.ResultConfig.Enabled)
                        core.ApplyCutAround(form.ResultConfig);
                    else
                        core.DisableCutAround();

                    (FormsHelper.MainForm as MainForm)?.GrblPanel3D?.Invalidate();
                }
                return dr;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "✂ Cortar em Volta da Gravação — Configurações de Corte";
            this.Size = new Size(760, 520);
            this.MinimumSize = new Size(720, 490);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;

            // ==========================================
            // GROUP 1: Formato e Margem
            // ==========================================
            GroupBox gbGeometry = new GroupBox
            {
                Text = "Formato e Margem do Corte",
                Location = new Point(14, 12),
                Size = new Size(370, 145),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            Label lblShape = new Label { Text = "Formato:", Location = new Point(12, 28), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            cboShape = new ComboBox
            {
                Location = new Point(145, 25),
                Size = new Size(210, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                DrawMode = DrawMode.OwnerDrawFixed
            };
            cboShape.Items.Add("▭ Retângulo");
            cboShape.Items.Add("▢ Retângulo Arredondado");
            cboShape.Items.Add("⭕ Círculo");
            cboShape.Items.Add("⬭ Elipse");
            cboShape.Items.Add("✂ Contorno (Silhueta)");
            cboShape.DrawItem += DrawComboItem;
            cboShape.SelectedIndexChanged += (s, e) =>
            {
                if (mSuppressEvents) return;
                UpdateCornerRadiusState();
                UpdatePreviewSummary();
                pnlPreview.Invalidate();
            };

            Label lblMargin = new Label { Text = "Margem (mm):", Location = new Point(12, 62), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numMargin = new NumericUpDown
            {
                Location = new Point(145, 60),
                Size = new Size(95, 23),
                Minimum = 0m,
                Maximum = 200m,
                DecimalPlaces = 1,
                Increment = 0.5m,
                Value = 2.0m,
                Font = new Font("Segoe UI", 9f)
            };
            numMargin.ValueChanged += OnParameterChanged;

            lblCornerRadius = new Label { Text = "Raio dos Cantos (mm):", Location = new Point(12, 96), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numCornerRadius = new NumericUpDown
            {
                Location = new Point(145, 94),
                Size = new Size(95, 23),
                Minimum = 0m,
                Maximum = 100m,
                DecimalPlaces = 1,
                Increment = 0.5m,
                Value = 3.0m,
                Font = new Font("Segoe UI", 9f)
            };
            numCornerRadius.ValueChanged += OnParameterChanged;

            gbGeometry.Controls.AddRange(new Control[]
            {
                lblShape, cboShape,
                lblMargin, numMargin,
                lblCornerRadius, numCornerRadius
            });

            // ==========================================
            // GROUP 2: Parâmetros de Corte do Laser
            // ==========================================
            GroupBox gbLaser = new GroupBox
            {
                Text = "Parâmetros de Corte (Executado Após a Gravação)",
                Location = new Point(14, 165),
                Size = new Size(370, 240),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            Label lblSpeed = new Label { Text = "Velocidade (mm/min):", Location = new Point(12, 28), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numSpeed = new NumericUpDown
            {
                Location = new Point(155, 26),
                Size = new Size(95, 23),
                Minimum = 10m,
                Maximum = 20000m,
                Increment = 50m,
                Value = 300m,
                Font = new Font("Segoe UI", 9f)
            };
            numSpeed.ValueChanged += OnParameterChanged;

            Label lblPower = new Label { Text = "Força do Laser (%):", Location = new Point(12, 62), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numPower = new NumericUpDown
            {
                Location = new Point(155, 60),
                Size = new Size(70, 23),
                Minimum = 1m,
                Maximum = 100m,
                Increment = 5m,
                Value = 80m,
                Font = new Font("Segoe UI", 9f)
            };
            numPower.ValueChanged += (s, e) =>
            {
                if (mSuppressEvents) return;
                UpdatePowerSLabel();
                UpdatePreviewSummary();
            };

            lblPowerS = new Label
            {
                Location = new Point(232, 63),
                Size = new Size(128, 20),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248)
            };

            Label lblPasses = new Label { Text = "Número de Passadas:", Location = new Point(12, 96), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numPasses = new NumericUpDown
            {
                Location = new Point(155, 94),
                Size = new Size(70, 23),
                Minimum = 1m,
                Maximum = 50m,
                Increment = 1m,
                Value = 1m,
                Font = new Font("Segoe UI", 9f)
            };
            numPasses.ValueChanged += OnParameterChanged;

            Label lblMode = new Label { Text = "Modo do Laser:", Location = new Point(12, 130), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            cboLaserMode = new ComboBox
            {
                Location = new Point(155, 128),
                Size = new Size(200, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                DrawMode = DrawMode.OwnerDrawFixed
            };
            cboLaserMode.Items.Add("M3 (Potência Constante)");
            cboLaserMode.Items.Add("M4 (Potência Dinâmica)");
            cboLaserMode.DrawItem += DrawComboItem;

            Label lblZStep = new Label { Text = "Passo Z / passada (mm):", Location = new Point(12, 166), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numZStep = new NumericUpDown
            {
                Location = new Point(155, 164),
                Size = new Size(95, 23),
                Minimum = 0m,
                Maximum = 20m,
                DecimalPlaces = 2,
                Increment = 0.1m,
                Value = 0.0m,
                Font = new Font("Segoe UI", 9f)
            };
            numZStep.ValueChanged += OnParameterChanged;

            chkAirAssist = new CheckBox
            {
                Text = "Ativar Air Assist (M8 / M9)",
                Location = new Point(12, 200),
                Size = new Size(240, 24),
                Font = new Font("Segoe UI", 9f)
            };

            gbLaser.Controls.AddRange(new Control[]
            {
                lblSpeed, numSpeed,
                lblPower, numPower, lblPowerS,
                lblPasses, numPasses,
                lblMode, cboLaserMode,
                lblZStep, numZStep,
                chkAirAssist
            });

            // ==========================================
            // GROUP 3: Pré-visualização 2D do Corte
            // ==========================================
            GroupBox gbPreview = new GroupBox
            {
                Text = "Pré-visualização (Gravação → Corte em Volta)",
                Location = new Point(396, 12),
                Size = new Size(340, 393),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            pnlPreview = new Panel
            {
                Location = new Point(12, 24),
                Size = new Size(316, 290),
                BackColor = Color.FromArgb(18, 24, 34),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlPreview.Paint += PnlPreview_Paint;

            lblDimensions = new Label
            {
                Location = new Point(12, 322),
                Size = new Size(316, 36),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular)
            };

            lblBedWarning = new Label
            {
                Location = new Point(12, 360),
                Size = new Size(316, 24),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 222, 128)
            };

            gbPreview.Controls.AddRange(new Control[] { pnlPreview, lblDimensions, lblBedWarning });

            // ==========================================
            // Action Buttons
            // ==========================================
            btnApply = new Button
            {
                Text = "✓ Aplicar Corte",
                Location = new Point(396, 424),
                Size = new Size(125, 34),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += BtnApply_Click;

            btnDisable = new Button
            {
                Text = "Desabilitar Corte",
                Location = new Point(529, 424),
                Size = new Size(118, 34),
                BackColor = Color.FromArgb(185, 28, 28),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDisable.FlatAppearance.BorderSize = 0;
            btnDisable.Click += BtnDisable_Click;

            btnCancel = new Button
            {
                Text = "Cancelar",
                Location = new Point(655, 424),
                Size = new Size(81, 34),
                Font = new Font("Segoe UI", 9f),
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };

            Label lblOrderInfo = new Label
            {
                Text = "Ordem de execução: 1º Gravação da imagem  →  2º Corte em volta",
                Location = new Point(14, 431),
                Size = new Size(370, 22),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic)
            };

            this.AcceptButton = btnApply;
            this.CancelButton = btnCancel;

            this.Controls.AddRange(new Control[]
            {
                gbGeometry, gbLaser, gbPreview,
                lblOrderInfo, btnApply, btnDisable, btnCancel
            });
        }

        private void DrawComboItem(object sender, DrawItemEventArgs e)
        {
            if (!(sender is ComboBox cb) || e.Index < 0) return;
            bool isDark = ColorScheme.DarkScheme;
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color bg = isSelected
                ? (isDark ? Color.FromArgb(37, 99, 235) : SystemColors.Highlight)
                : (isDark ? Color.FromArgb(30, 41, 59) : SystemColors.Window);
            Color fg = isSelected
                ? Color.White
                : (isDark ? Color.FromArgb(248, 250, 252) : SystemColors.WindowText);

            using (SolidBrush bgBrush = new SolidBrush(bg))
            using (SolidBrush fgBrush = new SolidBrush(fg))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
                string text = cb.Items[e.Index].ToString();
                e.Graphics.DrawString(text, cb.Font, fgBrush, e.Bounds.X + 3, e.Bounds.Y + 3);
            }
        }

        private void OnParameterChanged(object sender, EventArgs e)
        {
            if (mSuppressEvents) return;
            UpdatePreviewSummary();
            pnlPreview.Invalidate();
        }

        private void LoadConfigIntoControls(CutAroundConfig cfg)
        {
            mSuppressEvents = true;
            try
            {
                int shapeIdx = (int)cfg.Shape;
                cboShape.SelectedIndex = (shapeIdx >= 0 && shapeIdx < cboShape.Items.Count) ? shapeIdx : 0;

                numMargin.Value = ClampNumeric(numMargin, (decimal)cfg.MarginMm);
                numCornerRadius.Value = ClampNumeric(numCornerRadius, (decimal)cfg.CornerRadiusMm);
                numSpeed.Value = ClampNumeric(numSpeed, cfg.SpeedMmMin);
                numPower.Value = ClampNumeric(numPower, cfg.PowerPercent);
                numPasses.Value = ClampNumeric(numPasses, cfg.Passes);

                string mode = cfg.GetNormalizedLaserMode();
                cboLaserMode.SelectedIndex = mode == "M4" ? 1 : 0;

                numZStep.Value = ClampNumeric(numZStep, (decimal)cfg.ZStepMm);
                chkAirAssist.Checked = cfg.AirAssist;
            }
            finally
            {
                mSuppressEvents = false;
            }
        }

        private static decimal ClampNumeric(NumericUpDown num, decimal val)
        {
            if (val < num.Minimum) return num.Minimum;
            if (val > num.Maximum) return num.Maximum;
            return val;
        }

        public CutAroundConfig BuildConfigFromControls(bool enabled)
        {
            var cfg = new CutAroundConfig
            {
                Enabled = enabled,
                Shape = (CutAroundShape)Math.Max(0, cboShape.SelectedIndex),
                MarginMm = (double)numMargin.Value,
                CornerRadiusMm = (double)numCornerRadius.Value,
                SpeedMmMin = (int)numSpeed.Value,
                PowerPercent = (int)numPower.Value,
                Passes = (int)numPasses.Value,
                LaserMode = cboLaserMode.SelectedIndex == 1 ? "M4" : "M3",
                ZStepMm = (double)numZStep.Value,
                AirAssist = chkAirAssist.Checked
            };
            cfg.ValidateAndClamp();
            return cfg;
        }

        private void UpdateCornerRadiusState()
        {
            bool isRounded = (CutAroundShape)Math.Max(0, cboShape.SelectedIndex) == CutAroundShape.RoundedRectangle;
            numCornerRadius.Enabled = isRounded;
            lblCornerRadius.Enabled = isRounded;
        }

        private void UpdatePowerSLabel()
        {
            int pct = (int)numPower.Value;
            if (mMachineOptions.IsSmoothie)
            {
                lblPowerS.Text = string.Format(CultureInfo.InvariantCulture, "(S = {0:0.00})", pct / 100.0);
            }
            else if (mMachineOptions.HardwarePwm)
            {
                int sVal = CutAroundGenerator.CalculateSValue(pct, mMachineOptions.MaxPWM);
                lblPowerS.Text = $"(S{sVal} / {mMachineOptions.MaxPWM})";
            }
            else
            {
                lblPowerS.Text = "(Relé sem PWM)";
            }
        }

        private CutAroundBuildResult ComputePreviewBuild(out bool isIllustrative)
        {
            isIllustrative = mIsIllustrativePreview;
            CutAroundConfig previewCfg = BuildConfigFromControls(true);
            return CutAroundGenerator.BuildCutAround(mCachedEngravingGeometry, previewCfg, mMachineOptions);
        }

        private void UpdatePreviewSummary()
        {
            var res = ComputePreviewBuild(out bool isIllustrative);
            mCachedPreviewBuild = res;
            if (res == null || !res.IsValid)
            {
                lblDimensions.Text = "Sem geometria válida para calcular o corte.";
                lblBedWarning.Text = string.Empty;
                return;
            }

            string prefix = isIllustrative ? "[Amostra] " : string.Empty;
            lblDimensions.Text = string.Format(
                CultureInfo.InvariantCulture,
                "{0}Gravação: {1:0.0} × {2:0.0} mm\nCorte externo: {3:0.0} × {4:0.0} mm",
                prefix,
                res.Engraving.Width,
                res.Engraving.Height,
                res.CutWidth,
                res.CutHeight);

            if (isIllustrative)
            {
                lblBedWarning.ForeColor = Color.FromArgb(148, 163, 184);
                lblBedWarning.Text = "Prévia ilustrativa (nenhuma arte carregada).";
                return;
            }

            bool outOfBed = res.CutMinX < 0 || res.CutMaxX > mBedWidthMm || res.CutMinY < 0 || res.CutMaxY > mBedHeightMm;
            if (outOfBed)
            {
                lblBedWarning.ForeColor = Color.FromArgb(248, 113, 113);
                lblBedWarning.Text = $"⚠ Corte ultrapassa os limites da mesa ({mBedWidthMm:0}×{mBedHeightMm:0} mm)!";
            }
            else
            {
                lblBedWarning.ForeColor = Color.FromArgb(74, 222, 128);
                lblBedWarning.Text = $"✓ Dentro dos limites da mesa ({mBedWidthMm:0}×{mBedHeightMm:0} mm)";
            }
        }

        private void PnlPreview_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int pw = pnlPreview.Width;
            int ph = pnlPreview.Height;

            using (Pen gridPen = new Pen(Color.FromArgb(35, 48, 65), 1f))
            {
                for (int x = 0; x < pw; x += 20) g.DrawLine(gridPen, x, 0, x, ph);
                for (int y = 0; y < ph; y += 20) g.DrawLine(gridPen, 0, y, pw, y);
            }

            if (mCachedPreviewBuild == null)
                UpdatePreviewSummary();

            var res = mCachedPreviewBuild;
            bool isIllustrative = mIsIllustrativePreview;
            if (res == null || !res.IsValid) return;

            double minX = Math.Min(res.Engraving.MinX, res.CutMinX);
            double maxX = Math.Max(res.Engraving.MaxX, res.CutMaxX);
            double minY = Math.Min(res.Engraving.MinY, res.CutMinY);
            double maxY = Math.Max(res.Engraving.MaxY, res.CutMaxY);

            double spanW = Math.Max(1.0, maxX - minX);
            double spanH = Math.Max(1.0, maxY - minY);

            float pad = 26f;
            float availW = Math.Max(20f, pw - pad * 2f);
            float availH = Math.Max(20f, ph - pad * 2f - 20f);
            float scale = (float)Math.Min(availW / spanW, availH / spanH);

            float drawW = (float)(spanW * scale);
            float drawH = (float)(spanH * scale);
            float offX = (pw - drawW) / 2f;
            float offY = (ph - 20f - drawH) / 2f;

            Func<double, double, PointF> toScreen = (wx, wy) =>
            {
                float sx = offX + (float)((wx - minX) * scale);
                float sy = offY + drawH - (float)((wy - minY) * scale);
                return new PointF(sx, sy);
            };

            // Draw engraving bounds
            PointF engBL = toScreen(res.Engraving.MinX, res.Engraving.MinY);
            PointF engTR = toScreen(res.Engraving.MaxX, res.Engraving.MaxY);
            RectangleF engRect = RectangleF.FromLTRB(
                Math.Min(engBL.X, engTR.X),
                Math.Min(engBL.Y, engTR.Y),
                Math.Max(engBL.X, engTR.X),
                Math.Max(engBL.Y, engTR.Y));

            using (SolidBrush engFill = new SolidBrush(Color.FromArgb(45, 56, 189, 248)))
            using (Pen engPen = new Pen(Color.FromArgb(56, 189, 248), 1.5f))
            {
                if (engRect.Width > 0.5f && engRect.Height > 0.5f)
                {
                    g.FillRectangle(engFill, engRect);
                    g.DrawRectangle(engPen, engRect.X, engRect.Y, engRect.Width, engRect.Height);
                }
            }

            // Draw sampled burning points inside engraving bounds for visual silhouette feedback
            var pts = res.Engraving.BurningPoints;
            if (pts != null && pts.Count > 1)
            {
                int step = Math.Max(1, pts.Count / 400);
                using (SolidBrush ptBrush = new SolidBrush(Color.FromArgb(140, 250, 204, 21)))
                {
                    for (int i = 0; i < pts.Count; i += step)
                    {
                        PointF sp = toScreen(pts[i].X, pts[i].Y);
                        g.FillRectangle(ptBrush, sp.X - 1f, sp.Y - 1f, 2f, 2f);
                    }
                }
            }

            // Draw cut polygon
            bool outOfBed = !isIllustrative && (res.CutMinX < 0 || res.CutMaxX > mBedWidthMm || res.CutMinY < 0 || res.CutMaxY > mBedHeightMm);
            Color cutColor = outOfBed ? Color.FromArgb(248, 113, 113) : Color.FromArgb(244, 114, 182);

            if (res.CutPolygon.Count >= 2)
            {
                PointF[] screenPoly = new PointF[res.CutPolygon.Count];
                for (int i = 0; i < res.CutPolygon.Count; i++)
                    screenPoly[i] = toScreen(res.CutPolygon[i].X, res.CutPolygon[i].Y);

                using (Pen cutPen = new Pen(cutColor, 2.2f) { DashStyle = DashStyle.Dash })
                {
                    g.DrawLines(cutPen, screenPoly);
                }
            }

            // Bottom legend
            using (Font legFont = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (SolidBrush legBrush = new SolidBrush(Color.FromArgb(226, 232, 240)))
            {
                string legend = $"1º Gravação ({res.Engraving.Width:0.0}×{res.Engraving.Height:0.0})  →  2º Corte ({res.CutWidth:0.0}×{res.CutHeight:0.0} mm)";
                SizeF sz = g.MeasureString(legend, legFont);
                g.DrawString(legend, legFont, legBrush, Math.Max(4f, (pw - sz.Width) / 2f), ph - 20f);
            }
        }

        internal void CommitApply()
        {
            BtnApply_Click(this, EventArgs.Empty);
        }

        internal void CommitDisable()
        {
            BtnDisable_Click(this, EventArgs.Empty);
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            ResultConfig = BuildConfigFromControls(true);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnDisable_Click(object sender, EventArgs e)
        {
            ResultConfig = BuildConfigFromControls(false);
            this.DialogResult = DialogResult.OK;
            this.Close();
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

            try
            {
                ThemeMgr.SetTheme(this, true);
            }
            catch { }
        }

        private void ApplyThemeRecursive(Control parent, bool isDark, Color backColor, Color foreColor, Color panelBack, Color inputBack, Color inputFore, Color borderCol)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is GroupBox gb)
                {
                    gb.ForeColor = isDark ? Color.FromArgb(56, 189, 248) : ColorScheme.FormForeColor;
                    ApplyThemeRecursive(gb, isDark, backColor, foreColor, panelBack, inputBack, inputFore, borderCol);
                }
                else if (c is Label lbl)
                {
                    if (lbl == lblPowerS)
                    {
                        lbl.ForeColor = isDark ? Color.FromArgb(56, 189, 248) : Color.FromArgb(0, 120, 215);
                    }
                    else if (lbl != lblBedWarning)
                    {
                        lbl.ForeColor = isDark ? Color.FromArgb(226, 232, 240) : Color.FromArgb(30, 30, 30);
                    }
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
                    if (btn != btnApply && btn != btnDisable)
                    {
                        btn.BackColor = isDark ? Color.FromArgb(51, 65, 85) : Color.FromArgb(235, 235, 235);
                        btn.ForeColor = isDark ? Color.White : Color.Black;
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = borderCol;
                    }
                }
                else if (c is Panel p && p != pnlPreview)
                {
                    ApplyThemeRecursive(p, isDark, backColor, foreColor, panelBack, inputBack, inputFore, borderCol);
                }
            }
        }
    }
}
