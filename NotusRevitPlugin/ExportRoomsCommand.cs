using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Models;
using NotusRevitPlugin.Services;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class ExportRoomsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            string outputPath = App.GetExportCsvPath();
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                string defaultFileName = MakeSafeFileName(doc.Title) + "_Notus_Revit.csv";
                outputPath = Path.Combine(App.GetExportCsvFolder(), defaultFileName);
                App.SetExportCsvPath(outputPath);
            }

            try
            {
                string directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                int count = ExportEnvironments(uidoc, outputPath);

                TaskDialog.Show("Notus", "Exportacao concluida.\nAmbientes exportados: " + count + "\nArquivo: " + outputPath);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Notus", "Nao foi possivel exportar os ambientes.\n\n" + ex.Message);
                return Result.Failed;
            }
        }

        private int ExportEnvironments(UIDocument uidoc, string outputPath)
        {
            Document doc = uidoc.Document;
            RevitRoomDataService service = new RevitRoomDataService();
            List<Element> elements = service.GetSelectedOrAllRoomsAndSpaces(uidoc);
            List<string> lines = new List<string>();
            lines.Add("ProjectName,DocumentTitle,ElementKind,ElementId,UniqueId,Number,Name,Level,AreaM2,HeightM,VolumeM3,Occupants,LightingWatts,EquipmentWatts,EnvironmentType,SolarOrientation,InternalTemp,ExternalTemp,InternalRH,ExternalRH,ProjectCity,ProjectState,ClimateSource,NormativeReference,DataQualityNote,TotalBTUh,TotalTR,SensibleBTUh,SensibleTR,LatentBTUh,SHR,SupplyAirflowM3h,ExternalAirflowM3h,AirChangesHour,EquipmentSelected,Method,ClimateSummary,InternalExternalSummary,MemorialSummary,Notes");

            foreach (Element element in elements)
            {
                RoomInput input = service.BuildInput(doc, element);
                if (input.AreaM2 <= 0)
                {
                    continue;
                }

                lines.Add(BuildLine(doc, element, input));
            }

            File.WriteAllLines(outputPath, lines, new UTF8Encoding(true));
            return Math.Max(0, lines.Count - 1);
        }

        private string BuildLine(Document hostDoc, Element element, RoomInput input)
        {
            Document sourceDoc = hostDoc;
            try { if (element != null && element.Document != null) sourceDoc = element.Document; } catch { }

            string projectName = sourceDoc.ProjectInformation != null ? sourceDoc.ProjectInformation.Name : sourceDoc.Title;
            string documentTitle = sourceDoc.Title;
            string[] values = new string[]
            {
                projectName,
                documentTitle,
                input.ElementKind,
                input.ElementIdValue.ToString(),
                input.UniqueId,
                input.Number,
                input.Name,
                input.Level,
                CsvUtils.FormatDouble(input.AreaM2),
                CsvUtils.FormatDouble(input.HeightM),
                CsvUtils.FormatDouble(input.VolumeM3),
                input.Occupants.ToString(),
                CsvUtils.FormatDouble(input.LightingWatts),
                CsvUtils.FormatDouble(input.EquipmentWatts),
                input.EnvironmentType,
                input.SolarOrientation,
                CsvUtils.FormatDouble(input.InternalTempC),
                CsvUtils.FormatDouble(input.ExternalTempC),
                CsvUtils.FormatDouble(input.InternalRelativeHumidity),
                CsvUtils.FormatDouble(input.ExternalRelativeHumidity),
                input.ProjectCity,
                input.ProjectState,
                input.ClimateSource,
                input.NormativeReference,
                input.DataQualityNote,
                GetParameterText(element, "Notus_Total_BTU"),
                GetParameterText(element, "Notus_TR"),
                GetParameterText(element, "Notus_Sensible_BTU"),
                GetParameterText(element, "Notus_Sensible_TR"),
                GetParameterText(element, "Notus_Latent_BTU"),
                GetParameterText(element, "Notus_SHR"),
                GetParameterText(element, "Notus_Supply_m3h"),
                GetParameterText(element, "Notus_External_m3h"),
                GetParameterText(element, "Notus_AirChanges_h"),
                GetParameterText(element, "Notus_Equipment"),
                GetParameterText(element, "Notus_Method"),
                GetParameterText(element, "Notus_Climate"),
                GetParameterText(element, "Notus_InternalExternal"),
                GetParameterText(element, "Notus_Memorial"),
                GetParameterText(element, "Notus_Notes")
            };

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = CsvUtils.Escape(values[i]);
            }

            return string.Join(",", values);
        }

        private string GetParameterText(Element element, string parameterName)
        {
            if (element == null) return "";
            Parameter parameter = element.LookupParameter(parameterName);
            if (parameter == null || !parameter.HasValue) return "";
            if (parameter.StorageType == StorageType.String) return parameter.AsString() ?? "";
            return parameter.AsValueString() ?? "";
        }

        private string MakeSafeFileName(string text)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                text = text.Replace(c, '_');
            }
            return text;
        }
    }
}
