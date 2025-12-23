using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.Plugins;

namespace VolumeMixerPlugin.Actions;

public class RefreshDevicesAction : PluginAction
{
    public override string Name => "Refresh Devices";
    public override string Description => "Force refresh of audio device and session list";
    public override bool CanConfigure => false;

    public override void Trigger(string clientId, ActionButton actionButton)
    {
        VolumeMixerPluginMain.Instance?.UpdateVariables();
    }
}
