using Newtonsoft.Json;
using SuchByte.MacroDeck.ActionButton;
using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using VolumeMixerPlugin.Services;
using MacroDeckRoundedComboBox = SuchByte.MacroDeck.GUI.CustomControls.RoundedComboBox;

namespace VolumeMixerPlugin.Actions;

public class MuteAppAction : PluginAction
{
    public override string Name => "Mute/Unmute App";
    public override string Description => "Toggle mute state for an application";
    public override bool CanConfigure => true;

    public override void Trigger(string clientId, ActionButton actionButton)
    {
        var config = GetConfig();
        if (config == null || string.IsNullOrEmpty(config.AppName)) return;

        VolumeMixerPluginMain.Instance?.AudioService?.ToggleAppMute(config.AppName);
        VolumeMixerPluginMain.Instance?.UpdateVariables();
    }

    public override ActionConfigControl GetActionConfigControl(ActionConfigurator actionConfigurator)
    {
        return new MuteAppConfigControl(this, actionConfigurator);
    }

    private MuteAppConfig? GetConfig()
    {
        if (string.IsNullOrEmpty(Configuration)) return null;
        try
        {
            return JsonConvert.DeserializeObject<MuteAppConfig>(Configuration);
        }
        catch
        {
            return null;
        }
    }
}

public class MuteAppConfig
{
    public string AppName { get; set; } = "";
}

public class MuteAppConfigControl : ActionConfigControl
{
    private readonly MacroDeckRoundedComboBox _appComboBox;
    private readonly MuteAppAction _action;

    public MuteAppConfigControl(MuteAppAction action, ActionConfigurator actionConfigurator)
    {
        _action = action;

        var label = new Label { Text = "App Name:", Location = new Point(14, 18), AutoSize = true };
        _appComboBox = new MacroDeckRoundedComboBox { Location = new Point(120, 14), Width = 200, DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList };

        var refreshButton = new Button { Location = new Point(330, 12), Width = 30, Height = 26, Text = "↻" };
        refreshButton.Click += (sender, e) => PopulateApps();

        var importIconButton = new ButtonPrimary { Text = "Import Icon", Location = new Point(370, 12), Width = 100, Height = 26 };
        importIconButton.Click += (sender, e) => OnImportIconClick();

        Controls.Add(label);
        Controls.Add(_appComboBox);
        Controls.Add(refreshButton);
        Controls.Add(importIconButton);

        PopulateApps();
        LoadConfig();
    }

    private void OnImportIconClick()
    {
        var selectedApp = _appComboBox.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(selectedApp))
        {
            using var msgBox = new SuchByte.MacroDeck.GUI.CustomControls.MessageBox();
            msgBox.ShowDialog("Import Icon", "Please select an application first.", System.Windows.Forms.MessageBoxButtons.OK);
            return;
        }
        IconImportService.PromptAndImportIcon(selectedApp);
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
            var config = JsonConvert.DeserializeObject<MuteAppConfig>(_action.Configuration);
            if (config != null && !string.IsNullOrEmpty(config.AppName))
            {
                if (!_appComboBox.Items.Contains(config.AppName))
                {
                    _appComboBox.Items.Add(config.AppName);
                }
                _appComboBox.SelectedItem = config.AppName;
            }
        }
        catch
        {
        }
    }

    public override bool OnActionSave()
    {
        var config = new MuteAppConfig { AppName = _appComboBox.SelectedItem?.ToString() ?? "" };
        _action.Configuration = JsonConvert.SerializeObject(config);
        _action.ConfigurationSummary = $"Toggle {config.AppName}";
        return true;
    }
}
