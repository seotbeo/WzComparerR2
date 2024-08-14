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

        public void GetValues(out Point lt, out Point rb, out int start, out int end, ImageHandlerConfig config)
        {
            lt = new Point(this.txtLeft.ValueObject as int? ?? 0, this.txtTop.ValueObject as int? ?? 0);
            rb = new Point(this.txtRight.ValueObject as int? ?? 0, this.txtBottom.ValueObject as int? ?? 0);
            var s = this.txtStart.ValueObject as int? ?? 0;
            var e = this.txtEnd.ValueObject as int? ?? 0;
            start = s;
            end = s <= e ? e : s;

            config.OverlayRectColor = this.colorPickerButton1.SelectedColor;

            return;
        }
    }
}