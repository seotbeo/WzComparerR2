using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevComponents.Editors;
using WzComparerR2.Animation;
using WzComparerR2.Controls;

namespace WzComparerR2
{
    public partial class FrmCaptureAniOptions : DevComponents.DotNetBar.Office2007Form
    {
        public FrmCaptureAniOptions(int max)
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
#endif
            this.txtCaptureTime.Value = 0;
            this.txtCaptureTime.MaxValue = max;
        }

        public CaptureAniOptions GetValues()
        {
            var ret = new CaptureAniOptions()
            {
                CaptureTime = this.txtCaptureTime.ValueObject as int? ?? 0,
            };

            return ret;
        }
    }
}