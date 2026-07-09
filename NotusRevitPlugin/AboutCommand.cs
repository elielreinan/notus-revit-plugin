using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Services;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class AboutCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            TaskDialog dialog = new TaskDialog(ProductInfo.FullName)
            {
                MainInstruction = ProductInfo.FullName + " v" + ProductInfo.Version + " | Revit " + ProductInfo.RevitVersion,
                MainContent =
                    ProductInfo.Tagline + "\n\n" +
                    "Recursos: calculo direto em Rooms e MEP Spaces; leitura de ambientes em vinculos; Assistente IFC para modelos sem IfcSpace; Ambientes Notus manuais; parametros compativeis Notus_*; tabela tecnica; CSV; Excel; Memorial PDF; validacoes tecnicas; logs e diagnostico.\n\n" +
                    "Novidades v1.0.0:\n" +
                    "- Marca Notus.\n" +
                    "- Assistente IFC para criar ambientes manuais a partir de pisos/lajes/elementos IFC.\n" +
                    "- Memorial PDF com capa, resumo, premissas, tabela e alertas.\n" +
                    "- Alertas tecnicos mais completos para area, volume, pe-direito, ocupacao, carga por m2, IFC e vinculos.\n" +
                    "- Versionamento e material comercial inicial.\n\n" +
                    "Compativel com Autodesk Revit 2025.\n\n" +
                    ProductInfo.TechnicalNote,
                CommonButtons = TaskDialogCommonButtons.Ok
            };
            dialog.Show();
            return Result.Succeeded;
        }
    }
}
