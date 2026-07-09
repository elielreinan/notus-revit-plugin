using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.DB;

namespace NotusRevitPlugin.Services
{
    public class HvacScheduleService
    {
        private readonly string[] _resultParameterNames = HvacParameterService.ResultParameterNames;

        public string CreateTechnicalSchedulesAndChart(Document doc)
        {
            ViewSchedule roomSchedule = CreateSchedule(doc, BuiltInCategory.OST_Rooms, "Notus - Tabela Técnica Rooms");
            ViewSchedule spaceSchedule = CreateSchedule(doc, BuiltInCategory.OST_MEPSpaces, "Notus - Tabela Técnica Spaces");
            ViewSchedule ifcSchedule = CreateSchedule(doc, BuiltInCategory.OST_GenericModel, "Notus - Tabela Técnica IFC");
            ViewDrafting chartView = CreateChartView(doc);

            string msg = "Tabela técnica HVAC gerada no Revit.";
            if (roomSchedule != null) msg += "\n- " + roomSchedule.Name;
            if (spaceSchedule != null) msg += "\n- " + spaceSchedule.Name;
            if (ifcSchedule != null) msg += "\n- " + ifcSchedule.Name + " (IFC/Generic Model)";
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

            AddFieldIfAvailable(definition, doc, "Number", "Número", "Numero");
            AddFieldIfAvailable(definition, doc, "Name", "Nome");
            AddFieldIfAvailable(definition, doc, "Area", "Área");
            AddFieldIfAvailable(definition, doc, "Volume");

            foreach (string parameterName in _resultParameterNames)
            {
                AddFieldIfAvailable(definition, doc, parameterName);
            }

            return schedule;
        }

        private void AddFieldIfAvailable(ScheduleDefinition definition, Document doc, params string[] targetNames)
        {
            foreach (SchedulableField field in definition.GetSchedulableFields())
            {
                string fieldName = field.GetName(doc);
                if (!Matches(fieldName, targetNames))
                {
                    continue;
                }

                bool exists = false;
                for (int i = 0; i < definition.GetFieldCount(); i++)
                {
                    ScheduleField currentField = definition.GetField(i);
                    if (Matches(currentField.GetName(), targetNames))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    definition.AddField(field);
                }

                return;
            }
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

            List<Element> hvacElements = GetRoomsAndSpacesWithResults(doc);

            TextNote.Create(doc, view.Id, new XYZ(0, 0, 0), "Notus - Carga térmica por ambiente (TR)", textType.Id);

            if (hvacElements.Count == 0)
            {
                TextNote.Create(doc, view.Id, new XYZ(0, -0.35, 0), "Nenhum Room/Space com resultados Notus_TR encontrado.", textType.Id);
                return view;
            }

            double maxTr = hvacElements.Max(e => ParseDouble(GetParameterText(e, "Notus_TR")));
            if (maxTr <= 0) maxTr = 1;

            double y = -0.45;
            int index = 1;

            foreach (Element element in hvacElements.Take(30))
            {
                string name = GetParameterText(element, "Name", "Nome");
                string number = GetParameterText(element, "Number", "Número", "Numero");
                string trText = GetParameterText(element, "Notus_TR");
                double tr = ParseDouble(trText);
                double barLength = Math.Max(0.1, (tr / maxTr) * 4.0);

                TextNote.Create(doc, view.Id, new XYZ(0, y, 0), index + ". " + number + " - " + name + " | " + trText + " TR", textType.Id);
                Line line = Line.CreateBound(new XYZ(2.8, y + 0.02, 0), new XYZ(2.8 + barLength, y + 0.02, 0));
                doc.Create.NewDetailCurve(view, line);

                y -= 0.25;
                index++;
            }

            TextNote.Create(doc, view.Id, new XYZ(0, y - 0.20, 0), "Observação: pré-dimensionamento. Validar critérios e fatores com o responsável técnico.", textType.Id);
            return view;
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


