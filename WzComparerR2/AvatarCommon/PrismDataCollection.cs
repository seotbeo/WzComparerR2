using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.AvatarCommon
{
    public class PrismDataCollection
    {
        public PrismDataCollection()
        {
            this.PrismData_Default = new PrismData();
            this.PrismData_WeaponEffect = new PrismData();
        }

        public PrismDataCollection(int type, int hue, int saturation, int brightness)
        {
            this.PrismData_Default = new PrismData(type, hue, saturation, brightness);
        }

        private PrismData PrismData_Default;
        private PrismData PrismData_WeaponEffect;

        public bool Valid
        {
            get { return this.PrismData_Default.Valid || this.PrismData_WeaponEffect.Valid; }
        }

        public bool IsValid(PrismDataType datatype)
        {
            switch (datatype)
            {
                case PrismDataType.Default:
                    return this.PrismData_Default.Valid;

                case PrismDataType.WeaponEffect:
                    return this.PrismData_WeaponEffect.Valid;

                default:
                    return false;
            }
        }

        public void Clear()
        {
            foreach (PrismDataType datatype in Enum.GetValues(typeof(PrismDataType)))
            {
                this.Clear(datatype);
            }
        }

        public void Clear(PrismDataType datatype)
        {
            switch (datatype)
            {
                case PrismDataType.Default:
                    this.PrismData_Default.Clear();
                    break;

                case PrismDataType.WeaponEffect:
                    this.PrismData_WeaponEffect.Clear();
                    break;
            }
        }

        public void Set(PrismDataType datatype, int type, int hue, int saturation, int brightness)
        {
            switch (datatype)
            {
                case PrismDataType.Default:
                    this.PrismData_Default.Set(type, hue, saturation, brightness);
                    break;

                case PrismDataType.WeaponEffect:
                    this.PrismData_WeaponEffect.Set(type, hue, saturation, brightness);
                    break;
            }
        }

        public PrismData Get(PrismDataType datatype)
        {
            switch (datatype)
            {
                default:
                    return this.PrismData_Default;

                case PrismDataType.WeaponEffect:
                    return this.PrismData_WeaponEffect;
            }
        }

        public PrismDataCollection Clone()
        {
            var ret = new PrismDataCollection();
            ret.PrismData_Default = new PrismData(this.PrismData_Default.Type, this.PrismData_Default.Hue, this.PrismData_Default.Saturation, this.PrismData_Default.Brightness);
            ret.PrismData_WeaponEffect = new PrismData(this.PrismData_WeaponEffect.Type, this.PrismData_WeaponEffect.Hue, this.PrismData_WeaponEffect.Saturation, this.PrismData_WeaponEffect.Brightness);

            return ret;
        }

        public string GetColorType(PrismDataType datatype)
        {
            switch (datatype)
            {
                case PrismDataType.Default:
                    return this.PrismData_Default.GetColorType();

                case PrismDataType.WeaponEffect:
                    return this.PrismData_WeaponEffect.GetColorType();

                default:
                    return null;
            }
        }

        public enum PrismDataType
        {
            Default = 0,
            WeaponEffect = 1,
        }
    }
}
