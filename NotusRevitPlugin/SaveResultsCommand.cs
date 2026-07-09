using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class SaveResultsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            TaskDialog.Show("Notus", "Os resultados são gravados automaticamente nos parâmetros do Revit ao executar Calcular HVAC ou Importar CSV.\n\nPara atualizar, use Calcular HVAC novamente.");
            return Result.Succeeded;
        }
    }
}


