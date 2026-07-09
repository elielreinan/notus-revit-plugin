using System;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Services;
using Microsoft.Win32;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class ExportExcelCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Document doc = commandData.Application.ActiveUIDocument.Document;
                SaveFileDialog dialog = new SaveFileDialog
                {
                    Title = "Exportar Excel nativo Notus",
                    Filter = "Excel nativo (*.xlsx)|*.xlsx|Todos os arquivos (*.*)|*.*",
                    FileName = MakeSafe(doc.Title) + "_Notus_Relatorio.xlsx",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    AddExtension = true,
                    DefaultExt = ".xlsx",
                    OverwritePrompt = true
                };
                if (dialog.ShowDialog() != true) return Result.Cancelled;
                string path = new HvacReportExportService().ExportExcelXlsx(doc, dialog.FileName);
                TaskDialog.Show("Notus", "Excel .xlsx exportado com abas: resumo geral, ambientes, pavimentos, críticos e critérios.\n\n" + path);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                HvacLogService.Error("Erro ao exportar Excel", ex);
                message = ex.Message;
                TaskDialog.Show("Notus", "Não foi possível exportar o Excel.\n\n" + ex.Message);
                return Result.Failed;
            }
        }

        private string MakeSafe(string text)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) text = text.Replace(c, '_');
            return text;
        }
    }
}


