using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.GUI.Dialogs;
using SuchByte.MacroDeck.Icons;
using VolumeMixerPlugin.GUI;
using VolumeMixerPlugin.Utils;

namespace VolumeMixerPlugin.Services;

public static class IconImportService
{
    public static bool PromptAndImportIcon(string processName)
    {
        string? exePath = GetExecutablePath(processName);
        if (string.IsNullOrEmpty(exePath))
        {
            ShowError("Could not find executable path for this application.");
            return false;
        }

        using var confirmDialog = new MessageBox();
        var result = confirmDialog.ShowDialog(
            "Import Icon",
            $"Do you want to import the icon for {processName}?",
            MessageBoxButtons.YesNo);

        if (result != DialogResult.Yes)
            return false;

        return ImportIconFromPath(exePath, processName);
    }

    public static bool ImportIconFromPath(string exePath, string displayName)
    {
        using var qualityDialog = new IconImportQuality();
        if (qualityDialog.ShowDialog() != DialogResult.OK)
            return false;

        Bitmap? icon = ShellIcon.ExtractIconFromFile(exePath);
        if (icon == null)
        {
            ShowError("Failed to extract icon from the application.");
            return false;
        }

        try
        {
            if (qualityDialog.Pixels > 0 && (icon.Width != qualityDialog.Pixels || icon.Height != qualityDialog.Pixels))
            {
                var resized = ImageResize.Resize(icon, qualityDialog.Pixels, qualityDialog.Pixels);
                icon.Dispose();
                icon = resized;
            }

            using var packSelector = new IconPackSelector();
            if (packSelector.ShowDialog() != DialogResult.OK)
            {
                icon.Dispose();
                return false;
            }

            IconPack? iconPack = IconManager.GetIconPackByName(packSelector.SelectedIconPack);
            if (iconPack == null)
            {
                ShowError("Could not find the selected icon pack.");
                icon.Dispose();
                return false;
            }

            IconManager.AddIconImage(iconPack, icon);

            using var successDialog = new MessageBox();
            successDialog.ShowDialog(
                "Import Icon",
                $"Icon successfully imported to '{packSelector.SelectedIconPack}'.",
                MessageBoxButtons.OK);

            return true;
        }
        finally
        {
            icon?.Dispose();
        }
    }

    private static string? GetExecutablePath(string processName)
    {
        try
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
                return null;

            foreach (var proc in processes)
            {
                try
                {
                    string? path = proc.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(path))
                        return path;
                }
                catch
                {
                }
                finally
                {
                    proc.Dispose();
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static void ShowError(string message)
    {
        using var msgBox = new MessageBox();
        msgBox.ShowDialog("Import Icon", message, MessageBoxButtons.OK);
    }
}
