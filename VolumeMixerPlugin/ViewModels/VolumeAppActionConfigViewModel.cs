using SuchByte.MacroDeck.Logging;
using SuchByte.MacroDeck.Models;
using SuchByte.MacroDeck.Plugins;
using SuchByte.MacroDeck.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VolumeMixerPlugin.Models;

namespace VolumeMixerPlugin.ViewModels
{
    internal class VolumeAppActionConfigViewModel : ISerializableConfigViewModel
    {
        public readonly PluginAction _action;
        public VolumeAppActionConfigModel Configuration { get; set; }
        ISerializableConfiguration ISerializableConfigViewModel.SerializableConfiguration => Configuration;

        public VolumeAppActionConfigViewModel(PluginAction action)
        {
            this._action = action;
            this.Configuration = VolumeAppActionConfigModel.Deserialize(this._action.Configuration);
        }

        public void SetConfig()
        {
            this._action.ConfigurationSummary = $"Volume App: {this.Configuration.AppName}";
            this._action.Configuration = this.Configuration.Serialize();
        }

        public bool SaveConfig()
        {
            try
            {
                this.SetConfig();
                MacroDeckLogger.Info(PluginInstance.Main, $"{this.GetType().Name}: config saved");
            }
            catch (Exception e)
            {
                MacroDeckLogger.Error(PluginInstance.Main, $"{this.GetType().Name}: Error while saving Config: {e.Message}\n{e.StackTrace}");
            }
            return true;
        }
    }
}
