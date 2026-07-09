using System;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class ImportResultsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            string csvPath = App.GetImportCsvPath();
            if (string.IsNullOrWhiteSpace(csvPath))
            {
                TaskDialog.Show("Notus", "Selecione primeiro o CSV calculado no ComboBox da Ribbon de Importação ou use o botão Escolher CSV.");
                return Result.Cancelled;
            }

            try
            {
                if (!System.IO.File.Exists(csvPath))
                {
                    TaskDialog.Show("Notus", "CSV não encontrado:\n" + csvPath);
                    return Result.Failed;
                }

                List<Dictionary<string, string>> rows = CsvUtils.ReadCsv(csvPath);
                int updated = 0;
                int notFound = 0;

                using (Transaction transaction = new Transaction(doc, "Importar resultados Notus"))
                {
                    transaction.Start();

                    foreach (Dictionary<string, string> row in rows)
                    {
                        Element element = FindElement(doc, row);

                        if (element == null)
                        {
                            notFound++;
                            continue;
                        }

                        TrySetParameter(element, row, "Notus_Total_BTU", "TotalBTUh", "Notus_Total_BTU");
                        TrySetParameter(element, row, "Notus_TR", "TotalTR", "Notus_TR");
                        TrySetParameter(element, row, "Notus_Sensible_BTU", "SensibleBTUh", "Notus_Sensible_BTU");
                        TrySetParameter(element, row, "Notus_Sensible_TR", "SensibleTR", "Notus_Sensible_TR");
                        TrySetParameter(element, row, "Notus_Latent_BTU", "LatentBTUh", "Notus_Latent_BTU");
                        TrySetParameter(element, row, "Notus_SHR", "SHR", "Notus_SHR");
                        TrySetParameter(element, row, "Notus_Supply_m3h", "SupplyAirflowM3h", "Notus_Supply_m3h");
                        TrySetParameter(element, row, "Notus_External_m3h", "ExternalAirflowM3h", "Notus_External_m3h");
                        TrySetParameter(element, row, "Notus_AirChanges_h", "AirChangesHour", "Notus_AirChanges_h");
                        TrySetParameter(element, row, "Notus_Equipment", "EquipmentSelected", "Notus_Equipment");
                        TrySetParameter(element, row, "Notus_Method", "Method", "Notus_Method");
                        TrySetParameter(element, row, "Notus_Climate", "ClimateSummary", "Notus_Climate");
                        TrySetParameter(element, row, "Notus_InternalExternal", "InternalExternalSummary", "Notus_InternalExternal");
                        TrySetParameter(element, row, "Notus_NormRef", "NormativeReference", "Notus_NormRef");
                        TrySetParameter(element, row, "Notus_Notes", "Notes", "Notus_Notes");
                        TrySetParameter(element, row, "Notus_Memorial", "MemorialSummary", "Notus_Memorial");

                        updated++;
                    }

                    transaction.Commit();
                }

                TaskDialog dialog = new TaskDialog("Notus")
                {
                    MainInstruction = "Importação concluída.",
                    MainContent = "Ambientes atualizados: " + updated + "\nNão encontrados: " + notFound + "\nArquivo: " + csvPath,
                    CommonButtons = TaskDialogCommonButtons.Ok
                };
                dialog.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;

                TaskDialog dialog = new TaskDialog("Notus")
                {
                    MainInstruction = "Não foi possível importar os resultados.",
                    MainContent = ex.Message,
                    CommonButtons = TaskDialogCommonButtons.Ok
                };
                dialog.Show();

                return Result.Failed;
            }
        }

        private Element FindElement(Document doc, Dictionary<string, string> row)
        {
            string uniqueId = Get(row, "UniqueId");
            if (!string.IsNullOrWhiteSpace(uniqueId))
            {
                Element element = doc.GetElement(uniqueId);
                if (element != null)
                {
                    return element;
                }
            }

            string elementIdText = Get(row, "ElementId");
            long elementId;
            if (long.TryParse(elementIdText, out elementId))
            {
                Element element = doc.GetElement(new ElementId(elementId));
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }

        private string Get(Dictionary<string, string> row, string key)
        {
            if (row.ContainsKey(key))
            {
                return row[key];
            }

            return "";
        }


        private void TrySetParameter(Element element, Dictionary<string, string> row, string parameterName, params string[] csvKeys)
        {
            foreach (string key in csvKeys)
            {
                string value = Get(row, key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    SetParameter(element, parameterName, value);
                    return;
                }
            }
        }

        private void SetParameter(Element element, string name, string value)
        {
            Parameter parameter = element.LookupParameter(name);

            if (parameter == null || parameter.IsReadOnly)
            {
                return;
            }

            parameter.Set(value ?? "");
        }
    }
}


