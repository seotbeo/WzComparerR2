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
    public partial class FrmMultiAniOptions : DevComponents.DotNetBar.Office2007Form
    {
        public FrmMultiAniOptions() : this(0, 0)
        {

        }

        public FrmMultiAniOptions(int startIdx, int endIdx)
        {
            InitializeComponent();
            this.txtDelayOffset.Value = 0;
            this.txtMoveX.Value = 0;
            this.txtMoveY.Value = 0;
            this.txtFrameStart.Value = startIdx;
            this.txtFrameEnd.Value = endIdx;
            this.txtFrameStart.MaxValue = endIdx;
            this.txtFrameEnd.MaxValue = endIdx;
        }

        public void GetValues(out int delayOffset, out int moveX, out int moveY, out int frameStart, out int frameEnd)
        {
            delayOffset = this.txtDelayOffset.ValueObject as int? ?? 0;
            moveX = this.txtMoveX.ValueObject as int? ?? 0;
            moveY = this.txtMoveY.ValueObject as int? ?? 0;
            frameStart = this.txtFrameStart.ValueObject as int? ?? -1;
            frameEnd = this.txtFrameEnd.ValueObject as int? ?? -1;

            delayOffset = delayOffset / 10 * 10;

            return;
        }
    }
}
