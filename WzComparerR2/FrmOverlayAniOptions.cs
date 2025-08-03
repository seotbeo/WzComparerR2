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
    public partial class FrmOverlayAniOptions : DevComponents.DotNetBar.Office2007Form
    {
        public FrmOverlayAniOptions(List<Frame> frames, string multiFrameInfo, bool isPngFrameAni)
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
            this.Frames = frames;
            var endIdx = frames.Count - 1;

            this.txtDelayOffset.Value = 0;
            this.txtMoveX.Value = 0;
            this.txtMoveY.Value = 0;
            this.txtFrameStart.Value = 0;
            this.txtFrameEnd.Value = endIdx;
            this.txtFrameStart.MaxValue = endIdx;
            this.txtFrameEnd.MaxValue = endIdx;
            this.txtSpeedX.Value = 0;
            this.txtSpeedY.Value = 0;
            this.txtGoX.Value = 0;
            this.txtGoY.Value = 0;
            this.chkFullMove.Checked = true;

            if (isPngFrameAni)
            {
                this.txtPngDelay.Enabled = true;
            }

        }

        private List<Frame> Frames {  get; set; }

        private int GetDelay(int start, int end)
        {
            var ret = 0;
            for (int i = Math.Max(0, start); i < Math.Min(Frames.Count, end); i++)
            {
                ret += Frames[i].Delay;
            }
            return ret;
        }

        public void SetSpine()
        {
            this.txtFrameStart.Enabled = false;
            this.txtFrameEnd.Enabled = false;
            this.txtSpeedX.Enabled = false;
            this.txtSpeedY.Enabled = false;
            this.txtGoX.Enabled = false;
            this.txtGoY.Enabled = false;
            this.chkFullMove.Enabled = false;
        }

        public OverlayOptions GetValues()
        {
            var ret = new OverlayOptions()
            {
                AniOffset = this.txtDelayOffset.ValueObject as int? ?? 0,
                AniStart = this.txtFrameStart.ValueObject as int? ?? -1,
                AniEnd = this.txtFrameEnd.ValueObject as int? ?? -1,
                PosX = this.txtMoveX.ValueObject as int? ?? 0,
                PosY = this.txtMoveY.ValueObject as int? ?? 0,

                PngDelay = this.txtPngDelay.ValueObject as int? ?? 0,

                FullMove = this.chkFullMove.Checked,

                SpeedX = this.txtSpeedX.ValueObject as int? ?? 0,
                SpeedY = this.txtSpeedY.ValueObject as int? ?? 0,
                GoX = this.txtGoX.ValueObject as int? ?? 0,
                GoY = this.txtGoY.ValueObject as int? ?? 0
            };

            ret.AniOffset = ret.AniOffset / 10 * 10;
            ret.PngDelay = ret.PngDelay / 10 * 10;

            return ret;
        }
    }
}