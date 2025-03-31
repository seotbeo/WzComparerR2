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
    public partial class FrmOverlayAniOptions : DevComponents.DotNetBar.Office2007Form
    {
        public FrmOverlayAniOptions() : this(0, 0)
        {

        }

        public FrmOverlayAniOptions(int startIdx, int endIdx) : this(startIdx, endIdx, null, false)
        {

        }

        public FrmOverlayAniOptions(int startIdx, int endIdx, string multiFrameInfo, bool isPngFrameAni)
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
#endif
            if (multiFrameInfo != null)
            {
                this.Text += " (멀티 프레임 : " + multiFrameInfo + ")";
            }
            this.txtDelayOffset.Value = 0;
            this.txtMoveX.Value = 0;
            this.txtMoveY.Value = 0;
            this.txtFrameStart.Value = startIdx;
            this.txtFrameEnd.Value = endIdx;
            this.txtFrameStart.MaxValue = endIdx;
            this.txtFrameEnd.MaxValue = endIdx;

            if (isPngFrameAni)
            {
                this.txtPngDelay.Enabled = true;
            }
        }

        public void GetValues(out int delayOffset, out int moveX, out int moveY, out int frameStart, out int frameEnd, out int pngDelay)
        {
            delayOffset = this.txtDelayOffset.ValueObject as int? ?? 0;
            moveX = this.txtMoveX.ValueObject as int? ?? 0;
            moveY = this.txtMoveY.ValueObject as int? ?? 0;
            frameStart = this.txtFrameStart.ValueObject as int? ?? -1;
            frameEnd = this.txtFrameEnd.ValueObject as int? ?? -1;
            pngDelay = this.txtPngDelay.ValueObject as int? ?? 0;

            delayOffset = delayOffset / 10 * 10;
            pngDelay = pngDelay / 10 * 10;

            return;
        }
    }
}