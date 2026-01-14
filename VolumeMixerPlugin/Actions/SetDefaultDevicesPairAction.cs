using Newtonsoft.Json;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Logging;
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

        var outputDevices = audioService.GetActivePlaybackDevices().ToDictionary(d => d.Id);
        var inputDevices = audioService.GetActiveCaptureDevices().ToDictionary(d => d.Id);

        if (!config.OutputToggleMode)
        {
            if (!string.IsNullOrEmpty(config.OutputDeviceId))
            {
                if (outputDevices.ContainsKey(config.OutputDeviceId))
                {
                    audioService.SetDefaultDevice(config.OutputDeviceId, config.AllRoles);
                }
                else
                {
                    MacroDeckLogger.Warning(VolumeMixerPluginMain.Instance!, $"Output '{config.OutputDeviceName}' is not available");
                }
            }
        }
        else
        {
            var currentOutput = audioService.GetDefaultMultimediaDeviceInfo();
            string targetOutputId;
            if (currentOutput?.Id == config.OutputDeviceId || currentOutput?.Id != config.OutputDeviceId2)
            {
                targetOutputId = config.OutputDeviceId2;
            }
            else
            {
                targetOutputId = config.OutputDeviceId;
            }

            if (!string.IsNullOrEmpty(targetOutputId) && outputDevices.ContainsKey(targetOutputId))
            {
                audioService.SetDefaultDevice(targetOutputId, config.AllRoles);
            }
            else
            {
                var name = currentOutput?.Id == config.OutputDeviceId ? config.OutputDeviceName2 : config.OutputDeviceName;
                MacroDeckLogger.Warning(VolumeMixerPluginMain.Instance!, $"Output '{name}' is not available for toggle");
            }
        }

        if (!config.InputToggleMode)
        {
            if (!string.IsNullOrEmpty(config.InputDeviceId))
            {
                if (inputDevices.ContainsKey(config.InputDeviceId))
                {
                    audioService.SetDefaultCaptureDevice(config.InputDeviceId, config.AllRoles);
                }
                else
                {
                    MacroDeckLogger.Warning(VolumeMixerPluginMain.Instance!, $"Microphone '{config.InputDeviceName}' is not available");
                }
            }
        }
        else
        {
            var currentInput = audioService.GetDefaultCaptureDeviceInfo();
            string targetInputId;
            if (currentInput?.Id == config.InputDeviceId || currentInput?.Id != config.InputDeviceId2)
            {
                targetInputId = config.InputDeviceId2;
            }
            else
            {
                targetInputId = config.InputDeviceId;
            }

            if (!string.IsNullOrEmpty(targetInputId) && inputDevices.ContainsKey(targetInputId))
            {
                audioService.SetDefaultCaptureDevice(targetInputId, config.AllRoles);
            }
            else
            {
                var name = currentInput?.Id == config.InputDeviceId ? config.InputDeviceName2 : config.InputDeviceName;
                MacroDeckLogger.Warning(VolumeMixerPluginMain.Instance!, $"Microphone '{name}' is not available for toggle");
            }
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
    public bool OutputToggleMode { get; set; } = false;
    public string OutputDeviceId2 { get; set; } = "";
    public string OutputDeviceName2 { get; set; } = "";

    public string InputDeviceId { get; set; } = "";
    public string InputDeviceName { get; set; } = "";
    public bool InputToggleMode { get; set; } = false;
    public string InputDeviceId2 { get; set; } = "";
    public string InputDeviceName2 { get; set; } = "";

    public bool AllRoles { get; set; } = true;
}

public class SetDefaultDevicesPairConfigControl : ActionConfigControl
{
    private readonly MacroDeckRoundedComboBox _outputComboBox;
    private readonly MacroDeckRoundedComboBox _output2ComboBox;
    private readonly Label _output2Label;
    private readonly CheckBox _outputToggleCheckBox;
    private readonly MacroDeckRoundedComboBox _inputComboBox;
    private readonly MacroDeckRoundedComboBox _input2ComboBox;
    private readonly Label _input2Label;
    private readonly CheckBox _inputToggleCheckBox;
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
            Location = new Point(150, 14),
            Width = 200,
            DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        };

        _outputToggleCheckBox = new CheckBox
        {
            Text = "Toggle",
            Location = new Point(360, 14),
            AutoSize = true,
            Checked = false
        };
        _outputToggleCheckBox.CheckedChanged += (_, _) => UpdateOutputUI();

        _output2Label = new Label
        {
            Text = "Output 2:",
            Location = new Point(14, 52),
            AutoSize = true,
            Visible = false
        };

        _output2ComboBox = new MacroDeckRoundedComboBox
        {
            Location = new Point(150, 48),
            Width = 200,
            DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList,
            Visible = false
        };
        _output2ComboBox.SelectedIndexChanged += (_, _) => ValidateOutputSelection();

        var inputLabel = new Label
        {
            Text = "Microphone:",
            Location = new Point(14, 90),
            AutoSize = true
        };

        _inputComboBox = new MacroDeckRoundedComboBox
        {
            Location = new Point(150, 86),
            Width = 200,
            DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        };

        _inputToggleCheckBox = new CheckBox
        {
            Text = "Toggle",
            Location = new Point(360, 86),
            AutoSize = true,
            Checked = false
        };
        _inputToggleCheckBox.CheckedChanged += (_, _) => UpdateInputUI();

        _input2Label = new Label
        {
            Text = "Microphone 2:",
            Location = new Point(14, 128),
            AutoSize = true,
            Visible = false
        };

        _input2ComboBox = new MacroDeckRoundedComboBox
        {
            Location = new Point(150, 124),
            Width = 200,
            DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList,
            Visible = false
        };
        _input2ComboBox.SelectedIndexChanged += (_, _) => ValidateInputSelection();

        _refreshButton = new Button
        {
            Text = "↻",
            Location = new Point(390, 112),
            Width = 30,
            Height = 26
        };
        _refreshButton.Click += (_, _) => PopulateDevices();

        _allRolesCheckBox = new CheckBox
        {
            Text = "Set for all roles (Console, Multimedia, Communications)",
            Location = new Point(14, 166),
            AutoSize = true,
            Checked = true
        };

        Controls.Add(outputLabel);
        Controls.Add(_outputComboBox);
        Controls.Add(_outputToggleCheckBox);
        Controls.Add(_output2Label);
        Controls.Add(_output2ComboBox);
        Controls.Add(inputLabel);
        Controls.Add(_inputComboBox);
        Controls.Add(_inputToggleCheckBox);
        Controls.Add(_input2Label);
        Controls.Add(_input2ComboBox);
        Controls.Add(_refreshButton);
        Controls.Add(_allRolesCheckBox);

        PopulateDevices();
        LoadConfig();
    }

    private void UpdateOutputUI()
    {
        var isToggleMode = _outputToggleCheckBox.Checked;
        _output2Label.Visible = isToggleMode;
        _output2ComboBox.Visible = isToggleMode;
    }

    private void UpdateInputUI()
    {
        var isToggleMode = _inputToggleCheckBox.Checked;
        _input2Label.Visible = isToggleMode;
        _input2ComboBox.Visible = isToggleMode;
    }

    private void ValidateOutputSelection()
    {
        if (!_outputToggleCheckBox.Checked) return;

        var device1 = _outputComboBox.SelectedItem as DeviceItem;
        var device2 = _output2ComboBox.SelectedItem as DeviceItem;

        if (device1 != null && device2 != null && device1.Id == device2.Id)
        {
            _output2ComboBox.SelectedIndex = -1;
        }
    }

    private void ValidateInputSelection()
    {
        if (!_inputToggleCheckBox.Checked) return;

        var device1 = _inputComboBox.SelectedItem as DeviceItem;
        var device2 = _input2ComboBox.SelectedItem as DeviceItem;

        if (device1 != null && device2 != null && device1.Id == device2.Id)
        {
            _input2ComboBox.SelectedIndex = -1;
        }
    }

    private void PopulateDevices()
    {
        var audioService = VolumeMixerPluginMain.Instance?.AudioService;

        var selectedOutputId1 = _outputComboBox.SelectedItem is DeviceItem selOut1 ? selOut1.Id : null;
        var selectedOutputId2 = _output2ComboBox.SelectedItem is DeviceItem selOut2 ? selOut2.Id : null;
        var selectedInputId1 = _inputComboBox.SelectedItem is DeviceItem selIn1 ? selIn1.Id : null;
        var selectedInputId2 = _input2ComboBox.SelectedItem is DeviceItem selIn2 ? selIn2.Id : null;

        _outputComboBox.Items.Clear();
        _output2ComboBox.Items.Clear();
        var outputDevices = audioService?.GetActivePlaybackDevices() ?? [];
        foreach (var (name, id) in outputDevices)
        {
            _outputComboBox.Items.Add(new DeviceItem(name, id));
            _output2ComboBox.Items.Add(new DeviceItem(name, id));
        }

        _inputComboBox.Items.Clear();
        _input2ComboBox.Items.Clear();
        var inputDevices = audioService?.GetActiveCaptureDevices() ?? [];
        foreach (var (name, id) in inputDevices)
        {
            _inputComboBox.Items.Add(new DeviceItem(name, id));
            _input2ComboBox.Items.Add(new DeviceItem(name, id));
        }

        RestoreSelection(_outputComboBox, selectedOutputId1);
        RestoreSelection(_output2ComboBox, selectedOutputId2);
        RestoreSelection(_inputComboBox, selectedInputId1);
        RestoreSelection(_input2ComboBox, selectedInputId2);
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
                _outputToggleCheckBox.Checked = config.OutputToggleMode;
                _inputToggleCheckBox.Checked = config.InputToggleMode;
                RestoreSelection(_outputComboBox, config.OutputDeviceId);
                RestoreSelection(_output2ComboBox, config.OutputDeviceId2);
                RestoreSelection(_inputComboBox, config.InputDeviceId);
                RestoreSelection(_input2ComboBox, config.InputDeviceId2);
                UpdateOutputUI();
                UpdateInputUI();
            }
        }
        catch
        {
        }
    }

    public override bool OnActionSave()
    {
        var selectedOutput = _outputComboBox.SelectedItem as DeviceItem;
        var selectedOutput2 = _output2ComboBox.SelectedItem as DeviceItem;
        var selectedInput = _inputComboBox.SelectedItem as DeviceItem;
        var selectedInput2 = _input2ComboBox.SelectedItem as DeviceItem;

        if (_outputToggleCheckBox.Checked && (selectedOutput == null || selectedOutput2 == null || selectedOutput.Id == selectedOutput2.Id))
        {
            return false;
        }

        if (_inputToggleCheckBox.Checked && (selectedInput == null || selectedInput2 == null || selectedInput.Id == selectedInput2.Id))
        {
            return false;
        }

        if (!_outputToggleCheckBox.Checked && selectedOutput == null)
        {
            return false;
        }

        if (!_inputToggleCheckBox.Checked && selectedInput == null)
        {
            return false;
        }

        var config = new SetDefaultDevicesPairConfig
        {
            OutputDeviceId = selectedOutput?.Id ?? "",
            OutputDeviceName = selectedOutput?.Name ?? "",
            OutputToggleMode = _outputToggleCheckBox.Checked,
            OutputDeviceId2 = selectedOutput2?.Id ?? "",
            OutputDeviceName2 = selectedOutput2?.Name ?? "",
            InputDeviceId = selectedInput?.Id ?? "",
            InputDeviceName = selectedInput?.Name ?? "",
            InputToggleMode = _inputToggleCheckBox.Checked,
            InputDeviceId2 = selectedInput2?.Id ?? "",
            InputDeviceName2 = selectedInput2?.Name ?? "",
            AllRoles = _allRolesCheckBox.Checked
        };

        _action.Configuration = JsonConvert.SerializeObject(config);

        var summary = new List<string>();
        if (config.OutputToggleMode)
        {
            summary.Add($"Out: {config.OutputDeviceName} ↔ {config.OutputDeviceName2}");
        }
        else
        {
            summary.Add($"Out: {config.OutputDeviceName}");
        }

        if (config.InputToggleMode)
        {
            summary.Add($"Mic: {config.InputDeviceName} ↔ {config.InputDeviceName2}");
        }
        else
        {
            summary.Add($"Mic: {config.InputDeviceName}");
        }

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
