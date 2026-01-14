using Newtonsoft.Json;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Logging;
using SuchByte.MacroDeck.Plugins;
using MacroDeckRoundedComboBox = SuchByte.MacroDeck.GUI.CustomControls.RoundedComboBox;

namespace VolumeMixerPlugin.Actions;

public class SetDefaultMicrophoneAction : PluginAction
{
    public override string Name => "Set Default Microphone";
    public override string Description => "Change the default audio input (microphone) device";
    public override bool CanConfigure => true;

    public override void Trigger(string clientId, ActionButton actionButton)
    {
        var config = GetConfig();
        if (config == null) return;

        var audioService = VolumeMixerPluginMain.Instance?.AudioService;
        if (audioService == null) return;

        if (!config.ToggleMode)
        {
            if (string.IsNullOrEmpty(config.DeviceId)) return;

            var availableDevices = audioService.GetActiveCaptureDevices();
            if (!availableDevices.Any(d => d.Id == config.DeviceId))
            {
                MacroDeckLogger.Warning(VolumeMixerPluginMain.Instance!, $"Microphone '{config.DeviceName}' is not available");
                return;
            }

            audioService.SetDefaultCaptureDevice(config.DeviceId, config.AllRoles);
        }
        else
        {
            var current = audioService.GetDefaultCaptureDeviceInfo();
            var availableDevices = audioService.GetActiveCaptureDevices().ToDictionary(d => d.Id);

            string targetId;
            if (current?.Id == config.DeviceId || current?.Id != config.DeviceId2)
            {
                targetId = config.DeviceId2;
            }
            else
            {
                targetId = config.DeviceId;
            }

            if (!string.IsNullOrEmpty(targetId) && availableDevices.ContainsKey(targetId))
            {
                audioService.SetDefaultCaptureDevice(targetId, config.AllRoles);
            }
            else
            {
                var name = current?.Id == config.DeviceId ? config.DeviceName2 : config.DeviceName;
                MacroDeckLogger.Warning(VolumeMixerPluginMain.Instance!, $"Microphone '{name}' is not available for toggle");
            }
        }

        VolumeMixerPluginMain.Instance?.UpdateVariables();
    }

    public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
    {
        return new SetDefaultMicrophoneConfigControl(this, actionConfigurator);
    }

    private SetDefaultMicrophoneConfig? GetConfig()
    {
        if (string.IsNullOrEmpty(Configuration)) return null;
        try
        {
            return JsonConvert.DeserializeObject<SetDefaultMicrophoneConfig>(Configuration);
        }
        catch
        {
            return null;
        }
    }
}

public class SetDefaultMicrophoneConfig
{
    public string DeviceId { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public bool ToggleMode { get; set; } = false;
    public string DeviceId2 { get; set; } = "";
    public string DeviceName2 { get; set; } = "";
    public bool AllRoles { get; set; } = true;
}

public class SetDefaultMicrophoneConfigControl : ActionConfigControl
{
    private readonly MacroDeckRoundedComboBox _deviceComboBox;
    private readonly MacroDeckRoundedComboBox _device2ComboBox;
    private readonly Label _device2Label;
    private readonly CheckBox _toggleModeCheckBox;
    private readonly CheckBox _allRolesCheckBox;
    private readonly Button _refreshButton;
    private readonly SetDefaultMicrophoneAction _action;
    private List<(string Name, string Id)> _devices = new();

    public SetDefaultMicrophoneConfigControl(SetDefaultMicrophoneAction action, ActionConfigurator actionConfigurator)
    {
        _action = action;

        var label = new Label
        {
            Text = "Microphone:",
            Location = new Point(14, 18),
            AutoSize = true
        };

        _deviceComboBox = new MacroDeckRoundedComboBox
        {
            Location = new Point(150, 14),
            Width = 230,
            DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        };

        _refreshButton = new Button
        {
            Text = "↻",
            Location = new Point(390, 12),
            Width = 30,
            Height = 26
        };
        _refreshButton.Click += (_, _) => PopulateDevices();

        _toggleModeCheckBox = new CheckBox
        {
            Text = "Toggle between 2 microphones",
            Location = new Point(14, 50),
            AutoSize = true,
            Checked = false
        };
        _toggleModeCheckBox.CheckedChanged += (_, _) => UpdateUI();

        _device2Label = new Label
        {
            Text = "Microphone 2:",
            Location = new Point(14, 82),
            AutoSize = true,
            Visible = false
        };

        _device2ComboBox = new MacroDeckRoundedComboBox
        {
            Location = new Point(150, 78),
            Width = 230,
            DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList,
            Visible = false
        };
        _device2ComboBox.SelectedIndexChanged += (_, _) => ValidateDeviceSelection();

        _allRolesCheckBox = new CheckBox
        {
            Text = "Set for all roles (Console, Multimedia, Communications)",
            Location = new Point(14, 116),
            AutoSize = true,
            Checked = true
        };

        Controls.Add(label);
        Controls.Add(_deviceComboBox);
        Controls.Add(_refreshButton);
        Controls.Add(_toggleModeCheckBox);
        Controls.Add(_device2Label);
        Controls.Add(_device2ComboBox);
        Controls.Add(_allRolesCheckBox);

        PopulateDevices();
        LoadConfig();
    }

    private void UpdateUI()
    {
        var isToggleMode = _toggleModeCheckBox.Checked;
        _device2Label.Visible = isToggleMode;
        _device2ComboBox.Visible = isToggleMode;

        if (!isToggleMode)
        {
            _allRolesCheckBox.Location = new Point(14, 86);
        }
        else
        {
            _allRolesCheckBox.Location = new Point(14, 116);
        }
    }

    private void ValidateDeviceSelection()
    {
        if (!_toggleModeCheckBox.Checked) return;

        var device1 = _deviceComboBox.SelectedItem as DeviceItem;
        var device2 = _device2ComboBox.SelectedItem as DeviceItem;

        if (device1 != null && device2 != null && device1.Id == device2.Id)
        {
            _device2ComboBox.SelectedIndex = -1;
        }
    }

    private void PopulateDevices()
    {
        _devices = VolumeMixerPluginMain.Instance?.AudioService?.GetActiveCaptureDevices().ToList()
                   ?? new List<(string, string)>();

        var selectedId1 = _deviceComboBox.SelectedItem is DeviceItem sel1 ? sel1.Id : null;
        var selectedId2 = _device2ComboBox.SelectedItem is DeviceItem sel2 ? sel2.Id : null;

        _deviceComboBox.Items.Clear();
        _device2ComboBox.Items.Clear();
        foreach (var (name, id) in _devices)
        {
            _deviceComboBox.Items.Add(new DeviceItem(name, id));
            _device2ComboBox.Items.Add(new DeviceItem(name, id));
        }

        RestoreSelection(_deviceComboBox, selectedId1);
        RestoreSelection(_device2ComboBox, selectedId2);
    }

    private static void RestoreSelection(MacroDeckRoundedComboBox comboBox, string? selectedId)
    {
        if (string.IsNullOrEmpty(selectedId)) return;
        for (int i = 0; i < comboBox.Items.Count; i++)
        {
            if (comboBox.Items[i] is DeviceItem item && item.Id == selectedId)
            {
                comboBox.SelectedIndex = i;
                return;
            }
        }
    }

    private void LoadConfig()
    {
        if (string.IsNullOrEmpty(_action.Configuration)) return;
        try
        {
            var config = JsonConvert.DeserializeObject<SetDefaultMicrophoneConfig>(_action.Configuration);
            if (config != null)
            {
                _toggleModeCheckBox.Checked = config.ToggleMode;
                _allRolesCheckBox.Checked = config.AllRoles;
                RestoreSelection(_deviceComboBox, config.DeviceId);
                RestoreSelection(_device2ComboBox, config.DeviceId2);
                UpdateUI();
            }
        }
        catch
        {
        }
    }

    public override bool OnActionSave()
    {
        var selectedDevice = _deviceComboBox.SelectedItem as DeviceItem;
        var selectedDevice2 = _device2ComboBox.SelectedItem as DeviceItem;

        if (_toggleModeCheckBox.Checked && (selectedDevice == null || selectedDevice2 == null || selectedDevice.Id == selectedDevice2.Id))
        {
            return false;
        }

        if (!_toggleModeCheckBox.Checked && selectedDevice == null)
        {
            return false;
        }

        var config = new SetDefaultMicrophoneConfig
        {
            DeviceId = selectedDevice?.Id ?? "",
            DeviceName = selectedDevice?.Name ?? "",
            ToggleMode = _toggleModeCheckBox.Checked,
            DeviceId2 = selectedDevice2?.Id ?? "",
            DeviceName2 = selectedDevice2?.Name ?? "",
            AllRoles = _allRolesCheckBox.Checked
        };
        _action.Configuration = JsonConvert.SerializeObject(config);

        if (config.ToggleMode)
        {
            _action.ConfigurationSummary = $"Toggle: {config.DeviceName} ↔ {config.DeviceName2}";
        }
        else
        {
            _action.ConfigurationSummary = config.DeviceName;
        }

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
