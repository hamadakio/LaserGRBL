using LaserGRBL.Icons;
using LaserGRBL.UserControls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace LaserGRBL.LightBurn
{
    public class LightBurnFramingForm : Form
    {
        private GrblCore mCore;
        private RadioButton rbBoundingBox;
        private RadioButton rbRubberBand;
        private CheckBox chkLaserGuide;
        private NumericUpDown numPower;
        private NumericUpDown numSpeed;
        private NumericUpDown numLoops;
        private Button btnStartFrame;
        private Button btnStopFrame;
        private Button btnToggleDot;
        private Button btnGoCenter;
        private Button btnGoOrigin;
        private Button btnGoTopLeft;
        private Button btnGoBottomRight;
        private Panel pnlPreview;
        private Label lblDimensions;
        private bool mIsLaserDotOn = false;

        public LightBurnFramingForm(GrblCore core)
        {
            mCore = core;
            InitializeComponent();
            ApplyMaterialTheme();
            UpdateDimensionsInfo();
        }

        public static void CreateAndShowDialog(GrblCore core)
        {
            using (LightBurnFramingForm f = new LightBurnFramingForm(core))
            {
                f.ShowDialog(FormsHelper.MainForm);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "LightBurn - Controle de Enquadramento Avançado (Framing & Guide Laser)";
            this.Size = new Size(620, 500);
            this.MinimumSize = new Size(580, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowIcon = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // GroupBox Mode
            GroupBox gbMode = new GroupBox()
            {
                Text = "Tipo de Enquadramento",
                Location = new Point(16, 12),
                Size = new Size(330, 85),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            rbBoundingBox = new RadioButton()
            {
                Text = "Enquadramento Retangular (Bounding Box)",
                Location = new Point(14, 24),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            rbBoundingBox.CheckedChanged += (s, e) => pnlPreview.Invalidate();

            rbRubberBand = new RadioButton()
            {
                Text = "Contorno Real / Rubber-Band (Convex Hull)",
                Location = new Point(14, 52),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            rbRubberBand.CheckedChanged += (s, e) => pnlPreview.Invalidate();

            gbMode.Controls.AddRange(new Control[] { rbBoundingBox, rbRubberBand });

            // GroupBox Settings
            GroupBox gbSettings = new GroupBox()
            {
                Text = "Parâmetros do Laser e Movimento",
                Location = new Point(16, 105),
                Size = new Size(330, 155),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            chkLaserGuide = new CheckBox()
            {
                Text = "Laser Guia de Baixa Potência Ativo",
                Location = new Point(14, 25),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };

            Label lblPower = new Label() { Text = "Potência Mira (S 1..50):", Location = new Point(14, 56), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numPower = new NumericUpDown() { Location = new Point(175, 53), Width = 70, Minimum = 1, Maximum = 100, Value = 2 };

            Label lblSpeed = new Label() { Text = "Velocidade (mm/min):", Location = new Point(14, 88), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numSpeed = new NumericUpDown() { Location = new Point(175, 85), Width = 70, Minimum = 100, Maximum = 10000, Value = 2000, Increment = 100 };

            Label lblLoops = new Label() { Text = "Repetições (Voltas):", Location = new Point(14, 120), AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            numLoops = new NumericUpDown() { Location = new Point(175, 117), Width = 70, Minimum = 1, Maximum = 20, Value = 1 };

            gbSettings.Controls.AddRange(new Control[] { chkLaserGuide, lblPower, numPower, lblSpeed, numSpeed, lblLoops, numLoops });

            // Preview Panel (right side)
            pnlPreview = new Panel()
            {
                Location = new Point(360, 20),
                Size = new Size(230, 240),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlPreview.Paint += PnlPreview_Paint;

            lblDimensions = new Label()
            {
                Location = new Point(360, 268),
                Size = new Size(230, 40),
                TextAlign = ContentAlignment.TopCenter,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic)
            };

            // Quick Move GroupBox
            GroupBox gbQuickMove = new GroupBox()
            {
                Text = "Posicionamento Rápido da Cabeça Laser",
                Location = new Point(16, 270),
                Size = new Size(330, 95),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            btnGoOrigin = new Button() { Text = "Origem (0,0)", Location = new Point(12, 22), Width = 95, Height = 28, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f) };
            btnGoOrigin.Click += (s, e) => SendImmediateMove(0, 0);

            btnGoCenter = new Button() { Text = "Centro Peça", Location = new Point(115, 22), Width = 95, Height = 28, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f) };
            btnGoCenter.Click += (s, e) =>
            {
                if (mCore.LoadedFile != null && mCore.LoadedFile.Range.DrawingRange.ValidRange)
                {
                    float cx = (float)(mCore.LoadedFile.Range.DrawingRange.X.Min + mCore.LoadedFile.Range.DrawingRange.Width / 2);
                    float cy = (float)(mCore.LoadedFile.Range.DrawingRange.Y.Min + mCore.LoadedFile.Range.DrawingRange.Height / 2);
                    SendImmediateMove(cx, cy);
                }
            };

            btnToggleDot = new Button() { Text = "🔴 Mira Laser", Location = new Point(218, 22), Width = 95, Height = 28, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f) };
            btnToggleDot.Click += BtnToggleDot_Click;

            btnGoTopLeft = new Button() { Text = "Topo-Esq", Location = new Point(12, 56), Width = 95, Height = 28, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f) };
            btnGoTopLeft.Click += (s, e) =>
            {
                if (mCore.LoadedFile != null && mCore.LoadedFile.Range.DrawingRange.ValidRange)
                    SendImmediateMove((float)mCore.LoadedFile.Range.DrawingRange.X.Min, (float)mCore.LoadedFile.Range.DrawingRange.Y.Max);
            };

            btnGoBottomRight = new Button() { Text = "Fundo-Dir", Location = new Point(115, 56), Width = 95, Height = 28, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f) };
            btnGoBottomRight.Click += (s, e) =>
            {
                if (mCore.LoadedFile != null && mCore.LoadedFile.Range.DrawingRange.ValidRange)
                    SendImmediateMove((float)mCore.LoadedFile.Range.DrawingRange.X.Max, (float)mCore.LoadedFile.Range.DrawingRange.Y.Min);
            };

            gbQuickMove.Controls.AddRange(new Control[] { btnGoOrigin, btnGoCenter, btnToggleDot, btnGoTopLeft, btnGoBottomRight });

            // Bottom Actions
            btnStartFrame = new Button()
            {
                Text = "▶ INICIAR ENQUADRAMENTO",
                Location = new Point(16, 385),
                Size = new Size(280, 50),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold)
            };
            btnStartFrame.Click += BtnStartFrame_Click;

            btnStopFrame = new Button()
            {
                Text = "⏹ PARAR / DESLIGAR LASER",
                Location = new Point(310, 385),
                Size = new Size(280, 50),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold)
            };
            btnStopFrame.Click += BtnStopFrame_Click;

            this.Controls.AddRange(new Control[] { gbMode, gbSettings, pnlPreview, lblDimensions, gbQuickMove, btnStartFrame, btnStopFrame });
            this.FormClosing += (s, e) => TurnLaserOff();
        }

        private void ApplyMaterialTheme()
        {
            this.BackColor = ColorScheme.FormBackColor;
            this.ForeColor = ColorScheme.FormForeColor;
            ThemeMgr.SetTheme(this, true);

            pnlPreview.BackColor = ColorScheme.PreviewBackColor;

            btnStartFrame.BackColor = ColorScheme.PressedButtons;
            btnStartFrame.ForeColor = Color.White;
            btnStartFrame.FlatAppearance.BorderColor = ColorScheme.PressedButtons;

            btnStopFrame.BackColor = Color.FromArgb(220, 38, 38);
            btnStopFrame.ForeColor = Color.White;
            btnStopFrame.FlatAppearance.BorderColor = Color.FromArgb(220, 38, 38);

            btnGoOrigin.BackColor = ColorScheme.FormButtonsColor;
            btnGoOrigin.ForeColor = ColorScheme.FormForeColor;
            btnGoCenter.BackColor = ColorScheme.FormButtonsColor;
            btnGoCenter.ForeColor = ColorScheme.FormForeColor;
            btnToggleDot.BackColor = ColorScheme.FormButtonsColor;
            btnToggleDot.ForeColor = ColorScheme.FormForeColor;
            btnGoTopLeft.BackColor = ColorScheme.FormButtonsColor;
            btnGoTopLeft.ForeColor = ColorScheme.FormForeColor;
            btnGoBottomRight.BackColor = ColorScheme.FormButtonsColor;
            btnGoBottomRight.ForeColor = ColorScheme.FormForeColor;

            numPower.BackColor = ColorScheme.LogBackColor;
            numPower.ForeColor = ColorScheme.FormForeColor;
            numSpeed.BackColor = ColorScheme.LogBackColor;
            numSpeed.ForeColor = ColorScheme.FormForeColor;
            numLoops.BackColor = ColorScheme.LogBackColor;
            numLoops.ForeColor = ColorScheme.FormForeColor;
        }

        private void UpdateDimensionsInfo()
        {
            if (mCore.LoadedFile != null && mCore.LoadedFile.Range.DrawingRange.ValidRange)
            {
                double w = (double)mCore.LoadedFile.Range.DrawingRange.Width;
                double h = (double)mCore.LoadedFile.Range.DrawingRange.Height;
                lblDimensions.Text = string.Format(CultureInfo.InvariantCulture, "Largura: {0:0.1} mm\nAltura: {1:0.1} mm", w, h);
            }
            else
            {
                lblDimensions.Text = "Nenhum trabalho carregado.";
            }
        }

        private List<PointF> GetCurrentPolygon()
        {
            if (mCore.LoadedFile == null || !mCore.LoadedFile.Range.DrawingRange.ValidRange)
                return new List<PointF>();

            if (rbRubberBand.Checked)
                return FramingHelper.GetRubberBandHull(mCore.LoadedFile);
            else
                return FramingHelper.GetBoundingBox(mCore.LoadedFile);
        }

        private void PnlPreview_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var polygon = GetCurrentPolygon();
            if (polygon == null || polygon.Count < 2)
            {
                using (Brush b = new SolidBrush(ColorScheme.DisabledButtons))
                {
                    e.Graphics.DrawString("Sem visualização", new Font("Segoe UI", 9f), b, 60, 100);
                }
                return;
            }

            // Find scale to fit inside 210x210
            float minX = polygon.Min(p => p.X);
            float maxX = polygon.Max(p => p.X);
            float minY = polygon.Min(p => p.Y);
            float maxY = polygon.Max(p => p.Y);

            float w = Math.Max(1f, maxX - minX);
            float h = Math.Max(1f, maxY - minY);

            float pad = 20f;
            float targetW = pnlPreview.Width - pad * 2;
            float targetH = pnlPreview.Height - pad * 2;

            float scale = Math.Min(targetW / w, targetH / h);

            PointF[] screenPts = polygon.Select(p => new PointF(
                pad + (p.X - minX) * scale,
                pnlPreview.Height - pad - (p.Y - minY) * scale // invert Y for screen
            )).ToArray();

            // Draw bounding box guideline
            using (Pen dashed = new Pen(Color.FromArgb(100, ColorScheme.ControlsBorder), 1f))
            {
                dashed.DashStyle = DashStyle.Dash;
                e.Graphics.DrawRectangle(dashed, pad, pad, w * scale, h * scale);
            }

            // Draw framing shape
            using (Pen hullPen = new Pen(ColorScheme.PressedButtons, 2f))
            using (SolidBrush fillBrush = new SolidBrush(Color.FromArgb(30, ColorScheme.PressedButtons)))
            {
                e.Graphics.FillPolygon(fillBrush, screenPts);
                e.Graphics.DrawPolygon(hullPen, screenPts);
            }

            // Draw corner points
            using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(255, 23, 68)))
            {
                foreach (var pt in screenPts)
                    e.Graphics.FillEllipse(dotBrush, pt.X - 3, pt.Y - 3, 6, 6);
            }
        }

        private void BtnStartFrame_Click(object sender, EventArgs e)
        {
            if (!mCore.IsConnected)
            {
                MessageBox.Show("A máquina não está conectada ao LaserGRBL.", "Enquadramento", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var polygon = GetCurrentPolygon();
            if (polygon == null || polygon.Count < 2)
            {
                MessageBox.Show("Não há caminho ou limites válidos no trabalho atual.", "Enquadramento", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int speed = (int)numSpeed.Value;
            int power = (int)numPower.Value;
            bool laserGuide = chkLaserGuide.Checked;
            int loops = (int)numLoops.Value;

            string gcode = FramingHelper.GenerateFramingGCode(polygon, speed, power, laserGuide, loops);
            mCore.ExecuteCustomCode(gcode);
        }

        private void BtnStopFrame_Click(object sender, EventArgs e)
        {
            TurnLaserOff();
            mCore.GrblReset();
        }

        private void TurnLaserOff()
        {
            mIsLaserDotOn = false;
            btnToggleDot.Text = "🔴 Mira Laser";
            if (mCore.IsConnected)
            {
                mCore.ExecuteCustomCode("M5 S0");
            }
        }

        private void BtnToggleDot_Click(object sender, EventArgs e)
        {
            if (!mCore.IsConnected) return;

            mIsLaserDotOn = !mIsLaserDotOn;
            if (mIsLaserDotOn)
            {
                int pwr = (int)numPower.Value;
                mCore.ExecuteCustomCode($"M3 S{pwr}");
                btnToggleDot.Text = "⏹ Desligar Mira";
            }
            else
            {
                TurnLaserOff();
            }
        }

        private void SendImmediateMove(float x, float y)
        {
            if (!mCore.IsConnected) return;
            int speed = (int)numSpeed.Value;
            string cmd = string.Format(CultureInfo.InvariantCulture, "G90\nG0 X{0:0.000} Y{1:0.000}", x, y);
            mCore.ExecuteCustomCode(cmd);
        }
    }
}
