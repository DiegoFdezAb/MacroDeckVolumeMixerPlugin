using System.Diagnostics;
using NAudio.CoreAudioApi;
using SuchByte.MacroDeck.Logging;
using SuchByte.MacroDeck.Plugins;

namespace VolumeMixerPlugin.Services;

public sealed class AudioService : IDisposable
{
    private readonly MMDeviceEnumerator _deviceEnumerator = new();
    private readonly PolicyConfigClient _policyConfig = new();
    private readonly MacroDeckPlugin _owner;
    private bool _disposed;

    public AudioService(MacroDeckPlugin owner)
    {
        _owner = owner;
    }

    public (string Name, string Id)? GetDefaultMultimediaDeviceInfo()
    {
        try
        {
            using var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return (device.FriendlyName, device.ID);
        }
        catch
        {
            return null;
        }
    }

    public (string Name, string Id)? GetDefaultCommunicationsDeviceInfo()
    {
        try
        {
            using var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Communications);
            return (device.FriendlyName, device.ID);
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyList<string> GetActivePlaybackDeviceNames()
    {
        var result = new List<string>();
        try
        {
            var devices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (var device in devices)
            {
                try
                {
                    result.Add(device.FriendlyName);
                }
                finally
                {
                    device.Dispose();
                }
            }
        }
        catch
        {
        }
        return result;
    }

    public IReadOnlyList<(string Name, string Id)> GetActivePlaybackDevices()
    {
        var result = new List<(string, string)>();
        try
        {
            var devices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (var device in devices)
            {
                try
                {
                    result.Add((device.FriendlyName, device.ID));
                }
                finally
                {
                    device.Dispose();
                }
            }
        }
        catch
        {
        }
        return result;
    }

    public IReadOnlyList<string> GetActiveAppNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var devices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            MacroDeckLogger.Info(_owner, $"[VolumeMixer] Found {devices.Count} active audio devices");
            
            foreach (var device in devices)
            {
                try
                {
                    MacroDeckLogger.Info(_owner, $"[VolumeMixer] Scanning device: {device.FriendlyName}");
                    var sessions = device.AudioSessionManager.Sessions;
                    MacroDeckLogger.Info(_owner, $"[VolumeMixer] Device {device.FriendlyName} has {sessions.Count} sessions");
                    
                    for (int i = 0; i < sessions.Count; i++)
                    {
                        try
                        {
                            var session = sessions[i];
                            var pid = (int)session.GetProcessID;
                            MacroDeckLogger.Info(_owner, $"[VolumeMixer] Session {i}: PID={pid}, State={session.State}");
                            
                            if (pid == 0) continue;
                            
                            try
                            {
                                using var proc = Process.GetProcessById(pid);
                                if (!string.IsNullOrWhiteSpace(proc.ProcessName))
                                {
                                    MacroDeckLogger.Info(_owner, $"[VolumeMixer] Found app: {proc.ProcessName}");
                                    names.Add(proc.ProcessName);
                                }
                            }
                            catch (Exception procEx)
                            {
                                MacroDeckLogger.Warning(_owner, $"[VolumeMixer] Could not get process for PID {pid}: {procEx.Message}");
                            }
                        }
                        catch (Exception sessionEx)
                        {
                            MacroDeckLogger.Warning(_owner, $"[VolumeMixer] Error reading session {i}: {sessionEx.Message}");
                        }
                    }
                }
                catch (Exception deviceEx)
                {
                    MacroDeckLogger.Warning(_owner, $"[VolumeMixer] Error scanning device: {deviceEx.Message}");
                }
                finally
                {
                    device.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            MacroDeckLogger.Error(_owner, $"[VolumeMixer] Error enumerating devices: {ex.Message}");
        }
        
        MacroDeckLogger.Info(_owner, $"[VolumeMixer] Total apps found: {names.Count} - {string.Join(", ", names)}");
        return names.OrderBy(n => n).ToList();
    }

    public bool SetDefaultDevice(string deviceId, bool allRoles = true)
    {
        if (string.IsNullOrEmpty(deviceId)) return false;
        try
        {
            if (allRoles)
            {
                _policyConfig.SetDefaultEndpoint(deviceId, ERole.Console);
                _policyConfig.SetDefaultEndpoint(deviceId, ERole.Multimedia);
                _policyConfig.SetDefaultEndpoint(deviceId, ERole.Communications);
            }
            else
            {
                _policyConfig.SetDefaultEndpoint(deviceId, ERole.Multimedia);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ───────────────────────────────────────────────────────────────
    // Capture (Microphone) support
    // ───────────────────────────────────────────────────────────────

    public (string Name, string Id)? GetDefaultCaptureDeviceInfo()
    {
        try
        {
            using var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            return (device.FriendlyName, device.ID);
        }
        catch
        {
            return null;
        }
    }

    public (string Name, string Id)? GetDefaultCaptureCommDeviceInfo()
    {
        try
        {
            using var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            return (device.FriendlyName, device.ID);
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyList<(string Name, string Id)> GetActiveCaptureDevices()
    {
        var result = new List<(string, string)>();
        try
        {
            var devices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            foreach (var device in devices)
            {
                try
                {
                    result.Add((device.FriendlyName, device.ID));
                }
                finally
                {
                    device.Dispose();
                }
            }
        }
        catch
        {
        }
        return result;
    }

    public IReadOnlyList<string> GetActiveCaptureDeviceNames()
    {
        var result = new List<string>();
        try
        {
            var devices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            foreach (var device in devices)
            {
                try
                {
                    result.Add(device.FriendlyName);
                }
                finally
                {
                    device.Dispose();
                }
            }
        }
        catch
        {
        }
        return result;
    }

    public bool SetDefaultCaptureDevice(string deviceId, bool allRoles = true)
    {
        if (string.IsNullOrEmpty(deviceId)) return false;
        try
        {
            if (allRoles)
            {
                _policyConfig.SetDefaultEndpoint(deviceId, ERole.Console);
                _policyConfig.SetDefaultEndpoint(deviceId, ERole.Multimedia);
                _policyConfig.SetDefaultEndpoint(deviceId, ERole.Communications);
            }
            else
            {
                _policyConfig.SetDefaultEndpoint(deviceId, ERole.Multimedia);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public IReadOnlyList<(string ProcessName, int Volume, bool Muted)> SnapshotDefaultDeviceSessions()
    {
        var result = new List<(string, int, bool)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var devices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (var device in devices)
            {
                try
                {
                    var sessions = device.AudioSessionManager.Sessions;
                    for (int i = 0; i < sessions.Count; i++)
                    {
                        try
                        {
                            var session = sessions[i];
                            var pid = (int)session.GetProcessID;
                            if (pid == 0) continue;

                            string? name = null;
                            try
                            {
                                using var proc = Process.GetProcessById(pid);
                                name = proc.ProcessName;
                            }
                            catch
                            {
                            }

                            if (string.IsNullOrWhiteSpace(name)) continue;
                            if (seen.Contains(name)) continue;
                            seen.Add(name);

                            var vol = (int)(session.SimpleAudioVolume.Volume * 100);
                            var muted = session.SimpleAudioVolume.Mute;
                            result.Add((name, vol, muted));
                        }
                        catch
                        {
                        }
                    }
                }
                finally
                {
                    device.Dispose();
                }
            }
        }
        catch
        {
        }
        return result;
    }

    public void SetAppVolume(string processName, float volumePercent)
        => MutateSession(processName, s => s.SimpleAudioVolume.Volume = Math.Clamp(volumePercent / 100f, 0f, 1f));

    public void AdjustAppVolume(string processName, float deltaPercent)
        => MutateSession(processName, s =>
        {
            var current = s.SimpleAudioVolume.Volume;
            s.SimpleAudioVolume.Volume = Math.Clamp(current + (deltaPercent / 100f), 0f, 1f);
        });

    public void ToggleAppMute(string processName)
        => MutateSession(processName, s => s.SimpleAudioVolume.Mute = !s.SimpleAudioVolume.Mute);

    private void MutateSession(string processName, Action<AudioSessionControl> mutate)
    {
        try
        {
            using var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var sessions = device.AudioSessionManager.Sessions;

            for (int i = 0; i < sessions.Count; i++)
            {
                try
                {
                    var session = sessions[i];
                    var pid = (int)session.GetProcessID;
                    if (pid == 0) continue;

                    string? name = null;
                    try
                    {
                        using var proc = Process.GetProcessById(pid);
                        name = proc.ProcessName;
                    }
                    catch
                    {
                    }

                    if (!string.Equals(name, processName, StringComparison.OrdinalIgnoreCase)) continue;

                    mutate(session);
                    return;
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            _deviceEnumerator.Dispose();
        }
        catch
        {
        }
    }
}
