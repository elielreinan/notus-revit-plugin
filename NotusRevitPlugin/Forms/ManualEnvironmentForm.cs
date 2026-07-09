using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using NotusRevitPlugin.Models;
using NotusRevitPlugin.Services;

namespace NotusRevitPlugin.Forms
{
    public class ManualEnvironmentForm : Form
    {
        private readonly TextBox _name = new TextBox();
        private readonly TextBox _level = new TextBox();
        private readonly NumericUpDown _area = NumberBox(0, 100000, 2);
        private readonly NumericUpDown _height = NumberBox(0.5M, 20, 2);
        private readonly NumericUpDown _volume = NumberBox(0, 1000000, 2);
        private readonly NumericUpDown _occupants = NumberBox(0, 100000, 0);
        private readonly ComboBox _type = new ComboBox();
        private readonly NumericUpDown _temp = NumberBox(10, 40, 1);
        private readonly NumericUpDown _rh = NumberBox(20, 90, 0);
        private readonly NumericUpDown _renewal = NumberBox(0, 1000000, 0);
        private readonly TextBox _obs = new TextBox();

        public ManualEnvironmentData Data { get; private set; }

        public ManualEnvironmentForm(ManualEnvironmentData data)
        {
            Data = data ?? new ManualEnvironmentData();
            Text = "Notus - Ambiente HVAC Manual";
            Width = 620;
            Height = 650;
            StartPosition = FormStartPosition.CenterScreen;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Font = new Font("Segoe UI", 9F);

            TableLayoutPanel table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 13,
                Padding = new Padding(16),
                AutoSize = true
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(table);

            Label title = new Label
            {
                Text = "Cadastro completo do Ambiente HVAC Manual",
                Font = new Font(Font, FontStyle.Bold),
                AutoSize = true,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 0, 10)
            };
            table.Controls.Add(title, 0, 0);
            table.SetColumnSpan(title, 2);

            _type.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (string item in HvacCriteriaService.GetCriteriaNames()) _type.Items.Add(item);
            if (_type.Items.Count > 0) _type.SelectedIndex = 0;
            _obs.Multiline = true;
            _obs.Height = 80;
            _obs.ScrollBars = ScrollBars.Vertical;

            Add(table, 1, "Nome do ambiente", _name);
            Add(table, 2, "Pavimento", _level);
            Add(table, 3, "Área (m²)", _area);
            Add(table, 4, "Altura (m)", _height);
            Add(table, 5, "Volume (m³)", _volume);
            Add(table, 6, "Quantidade de pessoas", _occupants);
            Add(table, 7, "Tipo de uso", _type);
            Add(table, 8, "Temperatura interna (°C)", _temp);
            Add(table, 9, "Umidade interna (%)", _rh);
            Add(table, 10, "Renovação de ar (m³/h)", _renewal);
            Add(table, 11, "Observação técnica", _obs);

            FlowLayoutPanel buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Height = 45 };
            Button ok = new Button { Text = "Salvar ambiente", Width = 130, Height = 32, DialogResult = DialogResult.None };
            Button cancel = new Button { Text = "Cancelar", Width = 100, Height = 32, DialogResult = DialogResult.Cancel };
            ok.Click += Ok_Click;
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            table.Controls.Add(buttons, 0, 12);
            table.SetColumnSpan(buttons, 2);

            AcceptButton = ok;
            CancelButton = cancel;
            LoadData(Data);
            _area.ValueChanged += RecalcVolume;
            _height.ValueChanged += RecalcVolume;
        }

        private static NumericUpDown NumberBox(decimal min, decimal max, int decimals)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                DecimalPlaces = decimals,
                Increment = decimals == 0 ? 1 : 0.1M,
                ThousandsSeparator = true,
                Dock = DockStyle.Fill
            };
        }

        private void Add(TableLayoutPanel table, int row, string label, Control control)
        {
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, row == 11 ? 92 : 38));
            table.Controls.Add(new Label { Text = label, AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
            control.Dock = DockStyle.Fill;
            table.Controls.Add(control, 1, row);
        }

        private void LoadData(ManualEnvironmentData data)
        {
            _name.Text = data.Name;
            _level.Text = data.Level;
            _area.Value = Clamp((decimal)data.AreaM2, _area.Minimum, _area.Maximum);
            _height.Value = Clamp((decimal)data.HeightM, _height.Minimum, _height.Maximum);
            _volume.Value = Clamp((decimal)(data.VolumeM3 > 0 ? data.VolumeM3 : data.AreaM2 * data.HeightM), _volume.Minimum, _volume.Maximum);
            _occupants.Value = Clamp(data.Occupants, _occupants.Minimum, _occupants.Maximum);
            SelectType(data.EnvironmentType);
            _temp.Value = Clamp((decimal)data.InternalTempC, _temp.Minimum, _temp.Maximum);
            _rh.Value = Clamp((decimal)data.InternalRelativeHumidity, _rh.Minimum, _rh.Maximum);
            _renewal.Value = Clamp((decimal)data.RenewalAirM3h, _renewal.Minimum, _renewal.Maximum);
            _obs.Text = data.Observation;
        }

        private void SelectType(string value)
        {
            for (int i = 0; i < _type.Items.Count; i++)
            {
                if (string.Equals(_type.Items[i].ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    _type.SelectedIndex = i;
                    return;
                }
            }
        }

        private void RecalcVolume(object sender, EventArgs e)
        {
            decimal v = _area.Value * _height.Value;
            if (v <= _volume.Maximum) _volume.Value = v;
        }

        private void Ok_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_name.Text))
            {
                MessageBox.Show("Informe o nome do ambiente.", "Notus", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_area.Value <= 0 || _height.Value <= 0 || _volume.Value <= 0)
            {
                MessageBox.Show("Área, altura e volume precisam ser maiores que zero.", "Notus", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Data.Name = _name.Text.Trim();
            Data.Level = _level.Text.Trim();
            Data.AreaM2 = (double)_area.Value;
            Data.HeightM = (double)_height.Value;
            Data.VolumeM3 = (double)_volume.Value;
            Data.Occupants = (int)_occupants.Value;
            Data.EnvironmentType = _type.SelectedItem != null ? _type.SelectedItem.ToString() : "Escritório";
            Data.InternalTempC = (double)_temp.Value;
            Data.InternalRelativeHumidity = (double)_rh.Value;
            Data.RenewalAirM3h = (double)_renewal.Value;
            Data.Observation = _obs.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }

        private decimal Clamp(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}


