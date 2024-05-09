using SuchByte.MacroDeck.GUI.CustomControls;

namespace VolumeMixerPlugin.Views
{
    partial class VolumeAppActionConfigView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            appList = new RoundedComboBox();
            label1 = new Label();
            SuspendLayout();
            // 
            // appList
            // 
            appList.BackColor = Color.FromArgb(65, 65, 65);
            appList.DropDownStyle = ComboBoxStyle.DropDownList;
            appList.Font = new Font("Tahoma", 9F);
            appList.Icon = null;
            appList.Location = new Point(348, 228);
            appList.Name = "appList";
            appList.Padding = new Padding(8, 2, 8, 2);
            appList.SelectedIndex = -1;
            appList.SelectedItem = null;
            appList.Size = new Size(324, 26);
            appList.TabIndex = 0;
            appList.SelectedIndexChanged += appList_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(138, 231);
            label1.Name = "label1";
            label1.Size = new Size(170, 23);
            label1.TabIndex = 2;
            label1.Text = "Mixer Volume Apps";
            // 
            // VolumeAppActionConfigView
            // 
            AutoScaleDimensions = new SizeF(10F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(label1);
            Controls.Add(appList);
            Name = "VolumeAppActionConfigView";
            Size = new Size(800, 450);
            this.Load += new System.EventHandler(this.VolumeAppActionConfigView_Load);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private RoundedComboBox appList;
        private Label label1;
    }
}