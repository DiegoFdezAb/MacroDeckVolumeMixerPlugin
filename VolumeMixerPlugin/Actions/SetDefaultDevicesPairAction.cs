using Newtonsoft.Json;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using MacroDeckRoundedComboBox = SuchByte.MacroDeck.GUI.CustomControls.RoundedComboBox;

namespace VolumeMixerPlugin.Actions;

public class SetDefaultDevicesPairAction : PluginAction
{
    public override string Name => "Set Default Output + Microphone";
    public override string Description => "Change both the default output and microphone devices at once";
    public override bool CanConfigure => true;

    public override void Trigger(string clientId, ActionButton actionButton)
    {
        var config = GetConfig();
        if (config == null) return;

        var audioService = VolumeMixerPluginMain.Instance?.AudioService;
        if (audioService == null) return;

        if (!string.IsNullOrEmpty(config.OutputDeviceId))
        {
            audioService.SetDefaultDevice(config.OutputDeviceId, config.AllRoles);
        }

        if (!string.IsNullOrEmpty(config.InputDeviceId))
        {
            audioService.SetDefaultCaptureDevice(config.InputDeviceId, config.AllRoles);
        }

        VolumeMixerPluginMain.Instance?.UpdateVariables();
    }

    public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
    {
        return new SetDefaultDevicesPairConfigControl(this, actionConfigurator);
    }

    private SetDefaultDevicesPairConfig? GetConfig()
    {
        if (string.IsNullOrEmpty(Configuration)) return null;
        try
        {
            return JsonConvert.DeserializeObject<SetDefaultDevicesPairConfig>(Configuration);
        }
        catch
        {
            return null;
        }
    }
}

public class SetDefaultDevicesPairConfig
{
    public string OutputDeviceId { get; set; } = "";
    public string OutputDeviceName { get; set; } = "";
    public string InputDeviceId { get; set; } = "";
    public string InputDeviceName { get; set; } = "";
    public bool AllRoles { get; set; } = true;
}

public class SetDefaultDevicesPairConfigControl : ActionConfigControl
{
    private readonly MacroDeckRoundedComboBox _outputComboBox;
    private readonly MacroDeckRoundedComboBox _inputComboBox;
    private readonly CheckBox _allRolesCheckBox;
    private readonly Button _refreshButton;
    private readonly SetDefaultDevicesPairAction _action;

    public SetDefaultDevicesPairConfigControl(SetDefaultDevicesPairAction action, ActionConfigurator actionConfigurator)
    {
        _action = action;

        var outputLabel = new Label
        {
            Text = "Output:",
            Location = new Point(14, 18),
            AutoSize = true
        };

        _outputComboBox = new MacroDeckRoundedComboBox
        {
            Location = new Point(100, 14),
            Width = 260,
            DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        };

        var inputLabel = new Label
        {
            Text = "Microphone:",
            Location = new Point(14, 52),
            AutoSize = true
        };

        _inputComboBox = new MacroDeckRoundedComboBox
        {
            Location = new Point(100, 48),
            Width = 260,
            DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        };

        _refreshButton = new Button
        {
            Text = "↻",
            Location = new Point(370, 30),
            Width = 30,
            Height = 26
        };
        _refreshButton.Click += (_, _) => PopulateDevices();

        _allRolesCheckBox = new CheckBox
        {
            Text = "Set for all roles (Console, Multimedia, Communications)",
            Location = new Point(100, 82),
            AutoSize = true,
            Checked = true
        };

        Controls.Add(outputLabel);
        Controls.Add(_outputComboBox);
        Controls.Add(inputLabel);
        Controls.Add(_inputComboBox);
        Controls.Add(_refreshButton);
        Controls.Add(_allRolesCheckBox);

        PopulateDevices();
        LoadConfig();
    }

    private void PopulateDevices()
    {
        var audioService = VolumeMixerPluginMain.Instance?.AudioService;

        var selectedOutputId = _outputComboBox.SelectedItem is DeviceItem selOut ? selOut.Id : null;
        var selectedInputId = _inputComboBox.SelectedItem is DeviceItem selIn ? selIn.Id : null;

        _outputComboBox.Items.Clear();
        var outputDevices = audioService?.GetActivePlaybackDevices() ?? [];
        foreach (var (name, id) in outputDevices)
        {
            _outputComboBox.Items.Add(new DeviceItem(name, id));
        }

        _inputComboBox.Items.Clear();
        var inputDevices = audioService?.GetActiveCaptureDevices() ?? [];
        foreach (var (name, id) in inputDevices)
        {
            _inputComboBox.Items.Add(new DeviceItem(name, id));
        }

        RestoreSelection(_outputComboBox, selectedOutputId);
        RestoreSelection(_inputComboBox, selectedInputId);
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
            var config = JsonConvert.DeserializeObject<SetDefaultDevicesPairConfig>(_action.Configuration);
            if (config != null)
            {
                _allRolesCheckBox.Checked = config.AllRoles;
                RestoreSelection(_outputComboBox, config.OutputDeviceId);
                RestoreSelection(_inputComboBox, config.InputDeviceId);
            }
        }
        catch
        {
        }
    }

    public override bool OnActionSave()
    {
        var selectedOutput = _outputComboBox.SelectedItem as DeviceItem;
        var selectedInput = _inputComboBox.SelectedItem as DeviceItem;

        var config = new SetDefaultDevicesPairConfig
        {
            OutputDeviceId = selectedOutput?.Id ?? "",
            OutputDeviceName = selectedOutput?.Name ?? "",
            InputDeviceId = selectedInput?.Id ?? "",
            InputDeviceName = selectedInput?.Name ?? "",
            AllRoles = _allRolesCheckBox.Checked
        };

        _action.Configuration = JsonConvert.SerializeObject(config);

        var summary = new List<string>();
        if (!string.IsNullOrEmpty(config.OutputDeviceName))
            summary.Add($"Out: {config.OutputDeviceName}");
        if (!string.IsNullOrEmpty(config.InputDeviceName))
            summary.Add($"Mic: {config.InputDeviceName}");

        _action.ConfigurationSummary = summary.Count > 0 ? string.Join(" | ", summary) : "Not configured";
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
