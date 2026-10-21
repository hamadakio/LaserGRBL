using LaserGRBL.Icons;
using LaserGRBL.UserControls;
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace LaserGRBL.LightBurn
{
    public class RotarySetupForm : Form
    {
        private GrblCore mCore;
        private RadioButton rbRoller;
        private RadioButton rbChuck;
        private NumericUpDown numRollerDiameter;
        private NumericUpDown numObjectDiameter;
        private NumericUpDown numStepsPerRev;
        private NumericUpDown numGearRatio;
        private Label lblCalculatedSteps;
        private Label lblOriginalSteps;
        private Label lblCircumference;
        private Button btnApplyToGrbl;
        private Button btnRestoreOriginal;
        private Button btnTest360;
        private Button btnClose;
        private double mOriginalYSteps = 80.0; // default standard GRBL $101

        public RotarySetupForm(GrblCore core)
        {
            mCore = core;
            InitializeComponent();
            ApplyMaterialTheme();
            ReadGrblSettings();
            Recalculate();
        }

        public static void CreateAndShowDialog(GrblCore core)
        {
            using (RotarySetupForm f = new RotarySetupForm(core))
            {
                f.ShowDialog(FormsHelper.MainForm);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "LightBurn - Configuração de Eixo Rotativo (Rotary Setup)";
            this.Size = new Size(580, 480);
            this.MinimumSize = new Size(540, 440);
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowIcon = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // GroupBox Type
            GroupBox gbType = new GroupBox()
            {
                Text = "Tipo de Mecanismo Rotativo",
                Location = new Point(16, 12),
                Size = new Size(530, 70),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            rbRoller = new RadioButton()
            {
                Text = "Rolos (Roller - 2 rolos cilíndricos)",
                Location = new Point(20, 25),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            rbRoller.CheckedChanged += (s, e) => { UpdateRollerVisibility(); Recalculate(); };

            rbChuck = new RadioButton()
            {
                Text = "Mandril / Castanhas (Chuck - garra direta)",
                Location = new Point(280, 25),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            rbChuck.CheckedChanged += (s, e) => { UpdateRollerVisibility(); Recalculate(); };

            gbType.Controls.AddRange(new Control[] { rbRoller, rbChuck });

            // GroupBox Dimensions
            GroupBox gbDims = new GroupBox()
            {
                Text = "Parâmetros Físicos e Calibração",
                Location = new Point(16, 90),
                Size = new Size(530, 160),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            int y = 25;
            Label lblObjDiam = new Label() { Text = "Diâmetro do Objeto (Copo / Garrafa mm):", Location = new Point(16, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numObjectDiameter = new NumericUpDown() { Location = new Point(285, y - 3), Width = 80, Minimum = 5, Maximum = 500, Value = 75, DecimalPlaces = 1 };
            numObjectDiameter.ValueChanged += (s, e) => Recalculate();
            gbDims.Controls.AddRange(new Control[] { lblObjDiam, numObjectDiameter });

            y += 32;
            Label lblRollerDiam = new Label() { Text = "Diâmetro dos Rolos (mm):", Location = new Point(16, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numRollerDiameter = new NumericUpDown() { Location = new Point(285, y - 3), Width = 80, Minimum = 5, Maximum = 100, Value = 20, DecimalPlaces = 1 };
            numRollerDiameter.ValueChanged += (s, e) => Recalculate();
            gbDims.Controls.AddRange(new Control[] { lblRollerDiam, numRollerDiameter });

            y += 32;
            Label lblSteps = new Label() { Text = "Passos do Motor por Volta (Passos * Micropassos):", Location = new Point(16, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numStepsPerRev = new NumericUpDown() { Location = new Point(285, y - 3), Width = 80, Minimum = 200, Maximum = 200000, Value = 3200, Increment = 100 };
            numStepsPerRev.ValueChanged += (s, e) => Recalculate();
            gbDims.Controls.AddRange(new Control[] { lblSteps, numStepsPerRev });

            y += 32;
            Label lblRatio = new Label() { Text = "Relação de Polias (Gear Ratio):", Location = new Point(16, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numGearRatio = new NumericUpDown() { Location = new Point(285, y - 3), Width = 80, Minimum = 0.1M, Maximum = 20M, Value = 1.0M, DecimalPlaces = 2, Increment = 0.1M };
            numGearRatio.ValueChanged += (s, e) => Recalculate();
            gbDims.Controls.AddRange(new Control[] { lblRatio, numGearRatio });

            // GroupBox Results
            GroupBox gbResults = new GroupBox()
            {
                Text = "Cálculo de Passos/mm do Eixo Y ($101)",
                Location = new Point(16, 260),
                Size = new Size(530, 95),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            lblOriginalSteps = new Label() { Text = "Valor $101 Atual da Máquina: 80.000 passos/mm", Location = new Point(16, 24), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            lblCircumference = new Label() { Text = "Circunferência do Objeto: 235.6 mm", Location = new Point(16, 46), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            lblCalculatedSteps = new Label() { Text = "Novo $101 para Eixo Rotativo: 50.930 passos/mm", Location = new Point(16, 68), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 229, 255) };

            gbResults.Controls.AddRange(new Control[] { lblOriginalSteps, lblCircumference, lblCalculatedSteps });

            // Buttons
            btnApplyToGrbl = new Button()
            {
                Text = "✓ GRAVAR NO GRBL ($101)",
                Location = new Point(16, 375),
                Size = new Size(190, 44),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnApplyToGrbl.Click += BtnApplyToGrbl_Click;

            btnRestoreOriginal = new Button()
            {
                Text = "↺ Restaurar $101 Normal",
                Location = new Point(215, 375),
                Size = new Size(160, 44),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f)
            };
            btnRestoreOriginal.Click += BtnRestoreOriginal_Click;

            btnTest360 = new Button()
            {
                Text = "🔄 Teste 360°",
                Location = new Point(385, 375),
                Size = new Size(95, 44),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f)
            };
            btnTest360.Click += BtnTest360_Click;

            btnClose = new Button()
            {
                Text = "Fechar",
                Location = new Point(488, 375),
                Size = new Size(58, 44),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f)
            };
            btnClose.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { gbType, gbDims, gbResults, btnApplyToGrbl, btnRestoreOriginal, btnTest360, btnClose });
        }

        private void ApplyMaterialTheme()
        {
            this.BackColor = ColorScheme.FormBackColor;
            this.ForeColor = ColorScheme.FormForeColor;
            ThemeMgr.SetTheme(this, true);

            btnApplyToGrbl.BackColor = ColorScheme.PressedButtons;
            btnApplyToGrbl.ForeColor = Color.White;
            btnApplyToGrbl.FlatAppearance.BorderColor = ColorScheme.PressedButtons;

            btnRestoreOriginal.BackColor = ColorScheme.FormButtonsColor;
            btnRestoreOriginal.ForeColor = ColorScheme.FormForeColor;
            btnRestoreOriginal.FlatAppearance.BorderColor = ColorScheme.ControlsBorder;

            btnTest360.BackColor = ColorScheme.FormButtonsColor;
            btnTest360.ForeColor = ColorScheme.FormForeColor;
            btnTest360.FlatAppearance.BorderColor = ColorScheme.ControlsBorder;

            btnClose.BackColor = ColorScheme.FormButtonsColor;
            btnClose.ForeColor = ColorScheme.FormForeColor;
            btnClose.FlatAppearance.BorderColor = ColorScheme.ControlsBorder;

            numRollerDiameter.BackColor = ColorScheme.LogBackColor;
            numRollerDiameter.ForeColor = ColorScheme.FormForeColor;
            numObjectDiameter.BackColor = ColorScheme.LogBackColor;
            numObjectDiameter.ForeColor = ColorScheme.FormForeColor;
            numStepsPerRev.BackColor = ColorScheme.LogBackColor;
            numStepsPerRev.ForeColor = ColorScheme.FormForeColor;
            numGearRatio.BackColor = ColorScheme.LogBackColor;
            numGearRatio.ForeColor = ColorScheme.FormForeColor;

            lblCalculatedSteps.ForeColor = ColorScheme.DarkScheme ? Color.FromArgb(0, 229, 255) : Color.FromArgb(37, 99, 235);
        }

        private void ReadGrblSettings()
        {
            try
            {
                if (GrblCore.Configuration != null)
                {
                    mOriginalYSteps = (double)GrblCore.Configuration.ResolutionY;
                    Settings.SetObject("OriginalRotaryYSteps", mOriginalYSteps);
                }
                else
                {
                    mOriginalYSteps = Settings.GetObject("OriginalRotaryYSteps", 80.0);
                }
            }
            catch
            {
                mOriginalYSteps = Settings.GetObject("OriginalRotaryYSteps", 80.0);
            }

            lblOriginalSteps.Text = string.Format(CultureInfo.InvariantCulture, "Valor $101 Atual da Máquina: {0:0.000} passos/mm", mOriginalYSteps);
        }

        private void UpdateRollerVisibility()
        {
            numRollerDiameter.Enabled = rbRoller.Checked;
        }

        private double CalculateRotarySteps()
        {
            double objDiam = (double)numObjectDiameter.Value;
            double rollerDiam = (double)numRollerDiameter.Value;
            double stepsRev = (double)numStepsPerRev.Value;
            double ratio = (double)numGearRatio.Value;

            double circum = Math.PI * objDiam;
            lblCircumference.Text = string.Format(CultureInfo.InvariantCulture, "Circunferência do Objeto: {0:0.1} mm", circum);

            double targetStepsPerMm;
            if (rbRoller.Checked)
            {
                // In roller mode, 1 rotation of roller moves the surface by PI * rollerDiam
                targetStepsPerMm = (stepsRev * ratio) / (Math.PI * rollerDiam);
            }
            else
            {
                // In chuck mode, 1 rotation of motor turns the object 1 full revolution (PI * objDiam)
                targetStepsPerMm = (stepsRev * ratio) / (Math.PI * objDiam);
            }

            return targetStepsPerMm;
        }

        private void Recalculate()
        {
            double steps = CalculateRotarySteps();
            lblCalculatedSteps.Text = string.Format(CultureInfo.InvariantCulture, "Novo $101 para Eixo Rotativo: {0:0.000} passos/mm", steps);
        }

        private void BtnApplyToGrbl_Click(object sender, EventArgs e)
        {
            if (!mCore.IsConnected)
            {
                MessageBox.Show("Conecte a máquina laser primeiro para gravar o comando no GRBL.", "Eixo Rotativo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double steps = CalculateRotarySteps();
            string cmd = string.Format(CultureInfo.InvariantCulture, "$101={0:0.000}", steps);
            mCore.ExecuteCustomCode(cmd);

            MessageBox.Show($"Configuração aplicada no GRBL!\nComando enviado: {cmd}\nAgora seu eixo Y está calibrado para gravação cilíndrica perfeita.", "Eixo Rotativo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnRestoreOriginal_Click(object sender, EventArgs e)
        {
            if (!mCore.IsConnected)
            {
                MessageBox.Show("Conecte a máquina laser primeiro.", "Eixo Rotativo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string cmd = string.Format(CultureInfo.InvariantCulture, "$101={0:0.000}", mOriginalYSteps);
            mCore.ExecuteCustomCode(cmd);

            MessageBox.Show($"Eixo Y restaurado para modo plano original!\nComando enviado: {cmd}", "Eixo Rotativo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnTest360_Click(object sender, EventArgs e)
        {
            if (!mCore.IsConnected)
            {
                MessageBox.Show("Conecte a máquina laser primeiro.", "Eixo Rotativo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double circum = Math.PI * (double)numObjectDiameter.Value;
            string cmd = string.Format(CultureInfo.InvariantCulture, "$J=G91 G21 Y{0:0.000} F1000", circum);
            mCore.ExecuteCustomCode(cmd);
        }
    }
}
