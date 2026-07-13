using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.DB;

namespace NotusRevitPlugin.Services
{
    public class HvacScheduleService
    {
        public string CreateTechnicalSchedulesAndChart(Document doc)
        {
            ViewSchedule roomSchedule = CreateSchedule(doc, BuiltInCategory.OST_Rooms, "Notus - Tabela Técnica Rooms");
            ViewSchedule spaceSchedule = CreateSchedule(doc, BuiltInCategory.OST_MEPSpaces, "Notus - Tabela Técnica Spaces");
            ViewSchedule ifcSchedule = CreateSchedule(doc, BuiltInCategory.OST_GenericModel, "Notus - Tabela Técnica IFC e Vínculos");
            ViewDrafting chartView = CreateChartView(doc);

            string msg = "Tabela técnica HVAC gerada no Revit.";
            if (roomSchedule != null) msg += "\n- " + roomSchedule.Name;
            if (spaceSchedule != null) msg += "\n- " + spaceSchedule.Name;
            if (ifcSchedule != null) msg += "\n- " + ifcSchedule.Name + " (IFC/Generic Model/vínculos)";
            if (chartView != null) msg += "\n- " + chartView.Name;
            return msg;
        }

        private ViewSchedule CreateSchedule(Document doc, BuiltInCategory category, string scheduleName)
        {
            Category revitCategory = doc.Settings.Categories.get_Item(category);
            if (revitCategory == null)
            {
                return null;
            }

            ViewSchedule existing = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSchedule))
                .Cast<ViewSchedule>()
                .FirstOrDefault(v => v.Name.Equals(scheduleName, StringComparison.OrdinalIgnoreCase));

            ViewSchedule schedule = existing ?? ViewSchedule.CreateSchedule(doc, revitCategory.Id);
            schedule.Name = MakeUniqueName(doc, scheduleName, schedule.Id);

            ScheduleDefinition definition = schedule.Definition;
            try { definition.IsItemized = true; } catch { }

            AddFieldIfAvailable(definition, doc, "Número", 0.55, "Notus_Room_Number", "Number", "Número", "Numero");
            AddFieldIfAvailable(definition, doc, "Ambiente", 1.45, "Notus_Room_Name", "Notus_Manual_Name", "Name", "Nome");
            AddFieldIfAvailable(definition, doc, "Pavimento", 0.70, "Notus_Level", "Notus_Manual_Level", "Level", "Nível", "Nivel", "Pavimento");
            AddFieldIfAvailable(definition, doc, "TR total", 0.55, "Notus_TR");
            AddFieldIfAvailable(definition, doc, "TR sensível", 0.65, "Notus_Sensible_TR");
            AddFieldIfAvailable(definition, doc, "SHR", 0.45, "Notus_SHR");
            AddFieldIfAvailable(definition, doc, "m2/TR", 0.55, "Notus_m2_per_TR");
            AddFieldIfAvailable(definition, doc, "Insuflado m3/h", 0.80, "Notus_Supply_m3h");
            AddFieldIfAvailable(definition, doc, "Externo m3/h", 0.75, "Notus_External_m3h");
            AddFieldIfAvailable(definition, doc, "Trocas/h", 0.55, "Notus_AirChanges_h");
            AddFieldIfAvailable(definition, doc, "Área m2", 0.60, "Notus_Area_m2", "Notus_Manual_Area_m2", "Area", "Área");
            AddFieldIfAvailable(definition, doc, "Pessoas", 0.55, "Notus_Occupants", "Notus_Manual_Occupants", "Occupants", "Ocupantes", "Pessoas");
            AddFieldIfAvailable(definition, doc, "Origem", 1.10, "Notus_Source");
            AddFieldIfAvailable(definition, doc, "Alertas", 1.40, "Notus_ValidationAlerts");

            return schedule;
        }

        private ScheduleField AddFieldIfAvailable(ScheduleDefinition definition, Document doc, string heading, double width, params string[] targetNames)
        {
            foreach (SchedulableField field in definition.GetSchedulableFields())
            {
                string fieldName = field.GetName(doc);
                if (!Matches(fieldName, targetNames))
                {
                    continue;
                }

                ScheduleField scheduleField = null;
                for (int i = 0; i < definition.GetFieldCount(); i++)
                {
                    ScheduleField currentField = definition.GetField(i);
                    if (Matches(currentField.GetName(), targetNames) || Matches(currentField.GetName(), heading) || Matches(GetColumnHeading(currentField), targetNames) || Matches(GetColumnHeading(currentField), heading))
                    {
                        scheduleField = currentField;
                        break;
                    }
                }

                if (scheduleField == null)
                {
                    scheduleField = definition.AddField(field);
                }

                try { scheduleField.ColumnHeading = heading; } catch { }
                try { scheduleField.GridColumnWidth = width; } catch { }
                return scheduleField;
            }

            return null;
        }

        private bool Matches(string current, params string[] targets)
        {
            if (string.IsNullOrWhiteSpace(current))
            {
                return false;
            }

            foreach (string target in targets)
            {
                if (current.Equals(target, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private string GetColumnHeading(ScheduleField field)
        {
            try { return field.ColumnHeading ?? ""; }
            catch { return ""; }
        }

        private ViewDrafting CreateChartView(Document doc)
        {
            ViewFamilyType draftingType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(v => v.ViewFamily == ViewFamily.Drafting);

            if (draftingType == null)
            {
                return null;
            }

            ViewDrafting view = ViewDrafting.Create(doc, draftingType.Id);
            view.Name = MakeUniqueName(doc, "Notus - Gráfico TR", view.Id);

            TextNoteType textType = new FilteredElementCollector(doc)
                .OfClass(typeof(TextNoteType))
                .Cast<TextNoteType>()
                .FirstOrDefault();

            if (textType == null)
            {
                return view;
            }

            List<Element> hvacElements = GetRoomsAndSpacesWithResults(doc)
                .OrderByDescending(e => ParseDouble(GetParameterText(e, "Notus_TR")))
                .Take(24)
                .ToList();

            TextNote.Create(doc, view.Id, new XYZ(0, 0, 0), "Notus - Carga térmica por ambiente (TR)", textType.Id);

            if (hvacElements.Count == 0)
            {
                TextNote.Create(doc, view.Id, new XYZ(0, -0.35, 0), "Nenhum ambiente com resultados Notus_TR encontrado.", textType.Id);
                return view;
            }

            double maxTr = hvacElements.Max(e => ParseDouble(GetParameterText(e, "Notus_TR")));
            if (maxTr <= 0) maxTr = 1;

            FilledRegionType fillType = new FilteredElementCollector(doc)
                .OfClass(typeof(FilledRegionType))
                .Cast<FilledRegionType>()
                .FirstOrDefault();

            double originX = 0.35;
            double originY = -3.75;
            double chartHeight = 2.80;
            double barWidth = 0.20;
            double gap = hvacElements.Count > 18 ? 0.08 : 0.14;
            double chartWidth = hvacElements.Count * (barWidth + gap);

            doc.Create.NewDetailCurve(view, Line.CreateBound(new XYZ(originX, originY, 0), new XYZ(originX + chartWidth, originY, 0)));
            doc.Create.NewDetailCurve(view, Line.CreateBound(new XYZ(originX, originY, 0), new XYZ(originX, originY + chartHeight + 0.25, 0)));
            TextNote.Create(doc, view.Id, new XYZ(originX - 0.10, originY + chartHeight + 0.34, 0), "Maior carga: " + HvacCalculationService.FormatNumber(maxTr, 2) + " TR", textType.Id);

            for (int i = 0; i < hvacElements.Count; i++)
            {
                Element element = hvacElements[i];
                string name = GetParameterText(element, "Notus_Room_Name", "Notus_Manual_Name", "Name", "Nome");
                string number = GetParameterText(element, "Notus_Room_Number", "Number", "Número", "Numero");
                string trText = GetParameterText(element, "Notus_TR");
                double tr = ParseDouble(trText);
                double barHeight = Math.Max(0.08, (tr / maxTr) * chartHeight);
                double x = originX + 0.12 + i * (barWidth + gap);
                CreateBar(doc, view, fillType, x, originY, barWidth, barHeight);

                TextNote.Create(doc, view.Id, new XYZ(x - 0.03, originY + barHeight + 0.06, 0), HvacCalculationService.FormatNumber(tr, 2), textType.Id);
                string label = ShortLabel(string.IsNullOrWhiteSpace(number) ? name : number);
                TextNote.Create(doc, view.Id, new XYZ(x - 0.03, originY - 0.22, 0), label, textType.Id);
            }

            TextNote.Create(doc, view.Id, new XYZ(originX + chartWidth + 0.15, originY - 0.02, 0), "Ambientes", textType.Id);
            TextNote.Create(doc, view.Id, new XYZ(originX + 0.55, originY - 0.55, 0), "Observação: pré-dimensionamento. Validar critérios e fatores com o responsável técnico.", textType.Id);
            return view;
        }

        private void CreateBar(Document doc, View view, FilledRegionType fillType, double x, double y, double width, double height)
        {
            XYZ p1 = new XYZ(x, y, 0);
            XYZ p2 = new XYZ(x + width, y, 0);
            XYZ p3 = new XYZ(x + width, y + height, 0);
            XYZ p4 = new XYZ(x, y + height, 0);

            CurveLoop loop = new CurveLoop();
            loop.Append(Line.CreateBound(p1, p2));
            loop.Append(Line.CreateBound(p2, p3));
            loop.Append(Line.CreateBound(p3, p4));
            loop.Append(Line.CreateBound(p4, p1));

            if (fillType != null)
            {
                try
                {
                    FilledRegion.Create(doc, fillType.Id, view.Id, new List<CurveLoop> { loop });
                    return;
                }
                catch
                {
                }
            }

            doc.Create.NewDetailCurve(view, Line.CreateBound(p1, p2));
            doc.Create.NewDetailCurve(view, Line.CreateBound(p2, p3));
            doc.Create.NewDetailCurve(view, Line.CreateBound(p3, p4));
            doc.Create.NewDetailCurve(view, Line.CreateBound(p4, p1));
        }

        private string ShortLabel(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            text = text.Trim();
            return text.Length <= 8 ? text : text.Substring(0, 8);
        }

        private List<Element> GetRoomsAndSpacesWithResults(Document doc)
        {
            List<Element> elements = new List<Element>();

            elements.AddRange(new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .Where(e => ParseDouble(GetParameterText(e, "Notus_TR")) > 0));

            elements.AddRange(new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_MEPSpaces)
                .WhereElementIsNotElementType()
                .Where(e => ParseDouble(GetParameterText(e, "Notus_TR")) > 0));

            elements.AddRange(new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .WhereElementIsNotElementType()
                .Where(e => ParseDouble(GetParameterText(e, "Notus_TR")) > 0));

            return elements;
        }

        private string GetParameterText(Element element, params string[] parameterNames)
        {
            foreach (string parameterName in parameterNames)
            {
                Parameter parameter = element.LookupParameter(parameterName);
                if (parameter == null || !parameter.HasValue)
                {
                    continue;
                }

                if (parameter.StorageType == StorageType.String)
                {
                    return parameter.AsString() ?? "";
                }

                return parameter.AsValueString() ?? "";
            }

            return "";
        }

        private double ParseDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            value = value.Replace("TR", "").Replace("m³/h", "").Replace("m3/h", "").Replace("BTU/h", "").Trim();
            if (value.Contains(","))
            {
                value = value.Replace(".", "").Replace(",", ".");
            }

            double parsed;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return 0;
        }

        private string MakeUniqueName(Document doc, string baseName, ElementId currentId)
        {
            string name = baseName;
            int suffix = 1;

            while (new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Any(v => v.Id != currentId && v.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                suffix++;
                name = baseName + " " + suffix;
            }

            return name;
        }
    }
}


