using System;
using System.Diagnostics;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Services;
using Microsoft.Win32;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class ExportPdfMemorialCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Document doc = commandData.Application.ActiveUIDocument.Document;
                SaveFileDialog dialog = new SaveFileDialog
                {
                    Title = "Gerar Memorial PDF Notus",
                    Filter = "PDF (*.pdf)|*.pdf|Todos os arquivos (*.*)|*.*",
                    FileName = MakeSafe(doc.Title) + "_Notus_Memorial.pdf",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    AddExtension = true,
                    DefaultExt = ".pdf",
                    OverwritePrompt = true
                };
                if (dialog.ShowDialog() != true) return Result.Cancelled;
                string path = new NotusMemorialPdfService().Export(doc, dialog.FileName);
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                TaskDialog.Show(ProductInfo.FullName, "Memorial PDF Notus gerado e aberto.\n\n" + path);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                HvacLogService.Error("Erro ao gerar Memorial PDF Notus", ex);
                message = ex.Message;
                TaskDialog.Show(ProductInfo.FullName, "Nao foi possivel gerar o Memorial PDF.\n\n" + ex.Message);
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
