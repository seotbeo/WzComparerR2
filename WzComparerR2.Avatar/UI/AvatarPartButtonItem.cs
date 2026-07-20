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
        public AvatarPartButtonItem(int ID, int? mixColor, int? mixOpacity, bool hasWhiteMixColor, PrismDataCollection pdc)
        {
            InitializeComponent();
            this.chkShowEffect.Name += ID.ToString();
            this.SubItems.Add(this.chkShowEffect);
            this.PrismData = pdc.Clone();
            this.PrismIndex = 0;
            GearType type = Gear.GetGearType(ID);
            if (Gear.IsFace(type) || Gear.IsHair(type))
            {
                CheckBoxItem[] rdoMixColors = { this.rdoMixColor0, this.rdoMixColor1, this.rdoMixColor2, this.rdoMixColor3, this.rdoMixColor4, this.rdoMixColor5, this.rdoMixColor6, this.rdoMixColor7 };
                if (hasWhiteMixColor)
                {
                    rdoMixColors = new[] { this.rdoMixColor0, this.rdoMixColor1, this.rdoMixColor2, this.rdoMixColor3, this.rdoMixColor4, this.rdoMixColor5, this.rdoMixColor6, this.rdoMixColor7, this.rdoMixColor8 };
                }
                this.SubItems.AddRange(rdoMixColors);
                this.SubItems.Add(this.sliderMixRatio);

                string[] colorsRef;
                string resourceType;
                int color;
                if (Gear.IsFace(type))
                {
                    colorsRef = LensColors;
                    resourceType = "MixLens";
                    color = (ID / 100 % 10);
                    if (!hasWhiteMixColor)
                    {
                        color %= 8;
                    }
                }
                else
                {
                    colorsRef = HairColors;
                    resourceType = "MixHair";
                    color = ID % 10;
                }

                for (int i = 0; i < rdoMixColors.Length; i++)
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
                SetPrism(ID);
            }
        }

        public static readonly string[] HairColors = new[] { "검은색", "빨간색", "주황색", "노란색", "초록색", "파란색", "보라색", "갈색" };
        public static readonly string[] LensColors = new[] { "검은색", "파란색", "빨간색", "초록색", "갈색", "에메랄드", "보라색", "자수정색", "흰색" };
        public static readonly string[] PrismResourceTypes = new[] { "Hair", "Hair", "Hair", "Lens", "Lens", "Hair", "Lens", };
        public static readonly int[] PrismResourceIndex = new[] { 0, 1, 3, 3, 5, 5, 7 };
        public PrismDataCollection PrismData;
        public int PrismIndex;

        public void Reset(int ID, bool hasWhiteMixColor)
        {
            GearType type = Gear.GetGearType(ID);
            if (Gear.IsFace(type) || Gear.IsHair(type))
            {
                CheckBoxItem[] rdoMixColors = { this.rdoMixColor0, this.rdoMixColor1, this.rdoMixColor2, this.rdoMixColor3, this.rdoMixColor4, this.rdoMixColor5, this.rdoMixColor6, this.rdoMixColor7, this.rdoMixColor8 };
                int color;
                int mixOpacity = 0;
                if (Gear.IsFace(type))
                {
                    color = (ID / 100 % 10);
                    if (!hasWhiteMixColor)
                    {
                        color %= 8;
                    }
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
                this.PrismData.Clear();
                var part = this.Tag as AvatarPart;
                if (part != null)
                {
                    part.PrismData.Clear();
                }
                this.PrismIndex = 0;

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
                this.chkConvertPureBlack.Checked = false;
            }
        }

        public void SetPrism(int ID)
        {
            GearType type = Gear.GetGearType(ID);
            PrismDataCollection.PrismDataType pidx = 0;
            Enum.TryParse(this.PrismIndex.ToString(), out pidx);
            PrismData prismData = this.PrismData.Get(pidx);

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
            this.chkConvertPureBlack.Checked = prismData.ConvertPureBlack;

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

            if (Gear.IsWeapon(type) || Gear.IsCashWeapon(type))
            {
                this.SubItems.Add(this.btnChangePrismIndex);
            }
            this.SubItems.AddRange(rdoPrismType);
            this.SubItems.Add(this.labelHue);
            this.SubItems.Add(this.sliderHue);
            this.SubItems.Add(this.labelSaturation);
            this.SubItems.Add(this.sliderSaturation);
            this.SubItems.Add(this.labelBrightness);
            this.SubItems.Add(this.sliderBrightness);
            this.SubItems.Add(this.chkConvertPureBlack);
        }

        public void PrismIndexChanged(int value)
        {
            if (Enum.TryParse(value.ToString(), out PrismDataCollection.PrismDataType type))
            {
                var text = "";
                switch (type)
                {
                    case PrismDataCollection.PrismDataType.Default:
                        text = "일반 프리즘";
                        break;

                    case PrismDataCollection.PrismDataType.WeaponEffect:
                        text = "무기 이펙트 프리즘";
                        break;
                }
                this.btnChangePrismIndex.Text = text;
            }
        }

        public void PrismTypeChanged(int value)
        {
            var part = this.Tag as AvatarPart;
            if (part != null)
            {
                PrismDataCollection.PrismDataType pidx = 0;
                Enum.TryParse(this.PrismIndex.ToString(), out pidx);
                PrismData prismData = this.PrismData.Get(pidx);
                PrismData partPrismData = part.PrismData.Get(pidx);

                prismData.Type = value;
                partPrismData.Type = value;
            }
        }

        public void PrismHueChanged(int value)
        {
            var part = this.Tag as AvatarPart;
            if (part != null)
            {
                PrismDataCollection.PrismDataType pidx = 0;
                Enum.TryParse(this.PrismIndex.ToString(), out pidx);
                PrismData prismData = this.PrismData.Get(pidx);
                PrismData partPrismData = part.PrismData.Get(pidx);

                prismData.Hue = value;
                partPrismData.Hue = value;
            }
        }

        public void PrismSaturationChanged(int value)
        {
            var part = this.Tag as AvatarPart;
            if (part != null)
            {
                PrismDataCollection.PrismDataType pidx = 0;
                Enum.TryParse(this.PrismIndex.ToString(), out pidx);
                PrismData prismData = this.PrismData.Get(pidx);
                PrismData partPrismData = part.PrismData.Get(pidx);

                prismData.Saturation = value;
                partPrismData.Saturation = value;
            }
        }

        public void PrismBrightnessChanged(int value)
        {
            var part = this.Tag as AvatarPart;
            if (part != null)
            {
                PrismDataCollection.PrismDataType pidx = 0;
                Enum.TryParse(this.PrismIndex.ToString(), out pidx);
                PrismData prismData = this.PrismData.Get(pidx);
                PrismData partPrismData = part.PrismData.Get(pidx);

                prismData.Brightness = value;
                partPrismData.Brightness = value;
            }
        }

        public void PrismConvertPureBlackChanged(bool value)
        {
            var part = this.Tag as AvatarPart;
            if (part != null)
            {
                PrismDataCollection.PrismDataType pidx = 0;
                Enum.TryParse(this.PrismIndex.ToString(), out pidx);
                PrismData prismData = this.PrismData.Get(pidx);
                PrismData partPrismData = part.PrismData.Get(pidx);

                prismData.ConvertPureBlack = value;
                partPrismData.ConvertPureBlack = value;
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
