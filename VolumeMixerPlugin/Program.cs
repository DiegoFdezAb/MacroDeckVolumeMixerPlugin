using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using SuchByte.MacroDeck;
using SuchByte.MacroDeck.Logging;
using SuchByte.MacroDeck.Plugins;
using SuchByte.MacroDeck.Variables;
using System.Runtime.InteropServices;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using System;
using VolumeMixerPlugin.Views;
using VolumeMixerPlugin.Utils;


namespace VolumeMixerPlugin
{
    public static class PluginInstance
    {
        public static Main Main { get; set; }
    }

    public class Main : MacroDeckPlugin
    {
        public static Main Instance;

        public Main()
        {
            Instance = this;
            PluginInstance.Main = this;
        }

        public override bool CanConfigure => false;

        public override void Enable()
        {
            try
            {
                Instance ??= this;
                Actions = new List<PluginAction>
                {
                    new VolumeApp()
                };
                Init();
            }
            catch (Exception e)
            {
                MacroDeckLogger.Error(this, $"There is a error.\r\n{e}");
            }
        }

        public class VolumeApp : PluginAction
        {
            public override string Name => "Volume Mixer App";

            public override string Description => "Choose the app";

            public override bool CanConfigure => true;

            //public override string BindableVariable => "wnp_is_playing";

            public override void Trigger(string clientId, ActionButton actionButton)
            {
                VariableManager.SetValue("Play", "isStarted", VariableType.String, PluginInstance.Main,
                        true);
            }

            public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
            {
                return new VolumeAppActionConfigView(this);
            }
        }

        public void Init()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName().Version;
            string version = $"{assembly.Major}.{assembly.Minor}.{assembly.Build}";

            var worker = new BackgroundWorker();
            worker.DoWork += worker_DoWork;
            worker.RunWorkerAsync();
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                try
                {
                    VariableManager.SetValue("Play", "isPause", VariableType.String, PluginInstance.Main,
                        null);

                    Thread.Sleep(300);
                    VariableManager.SetValue("volumeAppChoosed", VolumeMixerUtils.GetVolumeFromApp(VolumeMixerUtils.GetAppFromNameNoDevice("Spotify")), VariableType.Float, PluginInstance.Main,null);
                }
                catch (Exception ex)
                {
                    MacroDeckLogger.Error(this, $"There is a error.\r\n{ex}");
                }
            }
        }

        public void Logger(int type, string message)
        {
            if (type == 0)
                MacroDeckLogger.Info(PluginInstance.Main, message);
            else if (type == 1)
                MacroDeckLogger.Warning(PluginInstance.Main, message);
            else
                MacroDeckLogger.Error(PluginInstance.Main, message);
        }
    }
}