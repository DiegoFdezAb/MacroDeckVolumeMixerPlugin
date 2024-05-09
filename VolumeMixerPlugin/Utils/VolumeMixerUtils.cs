using NAudio.CoreAudioApi;
using NAudio.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VolumeMixerPlugin.Utils
{
    internal class VolumeMixerUtils
    {
        private const float VolumeIncrement = 5 / 100f;
        public static MMDeviceEnumerator deviceEnumerator = new();

        private static (MMDevice device, SessionCollection apps) GetDeviceDefaultAndApps(Role role)
        {
            MMDevice defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, role);
            AudioSessionManager audioSessionManager = defaultDevice.AudioSessionManager;
            SessionCollection sessionEnumerator = audioSessionManager.Sessions;

            return (defaultDevice, sessionEnumerator);
        }

        public static (MMDevice device, SessionCollection apps) GetMultimediaDeviceDefaultAndApps()
        {
            return GetDeviceDefaultAndApps(Role.Multimedia);
        }

        public static (MMDevice device, SessionCollection apps) GetCommunicationsDeviceDefaultAndApps()
        {
            return GetDeviceDefaultAndApps(Role.Communications);
        }

        public static AudioSessionControl GetAppFromName(SessionCollection apps, string name)
        {
            for (int i = 0; i < apps.Count; i++)
            {
                if (GetRealAppName(apps[i]) == name)
                {
                    return apps[i];
                }
            }
            return null;
        }

        public static AudioSessionControl GetAppFromNameNoDevice(string name)
        {
            MMDeviceCollection devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            for (int i = 0; i < devices.Count; i++)
            {
                AudioSessionManager device = devices[i].AudioSessionManager;
                SessionCollection apps = device.Sessions;
                for (int j = 0; j < apps.Count; j++)
                {
                    if (GetRealAppName(apps[j]) == name)
                    {
                        return apps[j];
                    }
                }
            }
            return null;
        }

        public static string GetRealAppName(AudioSessionControl app)
        {
            return System.Diagnostics.Process.GetProcessById((int)app.GetProcessID).ProcessName;
        }

        public static SessionCollection GetAppsFromDevice(MMDevice device)
        {
            AudioSessionManager audioSessionManager = device.AudioSessionManager;

            return audioSessionManager.Sessions;
        }

        public static MMDevice GetDeviceFromApp(AudioSessionControl app)
        {
            MMDeviceEnumerator deviceEnumerator = (new MMDeviceEnumerator());

            return deviceEnumerator.GetDevice(app.GetSessionIdentifier.Split("|")[0]);
        }

        public static int GetVolumeFromApp(AudioSessionControl app)
        {
            float volume = app.SimpleAudioVolume.Volume;
            return (int)(volume * 100);
        }

        public static void SetVolumeUpFromApp(AudioSessionControl app)
        {
            app.SimpleAudioVolume.Volume += VolumeIncrement;
        }
        public static void SetVolumeDownFromApp(AudioSessionControl app)
        {
            app.SimpleAudioVolume.Volume -= VolumeIncrement;
        }

        public static void SetVolumeFromApp(AudioSessionControl app, float volume)
        {
            app.SimpleAudioVolume.Volume = volume / 100;
        }
    }
}