using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace NotusSetup;

internal static class Program
{
    private const string ProductName = "Notus";
    private const string RevitVersion = "2025";
    private const string DllName = "NotusRevitPlugin.dll";
    private const string AddinName = "Notus.addin";
    private const string AddinId = "8D83E0A4-8D57-4C8D-B3F2-AC9D8E123456";
    private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Notus_Revit2025";

    [STAThread]
    private static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        bool silent = HasArg(args, "/quiet") || HasArg(args, "/silent") || HasArg(args, "--silent");
        bool uninstall = HasArg(args, "/uninstall") || HasArg(args, "--uninstall");

        try
        {
            if (uninstall)
            {
                return RunWithProgress("Desinstalando Notus", "Removendo plugin do Revit 2025...", silent, DoUninstall);
            }

            return RunWithProgress("Instalando Notus", "Instalando plugin no Revit 2025...", silent, DoInstall);
        }
        catch (Exception ex)
        {
            string message = "Operação não concluída.\n\n" + ex.Message;
            if (silent) Console.Error.WriteLine(message);
            else MessageBox.Show(message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    private static int RunWithProgress(string title, string text, bool silent, Action<ProgressForm> action)
    {
        if (silent)
        {
            action(null!);
            return 0;
        }

        using ProgressForm form = new ProgressForm(title, text);
        Exception? error = null;
        form.Shown += (_, _) =>
        {
            try
            {
                action(form);
                form.SetProgress(100, "Concluído.");
                Thread.Sleep(350);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                form.Close();
            }
        };

        Application.Run(form);

        if (error != null) throw error;

        MessageBox.Show(
            title.StartsWith("Desinstalando", StringComparison.OrdinalIgnoreCase)
                ? "Notus removido com sucesso.\n\nFeche e abra o Revit 2025, se ele estiver aberto."
                : "Notus instalado com sucesso.\n\nFeche e abra o Revit 2025.\nA aba sera exibida como Notus.",
            ProductName,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        return 0;
    }

    private static void DoInstall(ProgressForm form)
    {
        string addinDir = GetAddinDir();
        string dllPath = Path.Combine(addinDir, DllName);
        string addinPath = Path.Combine(addinDir, AddinName);
        string installedInstaller = Path.Combine(addinDir, "Notus_Uninstall.exe");

        form?.SetProgress(10, "Verificando Autodesk Revit 2025...");
        CheckRevitInstalled();

        form?.SetProgress(20, "Preparando pasta Addins do Revit...");
        Directory.CreateDirectory(addinDir);

        form?.SetProgress(25, "Removendo versões antigas...");
        RemoveOldVersions(addinDir);

        form?.SetProgress(45, "Copiando plugin...");
        ExtractEmbeddedFile("Payload.NotusRevitPlugin.dll", dllPath, "Payload do plugin não foi encontrado dentro do instalador.");

        form?.SetProgress(58, "Copiando ícones e arquivos auxiliares...");
        ExtractEmbeddedFolder("Payload.Assets.Ribbon.", Path.Combine(addinDir, "Assets", "Ribbon"));
        ExtractOptionalEmbeddedFile("Payload.Notus_SharedParameters.txt", Path.Combine(addinDir, "Notus_SharedParameters.txt"));

        form?.SetProgress(70, "Registrando add-in no Revit...");
        File.WriteAllText(addinPath, BuildAddinXml(dllPath), Encoding.UTF8);

        form?.SetProgress(80, "Criando desinstalador...");
        CopySelf(installedInstaller);
        RegisterUninstaller(installedInstaller);
        CreateStartMenuShortcut(installedInstaller);

        form?.SetProgress(95, "Finalizando...");
    }

    private static void DoUninstall(ProgressForm form)
    {
        string addinDir = GetAddinDir();
        form?.SetProgress(20, "Removendo arquivos do plugin...");
        DeleteIfExists(Path.Combine(addinDir, AddinName));
        DeleteIfExists(Path.Combine(addinDir, DllName));
        DeleteIfExists(Path.Combine(addinDir, "Notus_SharedParameters.txt"));
        DeleteDirectoryIfExists(Path.Combine(addinDir, "Assets", "Ribbon"));
        DeleteEmptyDirectoryIfExists(Path.Combine(addinDir, "Assets"));

        form?.SetProgress(65, "Removendo versões antigas...");
        RemoveOldVersions(addinDir);

        form?.SetProgress(85, "Removendo registro do desinstalador...");
        try { Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false); } catch { }
        RemoveStartMenuShortcut();
    }


    private static void CheckRevitInstalled()
    {
        string revitExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autodesk", "Revit 2025", "Revit.exe");
        if (!File.Exists(revitExe))
        {
            // Não bloqueia totalmente, pois alguns ambientes corporativos usam caminhos personalizados.
            // O add-in será instalado em AppData, mas o usuário deve validar se o Revit 2025 existe.
        }
    }

    private static void CreateStartMenuShortcut(string uninstallExe)
    {
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Notus");
            Directory.CreateDirectory(folder);
            string cmd = Path.Combine(folder, "Desinstalar Notus Revit 2025.cmd");
            File.WriteAllText(cmd, "@echo off\r\n\"" + uninstallExe + "\" /uninstall\r\n", Encoding.UTF8);
        }
        catch { }
    }

    private static void RemoveStartMenuShortcut()
    {
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Notus");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
        catch { }
    }

    private static string GetAddinDir()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Autodesk", "Revit", "Addins", RevitVersion);
    }

    private static void RemoveOldVersions(string addinDir)
    {
        if (!Directory.Exists(addinDir)) return;
        string[] patterns = { "*Notus*.addin", "*Notus*.dll", "*Notus*.addin", "*Notus*.dll" };
        foreach (string pattern in patterns)
        {
            foreach (string file in Directory.GetFiles(addinDir, pattern, SearchOption.TopDirectoryOnly))
            {
                DeleteIfExists(file);
            }
        }
    }

    private static void DeleteIfExists(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Arquivo pode estar bloqueado se o Revit estiver aberto.
        }
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
        catch
        {
            // Pasta pode estar bloqueada se o Revit estiver aberto.
        }
    }

    private static void DeleteEmptyDirectoryIfExists(string path)
    {
        try
        {
            if (Directory.Exists(path) && Directory.GetFileSystemEntries(path).Length == 0) Directory.Delete(path, false);
        }
        catch { }
    }

    private static void ExtractOptionalEmbeddedFile(string logicalName, string outputPath)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream(logicalName);
        if (stream == null) return;
        ExtractEmbeddedFile(logicalName, outputPath, "Payload opcional não foi encontrado dentro do instalador: " + logicalName);
    }

    private static void ExtractEmbeddedFolder(string logicalPrefix, string outputDirectory)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Directory.CreateDirectory(outputDirectory);

        foreach (string resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(logicalPrefix, StringComparison.OrdinalIgnoreCase)) continue;

            string fileName = resourceName.Substring(logicalPrefix.Length);
            if (string.IsNullOrWhiteSpace(fileName)) continue;

            ExtractEmbeddedFile(resourceName, Path.Combine(outputDirectory, fileName), "Payload de asset não foi encontrado dentro do instalador: " + resourceName);
        }
    }

    private static void ExtractEmbeddedFile(string logicalName, string outputPath, string missingMessage)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream(logicalName);
        if (stream == null) throw new InvalidOperationException(missingMessage);

        string? folder = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(folder)) Directory.CreateDirectory(folder);

        string tempPath = outputPath + ".tmp";
        using (FileStream fileStream = File.Create(tempPath)) stream.CopyTo(fileStream);
        if (File.Exists(outputPath)) File.Delete(outputPath);
        File.Move(tempPath, outputPath);
    }

    private static void CopySelf(string targetPath)
    {
        try
        {
            string? self = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(self) && File.Exists(self)) File.Copy(self, targetPath, true);
        }
        catch { }
    }

    private static void RegisterUninstaller(string uninstallExe)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(UninstallKey);
        key.SetValue("DisplayName", "Notus Revit 2025");
        key.SetValue("DisplayVersion", "1.0.0");
        key.SetValue("Publisher", "Notus");
        key.SetValue("DisplayIcon", uninstallExe);
        key.SetValue("UninstallString", "\"" + uninstallExe + "\" /uninstall");
        key.SetValue("QuietUninstallString", "\"" + uninstallExe + "\" /uninstall /quiet");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }

    private static string BuildAddinXml(string dllPath)
    {
        string safePath = dllPath.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
               "<RevitAddIns>\r\n" +
               "  <AddIn Type=\"Application\">\r\n" +
               "    <Name>Notus</Name>\r\n" +
               $"    <Assembly>{safePath}</Assembly>\r\n" +
               $"    <AddInId>{AddinId}</AddInId>\r\n" +
               "    <FullClassName>NotusRevitPlugin.App</FullClassName>\r\n" +
               "    <VendorId>NORLAX</VendorId>\r\n" +
               "    <VendorDescription>Notus | Calculo HVAC inteligente para Revit 2025</VendorDescription>\r\n" +
               "  </AddIn>\r\n" +
               "</RevitAddIns>\r\n";
    }

    private static bool HasArg(string[] args, string arg)
    {
        foreach (string item in args)
            if (string.Equals(item, arg, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}

internal sealed class ProgressForm : Form
{
    private readonly Label _label;
    private readonly ProgressBar _bar;

    public ProgressForm(string title, string text)
    {
        Text = title;
        Width = 460;
        Height = 155;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _label = new Label { Left = 18, Top = 18, Width = 410, Height = 40, Text = text };
        _bar = new ProgressBar { Left = 18, Top = 68, Width = 410, Height = 22, Style = ProgressBarStyle.Continuous, Minimum = 0, Maximum = 100 };

        Controls.Add(_label);
        Controls.Add(_bar);
    }

    public void SetProgress(int value, string text)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<int, string>(SetProgress), value, text);
            return;
        }

        _bar.Value = Math.Max(_bar.Minimum, Math.Min(_bar.Maximum, value));
        _label.Text = text;
        Refresh();
        Application.DoEvents();
    }
}



