using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using System.Drawing.Imaging;
using WzComparerR2.CharaSim;

namespace WzComparerR2.Avatar.UI
{
    internal partial class AvatarPartButtonItem : ButtonItem
    {
        public AvatarPartButtonItem(int ID, int? mixColor, int? mixOpacity)
        {
            InitializeComponent();
            GearType type = Gear.GetGearType(ID);
            if (Gear.IsFace(type) || Gear.IsHair(type))
            {
                CheckBoxItem[] rdoMixColors = { this.rdoMixColor0, this.rdoMixColor1, this.rdoMixColor2, this.rdoMixColor3, this.rdoMixColor4, this.rdoMixColor5, this.rdoMixColor6, this.rdoMixColor7 };

                this.SubItems.AddRange(rdoMixColors);
                this.SubItems.Add(this.sliderMixRatio);

                string[] colorsRef;
                string resourceType;
                int color;
                if (Gear.IsFace(type))
                {
                    colorsRef = LensColors;
                    resourceType = "MixLens";
                    color = (ID / 100 % 10) % 8;
                }
                else
                {
                    colorsRef = HairColors;
                    resourceType = "MixHair";
                    color = ID % 10;
                }

                for (int i = 0; i <= 7; i++)
                {
                    rdoMixColors[i].Name = $"ID{ID}_{rdoMixColors[i].Name}";
                    rdoMixColors[i].Text += colorsRef[i];

                    Bitmap normal = (Bitmap)Properties.Resources.ResourceManager.GetObject($"UtilDlgEx_{resourceType}_KR_BtColor_button_BtColor{i}_normal_0");
                    Bitmap pressed = (Bitmap)Properties.Resources.ResourceManager.GetObject($"UtilDlgEx_{resourceType}_KR_BtColor_button_BtColor{i}_pressed_0");
                    rdoMixColors[i].CheckBoxImageUnChecked = PadImage(normal, pressed.Size);
                    rdoMixColors[i].CheckBoxImageChecked = PadImage(pressed, normal.Size);
                }

                rdoMixColors[mixColor ?? color].Checked = true;
                rdoMixColors[color].Enabled = false;

                this.sliderMixRatio.Name = $"ID{ID}_{this.sliderMixRatio.Name}";
                this.sliderMixRatio.Value = mixOpacity ?? 0;
            }
        }

        public static readonly string[] HairColors = new[] { "검은색", "빨간색", "주황색", "노란색", "초록색", "파란색", "보라색", "갈색" };
        public static readonly string[] LensColors = new[] { "검은색", "파란색", "빨간색", "초록색", "갈색", "에메랄드", "보라색", "자수정색" };

        public void SetIcon(Bitmap icon)
        {
            if (icon != null)
            {
                if (!this.ImageFixedSize.IsEmpty && icon.Size != this.ImageFixedSize)
                {
                    Bitmap newIcon = new Bitmap(this.ImageFixedSize.Width, this.ImageFixedSize.Height, PixelFormat.Format32bppArgb);
                    Graphics g = Graphics.FromImage(newIcon);
                    int x = (newIcon.Width - icon.Width) / 2;
                    int y = (newIcon.Height - icon.Height) / 2;
                    g.DrawImage(icon, x, y);
                    g.Dispose();
                    this.Image = newIcon;
                }
                else
                {
                    this.Image = icon;
                }
            }
            else
            {
                this.Image = null;
            }
        }

        private Bitmap PadImage(Bitmap originalImage, Size newSize)
        {
            if (originalImage.Width >= newSize.Width && originalImage.Height >= newSize.Height)
            {
                return originalImage;
            }

            Bitmap newImage = new Bitmap(Math.Max(originalImage.Width, newSize.Width), Math.Max(originalImage.Height, newSize.Height));
            using (Graphics graphics = Graphics.FromImage(newImage))
            {
                graphics.Clear(Color.Transparent);
                int x = (newImage.Width - originalImage.Width) / 2;
                int y = (newImage.Height - originalImage.Height) / 2;
                graphics.DrawImage(originalImage, x, y);
            }
            return newImage;
        }
    }
}
