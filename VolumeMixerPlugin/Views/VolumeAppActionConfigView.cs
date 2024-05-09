using SuchByte.MacroDeck.GUI;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Plugins;
using VolumeMixerPlugin.ViewModels;
using VolumeMixerPlugin.Utils;
using NAudio.CoreAudioApi;

namespace VolumeMixerPlugin.Views
{
    public partial class VolumeAppActionConfigView : ActionConfigControl
    {
        private readonly VolumeAppActionConfigViewModel _viewModel;
        private IEnumerable<string> elements;

        public VolumeAppActionConfigView(PluginAction action)
        {
            this.InitializeComponent();
            this._viewModel = new VolumeAppActionConfigViewModel(action);
        }

        private void VolumeAppActionConfigView_Load(object sender, EventArgs e)
        {
            SessionCollection apps = VolumeMixerUtils.GetMultimediaDeviceDefaultAndApps().apps;

            for (int i = 0; i < apps.Count; i++)
            {
                this.appList.Items.Add(VolumeMixerUtils.GetRealAppName(apps[i]));
            }

            this.appList.SelectedIndexChanged += new System.EventHandler(this.appList_SelectedIndexChanged);
        }

        private void appList_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = this.appList.SelectedIndex;
            string selectedItem = this.appList.SelectedItem.ToString();

        }

        public override bool OnActionSave()
        {
            this._viewModel.Configuration.AppName = this.appList.SelectedItem.ToString();
            return this._viewModel.SaveConfig();
        }



        private void label1_Click(object sender, EventArgs e)
        {
        }
    }
}

