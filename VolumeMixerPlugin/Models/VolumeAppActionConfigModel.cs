using SuchByte.MacroDeck.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VolumeMixerPlugin.Models
{
    internal class VolumeAppActionConfigModel : ISerializableConfiguration
    {
        public string AppName { get; set; } = String.Empty;
        public int Volume { get; set; } = 0;

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        public static VolumeAppActionConfigModel Deserialize(string value)
        {
            return ISerializableConfiguration.Deserialize<VolumeAppActionConfigModel>(value);
        }

        /*public void Deserialize(string value)
        {
            string[] values = value.Split(';');
            AppName = values[0];
            Volume = int.Parse(values[1]);
        }*/
    }
}
