using Newtonsoft.Json;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using MacroDeckRoundedComboBox = SuchByte.MacroDeck.GUI.CustomControls.RoundedComboBox;

namespace VolumeMixerPlugin.Actions;

public class SetAppVolumeAction : PluginAction
{
    public override string Name => "Set App Volume";
    public override string Description => "Set volume for a specific application";
    public override bool CanConfigure => true;

    public override void Trigger(string clientId, ActionButton actionButton)
    {
        var config = GetConfig();
        if (config == null || string.IsNullOrEmpty(config.AppName)) return;

        VolumeMixerPluginMain.Instance?.AudioService?.SetAppVolume(config.AppName, config.Volume);
        VolumeMixerPluginMain.Instance?.UpdateVariables();
    }

    public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
    {
        return new SetAppVolumeConfigControl(this, actionConfigurator);
    }

    private SetAppVolumeConfig? GetConfig()
    {
        if (string.IsNullOrEmpty(Configuration)) return null;
        try
        {
            return JsonConvert.DeserializeObject<SetAppVolumeConfig>(Configuration);
        }
        catch
        {
            return null;
        }
    }
}

public class SetAppVolumeConfig
{
    public string AppName { get; set; } = "";
    public float Volume { get; set; } = 50;
}

public class SetAppVolumeConfigControl : ActionConfigControl
{
    private readonly MacroDeckRoundedComboBox _appComboBox;
    private readonly NumericUpDown _volumeNumeric;
    private readonly SetAppVolumeAction _action;

    public SetAppVolumeConfigControl(SetAppVolumeAction action, ActionConfigurator actionConfigurator)
    {
        _action = action;

        var label1 = new Label { Text = "App Name:", Location = new Point(14, 18), AutoSize = true };
        _appComboBox = new MacroDeckRoundedComboBox { Location = new Point(120, 14), Width = 200, DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList };

        var refreshButton = new Button { Location = new Point(330, 12), Width = 30, Height = 26, Text = "↻" };
        refreshButton.Click += (sender, e) => PopulateApps();

        var label2 = new Label { Text = "Volume (0-100):", Location = new Point(14, 54), AutoSize = true };
        _volumeNumeric = new NumericUpDown
        {
            Location = new Point(120, 50),
            Minimum = 0,
            Maximum = 100,
            Value = 50,
            Width = 80
        };

        Controls.Add(label1);
        Controls.Add(_appComboBox);
        Controls.Add(refreshButton);
        Controls.Add(label2);
        Controls.Add(_volumeNumeric);

        PopulateApps();
        LoadConfig();
    }

    private void PopulateApps()
    {
        var apps = VolumeMixerPluginMain.Instance?.AudioService?.GetActiveAppNames() ?? new List<string>();
        _appComboBox.Items.Clear();
        foreach (var app in apps)
            _appComboBox.Items.Add(app);
    }

    private void LoadConfig()
    {
        if (string.IsNullOrEmpty(_action.Configuration)) return;
        try
        {
            var config = JsonConvert.DeserializeObject<SetAppVolumeConfig>(_action.Configuration);
            if (config != null)
            {
                if (!string.IsNullOrEmpty(config.AppName))
                {
                    if (!_appComboBox.Items.Contains(config.AppName))
                    {
                        _appComboBox.Items.Add(config.AppName);
                    }
                    _appComboBox.SelectedItem = config.AppName;
                }
                _volumeNumeric.Value = (decimal)config.Volume;
            }
        }
        catch
        {
        }
    }

    public override bool OnActionSave()
    {
        var config = new SetAppVolumeConfig
        {
            AppName = _appComboBox.SelectedItem?.ToString() ?? "",
            Volume = (float)_volumeNumeric.Value
        };
        _action.Configuration = JsonConvert.SerializeObject(config);
        _action.ConfigurationSummary = $"{config.AppName} -> {config.Volume}%";
        return true;
    }
}
