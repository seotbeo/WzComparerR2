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
using System.Drawing;

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
            this.Font = new Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
#endif
            this.txtLeft.Value = 0;
            this.txtRight.Value = 0;
            this.txtTop.Value = 0;
            this.txtBottom.Value = 0;
            this.txtStart.Value = 0;
            this.txtStart.MaxValue = e;
            this.txtEnd.Value = e;
            this.txtEnd.MaxValue = e;

            this.colorPickerButton1.SelectedColor = config.OverlayRectColor;
        }

        public void GetValues(out Microsoft.Xna.Framework.Point lt, out Microsoft.Xna.Framework.Point rb, out int start, out int end, ImageHandlerConfig config)
        {
            lt = new Microsoft.Xna.Framework.Point(this.txtLeft.ValueObject as int? ?? 0, this.txtTop.ValueObject as int? ?? 0);
            rb = new Microsoft.Xna.Framework.Point(this.txtRight.ValueObject as int? ?? 0, this.txtBottom.ValueObject as int? ?? 0);
            var s = this.txtStart.ValueObject as int? ?? 0;
            var e = this.txtEnd.ValueObject as int? ?? 0;
            start = s;
            end = s <= e ? e : s;

            config.OverlayRectColor = this.colorPickerButton1.SelectedColor;

            return;
        }
    }
}