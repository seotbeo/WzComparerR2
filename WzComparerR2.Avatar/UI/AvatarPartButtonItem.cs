using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using System.Drawing.Imaging;
using WzComparerR2.CharaSim;
using WzComparerR2.AvatarCommon;

namespace WzComparerR2.Avatar.UI
{
    internal partial class AvatarPartButtonItem : ButtonItem
    {
        public AvatarPartButtonItem(int ID, int? mixColor, int? mixOpacity, PrismData prismData)
        {
            InitializeComponent();
            this.chkShowEffect.Name += ID.ToString();
            this.SubItems.Add(this.chkShowEffect);
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
            else if (type != GearType.body) // 프리즘
            {
                var prismType = prismData.Type;
                var hue = prismData.Hue;
                var saturation = prismData.Saturation;
                var brightness = prismData.Brightness;

                this.sliderHue.Name = $"{ID}_sliderHue";
                this.sliderSaturation.Name = $"{ID}_sliderSaturation_";
                this.sliderBrightness.Name = $"{ID}_sliderBrightness";
                this.sliderHue.Value = hue;
                this.sliderSaturation.Value = saturation - 100;
                this.sliderBrightness.Value = brightness - 100;

                this.labelHue.Name = $"{ID}_labelHue";
                this.labelSaturation.Name = $"{ID}_labelSaturation";
                this.labelBrightness.Name = $"{ID}_labelBrightness";
                this.labelHue.Text = $"색조({hue})";
                this.labelSaturation.Text = $"채도({(saturation > 100 ? "+" : "")}{saturation - 100})";
                this.labelBrightness.Text = $"명도({(brightness > 100 ? "+" : "")}{brightness - 100})";

                CheckBoxItem[] rdoPrismType = { this.rdoPrismType0, this.rdoPrismType1, this.rdoPrismType2, this.rdoPrismType3, this.rdoPrismType4, this.rdoPrismType5, this.rdoPrismType6 };
                for (int i = 0; i < rdoPrismType.Length; i++)
                {
                    rdoPrismType[i].Name = $"{ID}_rdoPrismType{i}";

                    Bitmap normal = (Bitmap)Properties.Resources.ResourceManager.GetObject($"UtilDlgEx_Mix{PrismResourceTypes[i]}_KR_BtColor_button_BtColor{PrismResourceIndex[i]}_normal_0");
                    Bitmap pressed = (Bitmap)Properties.Resources.ResourceManager.GetObject($"UtilDlgEx_Mix{PrismResourceTypes[i]}_KR_BtColor_button_BtColor{PrismResourceIndex[i]}_pressed_0");
                    rdoPrismType[i].CheckBoxImageUnChecked = PadImage(normal, pressed.Size);
                    rdoPrismType[i].CheckBoxImageChecked = PadImage(pressed, normal.Size);
                }
                rdoPrismType[Math.Max(0, prismType)].Checked = true;

                this.SubItems.AddRange(rdoPrismType);
                this.SubItems.Add(this.labelHue);
                this.SubItems.Add(this.sliderHue);
                this.SubItems.Add(this.labelSaturation);
                this.SubItems.Add(this.sliderSaturation);
                this.SubItems.Add(this.labelBrightness);
                this.SubItems.Add(this.sliderBrightness);
            }
        }

        public static readonly string[] HairColors = new[] { "검은색", "빨간색", "주황색", "노란색", "초록색", "파란색", "보라색", "갈색" };
        public static readonly string[] LensColors = new[] { "검은색", "파란색", "빨간색", "초록색", "갈색", "에메랄드", "보라색", "자수정색" };
        public static readonly string[] PrismResourceTypes = new[] { "Hair", "Hair", "Hair", "Lens", "Lens", "Hair", "Hair", };
        public static readonly int[] PrismResourceIndex = new[] { 0, 1, 3, 3, 5, 5, 6 };

        public void Reset(int ID)
        {
            GearType type = Gear.GetGearType(ID);
            if (Gear.IsFace(type) || Gear.IsHair(type))
            {
                CheckBoxItem[] rdoMixColors = { this.rdoMixColor0, this.rdoMixColor1, this.rdoMixColor2, this.rdoMixColor3, this.rdoMixColor4, this.rdoMixColor5, this.rdoMixColor6, this.rdoMixColor7 };
                int color;
                int mixOpacity = 0;
                if (Gear.IsFace(type))
                {
                    color = (ID / 100 % 10) % 8;
                }
                else
                {
                    color = ID % 10;
                }

                rdoMixColors[color].Checked = true;
                this.sliderMixRatio.Value = mixOpacity;
            }
            else
            {
                int hue = 0;
                int saturation = 100;
                int brightness = 100;

                this.sliderHue.Value = hue;
                this.sliderSaturation.Value = saturation - 100;
                this.sliderBrightness.Value = brightness - 100;
                this.labelHue.Text = $"색조({hue})";
                this.labelSaturation.Text = $"채도({(saturation > 100 ? "+" : "")}{saturation - 100})";
                this.labelBrightness.Text = $"명도({(brightness > 100 ? "+" : "")}{brightness - 100})";
                this.rdoPrismType0.Checked = true;
            }
        }

        public void SetIcon(Bitmap icon, bool setPrismLabel = false)
        {
            if (this.Image != null)
            {
                this.Image.Dispose();
                this.Image = null;
            }

            if (icon != null)
            {
                if (!this.ImageFixedSize.IsEmpty && icon.Size != this.ImageFixedSize)
                {
                    Bitmap newIcon = new Bitmap(this.ImageFixedSize.Width, this.ImageFixedSize.Height, PixelFormat.Format32bppArgb);
                    using Graphics g = Graphics.FromImage(newIcon);
                    int x = (newIcon.Width - icon.Width) / 2;
                    int y = (newIcon.Height - icon.Height) / 2;
                    g.DrawImage(icon, x, y);
                    if (setPrismLabel) g.DrawImage(WzComparerR2.Avatar.Properties.Resources.UIWindow2_img_ColoringPrism_ColoringPrismLabel_0, new Point(2, 22));
                    this.Image = newIcon;
                }
                else
                {
                    Bitmap newIcon = new Bitmap(icon);
                    using Graphics g = Graphics.FromImage(newIcon);
                    if (setPrismLabel) g.DrawImage(WzComparerR2.Avatar.Properties.Resources.UIWindow2_img_ColoringPrism_ColoringPrismLabel_0, new Point(2, 22));
                    this.Image = newIcon;
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
