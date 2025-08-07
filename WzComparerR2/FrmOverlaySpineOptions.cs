using Microsoft.Xna.Framework;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WzComparerR2.Animation;
using WzComparerR2.Controls;

namespace WzComparerR2
{
    public partial class FrmOverlaySpineOptions : DevComponents.DotNetBar.Office2007Form
    {
        public FrmOverlaySpineOptions(AnimationItem aniItem)
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
#endif
            this.aniItem = aniItem;
            bool isMultiFrameAni = aniItem is MultiFrameAnimator;
            if (isMultiFrameAni)
            {
                this.Text = "멀티 프레임 애니메이션 중첩 설정";
                var names = (this.aniItem as MultiFrameAnimator).Animations.ToArray();
                this.comboBoxEx1.Items.AddRange(names);
                this.comboBoxEx2.Items.Add("");
                this.comboBoxEx2.Enabled = false;
                this.txtDelay.Enabled = false;
            }
            else
            {
                var names = (this.aniItem as ISpineAnimator).Animations.ToArray();
                var skins = (this.aniItem as ISpineAnimator).Skins.ToArray();
                this.comboBoxEx1.Items.AddRange(names);
                this.comboBoxEx2.Items.AddRange(skins);
            }
            this.comboBoxEx1.SelectedIndexChanged += ComboBoxEx1_SelectedIndexChanged;
            this.comboBoxEx2.SelectedIndexChanged += ComboBoxEx2_SelectedIndexChanged;
            this.comboBoxEx1.SelectedIndex = 0;
            this.comboBoxEx2.SelectedIndex = 0;

        }

        private AnimationItem aniItem;

        private void ComboBoxEx1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var name = this.comboBoxEx1.SelectedItem as string;
            if (!string.IsNullOrEmpty(name))
            {
                switch (aniItem)
                {
                    case ISpineAnimator spine:
                        spine.SelectedAnimationName = name;
                        break;
                }
            }
            this.txtDelay.Value = aniItem.Length;
        }

        private void ComboBoxEx2_SelectedIndexChanged(object sender, EventArgs e)
        {
            var skin = this.comboBoxEx2.SelectedItem as string;
            if (!string.IsNullOrEmpty(skin))
            {
                switch (aniItem)
                {
                    case ISpineAnimator spine:
                        spine.SelectedSkin = skin;
                        break;
                }
            }
            this.txtDelay.Value = aniItem.Length;
        }

        public void GetValues(out string name, out string skin, out int delay)
        {
            name = this.comboBoxEx1.SelectedItem as string;
            skin = this.comboBoxEx2.SelectedItem as string;
            delay = this.txtDelay.ValueObject as int? ?? aniItem.Length;

            return;
        }
    }
}