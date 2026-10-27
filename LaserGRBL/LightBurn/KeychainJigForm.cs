using LaserGRBL.Icons;
using LaserGRBL.UserControls;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace LaserGRBL.LightBurn
{
    public class KeychainJigForm : Form
    {
        private GrblCore mCore;

        // Shape & dimensions
        private ComboBox cboShape;
        private Label lblDiameter;
        private NumericUpDown numDiameter;
        private Label lblWidth;
        private NumericUpDown numWidth;
        private Label lblHeight;
        private NumericUpDown numHeight;
        private Label lblCornerRadius;
        private NumericUpDown numCornerRadius;
        private FlowLayoutPanel flpPresets;

        // Origin position
        private NumericUpDown numCenterX;
        private NumericUpDown numCenterY;
        private Button btnFromLaserPos;
        private Button btnCenterBed;

        // Grid / Multi-piece
        private NumericUpDown numGridCols;
        private NumericUpDown numGridRows;
        private NumericUpDown numSpacingX;
        private NumericUpDown numSpacingY;
        private Label lblGridSummary;

        // Laser parameters
        private NumericUpDown numSpeed;
        private NumericUpDown numPower;
        private CheckBox chkHole;
        private NumericUpDown numHoleDiameter;
        private CheckBox chkShowOnBed;

        // Actions
        private Button btnBurnCardboard;
        private Button btnDuplicateGrid;
        private Button btnCenterArtwork;
        private Button btnRemoveJig;
        private Button btnLoadGCode;
        private Button btnClose;
        private Panel pnlPreview;
        private Label lblArtStatus;

        public KeychainJigForm(GrblCore core)
        {
            mCore = core;
            InitializeComponent();
            ApplyTheme();
            LoadCurrentValues();
            UpdateShapeVisibility();
            UpdateGridSummary();
        }

        public static void CreateAndShowDialog(GrblCore core)
        {
            using (KeychainJigForm f = new KeychainJigForm(core))
            {
                f.ShowDialog(FormsHelper.MainForm);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Gabarito de Chaveiros & Peças (Ninho no Papelão e Grade Multi-Peças)";
            this.Size = new Size(740, 680);
            this.MinimumSize = new Size(700, 640);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;

            // ==========================================
            // GROUP 1: Formato e Dimensões da Peça
            // ==========================================
            GroupBox gbShape = new GroupBox()
            {
                Text = "Formato e Dimensões da Peça / Chaveiro",
                Location = new Point(14, 10),
                Size = new Size(390, 160),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            Label lblShapeTitle = new Label() { Text = "Formato:", Location = new Point(12, 24), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            cboShape = new ComboBox()
            {
                Location = new Point(135, 21),
                Size = new Size(240, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                DrawMode = DrawMode.OwnerDrawFixed
            };
            cboShape.Items.Add("⭕ Redondo (Círculo)");
            cboShape.Items.Add("⬛ Quadrado");
            cboShape.Items.Add("▭ Retangular");
            cboShape.SelectedIndex = (int)KeychainJigManager.Shape;
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
                KeychainJigManager.Shape = (JigShape)cboShape.SelectedIndex;
                UpdateShapeVisibility();
                SyncValues();
                pnlPreview.Invalidate();
            };

            // Diameter (Circle)
            lblDiameter = new Label() { Text = "Diâmetro (mm):", Location = new Point(12, 54), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numDiameter = new NumericUpDown()
            {
                Location = new Point(135, 52),
                Size = new Size(80, 23),
                Minimum = 5,
                Maximum = 500,
                DecimalPlaces = 1,
                Value = (decimal)KeychainJigManager.Diameter,
                Font = new Font("Segoe UI", 9f)
            };
            numDiameter.ValueChanged += (s, e) => { SyncValues(); pnlPreview.Invalidate(); };

            // Width (Square & Rectangle)
            lblWidth = new Label() { Text = "Largura (mm):", Location = new Point(12, 54), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numWidth = new NumericUpDown()
            {
                Location = new Point(135, 52),
                Size = new Size(80, 23),
                Minimum = 5,
                Maximum = 500,
                DecimalPlaces = 1,
                Value = (decimal)KeychainJigManager.Width,
                Font = new Font("Segoe UI", 9f)
            };
            numWidth.ValueChanged += (s, e) => { SyncValues(); pnlPreview.Invalidate(); };

            // Height (Rectangle)
            lblHeight = new Label() { Text = "Altura (mm):", Location = new Point(225, 54), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numHeight = new NumericUpDown()
            {
                Location = new Point(300, 52),
                Size = new Size(75, 23),
                Minimum = 5,
                Maximum = 500,
                DecimalPlaces = 1,
                Value = (decimal)KeychainJigManager.Height,
                Font = new Font("Segoe UI", 9f)
            };
            numHeight.ValueChanged += (s, e) => { SyncValues(); pnlPreview.Invalidate(); };

            // Corner Radius (Square & Rectangle)
            lblCornerRadius = new Label() { Text = "Cantos Arred. (mm):", Location = new Point(12, 84), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numCornerRadius = new NumericUpDown()
            {
                Location = new Point(135, 82),
                Size = new Size(80, 23),
                Minimum = 0,
                Maximum = 100,
                DecimalPlaces = 1,
                Value = (decimal)KeychainJigManager.CornerRadius,
                Font = new Font("Segoe UI", 9f)
            };
            numCornerRadius.ValueChanged += (s, e) => { SyncValues(); pnlPreview.Invalidate(); };

            // Presets row
            flpPresets = new FlowLayoutPanel()
            {
                Location = new Point(12, 115),
                Size = new Size(365, 34),
                WrapContents = false
            };
            int[] standardSizes = new int[] { 30, 35, 40, 45, 50, 60 };
            foreach (int size in standardSizes)
            {
                Button btnPreset = new Button()
                {
                    Text = $"{size}mm",
                    Size = new Size(54, 26),
                    Font = new Font("Segoe UI", 8.2f),
                    Margin = new Padding(2, 0, 2, 0)
                };
                int targetSize = size;
                btnPreset.Click += (s, e) =>
                {
                    if (KeychainJigManager.Shape == JigShape.Circle)
                        numDiameter.Value = targetSize;
                    else
                    {
                        numWidth.Value = targetSize;
                        if (KeychainJigManager.Shape == JigShape.Square)
                            numHeight.Value = targetSize;
                    }
                    SyncValues();
                    pnlPreview.Invalidate();
                };
                flpPresets.Controls.Add(btnPreset);
            }

            gbShape.Controls.AddRange(new Control[] {
                lblShapeTitle, cboShape,
                lblDiameter, numDiameter,
                lblWidth, numWidth,
                lblHeight, numHeight,
                lblCornerRadius, numCornerRadius,
                flpPresets
            });

            // ==========================================
            // GROUP 2: Grade / Multiplicação de Peças
            // ==========================================
            GroupBox gbGrid = new GroupBox()
            {
                Text = "Multiplicação em Grade (Fazer Vários de Uma Vez)",
                Location = new Point(14, 175),
                Size = new Size(390, 115),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            Label lblCols = new Label() { Text = "Colunas (X):", Location = new Point(12, 24), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numGridCols = new NumericUpDown()
            {
                Location = new Point(95, 22),
                Size = new Size(55, 23),
                Minimum = 1,
                Maximum = 30,
                Value = KeychainJigManager.GridCols,
                Font = new Font("Segoe UI", 9f)
            };
            numGridCols.ValueChanged += (s, e) => { SyncValues(); UpdateGridSummary(); pnlPreview.Invalidate(); };

            Label lblRows = new Label() { Text = "Linhas (Y):", Location = new Point(165, 24), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numGridRows = new NumericUpDown()
            {
                Location = new Point(235, 22),
                Size = new Size(55, 23),
                Minimum = 1,
                Maximum = 30,
                Value = KeychainJigManager.GridRows,
                Font = new Font("Segoe UI", 9f)
            };
            numGridRows.ValueChanged += (s, e) => { SyncValues(); UpdateGridSummary(); pnlPreview.Invalidate(); };

            Label lblSpX = new Label() { Text = "Espaç. X (mm):", Location = new Point(12, 54), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numSpacingX = new NumericUpDown()
            {
                Location = new Point(95, 52),
                Size = new Size(55, 23),
                Minimum = 0,
                Maximum = 100,
                DecimalPlaces = 1,
                Value = (decimal)KeychainJigManager.SpacingX,
                Font = new Font("Segoe UI", 9f)
            };
            numSpacingX.ValueChanged += (s, e) => { SyncValues(); UpdateGridSummary(); pnlPreview.Invalidate(); };

            Label lblSpY = new Label() { Text = "Espaç. Y (mm):", Location = new Point(165, 54), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numSpacingY = new NumericUpDown()
            {
                Location = new Point(235, 52),
                Size = new Size(55, 23),
                Minimum = 0,
                Maximum = 100,
                DecimalPlaces = 1,
                Value = (decimal)KeychainJigManager.SpacingY,
                Font = new Font("Segoe UI", 9f)
            };
            numSpacingY.ValueChanged += (s, e) => { SyncValues(); UpdateGridSummary(); pnlPreview.Invalidate(); };

            lblGridSummary = new Label()
            {
                Location = new Point(12, 84),
                Size = new Size(365, 22),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248)
            };

            gbGrid.Controls.AddRange(new Control[] {
                lblCols, numGridCols,
                lblRows, numGridRows,
                lblSpX, numSpacingX,
                lblSpY, numSpacingY,
                lblGridSummary
            });

            // ==========================================
            // GROUP 3: Posição Inicial na Mesa
            // ==========================================
            GroupBox gbPos = new GroupBox()
            {
                Text = "Origem da 1ª Peça na Mesa (X / Y)",
                Location = new Point(14, 295),
                Size = new Size(390, 95),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            Label lblX = new Label() { Text = "X Inicial (mm):", Location = new Point(12, 24), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numCenterX = new NumericUpDown()
            {
                Location = new Point(105, 22),
                Size = new Size(75, 23),
                Minimum = -1000,
                Maximum = 3000,
                DecimalPlaces = 1,
                Value = (decimal)KeychainJigManager.CenterX,
                Font = new Font("Segoe UI", 9f)
            };
            numCenterX.ValueChanged += (s, e) => { SyncValues(); pnlPreview.Invalidate(); };

            Label lblY = new Label() { Text = "Y Inicial (mm):", Location = new Point(200, 24), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numCenterY = new NumericUpDown()
            {
                Location = new Point(290, 22),
                Size = new Size(75, 23),
                Minimum = -1000,
                Maximum = 3000,
                DecimalPlaces = 1,
                Value = (decimal)KeychainJigManager.CenterY,
                Font = new Font("Segoe UI", 9f)
            };
            numCenterY.ValueChanged += (s, e) => { SyncValues(); pnlPreview.Invalidate(); };

            btnFromLaserPos = new Button()
            {
                Text = "📍 Usar Posição Atual do Laser",
                Location = new Point(12, 54),
                Size = new Size(185, 28),
                Font = new Font("Segoe UI", 8.2f)
            };
            btnFromLaserPos.Click += BtnFromLaserPos_Click;

            btnCenterBed = new Button()
            {
                Text = "🎯 Centro da Mesa",
                Location = new Point(205, 54),
                Size = new Size(165, 28),
                Font = new Font("Segoe UI", 8.2f)
            };
            btnCenterBed.Click += BtnCenterBed_Click;

            gbPos.Controls.AddRange(new Control[] { lblX, numCenterX, lblY, numCenterY, btnFromLaserPos, btnCenterBed });

            // ==========================================
            // GROUP 4: Parâmetros do Ninho (Papelão)
            // ==========================================
            GroupBox gbLaser = new GroupBox()
            {
                Text = "Parâmetros do Ninho (Marcação no Papelão)",
                Location = new Point(14, 395),
                Size = new Size(390, 130),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            Label lblSpd = new Label() { Text = "Velocidade:", Location = new Point(12, 24), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numSpeed = new NumericUpDown()
            {
                Location = new Point(90, 22),
                Size = new Size(70, 23),
                Minimum = 100,
                Maximum = 20000,
                Value = KeychainJigManager.Speed,
                Font = new Font("Segoe UI", 9f)
            };
            numSpeed.ValueChanged += (s, e) => SyncValues();
            Label lblMmMin = new Label() { Text = "mm/min", Location = new Point(164, 24), AutoSize = true, Font = new Font("Segoe UI", 8f) };

            Label lblPwr = new Label() { Text = "Potência:", Location = new Point(230, 24), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            numPower = new NumericUpDown()
            {
                Location = new Point(295, 22),
                Size = new Size(55, 23),
                Minimum = 1,
                Maximum = 100,
                Value = KeychainJigManager.PowerPercent,
                Font = new Font("Segoe UI", 9f)
            };
            numPower.ValueChanged += (s, e) => SyncValues();
            Label lblPct = new Label() { Text = "%", Location = new Point(353, 24), AutoSize = true, Font = new Font("Segoe UI", 9f) };

            chkHole = new CheckBox()
            {
                Text = "Marcar furo de argola (topo):",
                Location = new Point(12, 56),
                AutoSize = true,
                Checked = KeychainJigManager.HoleDiameter > 0.5f,
                Font = new Font("Segoe UI", 9f)
            };
            numHoleDiameter = new NumericUpDown()
            {
                Location = new Point(215, 55),
                Size = new Size(55, 23),
                Minimum = 1,
                Maximum = 20,
                DecimalPlaces = 1,
                Value = KeychainJigManager.HoleDiameter > 0.5f ? (decimal)KeychainJigManager.HoleDiameter : 3.0m,
                Enabled = chkHole.Checked,
                Font = new Font("Segoe UI", 9f)
            };
            chkHole.CheckedChanged += (s, e) =>
            {
                numHoleDiameter.Enabled = chkHole.Checked;
                SyncValues();
                pnlPreview.Invalidate();
            };
            numHoleDiameter.ValueChanged += (s, e) => { SyncValues(); pnlPreview.Invalidate(); };
            Label lblMmHole = new Label() { Text = "mm", Location = new Point(274, 57), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };

            chkShowOnBed = new CheckBox()
            {
                Text = "Visualizar gabaritos de todas as peças na mesa 3D",
                Location = new Point(12, 92),
                AutoSize = true,
                Checked = KeychainJigManager.Enabled,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            chkShowOnBed.CheckedChanged += (s, e) =>
            {
                KeychainJigManager.Enabled = chkShowOnBed.Checked;
                (FormsHelper.MainForm as MainForm)?.GrblPanel3D?.Invalidate();
            };

            Button btnQuickRemove = new Button()
            {
                Text = "❌ Remover",
                Location = new Point(300, 89),
                Size = new Size(80, 26),
                Font = new Font("Segoe UI", 7.8f, FontStyle.Bold),
                BackColor = Color.FromArgb(175, 45, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnQuickRemove.FlatAppearance.BorderSize = 0;
            btnQuickRemove.Click += (s, e) => RemoveJig();

            gbLaser.Controls.AddRange(new Control[] {
                lblSpd, numSpeed, lblMmMin,
                lblPwr, numPower, lblPct,
                chkHole, numHoleDiameter, lblMmHole,
                chkShowOnBed, btnQuickRemove
            });

            // ==========================================
            // PREVIEW PANEL (Right side)
            // ==========================================
            pnlPreview = new Panel()
            {
                Location = new Point(415, 18),
                Size = new Size(295, 305),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(28, 35, 45)
            };
            pnlPreview.Paint += PnlPreview_Paint;

            lblArtStatus = new Label()
            {
                Location = new Point(415, 330),
                Size = new Size(295, 58),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(148, 163, 184)
            };

            // ==========================================
            // ACTION BUTTONS
            // ==========================================
            btnBurnCardboard = new Button()
            {
                Text = "🔥 Gravar Gabarito no Papelão (15% @ 5000 mm/min)",
                Location = new Point(14, 535),
                Size = new Size(390, 42),
                BackColor = Color.FromArgb(230, 95, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnBurnCardboard.FlatAppearance.BorderSize = 0;
            btnBurnCardboard.Click += (s, e) =>
            {
                SyncValues();
                KeychainJigManager.ExecuteBurn(mCore, this);
            };

            btnDuplicateGrid = new Button()
            {
                Text = "📋 Duplicar Arte na Grade Completa",
                Location = new Point(415, 395),
                Size = new Size(295, 40),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(20, 130, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDuplicateGrid.FlatAppearance.BorderSize = 0;
            btnDuplicateGrid.Click += (s, e) =>
            {
                SyncValues();
                KeychainJigManager.DuplicateArtworkAcrossGrid(mCore, this);
                (FormsHelper.MainForm as MainForm)?.GrblPanel3D?.Invalidate();
                pnlPreview.Invalidate();
                UpdateArtStatus();
            };

            btnCenterArtwork = new Button()
            {
                Text = "🎯 Centralizar Arte na 1ª Peça",
                Location = new Point(415, 442),
                Size = new Size(295, 38),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(45, 95, 140),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCenterArtwork.FlatAppearance.BorderSize = 0;
            btnCenterArtwork.Click += (s, e) =>
            {
                SyncValues();
                KeychainJigManager.CenterArtwork(mCore);
                (FormsHelper.MainForm as MainForm)?.GrblPanel3D?.Invalidate();
                pnlPreview.Invalidate();
                UpdateArtStatus();
                MessageBox.Show(this, "A arte carregada foi centralizada perfeitamente na 1ª peça do gabarito!", "Centralizado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnRemoveJig = new Button()
            {
                Text = "🗑️ Remover Gabarito da Mesa",
                Location = new Point(415, 488),
                Size = new Size(295, 38),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(185, 45, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRemoveJig.FlatAppearance.BorderSize = 0;
            btnRemoveJig.Click += (s, e) => RemoveJig();

            btnLoadGCode = new Button()
            {
                Text = "💾 Carregar Gabarito como G-Code no LaserGRBL",
                Location = new Point(14, 584),
                Size = new Size(390, 36),
                Font = new Font("Segoe UI", 8.8f),
                Cursor = Cursors.Hand
            };
            btnLoadGCode.Click += (s, e) =>
            {
                SyncValues();
                KeychainJigManager.LoadJigAsJob(mCore);
                this.Close();
            };

            btnClose = new Button()
            {
                Text = "Fechar",
                Location = new Point(590, 584),
                Size = new Size(120, 36),
                DialogResult = DialogResult.OK,
                Font = new Font("Segoe UI", 9f)
            };

            this.Controls.AddRange(new Control[] {
                gbShape,
                gbGrid,
                gbPos,
                gbLaser,
                pnlPreview,
                lblArtStatus,
                btnBurnCardboard,
                btnDuplicateGrid,
                btnCenterArtwork,
                btnRemoveJig,
                btnLoadGCode,
                btnClose
            });

            UpdateArtStatus();
        }

        private void RemoveJig()
        {
            KeychainJigManager.Enabled = false;
            chkShowOnBed.Checked = false;
            (FormsHelper.MainForm as MainForm)?.GrblPanel3D?.Invalidate();
            MessageBox.Show(this, "O gabarito foi desativado e removido da mesa de trabalho.", "Gabarito Removido", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateShapeVisibility()
        {
            var shape = (JigShape)cboShape.SelectedIndex;
            bool isCircle = shape == JigShape.Circle;
            bool isSquare = shape == JigShape.Square;
            bool isRect = shape == JigShape.Rectangle;

            lblDiameter.Visible = isCircle;
            numDiameter.Visible = isCircle;

            lblWidth.Visible = isSquare || isRect;
            numWidth.Visible = isSquare || isRect;
            lblWidth.Text = isSquare ? "Lado (mm):" : "Largura (mm):";

            lblHeight.Visible = isRect;
            numHeight.Visible = isRect;

            lblCornerRadius.Visible = isSquare || isRect;
            numCornerRadius.Visible = isSquare || isRect;

            UpdateGridSummary();
        }

        private void UpdateGridSummary()
        {
            int cols = (int)numGridCols.Value;
            int rows = (int)numGridRows.Value;
            int total = cols * rows;

            float w = KeychainJigManager.EffectiveWidth;
            float h = KeychainJigManager.EffectiveHeight;
            float spX = (float)numSpacingX.Value;
            float spY = (float)numSpacingY.Value;

            float totalW = cols * w + (cols - 1) * spX;
            float totalH = rows * h + (rows - 1) * spY;

            lblGridSummary.Text = $"Total: {total} peças ({cols}×{rows}) | Área: {totalW:0.0} × {totalH:0.0} mm";
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
                if (c is GroupBox gb)
                {
                    gb.ForeColor = isDark ? Color.FromArgb(56, 189, 248) : ColorScheme.FormForeColor;
                    ApplyThemeRecursive(gb, isDark, backColor, foreColor, panelBack, inputBack, inputFore, borderCol);
                }
                else if (c is Label lbl)
                {
                    if (lbl == lblGridSummary)
                    {
                        lbl.ForeColor = isDark ? Color.FromArgb(56, 189, 248) : Color.FromArgb(0, 120, 215);
                    }
                    else if (lbl != lblArtStatus)
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
                    if (btn == btnBurnCardboard || btn == btnDuplicateGrid || btn == btnCenterArtwork || btn == btnRemoveJig || btn.Text.Contains("Remover"))
                    {
                        // Keep specific action button colors
                    }
                    else
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

        private void LoadCurrentValues()
        {
            cboShape.SelectedIndex = (int)KeychainJigManager.Shape;
            numDiameter.Value = (decimal)KeychainJigManager.Diameter;
            numWidth.Value = (decimal)KeychainJigManager.Width;
            numHeight.Value = (decimal)KeychainJigManager.Height;
            numCornerRadius.Value = (decimal)KeychainJigManager.CornerRadius;

            numGridCols.Value = KeychainJigManager.GridCols;
            numGridRows.Value = KeychainJigManager.GridRows;
            numSpacingX.Value = (decimal)KeychainJigManager.SpacingX;
            numSpacingY.Value = (decimal)KeychainJigManager.SpacingY;

            numCenterX.Value = (decimal)KeychainJigManager.CenterX;
            numCenterY.Value = (decimal)KeychainJigManager.CenterY;
            numSpeed.Value = KeychainJigManager.Speed;
            numPower.Value = KeychainJigManager.PowerPercent;
            chkHole.Checked = KeychainJigManager.HoleDiameter > 0.5f;
            numHoleDiameter.Value = KeychainJigManager.HoleDiameter > 0.5f ? (decimal)KeychainJigManager.HoleDiameter : 3.0m;
            chkShowOnBed.Checked = KeychainJigManager.Enabled;
        }

        private void SyncValues()
        {
            KeychainJigManager.Shape = (JigShape)cboShape.SelectedIndex;
            KeychainJigManager.Diameter = (float)numDiameter.Value;
            KeychainJigManager.Width = (float)numWidth.Value;
            KeychainJigManager.Height = (float)numHeight.Value;
            KeychainJigManager.CornerRadius = (float)numCornerRadius.Value;

            KeychainJigManager.GridCols = (int)numGridCols.Value;
            KeychainJigManager.GridRows = (int)numGridRows.Value;
            KeychainJigManager.SpacingX = (float)numSpacingX.Value;
            KeychainJigManager.SpacingY = (float)numSpacingY.Value;

            KeychainJigManager.CenterX = (float)numCenterX.Value;
            KeychainJigManager.CenterY = (float)numCenterY.Value;
            KeychainJigManager.Speed = (int)numSpeed.Value;
            KeychainJigManager.PowerPercent = (int)numPower.Value;
            KeychainJigManager.HoleDiameter = chkHole.Checked ? (float)numHoleDiameter.Value : 0.0f;
            KeychainJigManager.Enabled = chkShowOnBed.Checked;

            (FormsHelper.MainForm as MainForm)?.GrblPanel3D?.Invalidate();
            UpdateArtStatus();
        }

        private void BtnFromLaserPos_Click(object sender, EventArgs e)
        {
            if (mCore != null && mCore.IsConnected)
            {
                numCenterX.Value = (decimal)mCore.WorkPosition.X;
                numCenterY.Value = (decimal)mCore.WorkPosition.Y;
                SyncValues();
                pnlPreview.Invalidate();
            }
            else
            {
                MessageBox.Show(this, "Nenhuma posição do laser detectada no momento (máquina desconectada).", "Posição do Laser", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnCenterBed_Click(object sender, EventArgs e)
        {
            float bedW = mCore != null ? mCore.MachineBedWidth.Value : 300f;
            float bedH = mCore != null ? mCore.MachineBedHeight.Value : 300f;

            numCenterX.Value = (decimal)(bedW / 2f);
            numCenterY.Value = (decimal)(bedH / 2f);
            SyncValues();
            pnlPreview.Invalidate();
        }

        private void UpdateArtStatus()
        {
            if (mCore?.LoadedFile?.Range?.DrawingRange?.ValidRange == true)
            {
                var dr = mCore.LoadedFile.Range.DrawingRange;
                float artW = (float)(dr.X.Max - dr.X.Min);
                float artH = (float)(dr.Y.Max - dr.Y.Min);
                float jigW = KeychainJigManager.EffectiveWidth;
                float jigH = KeychainJigManager.EffectiveHeight;

                if (artW > jigW || artH > jigH)
                {
                    lblArtStatus.ForeColor = Color.FromArgb(248, 113, 113); // High-contrast coral red
                    lblArtStatus.Text = $"⚠️ Arte ({artW:0.0}×{artH:0.0} mm) é maior que a peça ({jigW:0.0}×{jigH:0.0} mm)! Reduza no importador para caber.";
                }
                else
                {
                    lblArtStatus.ForeColor = Color.FromArgb(74, 222, 128); // High-contrast mint green
                    lblArtStatus.Text = $"✓ Arte ({artW:0.0}×{artH:0.0} mm) cabe na peça de {jigW:0.0}×{jigH:0.0} mm.\nClique em 'Duplicar Arte' para gerar as {KeychainJigManager.TotalItems} peças.";
                }
            }
            else
            {
                lblArtStatus.ForeColor = Color.FromArgb(148, 163, 184); // Readable light slate in dark mode
                lblArtStatus.Text = "Nenhuma arte carregada no LaserGRBL.\nAbra uma imagem ou arquivo G-Code primeiro.";
            }
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

        private void PnlPreview_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int pw = pnlPreview.Width;
            int ph = pnlPreview.Height;

            // Background grid
            using (Pen gridPen = new Pen(Color.FromArgb(40, 50, 65), 1f))
            {
                for (int x = 0; x < pw; x += 20) g.DrawLine(gridPen, x, 0, x, ph);
                for (int y = 0; y < ph; y += 20) g.DrawLine(gridPen, 0, y, pw, y);
            }

            int cols = Math.Max(1, KeychainJigManager.GridCols);
            int rows = Math.Max(1, KeychainJigManager.GridRows);
            float wMm = KeychainJigManager.EffectiveWidth;
            float hMm = KeychainJigManager.EffectiveHeight;
            float spX = KeychainJigManager.SpacingX;
            float spY = KeychainJigManager.SpacingY;
            float crMm = KeychainJigManager.Shape == JigShape.Circle ? 0 : Math.Min(KeychainJigManager.CornerRadius, Math.Min(wMm, hMm) / 2.0f);

            float totalAreaW = cols * wMm + (cols - 1) * spX;
            float totalAreaH = rows * hMm + (rows - 1) * spY;

            float pad = 24f;
            float availW = pw - pad * 2f;
            float availH = ph - pad * 2f - 20f; // leave room for badge

            float scale = Math.Min(availW / totalAreaW, availH / totalAreaH);
            float gridScreenW = totalAreaW * scale;
            float gridScreenH = totalAreaH * scale;

            float startX = (pw - gridScreenW) / 2f;
            float startY = (ph - 20f - gridScreenH) / 2f;

            using (SolidBrush fillBrush = new SolidBrush(Color.FromArgb(45, 255, 180, 0)))
            using (Pen borderPen = new Pen(Color.FromArgb(255, 200, 40), 1.8f))
            using (Pen crossPen = new Pen(Color.FromArgb(180, 255, 220, 80), 1.2f))
            using (SolidBrush holeBrush = new SolidBrush(Color.FromArgb(20, 25, 35)))
            using (Pen holePen = new Pen(Color.FromArgb(255, 220, 100), 1.2f))
            using (Font idxFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (SolidBrush idxBrush = new SolidBrush(Color.FromArgb(160, 255, 255, 255)))
            {
                int itemIdx = 1;
                for (int r = rows - 1; r >= 0; r--) // draw bottom to top as CNC coordinates
                {
                    for (int c = 0; c < cols; c++)
                    {
                        float itemX = startX + (c * (wMm + spX) * scale);
                        float itemY = startY + ((rows - 1 - r) * (hMm + spY) * scale);
                        float itemW = wMm * scale;
                        float itemH = hMm * scale;
                        float itemCr = crMm * scale;

                        RectangleF itemRect = new RectangleF(itemX, itemY, itemW, itemH);

                        if (KeychainJigManager.Shape == JigShape.Circle)
                        {
                            g.FillEllipse(fillBrush, itemRect);
                            g.DrawEllipse(borderPen, itemRect);
                        }
                        else
                        {
                            using (GraphicsPath path = CreateRoundedPath(itemRect, itemCr))
                            {
                                g.FillPath(fillBrush, path);
                                g.DrawPath(borderPen, path);
                            }
                        }

                        // Crosshair
                        float cx = itemX + itemW / 2f;
                        float cy = itemY + itemH / 2f;
                        float arm = Math.Min(8f, Math.Min(itemW, itemH) * 0.3f);
                        g.DrawLine(crossPen, cx - arm, cy, cx + arm, cy);
                        g.DrawLine(crossPen, cx, cy - arm, cx, cy + arm);

                        // Hole if enabled
                        if (chkHole.Checked && numHoleDiameter.Value > 0)
                        {
                            float hrMm = (float)numHoleDiameter.Value / 2.0f;
                            float hrPx = Math.Max(2f, hrMm * scale);
                            float holeMarginPx = Math.Max(2f, KeychainJigManager.HoleMargin * scale);
                            float hcy = itemY + holeMarginPx + hrPx;
                            RectangleF hRect = new RectangleF(cx - hrPx, hcy - hrPx, hrPx * 2f, hrPx * 2f);
                            g.FillEllipse(holeBrush, hRect);
                            g.DrawEllipse(holePen, hRect);
                        }

                        // Item index number if grid > 1
                        if (cols * rows > 1 && itemW > 20 && itemH > 20)
                        {
                            g.DrawString($"#{itemIdx}", idxFont, idxBrush, itemX + 2, itemY + 2);
                        }

                        itemIdx++;
                    }
                }
            }

            // Summary badge at the bottom of preview
            using (Font dimFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (SolidBrush dimBrush = new SolidBrush(Color.White))
            {
                string shapeName = KeychainJigManager.Shape == JigShape.Circle ? $"Ø {wMm:0.0} mm" :
                                   KeychainJigManager.Shape == JigShape.Square ? $"{wMm:0.0}×{wMm:0.0} mm" :
                                   $"{wMm:0.0}×{hMm:0.0} mm";

                string txt = cols * rows > 1 ? $"Grade: {cols * rows} peças ({cols}×{rows}) | {shapeName}" : $"{shapeName}";
                SizeF sz = g.MeasureString(txt, dimFont);
                g.DrawString(txt, dimFont, dimBrush, (pw - sz.Width) / 2f, ph - 22);
            }
        }
    }
}
