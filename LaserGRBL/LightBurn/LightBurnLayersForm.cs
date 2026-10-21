using LaserGRBL.Icons;
using LaserGRBL.UserControls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace LaserGRBL.LightBurn
{
    public class LightBurnLayersForm : Form
    {
        private GrblCore mCore;
        private DataGridView dgvLayers;
        private ComboBox cbPresets;
        private Button btnApply;
        private Button btnClose;
        private Button btnReset;
        private Button btnAddLayer;
        private Label lblPreset;
        private Label lblHeaderInfo;
        private Panel pnlHeader;
        private Panel pnlFooter;

        public LightBurnLayersForm(GrblCore core)
        {
            mCore = core;
            InitializeComponent();
            ApplyMaterialTheme();
            LoadPresets();
            PopulateTable();
        }

        public static void CreateAndShowDialog(GrblCore core)
        {
            using (LightBurnLayersForm f = new LightBurnLayersForm(core))
            {
                f.ShowDialog(FormsHelper.MainForm);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "LightBurn - Gerenciador de Camadas e Cores (Layers & Settings)";
            this.Size = new Size(880, 520);
            this.MinimumSize = new Size(750, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowIcon = false;

            pnlHeader = new Panel() { Dock = DockStyle.Top, Height = 64, Padding = new Padding(12) };
            pnlFooter = new Panel() { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(12) };

            lblHeaderInfo = new Label()
            {
                Text = "Configure velocidade, potência, passes e Air Assist por camada de cor (estilo LightBurn).",
                Location = new Point(12, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
            };

            lblPreset = new Label()
            {
                Text = "Preset de Material:",
                Location = new Point(12, 36),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            cbPresets = new ComboBox()
            {
                Location = new Point(135, 33),
                Width = 240,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f)
            };
            cbPresets.SelectedIndexChanged += CbPresets_SelectedIndexChanged;

            btnReset = new Button()
            {
                Text = "Restaurar Padrão",
                Location = new Point(385, 32),
                Width = 130,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f)
            };
            btnReset.Click += (s, e) =>
            {
                LightBurnLayerManager.ResetToDefaults();
                PopulateTable();
            };

            btnAddLayer = new Button()
            {
                Text = "+ Nova Camada",
                Location = new Point(525, 32),
                Width = 110,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f)
            };
            btnAddLayer.Click += BtnAddLayer_Click;

            pnlHeader.Controls.AddRange(new Control[] { lblHeaderInfo, lblPreset, cbPresets, btnReset, btnAddLayer });

            dgvLayers = new DataGridView()
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = ColorScheme.FormBackColor,
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 9f),
                RowTemplate = { Height = 28 }
            };
            dgvLayers.CellPainting += DgvLayers_CellPainting;

            btnApply = new Button()
            {
                Text = "✓ Aplicar ao G-Code Atual",
                Location = new Point(12, 12),
                Width = 200,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnApply.Click += BtnApply_Click;

            btnClose = new Button()
            {
                Text = "Fechar",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(765, 12),
                Width = 90,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
            btnClose.Click += (s, e) => this.Close();

            pnlFooter.Controls.AddRange(new Control[] { btnApply, btnClose });

            this.Controls.Add(dgvLayers);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);
        }

        private void ApplyMaterialTheme()
        {
            this.BackColor = ColorScheme.FormBackColor;
            this.ForeColor = ColorScheme.FormForeColor;
            ThemeMgr.SetTheme(this, true);

            pnlHeader.BackColor = ColorScheme.FormBackColor;
            pnlFooter.BackColor = ColorScheme.FormBackColor;

            lblHeaderInfo.ForeColor = ColorScheme.FormForeColor;
            lblPreset.ForeColor = ColorScheme.FormForeColor;

            btnApply.BackColor = ColorScheme.PressedButtons;
            btnApply.ForeColor = Color.White;
            btnApply.FlatAppearance.BorderColor = ColorScheme.PressedButtons;

            btnClose.BackColor = ColorScheme.FormButtonsColor;
            btnClose.ForeColor = ColorScheme.FormForeColor;
            btnClose.FlatAppearance.BorderColor = ColorScheme.ControlsBorder;

            btnReset.BackColor = ColorScheme.FormButtonsColor;
            btnReset.ForeColor = ColorScheme.FormForeColor;
            btnReset.FlatAppearance.BorderColor = ColorScheme.ControlsBorder;

            btnAddLayer.BackColor = ColorScheme.FormButtonsColor;
            btnAddLayer.ForeColor = ColorScheme.FormForeColor;
            btnAddLayer.FlatAppearance.BorderColor = ColorScheme.ControlsBorder;

            cbPresets.BackColor = ColorScheme.LogBackColor;
            cbPresets.ForeColor = ColorScheme.FormForeColor;

            dgvLayers.BackgroundColor = ColorScheme.LogBackColor;
            dgvLayers.GridColor = ColorScheme.ControlsBorder;
            dgvLayers.DefaultCellStyle.BackColor = ColorScheme.LogBackColor;
            dgvLayers.DefaultCellStyle.ForeColor = ColorScheme.FormForeColor;
            dgvLayers.DefaultCellStyle.SelectionBackColor = ColorScheme.MenuHighlightColor;
            dgvLayers.DefaultCellStyle.SelectionForeColor = ColorScheme.FormForeColor;

            dgvLayers.ColumnHeadersDefaultCellStyle.BackColor = ColorScheme.FormButtonsColor;
            dgvLayers.ColumnHeadersDefaultCellStyle.ForeColor = ColorScheme.FormForeColor;
            dgvLayers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvLayers.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        }

        private void LoadPresets()
        {
            cbPresets.Items.Clear();
            cbPresets.Items.Add("-- Selecione um Preset --");
            var presets = LightBurnLayerManager.GetMaterialPresets();
            foreach (var key in presets.Keys)
            {
                cbPresets.Items.Add(key);
            }
            cbPresets.SelectedIndex = 0;
        }

        private void PopulateTable()
        {
            dgvLayers.Columns.Clear();

            // Ativo (CheckBox)
            DataGridViewCheckBoxColumn colActive = new DataGridViewCheckBoxColumn()
            {
                HeaderText = "Ativo",
                Name = "Active",
                FillWeight = 40
            };
            dgvLayers.Columns.Add(colActive);

            // Cor (Image badge)
            DataGridViewTextBoxColumn colColor = new DataGridViewTextBoxColumn()
            {
                HeaderText = "Cor",
                Name = "Color",
                ReadOnly = true,
                FillWeight = 45
            };
            dgvLayers.Columns.Add(colColor);

            // Código
            DataGridViewTextBoxColumn colCode = new DataGridViewTextBoxColumn()
            {
                HeaderText = "Código",
                Name = "Code",
                ReadOnly = true,
                FillWeight = 50
            };
            dgvLayers.Columns.Add(colCode);

            // Nome
            DataGridViewTextBoxColumn colName = new DataGridViewTextBoxColumn()
            {
                HeaderText = "Nome da Camada",
                Name = "Name",
                FillWeight = 110
            };
            dgvLayers.Columns.Add(colName);

            // Modo (Line / Fill)
            DataGridViewComboBoxColumn colMode = new DataGridViewComboBoxColumn()
            {
                HeaderText = "Modo",
                Name = "Mode",
                FillWeight = 70
            };
            colMode.Items.AddRange("Line", "Fill");
            dgvLayers.Columns.Add(colMode);

            // Velocidade (mm/min)
            DataGridViewTextBoxColumn colSpeed = new DataGridViewTextBoxColumn()
            {
                HeaderText = "Velocidade (mm/min)",
                Name = "Speed",
                FillWeight = 85
            };
            dgvLayers.Columns.Add(colSpeed);

            // Potência Máx (%)
            DataGridViewTextBoxColumn colPower = new DataGridViewTextBoxColumn()
            {
                HeaderText = "Potência (%)",
                Name = "Power",
                FillWeight = 65
            };
            dgvLayers.Columns.Add(colPower);

            // Passes
            DataGridViewTextBoxColumn colPasses = new DataGridViewTextBoxColumn()
            {
                HeaderText = "Passes",
                Name = "Passes",
                FillWeight = 50
            };
            dgvLayers.Columns.Add(colPasses);

            // Passo Z (mm)
            DataGridViewTextBoxColumn colZ = new DataGridViewTextBoxColumn()
            {
                HeaderText = "Passo Z (mm)",
                Name = "ZStep",
                FillWeight = 65
            };
            dgvLayers.Columns.Add(colZ);

            // Air Assist (Checkbox)
            DataGridViewCheckBoxColumn colAir = new DataGridViewCheckBoxColumn()
            {
                HeaderText = "Air Assist",
                Name = "AirAssist",
                FillWeight = 55
            };
            dgvLayers.Columns.Add(colAir);

            // Populate rows
            dgvLayers.Rows.Clear();
            foreach (var l in LightBurnLayerManager.CurrentLayers)
            {
                int r = dgvLayers.Rows.Add(l.Output, "", l.Code, l.Name, l.Mode, l.Speed, l.PowerMax, l.Passes, l.ZStep, l.AirAssist);
                dgvLayers.Rows[r].Tag = l;
            }
        }

        private void DgvLayers_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 1) // Column "Color"
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
                var layer = dgvLayers.Rows[e.RowIndex].Tag as LightBurnLayer;
                if (layer != null)
                {
                    Rectangle badge = new Rectangle(e.CellBounds.X + 8, e.CellBounds.Y + 6, 20, 16);
                    using (GraphicsPath p = Tools.Graph.RoundedRect(badge, 3))
                    using (SolidBrush b = new SolidBrush(layer.Color))
                    using (Pen pen = new Pen(ColorScheme.ControlsBorder, 1f))
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        e.Graphics.FillPath(b, p);
                        e.Graphics.DrawPath(pen, p);
                    }
                }
                e.Handled = true;
            }
        }

        private void CbPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = cbPresets.SelectedItem?.ToString();
            var presets = LightBurnLayerManager.GetMaterialPresets();
            if (presets.TryGetValue(selected, out var list))
            {
                LightBurnLayerManager.CurrentLayers.Clear();
                foreach (var item in list)
                    LightBurnLayerManager.CurrentLayers.Add(item);
                PopulateTable();
            }
        }

        private void BtnAddLayer_Click(object sender, EventArgs e)
        {
            int idx = LightBurnLayerManager.CurrentLayers.Count;
            string code = $"C{idx:D2}";
            Color[] palette = new Color[] { Color.Cyan, Color.Lime, Color.Orange, Color.Magenta, Color.Yellow, Color.White };
            Color col = palette[idx % palette.Length];
            var newLayer = new LightBurnLayer(idx, code, $"Nova Camada {code}", col, "Line", 1000, 0, 80, 1, 0.0, true);
            LightBurnLayerManager.CurrentLayers.Add(newLayer);
            PopulateTable();
        }

        private void SyncGridToLayers()
        {
            for (int r = 0; r < dgvLayers.Rows.Count; r++)
            {
                var row = dgvLayers.Rows[r];
                var layer = row.Tag as LightBurnLayer;
                if (layer != null)
                {
                    layer.Output = Convert.ToBoolean(row.Cells["Active"].Value);
                    layer.Name = row.Cells["Name"].Value?.ToString() ?? layer.Name;
                    layer.Mode = row.Cells["Mode"].Value?.ToString() ?? "Line";

                    int.TryParse(row.Cells["Speed"].Value?.ToString(), out int spd);
                    layer.Speed = spd > 0 ? spd : layer.Speed;

                    int.TryParse(row.Cells["Power"].Value?.ToString(), out int pwr);
                    layer.PowerMax = Math.Min(100, Math.Max(0, pwr));

                    int.TryParse(row.Cells["Passes"].Value?.ToString(), out int passes);
                    layer.Passes = Math.Max(1, passes);

                    double.TryParse(row.Cells["ZStep"].Value?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double z);
                    layer.ZStep = z;

                    layer.AirAssist = Convert.ToBoolean(row.Cells["AirAssist"].Value);
                }
            }
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            SyncGridToLayers();

            if (mCore.LoadedFile == null || mCore.LoadedFile.Commands == null || mCore.LoadedFile.Commands.Count == 0)
            {
                MessageBox.Show("Nenhum arquivo de G-Code carregado no LaserGRBL para aplicar as camadas.\nPor favor, carregue um arquivo primeiro.", "LightBurn Layers", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int maxPwm = 1000;
            try
            {
                maxPwm = (int)GrblCore.Configuration.MaxPWM;
            }
            catch { }

            string gcode = LightBurnLayerManager.ApplyLayersToGCode(mCore.LoadedFile, LightBurnLayerManager.CurrentLayers, maxPwm);

            if (!string.IsNullOrEmpty(gcode))
            {
                string tempFile = Path.Combine(Path.GetTempPath(), $"LightBurn_Job_{DateTime.Now:yyyyMMdd_HHmmss}.nc");
                File.WriteAllText(tempFile, gcode);
                mCore.OpenFile(tempFile, false);

                MessageBox.Show("Camadas aplicadas com sucesso ao trabalho!\nVelocidades, potências, passes e Air Assist atualizados no G-Code.", "LightBurn Layers", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
        }
    }
}
