using System.Text.RegularExpressions;
using SuchByte.MacroDeck.Logging;
using SuchByte.MacroDeck.Plugins;
using SuchByte.MacroDeck.Variables;
using VolumeMixerPlugin.Actions;
using VolumeMixerPlugin.Services;

namespace VolumeMixerPlugin;

public class VolumeMixerPluginMain : MacroDeckPlugin
{
    public static VolumeMixerPluginMain? Instance { get; private set; }
    public AudioService? AudioService { get; private set; }

    private System.Timers.Timer? _refreshTimer;
    private int _updateRunning;
    private SynchronizationContext? _syncContext;
    private int _disposed;
    private int _shutdownHooksRegistered;

    private const int RefreshIntervalMs = 2000;

    public override void Enable()
    {
        Instance = this;
        _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();

        RegisterShutdownHooks();

        AudioService = new AudioService(this);

        Actions = new List<PluginAction>
        {
            new SetAppVolumeAction(),
            new VolumeUpAction(),
            new VolumeDownAction(),
            new MuteAppAction(),
            new SetDefaultDeviceAction(),
            new RefreshDevicesAction()
        };

        UpdateVariables();

        _refreshTimer = new System.Timers.Timer(RefreshIntervalMs) { AutoReset = true };
        _refreshTimer.Elapsed += (_, _) => _syncContext?.Post(_ => UpdateVariables(), null);
        _refreshTimer.Start();
    }

    public void Disable()
    {
        DisposeResources();
    }

    private void RegisterShutdownHooks()
    {
        if (Interlocked.Exchange(ref _shutdownHooksRegistered, 1) == 1) return;

        AppDomain.CurrentDomain.ProcessExit += (_, _) => DisposeResources();
        AppDomain.CurrentDomain.DomainUnload += (_, _) => DisposeResources();
    }

    private void DisposeResources()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
        _refreshTimer = null;

        AudioService?.Dispose();
        AudioService = null;
        Instance = null;
    }

    public void UpdateVariables()
    {
        if (AudioService == null) return;
        if (Interlocked.Exchange(ref _updateRunning, 1) == 1) return;

        try
        {
            var defaultDeviceInfo = AudioService.GetDefaultMultimediaDeviceInfo();
            if (defaultDeviceInfo.HasValue)
            {
                VariableManager.SetValue("volumemixer_default_device", defaultDeviceInfo.Value.Name, VariableType.String, this, Array.Empty<string>());
                VariableManager.SetValue("volumemixer_default_device_id", defaultDeviceInfo.Value.Id, VariableType.String, this, Array.Empty<string>());
            }

            var commDeviceInfo = AudioService.GetDefaultCommunicationsDeviceInfo();
            if (commDeviceInfo.HasValue)
            {
                VariableManager.SetValue("volumemixer_comm_device", commDeviceInfo.Value.Name, VariableType.String, this, Array.Empty<string>());
            }

            var sessions = AudioService.SnapshotDefaultDeviceSessions();
            foreach (var (processName, volume, muted) in sessions)
            {
                var safeName = SanitizeVariableName(processName);
                VariableManager.SetValue($"volumemixer_app_{safeName}_volume", volume, VariableType.Integer, this, Array.Empty<string>());
                VariableManager.SetValue($"volumemixer_app_{safeName}_muted", muted, VariableType.Bool, this, Array.Empty<string>());
            }

            var deviceNames = AudioService.GetActivePlaybackDeviceNames();
            VariableManager.SetValue("volumemixer_devices", string.Join(", ", deviceNames), VariableType.String, this, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            MacroDeckLogger.Warning(this, $"Error updating variables: {ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _updateRunning, 0);
        }
    }

    private static string SanitizeVariableName(string name)
    {
        var lower = name.ToLowerInvariant();
        var cleaned = Regex.Replace(lower, @"[^a-z0-9_]+", "_");
        cleaned = cleaned.Trim('_');
        if (cleaned.Length == 0) cleaned = "unknown";
        if (!char.IsLetter(cleaned[0])) cleaned = "app_" + cleaned;
        return cleaned;
    }

}
