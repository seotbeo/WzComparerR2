using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace WzComparerR2.Config
{
    public class CharaSimItemConfig : ConfigurationElement
    {
        [ConfigurationProperty("showID", DefaultValue = true)]
        public bool ShowID
        {
            get { return (bool)this["showID"]; }
            set { this["showID"] = value; }
        }

        [ConfigurationProperty("linkRecipeInfo", DefaultValue = true)]
        public bool LinkRecipeInfo
        {
            get { return (bool)this["linkRecipeInfo"]; }
            set { this["linkRecipeInfo"] = value; }
        }

        [ConfigurationProperty("linkRecipeItem", DefaultValue = true)]
        public bool LinkRecipeItem
        {
            get { return (bool)this["linkRecipeItem"]; }
            set { this["linkRecipeItem"] = value; }
        }

        [ConfigurationProperty("showNickTag", DefaultValue = false)]
        public bool ShowNickTag
        {
            get { return (bool)this["showNickTag"]; }
            set { this["showNickTag"] = value; }
        }

        [ConfigurationProperty("cosmeticHairColor", DefaultValue = 0)]
        public int CosmeticHairColor
        {
            get { return (int)this["cosmeticHairColor"]; }
            set { this["cosmeticHairColor"] = value; }
        }

        [ConfigurationProperty("cosmeticFaceColor", DefaultValue = 0)]
        public int CosmeticFaceColor
        {
            get { return (int)this["cosmeticFaceColor"]; }
            set { this["cosmeticFaceColor"] = value; }
        }
    }
}
