using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using NotusRevitPlugin.Models;

namespace NotusRevitPlugin.Forms
{
    public class HvacReviewForm : Form
    {
        public bool Confirmed { get; private set; }

        public HvacReviewForm(IList<HvacCalculationPreview> previews)
        {
            Text = "Notus - Revisao antes de gravar";
            Width = 1240;
            Height = 680;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);

            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
            Controls.Add(root);

            double totalBtu = previews.Sum(p => p.Result.TotalBTUh);
            double totalTr = previews.Sum(p => p.Result.TotalTR);
            int alerts = previews.Sum(p => p.Alerts.Count);
            int writable = previews.Count(p => p.CanWrite);
            int readOnly = previews.Count - writable;
            Label header = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Text = "Revise os resultados antes de gravar os ambientes editaveis no modelo Revit.\n" +
                       "Ambientes: " + previews.Count + " | Editaveis: " + writable + " | Somente leitura/vinculos: " + readOnly + " | Carga total: " + totalBtu.ToString("N0", new CultureInfo("pt-BR")) + " BTU/h | " + totalTr.ToString("N2", new CultureInfo("pt-BR")) + " TR | Alertas: " + alerts +
                       "\nOs resultados sao estimativos e devem ser validados pelo responsavel tecnico.",
                Font = new Font(Font, FontStyle.Bold)
            };
            root.Controls.Add(header, 0, 0);

            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                RowHeadersVisible = false
            };
            grid.Columns.Add("Nome", "Nome");
            grid.Columns.Add("Origem", "Origem");
            grid.Columns.Add("Gravacao", "Gravacao");
            grid.Columns.Add("Pavimento", "Pavimento");
            grid.Columns.Add("Area", "Area m2");
            grid.Columns.Add("Volume", "Volume m3");
            grid.Columns.Add("BTU", "BTU/h");
            grid.Columns.Add("TR", "TR");
            grid.Columns.Add("SHR", "SHR");
            grid.Columns.Add("Insuflada", "Insuflada m3/h");
            grid.Columns.Add("Externa", "Externa m3/h");
            grid.Columns.Add("ACH", "ACH");
            grid.Columns.Add("Alertas", "Alertas tecnicos");

            foreach (HvacCalculationPreview p in previews)
            {
                string origin = p.Input.IsLinkedElement ? "Vinculo: " + p.Input.SourceDocumentTitle : "Modelo ativo";
                string writeState = p.CanWrite ? "Editavel" : "Somente leitura";
                grid.Rows.Add(
                    p.Input.Name,
                    origin,
                    writeState,
                    p.Input.Level,
                    p.Input.AreaM2.ToString("N2", new CultureInfo("pt-BR")),
                    p.Input.VolumeM3.ToString("N2", new CultureInfo("pt-BR")),
                    p.Result.TotalBTUh.ToString("N0", new CultureInfo("pt-BR")),
                    p.Result.TotalTR.ToString("N2", new CultureInfo("pt-BR")),
                    p.Result.SHR.ToString("N2", new CultureInfo("pt-BR")),
                    p.Result.SupplyAirflowM3h.ToString("N0", new CultureInfo("pt-BR")),
                    p.Result.ExternalAirflowM3h.ToString("N0", new CultureInfo("pt-BR")),
                    p.Result.AirChangesHour.ToString("N2", new CultureInfo("pt-BR")),
                    p.Alerts.Count == 0 ? "" : string.Join("; ", p.Alerts));
            }
            grid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            root.Controls.Add(grid, 0, 1);

            FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            Button save = new Button { Text = "Gravar e gerar tabela", Width = 170, Height = 34 };
            Button cancel = new Button { Text = "Cancelar", Width = 100, Height = 34 };
            save.Click += (s, e) => { Confirmed = true; DialogResult = DialogResult.OK; Close(); };
            cancel.Click += (s, e) => { Confirmed = false; DialogResult = DialogResult.Cancel; Close(); };
            buttons.Controls.Add(save);
            buttons.Controls.Add(cancel);
            root.Controls.Add(buttons, 0, 2);
        }
    }
}
