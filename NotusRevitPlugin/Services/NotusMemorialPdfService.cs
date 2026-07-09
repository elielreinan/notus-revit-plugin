using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

namespace NotusRevitPlugin.Services
{
    public class NotusMemorialPdfService
    {
        private const double PageWidth = 595;
        private const double PageHeight = 842;

        public string Export(Document doc, string outputPath)
        {
            List<Element> elements = new HvacReportExportService().GetCalculatedElements(doc)
                .OrderByDescending(e => ParseDouble(GetParameterText(e, "Notus_TR")))
                .ToList();

            double totalBtu = elements.Sum(e => ParseDouble(GetParameterText(e, "Notus_Total_BTU")));
            double totalTr = elements.Sum(e => ParseDouble(GetParameterText(e, "Notus_TR")));
            double totalSupply = elements.Sum(e => ParseDouble(GetParameterText(e, "Notus_Supply_m3h")));
            double totalExternal = elements.Sum(e => ParseDouble(GetParameterText(e, "Notus_External_m3h")));
            int alertCount = elements.Count(e => !string.IsNullOrWhiteSpace(GetParameterText(e, "Notus_ValidationAlerts")) && !GetParameterText(e, "Notus_ValidationAlerts").Equals("Sem alertas", StringComparison.OrdinalIgnoreCase));

            List<string> pages = new List<string>();
            pages.Add(BuildCoverPage(doc, elements.Count, totalBtu, totalTr, totalSupply, totalExternal, alertCount));
            pages.Add(BuildSummaryPage(doc, elements, totalBtu, totalTr, totalSupply, totalExternal));
            pages.AddRange(BuildEnvironmentPages(elements));
            if (pages.Count == 2) pages.Add(BuildEmptyEnvironmentPage());

            WritePdf(outputPath, pages);
            return outputPath;
        }

        private string BuildCoverPage(Document doc, int count, double totalBtu, double totalTr, double totalSupply, double totalExternal, int alertCount)
        {
            PdfContent c = new PdfContent();
            c.Line(48, 790, 547, 790, 1.2);
            c.Text(ProductInfo.Name, 48, 720, 38, true);
            c.Text("HVAC", 48, 684, 24, false);
            c.Text(ProductInfo.Tagline, 48, 650, 13, false);
            c.Text("Memorial tecnico de pre-dimensionamento", 48, 594, 18, true);
            c.Text("Projeto: " + Safe(GetProjectName(doc)), 48, 560, 12, false);
            c.Text("Documento: " + Safe(doc.Title), 48, 540, 12, false);
            c.Text("Gerado em: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")), 48, 520, 12, false);
            c.Text("Versao: " + ProductInfo.Version + " | Revit " + ProductInfo.RevitVersion, 48, 500, 12, false);
            c.Box(48, 335, 499, 118);
            c.Text("Resumo do calculo", 68, 424, 14, true);
            c.Text("Ambientes calculados: " + count, 68, 398, 12, false);
            c.Text("Carga total: " + Format(totalBtu, 0) + " BTU/h", 68, 376, 12, false);
            c.Text("Capacidade total: " + Format(totalTr, 2) + " TR", 68, 354, 12, false);
            c.Text("Vazao insuflada total: " + Format(totalSupply, 0) + " m3/h", 300, 398, 12, false);
            c.Text("Vazao externa total: " + Format(totalExternal, 0) + " m3/h", 300, 376, 12, false);
            c.Text("Ambientes com alertas: " + alertCount, 300, 354, 12, false);
            c.Text(ProductInfo.TechnicalNote, 48, 110, 10, false, 96);
            c.Text(ProductInfo.CompatibilityNote, 48, 88, 9, false, 96);
            return c.ToString();
        }

        private string BuildSummaryPage(Document doc, List<Element> elements, double totalBtu, double totalTr, double totalSupply, double totalExternal)
        {
            PdfContent c = NewPage("Resumo tecnico");
            double y = 728;
            c.Text("Totais do projeto", 48, y, 15, true); y -= 28;
            c.Text("Ambientes: " + elements.Count, 58, y, 11, false); y -= 18;
            c.Text("BTU/h total: " + Format(totalBtu, 0), 58, y, 11, false); y -= 18;
            c.Text("TR total: " + Format(totalTr, 2), 58, y, 11, false); y -= 18;
            c.Text("Vazao insuflada total: " + Format(totalSupply, 0) + " m3/h", 58, y, 11, false); y -= 18;
            c.Text("Vazao externa total: " + Format(totalExternal, 0) + " m3/h", 58, y, 11, false); y -= 34;

            c.Text("Premissas registradas", 48, y, 15, true); y -= 24;
            c.Text("Metodo: " + MostCommon(elements, "Notus_Method"), 58, y, 11, false); y -= 18;
            c.Text("Clima: " + MostCommon(elements, "Notus_Climate"), 58, y, 11, false, 92); y -= 34;
            c.Text("Notas tecnicas", 48, y, 15, true); y -= 24;
            c.Text("- Conferir cargas internas, renovacao de ar, orientacao solar, envoltoria e criterios normativos antes de emissao final.", 58, y, 10, false, 96); y -= 28;
            c.Text("- Ambientes de vinculos podem ser calculados para revisao, mas os parametros sao gravados apenas no modelo ativo.", 58, y, 10, false, 96); y -= 28;
            c.Text("- IFC exportado sem IfcSpace exige reexportacao com ambientes ou criacao/revisao de Ambientes Notus manuais.", 58, y, 10, false, 96); y -= 34;

            c.Text("Top ambientes por TR", 48, y, 15, true); y -= 24;
            AddTableHeader(c, y); y -= 18;
            foreach (Element e in elements.Take(10))
            {
                AddEnvironmentRow(c, e, y);
                y -= 18;
            }
            return c.ToString();
        }

        private IEnumerable<string> BuildEnvironmentPages(List<Element> elements)
        {
            List<string> pages = new List<string>();
            int page = 1;
            foreach (List<Element> chunk in Chunk(elements, 28))
            {
                PdfContent c = NewPage("Ambientes calculados - pagina " + page);
                double y = 730;
                AddTableHeader(c, y); y -= 18;
                foreach (Element e in chunk)
                {
                    AddEnvironmentRow(c, e, y);
                    y -= 22;
                }
                pages.Add(c.ToString());
                page++;
            }
            return pages;
        }

        private string BuildEmptyEnvironmentPage()
        {
            PdfContent c = NewPage("Ambientes calculados");
            c.Text("Nenhum ambiente calculado encontrado no modelo ativo.", 48, 720, 12, false);
            c.Text("Execute Calcular HVAC antes de emitir o memorial tecnico.", 48, 700, 12, false);
            return c.ToString();
        }

        private PdfContent NewPage(string title)
        {
            PdfContent c = new PdfContent();
            c.Text(ProductInfo.FullName, 48, 795, 12, true);
            c.Text(title, 48, 766, 20, true);
            c.Line(48, 752, 547, 752, 0.8);
            c.Text("Gerado em " + DateTime.Now.ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")), 390, 795, 9, false);
            return c;
        }

        private void AddTableHeader(PdfContent c, double y)
        {
            c.Line(48, y + 13, 547, y + 13, 0.4);
            c.Text("Pavimento", 50, y, 8.5, true);
            c.Text("Ambiente", 125, y, 8.5, true);
            c.Text("BTU/h", 325, y, 8.5, true);
            c.Text("TR", 388, y, 8.5, true);
            c.Text("SHR", 428, y, 8.5, true);
            c.Text("Alertas", 470, y, 8.5, true);
            c.Line(48, y - 4, 547, y - 4, 0.4);
        }

        private void AddEnvironmentRow(PdfContent c, Element e, double y)
        {
            string alerts = GetParameterText(e, "Notus_ValidationAlerts");
            if (alerts.Equals("Sem alertas", StringComparison.OrdinalIgnoreCase)) alerts = "";
            c.Text(Short(GetLevelName(e), 14), 50, y, 8.3, false);
            c.Text(Short(GetEnvironmentName(e), 36), 125, y, 8.3, false);
            c.Text(Short(GetParameterText(e, "Notus_Total_BTU"), 11), 325, y, 8.3, false);
            c.Text(Short(GetParameterText(e, "Notus_TR"), 8), 388, y, 8.3, false);
            c.Text(Short(GetParameterText(e, "Notus_SHR"), 6), 428, y, 8.3, false);
            c.Text(Short(alerts, 22), 470, y, 8.0, false);
        }

        private void WritePdf(string path, List<string> pageStreams)
        {
            List<byte[]> streams = pageStreams.Select(s => Encoding.ASCII.GetBytes(s)).ToList();
            List<long> offsets = new List<long>();
            int pageCount = streams.Count;
            int firstPageObj = 5;
            int objectCount = 4 + pageCount * 2;

            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (StreamWriter w = new StreamWriter(fs, Encoding.ASCII))
            {
                w.WriteLine("%PDF-1.4"); w.Flush();
                offsets.Add(fs.Position); w.WriteLine("1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj"); w.Flush();

                StringBuilder kids = new StringBuilder();
                for (int i = 0; i < pageCount; i++) kids.Append(firstPageObj + i * 2).Append(" 0 R ");
                offsets.Add(fs.Position); w.WriteLine("2 0 obj << /Type /Pages /Kids [" + kids + "] /Count " + pageCount + " >> endobj"); w.Flush();
                offsets.Add(fs.Position); w.WriteLine("3 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj"); w.Flush();
                offsets.Add(fs.Position); w.WriteLine("4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >> endobj"); w.Flush();

                for (int i = 0; i < pageCount; i++)
                {
                    int pageObj = firstPageObj + i * 2;
                    int contentObj = pageObj + 1;
                    offsets.Add(fs.Position);
                    w.WriteLine(pageObj + " 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 " + PageWidth.ToString(CultureInfo.InvariantCulture) + " " + PageHeight.ToString(CultureInfo.InvariantCulture) + "] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents " + contentObj + " 0 R >> endobj"); w.Flush();
                    offsets.Add(fs.Position);
                    w.WriteLine(contentObj + " 0 obj << /Length " + streams[i].Length + " >> stream"); w.Flush();
                    fs.Write(streams[i], 0, streams[i].Length);
                    w.WriteLine("endstream endobj"); w.Flush();
                }

                long xref = fs.Position;
                w.WriteLine("xref");
                w.WriteLine("0 " + (objectCount + 1));
                w.WriteLine("0000000000 65535 f ");
                foreach (long offset in offsets) w.WriteLine(offset.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n ");
                w.WriteLine("trailer << /Size " + (objectCount + 1) + " /Root 1 0 R >>");
                w.WriteLine("startxref");
                w.WriteLine(xref.ToString(CultureInfo.InvariantCulture));
                w.WriteLine("%%EOF");
            }
        }

        private static IEnumerable<List<Element>> Chunk(List<Element> elements, int size)
        {
            for (int i = 0; i < elements.Count; i += size) yield return elements.Skip(i).Take(size).ToList();
        }

        private string MostCommon(List<Element> elements, string parameter)
        {
            return elements.Select(e => GetParameterText(e, parameter))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .GroupBy(v => v)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? "Nao informado";
        }

        private string GetProjectName(Document doc)
        {
            try
            {
                string name = doc.ProjectInformation != null ? doc.ProjectInformation.Name : "";
                return string.IsNullOrWhiteSpace(name) ? doc.Title : name;
            }
            catch { return doc.Title; }
        }

        private string GetEnvironmentName(Element e)
        {
            string value = GetParameterText(e, "Number", "Numero", "Número");
            string name = GetParameterText(e, "Notus_Manual_Name", "Name", "Nome");
            if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(name)) return value + " - " + name;
            return string.IsNullOrWhiteSpace(name) ? Safe(e.Name) : name;
        }

        private string GetLevelName(Element e)
        {
            try
            {
                string manual = GetParameterText(e, "Notus_Manual_Level");
                if (!string.IsNullOrWhiteSpace(manual)) return manual;
                Element level = e.Document.GetElement(e.LevelId);
                return level != null ? level.Name : "Sem pavimento";
            }
            catch { return "Sem pavimento"; }
        }

        private string GetParameterText(Element element, params string[] names)
        {
            if (element == null) return "";
            foreach (string name in names)
            {
                Parameter p = element.LookupParameter(name);
                if (p == null || !p.HasValue) continue;
                if (p.StorageType == StorageType.String) return p.AsString() ?? "";
                return p.AsValueString() ?? "";
            }
            return "";
        }

        private double ParseDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            value = value.Replace("BTU/h", "").Replace("TR", "").Replace("m3/h", "").Replace("m³/h", "").Trim();
            if (value.Contains(",")) value = value.Replace(".", "").Replace(",", ".");
            double parsed;
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed) ? parsed : 0;
        }

        private string Format(double value, int decimals)
        {
            return value.ToString("N" + decimals, new CultureInfo("pt-BR"));
        }

        private string Short(string text, int max)
        {
            text = Safe(text);
            return text.Length <= max ? text : text.Substring(0, Math.Max(0, max - 3)) + "...";
        }

        private string Safe(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            string normalized = text.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();
            foreach (char c in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == UnicodeCategory.NonSpacingMark) continue;
                if (c >= 32 && c <= 126) sb.Append(c);
            }
            return sb.ToString().Replace("\r", " ").Replace("\n", " ");
        }

        private static string PdfEscape(string text)
        {
            return (text ?? "").Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }

        private sealed class PdfContent
        {
            private readonly StringBuilder _sb = new StringBuilder();

            public void Text(string text, double x, double y, double size, bool bold, int wrapAt = 0)
            {
                if (wrapAt > 0 && text != null && text.Length > wrapAt)
                {
                    foreach (string line in Wrap(text, wrapAt))
                    {
                        Text(line, x, y, size, bold, 0);
                        y -= size + 3;
                    }
                    return;
                }
                string font = bold ? "F2" : "F1";
                _sb.Append("BT /").Append(font).Append(' ').Append(size.ToString("0.##", CultureInfo.InvariantCulture)).Append(" Tf ")
                    .Append(x.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
                    .Append(y.ToString("0.##", CultureInfo.InvariantCulture)).Append(" Td (")
                    .Append(PdfEscape(text ?? "")).AppendLine(") Tj ET");
            }

            public void Line(double x1, double y1, double x2, double y2, double width)
            {
                _sb.Append(width.ToString("0.##", CultureInfo.InvariantCulture)).Append(" w ")
                    .Append(x1.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
                    .Append(y1.ToString("0.##", CultureInfo.InvariantCulture)).Append(" m ")
                    .Append(x2.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
                    .Append(y2.ToString("0.##", CultureInfo.InvariantCulture)).AppendLine(" l S");
            }

            public void Box(double x, double y, double width, double height)
            {
                _sb.Append("0.6 w ")
                    .Append(x.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
                    .Append(y.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
                    .Append(width.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
                    .Append(height.ToString("0.##", CultureInfo.InvariantCulture)).AppendLine(" re S");
            }

            public override string ToString()
            {
                return _sb.ToString();
            }

            private static IEnumerable<string> Wrap(string text, int max)
            {
                List<string> lines = new List<string>();
                string current = "";
                foreach (string word in (text ?? "").Split(' '))
                {
                    if ((current + " " + word).Trim().Length > max)
                    {
                        if (!string.IsNullOrWhiteSpace(current)) lines.Add(current);
                        current = word;
                    }
                    else
                    {
                        current = (current + " " + word).Trim();
                    }
                }
                if (!string.IsNullOrWhiteSpace(current)) lines.Add(current);
                return lines;
            }
        }
    }
}

