using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using Autodesk.Revit.DB;

namespace NotusRevitPlugin.Services
{
    public class HvacReportExportService
    {
        private readonly string[] _columns = new string[]
        {
            "ElementId", "Número", "Nome", "Categoria", "Pavimento", "BTU/h", "TR", "SHR", "Vazão insuflada m³/h", "Vazão externa m³/h", "ACH", "Método", "Clima", "Memorial", "Observações", "Alertas"
        };

        public List<Element> GetCalculatedElements(Document doc)
        {
            List<Element> elements = new List<Element>();
            elements.AddRange(GetByCategory(doc, BuiltInCategory.OST_Rooms));
            elements.AddRange(GetByCategory(doc, BuiltInCategory.OST_MEPSpaces));
            elements.AddRange(GetByCategory(doc, BuiltInCategory.OST_GenericModel));
            return elements.Where(e => ParseDouble(GetParameterText(e, "Notus_TR")) > 0).ToList();
        }

        private IEnumerable<Element> GetByCategory(Document doc, BuiltInCategory category)
        {
            try { return new FilteredElementCollector(doc).OfCategory(category).WhereElementIsNotElementType().ToElements(); }
            catch { return new List<Element>(); }
        }

        public string ExportExcelXlsx(Document doc, string outputPath)
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
            List<Element> elements = GetCalculatedElements(doc);
            using (ZipArchive zip = ZipFile.Open(outputPath, ZipArchiveMode.Create))
            {
                Add(zip, "[Content_Types].xml", ContentTypes());
                Add(zip, "_rels/.rels", RootRels());
                Add(zip, "xl/workbook.xml", WorkbookXml());
                Add(zip, "xl/_rels/workbook.xml.rels", WorkbookRels());
                Add(zip, "xl/styles.xml", StylesXml());
                Add(zip, "xl/worksheets/sheet1.xml", SheetXml("Resumo Geral", BuildSummaryRows(doc, elements)));
                Add(zip, "xl/worksheets/sheet2.xml", SheetXml("Ambientes", BuildAllRows(elements)));
                Add(zip, "xl/worksheets/sheet3.xml", SheetXml("Por Pavimento", BuildLevelRows(elements)));
                Add(zip, "xl/worksheets/sheet4.xml", SheetXml("Ambientes Críticos", BuildCriticalRows(elements)));
                Add(zip, "xl/worksheets/sheet5.xml", SheetXml("Critérios", BuildCriteriaRows()));
            }
            return outputPath;
        }

        public string ExportMemorialPdf(Document doc, string outputPath)
        {
            List<Element> elements = GetCalculatedElements(doc);
            List<string> lines = new List<string>();
            double totalBtu = elements.Sum(e => ParseDouble(GetParameterText(e, "Notus_Total_BTU")));
            double totalTr = elements.Sum(e => ParseDouble(GetParameterText(e, "Notus_TR")));
            double totalSupply = elements.Sum(e => ParseDouble(GetParameterText(e, "Notus_Supply_m3h")));
            double totalExternal = elements.Sum(e => ParseDouble(GetParameterText(e, "Notus_External_m3h")));

            lines.Add("Notus - Memorial Técnico");
            lines.Add("Projeto: " + doc.Title);
            lines.Add("Gerado em: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            lines.Add("Ferramenta de apoio para pré-dimensionamento. Validar com responsável técnico.");
            lines.Add("");
            lines.Add("Resumo geral");
            lines.Add("Ambientes: " + elements.Count);
            lines.Add("Carga total: " + Format(totalBtu, 0) + " BTU/h");
            lines.Add("Capacidade total: " + Format(totalTr, 2) + " TR");
            lines.Add("Vazão insuflada total: " + Format(totalSupply, 0) + " m3/h");
            lines.Add("Vazão externa total: " + Format(totalExternal, 0) + " m3/h");
            lines.Add("");
            lines.Add("Tabela por ambiente");
            foreach (Element e in elements.OrderByDescending(e => ParseDouble(GetParameterText(e, "Notus_TR"))))
            {
                string name = GetParameterText(e, "Notus_Manual_Name", "Name", "Nome");
                string level = GetParameterText(e, "Notus_Manual_Level");
                if (string.IsNullOrWhiteSpace(level)) level = GetLevelName(e);
                lines.Add("- " + level + " | " + name + " | " + GetParameterText(e, "Notus_Total_BTU") + " BTU/h | " + GetParameterText(e, "Notus_TR") + " TR | SHR " + GetParameterText(e, "Notus_SHR"));
            }
            lines.Add("");
            lines.Add("Observações técnicas");
            lines.Add("Resultados estimativos. Conferir cargas internas, renovação de ar, dados climáticos, envoltória e critérios aplicáveis antes de documentação oficial ou obra.");

            WriteSimplePdf(outputPath, lines);
            return outputPath;
        }

        public string ExportExcelHtml(Document doc, string outputPath)
        {
            // Mantido por compatibilidade. Preferir ExportExcelXlsx.
            List<Element> elements = GetCalculatedElements(doc);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<html><head><meta charset=\"utf-8\"><style>body{font-family:Segoe UI,Arial}table{border-collapse:collapse;width:100%}th,td{border:1px solid #999;padding:6px;font-size:12px}th{background:#e8eef8}.warn{color:#a00;font-weight:bold}</style></head><body>");
            sb.AppendLine("<h2>Notus - Relatório Excel</h2>");
            sb.AppendLine("<p>Projeto: " + Html(doc.Title) + " | Gerado em: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + "</p>");
            sb.AppendLine("<p class=\"warn\">Resultados estimativos para pré-dimensionamento. Validar com responsável técnico.</p>");
            sb.AppendLine("<table><tr>");
            foreach (string col in _columns) sb.Append("<th>" + Html(col) + "</th>");
            sb.AppendLine("</tr>");
            foreach (Element e in elements)
            {
                sb.AppendLine("<tr>");
                foreach (string value in BuildRow(e)) sb.Append("<td>" + Html(value) + "</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table></body></html>");
            File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(true));
            return outputPath;
        }

        public string ExportMemorialHtml(Document doc, string outputPath)
        {
            List<Element> elements = GetCalculatedElements(doc);
            double totalBtu = elements.Sum(e => ParseDouble(GetParameterText(e, "Notus_Total_BTU")));
            double totalTr = elements.Sum(e => ParseDouble(GetParameterText(e, "Notus_TR")));
            double totalSupply = elements.Sum(e => ParseDouble(GetParameterText(e, "Notus_Supply_m3h")));
            double totalExternal = elements.Sum(e => ParseDouble(GetParameterText(e, "Notus_External_m3h")));

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<html><head><meta charset=\"utf-8\"><style>body{font-family:Segoe UI,Arial;margin:32px;color:#222}h1{color:#1f3b5f}h2{border-bottom:1px solid #ccc;padding-bottom:4px}.card{border:1px solid #ddd;border-radius:8px;padding:12px;margin:12px 0}.warn{background:#fff4d6;border:1px solid #e3c46b;padding:10px;border-radius:6px}table{border-collapse:collapse;width:100%}td,th{border:1px solid #ccc;padding:6px}</style></head><body>");
            sb.AppendLine("<h1>Memorial Técnico Notus</h1>");
            sb.AppendLine("<p>Projeto: " + Html(doc.Title) + "<br>Gerado em: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + "</p>");
            sb.AppendLine("<div class=\"warn\">Os resultados são estimativos e devem ser validados pelo responsável técnico antes de emissão executiva ou uso em obra.</div>");
            sb.AppendLine("<h2>Resumo total</h2>");
            sb.AppendLine("<table><tr><th>Ambientes</th><th>BTU/h</th><th>TR</th><th>Vazão insuflada</th><th>Vazão externa</th></tr>");
            sb.AppendLine("<tr><td>" + elements.Count + "</td><td>" + Format(totalBtu,0) + "</td><td>" + Format(totalTr,2) + "</td><td>" + Format(totalSupply,0) + " m³/h</td><td>" + Format(totalExternal,0) + " m³/h</td></tr></table>");
            sb.AppendLine("<h2>Memorial por ambiente</h2>");
            foreach (Element e in elements)
            {
                string title = GetParameterText(e, "Number", "Número") + " - " + GetParameterText(e, "Notus_Manual_Name", "Name", "Nome");
                sb.AppendLine("<div class=\"card\"><h3>" + Html(title) + "</h3>");
                sb.AppendLine("<p><b>BTU/h:</b> " + Html(GetParameterText(e, "Notus_Total_BTU")) + " | <b>TR:</b> " + Html(GetParameterText(e, "Notus_TR")) + " | <b>SHR:</b> " + Html(GetParameterText(e, "Notus_SHR")) + "</p>");
                sb.AppendLine("<p>" + Html(GetParameterText(e, "Notus_Memorial")) + "</p>");
                sb.AppendLine("<p><b>Notas:</b> " + Html(GetParameterText(e, "Notus_Notes")) + "</p></div>");
            }
            sb.AppendLine("</body></html>");
            File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(true));
            return outputPath;
        }

        public string BuildCriticalRoomsText(Document doc)
        {
            List<Element> elements = GetCalculatedElements(doc).OrderByDescending(e => ParseDouble(GetParameterText(e, "Notus_TR"))).Take(15).ToList();
            if (elements.Count == 0) return "Nenhum ambiente calculado encontrado.";
            StringBuilder sb = new StringBuilder();
            int i = 1;
            foreach (Element e in elements)
            {
                sb.AppendLine(i + ". " + GetParameterText(e, "Number", "Número") + " - " + GetParameterText(e, "Notus_Manual_Name", "Name", "Nome") + " | " + GetParameterText(e, "Notus_TR") + " TR | " + GetParameterText(e, "Notus_Total_BTU") + " BTU/h");
                i++;
            }
            return sb.ToString();
        }

        private List<string[]> BuildSummaryRows(Document doc, List<Element> elements)
        {
            double totalBtu = elements.Sum(e => ParseDouble(GetParameterText(e, "Notus_Total_BTU")));
            double totalTr = elements.Sum(e => ParseDouble(GetParameterText(e, "Notus_TR")));
            double totalSupply = elements.Sum(e => ParseDouble(GetParameterText(e, "Notus_Supply_m3h")));
            double totalExternal = elements.Sum(e => ParseDouble(GetParameterText(e, "Notus_External_m3h")));
            return new List<string[]>
            {
                new [] { "Item", "Valor" },
                new [] { "Projeto", doc.Title },
                new [] { "Data", DateTime.Now.ToString("dd/MM/yyyy HH:mm") },
                new [] { "Ambientes", elements.Count.ToString(CultureInfo.InvariantCulture) },
                new [] { "BTU/h total", Format(totalBtu, 0) },
                new [] { "TR total", Format(totalTr, 2) },
                new [] { "Vazão insuflada total m3/h", Format(totalSupply, 0) },
                new [] { "Vazão externa total m3/h", Format(totalExternal, 0) },
                new [] { "Aviso", "Resultados estimativos. Validar com responsável técnico." }
            };
        }

        private List<string[]> BuildAllRows(List<Element> elements)
        {
            List<string[]> rows = new List<string[]> { _columns };
            foreach (Element e in elements) rows.Add(BuildRow(e));
            return rows;
        }

        private List<string[]> BuildLevelRows(List<Element> elements)
        {
            List<string[]> rows = new List<string[]> { new [] { "Pavimento", "Ambientes", "BTU/h", "TR", "Vazão insuflada m3/h", "Vazão externa m3/h" } };
            foreach (var g in elements.GroupBy(e => GetLevelName(e)).OrderBy(g => g.Key))
            {
                rows.Add(new [] { g.Key, g.Count().ToString(CultureInfo.InvariantCulture), Format(g.Sum(e => ParseDouble(GetParameterText(e, "Notus_Total_BTU"))), 0), Format(g.Sum(e => ParseDouble(GetParameterText(e, "Notus_TR"))), 2), Format(g.Sum(e => ParseDouble(GetParameterText(e, "Notus_Supply_m3h"))), 0), Format(g.Sum(e => ParseDouble(GetParameterText(e, "Notus_External_m3h"))), 0) });
            }
            return rows;
        }

        private List<string[]> BuildCriticalRows(List<Element> elements)
        {
            List<string[]> rows = new List<string[]> { _columns };
            foreach (Element e in elements.OrderByDescending(e => ParseDouble(GetParameterText(e, "Notus_TR"))).Take(30)) rows.Add(BuildRow(e));
            return rows;
        }

        private List<string[]> BuildCriteriaRows()
        {
            List<string[]> rows = new List<string[]> { new [] { "Tipo", "Pessoas m2/pessoa", "Iluminação W/m2", "Equipamentos W/m2", "Renovação L/s pessoa", "Renovação L/s m2", "Temp interna", "UR interna", "Fator segurança", "ACH mínimo", "Aviso" } };
            foreach (string name in HvacCriteriaService.GetCriteriaNames())
            {
                var c = HvacCriteriaService.Get(name);
                rows.Add(new [] { c.Name, Format(c.M2PerPerson, 2), Format(c.LightingWPerM2, 1), Format(c.EquipmentWPerM2, 1), Format(c.RenewalLsPerPerson, 2), Format(c.RenewalLsPerM2, 2), Format(c.InternalTempC, 1), Format(c.InternalRh, 0), Format(c.SafetyFactor, 2), Format(c.MinimumAch, 1), "Perfil técnico baseado em critérios editáveis. Validar com responsável técnico." });
            }
            return rows;
        }

        private string[] BuildRow(Element e)
        {
            return new string[]
            {
                e.Id.Value.ToString(CultureInfo.InvariantCulture),
                GetParameterText(e, "Number", "Número"),
                GetParameterText(e, "Notus_Manual_Name", "Name", "Nome"),
                e.Category != null ? e.Category.Name : "",
                GetLevelName(e),
                GetParameterText(e, "Notus_Total_BTU"),
                GetParameterText(e, "Notus_TR"),
                GetParameterText(e, "Notus_SHR"),
                GetParameterText(e, "Notus_Supply_m3h"),
                GetParameterText(e, "Notus_External_m3h"),
                GetParameterText(e, "Notus_AirChanges_h"),
                GetParameterText(e, "Notus_Method"),
                GetParameterText(e, "Notus_Climate"),
                GetParameterText(e, "Notus_Memorial"),
                GetParameterText(e, "Notus_Notes"),
                GetParameterText(e, "Notus_ValidationAlerts")
            };
        }

        private string SheetXml(string name, List<string[]> rows)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            for (int r = 0; r < rows.Count; r++)
            {
                sb.Append("<row r=\"").Append(r + 1).Append("\">");
                string[] row = rows[r];
                for (int c = 0; c < row.Length; c++)
                {
                    string cellRef = Column(c) + (r + 1).ToString(CultureInfo.InvariantCulture);
                    sb.Append("<c r=\"").Append(cellRef).Append("\" t=\"inlineStr\" s=\"").Append(r == 0 ? "1" : "0").Append("\"><is><t>").Append(Xml(row[c])).Append("</t></is></c>");
                }
                sb.Append("</row>");
            }
            sb.Append("</sheetData></worksheet>");
            return sb.ToString();
        }

        private string WorkbookXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Resumo Geral\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"Ambientes\" sheetId=\"2\" r:id=\"rId2\"/><sheet name=\"Por Pavimento\" sheetId=\"3\" r:id=\"rId3\"/><sheet name=\"Ambientes Críticos\" sheetId=\"4\" r:id=\"rId4\"/><sheet name=\"Critérios\" sheetId=\"5\" r:id=\"rId5\"/></sheets></workbook>";
        }

        private string WorkbookRels()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/><Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet3.xml\"/><Relationship Id=\"rId4\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet4.xml\"/><Relationship Id=\"rId5\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet5.xml\"/><Relationship Id=\"rId6\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
        }

        private string ContentTypes()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet3.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet4.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet5.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>";
        }

        private string RootRels()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        }

        private string StylesXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts><fills count=\"1\"><fill><patternFill patternType=\"none\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/></cellXfs></styleSheet>";
        }

        private void Add(ZipArchive zip, string name, string text)
        {
            ZipArchiveEntry entry = zip.CreateEntry(name);
            using (StreamWriter sw = new StreamWriter(entry.Open(), new UTF8Encoding(false))) sw.Write(text);
        }

        private string Column(int index)
        {
            string col = "";
            index++;
            while (index > 0)
            {
                int rem = (index - 1) % 26;
                col = (char)('A' + rem) + col;
                index = (index - rem - 1) / 26;
            }
            return col;
        }

        private void WriteSimplePdf(string path, List<string> lines)
        {
            List<string> pageLines = new List<string>();
            foreach (string line in lines)
            {
                string l = RemoveNonLatin(line);
                while (l.Length > 105)
                {
                    pageLines.Add(l.Substring(0, 105));
                    l = l.Substring(105);
                }
                pageLines.Add(l);
            }

            StringBuilder content = new StringBuilder();
            content.AppendLine("BT /F1 11 Tf 50 790 Td 14 TL");
            int lineCount = 0;
            foreach (string line in pageLines.Take(52))
            {
                content.Append("(").Append(PdfEscape(line)).AppendLine(") Tj T*");
                lineCount++;
            }
            content.AppendLine("ET");
            byte[] stream = Encoding.ASCII.GetBytes(content.ToString());

            List<long> offsets = new List<long>();
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (StreamWriter w = new StreamWriter(fs, Encoding.ASCII))
            {
                w.WriteLine("%PDF-1.4"); w.Flush();
                offsets.Add(fs.Position); w.WriteLine("1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj"); w.Flush();
                offsets.Add(fs.Position); w.WriteLine("2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj"); w.Flush();
                offsets.Add(fs.Position); w.WriteLine("3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj"); w.Flush();
                offsets.Add(fs.Position); w.WriteLine("4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj"); w.Flush();
                offsets.Add(fs.Position); w.WriteLine("5 0 obj << /Length " + stream.Length + " >> stream"); w.Flush();
                fs.Write(stream, 0, stream.Length); w.WriteLine("endstream endobj"); w.Flush();
                long xref = fs.Position;
                w.WriteLine("xref");
                w.WriteLine("0 6");
                w.WriteLine("0000000000 65535 f ");
                foreach (long o in offsets) w.WriteLine(o.ToString("D10") + " 00000 n ");
                w.WriteLine("trailer << /Size 6 /Root 1 0 R >>");
                w.WriteLine("startxref");
                w.WriteLine(xref);
                w.WriteLine("%%EOF");
            }
        }

        private string RemoveNonLatin(string text)
        {
            if (text == null) return "";
            return text.Replace("³", "3").Replace("²", "2").Replace("—", "-").Replace("–", "-");
        }

        private string PdfEscape(string text) { return (text ?? "").Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)"); }
        private string Xml(string text) { return WebUtility.HtmlEncode(text ?? ""); }
        private string Html(string text) { return WebUtility.HtmlEncode(text ?? ""); }
        private string Format(double value, int decimals) { return value.ToString("N" + decimals, new CultureInfo("pt-BR")); }

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
            value = value.Replace("BTU/h", "").Replace("TR", "").Replace("m³/h", "").Replace("m3/h", "").Trim();
            if (value.Contains(",")) value = value.Replace(".", "").Replace(",", ".");
            double parsed;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed)) return parsed;
            return 0;
        }
    }
}


