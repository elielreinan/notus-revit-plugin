using System;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Services;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class ReadEnvironmentsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;
                RevitRoomDataService data = new RevitRoomDataService();
                List<Element> environments = data.GetSelectedOrAllRoomsAndSpaces(uidoc);
                IfcDiagnosticReport ifc = new IfcEnvironmentService().BuildDiagnosticReport(doc, doc.ActiveView);

                TaskDialog.Show("Notus - Ler Ambientes",
                    "Ambientes aptos para calculo: " + environments.Count + "\n" +
                    "Rooms no modelo ativo: " + ifc.Rooms + "\n" +
                    "MEP Spaces no modelo ativo: " + ifc.Spaces + "\n" +
                    "Rooms em vinculos: " + ifc.LinkedRooms + "\n" +
                    "MEP Spaces em vinculos: " + ifc.LinkedSpaces + "\n" +
                    "IfcSpaces: " + ifc.IfcSpaces + "\n" +
                    "DirectShapes: " + ifc.DirectShapes + "\n" +
                    "ImportInstances: " + ifc.ImportInstances + "\n" +
                    "Revit Links: " + ifc.RevitLinks + "\n" +
                    "Links nao acessiveis: " + ifc.UnreadableLinks + "\n" +
                    "Elementos com IFC GUID: " + ifc.ElementsWithIfcGuid + "\n\n" +
                    ifc.Conclusion + "\n\n" +
                    "Observacao: ambientes lidos de vinculos podem ser calculados, mas os parametros Notus so sao gravados nos ambientes editaveis do modelo ativo.");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                HvacLogService.Error("Erro ao ler ambientes", ex);
                message = ex.Message;
                TaskDialog.Show("Notus", "Nao foi possivel ler os ambientes.\n\n" + ex.Message);
                return Result.Failed;
            }
        }
    }
}
