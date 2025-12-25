using System.Windows.Forms;
using SuchByte.MacroDeck.GUI.CustomControls;
using SuchByte.MacroDeck.Icons;
using SuchByte.MacroDeck.Language;

namespace VolumeMixerPlugin.GUI;

public partial class IconPackSelector : DialogForm
{
    private RoundedComboBox iconPacks = null!;
    private ButtonPrimary btnOk = null!;

    public string SelectedIconPack => iconPacks.Text;

    public IconPackSelector()
    {
        InitializeComponent();
        LoadIconPacks();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        this.ClientSize = new System.Drawing.Size(350, 150);
        this.Text = "Select Icon Pack";
        this.Name = "IconPackSelector";

        var label = new Label
        {
            Text = "Icon Pack:",
            Location = new System.Drawing.Point(20, 30),
            AutoSize = true
        };

        iconPacks = new RoundedComboBox
        {
            Location = new System.Drawing.Point(120, 25),
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        btnOk = new ButtonPrimary
        {
            Text = LanguageManager.Strings.Ok,
            Location = new System.Drawing.Point(125, 80),
            Width = 100
        };
        btnOk.Click += BtnOk_Click;

        this.Controls.Add(label);
        this.Controls.Add(iconPacks);
        this.Controls.Add(btnOk);

        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private void LoadIconPacks()
    {
        var userPacks = IconManager.IconPacks.FindAll(p => !p.ExtensionStoreManaged);

        if (userPacks.Count == 0)
        {
            IconManager.CreateIconPack("Imported icons", Environment.UserName, "1.0.0");
            userPacks = IconManager.IconPacks.FindAll(p => !p.ExtensionStoreManaged);
        }

        foreach (var pack in userPacks)
        {
            iconPacks.Items.Add(pack.Name);
        }

        if (iconPacks.Items.Count > 0)
            iconPacks.SelectedIndex = 0;
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}
