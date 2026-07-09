using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Services;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class HelpCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            TaskDialog dialog = new TaskDialog(ProductInfo.FullName)
            {
                MainInstruction = "Fluxo recomendado do Notus",
                MainContent =
                    "1. Abra o projeto no Revit 2025.\n" +
                    "2. Use Ambientes > Ler Ambientes para verificar Rooms, MEP Spaces, IfcSpaces e vinculos.\n" +
                    "3. Se o IFC veio sem ambientes, use Ambientes > Assistente IFC para criar Ambientes Notus manuais.\n" +
                    "4. Revise ambientes manuais quando houver area, pe-direito ou ocupacao duvidosa.\n" +
                    "5. Use Parametros > Criar Parametros ou clique direto em Calcular HVAC.\n" +
                    "6. Escolha o metodo: ABNT simplificado, ASHRAE simplificado ou Conservador.\n" +
                    "7. Revise os alertas antes de gravar os ambientes editaveis no modelo ativo.\n" +
                    "8. Gere CSV, Excel ou Memorial PDF em Relatorios.\n\n" +
                    "Dados que refinam o calculo: ocupantes, iluminacao W, equipamentos W, tipo de ambiente, orientacao solar, temperatura/umidade interna e externa.\n\n" +
                    ProductInfo.TechnicalNote,
                CommonButtons = TaskDialogCommonButtons.Ok
            };
            dialog.Show();
            return Result.Succeeded;
        }
    }
}
