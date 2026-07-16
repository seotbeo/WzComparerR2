using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.AvatarCommon
{
    public class PrismData
    {
        public PrismData()
        {
            this.Type = 0;
            this.Hue = 0;
            this.Saturation = 100;
            this.Brightness = 100;
            this.ConvertPureBlack = false;
        }

        public PrismData(int type, int hue, int saturation, int brightness)
        {
            this.Type = type;
            this.Hue = hue;
            this.Saturation = saturation;
            this.Brightness = brightness;
            this.ConvertPureBlack = false;
        }

        public int Type;
        public int Hue;
        public int Saturation;
        public int Brightness;
        public bool ConvertPureBlack;

        public bool Valid
        {
            get { return this.Hue != 0 || this.Saturation != 100 || this.Brightness != 100; }
        }

        public void Clear()
        {
            this.Type = 0;
            this.Hue = 0;
            this.Saturation = 100;
            this.Brightness = 100;
        }

        public void Set(int type, int hue, int saturation, int brightness, bool convertPureBlack)
        {
            this.Type = type;
            this.Hue = hue;
            this.Saturation = saturation;
            this.Brightness = brightness;
            this.ConvertPureBlack = convertPureBlack;

            if (!this.Valid)
            {
                this.Clear();
            }
        }

        public string GetColorType()
        {
            if (!this.Valid) return null;

            switch (this.Type)
            {
                case 0:
                    return "전체 색상 계열";
                case 1:
                    return "빨간색 계열";
                case 2:
                    return "노란색 계열";
                case 3:
                    return "초록색 계열";
                case 4:
                    return "청록색 계열";
                case 5:
                    return "파란색 계열";
                case 6:
                    return "자주색 계열";
                default:
                    return null;
            }
        }
    }
}
