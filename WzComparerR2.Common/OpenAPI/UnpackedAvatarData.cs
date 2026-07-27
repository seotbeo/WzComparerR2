using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NET6_0_OR_GREATER
namespace WzComparerR2.OpenAPI
{
    public class UnpackedAvatarData
    {
        public UnpackedAvatarData(int version)
        {
            Version = version;
            UnknownVer = false;

            if (!AvatarCodeStructure.Structure.ContainsKey(version))
            {
                if (version < AvatarCodeStructure.Structure.Keys.Min())
                {
                    version = AvatarCodeStructure.Structure.Keys.Min();
                }
                else
                {
                    version = AvatarCodeStructure.Structure.Keys.Max();
                }
                UnknownVer = true;
            }
                
            Unpacked = AvatarCodeStructure.Structure[version].Select(d => d.Clone()).ToList();
        }

        public int Version { get; set; }
        public bool UnknownVer { get; set; }
        public List<DataInfo> Unpacked { get; set; }

        private int GetValue(string name)
        {
            foreach (var data in Unpacked)
            {
                if (data.Name == name)
                {
                    if ((data.Value ^ 0x3FF) != 0)
                        return data.Value;
                    else return -1;
                }
            }

            return -1;
        }

        private int GetBits(string name)
        {
            foreach (var data in Unpacked)
            {
                if (data.Name == name)
                {
                    if ((data.Value ^ 0x3FF) != 0)
                        return data.Bits;
                    else return -1;
                }
            }

            return -1;
        }

        private int GetGender()
        {
            return GetValue("gender") != 0 ? 1 : 0;
        }

        private string GetSkin()
        {
            return GetValue("skinID").ToString().PadLeft(2, '0');
        }

        private string GetFace()
        {
            var id = GetValue("faceID");
            if (id == -1) return "";

            var ret = "";
            var face10k = GetValue("face10k");
            switch (face10k)
            {
                case 0:
                    ret += 2;
                    break;
                case 1:
                    ret += 5;
                    break;
                default:
                    ret += face10k;
                    break;
            }
            ret += GetValue("faceGender");
            ret += id.ToString().PadLeft(3, '0');
            return ret;
        }

        private string GetHair()
        {
            var id = GetValue("hairID");
            if (id == -1) return "";

            var ret = "";
            ret += GetValue("hair10k");
            ret += GetValue("hairGender");
            ret += id.ToString().PadLeft(3, '0');
            return ret;
        }

        private string GetCap()
        {
            var id = GetValue("capID");
            if (id == -1) return "";

            var ret = "100";
            ret += GetValue("capGender");
            ret += id.ToString().PadLeft(3, '0');
            return ret;
        }

        private string GetFaceAcc()
        {
            var id = GetValue("faceAccID");
            if (id == -1) return "";

            var ret = "101";
            ret += GetValue("faceAccGender");
            ret += id.ToString().PadLeft(3, '0');
            return ret;
        }

        private string GetEyeAcc()
        {
            var id = GetValue("eyeAccID");
            if (id == -1) return "";

            var ret = "102";
            ret += GetValue("eyeAccGender");
            ret += id.ToString().PadLeft(3, '0');
            return ret;
        }

        private string GetEarAcc()
        {
            var id = GetValue("earAccID");
            if (id == -1) return "";

            var ret = "103";
            ret += GetValue("earAccGender");
            ret += id.ToString().PadLeft(3, '0');
            return ret;
        }

        private string GetCoat()
        {
            var id = GetValue("coatID");
            if (id == -1) return "";

            var ret = GetValue("isLongCoat") == 1 ? "105" : "104";
            ret += GetValue("coatGender");
            ret += id.ToString().PadLeft(3, '0');
            return ret;
        }

        private string GetPants()
        {
            var id = GetValue("pantsID");
            if (id == -1) return "";

            var ret = "106";
            ret += GetValue("pantsGender");
            ret += id.ToString().PadLeft(3, '0');
            return ret;
        }

        private string GetShoes()
        {
            var id = GetValue("shoesID");
            if (id == -1) return "";

            var ret = "107";
            ret += GetValue("shoesGender");
            ret += id.ToString().PadLeft(3, '0');
            return ret;
        }

        private string GetGloves()
        {
            var id = GetValue("glovesID");
            if (id == -1) return "";

            var ret = "108";
            ret += GetValue("glovesGender");
            ret += id.ToString().PadLeft(3, '0');
            return ret;
        }

        private string GetCape()
        {
            var id = GetValue("capeID");
            if (id == -1) return "";

            var ret = "110";
            ret += GetValue("capeGender");
            ret += id.ToString().PadLeft(3, '0');
            return ret;
        }

        private string GetShield()
        {
            var zeroid = GetValue("zeroSubWeaponID");
            if (zeroid > 0)
            {
                var zeroret = "172";
                zeroret += GetValue("zeroSubWeaponGender");
                zeroret += zeroid.ToString().PadLeft(3, '0');
                return zeroret;
            }

            var id = GetValue("shieldID");
            if (id == -1) return "";

            var ret = "";
            switch (GetValue("subWeaponType"))
            {
                case 0:
                case 1:
                    ret += 109; break;
                case 2:
                    ret += 134; break;
                case 3:
                    ret += 135; break;
                case 4:
                    ret += 172; break;
            }
            ret += GetValue("shieldGender");
            ret += id.ToString().PadLeft(3, '0');
            return ret;
        }

        private string GetCashWeapon()
        {
            bool isCW = GetValue("isCashWeapon") != 0;
            if (!isCW) return "";

            var id = GetValue("cashWeaponID");
            if (id == -1) id = GetValue("weaponID");

            var g = GetValue("cashWeaponGender");
            if (g == -1) g = GetValue("weaponGender");

            var ret = "170";
            ret += g;
            ret += id.ToString().PadLeft(3, '0');
            return ret;
        }

        private string GetWeapon()
        {
            var id = GetValue("weaponID");
            var type = GetValue("weaponType");
            if (id == -1) return "";

            var ret = "";
            try
            {
                ret += Utils.WeaponsKMS[type].ToString();
            }
            catch
            {
                return ret;
            }

            var g = GetValue("weaponGender").ToString();
            if (ret.Length == 3) ret += g;

            ret += id.ToString().PadLeft(3, '0');
            return ret;
        }

        private string GetRing(int num)
        {
            var id = GetValue("ringID" + num);
            if (id == -1) return "";

            var g = GetValue("ringGender" + num);
            if (id <= 0 && g <= 0) return "";

            var ret = "111";
            ret += g;
            ret += id.ToString().PadLeft(3, '0');
            return ret;
        }

        private string GetEmotionFaceAcc()
        {
            var id = GetValue("emotionFaceAccID");
            if (id == -1) return "";

            var ret = "101";
            ret += GetValue("emotionFaceAccGender");
            ret += id.ToString().PadLeft(3, '0');
            return ret;
        }

        private byte GetEarType()
        {
            return (byte)GetValue("earType");
        }

        private byte GetJobWingTailType()
        {
            return (byte)GetValue("jobWingTailType");
        }

        private byte GetJobWingTailTypeDetail()
        {
            return (byte)GetValue("jobWingTailTypeDetail");
        }

        private string GetJobWingTailTypeString()
        {
            var detail = "";
            switch (this.JobWingTailTypeDetail)
            {
                case 0:
                    detail += "귀";
                    break;
                case 1:
                    detail += "머리 장식";
                    break;
                case 2:
                    break;
            }

            switch (this.JobWingTailType)
            {
                case 1:
                    return "호영";
                case 2:
                    return "라라";
                case 3:
                    return $"렌(여,{detail})";
                case 4:
                    return $"렌(남,{detail})";
                default:
                    return null;
            }
        }

        private byte GetEventJob()
        {
            return (byte)GetValue("eventJob");
        }

        private string GetEventJobString()
        {
            switch (this.EventJob)
            {
                case 1:
                    return "핑크빈";
                case 2:
                    return "예티";
                case 3:
                    return "카마도 탄지로";
                case 4:
                    return "사이타마";
                default:
                    return null;
            }
        }

        private byte GetWeaponMotionType()
        {
            return (byte)GetValue("weaponMotionType");
        }

        private string GetWeaponMotionTypeString()
        {
            switch (this.WeaponMotionType)
            {
                case 1:
                    return "한손 무기 모션";
                case 2:
                    return "두손 무기 모션";
                case 3:
                    return "건 무기 모션";
                default:
                    return "기본 무기 모션";
            }
        }

        private string GetMixHairRatio()
        {
            return GetValue("mixHairRatio").ToString().PadLeft(2, '0');
        }

        private string GetMixHairColor()
        {
            return GetValue("mixHairColor").ToString();
        }

        private string GetMixFaceRatio()
        {
            return GetValue("mixFaceInfo").ToString().PadLeft(3, '0').Substring(1, 2);
        }

        private string GetMixFaceColor()
        {
            return GetValue("mixFaceInfo").ToString().PadLeft(3, '0').Substring(0, 1);
        }

        private int GetShowEffectFlags()
        {
            return GetValue("showEffectFlags");
        }

        private PrismInfo GetPrismInfo(string type, string index = "")
        {
            var ret = new PrismInfo();
            if (GetValue($"has{type}Prism") == 1)
            {
                ret.Type = (byte)GetValue($"{type.ToLower()}Prism{index}Type");
                ret.ColorType = (byte)GetValue($"{type.ToLower()}Prism{index}ColorType");
                ret.Brightness = GetValue($"{type.ToLower()}Prism{index}Brightness");
                ret.Saturation = GetValue($"{type.ToLower()}Prism{index}Saturation");
                ret.Hue = GetValue($"{type.ToLower()}Prism{index}Hue");
                ret.ConvertPureBlack = GetValue($"{type.ToLower()}Prism{index}ConvertPureBlack") == 1;
                ret.Valid = true;
            }
            else
            {
                ret.Valid = false;
            }
            return ret;
        }

        private PrismInfoCollection GetPrismInfoCollection(string type)
        {
            var ret = new PrismInfoCollection();
            ret.Prism1 = GetPrismInfo(type);
            ret.Prism2 = GetPrismInfo(type, "2");
            return ret;
        }

        private Point GetCustomOrigin(int index)
        {
            short x = unchecked((short)(GetValue($"customOrigin{index}X") & 0xFFFF));
            short y = unchecked((short)(GetValue($"customOrigin{index}Y") & 0xFFFF));
            return new Point(x, y);
        }

        private void SetCustomOrigins()
        {
            const int count = 2;
            CustomOrigin = new CustomOriginInfo[count];

            for (int i = 0; i < count; i++)
            {
                CustomOriginInfo item = new CustomOriginInfo();
                item.Valid = GetValue($"customOrigin{i}") == 1;
                if (item.Valid) item.Origin = GetCustomOrigin(i);
                CustomOrigin[i] = item;
            }
        }

        public void SetProperties()
        {
            Gender = GetGender();

            Skin = GetSkin();
            Face = GetFace();
            Hair = GetHair();

            Cap = GetCap();
            FaceAcc = GetFaceAcc();
            EyeAcc = GetEyeAcc();
            EarAcc = GetEarAcc();
            Coat = GetCoat();
            Pants = GetPants();
            Shoes = GetShoes();
            Gloves = GetGloves();
            Cape = GetCape();
            Shield = GetShield();
            CashWeapon = GetCashWeapon();
            Weapon = GetWeapon();
            EmotionFaceAcc = GetEmotionFaceAcc();

            Ring1 = GetRing(1);
            Ring2 = GetRing(2);
            Ring3 = GetRing(3);
            Ring4 = GetRing(4);

            EarType = GetEarType();
            JobWingTailType = GetJobWingTailType();
            JobWingTailTypeDetail = GetJobWingTailTypeDetail();
            EventJob = GetEventJob();
            WeaponMotionType = GetWeaponMotionType();

            MixHairRatio = GetMixHairRatio();
            MixHairColor = GetMixHairColor();
            MixFaceRatio = GetMixFaceRatio();
            MixFaceColor = GetMixFaceColor();

            ShowEffectFlags = GetShowEffectFlags();

            CapPrismInfo = GetPrismInfoCollection("Cap");
            FaceAccPrismInfo = GetPrismInfoCollection("FaceAcc");
            EyeAccPrismInfo = GetPrismInfoCollection("EyeAcc");
            EarAccPrismInfo = GetPrismInfoCollection("EarAcc");
            CoatPrismInfo = GetPrismInfoCollection("Coat");
            PantsPrismInfo = GetPrismInfoCollection("Pants");
            ShoesPrismInfo = GetPrismInfoCollection("Shoes");
            GlovesPrismInfo = GetPrismInfoCollection("Gloves");
            CapePrismInfo = GetPrismInfoCollection("Cape");
            ShieldPrismInfo = GetPrismInfoCollection("Shield");
            WeaponPrismInfo = GetPrismInfoCollection("Weapon");
            SkinPrismInfo = GetPrismInfo("Skin");

            SetCustomOrigins();
        }

        public int Gender { get; private set; }
        public string Skin { get; private set; }
        public string Face { get; private set; }
        public string Hair { get; private set; }
        public string Cap { get; private set; }
        public string FaceAcc { get; private set; }
        public string EyeAcc { get; private set; }
        public string EarAcc { get; private set; }
        public string Coat { get; private set; }
        public string Pants { get; private set; }
        public string Shoes { get; private set; }
        public string Gloves { get; private set; }
        public string Cape { get; private set; }
        public string Shield { get; private set; }
        public string CashWeapon { get; private set; }
        public string Weapon { get; private set; }
        public string Ring1 { get; private set; }
        public string Ring2 { get; private set; }
        public string Ring3 { get; private set; }
        public string Ring4 { get; private set; }
        public string EmotionFaceAcc { get; private set; }
        public byte EarType { get; private set; }
        public byte JobWingTailType { get; private set; }
        public byte JobWingTailTypeDetail { get; private set; }
        public string JobWingTailTypeString { get { return this.GetJobWingTailTypeString(); } }
        public byte EventJob { get; private set; }
        public string EventJobString { get { return this.GetEventJobString(); } }
        public byte WeaponMotionType { get; private set; }
        public string WeaponMotionTypeString { get { return this.GetWeaponMotionTypeString(); } }
        public string MixHairRatio { get; private set; }
        public string MixHairColor { get; private set; }
        public string MixFaceRatio { get; private set; }
        public string MixFaceColor { get; private set; }
        public int ShowEffectFlags { get; private set; }
        public bool ShowWeaponEffect { get { return (ShowEffectFlags & 1) != 0; } }
        public bool ShowWeaponJumpEffect { get { return (ShowEffectFlags & (1 << 1)) != 0; } }
        public bool ShowWeaponSpecialEffect { get { return (ShowEffectFlags & (1 << 2)) != 0; } }
        public bool ShowCapeEffect { get { return (ShowEffectFlags & (1 << 3)) != 0; } }
        public PrismInfoCollection CapPrismInfo { get; private set; }
        public PrismInfoCollection FaceAccPrismInfo { get; private set; }
        public PrismInfoCollection EyeAccPrismInfo { get; private set; }
        public PrismInfoCollection EarAccPrismInfo { get; private set; }
        public PrismInfoCollection CoatPrismInfo { get; private set; }
        public PrismInfoCollection PantsPrismInfo { get; private set; }
        public PrismInfoCollection ShoesPrismInfo { get; private set; }
        public PrismInfoCollection GlovesPrismInfo { get; private set; }
        public PrismInfoCollection ShieldPrismInfo { get; private set; }
        public PrismInfoCollection CapePrismInfo { get; private set; }
        public PrismInfoCollection WeaponPrismInfo { get; private set; }
        public PrismInfo SkinPrismInfo { get; private set; }
        public CustomOriginInfo[] CustomOrigin { get; private set; }
    }

    public class PrismInfoCollection
    {
        public PrismInfo Prism1 { get; set; }
        public PrismInfo Prism2 { get; set; }
    }

    public class PrismInfo
    {
        public bool Valid { get; set; }
        /// <summary>
        /// 프리즘 유형
        /// </summary>
        public byte Type { get; set; }
        /// <summary>
        /// 프리즘 색상 계열
        /// </summary>
        public byte ColorType { get; set; }
        public string ColorTypeString { get { return this.GetColorType(); } }
        public int Hue { get; set; }
        public int Saturation { get; set; }
        public int Brightness { get; set; }
        /// <summary>
        /// 순수한 검은색도 명도 조절에 포함
        /// </summary>
        public bool ConvertPureBlack { get; set; }

        public bool HasValues()
        {
            return this.Valid;
        }

        public string GetColorType()
        {
            if (!this.Valid) return null;

            switch (ColorType)
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

    public class CustomOriginInfo
    {
        public bool Valid { get; set; }
        public Point Origin { get; set; }
    }
}
#endif