using LaserGRBL.Icons;
using LaserGRBL.UserControls;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace LaserGRBL.LightBurn
{
    public class MaterialTestGeneratorForm : Form
    {
        private GrblCore mCore;
        private TextBox txtTitle;
        private ComboBox cbMode;
        private NumericUpDown numCols;
        private NumericUpDown numRows;
        private NumericUpDown numSpeedMin;
        private NumericUpDown numSpeedMax;
        private NumericUpDown numPowerMin;
        private NumericUpDown numPowerMax;
        private NumericUpDown numBoxSize;
        private NumericUpDown numSpacing;
        private NumericUpDown numPasses;
        private ComboBox cbLaserMode;
        private Button btnGenerate;
        private Button btnSave;
        private Button btnCancel;
        private Panel pnlPreview;
        private Label lblTotalSize;

        public MaterialTestGeneratorForm(GrblCore core)
        {
            mCore = core;
            InitializeComponent();
            ApplyMaterialTheme();
            UpdateTotalSize();
        }

        public static void CreateAndShowDialog(GrblCore core)
        {
            using (MaterialTestGeneratorForm f = new MaterialTestGeneratorForm(core))
            {
                f.ShowDialog(FormsHelper.MainForm);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "LightBurn - Gerador de Matriz de Teste de Materiais (Material Test Matrix)";
            this.Size = new Size(720, 520);
            this.MinimumSize = new Size(680, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowIcon = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // GroupBox Config
            GroupBox gbConfig = new GroupBox()
            {
                Text = "Configurações da Matriz de Teste",
                Location = new Point(16, 12),
                Size = new Size(410, 395),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            int y = 26;
            Label lblTitle = new Label() { Text = "Título do Material:", Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            txtTitle = new TextBox() { Text = "TESTE DE MATERIAL 3MM", Location = new Point(155, y - 3), Width = 230, Font = new Font("Segoe UI", 8.5f) };
            txtTitle.TextChanged += (s, e) => pnlPreview.Invalidate();
            gbConfig.Controls.AddRange(new Control[] { lblTitle, txtTitle });

            y += 34;
            Label lblMode = new Label() { Text = "Modo do Teste:", Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            cbMode = new ComboBox() { Location = new Point(155, y - 3), Width = 230, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8.5f) };
            cbMode.Items.AddRange(new object[] { "Fill (Gravação / Preenchimento)", "Line (Corte / Contorno Vetorial)" });
            cbMode.SelectedIndex = 0;
            cbMode.SelectedIndexChanged += (s, e) => pnlPreview.Invalidate();
            gbConfig.Controls.AddRange(new Control[] { lblMode, cbMode });

            y += 34;
            Label lblSpeed = new Label() { Text = "Velocidade (Min/Max):", Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numSpeedMin = new NumericUpDown() { Location = new Point(155, y - 3), Width = 80, Minimum = 100, Maximum = 20000, Value = 500, Increment = 100 };
            numSpeedMax = new NumericUpDown() { Location = new Point(245, y - 3), Width = 80, Minimum = 200, Maximum = 20000, Value = 3000, Increment = 100 };
            Label lblSpeedUnit = new Label() { Text = "mm/min", Location = new Point(332, y), AutoSize = true, Font = new Font("Segoe UI", 8f) };
            gbConfig.Controls.AddRange(new Control[] { lblSpeed, numSpeedMin, numSpeedMax, lblSpeedUnit });

            y += 34;
            Label lblPower = new Label() { Text = "Potência % (Min/Max):", Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numPowerMin = new NumericUpDown() { Location = new Point(155, y - 3), Width = 80, Minimum = 1, Maximum = 100, Value = 10 };
            numPowerMax = new NumericUpDown() { Location = new Point(245, y - 3), Width = 80, Minimum = 1, Maximum = 100, Value = 100 };
            Label lblPowerUnit = new Label() { Text = "%", Location = new Point(332, y), AutoSize = true, Font = new Font("Segoe UI", 8f) };
            gbConfig.Controls.AddRange(new Control[] { lblPower, numPowerMin, numPowerMax, lblPowerUnit });

            y += 34;
            Label lblGrid = new Label() { Text = "Colunas x Linhas:", Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numCols = new NumericUpDown() { Location = new Point(155, y - 3), Width = 80, Minimum = 2, Maximum = 12, Value = 6 };
            numRows = new NumericUpDown() { Location = new Point(245, y - 3), Width = 80, Minimum = 2, Maximum = 12, Value = 6 };
            numCols.ValueChanged += (s, e) => { UpdateTotalSize(); pnlPreview.Invalidate(); };
            numRows.ValueChanged += (s, e) => { UpdateTotalSize(); pnlPreview.Invalidate(); };
            gbConfig.Controls.AddRange(new Control[] { lblGrid, numCols, numRows });

            y += 34;
            Label lblSize = new Label() { Text = "Tamanho do Quadrado:", Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numBoxSize = new NumericUpDown() { Location = new Point(155, y - 3), Width = 80, Minimum = 3, Maximum = 30, Value = 6 };
            Label lblSizeUnit = new Label() { Text = "mm", Location = new Point(240, y), AutoSize = true, Font = new Font("Segoe UI", 8f) };
            numBoxSize.ValueChanged += (s, e) => { UpdateTotalSize(); pnlPreview.Invalidate(); };
            gbConfig.Controls.AddRange(new Control[] { lblSize, numBoxSize, lblSizeUnit });

            y += 34;
            Label lblSpacing = new Label() { Text = "Espaçamento:", Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numSpacing = new NumericUpDown() { Location = new Point(155, y - 3), Width = 80, Minimum = 1, Maximum = 10, Value = 2 };
            Label lblSpacingUnit = new Label() { Text = "mm", Location = new Point(240, y), AutoSize = true, Font = new Font("Segoe UI", 8f) };
            numSpacing.ValueChanged += (s, e) => { UpdateTotalSize(); pnlPreview.Invalidate(); };
            gbConfig.Controls.AddRange(new Control[] { lblSpacing, numSpacing, lblSpacingUnit });

            y += 34;
            Label lblPasses = new Label() { Text = "Passes (Repetições):", Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numPasses = new NumericUpDown() { Location = new Point(155, y - 3), Width = 80, Minimum = 1, Maximum = 10, Value = 1 };
            gbConfig.Controls.AddRange(new Control[] { lblPasses, numPasses });

            y += 34;
            Label lblLaser = new Label() { Text = "Comando Laser ON:", Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            cbLaserMode = new ComboBox() { Location = new Point(155, y - 3), Width = 170, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8.5f) };
            cbLaserMode.Items.AddRange(new object[] { "M3 (Potência Constante)", "M4 (Potência Dinâmica)" });
            cbLaserMode.SelectedIndex = 1;
            gbConfig.Controls.AddRange(new Control[] { lblLaser, cbLaserMode });

            // Preview Panel (right side)
            pnlPreview = new Panel()
            {
                Location = new Point(440, 20),
                Size = new Size(250, 340),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlPreview.Paint += PnlPreview_Paint;

            lblTotalSize = new Label()
            {
                Location = new Point(440, 368),
                Size = new Size(250, 35),
                TextAlign = ContentAlignment.TopCenter,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic)
            };

            // Buttons
            btnGenerate = new Button()
            {
                Text = "✓ GERAR E CARREGAR NO LASERGRBL",
                Location = new Point(16, 420),
                Size = new Size(300, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnGenerate.Click += BtnGenerate_Click;

            btnSave = new Button()
            {
                Text = "💾 Salvar G-Code...",
                Location = new Point(325, 420),
                Size = new Size(160, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button()
            {
                Text = "Fechar",
                Location = new Point(570, 420),
                Size = new Size(120, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { gbConfig, pnlPreview, lblTotalSize, btnGenerate, btnSave, btnCancel });
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

            txtTitle.BackColor = ColorScheme.LogBackColor;
            txtTitle.ForeColor = ColorScheme.FormForeColor;
            cbMode.BackColor = ColorScheme.LogBackColor;
            cbMode.ForeColor = ColorScheme.FormForeColor;
            cbLaserMode.BackColor = ColorScheme.LogBackColor;
            cbLaserMode.ForeColor = ColorScheme.FormForeColor;

            numCols.BackColor = ColorScheme.LogBackColor;
            numCols.ForeColor = ColorScheme.FormForeColor;
            numRows.BackColor = ColorScheme.LogBackColor;
            numRows.ForeColor = ColorScheme.FormForeColor;
            numSpeedMin.BackColor = ColorScheme.LogBackColor;
            numSpeedMin.ForeColor = ColorScheme.FormForeColor;
            numSpeedMax.BackColor = ColorScheme.LogBackColor;
            numSpeedMax.ForeColor = ColorScheme.FormForeColor;
            numPowerMin.BackColor = ColorScheme.LogBackColor;
            numPowerMin.ForeColor = ColorScheme.FormForeColor;
            numPowerMax.BackColor = ColorScheme.LogBackColor;
            numPowerMax.ForeColor = ColorScheme.FormForeColor;
            numBoxSize.BackColor = ColorScheme.LogBackColor;
            numBoxSize.ForeColor = ColorScheme.FormForeColor;
            numSpacing.BackColor = ColorScheme.LogBackColor;
            numSpacing.ForeColor = ColorScheme.FormForeColor;
            numPasses.BackColor = ColorScheme.LogBackColor;
            numPasses.ForeColor = ColorScheme.FormForeColor;
        }

        private void UpdateTotalSize()
        {
            double w = (double)numCols.Value * ((double)numBoxSize.Value + (double)numSpacing.Value) + 20.0;
            double h = (double)numRows.Value * ((double)numBoxSize.Value + (double)numSpacing.Value) + 16.0;
            lblTotalSize.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Dimensão da Matriz: {0:0.1} mm x {1:0.1} mm", w, h);
        }

        private MaterialTestConfig BuildConfiguration()
        {
            int maxPwm = 1000;
            try
            {
                maxPwm = (int)GrblCore.Configuration.MaxPWM;
            }
            catch { }

            return new MaterialTestConfig()
            {
                Title = txtTitle.Text.Trim(),
                Mode = cbMode.SelectedIndex == 1 ? "Line" : "Fill",
                Columns = (int)numCols.Value,
                Rows = (int)numRows.Value,
                MinSpeed = (int)numSpeedMin.Value,
                MaxSpeed = (int)numSpeedMax.Value,
                MinPowerPct = (int)numPowerMin.Value,
                MaxPowerPct = (int)numPowerMax.Value,
                BoxSize = (double)numBoxSize.Value,
                Spacing = (double)numSpacing.Value,
                Passes = (int)numPasses.Value,
                LaserMode = cbLaserMode.SelectedIndex == 0 ? "M3" : "M4",
                MaxPwm = maxPwm
            };
        }

        private void PnlPreview_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int cols = (int)numCols.Value;
            int rows = (int)numRows.Value;
            bool isFill = cbMode.SelectedIndex == 0;

            float padX = 35f;
            float padY = 35f;
            float availW = pnlPreview.Width - padX - 15f;
            float availH = pnlPreview.Height - padY - 25f;

            float cellW = availW / cols;
            float cellH = availH / rows;

            using (Font f = new Font("Segoe UI", 7f))
            using (Brush textBrush = new SolidBrush(ColorScheme.FormForeColor))
            {
                // Title
                e.Graphics.DrawString(txtTitle.Text, new Font("Segoe UI", 8f, FontStyle.Bold), textBrush, 10, 10);

                for (int r = 0; r < rows; r++)
                {
                    float y = padY + (rows - 1 - r) * cellH; // bottom up
                    float pPct = (float)numPowerMin.Value + r * ((float)(numPowerMax.Value - numPowerMin.Value) / (rows - 1));
                    e.Graphics.DrawString($"{pPct:0}%", f, textBrush, 4, y + cellH / 4);

                    for (int c = 0; c < cols; c++)
                    {
                        float x = padX + c * cellW;
                        if (r == 0)
                        {
                            float spd = (float)numSpeedMin.Value + c * ((float)(numSpeedMax.Value - numSpeedMin.Value) / (cols - 1));
                            e.Graphics.DrawString($"{spd:0}", f, textBrush, x, pnlPreview.Height - 20);
                        }

                        RectangleF rect = new RectangleF(x + 2, y + 2, cellW - 4, cellH - 4);

                        if (isFill)
                        {
                            int alpha = (int)(40 + (pPct / 100f) * 200);
                            using (SolidBrush b = new SolidBrush(Color.FromArgb(alpha, ColorScheme.PreviewLaserPower)))
                            {
                                e.Graphics.FillRectangle(b, rect);
                            }
                        }
                        using (Pen p = new Pen(ColorScheme.PressedButtons, 1f))
                        {
                            e.Graphics.DrawRectangle(p, rect.X, rect.Y, rect.Width, rect.Height);
                        }
                    }
                }
            }
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            var cfg = BuildConfiguration();
            string gcode = MaterialTestGenerator.GenerateGCode(cfg);

            string tempFile = Path.Combine(Path.GetTempPath(), $"MaterialTest_{DateTime.Now:yyyyMMdd_HHmmss}.nc");
            File.WriteAllText(tempFile, gcode);
            mCore.OpenFile(tempFile, false);

            MessageBox.Show("Matriz de teste gerada e carregada no LaserGRBL com sucesso!\nVocê já pode visualizar o percurso e iniciar o teste na sua máquina.", "Teste de Material", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Arquivos G-Code (*.nc;*.gcode)|*.nc;*.gcode|Todos os Arquivos (*.*)|*.*";
                sfd.FileName = $"Teste_Material_{txtTitle.Text.Replace(" ", "_")}.nc";
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    var cfg = BuildConfiguration();
                    string gcode = MaterialTestGenerator.GenerateGCode(cfg);
                    File.WriteAllText(sfd.FileName, gcode);
                    MessageBox.Show($"G-Code salvo em:\n{sfd.FileName}", "Salvar G-Code", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
