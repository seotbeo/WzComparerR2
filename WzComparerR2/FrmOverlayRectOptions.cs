using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevComponents.Editors;
using Microsoft.Xna.Framework;
using WzComparerR2.Controls;
using WzComparerR2.Config;
using DevComponents.DotNetBar.Controls;

namespace WzComparerR2
{
    public partial class FrmOverlayRectOptions : DevComponents.DotNetBar.Office2007Form
    {
        public FrmOverlayRectOptions() : this(0, 0, null)
        {

        }

        public FrmOverlayRectOptions(int s, int e, ImageHandlerConfig config)
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
#endif
            this.txtLeft.Value = 0;
            this.txtRight.Value = 0;
            this.txtTop.Value = 0;
            this.txtBottom.Value = 0;
            this.txtRadius.Value = 0;
            this.txtStart.Value = 0;
            this.txtStart.MaxValue = e;
            this.txtEnd.Value = e;
            this.txtEnd.MaxValue = e;

            this.colorPickerButton1.SelectedColor = config.OverlayRectColor;
            this.txtAlpha.Value = config.OverlayRectAlpha;
        }

        public void GetValues(out Point lt, out Point rb, out int start, out int end, out int radius, out int alpha, out int type, ImageHandlerConfig config)
        {
            lt = new Point(this.txtLeft.ValueObject as int? ?? 0, this.txtTop.ValueObject as int? ?? 0);
            rb = new Point(this.txtRight.ValueObject as int? ?? 0, this.txtBottom.ValueObject as int? ?? 0);
            var s = this.txtStart.ValueObject as int? ?? 0;
            var e = this.txtEnd.ValueObject as int? ?? 0;
            start = s;
            end = s <= e ? e : s;
            radius = this.txtRadius.ValueObject as int? ?? 0;
            alpha = this.txtAlpha.ValueObject as int? ?? 60;
            type = this.chkIsCircle.Checked ? 1 : 0;

            config.OverlayRectColor = this.colorPickerButton1.SelectedColor;
            config.OverlayRectAlpha = alpha;

            return;
        }

        private void ChkIsCircle_CheckedChanged(object sender, System.EventArgs e)
        {
            if ((sender as CheckBoxX).Checked)
            {
                this.txtRadius.Enabled = true;
                this.txtRight.Enabled = false;
                this.txtBottom.Enabled = false;
            }
            else
            {
                this.txtRadius.Enabled = false;
                this.txtRight.Enabled = true;
                this.txtBottom.Enabled = true;
            }
        }
    }
}