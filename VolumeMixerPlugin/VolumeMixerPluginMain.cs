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

    private static readonly object _trackedAppsLock = new();
    private static readonly Dictionary<string, int> _trackedAppsRefCount = new(StringComparer.OrdinalIgnoreCase);

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
            new SetDefaultMicrophoneAction(),
            new SetDefaultDevicesPairAction(),
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
            }

            var commDeviceInfo = AudioService.GetDefaultCommunicationsDeviceInfo();
            if (commDeviceInfo.HasValue)
            {
                VariableManager.SetValue("volumemixer_comm_device", commDeviceInfo.Value.Name, VariableType.String, this, Array.Empty<string>());
            }

            var defaultMicInfo = AudioService.GetDefaultCaptureDeviceInfo();
            if (defaultMicInfo.HasValue)
            {
                VariableManager.SetValue("volumemixer_default_mic", defaultMicInfo.Value.Name, VariableType.String, this, Array.Empty<string>());
            }

            var commMicInfo = AudioService.GetDefaultCaptureCommDeviceInfo();
            if (commMicInfo.HasValue)
            {
                VariableManager.SetValue("volumemixer_comm_mic", commMicInfo.Value.Name, VariableType.String, this, Array.Empty<string>());
            }

            var trackedApps = GetTrackedAppsSnapshot();
            if (trackedApps.Count == 0) return;

            var sessions = AudioService.SnapshotDefaultDeviceSessions();
            var sessionsByProcessName = sessions.ToDictionary(x => x.ProcessName, x => (x.Volume, x.Muted), StringComparer.OrdinalIgnoreCase);

            foreach (var appName in trackedApps)
            {
                if (!sessionsByProcessName.TryGetValue(appName, out var sessionInfo)) continue;

                var safeName = SanitizeVariableName(appName);
                VariableManager.SetValue($"volumemixer_app_{safeName}_volume", sessionInfo.Volume, VariableType.Integer, this, Array.Empty<string>());
                VariableManager.SetValue($"volumemixer_app_{safeName}_muted", sessionInfo.Muted, VariableType.Bool, this, Array.Empty<string>());
            }
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

    internal static string SanitizeVariableName(string name)
    {
        var lower = name.ToLowerInvariant();
        var cleaned = Regex.Replace(lower, @"[^a-z0-9_]+", "_");
        cleaned = cleaned.Trim('_');
        if (cleaned.Length == 0) cleaned = "unknown";
        if (!char.IsLetter(cleaned[0])) cleaned = "app_" + cleaned;
        return cleaned;
    }

    internal static void TrackAppUsage(string? appName, string? previousAppName = null)
    {
        if (Instance == null) return;

        if (!string.IsNullOrWhiteSpace(previousAppName) && !string.Equals(previousAppName, appName, StringComparison.OrdinalIgnoreCase))
        {
            UntrackAppInternal(previousAppName);
        }

        if (string.IsNullOrWhiteSpace(appName)) return;
        TrackAppInternal(appName);
    }

    internal static void UntrackAppUsage(string? appName)
    {
        if (Instance == null) return;
        if (string.IsNullOrWhiteSpace(appName)) return;

        UntrackAppInternal(appName);
    }

    private static void TrackAppInternal(string appName)
    {
        lock (_trackedAppsLock)
        {
            if (_trackedAppsRefCount.TryGetValue(appName, out var count))
            {
                _trackedAppsRefCount[appName] = count + 1;
                return;
            }

            _trackedAppsRefCount[appName] = 1;
        }
    }

    private static void UntrackAppInternal(string appName)
    {
        var shouldDelete = false;
        lock (_trackedAppsLock)
        {
            if (!_trackedAppsRefCount.TryGetValue(appName, out var count)) return;

            count--;
            if (count <= 0)
            {
                _trackedAppsRefCount.Remove(appName);
                shouldDelete = true;
            }
            else
            {
                _trackedAppsRefCount[appName] = count;
            }
        }

        if (!shouldDelete) return;

        try
        {
            var safeName = SanitizeVariableName(appName);
            VariableManager.DeleteVariable($"volumemixer_app_{safeName}_volume");
            VariableManager.DeleteVariable($"volumemixer_app_{safeName}_muted");
        }
        catch
        {
        }
    }

    private static IReadOnlyList<string> GetTrackedAppsSnapshot()
    {
        lock (_trackedAppsLock)
        {
            return _trackedAppsRefCount.Keys.OrderBy(x => x).ToList();
        }
    }

}
