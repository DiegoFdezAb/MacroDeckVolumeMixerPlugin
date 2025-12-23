using Newtonsoft.Json;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using MacroDeckRoundedComboBox = SuchByte.MacroDeck.GUI.CustomControls.RoundedComboBox;

namespace VolumeMixerPlugin.Actions;

public class SetDefaultDeviceAction : PluginAction
{
    public override string Name => "Set Default Device";
    public override string Description => "Change the default audio playback device";
    public override bool CanConfigure => true;

    public override void Trigger(string clientId, ActionButton actionButton)
    {
        var config = GetConfig();
        if (config == null || string.IsNullOrEmpty(config.DeviceId)) return;

        VolumeMixerPluginMain.Instance?.AudioService?.SetDefaultDevice(config.DeviceId, config.AllRoles);
        VolumeMixerPluginMain.Instance?.UpdateVariables();
    }

    public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
    {
        return new SetDefaultDeviceConfigControl(this, actionConfigurator);
    }

    private SetDefaultDeviceConfig? GetConfig()
    {
        if (string.IsNullOrEmpty(Configuration)) return null;
        try
        {
            return JsonConvert.DeserializeObject<SetDefaultDeviceConfig>(Configuration);
        }
        catch
        {
            return null;
        }
    }
}

public class SetDefaultDeviceConfig
{
    public string DeviceId { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public bool AllRoles { get; set; } = true;
}

public class SetDefaultDeviceConfigControl : ActionConfigControl
{
    private readonly MacroDeckRoundedComboBox _deviceComboBox;
    private readonly CheckBox _allRolesCheckBox;
    private readonly Button _refreshButton;
    private readonly SetDefaultDeviceAction _action;
    private List<(string Name, string Id)> _devices = new();

    public SetDefaultDeviceConfigControl(SetDefaultDeviceAction action, ActionConfigurator actionConfigurator)
    {
        _action = action;

        var label = new Label
        {
            Text = "Device:",
            Location = new Point(14, 18),
            AutoSize = true
        };

        _deviceComboBox = new MacroDeckRoundedComboBox
        {
            Location = new Point(100, 14),
            Width = 260,
            DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        };

        _refreshButton = new Button
        {
            Text = "↻",
            Location = new Point(370, 12),
            Width = 30,
            Height = 26
        };
        _refreshButton.Click += (_, _) => PopulateDevices();

        _allRolesCheckBox = new CheckBox
        {
            Text = "Set for all roles (Console, Multimedia, Communications)",
            Location = new Point(100, 48),
            AutoSize = true,
            Checked = true
        };

        Controls.Add(label);
        Controls.Add(_deviceComboBox);
        Controls.Add(_refreshButton);
        Controls.Add(_allRolesCheckBox);

        PopulateDevices();
        LoadConfig();
    }

    private void PopulateDevices()
    {
        _devices = VolumeMixerPluginMain.Instance?.AudioService?.GetActivePlaybackDevices().ToList()
                   ?? new List<(string, string)>();

        var selectedId = _deviceComboBox.SelectedItem is DeviceItem sel ? sel.Id : null;

        _deviceComboBox.Items.Clear();
        foreach (var (name, id) in _devices)
        {
            _deviceComboBox.Items.Add(new DeviceItem(name, id));
        }

        if (!string.IsNullOrEmpty(selectedId))
        {
            for (int i = 0; i < _deviceComboBox.Items.Count; i++)
            {
                if (_deviceComboBox.Items[i] is DeviceItem item && item.Id == selectedId)
                {
                    _deviceComboBox.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private void LoadConfig()
    {
        if (string.IsNullOrEmpty(_action.Configuration)) return;
        try
        {
            var config = JsonConvert.DeserializeObject<SetDefaultDeviceConfig>(_action.Configuration);
            if (config != null)
            {
                _allRolesCheckBox.Checked = config.AllRoles;
                for (int i = 0; i < _deviceComboBox.Items.Count; i++)
                {
                    if (_deviceComboBox.Items[i] is DeviceItem item && item.Id == config.DeviceId)
                    {
                        _deviceComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
        catch
        {
        }
    }

    public override bool OnActionSave()
    {
        var selectedDevice = _deviceComboBox.SelectedItem as DeviceItem;
        var config = new SetDefaultDeviceConfig
        {
            DeviceId = selectedDevice?.Id ?? "",
            DeviceName = selectedDevice?.Name ?? "",
            AllRoles = _allRolesCheckBox.Checked
        };
        _action.Configuration = JsonConvert.SerializeObject(config);
        _action.ConfigurationSummary = config.DeviceName;
        return true;
    }

    private class DeviceItem
    {
        public string Name { get; }
        public string Id { get; }

        public DeviceItem(string name, string id)
        {
            Name = name;
            Id = id;
        }

        public override string ToString() => Name;
    }
}
