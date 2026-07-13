using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Forms;
using NotusRevitPlugin.Models;
using NotusRevitPlugin.Services;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class CalculateHvacCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                RevitRoomDataService roomDataService = new RevitRoomDataService();
                HvacCalculationService calculationService = new HvacCalculationService();
                HvacSettings settings = HvacSettingsService.Load();

                List<Element> targets = roomDataService.GetSelectedOrAllRoomsAndSpaces(uidoc);
                if (targets.Count == 0)
                {
                    IfcDiagnosticReport report = new IfcEnvironmentService().BuildDiagnosticReport(doc, doc.ActiveView);
                    TaskDialog.Show(
                        "Notus",
                        "Nenhum Room/Space/IfcSpace encontrado para calculo.\n\n" +
                        report.Conclusion + "\n\n" +
                        "Fluxo recomendado para IFC sem ambientes exportados:\n" +
                        "1. Reexporte o IFC com zonas/ambientes como IfcSpace, se possivel.\n" +
                        "2. Ou selecione pisos/lajes/elementos que representem ambientes.\n" +
                        "3. Use Ambientes > Criar Ambiente Manual.\n" +
                        "4. Depois clique em Calcular HVAC.");
                    return Result.Cancelled;
                }

                if (settings.ConfirmBeforeOverwrite && HasExistingResults(doc, targets))
                {
                    TaskDialog confirm = new TaskDialog("Notus")
                    {
                        MainInstruction = "Existem resultados Notus no modelo ativo.",
                        MainContent = "A proxima tela vai mostrar uma previa. So depois da sua confirmacao os parametros serao gravados/atualizados nos ambientes editaveis do Revit.",
                        CommonButtons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel
                    };
                    if (confirm.Show() != TaskDialogResult.Ok) return Result.Cancelled;
                }

                string methodPreset = App.GetCalculationMethodPreset();
                List<HvacCalculationPreview> previews = new List<HvacCalculationPreview>();
                int skipped = 0;

                foreach (Element element in targets)
                {
                    RoomInput input = roomDataService.BuildInput(doc, element);
                    bool canWrite = CanWriteResultToHost(doc, element);
                    if (input.AreaM2 <= 0 || input.VolumeM3 <= 0)
                    {
                        HvacCalculationPreview invalid = new HvacCalculationPreview
                        {
                            Element = element,
                            Input = input,
                            Result = new HvacResult(),
                            CanWrite = false
                        };
                        invalid.Alerts.Add(input.AreaM2 <= 0 ? "Area zerada" : "Volume zerado");
                        if (!canWrite) invalid.Alerts.Add("Somente leitura/vinculo: resultado nao sera gravado no modelo ativo.");
                        previews.Add(invalid);
                        skipped++;
                        continue;
                    }

                    HvacResult result = calculationService.Calculate(input, methodPreset);
                    List<string> alerts = HvacValidationService.Validate(input, result);
                    if (!canWrite)
                    {
                        alerts.Add("Somente leitura/vinculo: calculado para revisao, mas nao gravado no modelo ativo.");
                    }

                    HvacCalculationPreview preview = new HvacCalculationPreview
                    {
                        Element = element,
                        Input = input,
                        Result = result,
                        CanWrite = canWrite,
                        Alerts = alerts
                    };
                    previews.Add(preview);
                }

                if (previews.Count == 0 || previews.All(p => p.Input == null || p.Input.AreaM2 <= 0 || p.Input.VolumeM3 <= 0))
                {
                    TaskDialog.Show("Notus", "Nenhum ambiente valido para calcular. Verifique area, altura e volume.");
                    return Result.Cancelled;
                }

                using (HvacReviewForm review = new HvacReviewForm(previews))
                {
                    if (review.ShowDialog() != System.Windows.Forms.DialogResult.OK || !review.Confirmed)
                    {
                        HvacLogService.Info("Calculo cancelado na tela de revisao.");
                        return Result.Cancelled;
                    }
                }

                List<HvacCalculationPreview> validPreviews = previews.Where(p => p.Input != null && p.Input.AreaM2 > 0 && p.Input.VolumeM3 > 0).ToList();
                List<HvacCalculationPreview> writablePreviews = validPreviews.Where(p => p.CanWrite).ToList();
                List<HvacCalculationPreview> readOnlyPreviews = validPreviews.Where(p => !p.CanWrite).ToList();

                int calculated = 0;
                int reportRows = 0;
                int readOnlyCalculated = validPreviews.Count - writablePreviews.Count;
                int ifcCalculated = validPreviews.Count(p => p.Input.IsIfcFallback);
                double totalBTUh = validPreviews.Sum(p => p.Result.TotalBTUh);
                double totalTR = validPreviews.Sum(p => p.Result.TotalTR);
                double totalSupplyM3h = validPreviews.Sum(p => p.Result.SupplyAirflowM3h);
                double totalExternalM3h = validPreviews.Sum(p => p.Result.ExternalAirflowM3h);
                string lastClimate = validPreviews.Count > 0 ? validPreviews[validPreviews.Count - 1].Result.ClimateSummary : "";
                int warningCount = validPreviews.Sum(p => p.Alerts.Count);

                if (validPreviews.Count > 0)
                {
                    using (Transaction transaction = new Transaction(doc, "Calcular Notus"))
                    {
                        transaction.Start();
                        HvacParameterService.EnsureParameters(doc, uiapp.Application);

                        foreach (HvacCalculationPreview preview in writablePreviews)
                        {
                            HvacResultElementService.WriteResultData(preview.Element, preview.Input, preview.Result, preview.Alerts);
                            calculated++;
                        }

                        reportRows = HvacResultElementService.ReplaceReadOnlyResultElements(doc, readOnlyPreviews);
                        transaction.Commit();
                    }
                }

                string scheduleMessage = "";
                if (calculated + reportRows > 0)
                {
                    try
                    {
                        using (Transaction scheduleTransaction = new Transaction(doc, "Gerar tabela tecnica Notus"))
                        {
                            scheduleTransaction.Start();
                            scheduleMessage = new HvacScheduleService().CreateTechnicalSchedulesAndChart(doc);
                            scheduleTransaction.Commit();
                        }
                    }
                    catch (Exception scheduleEx)
                    {
                        scheduleMessage = "Tabela tecnica nao foi gerada automaticamente: " + scheduleEx.Message;
                        HvacLogService.Error("Falha ao gerar tabela automatica", scheduleEx);
                    }
                }
                else
                {
                    scheduleMessage = "Tabela tecnica nao foi gerada porque nenhum ambiente editavel foi gravado no modelo ativo.";
                }

                TaskDialog dialog = new TaskDialog("Notus")
                {
                    MainInstruction = calculated + reportRows > 0 ? "Calculo HVAC registrado no Revit." : "Calculo HVAC concluido para ambientes somente leitura.",
                    MainContent =
                        "Ambientes calculados na previa: " + validPreviews.Count +
                        "\nAmbientes editaveis atualizados: " + calculated +
                        (reportRows > 0 ? "\nLinhas de tabela para vinculos/IFC: " + reportRows : "") +
                        (readOnlyCalculated > 0 ? "\nAmbientes somente leitura/vinculados: " + readOnlyCalculated : "") +
                        "\nIgnorados sem area/volume: " + skipped +
                        (ifcCalculated > 0 ? "\nAmbientes IFC/manuais estimados: " + ifcCalculated : "") +
                        "\nMetodo: " + methodPreset +
                        "\nClima considerado: " + lastClimate +
                        "\nCarga total: " + HvacCalculationService.FormatNumber(totalBTUh, 0) + " BTU/h" +
                        "\nCarga total: " + HvacCalculationService.FormatNumber(totalTR, 2) + " TR" +
                        "\nVazao insuflada total: " + HvacCalculationService.FormatNumber(totalSupplyM3h, 0) + " m3/h" +
                        "\nVazao externa total: " + HvacCalculationService.FormatNumber(totalExternalM3h, 0) + " m3/h" +
                        (warningCount > 0 ? "\n\nAlertas tecnicos gerados: " + warningCount + ". Conferir a tela de revisao e parametros Notus_ValidationAlerts dos ambientes gravados." : "") +
                        "\n\n" + scheduleMessage +
                        "\n\nOs resultados sao estimativos e devem ser validados pelo responsavel tecnico.",
                    CommonButtons = TaskDialogCommonButtons.Ok
                };
                dialog.Show();
                HvacLogService.Info("Calculo concluido. Calculados=" + validPreviews.Count + ", Gravados=" + calculated + ", LinhasTabela=" + reportRows + ", SomenteLeitura=" + readOnlyCalculated + ", BTU=" + totalBTUh + ", TR=" + totalTR);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                HvacLogService.Error("Erro ao calcular HVAC", ex);
                message = ex.Message;
                TaskDialog.Show("Notus", "Nao foi possivel calcular HVAC no modelo.\n\n" + ex.Message);
                return Result.Failed;
            }
        }

        private bool HasExistingResults(Document hostDoc, List<Element> elements)
        {
            foreach (Element e in elements)
            {
                if (!CanWriteResultToHost(hostDoc, e)) continue;
                Parameter p = e.LookupParameter("Notus_TR");
                if (p != null && p.HasValue)
                {
                    string value = p.StorageType == StorageType.String ? p.AsString() : p.AsValueString();
                    if (!string.IsNullOrWhiteSpace(value)) return true;
                }
            }
            return false;
        }

        private bool CanWriteResultToHost(Document hostDoc, Element element)
        {
            if (hostDoc == null || element == null) return false;
            try { return object.ReferenceEquals(element.Document, hostDoc); }
            catch { return false; }
        }

    }
}
