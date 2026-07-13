using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Services;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class CreateScheduleChartCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                string resultMessage;
                using (Transaction transaction = new Transaction(doc, "Gerar tabela técnica Notus"))
                {
                    transaction.Start();
                    HvacParameterService.EnsureParameters(doc, commandData.Application.Application);
                    resultMessage = new HvacScheduleService().CreateTechnicalSchedulesAndChart(doc);
                    transaction.Commit();
                }

                TaskDialog.Show("Notus", resultMessage);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Notus", "Não foi possível criar a tabela técnica no Revit.\n\n" + ex.Message);
                return Result.Failed;
            }
        }
    }
}


