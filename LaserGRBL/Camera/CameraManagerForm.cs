using System;
using System.Drawing;
using System.Windows.Forms;

namespace LaserGRBL.Camera
{
    public class CameraManagerForm : Form
    {
        private Label lblUrl;
        private PictureBox pbQrCode;
        private PictureBox pbLivePreview;
        private CheckBox chkEnableOverlay;
        private TrackBar tbOpacity;
        private Label lblOpacityVal;
        private NumericUpDown numBedW;
        private NumericUpDown numBedH;
        private TextBox txtExternalUrl;
        private Label lblStatus;
        private Button btnCalibrate;
        private Button btnCopyUrl;
        private Button btnOpenBrowser;
        private Button btnToggleServer;
        private Label lblClientState;
        private Label lblFrameStats;
        private TextBox txtTerminal;
        private Button btnClearLogs;
        private Timer mRefreshTimer;

        // Fine tuning controls
        private Label lblOffXVal;
        private Label lblOffYVal;
        private Label lblScaleXVal;
        private Label lblScaleYVal;
        private Label lblRotationVal;

        private GrblCore mCore;

        public CameraManagerForm(GrblCore core = null)
        {
            mCore = core;
            InitializeComponent();
            ApplyTheme();
            LoadState();

            LaserCameraServer.OnFrameReceived += OnFrameReceivedFromThread;
            LaserCameraServer.OnLogMessage += OnLogMessageFromThread;

            mRefreshTimer = new Timer { Interval = 300 };
            mRefreshTimer.Tick += RefreshTimer_Tick;
            mRefreshTimer.Start();
        }

        private void InitializeComponent()
        {
            Text = "Câmera Sem Fio do Celular & Sobreposição da Mesa (WiFi Camera Overlay)";
            Size = new Size(1160, 700);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(16, 12, 16, 12)
            };

            Label lblTitle = new Label
            {
                Text = "Câmera do Celular via Wi-Fi (Sem Instalar Aplicativos)",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(96, 165, 250),
                AutoSize = true,
                Location = new Point(16, 10)
            };

            lblStatus = new Label
            {
                Text = "🟢 Servidor Ativo — Pronto para conectar",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Location = new Point(18, 36)
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblStatus);

            // ==========================================
            // 1. LEFT PANEL: Server Control & Connection
            // ==========================================
            Panel pnlLeft = new Panel
            {
                Location = new Point(16, 80),
                Size = new Size(330, 565),
                BackColor = ColorScheme.DarkScheme ? Color.FromArgb(30, 32, 48) : Color.FromArgb(245, 245, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12)
            };

            Label lblQrHeader = new Label
            {
                Text = "1. Conexão & Servidor Wi-Fi",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Location = new Point(12, 10),
                Size = new Size(300, 24)
            };

            btnToggleServer = new Button
            {
                Text = "⏹ Parar Servidor",
                Location = new Point(12, 38),
                Size = new Size(300, 36),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnToggleServer.FlatAppearance.BorderSize = 0;
            btnToggleServer.Click += BtnToggleServer_Click;

            pbQrCode = new PictureBox
            {
                Location = new Point(65, 84),
                Size = new Size(196, 196),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };

            lblUrl = new Label
            {
                Location = new Point(12, 288),
                Size = new Size(304, 28),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(59, 130, 246),
                TextAlign = ContentAlignment.MiddleCenter
            };

            btnCopyUrl = new Button
            {
                Text = "📋 Copiar Link",
                Location = new Point(12, 322),
                Size = new Size(145, 32),
                FlatStyle = FlatStyle.Flat
            };
            btnCopyUrl.Click += (s, e) =>
            {
                try { Clipboard.SetText(lblUrl.Text); MessageBox.Show("Link copiado para a área de transferência!"); } catch { }
            };

            btnOpenBrowser = new Button
            {
                Text = "🌐 Abrir Teste",
                Location = new Point(168, 322),
                Size = new Size(144, 32),
                FlatStyle = FlatStyle.Flat
            };
            btnOpenBrowser.Click += (s, e) =>
            {
                try { System.Diagnostics.Process.Start(lblUrl.Text); } catch { }
            };

            Label lblAppExt = new Label
            {
                Text = "Ou use app externo (IP Webcam / DroidCam):",
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(12, 368),
                Size = new Size(300, 18)
            };

            txtExternalUrl = new TextBox
            {
                Location = new Point(12, 390),
                Size = new Size(220, 24),
                Text = LaserCameraServer.ExternalAppUrl
            };

            Button btnApplyExt = new Button
            {
                Text = "Conectar",
                Location = new Point(238, 388),
                Size = new Size(76, 26),
                FlatStyle = FlatStyle.Flat
            };
            btnApplyExt.Click += (s, e) =>
            {
                LaserCameraServer.ExternalAppUrl = txtExternalUrl.Text.Trim();
                LaserCameraServer.Log($"URL externa configurada: {txtExternalUrl.Text.Trim()}");
            };

            Label lblHelp = new Label
            {
                Text = "💡 Conecte o celular na MESMA rede Wi-Fi do computador e abra a câmera do aparelho para ler o QR Code acima.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(12, 430),
                Size = new Size(300, 110)
            };

            pnlLeft.Controls.Add(lblQrHeader);
            pnlLeft.Controls.Add(btnToggleServer);
            pnlLeft.Controls.Add(pbQrCode);
            pnlLeft.Controls.Add(lblUrl);
            pnlLeft.Controls.Add(btnCopyUrl);
            pnlLeft.Controls.Add(btnOpenBrowser);
            pnlLeft.Controls.Add(lblAppExt);
            pnlLeft.Controls.Add(txtExternalUrl);
            pnlLeft.Controls.Add(btnApplyExt);
            pnlLeft.Controls.Add(lblHelp);

            // ==========================================
            // 2. CENTER PANEL: Live Preview & Settings
            // ==========================================
            Panel pnlCenter = new Panel
            {
                Location = new Point(360, 80),
                Size = new Size(375, 565),
                BackColor = ColorScheme.DarkScheme ? Color.FromArgb(30, 32, 48) : Color.FromArgb(245, 245, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12)
            };

            Label lblPreviewHeader = new Label
            {
                Text = "2. Visualização & Área do Laser",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Location = new Point(12, 10),
                Size = new Size(350, 24)
            };

            pbLivePreview = new PictureBox
            {
                Location = new Point(12, 36),
                Size = new Size(349, 148),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(15, 23, 42)
            };

            Button btnRotLeft = new Button
            {
                Text = "↶ -90°",
                Location = new Point(12, 188),
                Size = new Size(80, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRotLeft.Click += (s, e) =>
            {
                CameraOverlayManager.RotatePhoto(-90);
                if (lblRotationVal != null) lblRotationVal.Text = $"Giro: {CameraOverlayManager.RotationAngle}°";
                UpdateThumbnail();
            };

            lblRotationVal = new Label
            {
                Text = $"Giro: {CameraOverlayManager.RotationAngle}°",
                Location = new Point(96, 188),
                Size = new Size(181, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorScheme.DarkScheme ? Color.FromArgb(40, 44, 66) : Color.FromArgb(230, 235, 245)
            };

            Button btnRotRight = new Button
            {
                Text = "↷ +90°",
                Location = new Point(281, 188),
                Size = new Size(80, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRotRight.Click += (s, e) =>
            {
                CameraOverlayManager.RotatePhoto(90);
                if (lblRotationVal != null) lblRotationVal.Text = $"Giro: {CameraOverlayManager.RotationAngle}°";
                UpdateThumbnail();
            };

            btnCalibrate = new Button
            {
                Text = "🎯 Calibrar Perspectiva & Alvos (LightBurn)...",
                Location = new Point(12, 224),
                Size = new Size(349, 36),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnCalibrate.Click += (s, e) =>
            {
                using (CameraCalibrationForm calib = new CameraCalibrationForm(mCore))
                {
                    calib.ShowDialog(this);
                }
                UpdateThumbnail();
            };

            chkEnableOverlay = new CheckBox
            {
                Text = "Habilitar Sobreposição no Preview do Laser",
                Location = new Point(12, 268),
                Size = new Size(349, 24),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Checked = CameraOverlayManager.Enabled
            };
            chkEnableOverlay.CheckedChanged += (s, e) =>
            {
                CameraOverlayManager.Enabled = chkEnableOverlay.Checked;
                CameraOverlayManager.SaveSettings();
            };

            Label lblOpacityTitle = new Label
            {
                Text = "Opacidade da Imagem:",
                Location = new Point(12, 298),
                Size = new Size(240, 20),
                Font = new Font("Segoe UI", 9f)
            };

            lblOpacityVal = new Label
            {
                Text = $"{(int)(CameraOverlayManager.Opacity * 100)}%",
                Location = new Point(285, 298),
                Size = new Size(60, 20),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };

            tbOpacity = new TrackBar
            {
                Location = new Point(12, 318),
                Size = new Size(349, 36),
                Minimum = 10,
                Maximum = 100,
                Value = (int)(CameraOverlayManager.Opacity * 100),
                TickFrequency = 10
            };
            tbOpacity.Scroll += (s, e) =>
            {
                CameraOverlayManager.Opacity = tbOpacity.Value / 100f;
                lblOpacityVal.Text = $"{tbOpacity.Value}%";
                CameraOverlayManager.SaveSettings();
            };

            Label lblBedTitle = new Label
            {
                Text = "Área da Mesa do Laser (mm):",
                Location = new Point(12, 355),
                Size = new Size(340, 20),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            Label lblW = new Label { Text = "Largura (X):", Location = new Point(12, 380), Size = new Size(75, 22) };
            numBedW = new NumericUpDown
            {
                Location = new Point(88, 378),
                Size = new Size(82, 24),
                Minimum = 50,
                Maximum = 2000,
                Value = (decimal)CameraOverlayManager.BedWidthMm
            };
            numBedW.ValueChanged += (s, e) =>
            {
                CameraOverlayManager.BedWidthMm = (float)numBedW.Value;
                CameraOverlayManager.SaveSettings();
            };

            Label lblH = new Label { Text = "Altura (Y):", Location = new Point(185, 380), Size = new Size(70, 22) };
            numBedH = new NumericUpDown
            {
                Location = new Point(255, 378),
                Size = new Size(82, 24),
                Minimum = 50,
                Maximum = 2000,
                Value = (decimal)CameraOverlayManager.BedHeightMm
            };
            numBedH.ValueChanged += (s, e) =>
            {
                CameraOverlayManager.BedHeightMm = (float)numBedH.Value;
                CameraOverlayManager.SaveSettings();
            };

            // Preset buttons
            Button btnPresetTTS = new Button
            {
                Text = "TTS-55 (300)",
                Location = new Point(12, 412),
                Size = new Size(105, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f)
            };
            btnPresetTTS.Click += (s, e) =>
            {
                numBedW.Value = 300;
                numBedH.Value = 300;
                CameraOverlayManager.BedWidthMm = 300;
                CameraOverlayManager.BedHeightMm = 300;
                CameraOverlayManager.SaveSettings();
                UpdateThumbnail();
            };

            Button btnPreset400 = new Button
            {
                Text = "Padrão (400)",
                Location = new Point(124, 412),
                Size = new Size(105, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f)
            };
            btnPreset400.Click += (s, e) =>
            {
                numBedW.Value = 400;
                numBedH.Value = 400;
                CameraOverlayManager.BedWidthMm = 400;
                CameraOverlayManager.BedHeightMm = 400;
                CameraOverlayManager.SaveSettings();
                UpdateThumbnail();
            };

            Button btnPresetDetect = new Button
            {
                Text = "🔍 Ler GRBL ($130)",
                Location = new Point(236, 412),
                Size = new Size(125, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f)
            };
            btnPresetDetect.Click += (s, e) =>
            {
                try
                {
                    decimal tw = GrblCore.Configuration.TableWidth;
                    decimal th = GrblCore.Configuration.TableHeight;
                    if (tw >= 50 && th >= 50)
                    {
                        numBedW.Value = tw;
                        numBedH.Value = th;
                        CameraOverlayManager.BedWidthMm = (float)tw;
                        CameraOverlayManager.BedHeightMm = (float)th;
                        CameraOverlayManager.SaveSettings();
                        UpdateThumbnail();
                        MessageBox.Show(this, $"Dimensões da máquina lidas do GRBL com sucesso!\nLargura: {tw} mm ($130)\nAltura: {th} mm ($131)", "Dimensões Lidas", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(this, "Não foi possível ler as dimensões do firmware GRBL.\nDefina manualmente nos campos.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch
                {
                    MessageBox.Show(this, "Erro ao consultar configuração do GRBL.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            Button btnSnapshotNow = new Button
            {
                Text = "🔄 Atualizar Foto Agora",
                Location = new Point(12, 452),
                Size = new Size(349, 34),
                FlatStyle = FlatStyle.Flat
            };
            btnSnapshotNow.Click += (s, e) => UpdateThumbnail();

            pnlCenter.Controls.Add(lblPreviewHeader);
            pnlCenter.Controls.Add(pbLivePreview);
            pnlCenter.Controls.Add(btnRotLeft);
            pnlCenter.Controls.Add(lblRotationVal);
            pnlCenter.Controls.Add(btnRotRight);
            pnlCenter.Controls.Add(btnCalibrate);
            pnlCenter.Controls.Add(chkEnableOverlay);
            pnlCenter.Controls.Add(lblOpacityTitle);
            pnlCenter.Controls.Add(lblOpacityVal);
            pnlCenter.Controls.Add(tbOpacity);
            pnlCenter.Controls.Add(lblBedTitle);
            pnlCenter.Controls.Add(lblW);
            pnlCenter.Controls.Add(numBedW);
            pnlCenter.Controls.Add(lblH);
            pnlCenter.Controls.Add(numBedH);
            pnlCenter.Controls.Add(btnPresetTTS);
            pnlCenter.Controls.Add(btnPreset400);
            pnlCenter.Controls.Add(btnPresetDetect);
            pnlCenter.Controls.Add(btnSnapshotNow);

            // ==========================================
            // 3. RIGHT PANEL: Lateral Terminal & Real-Time Fine Adjustment
            // ==========================================
            Panel pnlTerminal = new Panel
            {
                Location = new Point(748, 80),
                Size = new Size(385, 565),
                BackColor = ColorScheme.DarkScheme ? Color.FromArgb(20, 24, 39) : Color.FromArgb(240, 243, 248),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12)
            };

            Label lblTermHeader = new Label
            {
                Text = "3. 📟 Status & Terminal",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Location = new Point(12, 10),
                Size = new Size(355, 24)
            };

            lblClientState = new Label
            {
                Text = "🟡 AGUARDANDO CELULAR...",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(12, 34),
                Size = new Size(359, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(250, 204, 21),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblFrameStats = new Label
            {
                Text = "Frames: 0 | IP: Nenhum",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(12, 64),
                Size = new Size(359, 16),
                TextAlign = ContentAlignment.MiddleLeft
            };

            txtTerminal = new TextBox
            {
                Location = new Point(12, 82),
                Size = new Size(359, 110),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(56, 189, 248),
                Font = new Font("Consolas", 8f),
                BorderStyle = BorderStyle.FixedSingle
            };

            btnClearLogs = new Button
            {
                Text = "🗑️ Limpar",
                Location = new Point(285, 196),
                Size = new Size(86, 24),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f)
            };
            btnClearLogs.Click += (s, e) =>
            {
                LaserCameraServer.ClearLogs();
                txtTerminal.Clear();
            };

            // ==========================================
            // REAL-TIME FINE TUNING PANEL (Offsets & Scale)
            // ==========================================
            Panel pnlFineTune = new Panel
            {
                Location = new Point(12, 226),
                Size = new Size(359, 325),
                BackColor = ColorScheme.DarkScheme ? Color.FromArgb(30, 32, 48) : Color.FromArgb(245, 245, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8)
            };

            Label lblFineHeader = new Label
            {
                Text = "🎯 Ajuste Fino em Tempo Real (Offset)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(8, 8),
                Size = new Size(340, 22),
                ForeColor = Color.FromArgb(59, 130, 246)
            };

            // Offset X
            Label lblOffXTitle = new Label { Text = "Deslocamento X (mm):", Location = new Point(8, 34), Size = new Size(340, 18), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };

            Button btnXMinus5 = new Button { Text = "-5", Location = new Point(8, 54), Size = new Size(46, 26), FlatStyle = FlatStyle.Flat };
            Button btnXMinus1 = new Button { Text = "-1", Location = new Point(58, 54), Size = new Size(46, 26), FlatStyle = FlatStyle.Flat };
            lblOffXVal = new Label { Location = new Point(108, 54), Size = new Size(130, 26), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            Button btnXPlus1 = new Button { Text = "+1", Location = new Point(242, 54), Size = new Size(46, 26), FlatStyle = FlatStyle.Flat };
            Button btnXPlus5 = new Button { Text = "+5", Location = new Point(292, 54), Size = new Size(46, 26), FlatStyle = FlatStyle.Flat };

            btnXMinus5.Click += (s, e) => NudgeOffset(-5f, 0f);
            btnXMinus1.Click += (s, e) => NudgeOffset(-1f, 0f);
            btnXPlus1.Click += (s, e) => NudgeOffset(1f, 0f);
            btnXPlus5.Click += (s, e) => NudgeOffset(5f, 0f);

            // Offset Y
            Label lblOffYTitle = new Label { Text = "Deslocamento Y (mm):", Location = new Point(8, 86), Size = new Size(340, 18), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };

            Button btnYMinus5 = new Button { Text = "-5", Location = new Point(8, 106), Size = new Size(46, 26), FlatStyle = FlatStyle.Flat };
            Button btnYMinus1 = new Button { Text = "-1", Location = new Point(58, 106), Size = new Size(46, 26), FlatStyle = FlatStyle.Flat };
            lblOffYVal = new Label { Location = new Point(108, 106), Size = new Size(130, 26), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            Button btnYPlus1 = new Button { Text = "+1", Location = new Point(242, 106), Size = new Size(46, 26), FlatStyle = FlatStyle.Flat };
            Button btnYPlus5 = new Button { Text = "+5", Location = new Point(292, 106), Size = new Size(46, 26), FlatStyle = FlatStyle.Flat };

            btnYMinus5.Click += (s, e) => NudgeOffset(0f, -5f);
            btnYMinus1.Click += (s, e) => NudgeOffset(0f, -1f);
            btnYPlus1.Click += (s, e) => NudgeOffset(0f, 1f);
            btnYPlus5.Click += (s, e) => NudgeOffset(0f, 5f);

            // Scale X and Y
            Label lblScaleTitle = new Label { Text = "Ajuste de Escala X e Y (%):", Location = new Point(8, 138), Size = new Size(340, 18), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };

            Button btnScaleXMinus = new Button { Text = "-1%", Location = new Point(8, 158), Size = new Size(44, 26), FlatStyle = FlatStyle.Flat };
            lblScaleXVal = new Label { Location = new Point(54, 158), Size = new Size(74, 26), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.5f) };
            Button btnScaleXPlus = new Button { Text = "+1%", Location = new Point(130, 158), Size = new Size(44, 26), FlatStyle = FlatStyle.Flat };

            Button btnScaleYMinus = new Button { Text = "-1%", Location = new Point(180, 158), Size = new Size(44, 26), FlatStyle = FlatStyle.Flat };
            lblScaleYVal = new Label { Location = new Point(226, 158), Size = new Size(74, 26), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.5f) };
            Button btnScaleYPlus = new Button { Text = "+1%", Location = new Point(302, 158), Size = new Size(44, 26), FlatStyle = FlatStyle.Flat };

            btnScaleXMinus.Click += (s, e) => NudgeScale(-0.01f, 0f);
            btnScaleXPlus.Click += (s, e) => NudgeScale(0.01f, 0f);
            btnScaleYMinus.Click += (s, e) => NudgeScale(0f, -0.01f);
            btnScaleYPlus.Click += (s, e) => NudgeScale(0f, 0.01f);

            Button btnResetFine = new Button
            {
                Text = "🔄 Zerar Ajustes de Offset e Escala",
                Location = new Point(8, 194),
                Size = new Size(338, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f)
            };
            btnResetFine.Click += (s, e) =>
            {
                CameraOverlayManager.OffsetX_Mm = 0f;
                CameraOverlayManager.OffsetY_Mm = 0f;
                CameraOverlayManager.ScaleX = 1f;
                CameraOverlayManager.ScaleY = 1f;
                CameraOverlayManager.SaveSettings();
                UpdateFineTuneLabels();
                TriggerRepaint();
            };

            Label lblTipFine = new Label
            {
                Text = "💡 Dica: Se o laser foi para a direita do ponto desejado, clique em [-1] no Deslocamento X até que o alvo no visualizador coincida perfeitamente!",
                Location = new Point(8, 230),
                Size = new Size(340, 75),
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(148, 163, 184)
            };

            pnlFineTune.Controls.Add(lblFineHeader);
            pnlFineTune.Controls.Add(lblOffXTitle);
            pnlFineTune.Controls.Add(btnXMinus5);
            pnlFineTune.Controls.Add(btnXMinus1);
            pnlFineTune.Controls.Add(lblOffXVal);
            pnlFineTune.Controls.Add(btnXPlus1);
            pnlFineTune.Controls.Add(btnXPlus5);

            pnlFineTune.Controls.Add(lblOffYTitle);
            pnlFineTune.Controls.Add(btnYMinus5);
            pnlFineTune.Controls.Add(btnYMinus1);
            pnlFineTune.Controls.Add(lblOffYVal);
            pnlFineTune.Controls.Add(btnYPlus1);
            pnlFineTune.Controls.Add(btnYPlus5);

            pnlFineTune.Controls.Add(lblScaleTitle);
            pnlFineTune.Controls.Add(btnScaleXMinus);
            pnlFineTune.Controls.Add(lblScaleXVal);
            pnlFineTune.Controls.Add(btnScaleXPlus);
            pnlFineTune.Controls.Add(btnScaleYMinus);
            pnlFineTune.Controls.Add(lblScaleYVal);
            pnlFineTune.Controls.Add(btnScaleYPlus);

            pnlFineTune.Controls.Add(btnResetFine);
            pnlFineTune.Controls.Add(lblTipFine);

            UpdateFineTuneLabels();

            pnlTerminal.Controls.Add(lblTermHeader);
            pnlTerminal.Controls.Add(lblClientState);
            pnlTerminal.Controls.Add(lblFrameStats);
            pnlTerminal.Controls.Add(txtTerminal);
            pnlTerminal.Controls.Add(btnClearLogs);
            pnlTerminal.Controls.Add(pnlFineTune);

            // Add all panels to form
            Controls.Add(pnlHeader);
            Controls.Add(pnlLeft);
            Controls.Add(pnlCenter);
            Controls.Add(pnlTerminal);
        }

        private void NudgeOffset(float dx, float dy)
        {
            CameraOverlayManager.OffsetX_Mm += dx;
            CameraOverlayManager.OffsetY_Mm += dy;
            CameraOverlayManager.SaveSettings();
            UpdateFineTuneLabels();
            TriggerRepaint();
        }

        private void NudgeScale(float dsx, float dsy)
        {
            CameraOverlayManager.ScaleX = (float)Math.Round(Math.Max(0.5f, Math.Min(2.0f, CameraOverlayManager.ScaleX + dsx)), 2);
            CameraOverlayManager.ScaleY = (float)Math.Round(Math.Max(0.5f, Math.Min(2.0f, CameraOverlayManager.ScaleY + dsy)), 2);
            CameraOverlayManager.SaveSettings();
            UpdateFineTuneLabels();
            TriggerRepaint();
        }

        private void UpdateFineTuneLabels()
        {
            if (lblOffXVal != null)
                lblOffXVal.Text = $"{CameraOverlayManager.OffsetX_Mm:+0.0;-0.0;0.0} mm";
            if (lblOffYVal != null)
                lblOffYVal.Text = $"{CameraOverlayManager.OffsetY_Mm:+0.0;-0.0;0.0} mm";
            if (lblScaleXVal != null)
                lblScaleXVal.Text = $"X: {(int)(CameraOverlayManager.ScaleX * 100)}%";
            if (lblScaleYVal != null)
                lblScaleYVal.Text = $"Y: {(int)(CameraOverlayManager.ScaleY * 100)}%";
        }

        private void TriggerRepaint()
        {
            using (Bitmap raw = CameraOverlayManager.RawFrame)
            {
                if (raw != null)
                    CameraOverlayManager.UpdateFrame(raw);
            }
        }

        private void ApplyTheme()
        {
            BackColor = ColorScheme.FormBackColor;
            ForeColor = ColorScheme.FormForeColor;
        }

        private void LoadState()
        {
            if (!LaserCameraServer.IsRunning)
            {
                LaserCameraServer.Start();
            }

            UpdateServerUI();
            UpdateThumbnail();
        }

        private void UpdateServerUI()
        {
            if (LaserCameraServer.IsRunning)
            {
                lblStatus.Text = "🟢 Servidor Ativo — Pronto para conectar seu celular";
                lblStatus.ForeColor = Color.FromArgb(74, 222, 128);

                btnToggleServer.Text = "⏹ Parar Servidor";
                btnToggleServer.BackColor = Color.FromArgb(220, 38, 38);

                string url = LaserCameraServer.ServerUrl;
                lblUrl.Text = url;

                try
                {
                    Bitmap qr = LaserCameraServer.GenerateQRCode(url);
                    pbQrCode.Image = qr;
                }
                catch { }
            }
            else
            {
                lblStatus.Text = "🔴 Servidor Parado — Clique em Iniciar para conectar";
                lblStatus.ForeColor = Color.FromArgb(248, 113, 113);

                btnToggleServer.Text = "▶ Iniciar Servidor";
                btnToggleServer.BackColor = Color.FromArgb(22, 163, 74);

                lblUrl.Text = "Servidor Desconectado";

                pbQrCode.Image?.Dispose();
                pbQrCode.Image = null;
            }
        }

        private void BtnToggleServer_Click(object sender, EventArgs e)
        {
            if (LaserCameraServer.IsRunning)
            {
                LaserCameraServer.Stop();
            }
            else
            {
                LaserCameraServer.Start();
            }

            UpdateServerUI();
        }

        private void OnFrameReceivedFromThread()
        {
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(UpdateThumbnail)); } catch { }
            }
            else
            {
                UpdateThumbnail();
            }
        }

        private void UpdateThumbnail()
        {
            Bitmap frame = CameraOverlayManager.WarpedFrame ?? CameraOverlayManager.RawFrame;
            if (frame != null)
            {
                pbLivePreview.Image = frame;
            }
        }

        private void OnLogMessageFromThread(string msg)
        {
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<string>(AppendLog), msg); } catch { }
            }
            else
            {
                AppendLog(msg);
            }
        }

        private void AppendLog(string msg)
        {
            try
            {
                if (txtTerminal != null && !txtTerminal.IsDisposed)
                {
                    txtTerminal.AppendText(msg + Environment.NewLine);
                }
            }
            catch { }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (LaserCameraServer.IsClientConnected)
            {
                lblClientState.Text = "🟢 CELULAR CONECTADO & TRANSMITINDO";
                lblClientState.BackColor = Color.FromArgb(20, 83, 45);
                lblClientState.ForeColor = Color.FromArgb(74, 222, 128);

                lblFrameStats.Text = $"Total Frames: {LaserCameraServer.FramesReceivedCount} | IP: {LaserCameraServer.ConnectedClientIP}";
            }
            else
            {
                lblClientState.Text = "🟡 AGUARDANDO CELULAR...";
                lblClientState.BackColor = Color.FromArgb(30, 41, 59);
                lblClientState.ForeColor = Color.FromArgb(250, 204, 21);

                lblFrameStats.Text = $"Total Frames: {LaserCameraServer.FramesReceivedCount} | IP: {(string.IsNullOrEmpty(LaserCameraServer.ConnectedClientIP) ? "-" : LaserCameraServer.ConnectedClientIP)}";
            }

            if (lblRotationVal != null)
            {
                lblRotationVal.Text = $"Giro: {CameraOverlayManager.RotationAngle}°";
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            LaserCameraServer.OnFrameReceived -= OnFrameReceivedFromThread;
            LaserCameraServer.OnLogMessage -= OnLogMessageFromThread;
            mRefreshTimer?.Stop();
            mRefreshTimer?.Dispose();
            base.OnFormClosing(e);
        }

        public static void CreateAndShowDialog(GrblCore core = null)
        {
            using (CameraManagerForm f = new CameraManagerForm(core))
            {
                f.ShowDialog();
            }
        }
    }
}
