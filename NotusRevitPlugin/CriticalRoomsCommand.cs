using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Services;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class CriticalRoomsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Document doc = commandData.Application.ActiveUIDocument.Document;
                string text = new HvacReportExportService().BuildCriticalRoomsText(doc);
                TaskDialog.Show("Notus - Ambientes críticos", text);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                HvacLogService.Error("Erro ao listar ambientes críticos", ex);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}


