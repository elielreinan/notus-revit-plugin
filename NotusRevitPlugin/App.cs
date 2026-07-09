using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using NotusRevitPlugin.Services;

namespace NotusRevitPlugin
{
    public class App : IExternalApplication
    {
        public const string TabName = ProductInfo.FullName;

        private const string ExportCsvComboBoxName = "NotusExportCsvComboBox";
        private const string ImportCsvComboBoxName = "NotusImportCsvComboBox";
        private const string CalculationMethodComboBoxName = "NotusCalculationMethodComboBox";

        private const string ExportDesktopPresetName = "NotusExportDesktopPreset";
        private const string ExportDocumentsPresetName = "NotusExportDocumentsPreset";
        private const string ExportDownloadsPresetName = "NotusExportDownloadsPreset";
        private const string ExportCustomPresetName = "NotusExportCustomPreset";

        private const string ImportSelectedPresetName = "NotusImportSelectedPreset";
        private const string ImportLastExportPresetName = "NotusImportLastExportPreset";
        private const string ImportDesktopPresetName = "NotusImportDesktopPreset";
        private const string ImportDocumentsPresetName = "NotusImportDocumentsPreset";

        private const string CalculationAbntPresetName = "NotusMethodAbntSimplificado";
        private const string CalculationAshraePresetName = "NotusMethodAshraeSimplificado";
        private const string CalculationConservativePresetName = "NotusMethodConservador";

        internal static ComboBox ExportCsvComboBox { get; private set; }
        internal static ComboBox ImportCsvComboBox { get; private set; }
        internal static ComboBox CalculationMethodComboBox { get; private set; }

        private static ComboBoxMember _exportCustomMember;
        private static ComboBoxMember _importSelectedMember;
        private static string _exportCsvPath = "";
        private static string _importCsvPath = "";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                try { application.CreateRibbonTab(TabName); } catch { }
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                HvacSettingsService.Load();

                BuildEnvironmentsPanel(application, assemblyPath);
                BuildCalculationPanel(application, assemblyPath);
                BuildParametersPanel(application, assemblyPath);
                BuildReportsPanel(application, assemblyPath);
                BuildConfigurationPanel(application, assemblyPath);

                HvacLogService.Info("Ribbon carregada com sucesso.");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                HvacLogService.Error("Falha ao carregar Ribbon", ex);
                TaskDialog dialog = new TaskDialog(ProductInfo.FullName)
                {
                    MainInstruction = "Não foi possível carregar a Ribbon do Notus.",
                    MainContent = ex.Message,
                    CommonButtons = TaskDialogCommonButtons.Ok
                };
                dialog.Show();
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            ExportCsvComboBox = null;
            ImportCsvComboBox = null;
            CalculationMethodComboBox = null;
            _exportCustomMember = null;
            _importSelectedMember = null;
            _exportCsvPath = "";
            _importCsvPath = "";
            return Result.Succeeded;
        }

        private void BuildEnvironmentsPanel(UIControlledApplication app, string assemblyPath)
        {
            RibbonPanel panel = app.CreateRibbonPanel(TabName, "Ambientes");

            panel.AddItem(Button("NotusReadEnvironments", "Ler\nAmbientes", assemblyPath, "NotusRevitPlugin.ReadEnvironmentsCommand", "Lê Rooms, MEP Spaces, IfcSpace e Ambientes HVAC manuais.", "Assets/Ribbon/validate_16.png", "Assets/Ribbon/validate_32.png"));

            PushButtonData manual = Button("NotusCreateManualEnvironment", "Criar Ambiente\nManual", assemblyPath, "NotusRevitPlugin.CreateManualEnvironmentCommand", "Cria Ambiente Notus manual a partir de elementos IFC/arquitetura selecionados.", "Assets/Ribbon/tools_16.png", "Assets/Ribbon/tools_32.png");
            PushButtonData editManual = Button("NotusEditManualEnvironment", "Editar Ambiente\nManual", assemblyPath, "NotusRevitPlugin.EditManualEnvironmentCommand", "Edita os dados do Ambiente Notus manual selecionado e salva no projeto.", "Assets/Ribbon/tools_16.png", "Assets/Ribbon/tools_32.png");
            PushButtonData assistant = Button("NotusIfcAssistant", "Assistente\nIFC", assemblyPath, "NotusRevitPlugin.IfcManualEnvironmentWizardCommand", "Ajuda a criar Ambientes Notus quando o IFC veio sem IfcSpace/Rooms.", "Assets/Ribbon/tools_16.png", "Assets/Ribbon/tools_32.png");
            PushButtonData detect = Button("NotusDetectIfc", "Diagnostico\nIFC", assemblyPath, "NotusRevitPlugin.DetectIfcEnvironmentsCommand", "Diagnostico IFC avancado: DirectShape, ImportInstance, categorias, IFC GUID, links e elementos visiveis.", "Assets/Ribbon/validate_16.png", "Assets/Ribbon/validate_32.png");
            PushButtonData validate = Button("NotusValidateModel", "Verificar\nmodelo", assemblyPath, "NotusRevitPlugin.ValidateModelCommand", "Mostra resumo de ambientes, parametros e dados disponiveis.", "Assets/Ribbon/validate_16.png", "Assets/Ribbon/validate_32.png");
            panel.AddStackedItems(manual, editManual, assistant);
            panel.AddStackedItems(detect, validate);
        }

        private void BuildCalculationPanel(UIControlledApplication app, string assemblyPath)
        {
            RibbonPanel panel = app.CreateRibbonPanel(TabName, "Cálculo HVAC");

            panel.AddItem(Button("NotusCalculateDirect", "Calcular\nHVAC", assemblyPath, "NotusRevitPlugin.CalculateHvacCommand", "Calcula HVAC direto no Revit e grava BTU/h, TR, SHR, vazões, ACH e memorial.", "Assets/Ribbon/calculate_16.png", "Assets/Ribbon/calculate_32.png"));

            ComboBoxData methodCombo = new ComboBoxData(CalculationMethodComboBoxName) { ToolTip = "Perfil de cálculo/norma usada no pré-dimensionamento." };
            PushButtonData saveResults = Button("NotusSaveResults", "Gravar\nResultados", assemblyPath, "NotusRevitPlugin.SaveResultsCommand", "Informa como os resultados são gravados nos parâmetros do Revit.", "Assets/Ribbon/parameters_16.png", "Assets/Ribbon/parameters_32.png");
            PushButtonData critical = Button("NotusCriticalRooms", "Ambientes\nCríticos", assemblyPath, "NotusRevitPlugin.CriticalRoomsCommand", "Lista os ambientes com maior TR/carga térmica.", "Assets/Ribbon/chart_16.png", "Assets/Ribbon/chart_32.png");
            var items = panel.AddStackedItems(methodCombo, saveResults, critical);
            CalculationMethodComboBox = items[0] as ComboBox;
            if (CalculationMethodComboBox != null)
            {
                ComboBoxMember abnt = AddComboMember(CalculationMethodComboBox, CalculationAbntPresetName, "Método: ABNT simplificado", "Preset ABNT/NBR parametrizado para pré-dimensionamento.", "Assets/Ribbon/parameters_16.png");
                AddComboMember(CalculationMethodComboBox, CalculationAshraePresetName, "Método: ASHRAE simplificado", "Preset ASHRAE 62.1 parametrizado.", "Assets/Ribbon/parameters_16.png");
                AddComboMember(CalculationMethodComboBox, CalculationConservativePresetName, "Método: Conservador", "Preset conservador com fatores de segurança maiores.", "Assets/Ribbon/validate_16.png");
                if (abnt != null) CalculationMethodComboBox.Current = abnt;
            }
        }

        private void BuildParametersPanel(UIControlledApplication app, string assemblyPath)
        {
            RibbonPanel panel = app.CreateRibbonPanel(TabName, "Parâmetros");
            panel.AddItem(Button("NotusCreateParameters", "Criar\nParâmetros", assemblyPath, "NotusRevitPlugin.CreateParametersCommand", "Cria automaticamente parâmetros Notus nos Rooms, Spaces e Generic Models.", "Assets/Ribbon/parameters_16.png", "Assets/Ribbon/parameters_32.png"));
            PushButtonData schedule = Button("NotusCreateSchedule", "Criar\nTabela", assemblyPath, "NotusRevitPlugin.CreateScheduleChartCommand", "Cria tabela técnica automática por ambiente e gráfico de TR.", "Assets/Ribbon/chart_16.png", "Assets/Ribbon/chart_32.png");
            PushButtonData help = Button("NotusHelp", "Fluxo\nde uso", assemblyPath, "NotusRevitPlugin.HelpCommand", "Mostra o fluxo recomendado de uso.", "Assets/Ribbon/help_16.png", "Assets/Ribbon/help_32.png");
            panel.AddStackedItems(schedule, help);
        }

        private void BuildReportsPanel(UIControlledApplication app, string assemblyPath)
        {
            RibbonPanel panel = app.CreateRibbonPanel(TabName, "Relatórios");

            panel.AddItem(Button("NotusExportRooms", "Exportar\nCSV", assemblyPath, "NotusRevitPlugin.ExportRoomsCommand", "Exporta ambientes e resultados para CSV.", "Assets/Ribbon/export_16.png", "Assets/Ribbon/export_32.png"));

            ComboBoxData exportCombo = new ComboBoxData(ExportCsvComboBoxName) { ToolTip = "Destino do CSV exportado." };
            PushButtonData browseExport = Button("NotusBrowseExportCsv", "Salvar\ncomo", assemblyPath, "NotusRevitPlugin.SelectExportCsvPathCommand", "Escolhe o caminho do CSV exportado.", "Assets/Ribbon/folder_16.png", "Assets/Ribbon/folder_32.png");
            PushButtonData openFolder = Button("NotusOpenExportFolder", "Abrir\npasta", assemblyPath, "NotusRevitPlugin.OpenExportFolderCommand", "Abre a pasta de exportação.", "Assets/Ribbon/open_folder_16.png", "Assets/Ribbon/open_folder_32.png");
            var expItems = panel.AddStackedItems(exportCombo, browseExport, openFolder);
            ExportCsvComboBox = expItems[0] as ComboBox;
            if (ExportCsvComboBox != null)
            {
                ComboBoxMember desktop = AddComboMember(ExportCsvComboBox, ExportDesktopPresetName, "Destino: Área de trabalho", "Salva na Área de trabalho.", "Assets/Ribbon/folder_16.png");
                AddComboMember(ExportCsvComboBox, ExportDocumentsPresetName, "Destino: Documentos", "Salva em Documentos.", "Assets/Ribbon/folder_16.png");
                AddComboMember(ExportCsvComboBox, ExportDownloadsPresetName, "Destino: Downloads", "Salva em Downloads.", "Assets/Ribbon/folder_16.png");
                _exportCustomMember = AddComboMember(ExportCsvComboBox, ExportCustomPresetName, "Destino: Personalizado", "Usa caminho escolhido em Salvar como.", "Assets/Ribbon/export_16.png");
                if (desktop != null) ExportCsvComboBox.Current = desktop;
            }

            PushButtonData import = Button("NotusImportResults", "Importar\nCSV", assemblyPath, "NotusRevitPlugin.ImportResultsCommand", "Importa CSV calculado e grava parâmetros.", "Assets/Ribbon/import_16.png", "Assets/Ribbon/import_32.png");
            PushButtonData excel = Button("NotusExportExcel", "Exportar\nExcel", assemblyPath, "NotusRevitPlugin.ExportExcelCommand", "Exporta relatório Excel/HTML com resultados HVAC.", "Assets/Ribbon/export_16.png", "Assets/Ribbon/export_32.png");
            PushButtonData memorial = Button("NotusMemorialPdf", "Memorial\nPDF", assemblyPath, "NotusRevitPlugin.ExportPdfMemorialCommand", "Gera memorial HTML pronto para salvar como PDF.", "Assets/Ribbon/chart_16.png", "Assets/Ribbon/chart_32.png");
            panel.AddStackedItems(import, excel, memorial);

            ComboBoxData importCombo = new ComboBoxData(ImportCsvComboBoxName) { ToolTip = "Origem do CSV calculado." };
            PushButtonData browseImport = Button("NotusBrowseImportCsv", "Escolher\nCSV", assemblyPath, "NotusRevitPlugin.SelectImportCsvPathCommand", "Seleciona CSV calculado para importar.", "Assets/Ribbon/folder_16.png", "Assets/Ribbon/folder_32.png");
            PushButtonData validateCsv = Button("NotusValidateImportCsv", "Validar\nCSV", assemblyPath, "NotusRevitPlugin.ValidateImportCsvCommand", "Valida o CSV antes da importação.", "Assets/Ribbon/validate_16.png", "Assets/Ribbon/validate_32.png");
            var impItems = panel.AddStackedItems(importCombo, browseImport, validateCsv);
            ImportCsvComboBox = impItems[0] as ComboBox;
            if (ImportCsvComboBox != null)
            {
                _importSelectedMember = AddComboMember(ImportCsvComboBox, ImportSelectedPresetName, "Origem: CSV selecionado", "Usa o último CSV escolhido.", "Assets/Ribbon/import_16.png");
                AddComboMember(ImportCsvComboBox, ImportLastExportPresetName, "Origem: último exportado", "Usa o CSV exportado nesta sessão.", "Assets/Ribbon/export_16.png");
                AddComboMember(ImportCsvComboBox, ImportDesktopPresetName, "Origem: Área de trabalho", "Procura CSV Notus mais recente na Área de trabalho.", "Assets/Ribbon/folder_16.png");
                AddComboMember(ImportCsvComboBox, ImportDocumentsPresetName, "Origem: Documentos", "Procura CSV Notus mais recente em Documentos.", "Assets/Ribbon/folder_16.png");
                if (_importSelectedMember != null) ImportCsvComboBox.Current = _importSelectedMember;
            }
        }

        private void BuildConfigurationPanel(UIControlledApplication app, string assemblyPath)
        {
            RibbonPanel panel = app.CreateRibbonPanel(TabName, "Configurações");
            panel.AddItem(Button("NotusConfiguration", "Configurações", assemblyPath, "NotusRevitPlugin.ConfigureHvacCommand", "Define cidade, norma, temperatura, umidade, ventilação e ocupação padrão.", "Assets/Ribbon/tools_16.png", "Assets/Ribbon/tools_32.png"));

            PulldownButtonData toolsData = new PulldownButtonData("NotusToolsPulldown", "Mais\nAções")
            {
                ToolTip = "Ações auxiliares e suporte."
            };
            PulldownButton tools = panel.AddItem(toolsData) as PulldownButton;
            if (tools == null) return;
            AddToolButton(tools, "NotusToolsOpenPluginFolder", "Abrir pasta do plugin", assemblyPath, "NotusRevitPlugin.OpenPluginFolderCommand", "Abre a pasta instalada do plugin.", "Assets/Ribbon/open_folder_16.png", "Assets/Ribbon/open_folder_32.png");
            AddToolButton(tools, "NotusToolsDiagnostics", "Enviar diagnóstico", assemblyPath, "NotusRevitPlugin.SendDiagnosticsCommand", "Gera arquivo de diagnóstico/log para suporte.", "Assets/Ribbon/help_16.png", "Assets/Ribbon/help_32.png");
            AddToolButton(tools, "NotusToolsHelp", "Fluxo de uso", assemblyPath, "NotusRevitPlugin.HelpCommand", "Mostra o fluxo recomendado.", "Assets/Ribbon/help_16.png", "Assets/Ribbon/help_32.png");
            AddToolButton(tools, "NotusToolsAbout", "Sobre", assemblyPath, "NotusRevitPlugin.AboutCommand", "Mostra versão e informações.", "Assets/Ribbon/info_16.png", "Assets/Ribbon/info_32.png");
            tools.AddSeparator();
            AddToolButton(tools, "NotusToolsUninstall", "Desinstalar plugin", assemblyPath, "NotusRevitPlugin.UninstallPluginCommand", "Remove o Notus do Revit (pede para fechar o Revit em seguida).", "Assets/Ribbon/uninstall_16.png", "Assets/Ribbon/uninstall_32.png");
        }

        // Cache simples para não reabrir o mesmo PNG do disco várias vezes ao montar a ribbon.
        private static readonly Dictionary<string, BitmapImage> _imageCache = new Dictionary<string, BitmapImage>();

        private static PushButtonData Button(string name, string text, string assemblyPath, string className, string tooltip, string image, string largeImage)
        {
            PushButtonData data = new PushButtonData(name, text, assemblyPath, className)
            {
                ToolTip = tooltip
            };

            BitmapImage small = LoadRibbonImage(assemblyPath, image);
            BitmapImage large = LoadRibbonImage(assemblyPath, largeImage);
            if (small != null) data.Image = small;
            if (large != null) data.LargeImage = large;
            return data;
        }

        // Carrega os ícones nativos (estilo Fluent/Segoe) que ficam em Assets/Ribbon, ao lado da DLL.
        // Se o arquivo não existir (ex.: instalação antiga sem a pasta Assets), a ribbon volta a
        // funcionar só com texto — nunca lança exceção e nunca impede o carregamento do plugin.
        private static BitmapImage LoadRibbonImage(string assemblyPath, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return null;

            string cacheKey = assemblyPath + "|" + relativePath;
            if (_imageCache.TryGetValue(cacheKey, out BitmapImage cached)) return cached;

            try
            {
                string pluginFolder = Path.GetDirectoryName(assemblyPath);
                if (string.IsNullOrWhiteSpace(pluginFolder)) return null;

                string fullPath = Path.Combine(pluginFolder, relativePath.Replace("/", "\\"));
                if (!File.Exists(fullPath)) return null;

                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(fullPath, UriKind.Absolute);
                image.EndInit();
                image.Freeze();

                _imageCache[cacheKey] = image;
                return image;
            }
            catch
            {
                return null;
            }
        }

        private static void AddToolButton(PulldownButton pulldown, string name, string text, string assemblyPath, string className, string tooltip, string image, string largeImage)
        {
            pulldown.AddPushButton(Button(name, text, assemblyPath, className, tooltip, image, largeImage));
        }

        internal static string GetCalculationMethodPreset()
        {
            string currentName = GetCurrentComboMemberName(CalculationMethodComboBox);
            if (currentName == CalculationAshraePresetName) return "ASHRAE simplificado";
            if (currentName == CalculationConservativePresetName) return "Conservador";
            if (currentName == CalculationAbntPresetName) return "ABNT simplificado";
            return HvacSettingsService.Load().DefaultNorm;
        }

        internal static string GetExportCsvPath()
        {
            string currentName = GetCurrentComboMemberName(ExportCsvComboBox);
            if (currentName == ExportCustomPresetName && !string.IsNullOrWhiteSpace(_exportCsvPath)) return _exportCsvPath;
            return "";
        }

        internal static string GetExportCsvFolder()
        {
            string currentName = GetCurrentComboMemberName(ExportCsvComboBox);
            if (currentName == ExportDocumentsPresetName) return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (currentName == ExportDownloadsPresetName)
            {
                string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (Directory.Exists(downloads)) return downloads;
            }
            return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        }

        internal static void SetExportCsvPath(string path)
        {
            _exportCsvPath = path ?? "";
            SelectComboMember(ExportCsvComboBox, _exportCustomMember);
        }

        internal static string GetImportCsvPath()
        {
            string currentName = GetCurrentComboMemberName(ImportCsvComboBox);
            if (currentName == ImportLastExportPresetName && !string.IsNullOrWhiteSpace(_exportCsvPath)) return _exportCsvPath;
            if (currentName == ImportDesktopPresetName) return FindLatestCsvInFolder(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            if (currentName == ImportDocumentsPresetName) return FindLatestCsvInFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            if (!string.IsNullOrWhiteSpace(_importCsvPath)) return _importCsvPath;
            return "";
        }

        internal static void SetImportCsvPath(string path)
        {
            _importCsvPath = path ?? "";
            SelectComboMember(ImportCsvComboBox, _importSelectedMember);
        }

        internal static string GetPluginFolder()
        {
            try { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ""; }
            catch { return ""; }
        }

        private static string FindLatestCsvInFolder(string folder)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return "";
                string newest = "";
                DateTime time = DateTime.MinValue;
                foreach (string file in Directory.GetFiles(folder, "*.csv", SearchOption.TopDirectoryOnly))
                {
                    DateTime last = File.GetLastWriteTime(file);
                    if (last >= time)
                    {
                        newest = file;
                        time = last;
                    }
                }
                return newest;
            }
            catch { return ""; }
        }

        private static ComboBoxMember AddComboMember(ComboBox comboBox, string name, string text, string tooltip, string imagePath)
        {
            if (comboBox == null) return null;
            ComboBoxMemberData data = new ComboBoxMemberData(name, text) { ToolTip = tooltip };
            BitmapImage icon = LoadRibbonImage(Assembly.GetExecutingAssembly().Location, imagePath);
            if (icon != null) data.Image = icon;
            return comboBox.AddItem(data);
        }

        private static string GetCurrentComboMemberName(ComboBox comboBox)
        {
            try { return comboBox != null && comboBox.Current != null ? comboBox.Current.Name : ""; }
            catch { return ""; }
        }

        private static void SelectComboMember(ComboBox comboBox, ComboBoxMember member)
        {
            try { if (comboBox != null && member != null) comboBox.Current = member; } catch { }
        }

    }
}


