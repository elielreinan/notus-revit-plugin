using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Models;
using NotusRevitPlugin.Services;

namespace NotusRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class ConfigureHvacCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                HvacSettings settings = HvacSettingsService.Load();
                TaskDialog dialog = new TaskDialog("Notus")
                {
                    MainInstruction = "Configurações Notus",
                    MainContent = "Configuração atual:\n" +
                        "Cidade/UF: " + settings.DefaultCity + "/" + settings.DefaultState + "\n" +
                        "Norma: " + settings.DefaultNorm + "\n" +
                        "Temperatura interna: " + settings.DefaultInternalTempC.ToString("0.#") + " °C\n" +
                        "Umidade interna: " + settings.DefaultInternalRelativeHumidity.ToString("0") + "%\n" +
                        "Tipo padrão: " + settings.DefaultEnvironmentType + "\n\n" +
                        "Para critérios editáveis, abra a pasta de configuração e edite notus_settings.ini.",
                    CommonButtons = TaskDialogCommonButtons.Close
                };
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Abrir pasta de configuração", "Editar cidade, norma, temperatura, umidade, ventilação e ocupação.");
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Perfil ABNT / Escritório / Salvador", "Define um perfil padrão brasileiro para pré-dimensionamento.");
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "Perfil ASHRAE / Escritório", "Define o preset ASHRAE simplificado.");
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink4, "Perfil Hospitalar conservador", "Define critérios internos mais conservadores.");
                TaskDialogResult result = dialog.Show();

                if (result == TaskDialogResult.CommandLink1)
                {
                    HvacSettingsService.OpenSettingsFolder();
                }
                else if (result == TaskDialogResult.CommandLink2)
                {
                    settings.DefaultCity = "Salvador";
                    settings.DefaultState = "BA";
                    settings.DefaultNorm = "ABNT simplificado";
                    settings.DefaultEnvironmentType = "Escritório";
                    settings.DefaultInternalTempC = 24;
                    settings.DefaultInternalRelativeHumidity = 50;
                    settings.DefaultMinimumAch = 2;
                    HvacSettingsService.Save(settings);
                    TaskDialog.Show("Notus", "Perfil ABNT/Escritório salvo.");
                }
                else if (result == TaskDialogResult.CommandLink3)
                {
                    settings.DefaultNorm = "ASHRAE simplificado";
                    settings.DefaultEnvironmentType = "Escritório";
                    settings.DefaultMinimumAch = 2;
                    HvacSettingsService.Save(settings);
                    TaskDialog.Show("Notus", "Perfil ASHRAE salvo.");
                }
                else if (result == TaskDialogResult.CommandLink4)
                {
                    settings.DefaultNorm = "Conservador";
                    settings.DefaultEnvironmentType = "Hospital";
                    settings.DefaultInternalTempC = 23;
                    settings.DefaultInternalRelativeHumidity = 50;
                    settings.DefaultMinimumAch = 6;
                    settings.DefaultVentilationLsPerPerson = 5;
                    settings.DefaultVentilationLsPerM2 = 0.60;
                    HvacSettingsService.Save(settings);
                    TaskDialog.Show("Notus", "Perfil hospitalar conservador salvo.");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                HvacLogService.Error("Erro em configurações", ex);
                message = ex.Message;
                TaskDialog.Show("Notus", "Não foi possível abrir/salvar as configurações.\n\n" + ex.Message);
                return Result.Failed;
            }
        }
    }
}


