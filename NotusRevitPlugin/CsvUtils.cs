using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace NotusRevitPlugin
{
    public static class CsvUtils
    {
        public static string Escape(string value)
        {
            if (value == null)
            {
                return "";
            }

            bool mustQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
            value = value.Replace("\"", "\"\"");

            return mustQuote ? "\"" + value + "\"" : value;
        }

        public static string FormatDouble(double value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        public static List<Dictionary<string, string>> ReadCsv(string path)
        {
            List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();

            // BUGFIX: a versão anterior lia o arquivo com File.ReadAllLines e só então tratava aspas.
            // Isso quebra qualquer campo que contenha uma quebra de linha DENTRO de aspas (CSV válido,
            // e o próprio CsvUtils.Escape() previa esse caso ao checar value.Contains("\n")) — o
            // ReadAllLines já teria cortado a linha ali, virando duas "linhas" erradas na importação.
            // Agora fazemos o parsing sobre o texto inteiro, respeitando aspas mesmo através de \r\n.
            string content = File.ReadAllText(path, Encoding.UTF8);
            List<string[]> records = SplitCsvRecords(content);
            if (records.Count == 0) return rows;

            string[] headers = records[0];
            for (int i = 1; i < records.Count; i++)
            {
                string[] values = records[i];
                if (values.Length == 1 && string.IsNullOrWhiteSpace(values[0])) continue;

                Dictionary<string, string> row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int c = 0; c < headers.Length; c++)
                {
                    row[headers[c]] = c < values.Length ? values[c] : "";
                }
                rows.Add(row);
            }

            return rows;
        }

        private static List<string[]> SplitCsvRecords(string content)
        {
            List<string[]> records = new List<string[]>();
            List<string> current = new List<string>();
            StringBuilder field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < content.Length; i++)
            {
                char ch = content[i];

                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < content.Length && content[i + 1] == '"') { field.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else
                    {
                        field.Append(ch);
                    }
                    continue;
                }

                if (ch == '"') { inQuotes = true; continue; }

                if (ch == ',') { current.Add(field.ToString()); field.Clear(); continue; }

                if (ch == '\r') continue; // normaliza \r\n e \r isolado

                if (ch == '\n')
                {
                    current.Add(field.ToString());
                    field.Clear();
                    records.Add(current.ToArray());
                    current = new List<string>();
                    continue;
                }

                field.Append(ch);
            }

            if (field.Length > 0 || current.Count > 0)
            {
                current.Add(field.ToString());
                records.Add(current.ToArray());
            }

            return records;
        }
    }
}


