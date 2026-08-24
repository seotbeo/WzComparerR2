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
using WzComparerR2.Rendering;

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
            this.isPngFrameAni = isPngFrameAni;
            if (isPngFrameAni)
            {
                this.txtPngDelay.Enabled = true;
            }
            this.Frames = frames;
            var endIdx = frames.Count - 1;
            InitFrameDelays();
            UpdateAniLength();

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
            this.txtAngle.Value = 0;
            this.chkFullMove.Checked = true;
            this.chkFlipX.Checked = false;
            this.chkFlipY.Checked = false;

            this.txtAlpha.Value = 100;
            this.txtAlphaDst.Value = 0;
            this.txtAlphaStart.Value = 0;
            this.txtAlphaEnd.Value = this.MaxDelay;
            this.txtAlphaEnd.MaxValue = this.MaxDelay;

            this.colorPickerButton1.SelectedColor = Color.White;

            this.txtPngDelay.ValueChanged += TxtPngDelay_ValueChanged;
            this.txtFrameStart.ValueChanged += TxtFrameStart_ValueChanged;
            this.txtFrameEnd.ValueChanged += TxtFrameEnd_ValueChanged;
            this.chkAlphaGradation.CheckedChanged += ChkAlphaGradation_CheckedChanged;
        }

        private List<Frame> Frames { get; set; }
        private List<int> CumulativeFrameDelay { get; set; }
        private int MaxDelay { get; set; }
        private bool isPngFrameAni { get; set; }

        private int GetDelay(int start)
        {
            return GetDelay(start, start);
        }

        private int GetDelay(int start, int end)
        {
            if (this.Frames == null || this.CumulativeFrameDelay == null || start > end) return 0;

            start = Math.Max(start, 0);
            end = Math.Max(Math.Min(end, this.Frames.Count - 1), 0);

            return this.CumulativeFrameDelay[end + 1] - this.CumulativeFrameDelay[start];
        }

        private void InitFrameDelays()
        {
            if ((this.Frames?.Count ?? 0) <= 0) return;

            this.CumulativeFrameDelay = new List<int>(this.Frames.Count + 1) { 0 };
            int sum = 0;
            foreach (var frame in this.Frames)
            {
                this.CumulativeFrameDelay.Add(sum += frame.Delay);
            }
        }

        private void UpdateAniLength()
        {
            if (this.txtPngDelay.Enabled)
            {
                this.MaxDelay = this.txtPngDelay.ValueObject as int? ?? 0;
            }
            else
            {
                var si = this.txtFrameStart.ValueObject as int? ?? 0;
                var ei = this.txtFrameEnd.ValueObject as int? ?? this.Frames.Count - 1;
                this.MaxDelay = GetDelay(si, ei);
            }

            this.labelXAniLengthValue.Text = $"{this.MaxDelay} ms";
            this.txtAlphaStart.MaxValue = this.MaxDelay;
            this.txtAlphaEnd.MaxValue = this.MaxDelay;
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
            this.chkFlipX.Enabled = false;
            this.chkFlipY.Enabled = false;
        }

        public OverlayOptions GetValues()
        {
            var si = this.txtFrameStart.ValueObject as int? ?? 0;
            var ei = this.txtFrameEnd.ValueObject as int? ?? this.Frames.Count - 1;
            var s = 0;
            var e = GetDelay(si, ei);
            var a = this.txtAlpha.ValueObject as int? ?? 100;
            var a_s = this.txtAlphaStart.ValueObject as int? ?? 0;
            var a_e = this.txtAlphaEnd.ValueObject as int? ?? (s <= e ? e : s);
            if (a_s < s) a_s = s;
            if (a_e > e) a_e = e;
            var pngDelay = this.txtPngDelay.ValueObject as int? ?? 0;

            var ret = new OverlayOptions()
            {
                AniOffset = this.txtDelayOffset.ValueObject as int? ?? 0,
                AniStartIndex = this.txtFrameStart.ValueObject as int? ?? -1,
                AniEndIndex = this.txtFrameEnd.ValueObject as int? ?? -1,
                AniStartTime = isPngFrameAni ? 0 : GetDelay(0, si - 1),
                AniEndTime = isPngFrameAni ? pngDelay : GetDelay(0, ei - 1),
                PosX = this.txtMoveX.ValueObject as int? ?? 0,
                PosY = this.txtMoveY.ValueObject as int? ?? 0,

                PngDelay = pngDelay,

                FullMove = this.chkFullMove.Checked,
                FlipX = this.chkFlipX.Checked,
                FlipY = this.chkFlipY.Checked,
                Angle = this.txtAngle.ValueObject as int? ?? 0,

                SpeedX = this.txtSpeedX.ValueObject as int? ?? 0,
                SpeedY = this.txtSpeedY.ValueObject as int? ?? 0,
                GoX = this.txtGoX.ValueObject as int? ?? 0,
                GoY = this.txtGoY.ValueObject as int? ?? 0,

                Color = this.colorPickerButton1.SelectedColor.ToXnaColor(),

                Alpha = a,
                AlphaGradation = this.chkAlphaGradation.Checked,
                AlphaDst = this.txtAlphaDst.ValueObject as int? ?? a,
                AlphaStart = a_s,
                AlphaEnd = a_e,
            };

            ret.AniOffset = ret.AniOffset / 10 * 10;
            ret.PngDelay = ret.PngDelay / 10 * 10;

            return ret;
        }

        private void TxtFrameStart_ValueChanged(object sender, EventArgs e)
        {
            UpdateAniLength();
        }

        private void TxtFrameEnd_ValueChanged(object sender, EventArgs e)
        {
            UpdateAniLength();
        }

        private void TxtPngDelay_ValueChanged(object sender, EventArgs e)
        {
            UpdateAniLength();
        }

        private void ChkAlphaGradation_CheckedChanged(object sender, EventArgs e)
        {
            bool value = (sender as DevComponents.DotNetBar.Controls.CheckBoxX).Checked;

            if (value)
            {
                this.txtAlphaStart.Value = 0;
                this.txtAlphaEnd.Value = this.MaxDelay;
            }
            this.txtAlphaDst.Enabled = value;
            this.txtAlphaStart.Enabled = value;
            this.txtAlphaEnd.Enabled = value;
        }
    }
}