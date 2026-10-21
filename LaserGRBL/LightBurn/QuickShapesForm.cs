using LaserGRBL.Icons;
using LaserGRBL.UserControls;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace LaserGRBL.LightBurn
{
    public class QuickShapesForm : Form
    {
        private GrblCore mCore;
        private ComboBox cbShapeType;
        private NumericUpDown numW;
        private NumericUpDown numH;
        private NumericUpDown numRadius;
        private NumericUpDown numDiameter;
        private NumericUpDown numInnerDiameter;
        private NumericUpDown numSides;
        private TextBox txtContent;
        private NumericUpDown numGridCols;
        private NumericUpDown numGridRows;
        private NumericUpDown numGridSpacing;
        private NumericUpDown numSpeed;
        private NumericUpDown numPower;
        private NumericUpDown numPasses;
        private NumericUpDown numZStep;
        private CheckBox chkAirAssist;
        private Panel pnlPreview;
        private Button btnGenerate;
        private Button btnSave;
        private Button btnCancel;
        private GroupBox gbShapeParams;
        private Label lblW, lblH, lblCorner, lblDiam, lblInnerDiam, lblSides, lblText;

        public QuickShapesForm(GrblCore core)
        {
            mCore = core;
            InitializeComponent();
            ApplyMaterialTheme();
            UpdateVisibleParams();
        }

        public static void CreateAndShowDialog(GrblCore core)
        {
            using (QuickShapesForm f = new QuickShapesForm(core))
            {
                f.ShowDialog(FormsHelper.MainForm);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "LightBurn - Gerador Rápido de Formas Vetoriais (Quick Shapes & Text)";
            this.Size = new Size(740, 530);
            this.MinimumSize = new Size(700, 490);
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowIcon = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // GroupBox Shape
            gbShapeParams = new GroupBox()
            {
                Text = "Forma Geométrica e Dimensões",
                Location = new Point(16, 12),
                Size = new Size(390, 215),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            int y = 24;
            Label lblType = new Label() { Text = "Tipo de Forma:", Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            cbShapeType = new ComboBox() { Location = new Point(140, y - 3), Width = 230, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8.5f) };
            cbShapeType.Items.AddRange(new object[] { "Retângulo / Quadrado", "Círculo / Anel", "Polígono Regular", "Estrela", "Texto Rápido" });
            cbShapeType.SelectedIndex = 0;
            cbShapeType.SelectedIndexChanged += (s, e) => { UpdateVisibleParams(); pnlPreview.Invalidate(); };
            gbShapeParams.Controls.AddRange(new Control[] { lblType, cbShapeType });

            y += 32;
            lblW = new Label() { Text = "Largura (X mm):", Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numW = new NumericUpDown() { Location = new Point(140, y - 3), Width = 80, Minimum = 1, Maximum = 1000, Value = 50 };
            numW.ValueChanged += (s, e) => pnlPreview.Invalidate();

            lblH = new Label() { Text = "Altura (Y mm):", Location = new Point(14, y + 32), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numH = new NumericUpDown() { Location = new Point(140, y + 29), Width = 80, Minimum = 1, Maximum = 1000, Value = 30 };
            numH.ValueChanged += (s, e) => pnlPreview.Invalidate();

            lblCorner = new Label() { Text = "Canto Arredondado (R):", Location = new Point(14, y + 64), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numRadius = new NumericUpDown() { Location = new Point(160, y + 61), Width = 60, Minimum = 0, Maximum = 100, Value = 0 };
            numRadius.ValueChanged += (s, e) => pnlPreview.Invalidate();

            lblDiam = new Label() { Text = "Diâmetro Externo (mm):", Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f), Visible = false };
            numDiameter = new NumericUpDown() { Location = new Point(160, y - 3), Width = 80, Minimum = 1, Maximum = 1000, Value = 40, Visible = false };
            numDiameter.ValueChanged += (s, e) => pnlPreview.Invalidate();

            lblInnerDiam = new Label() { Text = "Furo Interno (mm):", Location = new Point(14, y + 32), AutoSize = true, Font = new Font("Segoe UI", 8.5f), Visible = false };
            numInnerDiameter = new NumericUpDown() { Location = new Point(160, y + 29), Width = 80, Minimum = 0, Maximum = 1000, Value = 0, Visible = false };
            numInnerDiameter.ValueChanged += (s, e) => pnlPreview.Invalidate();

            lblSides = new Label() { Text = "Lados / Pontas:", Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f), Visible = false };
            numSides = new NumericUpDown() { Location = new Point(140, y - 3), Width = 70, Minimum = 3, Maximum = 20, Value = 6, Visible = false };
            numSides.ValueChanged += (s, e) => pnlPreview.Invalidate();

            lblText = new Label() { Text = "Texto a Gravar:", Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f), Visible = false };
            txtContent = new TextBox() { Text = "LASER", Location = new Point(140, y - 3), Width = 230, Visible = false };
            txtContent.TextChanged += (s, e) => pnlPreview.Invalidate();

            gbShapeParams.Controls.AddRange(new Control[] { lblW, numW, lblH, numH, lblCorner, numRadius, lblDiam, numDiameter, lblInnerDiam, numInnerDiameter, lblSides, numSides, lblText, txtContent });

            // GroupBox Array / Grid
            GroupBox gbArray = new GroupBox()
            {
                Text = "Matriz / Repetição (Array Grid)",
                Location = new Point(16, 235),
                Size = new Size(390, 75),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            Label lblGrid = new Label() { Text = "Cols x Linhas:", Location = new Point(14, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numGridCols = new NumericUpDown() { Location = new Point(110, 22), Width = 55, Minimum = 1, Maximum = 20, Value = 1 };
            numGridRows = new NumericUpDown() { Location = new Point(175, 22), Width = 55, Minimum = 1, Maximum = 20, Value = 1 };
            Label lblGridSpacing = new Label() { Text = "Espaço (mm):", Location = new Point(245, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numGridSpacing = new NumericUpDown() { Location = new Point(325, 22), Width = 50, Minimum = 1, Maximum = 50, Value = 5 };

            numGridCols.ValueChanged += (s, e) => pnlPreview.Invalidate();
            numGridRows.ValueChanged += (s, e) => pnlPreview.Invalidate();
            numGridSpacing.ValueChanged += (s, e) => pnlPreview.Invalidate();

            gbArray.Controls.AddRange(new Control[] { lblGrid, numGridCols, numGridRows, lblGridSpacing, numGridSpacing });

            // GroupBox Laser
            GroupBox gbLaser = new GroupBox()
            {
                Text = "Parâmetros de Corte / Gravação",
                Location = new Point(16, 318),
                Size = new Size(390, 95),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            Label lblSpeed = new Label() { Text = "Velocidade:", Location = new Point(14, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numSpeed = new NumericUpDown() { Location = new Point(90, 22), Width = 75, Minimum = 100, Maximum = 20000, Value = 800, Increment = 100 };
            Label lblSpeedUnit = new Label() { Text = "mm/min", Location = new Point(170, 25), AutoSize = true, Font = new Font("Segoe UI", 8f) };

            Label lblPower = new Label() { Text = "Potência:", Location = new Point(235, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numPower = new NumericUpDown() { Location = new Point(300, 22), Width = 60, Minimum = 1, Maximum = 100, Value = 85 };
            Label lblPowerUnit = new Label() { Text = "%", Location = new Point(365, 25), AutoSize = true, Font = new Font("Segoe UI", 8f) };

            Label lblPass = new Label() { Text = "Passes:", Location = new Point(14, 60), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numPasses = new NumericUpDown() { Location = new Point(90, 57), Width = 60, Minimum = 1, Maximum = 20, Value = 1 };

            Label lblZ = new Label() { Text = "Passo Z:", Location = new Point(165, 60), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numZStep = new NumericUpDown() { Location = new Point(225, 57), Width = 60, Minimum = -5, Maximum = 5, Value = 0, DecimalPlaces = 2, Increment = 0.1M };

            chkAirAssist = new CheckBox() { Text = "Air Assist", Location = new Point(300, 58), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 8.5f) };

            gbLaser.Controls.AddRange(new Control[] { lblSpeed, numSpeed, lblSpeedUnit, lblPower, numPower, lblPowerUnit, lblPass, numPasses, lblZ, numZStep, chkAirAssist });

            // Preview Panel (right side)
            pnlPreview = new Panel()
            {
                Location = new Point(420, 20),
                Size = new Size(290, 393),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlPreview.Paint += PnlPreview_Paint;

            // Buttons
            btnGenerate = new Button()
            {
                Text = "✓ GERAR E CARREGAR NO LASERGRBL",
                Location = new Point(16, 428),
                Size = new Size(300, 44),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnGenerate.Click += BtnGenerate_Click;

            btnSave = new Button()
            {
                Text = "💾 Salvar G-Code...",
                Location = new Point(325, 428),
                Size = new Size(160, 44),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button()
            {
                Text = "Fechar",
                Location = new Point(590, 428),
                Size = new Size(120, 44),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { gbShapeParams, gbArray, gbLaser, pnlPreview, btnGenerate, btnSave, btnCancel });
        }

        private void ApplyMaterialTheme()
        {
            this.BackColor = ColorScheme.FormBackColor;
            this.ForeColor = ColorScheme.FormForeColor;
            ThemeMgr.SetTheme(this, true);

            pnlPreview.BackColor = ColorScheme.PreviewBackColor;

            btnGenerate.BackColor = ColorScheme.PressedButtons;
            btnGenerate.ForeColor = Color.White;
            btnGenerate.FlatAppearance.BorderColor = ColorScheme.PressedButtons;

            btnSave.BackColor = ColorScheme.FormButtonsColor;
            btnSave.ForeColor = ColorScheme.FormForeColor;
            btnSave.FlatAppearance.BorderColor = ColorScheme.ControlsBorder;

            btnCancel.BackColor = ColorScheme.FormButtonsColor;
            btnCancel.ForeColor = ColorScheme.FormForeColor;
            btnCancel.FlatAppearance.BorderColor = ColorScheme.ControlsBorder;

            cbShapeType.BackColor = ColorScheme.LogBackColor;
            cbShapeType.ForeColor = ColorScheme.FormForeColor;
            txtContent.BackColor = ColorScheme.LogBackColor;
            txtContent.ForeColor = ColorScheme.FormForeColor;

            numW.BackColor = ColorScheme.LogBackColor;
            numW.ForeColor = ColorScheme.FormForeColor;
            numH.BackColor = ColorScheme.LogBackColor;
            numH.ForeColor = ColorScheme.FormForeColor;
            numRadius.BackColor = ColorScheme.LogBackColor;
            numRadius.ForeColor = ColorScheme.FormForeColor;
            numDiameter.BackColor = ColorScheme.LogBackColor;
            numDiameter.ForeColor = ColorScheme.FormForeColor;
            numInnerDiameter.BackColor = ColorScheme.LogBackColor;
            numInnerDiameter.ForeColor = ColorScheme.FormForeColor;
            numSides.BackColor = ColorScheme.LogBackColor;
            numSides.ForeColor = ColorScheme.FormForeColor;
            numGridCols.BackColor = ColorScheme.LogBackColor;
            numGridCols.ForeColor = ColorScheme.FormForeColor;
            numGridRows.BackColor = ColorScheme.LogBackColor;
            numGridRows.ForeColor = ColorScheme.FormForeColor;
            numGridSpacing.BackColor = ColorScheme.LogBackColor;
            numGridSpacing.ForeColor = ColorScheme.FormForeColor;
            numSpeed.BackColor = ColorScheme.LogBackColor;
            numSpeed.ForeColor = ColorScheme.FormForeColor;
            numPower.BackColor = ColorScheme.LogBackColor;
            numPower.ForeColor = ColorScheme.FormForeColor;
            numPasses.BackColor = ColorScheme.LogBackColor;
            numPasses.ForeColor = ColorScheme.FormForeColor;
            numZStep.BackColor = ColorScheme.LogBackColor;
            numZStep.ForeColor = ColorScheme.FormForeColor;
        }

        private void UpdateVisibleParams()
        {
            ShapeType type = (ShapeType)cbShapeType.SelectedIndex;

            lblW.Visible = numW.Visible = (type == ShapeType.Rectangle || type == ShapeType.Polygon || type == ShapeType.Star);
            lblH.Visible = numH.Visible = (type == ShapeType.Rectangle);
            lblCorner.Visible = numRadius.Visible = (type == ShapeType.Rectangle);

            lblDiam.Visible = numDiameter.Visible = (type == ShapeType.Circle);
            lblInnerDiam.Visible = numInnerDiameter.Visible = (type == ShapeType.Circle);

            lblSides.Visible = numSides.Visible = (type == ShapeType.Polygon || type == ShapeType.Star);
            lblText.Visible = txtContent.Visible = (type == ShapeType.Text);

            if (type == ShapeType.Polygon || type == ShapeType.Star)
            {
                lblW.Text = "Raio (mm):";
            }
            else
            {
                lblW.Text = "Largura (X mm):";
            }
        }

        private QuickShapeConfig BuildConfig()
        {
            int maxPwm = 1000;
            try
            {
                maxPwm = (int)GrblCore.Configuration.MaxPWM;
            }
            catch { }

            return new QuickShapeConfig()
            {
                Type = (ShapeType)cbShapeType.SelectedIndex,
                Width = (double)numW.Value,
                Height = (double)numH.Value,
                CornerRadius = (double)numRadius.Value,
                Diameter = (double)numDiameter.Value,
                InnerDiameter = (double)numInnerDiameter.Value,
                Sides = (int)numSides.Value,
                Radius = (double)numW.Value,
                InnerRadius = (double)numW.Value * 0.45,
                TextString = txtContent.Text.Trim(),
                TextSize = (double)numH.Value,
                GridCols = (int)numGridCols.Value,
                GridRows = (int)numGridRows.Value,
                GridSpacingX = (double)numGridSpacing.Value,
                GridSpacingY = (double)numGridSpacing.Value,
                Speed = (int)numSpeed.Value,
                PowerPct = (int)numPower.Value,
                Passes = (int)numPasses.Value,
                ZStep = (double)numZStep.Value,
                AirAssist = chkAirAssist.Checked,
                MaxPwm = maxPwm
            };
        }

        private void PnlPreview_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var cfg = BuildConfig();
            int cols = cfg.GridCols;
            int rows = cfg.GridRows;

            float pad = 25f;
            float availW = pnlPreview.Width - pad * 2;
            float availH = pnlPreview.Height - pad * 2;

            float cellW = availW / cols;
            float cellH = availH / rows;

            using (Pen outlinePen = new Pen(ColorScheme.PressedButtons, 2f))
            using (SolidBrush fillBrush = new SolidBrush(Color.FromArgb(35, ColorScheme.PressedButtons)))
            using (Font f = new Font("Segoe UI", 8f))
            using (Brush textBrush = new SolidBrush(ColorScheme.FormForeColor))
            {
                e.Graphics.DrawString($"Prévia: {cfg.Type} ({cols}x{rows})", new Font("Segoe UI", 8.5f, FontStyle.Bold), textBrush, 10, 10);

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        float x = pad + c * cellW;
                        float y = pad + (rows - 1 - r) * cellH; // bottom up
                        float w = cellW * 0.85f;
                        float h = cellH * 0.85f;

                        RectangleF rect = new RectangleF(x, y, w, h);

                        switch (cfg.Type)
                        {
                            case ShapeType.Rectangle:
                                if (cfg.CornerRadius > 0)
                                {
                                    using (GraphicsPath gp = Tools.Graph.RoundedRect(new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height), (int)cfg.CornerRadius))
                                    {
                                        e.Graphics.FillPath(fillBrush, gp);
                                        e.Graphics.DrawPath(outlinePen, gp);
                                    }
                                }
                                else
                                {
                                    e.Graphics.FillRectangle(fillBrush, rect);
                                    e.Graphics.DrawRectangle(outlinePen, rect.X, rect.Y, rect.Width, rect.Height);
                                }
                                break;
                            case ShapeType.Circle:
                                float d = Math.Min(w, h);
                                e.Graphics.FillEllipse(fillBrush, x, y, d, d);
                                e.Graphics.DrawEllipse(outlinePen, x, y, d, d);
                                if (cfg.InnerDiameter > 0 && cfg.InnerDiameter < cfg.Diameter)
                                {
                                    float id = d * (float)(cfg.InnerDiameter / cfg.Diameter);
                                    float offset = (d - id) / 2f;
                                    e.Graphics.DrawEllipse(outlinePen, x + offset, y + offset, id, id);
                                }
                                break;
                            case ShapeType.Polygon:
                            case ShapeType.Star:
                                float polyR = Math.Min(w, h) / 2f;
                                PointF center = new PointF(x + polyR, y + polyR);
                                int ptsCount = cfg.Type == ShapeType.Star ? cfg.Sides * 2 : cfg.Sides;
                                PointF[] pts = new PointF[ptsCount];
                                for (int i = 0; i < ptsCount; i++)
                                {
                                    double angle = (2.0 * Math.PI * i / ptsCount) - (Math.PI / 2.0);
                                    float curR = (cfg.Type == ShapeType.Star && i % 2 != 0) ? polyR * 0.45f : polyR;
                                    pts[i] = new PointF(center.X + (float)(curR * Math.Cos(angle)), center.Y + (float)(curR * Math.Sin(angle)));
                                }
                                e.Graphics.FillPolygon(fillBrush, pts);
                                e.Graphics.DrawPolygon(outlinePen, pts);
                                break;
                            case ShapeType.Text:
                                e.Graphics.DrawString(cfg.TextString, new Font("Segoe UI", Math.Max(8f, h * 0.4f), FontStyle.Bold), textBrush, rect);
                                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(80, ColorScheme.ControlsBorder), 1f), rect.X, rect.Y, rect.Width, rect.Height);
                                break;
                        }
                    }
                }
            }
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            var cfg = BuildConfig();
            string gcode = QuickShapesGenerator.GenerateGCode(cfg);

            string tempFile = Path.Combine(Path.GetTempPath(), $"QuickShape_{cfg.Type}_{DateTime.Now:yyyyMMdd_HHmmss}.nc");
            File.WriteAllText(tempFile, gcode);
            mCore.OpenFile(tempFile, false);

            MessageBox.Show($"Forma '{cfg.Type}' gerada e carregada no LaserGRBL com sucesso!\nVocê já pode visualizar o percurso e gravar.", "Formas Rápidas", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Arquivos G-Code (*.nc;*.gcode)|*.nc;*.gcode|Todos os Arquivos (*.*)|*.*";
                sfd.FileName = $"Forma_{cbShapeType.SelectedItem}.nc";
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    var cfg = BuildConfig();
                    string gcode = QuickShapesGenerator.GenerateGCode(cfg);
                    File.WriteAllText(sfd.FileName, gcode);
                    MessageBox.Show($"G-Code salvo em:\n{sfd.FileName}", "Salvar G-Code", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
