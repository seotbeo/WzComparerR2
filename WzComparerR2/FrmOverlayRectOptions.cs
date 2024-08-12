using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevComponents.Editors;
using WzComparerR2.Controls;

namespace WzComparerR2
{
    public partial class FrmOverlayRectOptions : DevComponents.DotNetBar.Office2007Form
    {
        public FrmOverlayRectOptions() : this(0, 0)
        {

        }

        public FrmOverlayRectOptions(int s, int e)
        {
            InitializeComponent();
            this.txtLeft.Value = 0;
            this.txtRight.Value = 0;
            this.txtTop.Value = 0;
            this.txtBottom.Value = 0;
            this.txtStart.Value = 0;
            this.txtStart.MaxValue = s;
            this.txtEnd.Value = e;
            this.txtEnd.MaxValue = e;
        }

        public void GetValues(out Microsoft.Xna.Framework.Point lt, out Microsoft.Xna.Framework.Point rb, out int start, out int end)
        {
            lt = new Microsoft.Xna.Framework.Point(this.txtLeft.ValueObject as int? ?? 0, this.txtTop.ValueObject as int? ?? 0);
            rb = new Microsoft.Xna.Framework.Point(this.txtRight.ValueObject as int? ?? 0, this.txtBottom.ValueObject as int? ?? 0);
            var s = this.txtStart.ValueObject as int? ?? 0;
            var e = this.txtEnd.ValueObject as int? ?? 0;

            start = s;
            end = s <= e ? e : s;

            return;
        }
    }
}