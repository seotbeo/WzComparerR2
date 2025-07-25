using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WzComparerR2
{
    public partial class FrmOverlaySpineOptions : DevComponents.DotNetBar.Office2007Form
    {
        public FrmOverlaySpineOptions(string[] names, string[] skins)
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
#endif
            this.comboBoxEx1.Items.AddRange(names);
            this.comboBoxEx2.Items.AddRange(skins);
            this.comboBoxEx1.SelectedIndex = 0;
            this.comboBoxEx2.SelectedIndex = 0;
        }

        public void GetValues(out string name, out string skin, out int delay)
        {
            name = this.comboBoxEx1.SelectedItem as string;
            skin = this.comboBoxEx2.SelectedItem as string;
            delay = this.txtDelay.ValueObject as int? ?? 60;
            delay = delay / 30 * 30;

            return;
        }
    }
}