using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class ValidateImportCsvCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                string csvPath = App.GetImportCsvPath();
                if (string.IsNullOrWhiteSpace(csvPath))
                {
                    TaskDialog.Show("Notus", "Selecione primeiro o CSV no ComboBox de Importação ou use o botão Escolher CSV.");
                    return Result.Cancelled;
                }

                if (!File.Exists(csvPath))
                {
                    TaskDialog.Show("Notus", "CSV não encontrado:\n" + csvPath);
                    return Result.Failed;
                }

                List<Dictionary<string, string>> rows = CsvUtils.ReadCsv(csvPath);
                bool hasUniqueId = rows.Count == 0 || rows[0].ContainsKey("UniqueId");
                bool hasElementId = rows.Count == 0 || rows[0].ContainsKey("ElementId");

                string status = (hasUniqueId || hasElementId)
                    ? "Estrutura básica compatível para importação."
                    : "O CSV foi lido, mas não encontrei as colunas UniqueId ou ElementId.";

                TaskDialog dialog = new TaskDialog("Notus")
                {
                    MainInstruction = "Validação do CSV",
                    MainContent =
                        "Arquivo: " + csvPath + "\n" +
                        "Linhas lidas: " + rows.Count + "\n\n" +
                        status,
                    CommonButtons = TaskDialogCommonButtons.Ok
                };
                dialog.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Notus", "Não foi possível validar o CSV.\n\n" + ex.Message);
                return Result.Failed;
            }
        }
    }
}


