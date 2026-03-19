using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NET6_0_OR_GREATER
namespace WzComparerR2.OpenAPI
{
    static class AvatarCodeStructure
    {
        public static readonly Dictionary<int, IReadOnlyList<DataInfo>> Structure = new Dictionary<int, IReadOnlyList<DataInfo>>
        {
            // not fully decoded yet
            { 26, GetStructureV26() },
            { 27, GetStructureV27() },
            { 28, GetStructureV28() },
            { 29, GetStructureV29() },
            { 30, GetStructureV30() },
            { 31, GetStructureV31() },
            { 32, GetStructureV32() },
            { 33, GetStructureV33() },
            { 34, GetStructureV34() },
            { 35, GetStructureV35() },
            { 36, GetStructureV36() },
            { 39, GetStructureV39() },
            { 40, GetStructureV40() },
        };

        private static List<DataInfo> GetBasePartV1()
        {
            return new List<DataInfo>()
            {
                new DataInfo("gender", 1),
                new DataInfo("skinID", 10),
                new DataInfo("face50k", 1),
                new DataInfo("faceID", 10),
                new DataInfo("faceGender", 4),
                new DataInfo("hair10k", 4),
                new DataInfo("hairID", 10),
                new DataInfo("hairGender", 4),
                new DataInfo("capID", 10),
                new DataInfo("capGender", 3),
                new DataInfo("faceAccID", 10),
                new DataInfo("faceAccGender", 2),
                new DataInfo("eyeAccID", 10),
                new DataInfo("eyeAccGender", 2),
                new DataInfo("earAccID", 10),
                new DataInfo("earAccGender", 2),
                new DataInfo("isLongCoat", 1),
                new DataInfo("coatID", 10),
                new DataInfo("coatGender", 4),
                new DataInfo("pantsID", 10),
                new DataInfo("pantsGender", 2),
                new DataInfo("shoesID", 10),
                new DataInfo("shoesGender", 2),
                new DataInfo("glovesID", 10),
                new DataInfo("glovesGender", 2),
                new DataInfo("capeID", 10),
                new DataInfo("capeGender", 2)
            };
        }

        private static List<DataInfo> GetBasePartV2()
        {
            return new List<DataInfo>()
            {
                new DataInfo("gender", 1),
                new DataInfo("skinID", 10),
                new DataInfo("face50k", 1),
                new DataInfo("faceID", 10),
                new DataInfo("faceGender", 4),
                new DataInfo("hair10k", 4),
                new DataInfo("hairID", 10),
                new DataInfo("hairGender", 4),
                new DataInfo("capID", 10),
                new DataInfo("capGender", 3),
                new DataInfo("faceAccID", 10),
                new DataInfo("faceAccGender", 2),
                new DataInfo("eyeAccID", 10),
                new DataInfo("eyeAccGender", 2),
                new DataInfo("earAccID", 10),
                new DataInfo("earAccGender", 2),
                new DataInfo("isLongCoat", 1),
                new DataInfo("coatID", 10),
                new DataInfo("coatGender", 4),
                new DataInfo("pantsID", 10),
                new DataInfo("pantsGender", 2),
                new DataInfo("shoesID", 10),
                new DataInfo("shoesGender", 4),
                new DataInfo("glovesID", 10),
                new DataInfo("glovesGender", 2),
                new DataInfo("capeID", 10),
                new DataInfo("capeGender", 2),
            };
        }

        private static List<DataInfo> GetBasePartV3()
        {
            return new List<DataInfo>()
            {
                new DataInfo("gender", 1),
                new DataInfo("skinID", 10),
                new DataInfo("face50k", 1),
                new DataInfo("faceID", 10),
                new DataInfo("faceGender", 4),
                new DataInfo("hair10k", 4),
                new DataInfo("hairID", 10),
                new DataInfo("hairGender", 4),
                new DataInfo("capID", 10),
                new DataInfo("capGender", 3),
                new DataInfo("faceAccID", 10),
                new DataInfo("faceAccGender", 2),
                new DataInfo("eyeAccID", 10),
                new DataInfo("eyeAccGender", 2),
                new DataInfo("earAccID", 10),
                new DataInfo("earAccGender", 2),
                new DataInfo("isLongCoat", 1),
                new DataInfo("coatID", 10),
                new DataInfo("coatGender", 4),
                new DataInfo("pantsID", 10),
                new DataInfo("pantsGender", 2),
                new DataInfo("shoesID", 10),
                new DataInfo("shoesGender", 4),
                new DataInfo("glovesID", 10),
                new DataInfo("glovesGender", 2),
                new DataInfo("capeID", 10),
                new DataInfo("capeGender", 4),
            };
        }

        private static List<DataInfo> GetPrismPartV1()
        {
            return new List<DataInfo>()
            {
                new DataInfo("hasCapPrism", 1),
                new DataInfo("hasCoatPrism", 1),
                new DataInfo("hasPantsPrism", 1),
                new DataInfo("hasShoesPrism", 1),
                new DataInfo("hasGlovesPrism", 1),
                new DataInfo("hasCapePrism", 1),
                new DataInfo("hasWeaponPrism", 1),
                new DataInfo("hasSkinPrism", 1)
            };
        }

        private static List<DataInfo> GetPrismPartV2()
        {
            return new List<DataInfo>()
            {
                new DataInfo("hasCapPrism", 1),
                new DataInfo("hasFaceAccPrism", 1),
                new DataInfo("hasEyeAccPrism", 1),
                new DataInfo("hasEarAccPrism", 1),
                new DataInfo("hasCoatPrism", 1),
                new DataInfo("hasPantsPrism", 1),
                new DataInfo("hasShoesPrism", 1),
                new DataInfo("hasGlovesPrism", 1),
                new DataInfo("hasCapePrism", 1),
                new DataInfo("hasShieldPrism", 1),
                new DataInfo("hasWeaponPrism", 1),
                new DataInfo("hasSkinPrism", 1),
            };
        }

        private static List<DataInfo> GetRingPartV1()
        {
            return new List<DataInfo>()
            {
                new DataInfo("ringID1", 10),
                new DataInfo("ringGender1", 4),
                new DataInfo("ringID2", 10),
                new DataInfo("ringGender2", 4),
                new DataInfo("ringID3", 10),
                new DataInfo("ringGender3", 4),
                new DataInfo("ringID4", 10),
                new DataInfo("ringGender4", 4),
            };
        }

        private static List<DataInfo> GetStructureV26()
        {
            var ret = new List<DataInfo>();
            ret.AddRange(GetBasePartV1());
            ret.AddRange(new List<DataInfo>()
            {
                new DataInfo("subWeaponType", 2),
                new DataInfo("shieldID", 10),
                new DataInfo("shieldGender", 4),
                new DataInfo("isCashWeapon", 1),
                new DataInfo("weaponID", 10),
                new DataInfo("weaponGender", 2),
                new DataInfo("weaponType", 8),
                new DataInfo("earType", 4),
                new DataInfo("mixHairColor", 4),
                new DataInfo("mixHairRatio", 8),
                new DataInfo("mixFaceInfo", 10),
                new DataInfo("unknown1", 2),
                new DataInfo("jobWingTailType", 2),
                new DataInfo("unknown2", 6),
                new DataInfo("eventJob", 3),
                new DataInfo("unknown2_2", 21),
                new DataInfo("weaponMotionType", 2),
                new DataInfo("unknown3", 11)
            });
            return ret;
        }

        private static List<DataInfo> GetStructureV27()
        {
            var ret = new List<DataInfo>();
            ret.AddRange(GetBasePartV1());
            ret.AddRange(new List<DataInfo>()
            {
                new DataInfo("subWeaponType", 2),
                new DataInfo("shieldID", 10),
                new DataInfo("shieldGender", 4),
                new DataInfo("isCashWeapon", 1),
                new DataInfo("weaponID", 10),
                new DataInfo("weaponGender", 2),
                new DataInfo("weaponType", 8),
                new DataInfo("earType", 4),
                new DataInfo("mixHairColor", 4),
                new DataInfo("mixHairRatio", 8),
                new DataInfo("mixFaceInfo", 10),
                new DataInfo("unknown1", 2),
                new DataInfo("jobWingTailType", 2),
                new DataInfo("unknown2", 6),
                new DataInfo("eventJob", 3),
                new DataInfo("unknown2_2", 21),
                new DataInfo("weaponMotionType", 2),
                new DataInfo("unknown3", 11),
            });
            ret.AddRange(GetPrismPartV1());
            return ret;
        }

        private static List<DataInfo> GetStructureV28()
        {
            var ret = new List<DataInfo>();
            ret.AddRange(GetBasePartV1());
            ret.AddRange(new List<DataInfo>()
            {
                new DataInfo("subWeaponType", 2),
                new DataInfo("shieldID", 10),
                new DataInfo("shieldGender", 4),
                new DataInfo("isCashWeapon", 1),
                new DataInfo("weaponID", 10),
                new DataInfo("weaponGender", 2),
                new DataInfo("weaponType", 8),
                new DataInfo("earType", 4),
                new DataInfo("mixHairColor", 4),
                new DataInfo("mixHairRatio", 8),
                new DataInfo("mixFaceInfo", 10),
                new DataInfo("unknown1", 2),
                new DataInfo("jobWingTailType", 2),
                new DataInfo("unknown2", 6),
                new DataInfo("eventJob", 3),
                new DataInfo("unknown2_2", 21),
                new DataInfo("weaponMotionType", 2),
                new DataInfo("unknown3", 11),
                new DataInfo("hasCapPrism", 1),
                new DataInfo("hasCoatPrism", 1),
                new DataInfo("hasPantsPrism", 1),
                new DataInfo("hasShoesPrism", 1),
                new DataInfo("hasGlovesPrism", 1),
                new DataInfo("hasCapePrism", 1),
                new DataInfo("hasWeaponPrism", 1),
                new DataInfo("hasSkinPrism", 1),
            });
            ret.AddRange(GetPrismPartV1());
            ret.AddRange(GetRingPartV1());
            return ret;
        }

        private static List<DataInfo> GetStructureV29()
        {
            var ret = new List<DataInfo>();
            ret.AddRange(GetBasePartV2());
            ret.AddRange(new List<DataInfo>()
            {
                new DataInfo("subWeaponType", 2),
                new DataInfo("shieldID", 10),
                new DataInfo("shieldGender", 4),
                new DataInfo("isCashWeapon", 1),
                new DataInfo("weaponID", 10),
                new DataInfo("weaponGender", 2),
                new DataInfo("weaponType", 8),
                new DataInfo("earType", 4),
                new DataInfo("mixHairColor", 4),
                new DataInfo("mixHairRatio", 8),
                new DataInfo("mixFaceInfo", 10),
                new DataInfo("unknown1", 2),
                new DataInfo("jobWingTailType", 2),
                new DataInfo("unknown2", 6),
                new DataInfo("eventJob", 3),
                new DataInfo("unknown2_2", 21),
                new DataInfo("weaponMotionType", 2),
                new DataInfo("unknown3", 11),
            });
            ret.AddRange(GetPrismPartV1());
            ret.AddRange(GetRingPartV1());
            return ret;
        }

        private static List<DataInfo> GetStructureV30()
        {
            var ret = new List<DataInfo>();
            ret.AddRange(GetBasePartV2());
            ret.AddRange(new List<DataInfo>()
            {
                new DataInfo("subWeaponType", 2),
                new DataInfo("shieldID", 10),
                new DataInfo("shieldGender", 4),
                new DataInfo("isCashWeapon", 1),
                new DataInfo("weaponID", 10),
                new DataInfo("weaponGender", 2),
                new DataInfo("weaponType", 8),
                new DataInfo("earType", 4),
                new DataInfo("mixHairColor", 4),
                new DataInfo("mixHairRatio", 8),
                new DataInfo("mixFaceInfo", 10),
                new DataInfo("unknown1", 4),
                new DataInfo("jobWingTailType", 8),
                new DataInfo("jobWingTailTypeDetail", 2),
                new DataInfo("unknown2", 6),
                new DataInfo("eventJob", 3),
                new DataInfo("unknown2_2", 21),
                new DataInfo("weaponMotionType", 2),
                new DataInfo("unknown3", 11),
            });
            ret.AddRange(GetPrismPartV1());
            ret.AddRange(GetRingPartV1());
            return ret;
        }

        private static List<DataInfo> GetStructureV31()
        {
            var ret = new List<DataInfo>();
            ret.AddRange(GetBasePartV2());
            ret.AddRange(new List<DataInfo>()
            {
                new DataInfo("subWeaponType", 2),
                new DataInfo("shieldID", 10),
                new DataInfo("shieldGender", 4),
                new DataInfo("isCashWeapon", 1),
                new DataInfo("weaponID", 10),
                new DataInfo("weaponGender", 2),
                new DataInfo("weaponType", 8),
                new DataInfo("earType", 4),
                new DataInfo("mixHairColor", 4),
                new DataInfo("mixHairRatio", 8),
                new DataInfo("mixFaceInfo", 10),
                new DataInfo("unknown1", 4),
                new DataInfo("jobWingTailType", 8),
                new DataInfo("jobWingTailTypeDetail", 2),
                new DataInfo("unknown2", 6),
                new DataInfo("eventJob", 3),
                new DataInfo("unknown2_2", 21),
                new DataInfo("weaponMotionType", 2),
                new DataInfo("unknown3", 11),
            });
            ret.AddRange(GetPrismPartV2());
            ret.AddRange(GetRingPartV1());
            return ret;
        }

        private static List<DataInfo> GetStructureV32()
        {
            var ret = new List<DataInfo>();
            ret.AddRange(GetBasePartV2());
            ret.AddRange(new List<DataInfo>()
            {
                new DataInfo("subWeaponType", 2),
                new DataInfo("shieldID", 10),
                new DataInfo("shieldGender", 4),
                new DataInfo("isCashWeapon", 1),
                new DataInfo("weaponID", 10),
                new DataInfo("weaponGender", 2),
                new DataInfo("weaponType", 8),
                new DataInfo("earType", 4),
                new DataInfo("mixHairColor", 4),
                new DataInfo("mixHairRatio", 8),
                new DataInfo("mixFaceInfo", 10),
                new DataInfo("unknown1", 4),
                new DataInfo("jobWingTailType", 8),
                new DataInfo("jobWingTailTypeDetail", 2),
                new DataInfo("unknown2", 6),
                new DataInfo("eventJob", 3),
                new DataInfo("unknown2_2", 21),
                new DataInfo("weaponMotionType", 2),
                new DataInfo("unknown3", 11),
                new DataInfo("showEffectFlags", 4),
            });
            ret.AddRange(GetPrismPartV2());
            ret.AddRange(GetRingPartV1());
            return ret;
        }

        private static List<DataInfo> GetStructureV33()
        {
            var ret = new List<DataInfo>();
            ret.AddRange(GetBasePartV2());
            ret.AddRange(new List<DataInfo>()
            {
                new DataInfo("subWeaponType", 2),
                new DataInfo("shieldID", 10),
                new DataInfo("shieldGender", 4),
                new DataInfo("isCashWeapon", 1),
                new DataInfo("weaponID", 10),
                new DataInfo("weaponGender", 2),
                new DataInfo("weaponType", 8),
                new DataInfo("earType", 4),
                new DataInfo("mixHairColor", 4),
                new DataInfo("mixHairRatio", 8),
                new DataInfo("mixFaceInfo", 10),
                new DataInfo("unknown1", 4),
                new DataInfo("jobWingTailType", 8),
                new DataInfo("jobWingTailTypeDetail", 2),
                new DataInfo("unknown2", 6),
                new DataInfo("eventJob", 3),
                new DataInfo("unknown2_2", 21),
                new DataInfo("weaponMotionType", 2),
                new DataInfo("unknown3", 11),
                new DataInfo("showEffectFlags", 4),
            });
            ret.AddRange(GetPrismPartV2());
            ret.AddRange(GetRingPartV1());
            return ret;
        }

        private static List<DataInfo> GetStructureV34()
        {
            var ret = new List<DataInfo>();
            ret.AddRange(GetBasePartV2());
            ret.AddRange(new List<DataInfo>()
            {
                new DataInfo("subWeaponType", 2),
                new DataInfo("shieldID", 10),
                new DataInfo("shieldGender", 4),
                new DataInfo("isCashWeapon", 1),
                new DataInfo("weaponID", 10),
                new DataInfo("weaponGender", 2),
                new DataInfo("weaponType", 8),
                new DataInfo("earType", 4),
                new DataInfo("mixHairColor", 4),
                new DataInfo("mixHairRatio", 8),
                new DataInfo("mixFaceInfo", 10),
                new DataInfo("unknown1", 4),
                new DataInfo("jobWingTailType", 8),
                new DataInfo("jobWingTailTypeDetail", 2),
                new DataInfo("unknown2", 6),
                new DataInfo("eventJob", 3),
                new DataInfo("unknown2_2", 21),
                new DataInfo("weaponMotionType", 2),
                new DataInfo("unknown3", 11),
                new DataInfo("showEffectFlags", 4),
                new DataInfo("emotionFaceAccID", 10),
                new DataInfo("emotionFaceAccGender", 2),
            });
            ret.AddRange(GetPrismPartV2());
            ret.AddRange(GetRingPartV1());
            return ret;
        }

        private static List<DataInfo> GetStructureV35()
        {
            var ret = new List<DataInfo>();
            ret.AddRange(GetBasePartV2());
            ret.AddRange(new List<DataInfo>()
            {
                new DataInfo("subWeaponType", 2),
                new DataInfo("shieldID", 10),
                new DataInfo("shieldGender", 4),
                new DataInfo("isCashWeapon", 1),
                new DataInfo("weaponID", 10),
                new DataInfo("weaponGender", 2),
                new DataInfo("weaponType", 8),
                new DataInfo("earType", 4),
                new DataInfo("mixHairColor", 4),
                new DataInfo("mixHairRatio", 8),
                new DataInfo("mixFaceInfo", 10),
                new DataInfo("unknown1", 4),
                new DataInfo("jobWingTailType", 8),
                new DataInfo("jobWingTailTypeDetail", 2),
                new DataInfo("unknown2", 6),
                new DataInfo("eventJob", 3),
                new DataInfo("unknown2_2", 21),
                new DataInfo("weaponMotionType", 2),
                new DataInfo("unknown3", 11),
                new DataInfo("showEffectFlags", 4),
                new DataInfo("emotionFaceAccID", 10),
                new DataInfo("emotionFaceAccGender", 2),
            });
            ret.AddRange(GetPrismPartV2());
            ret.AddRange(GetRingPartV1());
            return ret;
        }

        private static List<DataInfo> GetStructureV36()
        {
            var ret = new List<DataInfo>();
            ret.AddRange(GetBasePartV2());
            ret.AddRange(new List<DataInfo>()
            {
                new DataInfo("subWeaponType", 2),
                new DataInfo("shieldID", 10),
                new DataInfo("shieldGender", 4),
                new DataInfo("isCashWeapon", 1),
                new DataInfo("weaponID", 10),
                new DataInfo("weaponGender", 2),
                new DataInfo("weaponType", 8),
                new DataInfo("earType", 4),
                new DataInfo("mixHairColor", 4),
                new DataInfo("mixHairRatio", 8),
                new DataInfo("mixFaceInfo", 10),
                new DataInfo("unknown1", 4),
                new DataInfo("jobWingTailType", 8),
                new DataInfo("jobWingTailTypeDetail", 2),
                new DataInfo("unknown2", 6),
                new DataInfo("eventJob", 3),
                new DataInfo("unknown2_2", 21),
                new DataInfo("weaponMotionType", 2),
                new DataInfo("unknown3", 11),
                new DataInfo("showEffectFlags", 4),
                new DataInfo("emotionFaceAccID", 10),
                new DataInfo("emotionFaceAccGender", 2),
            });
            ret.AddRange(GetPrismPartV2());
            ret.AddRange(GetRingPartV1());
            return ret;
        }

        private static List<DataInfo> GetStructureV39()
        {
            var ret = new List<DataInfo>();
            ret.AddRange(GetBasePartV3());
            ret.AddRange(new List<DataInfo>()
            {
                new DataInfo("subWeaponType", 3),
                new DataInfo("uk2_1", 1),
                new DataInfo("isCashWeapon", 1),
                new DataInfo("weaponID", 10),
                new DataInfo("weaponGender", 2),
                new DataInfo("weaponType", 8),
                new DataInfo("earType", 4),
                new DataInfo("mixHairColor", 4),
                new DataInfo("mixHairRatio", 8),
                new DataInfo("mixFaceInfo", 10),
                new DataInfo("unknown1", 4),
                new DataInfo("jobWingTailType", 8),
                new DataInfo("jobWingTailTypeDetail", 2),
                new DataInfo("unknown2", 6),
                new DataInfo("eventJob", 3),
                new DataInfo("unknown2_2", 21),
                new DataInfo("weaponMotionType", 2),
                new DataInfo("unknown3", 11),
                new DataInfo("showEffectFlags", 4),
                new DataInfo("emotionFaceAccID", 10),
                new DataInfo("emotionFaceAccGender", 2),
            });
            ret.AddRange(GetPrismPartV2());
            ret.AddRange(GetRingPartV1());
            return ret;
        }

        private static List<DataInfo> GetStructureV40()
        {
            var ret = new List<DataInfo>();
            ret.AddRange(GetBasePartV3());
            ret.AddRange(new List<DataInfo>()
            {
                new DataInfo("subWeaponType", 3),
                new DataInfo("uk2_1", 1),
                new DataInfo("isCashWeapon", 1),
                new DataInfo("weaponID", 10),
                new DataInfo("weaponGender", 2),
                new DataInfo("weaponType", 8),
                new DataInfo("earType", 4),
                new DataInfo("mixHairColor", 4),
                new DataInfo("mixHairRatio", 8),
                new DataInfo("mixFaceInfo", 10),
                new DataInfo("unknown1", 4),
                new DataInfo("jobWingTailType", 8),
                new DataInfo("jobWingTailTypeDetail", 2),
                new DataInfo("unknown2", 6),
                new DataInfo("eventJob", 3),
                new DataInfo("unknown2_2", 21),
                new DataInfo("weaponMotionType", 2),
                new DataInfo("unknown3", 11),
                new DataInfo("showEffectFlags", 4),
                new DataInfo("unknown3_2", 3),
                new DataInfo("emotionFaceAccID", 10),
                new DataInfo("emotionFaceAccGender", 2),
            });
            ret.AddRange(GetPrismPartV2());
            ret.AddRange(GetRingPartV1());
            return ret;
        }
    }

    public class DataInfo
    {
        public DataInfo(string name, int bits)
        {
            Name = name;
            Bits = bits;
        }

        public string Name { get; set; }
        public int Bits { get; set; }
        public int Value { get; set; }

        public DataInfo Clone()
        {
            return new DataInfo(Name, Bits)
            {
                Value = Value
            };
        }
    }
}
#endif