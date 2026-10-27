namespace LaserGRBL.RasterConverter
{
    partial class PhotoStudioForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel PnlHeader;
        private System.Windows.Forms.Label LblTitle;
        private System.Windows.Forms.Label LblSubtitle;
        private System.Windows.Forms.SplitContainer SplitMain;
        private System.Windows.Forms.Panel PnlLeftControls;
        private System.Windows.Forms.Panel PnlCanvasContainer;
        private System.Windows.Forms.Panel PnlCanvas;
        private System.Windows.Forms.Panel PnlTopToolbar;
        private System.Windows.Forms.RadioButton RbViewWood;
        private System.Windows.Forms.RadioButton RbViewDots;
        private System.Windows.Forms.RadioButton RbViewSlate;
        private System.Windows.Forms.RadioButton RbViewSplit;
        private System.Windows.Forms.Button BtnZoomIn;
        private System.Windows.Forms.Button BtnZoomOut;
        private System.Windows.Forms.Button BtnZoomFit;
        private System.Windows.Forms.Button BtnZoom100;
        private System.Windows.Forms.Label LblZoomInfo;

        // Presets
        private System.Windows.Forms.GroupBox GbPresets;
        private System.Windows.Forms.Button BtnPresetPortrait;
        private System.Windows.Forms.Button BtnPresetHighDetail;
        private System.Windows.Forms.Button BtnPresetPet;
        private System.Windows.Forms.Button BtnPresetSketch;
        private System.Windows.Forms.Button BtnPresetSlate;

        // Sliders & Controls
        private System.Windows.Forms.GroupBox GbTuning;
        private System.Windows.Forms.Label LblGammaTitle;
        private System.Windows.Forms.Label LblGammaVal;
        private System.Windows.Forms.TrackBar TbGamma;
        private System.Windows.Forms.Label LblGammaHint;

        private System.Windows.Forms.Label LblSharpTitle;
        private System.Windows.Forms.Label LblSharpVal;
        private System.Windows.Forms.TrackBar TbSharpness;
        private System.Windows.Forms.Label LblSharpHint;

        private System.Windows.Forms.Label LblLocalTitle;
        private System.Windows.Forms.Label LblLocalVal;
        private System.Windows.Forms.TrackBar TbLocalContrast;
        private System.Windows.Forms.Label LblLocalHint;

        private System.Windows.Forms.Label LblBrightTitle;
        private System.Windows.Forms.Label LblBrightVal;
        private System.Windows.Forms.TrackBar TbBrightness;

        private System.Windows.Forms.Label LblContrastTitle;
        private System.Windows.Forms.Label LblContrastVal;
        private System.Windows.Forms.TrackBar TbContrast;

        private System.Windows.Forms.Label LblBgCleanTitle;
        private System.Windows.Forms.Label LblBgCleanVal;
        private System.Windows.Forms.TrackBar TbBgClean;
        private System.Windows.Forms.Label LblBgCleanHint;

        private System.Windows.Forms.Label LblDitherTitle;
        private System.Windows.Forms.ComboBox CbDitherAlgo;

        private System.Windows.Forms.CheckBox ChkInvert;

        // Bottom Actions
        private System.Windows.Forms.Panel PnlBottomActions;
        private System.Windows.Forms.Button BtnApply;
        private System.Windows.Forms.Button BtnReset;
        private System.Windows.Forms.Button BtnCancel;

        private void InitializeComponent()
        {
            this.PnlHeader = new System.Windows.Forms.Panel();
            this.LblTitle = new System.Windows.Forms.Label();
            this.LblSubtitle = new System.Windows.Forms.Label();
            this.SplitMain = new System.Windows.Forms.SplitContainer();
            this.PnlLeftControls = new System.Windows.Forms.Panel();
            this.GbTuning = new System.Windows.Forms.GroupBox();
            this.ChkInvert = new System.Windows.Forms.CheckBox();
            this.CbDitherAlgo = new System.Windows.Forms.ComboBox();
            this.LblDitherTitle = new System.Windows.Forms.Label();
            this.LblBgCleanHint = new System.Windows.Forms.Label();
            this.LblBgCleanVal = new System.Windows.Forms.Label();
            this.TbBgClean = new System.Windows.Forms.TrackBar();
            this.LblBgCleanTitle = new System.Windows.Forms.Label();
            this.LblContrastVal = new System.Windows.Forms.Label();
            this.TbContrast = new System.Windows.Forms.TrackBar();
            this.LblContrastTitle = new System.Windows.Forms.Label();
            this.LblBrightVal = new System.Windows.Forms.Label();
            this.TbBrightness = new System.Windows.Forms.TrackBar();
            this.LblBrightTitle = new System.Windows.Forms.Label();
            this.LblLocalHint = new System.Windows.Forms.Label();
            this.LblLocalVal = new System.Windows.Forms.Label();
            this.TbLocalContrast = new System.Windows.Forms.TrackBar();
            this.LblLocalTitle = new System.Windows.Forms.Label();
            this.LblSharpHint = new System.Windows.Forms.Label();
            this.LblSharpVal = new System.Windows.Forms.Label();
            this.TbSharpness = new System.Windows.Forms.TrackBar();
            this.LblSharpTitle = new System.Windows.Forms.Label();
            this.LblGammaHint = new System.Windows.Forms.Label();
            this.LblGammaVal = new System.Windows.Forms.Label();
            this.TbGamma = new System.Windows.Forms.TrackBar();
            this.LblGammaTitle = new System.Windows.Forms.Label();
            this.GbPresets = new System.Windows.Forms.GroupBox();
            this.BtnPresetSlate = new System.Windows.Forms.Button();
            this.BtnPresetSketch = new System.Windows.Forms.Button();
            this.BtnPresetPet = new System.Windows.Forms.Button();
            this.BtnPresetHighDetail = new System.Windows.Forms.Button();
            this.BtnPresetPortrait = new System.Windows.Forms.Button();
            this.PnlBottomActions = new System.Windows.Forms.Panel();
            this.BtnCancel = new System.Windows.Forms.Button();
            this.BtnReset = new System.Windows.Forms.Button();
            this.BtnApply = new System.Windows.Forms.Button();
            this.PnlCanvasContainer = new System.Windows.Forms.Panel();
            this.PnlCanvas = new System.Windows.Forms.Panel();
            this.PnlTopToolbar = new System.Windows.Forms.Panel();
            this.BtnZoom100 = new System.Windows.Forms.Button();
            this.BtnZoomFit = new System.Windows.Forms.Button();
            this.LblZoomInfo = new System.Windows.Forms.Label();
            this.BtnZoomIn = new System.Windows.Forms.Button();
            this.BtnZoomOut = new System.Windows.Forms.Button();
            this.RbViewSplit = new System.Windows.Forms.RadioButton();
            this.RbViewSlate = new System.Windows.Forms.RadioButton();
            this.RbViewDots = new System.Windows.Forms.RadioButton();
            this.RbViewWood = new System.Windows.Forms.RadioButton();
            this.PnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SplitMain)).BeginInit();
            this.SplitMain.Panel1.SuspendLayout();
            this.SplitMain.Panel2.SuspendLayout();
            this.SplitMain.SuspendLayout();
            this.PnlLeftControls.SuspendLayout();
            this.GbTuning.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TbBgClean)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TbContrast)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TbBrightness)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TbLocalContrast)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TbSharpness)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TbGamma)).BeginInit();
            this.GbPresets.SuspendLayout();
            this.PnlBottomActions.SuspendLayout();
            this.PnlCanvasContainer.SuspendLayout();
            this.PnlTopToolbar.SuspendLayout();
            this.SuspendLayout();
            // 
            // PnlHeader
            // 
            this.PnlHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(28)))));
            this.PnlHeader.Controls.Add(this.LblSubtitle);
            this.PnlHeader.Controls.Add(this.LblTitle);
            this.PnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.PnlHeader.Location = new System.Drawing.Point(0, 0);
            this.PnlHeader.Name = "PnlHeader";
            this.PnlHeader.Padding = new System.Windows.Forms.Padding(16, 10, 16, 10);
            this.PnlHeader.Size = new System.Drawing.Size(1184, 58);
            this.PnlHeader.TabIndex = 0;
            // 
            // LblTitle
            // 
            this.LblTitle.AutoSize = true;
            this.LblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LblTitle.ForeColor = System.Drawing.Color.White;
            this.LblTitle.Location = new System.Drawing.Point(12, 8);
            this.LblTitle.Name = "LblTitle";
            this.LblTitle.Size = new System.Drawing.Size(425, 21);
            this.LblTitle.TabIndex = 0;
            this.LblTitle.Text = "✨ Estúdio de Foto & Retrato HD — LaserGRBL Pro Quality";
            // 
            // LblSubtitle
            // 
            this.LblSubtitle.AutoSize = true;
            this.LblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(190)))), ((int)(((byte)(200)))));
            this.LblSubtitle.Location = new System.Drawing.Point(13, 31);
            this.LblSubtitle.Name = "LblSubtitle";
            this.LblSubtitle.Size = new System.Drawing.Size(730, 15);
            this.LblSubtitle.TabIndex = 1;
            this.LblSubtitle.Text = "Compensação de queima não-linear do diodo (Gama), realce cirúrgico de olhos/cabelo (Unsharp Mask) e simulação em madeira e ardósia.";
            // 
            // SplitMain
            // 
            this.SplitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SplitMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.SplitMain.Location = new System.Drawing.Point(0, 58);
            this.SplitMain.Name = "SplitMain";
            // 
            // SplitMain.Panel1
            // 
            this.SplitMain.Panel1.Controls.Add(this.PnlLeftControls);
            this.SplitMain.Panel1.Controls.Add(this.PnlBottomActions);
            // 
            // SplitMain.Panel2
            // 
            this.SplitMain.Panel2.Controls.Add(this.PnlCanvasContainer);
            this.SplitMain.Panel2.Controls.Add(this.PnlTopToolbar);
            this.SplitMain.Size = new System.Drawing.Size(1184, 703);
            this.SplitMain.SplitterDistance = 370;
            this.SplitMain.TabIndex = 1;
            // 
            // PnlLeftControls
            // 
            this.PnlLeftControls.AutoScroll = true;
            this.PnlLeftControls.Controls.Add(this.GbTuning);
            this.PnlLeftControls.Controls.Add(this.GbPresets);
            this.PnlLeftControls.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PnlLeftControls.Location = new System.Drawing.Point(0, 0);
            this.PnlLeftControls.Name = "PnlLeftControls";
            this.PnlLeftControls.Padding = new System.Windows.Forms.Padding(10);
            this.PnlLeftControls.Size = new System.Drawing.Size(370, 645);
            this.PnlLeftControls.TabIndex = 0;
            // 
            // GbPresets
            // 
            this.GbPresets.Controls.Add(this.BtnPresetSlate);
            this.GbPresets.Controls.Add(this.BtnPresetSketch);
            this.GbPresets.Controls.Add(this.BtnPresetPet);
            this.GbPresets.Controls.Add(this.BtnPresetHighDetail);
            this.GbPresets.Controls.Add(this.BtnPresetPortrait);
            this.GbPresets.Dock = System.Windows.Forms.DockStyle.Top;
            this.GbPresets.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.GbPresets.ForeColor = System.Drawing.Color.White;
            this.GbPresets.Location = new System.Drawing.Point(10, 10);
            this.GbPresets.Name = "GbPresets";
            this.GbPresets.Size = new System.Drawing.Size(333, 140);
            this.GbPresets.TabIndex = 0;
            this.GbPresets.TabStop = false;
            this.GbPresets.Text = "⚡ Pré-configurações Otimizadas (1 Clique)";
            // 
            // BtnPresetPortrait
            // 
            this.BtnPresetPortrait.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnPresetPortrait.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.BtnPresetPortrait.ForeColor = System.Drawing.Color.White;
            this.BtnPresetPortrait.Location = new System.Drawing.Point(10, 24);
            this.BtnPresetPortrait.Name = "BtnPresetPortrait";
            this.BtnPresetPortrait.Size = new System.Drawing.Size(150, 32);
            this.BtnPresetPortrait.TabIndex = 0;
            this.BtnPresetPortrait.Text = "👤 Retrato / Rosto Humano";
            this.BtnPresetPortrait.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.BtnPresetPortrait.UseVisualStyleBackColor = true;
            this.BtnPresetPortrait.Click += new System.EventHandler(this.BtnPresetPortrait_Click);
            // 
            // BtnPresetHighDetail
            // 
            this.BtnPresetHighDetail.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnPresetHighDetail.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.BtnPresetHighDetail.ForeColor = System.Drawing.Color.White;
            this.BtnPresetHighDetail.Location = new System.Drawing.Point(168, 24);
            this.BtnPresetHighDetail.Name = "BtnPresetHighDetail";
            this.BtnPresetHighDetail.Size = new System.Drawing.Size(155, 32);
            this.BtnPresetHighDetail.TabIndex = 1;
            this.BtnPresetHighDetail.Text = "🖼 Foto Geral HD";
            this.BtnPresetHighDetail.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.BtnPresetHighDetail.UseVisualStyleBackColor = true;
            this.BtnPresetHighDetail.Click += new System.EventHandler(this.BtnPresetHighDetail_Click);
            // 
            // BtnPresetPet
            // 
            this.BtnPresetPet.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnPresetPet.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.BtnPresetPet.ForeColor = System.Drawing.Color.White;
            this.BtnPresetPet.Location = new System.Drawing.Point(10, 62);
            this.BtnPresetPet.Name = "BtnPresetPet";
            this.BtnPresetPet.Size = new System.Drawing.Size(150, 32);
            this.BtnPresetPet.TabIndex = 2;
            this.BtnPresetPet.Text = "🐾 Pet / Pelagem & Animais";
            this.BtnPresetPet.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.BtnPresetPet.UseVisualStyleBackColor = true;
            this.BtnPresetPet.Click += new System.EventHandler(this.BtnPresetPet_Click);
            // 
            // BtnPresetSketch
            // 
            this.BtnPresetSketch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnPresetSketch.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.BtnPresetSketch.ForeColor = System.Drawing.Color.White;
            this.BtnPresetSketch.Location = new System.Drawing.Point(168, 62);
            this.BtnPresetSketch.Name = "BtnPresetSketch";
            this.BtnPresetSketch.Size = new System.Drawing.Size(155, 32);
            this.BtnPresetSketch.TabIndex = 3;
            this.BtnPresetSketch.Text = "🖋 Traço Fino & Desenho";
            this.BtnPresetSketch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.BtnPresetSketch.UseVisualStyleBackColor = true;
            this.BtnPresetSketch.Click += new System.EventHandler(this.BtnPresetSketch_Click);
            // 
            // BtnPresetSlate
            // 
            this.BtnPresetSlate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnPresetSlate.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.BtnPresetSlate.ForeColor = System.Drawing.Color.White;
            this.BtnPresetSlate.Location = new System.Drawing.Point(10, 100);
            this.BtnPresetSlate.Name = "BtnPresetSlate";
            this.BtnPresetSlate.Size = new System.Drawing.Size(313, 30);
            this.BtnPresetSlate.TabIndex = 4;
            this.BtnPresetSlate.Text = "🌑 Ardósia / Fundo Preto (Invertido)";
            this.BtnPresetSlate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.BtnPresetSlate.UseVisualStyleBackColor = true;
            this.BtnPresetSlate.Click += new System.EventHandler(this.BtnPresetSlate_Click);
            // 
            // GbTuning
            // 
            this.GbTuning.Controls.Add(this.ChkInvert);
            this.GbTuning.Controls.Add(this.CbDitherAlgo);
            this.GbTuning.Controls.Add(this.LblDitherTitle);
            this.GbTuning.Controls.Add(this.LblBgCleanHint);
            this.GbTuning.Controls.Add(this.LblBgCleanVal);
            this.GbTuning.Controls.Add(this.TbBgClean);
            this.GbTuning.Controls.Add(this.LblBgCleanTitle);
            this.GbTuning.Controls.Add(this.LblContrastVal);
            this.GbTuning.Controls.Add(this.TbContrast);
            this.GbTuning.Controls.Add(this.LblContrastTitle);
            this.GbTuning.Controls.Add(this.LblBrightVal);
            this.GbTuning.Controls.Add(this.TbBrightness);
            this.GbTuning.Controls.Add(this.LblBrightTitle);
            this.GbTuning.Controls.Add(this.LblLocalHint);
            this.GbTuning.Controls.Add(this.LblLocalVal);
            this.GbTuning.Controls.Add(this.TbLocalContrast);
            this.GbTuning.Controls.Add(this.LblLocalTitle);
            this.GbTuning.Controls.Add(this.LblSharpHint);
            this.GbTuning.Controls.Add(this.LblSharpVal);
            this.GbTuning.Controls.Add(this.TbSharpness);
            this.GbTuning.Controls.Add(this.LblSharpTitle);
            this.GbTuning.Controls.Add(this.LblGammaHint);
            this.GbTuning.Controls.Add(this.LblGammaVal);
            this.GbTuning.Controls.Add(this.TbGamma);
            this.GbTuning.Controls.Add(this.LblGammaTitle);
            this.GbTuning.Dock = System.Windows.Forms.DockStyle.Top;
            this.GbTuning.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.GbTuning.ForeColor = System.Drawing.Color.White;
            this.GbTuning.Location = new System.Drawing.Point(10, 150);
            this.GbTuning.Name = "GbTuning";
            this.GbTuning.Size = new System.Drawing.Size(333, 560);
            this.GbTuning.TabIndex = 1;
            this.GbTuning.TabStop = false;
            this.GbTuning.Text = "🛠 Ajustes de Alta Fidelidade (Boas Práticas)";
            // 
            // LblGammaTitle
            // 
            this.LblGammaTitle.AutoSize = true;
            this.LblGammaTitle.Location = new System.Drawing.Point(10, 24);
            this.LblGammaTitle.Name = "LblGammaTitle";
            this.LblGammaTitle.Size = new System.Drawing.Size(175, 15);
            this.LblGammaTitle.TabIndex = 0;
            this.LblGammaTitle.Text = "Curva Gama (Anti-Escurecer):";
            // 
            // LblGammaVal
            // 
            this.LblGammaVal.Location = new System.Drawing.Point(260, 24);
            this.LblGammaVal.Name = "LblGammaVal";
            this.LblGammaVal.Size = new System.Drawing.Size(60, 15);
            this.LblGammaVal.TabIndex = 1;
            this.LblGammaVal.Text = "1.55x";
            this.LblGammaVal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // TbGamma
            // 
            this.TbGamma.Location = new System.Drawing.Point(10, 42);
            this.TbGamma.Maximum = 250;
            this.TbGamma.Minimum = 60;
            this.TbGamma.Name = "TbGamma";
            this.TbGamma.Size = new System.Drawing.Size(310, 45);
            this.TbGamma.TabIndex = 2;
            this.TbGamma.TickFrequency = 20;
            this.TbGamma.Value = 155;
            this.TbGamma.Scroll += new System.EventHandler(this.Slider_Scroll);
            // 
            // LblGammaHint
            // 
            this.LblGammaHint.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            this.LblGammaHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(170)))), ((int)(((byte)(180)))));
            this.LblGammaHint.Location = new System.Drawing.Point(12, 72);
            this.LblGammaHint.Name = "LblGammaHint";
            this.LblGammaHint.Size = new System.Drawing.Size(308, 15);
            this.LblGammaHint.TabIndex = 3;
            this.LblGammaHint.Text = "Compensa o ganho de ponto no corte para pele não ficar preta.";
            // 
            // LblSharpTitle
            // 
            this.LblSharpTitle.AutoSize = true;
            this.LblSharpTitle.Location = new System.Drawing.Point(10, 94);
            this.LblSharpTitle.Name = "LblSharpTitle";
            this.LblSharpTitle.Size = new System.Drawing.Size(183, 15);
            this.LblSharpTitle.TabIndex = 4;
            this.LblSharpTitle.Text = "Nitidez Facial e Olhos (Unsharp):";
            // 
            // LblSharpVal
            // 
            this.LblSharpVal.Location = new System.Drawing.Point(260, 94);
            this.LblSharpVal.Name = "LblSharpVal";
            this.LblSharpVal.Size = new System.Drawing.Size(60, 15);
            this.LblSharpVal.TabIndex = 5;
            this.LblSharpVal.Text = "45%";
            this.LblSharpVal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // TbSharpness
            // 
            this.TbSharpness.Location = new System.Drawing.Point(10, 112);
            this.TbSharpness.Maximum = 100;
            this.TbSharpness.Name = "TbSharpness";
            this.TbSharpness.Size = new System.Drawing.Size(310, 45);
            this.TbSharpness.TabIndex = 6;
            this.TbSharpness.TickFrequency = 10;
            this.TbSharpness.Value = 45;
            this.TbSharpness.Scroll += new System.EventHandler(this.Slider_Scroll);
            // 
            // LblSharpHint
            // 
            this.LblSharpHint.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            this.LblSharpHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(170)))), ((int)(((byte)(180)))));
            this.LblSharpHint.Location = new System.Drawing.Point(12, 142);
            this.LblSharpHint.Name = "LblSharpHint";
            this.LblSharpHint.Size = new System.Drawing.Size(308, 15);
            this.LblSharpHint.TabIndex = 7;
            this.LblSharpHint.Text = "Destaca íris dos olhos, dentes, boca e fios de cabelo.";
            // 
            // LblLocalTitle
            // 
            this.LblLocalTitle.AutoSize = true;
            this.LblLocalTitle.Location = new System.Drawing.Point(10, 164);
            this.LblLocalTitle.Name = "LblLocalTitle";
            this.LblLocalTitle.Size = new System.Drawing.Size(193, 15);
            this.LblLocalTitle.TabIndex = 8;
            this.LblLocalTitle.Text = "Realce de Relevo (Contraste 3D):";
            // 
            // LblLocalVal
            // 
            this.LblLocalVal.Location = new System.Drawing.Point(260, 164);
            this.LblLocalVal.Name = "LblLocalVal";
            this.LblLocalVal.Size = new System.Drawing.Size(60, 15);
            this.LblLocalVal.TabIndex = 9;
            this.LblLocalVal.Text = "30%";
            this.LblLocalVal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // TbLocalContrast
            // 
            this.TbLocalContrast.Location = new System.Drawing.Point(10, 182);
            this.TbLocalContrast.Maximum = 100;
            this.TbLocalContrast.Name = "TbLocalContrast";
            this.TbLocalContrast.Size = new System.Drawing.Size(310, 45);
            this.TbLocalContrast.TabIndex = 10;
            this.TbLocalContrast.TickFrequency = 10;
            this.TbLocalContrast.Value = 30;
            this.TbLocalContrast.Scroll += new System.EventHandler(this.Slider_Scroll);
            // 
            // LblLocalHint
            // 
            this.LblLocalHint.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            this.LblLocalHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(170)))), ((int)(((byte)(180)))));
            this.LblLocalHint.Location = new System.Drawing.Point(12, 212);
            this.LblLocalHint.Name = "LblLocalHint";
            this.LblLocalHint.Size = new System.Drawing.Size(308, 15);
            this.LblLocalHint.TabIndex = 11;
            this.LblLocalHint.Text = "Dá sensação tridimensional ao rosto sem estourar luzes.";
            // 
            // LblBrightTitle
            // 
            this.LblBrightTitle.AutoSize = true;
            this.LblBrightTitle.Location = new System.Drawing.Point(10, 234);
            this.LblBrightTitle.Name = "LblBrightTitle";
            this.LblBrightTitle.Size = new System.Drawing.Size(43, 15);
            this.LblBrightTitle.TabIndex = 12;
            this.LblBrightTitle.Text = "Brilho:";
            // 
            // LblBrightVal
            // 
            this.LblBrightVal.Location = new System.Drawing.Point(260, 234);
            this.LblBrightVal.Name = "LblBrightVal";
            this.LblBrightVal.Size = new System.Drawing.Size(60, 15);
            this.LblBrightVal.TabIndex = 13;
            this.LblBrightVal.Text = "10";
            this.LblBrightVal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // TbBrightness
            // 
            this.TbBrightness.Location = new System.Drawing.Point(10, 252);
            this.TbBrightness.Maximum = 100;
            this.TbBrightness.Minimum = -100;
            this.TbBrightness.Name = "TbBrightness";
            this.TbBrightness.Size = new System.Drawing.Size(310, 45);
            this.TbBrightness.TabIndex = 14;
            this.TbBrightness.TickFrequency = 20;
            this.TbBrightness.Value = 10;
            this.TbBrightness.Scroll += new System.EventHandler(this.Slider_Scroll);
            // 
            // LblContrastTitle
            // 
            this.LblContrastTitle.AutoSize = true;
            this.LblContrastTitle.Location = new System.Drawing.Point(10, 298);
            this.LblContrastTitle.Name = "LblContrastTitle";
            this.LblContrastTitle.Size = new System.Drawing.Size(62, 15);
            this.LblContrastTitle.TabIndex = 15;
            this.LblContrastTitle.Text = "Contraste:";
            // 
            // LblContrastVal
            // 
            this.LblContrastVal.Location = new System.Drawing.Point(260, 298);
            this.LblContrastVal.Name = "LblContrastVal";
            this.LblContrastVal.Size = new System.Drawing.Size(60, 15);
            this.LblContrastVal.TabIndex = 16;
            this.LblContrastVal.Text = "15";
            this.LblContrastVal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // TbContrast
            // 
            this.TbContrast.Location = new System.Drawing.Point(10, 316);
            this.TbContrast.Maximum = 100;
            this.TbContrast.Minimum = -100;
            this.TbContrast.Name = "TbContrast";
            this.TbContrast.Size = new System.Drawing.Size(310, 45);
            this.TbContrast.TabIndex = 17;
            this.TbContrast.TickFrequency = 20;
            this.TbContrast.Value = 15;
            this.TbContrast.Scroll += new System.EventHandler(this.Slider_Scroll);
            // 
            // LblBgCleanTitle
            // 
            this.LblBgCleanTitle.AutoSize = true;
            this.LblBgCleanTitle.Location = new System.Drawing.Point(10, 362);
            this.LblBgCleanTitle.Name = "LblBgCleanTitle";
            this.LblBgCleanTitle.Size = new System.Drawing.Size(161, 15);
            this.LblBgCleanTitle.TabIndex = 18;
            this.LblBgCleanTitle.Text = "Limpeza de Fundo Branco:";
            // 
            // LblBgCleanVal
            // 
            this.LblBgCleanVal.Location = new System.Drawing.Point(260, 362);
            this.LblBgCleanVal.Name = "LblBgCleanVal";
            this.LblBgCleanVal.Size = new System.Drawing.Size(60, 15);
            this.LblBgCleanVal.TabIndex = 19;
            this.LblBgCleanVal.Text = "12";
            this.LblBgCleanVal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // TbBgClean
            // 
            this.TbBgClean.Location = new System.Drawing.Point(10, 380);
            this.TbBgClean.Maximum = 60;
            this.TbBgClean.Name = "TbBgClean";
            this.TbBgClean.Size = new System.Drawing.Size(310, 45);
            this.TbBgClean.TabIndex = 20;
            this.TbBgClean.TickFrequency = 10;
            this.TbBgClean.Value = 12;
            this.TbBgClean.Scroll += new System.EventHandler(this.Slider_Scroll);
            // 
            // LblBgCleanHint
            // 
            this.LblBgCleanHint.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            this.LblBgCleanHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(170)))), ((int)(((byte)(180)))));
            this.LblBgCleanHint.Location = new System.Drawing.Point(12, 410);
            this.LblBgCleanHint.Name = "LblBgCleanHint";
            this.LblBgCleanHint.Size = new System.Drawing.Size(308, 15);
            this.LblBgCleanHint.TabIndex = 21;
            this.LblBgCleanHint.Text = "Elimina pontos fantasmas no fundo para gravação rápida e limpa.";
            // 
            // LblDitherTitle
            // 
            this.LblDitherTitle.AutoSize = true;
            this.LblDitherTitle.Location = new System.Drawing.Point(10, 434);
            this.LblDitherTitle.Name = "LblDitherTitle";
            this.LblDitherTitle.Size = new System.Drawing.Size(188, 15);
            this.LblDitherTitle.TabIndex = 22;
            this.LblDitherTitle.Text = "Algoritmo de Dithering (Pontos):";
            // 
            // CbDitherAlgo
            // 
            this.CbDitherAlgo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CbDitherAlgo.FormattingEnabled = true;
            this.CbDitherAlgo.Location = new System.Drawing.Point(12, 454);
            this.CbDitherAlgo.Name = "CbDitherAlgo";
            this.CbDitherAlgo.Size = new System.Drawing.Size(306, 23);
            this.CbDitherAlgo.TabIndex = 23;
            this.CbDitherAlgo.SelectedIndexChanged += new System.EventHandler(this.CbDitherAlgo_SelectedIndexChanged);
            // 
            // ChkInvert
            // 
            this.ChkInvert.AutoSize = true;
            this.ChkInvert.Location = new System.Drawing.Point(14, 494);
            this.ChkInvert.Name = "ChkInvert";
            this.ChkInvert.Size = new System.Drawing.Size(268, 19);
            this.ChkInvert.TabIndex = 24;
            this.ChkInvert.Text = "Inverter Imagem (Ardósia / Material Escuro)";
            this.ChkInvert.UseVisualStyleBackColor = true;
            this.ChkInvert.CheckedChanged += new System.EventHandler(this.ChkInvert_CheckedChanged);
            // 
            // PnlBottomActions
            // 
            this.PnlBottomActions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(29)))), ((int)(((byte)(32)))));
            this.PnlBottomActions.Controls.Add(this.BtnCancel);
            this.PnlBottomActions.Controls.Add(this.BtnReset);
            this.PnlBottomActions.Controls.Add(this.BtnApply);
            this.PnlBottomActions.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PnlBottomActions.Location = new System.Drawing.Point(0, 645);
            this.PnlBottomActions.Name = "PnlBottomActions";
            this.PnlBottomActions.Padding = new System.Windows.Forms.Padding(10);
            this.PnlBottomActions.Size = new System.Drawing.Size(370, 58);
            this.PnlBottomActions.TabIndex = 1;
            // 
            // BtnApply
            // 
            this.BtnApply.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(17)))), ((int)(((byte)(169)))), ((int)(((byte)(38)))));
            this.BtnApply.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnApply.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnApply.ForeColor = System.Drawing.Color.White;
            this.BtnApply.Location = new System.Drawing.Point(10, 10);
            this.BtnApply.Name = "BtnApply";
            this.BtnApply.Size = new System.Drawing.Size(180, 38);
            this.BtnApply.TabIndex = 0;
            this.BtnApply.Text = "✨ Aplicar ao LaserGRBL";
            this.BtnApply.UseVisualStyleBackColor = false;
            this.BtnApply.Click += new System.EventHandler(this.BtnApply_Click);
            // 
            // BtnReset
            // 
            this.BtnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnReset.ForeColor = System.Drawing.Color.White;
            this.BtnReset.Location = new System.Drawing.Point(198, 10);
            this.BtnReset.Name = "BtnReset";
            this.BtnReset.Size = new System.Drawing.Size(75, 38);
            this.BtnReset.TabIndex = 1;
            this.BtnReset.Text = "Padrão";
            this.BtnReset.UseVisualStyleBackColor = true;
            this.BtnReset.Click += new System.EventHandler(this.BtnReset_Click);
            // 
            // BtnCancel
            // 
            this.BtnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnCancel.ForeColor = System.Drawing.Color.White;
            this.BtnCancel.Location = new System.Drawing.Point(281, 10);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(75, 38);
            this.BtnCancel.TabIndex = 2;
            this.BtnCancel.Text = "Cancelar";
            this.BtnCancel.UseVisualStyleBackColor = true;
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // PnlTopToolbar
            // 
            this.PnlTopToolbar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(40)))), ((int)(((byte)(44)))));
            this.PnlTopToolbar.Controls.Add(this.BtnZoom100);
            this.PnlTopToolbar.Controls.Add(this.BtnZoomFit);
            this.PnlTopToolbar.Controls.Add(this.LblZoomInfo);
            this.PnlTopToolbar.Controls.Add(this.BtnZoomIn);
            this.PnlTopToolbar.Controls.Add(this.BtnZoomOut);
            this.PnlTopToolbar.Controls.Add(this.RbViewSplit);
            this.PnlTopToolbar.Controls.Add(this.RbViewSlate);
            this.PnlTopToolbar.Controls.Add(this.RbViewDots);
            this.PnlTopToolbar.Controls.Add(this.RbViewWood);
            this.PnlTopToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.PnlTopToolbar.Location = new System.Drawing.Point(0, 0);
            this.PnlTopToolbar.Name = "PnlTopToolbar";
            this.PnlTopToolbar.Padding = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.PnlTopToolbar.Size = new System.Drawing.Size(810, 42);
            this.PnlTopToolbar.TabIndex = 0;
            // 
            // RbViewWood
            // 
            this.RbViewWood.Appearance = System.Windows.Forms.Appearance.Button;
            this.RbViewWood.Checked = true;
            this.RbViewWood.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.RbViewWood.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.RbViewWood.ForeColor = System.Drawing.Color.White;
            this.RbViewWood.Location = new System.Drawing.Point(10, 6);
            this.RbViewWood.Name = "RbViewWood";
            this.RbViewWood.Size = new System.Drawing.Size(125, 28);
            this.RbViewWood.TabIndex = 0;
            this.RbViewWood.TabStop = true;
            this.RbViewWood.Text = "🪵 Madeira Real";
            this.RbViewWood.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.RbViewWood.UseVisualStyleBackColor = true;
            this.RbViewWood.CheckedChanged += new System.EventHandler(this.RbViewMode_CheckedChanged);
            // 
            // RbViewDots
            // 
            this.RbViewDots.Appearance = System.Windows.Forms.Appearance.Button;
            this.RbViewDots.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.RbViewDots.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.RbViewDots.ForeColor = System.Drawing.Color.White;
            this.RbViewDots.Location = new System.Drawing.Point(141, 6);
            this.RbViewDots.Name = "RbViewDots";
            this.RbViewDots.Size = new System.Drawing.Size(125, 28);
            this.RbViewDots.TabIndex = 1;
            this.RbViewDots.Text = "⚫ Pontos Laser";
            this.RbViewDots.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.RbViewDots.UseVisualStyleBackColor = true;
            this.RbViewDots.CheckedChanged += new System.EventHandler(this.RbViewMode_CheckedChanged);
            // 
            // RbViewSlate
            // 
            this.RbViewSlate.Appearance = System.Windows.Forms.Appearance.Button;
            this.RbViewSlate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.RbViewSlate.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.RbViewSlate.ForeColor = System.Drawing.Color.White;
            this.RbViewSlate.Location = new System.Drawing.Point(272, 6);
            this.RbViewSlate.Name = "RbViewSlate";
            this.RbViewSlate.Size = new System.Drawing.Size(110, 28);
            this.RbViewSlate.TabIndex = 2;
            this.RbViewSlate.Text = "🪨 Ardósia";
            this.RbViewSlate.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.RbViewSlate.UseVisualStyleBackColor = true;
            this.RbViewSlate.CheckedChanged += new System.EventHandler(this.RbViewMode_CheckedChanged);
            // 
            // RbViewSplit
            // 
            this.RbViewSplit.Appearance = System.Windows.Forms.Appearance.Button;
            this.RbViewSplit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.RbViewSplit.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.RbViewSplit.ForeColor = System.Drawing.Color.White;
            this.RbViewSplit.Location = new System.Drawing.Point(388, 6);
            this.RbViewSplit.Name = "RbViewSplit";
            this.RbViewSplit.Size = new System.Drawing.Size(130, 28);
            this.RbViewSplit.TabIndex = 3;
            this.RbViewSplit.Text = "🌓 Lado a Lado";
            this.RbViewSplit.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.RbViewSplit.UseVisualStyleBackColor = true;
            this.RbViewSplit.CheckedChanged += new System.EventHandler(this.RbViewMode_CheckedChanged);
            // 
            // BtnZoomOut
            // 
            this.BtnZoomOut.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnZoomOut.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnZoomOut.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnZoomOut.ForeColor = System.Drawing.Color.White;
            this.BtnZoomOut.Location = new System.Drawing.Point(540, 6);
            this.BtnZoomOut.Name = "BtnZoomOut";
            this.BtnZoomOut.Size = new System.Drawing.Size(30, 28);
            this.BtnZoomOut.TabIndex = 4;
            this.BtnZoomOut.Text = "−";
            this.BtnZoomOut.UseVisualStyleBackColor = true;
            this.BtnZoomOut.Click += new System.EventHandler(this.BtnZoomOut_Click);
            // 
            // BtnZoomIn
            // 
            this.BtnZoomIn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnZoomIn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnZoomIn.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnZoomIn.ForeColor = System.Drawing.Color.White;
            this.BtnZoomIn.Location = new System.Drawing.Point(626, 6);
            this.BtnZoomIn.Name = "BtnZoomIn";
            this.BtnZoomIn.Size = new System.Drawing.Size(30, 28);
            this.BtnZoomIn.TabIndex = 5;
            this.BtnZoomIn.Text = "+";
            this.BtnZoomIn.UseVisualStyleBackColor = true;
            this.BtnZoomIn.Click += new System.EventHandler(this.BtnZoomIn_Click);
            // 
            // LblZoomInfo
            // 
            this.LblZoomInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.LblZoomInfo.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.LblZoomInfo.ForeColor = System.Drawing.Color.White;
            this.LblZoomInfo.Location = new System.Drawing.Point(573, 6);
            this.LblZoomInfo.Name = "LblZoomInfo";
            this.LblZoomInfo.Size = new System.Drawing.Size(50, 28);
            this.LblZoomInfo.TabIndex = 6;
            this.LblZoomInfo.Text = "100%";
            this.LblZoomInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // BtnZoomFit
            // 
            this.BtnZoomFit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnZoomFit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnZoomFit.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.BtnZoomFit.ForeColor = System.Drawing.Color.White;
            this.BtnZoomFit.Location = new System.Drawing.Point(664, 6);
            this.BtnZoomFit.Name = "BtnZoomFit";
            this.BtnZoomFit.Size = new System.Drawing.Size(65, 28);
            this.BtnZoomFit.TabIndex = 7;
            this.BtnZoomFit.Text = "Ajustar";
            this.BtnZoomFit.UseVisualStyleBackColor = true;
            this.BtnZoomFit.Click += new System.EventHandler(this.BtnZoomFit_Click);
            // 
            // BtnZoom100
            // 
            this.BtnZoom100.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnZoom100.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnZoom100.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.BtnZoom100.ForeColor = System.Drawing.Color.White;
            this.BtnZoom100.Location = new System.Drawing.Point(735, 6);
            this.BtnZoom100.Name = "BtnZoom100";
            this.BtnZoom100.Size = new System.Drawing.Size(65, 28);
            this.BtnZoom100.TabIndex = 8;
            this.BtnZoom100.Text = "100%";
            this.BtnZoom100.UseVisualStyleBackColor = true;
            this.BtnZoom100.Click += new System.EventHandler(this.BtnZoom100_Click);
            // 
            // PnlCanvasContainer
            // 
            this.PnlCanvasContainer.Controls.Add(this.PnlCanvas);
            this.PnlCanvasContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PnlCanvasContainer.Location = new System.Drawing.Point(0, 42);
            this.PnlCanvasContainer.Name = "PnlCanvasContainer";
            this.PnlCanvasContainer.Size = new System.Drawing.Size(810, 661);
            this.PnlCanvasContainer.TabIndex = 1;
            // 
            // PnlCanvas
            // 
            this.PnlCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PnlCanvas.Location = new System.Drawing.Point(0, 0);
            this.PnlCanvas.Name = "PnlCanvas";
            this.PnlCanvas.Size = new System.Drawing.Size(810, 661);
            this.PnlCanvas.TabIndex = 0;
            this.PnlCanvas.Paint += new System.Windows.Forms.PaintEventHandler(this.PnlCanvas_Paint);
            this.PnlCanvas.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PnlCanvas_MouseDown);
            this.PnlCanvas.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PnlCanvas_MouseMove);
            this.PnlCanvas.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PnlCanvas_MouseUp);
            this.PnlCanvas.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.PnlCanvas_MouseWheel);
            // 
            // PhotoStudioForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(33)))), ((int)(((byte)(36)))));
            this.ClientSize = new System.Drawing.Size(1184, 761);
            this.Controls.Add(this.SplitMain);
            this.Controls.Add(this.PnlHeader);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimumSize = new System.Drawing.Size(950, 650);
            this.Name = "PhotoStudioForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Estúdio de Foto & Retrato HD — LaserGRBL";
            this.PnlHeader.ResumeLayout(false);
            this.PnlHeader.PerformLayout();
            this.SplitMain.Panel1.ResumeLayout(false);
            this.SplitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SplitMain)).EndInit();
            this.SplitMain.ResumeLayout(false);
            this.PnlLeftControls.ResumeLayout(false);
            this.GbTuning.ResumeLayout(false);
            this.GbTuning.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TbBgClean)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TbContrast)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TbBrightness)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TbLocalContrast)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TbSharpness)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TbGamma)).EndInit();
            this.GbPresets.ResumeLayout(false);
            this.PnlBottomActions.ResumeLayout(false);
            this.PnlCanvasContainer.ResumeLayout(false);
            this.PnlTopToolbar.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
