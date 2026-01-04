using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace WzComparerR2.Config
{
    public class CharaSimMiscConfig : ConfigurationElement
    {
        [ConfigurationProperty("enable22AniStyle", DefaultValue = true)]
        public bool Enable22AniStyle
        {
            get { return (bool)this["enable22AniStyle"]; }
            set { this["enable22AniStyle"] = value; }
        }

        [ConfigurationProperty("PreferredStringCopyMethod", DefaultValue = 1)]
        public int PreferredStringCopyMethod
        {
            get { return (int)this["PreferredStringCopyMethod"]; }
            set { this["PreferredStringCopyMethod"] = value; }
        }
        
        [ConfigurationProperty("enableWorldArchive", DefaultValue = true)]
        public bool EnableWorldArchive
        {
            get { return (bool)this["enableWorldArchive"]; }
            set { this["enableWorldArchive"] = value; }
        }
    }
}
