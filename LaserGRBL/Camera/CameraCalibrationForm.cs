using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;

namespace LaserGRBL.Camera
{
    public class CameraCalibrationForm : Form
    {
        private PictureBox pbRaw;
        private PictureBox pbRectified;
        private Label lblInstructions;
        private ComboBox cmbCalibMode;
        private Button btnBurnTargets;
        private Button btnSave;
        private Button btnReset;
        private Button btnClose;
        private Panel pnlLegend;

        private PointF[] mNormPoints;
        private int mDraggingIndex = -1;
        private int mHoverIndex = -1;
        private Point mMouseScreenPos;
        private bool mShowLoupe = false;
        private Bitmap mCurrentRaw;
        private readonly object mRawLock = new object();
        private GrblCore mCore;

        private readonly Color[] MarkerColors = new Color[]
        {
            Color.FromArgb(33, 150, 243),  // P1: Blue
            Color.FromArgb(76, 175, 80),   // P2: Green
            Color.FromArgb(255, 152, 0),   // P3: Orange
            Color.FromArgb(156, 39, 176)   // P4: Purple
        };

        private readonly string[] MarkerLabelsCorners = new string[]
        {
            "P1: Superior Esquerdo (0, Y_Max)",
            "P2: Superior Direito (X_Max, Y_Max)",
            "P3: Inferior Direito (X_Max, 0)",
            "P4: Inferior Esquerdo (0, 0)"
        };

        private readonly string[] MarkerLabelsTargets = new string[]
        {
            "P1: Alvo Superior Esquerdo (30mm, Max-30mm)",
            "P2: Alvo Superior Direito (Max-30mm, Max-30mm)",
            "P3: Alvo Inferior Direito (Max-30mm, 30mm)",
            "P4: Alvo Inferior Esquerdo (30mm, 30mm)"
        };

        public CameraCalibrationForm(GrblCore core = null)
        {
            mCore = core;
            InitializeComponent();
            LoadCurrentPoints();
            UpdateRectifiedPreview();

            LaserCameraServer.OnFrameReceived += OnFrameReceivedFromThread;
        }

        private void InitializeComponent()
        {
            Text = "Calibração de Câmera & Alinhamento de Mesa (Método LightBurn)";
            Size = new Size(1060, 720);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ColorScheme.FormBackColor;
            ForeColor = ColorScheme.FormForeColor;

            lblInstructions = new Label
            {
                Text = "💡 Dica de Ouro: Para máxima precisão, use o botão 'Gravar 4 Alvos' abaixo. O laser queima 4 pequenas cruzes no papelão e elimina 100% dos erros de ângulo e paralaxe!",
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                BackColor = ColorScheme.DarkScheme ? Color.FromArgb(20, 24, 39) : Color.FromArgb(235, 240, 250)
            };

            // Left Box: Raw camera image with draggable corners
            pbRaw = new PictureBox
            {
                Location = new Point(12, 50),
                Size = new Size(620, 560),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(15, 23, 42),
                Cursor = Cursors.Cross
            };
            pbRaw.Paint += PbRaw_Paint;
            pbRaw.MouseDown += PbRaw_MouseDown;
            pbRaw.MouseMove += PbRaw_MouseMove;
            pbRaw.MouseUp += PbRaw_MouseUp;
            pbRaw.MouseLeave += (s, e) => { mShowLoupe = false; mHoverIndex = -1; pbRaw.Invalidate(); };

            // Right Panel for Calibration Settings & Preview
            int rx = 645;
            int rw = 385;

            Label lblModeTitle = new Label
            {
                Text = "Modo de Alinhamento:",
                Location = new Point(rx, 50),
                Size = new Size(rw, 20),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            cmbCalibMode = new ComboBox
            {
                Location = new Point(rx, 72),
                Size = new Size(rw, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f)
            };
            cmbCalibMode.Items.Add("Modo 1: Cantos da Área Útil (0,0 até Máximo)");
            cmbCalibMode.Items.Add("Modo 2: 4 Alvos Gravados no Material (Método LightBurn)");
            cmbCalibMode.SelectedIndex = CameraOverlayManager.CalibMode == 1 ? 1 : 0;
            cmbCalibMode.SelectedIndexChanged += (s, e) =>
            {
                CameraOverlayManager.CalibMode = cmbCalibMode.SelectedIndex;
                UpdateLegendLabels();
                UpdateRectifiedPreview();
            };

            btnBurnTargets = new Button
            {
                Text = "🎯 Gravar 4 Alvos no Papelão (Método LightBurn)",
                Location = new Point(rx, 108),
                Size = new Size(rw, 36),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(234, 88, 12),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnBurnTargets.FlatAppearance.BorderSize = 0;
            btnBurnTargets.Click += BurnTargets_Click;

            // Preview Title & PictureBox
            Label lblPreviewTitle = new Label
            {
                Text = "Visão Superior Retificada (1:1 com a Área do Laser):",
                Location = new Point(rx, 155),
                Size = new Size(rw, 22),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            pbRectified = new PictureBox
            {
                Location = new Point(rx, 178),
                Size = new Size(rw, 260),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(15, 23, 42)
            };

            // Legend Panel
            pnlLegend = new Panel
            {
                Location = new Point(rx, 448),
                Size = new Size(rw, 162),
                BackColor = ColorScheme.DarkScheme ? Color.FromArgb(30, 32, 48) : Color.FromArgb(245, 245, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8)
            };
            UpdateLegendLabels();

            // Bottom Buttons
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(12)
            };

            btnReset = new Button
            {
                Text = "Restaurar Padrão",
                Size = new Size(140, 34),
                Location = new Point(12, 12),
                FlatStyle = FlatStyle.Flat
            };
            btnReset.Click += (s, e) =>
            {
                if (CameraOverlayManager.CalibMode == 1)
                {
                    mNormPoints = new PointF[]
                    {
                        new PointF(0.12f, 0.12f),
                        new PointF(0.88f, 0.12f),
                        new PointF(0.88f, 0.88f),
                        new PointF(0.12f, 0.88f)
                    };
                }
                else
                {
                    mNormPoints = new PointF[]
                    {
                        new PointF(0.05f, 0.05f),
                        new PointF(0.95f, 0.05f),
                        new PointF(0.95f, 0.95f),
                        new PointF(0.05f, 0.95f)
                    };
                }
                pbRaw.Invalidate();
                UpdateRectifiedPreview();
            };

            Button btnRotate = new Button
            {
                Text = "🔄 Rotacionar Foto (90°)",
                Size = new Size(190, 34),
                Location = new Point(165, 12),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = ColorScheme.DarkScheme ? Color.FromArgb(40, 44, 66) : Color.FromArgb(230, 235, 245),
                Cursor = Cursors.Hand
            };
            btnRotate.Click += (s, e) =>
            {
                CameraOverlayManager.RotatePhoto(90);
                mNormPoints = (PointF[])CameraOverlayManager.CalibrationPoints.Clone();
                RefreshLiveFrame();
                pbRaw.Invalidate();
                UpdateRectifiedPreview();
            };

            btnSave = new Button
            {
                Text = "💾 Salvar Calibração",
                Size = new Size(180, 34),
                Location = new Point(730, 12),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White
            };
            btnSave.Click += BtnSave_Click;

            btnClose = new Button
            {
                Text = "Cancelar",
                Size = new Size(110, 34),
                Location = new Point(920, 12),
                FlatStyle = FlatStyle.Flat
            };
            btnClose.Click += (s, e) => Close();

            pnlBottom.Controls.Add(btnReset);
            pnlBottom.Controls.Add(btnRotate);
            pnlBottom.Controls.Add(btnSave);
            pnlBottom.Controls.Add(btnClose);

            Controls.Add(pbRaw);
            Controls.Add(lblModeTitle);
            Controls.Add(cmbCalibMode);
            Controls.Add(btnBurnTargets);
            Controls.Add(lblPreviewTitle);
            Controls.Add(pbRectified);
            Controls.Add(pnlLegend);
            Controls.Add(lblInstructions);
            Controls.Add(pnlBottom);
        }

        private void UpdateLegendLabels()
        {
            pnlLegend.Controls.Clear();
            string[] labels = cmbCalibMode?.SelectedIndex == 1 ? MarkerLabelsTargets : MarkerLabelsCorners;

            for (int i = 0; i < 4; i++)
            {
                Panel dot = new Panel
                {
                    Size = new Size(14, 14),
                    Location = new Point(10, 10 + (i * 28)),
                    BackColor = MarkerColors[i]
                };
                Label lbl = new Label
                {
                    Text = labels[i],
                    Location = new Point(32, 8 + (i * 28)),
                    Size = new Size(340, 20),
                    Font = new Font("Segoe UI", 8.5f)
                };
                pnlLegend.Controls.Add(dot);
                pnlLegend.Controls.Add(lbl);
            }

            Label lblNote = new Label
            {
                Text = "⚠️ Use a Lupa 4x que surge ao aproximar o mouse para centralizar com precisão de 1 pixel!",
                Location = new Point(10, 122),
                Size = new Size(365, 34),
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(148, 163, 184)
            };
            pnlLegend.Controls.Add(lblNote);
        }

        private void BurnTargets_Click(object sender, EventArgs e)
        {
            float w = CameraOverlayManager.BedWidthMm;
            float h = CameraOverlayManager.BedHeightMm;
            float m = CameraOverlayManager.TargetMarginMm;

            string msg = $"O Método de Alvos Gravados é o mesmo padrão utilizado pelo LightBurn:\n\n" +
                         $"O laser vai queimar 4 pequenos alvos '+' nos 4 cantos da sua folha ou papelão:\n" +
                         $"• P1: Superior Esquerdo (X{m:0}, Y{h - m:0})\n" +
                         $"• P2: Superior Direito (X{w - m:0}, Y{h - m:0})\n" +
                         $"• P3: Inferior Direito (X{w - m:0}, Y{m:0})\n" +
                         $"• P4: Inferior Esquerdo (X{m:0}, Y{m:0})\n\n" +
                         $"Como as marcas são feitas pelo próprio laser na altura do material:\n" +
                         $"1. Elimina 100% de qualquer erro de ângulo ou inclinação da câmera (paralaxe).\n" +
                         $"2. Garante alinhamento milimétrico com o zero real da máquina.\n\n" +
                         $"Deseja carregar este código G-Code no LaserGRBL agora?";

            if (MessageBox.Show(this, msg, "Gravar 4 Alvos de Precisão (Método LightBurn)", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("; Calibracao de Camera - 4 Alvos LightBurn");
                sb.AppendLine("; Gravacao de 4 cruzes com margem de 30mm");
                sb.AppendLine("G21");
                sb.AppendLine("G90");
                var ci = System.Globalization.CultureInfo.InvariantCulture;

                void AppendCross(float cx, float cy)
                {
                    sb.AppendLine($"G0 X{cx.ToString("F1", ci)} Y{cy.ToString("F1", ci)} F3000");
                    sb.AppendLine("M3 S250");
                    sb.AppendLine($"G1 X{(cx - 6).ToString("F1", ci)} Y{cy.ToString("F1", ci)} F800");
                    sb.AppendLine($"G1 X{(cx + 6).ToString("F1", ci)} Y{cy.ToString("F1", ci)} F800");
                    sb.AppendLine($"G0 X{cx.ToString("F1", ci)} Y{cy.ToString("F1", ci)}");
                    sb.AppendLine($"G1 X{cx.ToString("F1", ci)} Y{(cy - 6).ToString("F1", ci)} F800");
                    sb.AppendLine($"G1 X{cx.ToString("F1", ci)} Y{(cy + 6).ToString("F1", ci)} F800");
                    sb.AppendLine("M5");
                }

                // Target 1: Top-Left (m, h - m)
                AppendCross(m, h - m);
                // Target 2: Top-Right (w - m, h - m)
                AppendCross(w - m, h - m);
                // Target 3: Bottom-Right (w - m, m)
                AppendCross(w - m, m);
                // Target 4: Bottom-Left (m, m)
                AppendCross(m, m);

                sb.AppendLine("G0 X0 Y0 F3000");

                try
                {
                    string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Calibracao_Camera_4Alvos_{DateTime.Now:yyyyMMdd_HHmmss}.nc");
                    System.IO.File.WriteAllText(tempFile, sb.ToString());

                    if (mCore != null)
                    {
                        mCore.OpenFile(tempFile, false);
                        cmbCalibMode.SelectedIndex = 1;
                        MessageBox.Show(this, "G-Code dos 4 alvos carregado no LaserGRBL com sucesso!\n\nPassos:\n1. Clique no botão verde de Iniciar (Play) no LaserGRBL para gravar os alvos no papelão.\n2. Quando terminar, tire uma nova foto com o celular.\n3. Selecione o 'Modo 2: 4 Alvos Gravados' e arraste P1, P2, P3 e P4 para o centro de cada cruz usando a Lupa 4x!", "Alvos Carregados", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(this, $"G-Code salvo em:\n{tempFile}\nCarregue o arquivo no LaserGRBL para gravar os alvos.", "Alvos Salvos", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Erro ao gerar arquivo: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnFrameReceivedFromThread()
        {
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(RefreshLiveFrame)); } catch { }
            }
            else
            {
                RefreshLiveFrame();
            }
        }

        private void RefreshLiveFrame()
        {
            using (Bitmap raw = CameraOverlayManager.RawFrame)
            {
                if (raw != null)
                {
                    try
                    {
                        Bitmap fresh = new Bitmap(raw.Width, raw.Height, PixelFormat.Format32bppArgb);
                        using (Graphics g = Graphics.FromImage(fresh))
                        {
                            g.DrawImage(raw, 0, 0, raw.Width, raw.Height);
                        }

                        lock (mRawLock)
                        {
                            mCurrentRaw = fresh;
                        }

                        if (IsHandleCreated)
                        {
                            try { BeginInvoke(new Action(() => { pbRaw.Invalidate(); UpdateRectifiedPreview(); })); } catch { }
                        }
                    }
                    catch { }
                }
            }
        }

        private void LoadCurrentPoints()
        {
            mNormPoints = (PointF[])CameraOverlayManager.CalibrationPoints.Clone();

            using (Bitmap raw = CameraOverlayManager.RawFrame)
            {
                if (raw != null)
                {
                    try
                    {
                        Bitmap fresh = new Bitmap(raw.Width, raw.Height, PixelFormat.Format32bppArgb);
                        using (Graphics g = Graphics.FromImage(fresh))
                        {
                            g.DrawImage(raw, 0, 0, raw.Width, raw.Height);
                        }
                        lock (mRawLock)
                        {
                            mCurrentRaw = fresh;
                        }
                    }
                    catch
                    {
                        CreatePlaceholder();
                    }
                }
                else
                {
                    CreatePlaceholder();
                }
            }
        }

        private void CreatePlaceholder()
        {
            Bitmap place = new Bitmap(800, 600, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(place))
            {
                g.Clear(Color.FromArgb(40, 44, 52));
                using (Pen p = new Pen(Color.FromArgb(80, 90, 110), 1f))
                {
                    for (int x = 0; x < 800; x += 40) g.DrawLine(p, x, 0, x, 600);
                    for (int y = 0; y < 600; y += 40) g.DrawLine(p, 0, y, 800, y);
                }
                using (Pen pBed = new Pen(Color.Cyan, 2f))
                {
                    g.DrawRectangle(pBed, 60, 50, 680, 500);
                }
                g.DrawString("Simulação da Mesa do Laser (Conecte seu celular para ver a câmera real)", new Font("Segoe UI", 11), Brushes.White, 80, 80);
            }
            lock (mRawLock)
            {
                mCurrentRaw = place;
            }
        }

        private PointF NormToScreen(PointF norm)
        {
            return new PointF(norm.X * pbRaw.Width, norm.Y * pbRaw.Height);
        }

        private PointF ScreenToNorm(PointF screen)
        {
            float nx = Math.Max(0f, Math.Min(1f, screen.X / pbRaw.Width));
            float ny = Math.Max(0f, Math.Min(1f, screen.Y / pbRaw.Height));
            return new PointF(nx, ny);
        }

        private void PbRaw_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            lock (mRawLock)
            {
                if (mCurrentRaw != null)
                {
                    try
                    {
                        e.Graphics.InterpolationMode = InterpolationMode.Bilinear;
                        e.Graphics.DrawImage(mCurrentRaw, new Rectangle(0, 0, pbRaw.Width, pbRaw.Height));
                    }
                    catch (Exception ex)
                    {
                        LaserCameraServer.Log($"⚠️ Renderização: {ex.Message}");
                    }
                }

                // Draw bounding quadrilateral polygon
                PointF[] screenPts = new PointF[4];
                for (int i = 0; i < 4; i++) screenPts[i] = NormToScreen(mNormPoints[i]);

                using (Brush fillBrush = new SolidBrush(Color.FromArgb(40, 33, 150, 243)))
                using (Pen borderPen = new Pen(Color.Cyan, 2f) { DashStyle = DashStyle.Dash })
                {
                    e.Graphics.FillPolygon(fillBrush, screenPts);
                    e.Graphics.DrawPolygon(borderPen, screenPts);
                }

                // Draw 4 corner target handles
                for (int i = 0; i < 4; i++)
                {
                    PointF pt = screenPts[i];
                    float radius = 10f;
                    RectangleF rect = new RectangleF(pt.X - radius, pt.Y - radius, radius * 2, radius * 2);

                    using (Brush b = new SolidBrush(MarkerColors[i]))
                    using (Pen p = new Pen(Color.White, 2.5f))
                    {
                        e.Graphics.FillEllipse(b, rect);
                        e.Graphics.DrawEllipse(p, rect);
                    }

                    // Label P1..P4
                    string txt = $"P{i + 1}";
                    using (Font f = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    using (Brush bText = new SolidBrush(Color.White))
                    {
                        e.Graphics.DrawString(txt, f, bText, pt.X + radius + 2, pt.Y - radius);
                    }
                }

                // 4x Magnification Loupe with safe bounds clamping
                if (mShowLoupe && mCurrentRaw != null && mCurrentRaw.Width > 10 && mCurrentRaw.Height > 10)
                {
                    int activeIndex = mDraggingIndex >= 0 ? mDraggingIndex : mHoverIndex;
                    PointF targetPt = activeIndex >= 0 ? screenPts[activeIndex] : mMouseScreenPos;
                    PointF normPt = activeIndex >= 0 ? mNormPoints[activeIndex] : ScreenToNorm(targetPt);

                    int loupeSize = 136;
                    // Position loupe at top-right, or top-left if cursor is in top-right
                    int loupeX = targetPt.X > pbRaw.Width - 170 && targetPt.Y < 170 ? 14 : pbRaw.Width - loupeSize - 14;
                    int loupeY = 14;

                    Rectangle loupeRect = new Rectangle(loupeX, loupeY, loupeSize, loupeSize);

                    using (SolidBrush bg = new SolidBrush(Color.FromArgb(235, 15, 23, 42)))
                    using (Pen borderPen = new Pen(activeIndex >= 0 ? MarkerColors[activeIndex] : Color.Cyan, 2.5f))
                    {
                        e.Graphics.FillRectangle(bg, loupeRect);

                        try
                        {
                            int cropSize = 36;
                            int rawW = mCurrentRaw.Width;
                            int rawH = mCurrentRaw.Height;
                            int rawCenterX = (int)(normPt.X * rawW);
                            int rawCenterY = (int)(normPt.Y * rawH);

                            int cropX = Math.Max(0, Math.Min(rawW - cropSize, rawCenterX - cropSize / 2));
                            int cropY = Math.Max(0, Math.Min(rawH - cropSize, rawCenterY - cropSize / 2));
                            int cropW = Math.Max(1, Math.Min(cropSize, rawW - cropX));
                            int cropH = Math.Max(1, Math.Min(cropSize, rawH - cropY));

                            if (cropW > 0 && cropH > 0 && cropX >= 0 && cropY >= 0 && cropX + cropW <= rawW && cropY + cropH <= rawH)
                            {
                                Rectangle rawCrop = new Rectangle(cropX, cropY, cropW, cropH);
                                e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                                e.Graphics.DrawImage(mCurrentRaw, new Rectangle(loupeRect.X + 3, loupeRect.Y + 3, loupeRect.Width - 6, loupeRect.Height - 6), rawCrop, GraphicsUnit.Pixel);
                            }
                        }
                        catch { }

                        // Central crosshair
                        int cx = loupeRect.X + loupeRect.Width / 2;
                        int cy = loupeRect.Y + loupeRect.Height / 2;
                        using (Pen redPen = new Pen(Color.Red, 1.5f))
                        using (Pen whitePen = new Pen(Color.White, 1f) { DashStyle = DashStyle.Dot })
                        {
                            e.Graphics.DrawLine(redPen, cx - 16, cy, cx + 16, cy);
                            e.Graphics.DrawLine(redPen, cx, cy - 16, cx, cy + 16);
                            e.Graphics.DrawLine(whitePen, cx - 16, cy, cx + 16, cy);
                            e.Graphics.DrawLine(whitePen, cx, cy - 16, cx, cy + 16);
                            e.Graphics.DrawRectangle(redPen, cx - 3, cy - 3, 6, 6);
                        }

                        e.Graphics.DrawRectangle(borderPen, loupeRect);

                        string title = activeIndex >= 0 ? $"🔍 Lupa 4x [P{activeIndex + 1}]" : "🔍 Lupa 4x";
                        using (Font f = new Font("Segoe UI", 8f, FontStyle.Bold))
                        using (Brush textB = new SolidBrush(Color.White))
                        using (SolidBrush tagBg = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
                        {
                            e.Graphics.FillRectangle(tagBg, loupeRect.X + 4, loupeRect.Y + 4, 92, 18);
                            e.Graphics.DrawString(title, f, textB, loupeRect.X + 6, loupeRect.Y + 5);
                        }
                    }
                }
            }
        }

        private void PbRaw_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            float clickDist = 20f;
            for (int i = 0; i < 4; i++)
            {
                PointF pt = NormToScreen(mNormPoints[i]);
                float dx = pt.X - e.X;
                float dy = pt.Y - e.Y;
                if (Math.Sqrt(dx * dx + dy * dy) <= clickDist)
                {
                    mDraggingIndex = i;
                    mShowLoupe = true;
                    pbRaw.Invalidate();
                    break;
                }
            }
        }

        private void PbRaw_MouseMove(object sender, MouseEventArgs e)
        {
            mMouseScreenPos = e.Location;

            if (mDraggingIndex >= 0)
            {
                mNormPoints[mDraggingIndex] = ScreenToNorm(new PointF(e.X, e.Y));
                mShowLoupe = true;
                pbRaw.Invalidate();
            }
            else
            {
                // Hover cursor & loupe feedback
                float hoverDist = 24f;
                int hovered = -1;
                for (int i = 0; i < 4; i++)
                {
                    PointF pt = NormToScreen(mNormPoints[i]);
                    float dx = pt.X - e.X;
                    float dy = pt.Y - e.Y;
                    if (Math.Sqrt(dx * dx + dy * dy) <= hoverDist)
                    {
                        hovered = i;
                        break;
                    }
                }
                mHoverIndex = hovered;
                mShowLoupe = hovered >= 0;
                pbRaw.Cursor = hovered >= 0 ? Cursors.Hand : Cursors.Cross;
                pbRaw.Invalidate();
            }
        }

        private void PbRaw_MouseUp(object sender, MouseEventArgs e)
        {
            if (mDraggingIndex >= 0)
            {
                mDraggingIndex = -1;
                UpdateRectifiedPreview();
                pbRaw.Invalidate();
            }
        }

        private void UpdateRectifiedPreview()
        {
            Bitmap snapshot = null;
            lock (mRawLock)
            {
                if (mCurrentRaw != null)
                {
                    try
                    {
                        snapshot = new Bitmap(mCurrentRaw.Width, mCurrentRaw.Height, PixelFormat.Format32bppArgb);
                        using (Graphics g = Graphics.FromImage(snapshot))
                        {
                            g.DrawImage(mCurrentRaw, 0, 0, mCurrentRaw.Width, mCurrentRaw.Height);
                        }
                    }
                    catch { }
                }
            }

            if (snapshot != null)
            {
                try
                {
                    int pw = Math.Max(100, pbRectified.Width);
                    int ph = Math.Max(100, pbRectified.Height);
                    Bitmap warped = CameraOverlayManager.WarpPerspective(snapshot, mNormPoints, pw, ph);
                    if (warped != null)
                    {
                        pbRectified.Image = warped;
                    }
                }
                catch (Exception ex)
                {
                    LaserCameraServer.Log($"⚠️ Prévia retificada: {ex.Message}");
                }
                finally
                {
                    try { snapshot.Dispose(); } catch { }
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            CameraOverlayManager.CalibrationPoints = (PointF[])mNormPoints.Clone();
            CameraOverlayManager.CalibMode = cmbCalibMode.SelectedIndex;
            CameraOverlayManager.HasCalibratedPoints = true;
            CameraOverlayManager.SaveSettings();

            using (Bitmap raw = CameraOverlayManager.RawFrame)
            {
                if (raw != null)
                    CameraOverlayManager.UpdateFrame(raw);
            }

            MessageBox.Show(this, "Calibração dos 4 cantos da mesa salva com sucesso!\nO LaserGRBL agora projeta a visão superior alinhada milimetricamente.", "Calibração Concluída", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            LaserCameraServer.OnFrameReceived -= OnFrameReceivedFromThread;
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                mCurrentRaw?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
