using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.AvatarCommon
{
    public static class Prism
    {
        public static BitmapOrigin Apply(BitmapOrigin src, PrismData prismData, bool isEffect = false)
        {
            return Apply(src, prismData.Type, prismData.Hue, prismData.Saturation, prismData.Brightness, isEffect);
        }

        public static BitmapOrigin Apply(BitmapOrigin src, int type, int hue, int saturation, int brightness, bool isEffect = false)
        {
            return new BitmapOrigin(Apply(src.Bitmap, type, hue, saturation, brightness, isEffect), src.Origin);
        }

        public static unsafe Bitmap Apply(Bitmap src, int type, int hue, int saturation, int brightness, bool isEffect = false)
        {
            if (!Valid(type, hue, saturation, brightness))
                return src == null ? null : new Bitmap(src);

            var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            var srcData = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var dstData = dst.LockBits(new Rectangle(0, 0, dst.Width, dst.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            for (int y = 0; y < srcData.Height; y++)
            {
                byte* srcRow = (byte*)srcData.Scan0 + y * srcData.Stride;
                byte* dstRow = (byte*)dstData.Scan0 + y * dstData.Stride;
                for (int x = 0; x < srcData.Width; x++)
                {
                    var b = srcRow[x * 4];
                    var g = srcRow[x * 4 + 1];
                    var r = srcRow[x * 4 + 2];
                    var a = srcRow[x * 4 + 3];
                    var rgb = new RGB(r, g, b);
                    var hsv = new HSV(0, 0, 0);
                    SetHSVfromRGB(ref rgb, ref hsv);

                    bool convert = CheckColorType(type, (int)hsv.Hue);
                    if ((rgb.R == 0 && rgb.G == 0 && rgb.B == 0) || (rgb.R == 255 && rgb.G == 255 && rgb.B == 255) || a == 0)
                    {
                        convert = false;
                    }
                    if (convert)
                    {
                        RGB addRGB = new RGB(0, 0, 0);
                        bool[] breakUpperBound = [isEffect || rgb.R > 238, isEffect || rgb.G > 238, isEffect || rgb.B > 238];

                        if (hue > 0)
                        {
                            hsv.Hue = (hsv.Hue + hue) % 360;
                            SetRGBfromHSV(ref rgb, ref hsv, isEffect);
                        }

                        if (saturation != 100 && hsv.Saturation != 0)
                        {
                            var ds = (saturation - 100) / 100f;
                            hsv.Saturation = Clamp(hsv.Saturation + ds, 0, 1);

                            SetRGBfromHSV(ref rgb, ref hsv, false, doRounding: false);
                        }

                        if (brightness != 100)
                        {
                            addRGB = CalcBrightness(ref rgb, brightness, isEffect);
                        }

                        rgb.R = Clamp(rgb.R + addRGB.R, 0, breakUpperBound[0] ? 255 : 238);
                        rgb.G = Clamp(rgb.G + addRGB.G, 0, breakUpperBound[1] ? 255 : 238);
                        rgb.B = Clamp(rgb.B + addRGB.B, 0, breakUpperBound[2] ? 255 : 238);

                        if (!isEffect)
                        {
                            rgb.R = (int)ApplyStep(rgb.R);
                            rgb.G = (int)ApplyStep(rgb.G);
                            rgb.B = (int)ApplyStep(rgb.B);
                        }
                    }

                    dstRow[x * 4] = (byte)rgb.B;
                    dstRow[x * 4 + 1] = (byte)rgb.G;
                    dstRow[x * 4 + 2] = (byte)rgb.R;
                    dstRow[x * 4 + 3] = a;
                }
            }

            dst.UnlockBits(dstData);
            src.UnlockBits(srcData);

            return dst;
        }

        private static bool Valid(int type, int hue, int saturation, int brightness)
        {
            if (type < 0 || type > 6) return false;
            if (hue < 0 || hue > 359) return false;
            if (saturation < 1 || saturation > 199) return false;
            if (brightness < 1 || brightness > 199) return false;
            if (hue == 0 && saturation == 100 && brightness == 100) return false;

            return true;
        }

        private static void SetHSVfromRGB(ref RGB rgb, ref HSV hsv)
        {
            var r = rgb.R / 255f;
            var g = rgb.G / 255f;
            var b = rgb.B / 255f;

            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var mid = r + g + b - max - min;
            var d = max - min;
            rgb.Gap = d * 255;
            rgb.Max = max * 255;
            rgb.Min = min * 255;
            rgb.Gray = max == min;

            hsv.Brightness = (max + min) / 2;

            if (hsv.Brightness > 0 && hsv.Brightness < 1)
            {
                hsv.Saturation = d / (1 - Math.Abs(2 * hsv.Brightness - 1));
            }

            if (rgb.R == rgb.G && rgb.G == rgb.B)
            {
                hsv.Hue = 0;
            }
            else
            {
                if (r == max)
                {
                    hsv.Hue = (g - b) / d;
                }
                else if (g == max)
                {
                    hsv.Hue = 2f + (b - r) / d;
                }
                else if (b == max)
                {
                    hsv.Hue = 4f + (r - g) / d;
                }
                hsv.Hue *= 60f;

                if (hsv.Hue < 0f)
                {
                    hsv.Hue += 360f;
                }
            }
        }

        private static void SetRGBfromHSV(ref RGB rgb, ref HSV hsv, bool step, bool doRounding = true)
        {
            float r = 0, g = 0, b = 0;

            var c = (1 - Math.Abs(2 * hsv.Brightness - 1)) * hsv.Saturation;
            var x = c * (1 - Math.Abs(((hsv.Hue / 60) % 2f) - 1));
            var m = hsv.Brightness - c / 2;

            switch ((int)hsv.Hue / 60)
            {
                case 0:
                    r = c;
                    g = x;
                    break;
                case 1:
                    r = x;
                    g = c;
                    break;
                case 2:
                    g = c;
                    b = x;
                    break;
                case 3:
                    g = x;
                    b = c;
                    break;
                case 4:
                    b = c;
                    r = x;
                    break;
                case 5:
                    b = x;
                    r = c;
                    break;
            }

            if (doRounding)
            {
                rgb.R = (int)(ApplyStep((r + m) * 255, step: (step ? 1f : 17f)));
                rgb.G = (int)(ApplyStep((g + m) * 255, step: (step ? 1f : 17f)));
                rgb.B = (int)(ApplyStep((b + m) * 255, step: (step ? 1f : 17f)));
            }
            else
            {
                rgb.R = (r + m) * 255;
                rgb.G = (g + m) * 255;
                rgb.B = (b + m) * 255;
            }
        }

        private static RGB CalcBrightness(ref RGB rgb, int brightness, bool effect)
        {
            RGB addRGB = new RGB(0, 0, 0);
            float r = rgb.R;
            float g = rgb.G;
            float b = rgb.B;
            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            float[] color = [r, g, b];
            float[] addColor = [0, 0, 0];

            //bool gray = max == min;
            bool gray = rgb.Gray;
            float gap = rgb.Gap;

            if (brightness < 100)
            {
                if (gray) return addRGB;

                float adjustedGap = gap / rgb.Max * max;
                for (int i = 0; i < color.Length; i++)
                {
                    var c = color[i];
                    if (c <= 0)
                    {
                        addColor[i] = -255;
                        continue;
                    }
                    var v = 0f;

                    if (min == 0)
                    {
                        v = (c * (brightness - 100) / 100f);
                    }
                    else
                    {
                        v = (adjustedGap * (brightness - 100) / 100f);
                    }
                    addColor[i] = v;
                }
            }
            else
            {
                for (int i = 0; i < color.Length; i++)
                {
                    var c = color[i];
                    var v = 0f;

                    if (max == 255 || gray)
                    {
                        v = ((255 - c) * (brightness - 100) / 100f);
                    }
                    else
                    {
                        var amount = (255 - max) * (15 - 12 * gap / (rgb.Min + gap)) / 15 + max;
                        v = ((amount - c) * (brightness - 100) / 100f);
                    }
                    addColor[i] = v;
                }
            }

            addRGB.R = addColor[0];
            addRGB.G = addColor[1];
            addRGB.B = addColor[2];

            return addRGB;
        }

        private static bool CheckColorType(int type, int hue)
        {
            bool convert = true;
            switch (type)
            {
                case 0:
                    // 전체 색상 계열
                    break;
                case 1:
                    // 빨간색 계열
                    if (hue >= 30 && hue <= 330) convert = false;
                    break;
                case 2:
                    // 노란색 계열
                    if (hue < 30 || hue > 90) convert = false;
                    break;
                case 3:
                    // 초록색 계열
                    if (hue < 90 || hue > 150) convert = false;
                    break;
                case 4:
                    // 청록색 계열
                    if (hue < 150 || hue > 210) convert = false;
                    break;
                case 5:
                    // 파란색 계열
                    if (hue < 210 || hue > 270) convert = false;
                    break;
                case 6:
                    // 자주색 계열
                    if (hue < 270 || hue > 330) convert = false;
                    break;
            }
            return convert;
        }

        private static float CustomRound(float value, float threshold)
        {
            if (value - Math.Floor(value) >= threshold)
            {
                return (float)Math.Ceiling(value);
            }
            else
            {
                return (float)Math.Floor(value);
            }
        }

        private static float ApplyStep(float value, float step = 17f, float threshold = 0.985f)
        {
            return CustomRound(value / step, threshold) * step;
        }

        private static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0) return min;
            if (value.CompareTo(max) > 0) return max;
            return value;
        }

        private struct RGB
        {
            public float R;
            public float G;
            public float B;
            public float Gap;
            public float Min;
            public float Max;
            public bool Gray;

            public RGB(float r, float g, float b)
            {
                R = r;
                G = g;
                B = b;
            }
        }

        private struct HSV
        {
            public float Hue;
            public float Saturation;
            public float Brightness;

            public HSV(float h, float s, float v)
            {
                Hue = h;
                Saturation = s;
                Brightness = v;
            }
        }
    }
}
