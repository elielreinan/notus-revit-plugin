using System;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Services;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class ValidateModelCommand : IExternalCommand
    {
        private readonly string[] resultParameterNames = HvacParameterService.ResultParameterNames;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;
                IfcEnvironmentService ifcService = new IfcEnvironmentService();
                IfcDiagnosticReport ifc = ifcService.BuildDiagnosticReport(doc, doc.ActiveView);
                RevitRoomDataService dataService = new RevitRoomDataService();
                int environments = dataService.GetSelectedOrAllRoomsAndSpaces(uidoc).Count;
                int parametersFound = CountExistingParameters(doc, ifcService);
                string climate = new ClimateService().GetClimateForProject(doc).Summary;

                string status = parametersFound >= resultParameterNames.Length
                    ? "Parametros Notus/Notus encontrados."
                    : "Parametros ainda nao estao completos. Use Criar Parametros ou clique em Calcular HVAC para cria-los automaticamente.";

                if (environments == 0 && (ifc.DirectShapes > 0 || ifc.ImportInstances > 0 || ifc.ElementsWithIfcGuid > 0 || ifc.CategoryCounts.Count > 0))
                {
                    status += "\n\nAtencao: ha geometria IFC/arquitetonica, mas nenhum ambiente calculavel. Use Ambientes > Assistente IFC ou reexporte o IFC com IfcSpace.";
                }
                else if (ifc.LinkedRooms + ifc.LinkedSpaces > 0)
                {
                    status += "\n\nAmbientes em vinculos foram detectados. Eles entram na leitura/calculo; quando forem somente leitura, o Notus cria linhas de tabela/grafico no modelo ativo.";
                }

                TaskDialog dialog = new TaskDialog(ProductInfo.FullName)
                {
                    MainInstruction = "Verificacao do modelo",
                    MainContent =
                        "Ambientes aptos para calculo: " + environments + "\n" +
                        "Rooms no modelo ativo: " + ifc.Rooms + "\n" +
                        "MEP Spaces no modelo ativo: " + ifc.Spaces + "\n" +
                        "Rooms em vinculos: " + ifc.LinkedRooms + "\n" +
                        "MEP Spaces em vinculos: " + ifc.LinkedSpaces + "\n" +
                        "IfcSpaces/elementos equivalentes: " + ifc.IfcSpaces + "\n" +
                        "Links nao acessiveis: " + ifc.UnreadableLinks + "\n" +
                        "Parametros encontrados: " + parametersFound + " de " + resultParameterNames.Length + "\n" +
                        "Metodo selecionado: " + App.GetCalculationMethodPreset() + "\n" +
                        "Clima do projeto: " + climate + "\n\n" +
                        status,
                    CommonButtons = TaskDialogCommonButtons.Ok
                };
                dialog.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show(ProductInfo.FullName, "Nao foi possivel verificar o modelo.\n\n" + ex.Message);
                return Result.Failed;
            }
        }

        private int CountExistingParameters(Document doc, IfcEnvironmentService ifcService)
        {
            Element sample = null;
            try
            {
                sample = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .WhereElementIsNotElementType()
                    .Cast<Element>()
                    .FirstOrDefault();
            }
            catch { }

            if (sample == null)
            {
                try
                {
                    sample = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_MEPSpaces)
                        .WhereElementIsNotElementType()
                        .Cast<Element>()
                        .FirstOrDefault();
                }
                catch { }
            }

            if (sample == null)
            {
                sample = ifcService.GetIfcSpaceLikeElements(doc).FirstOrDefault();
            }

            if (sample == null) return 0;

            int count = 0;
            foreach (string parameterName in resultParameterNames)
            {
                if (sample.LookupParameter(parameterName) != null) count++;
            }
            return count;
        }
    }
}
