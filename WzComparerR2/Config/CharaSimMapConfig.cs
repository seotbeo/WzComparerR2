using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace WzComparerR2.Config
{
    public class CharaSimMapConfig : ConfigurationElement
    {
        [ConfigurationProperty("showMiniMap", DefaultValue = true)]
        public bool ShowMiniMap
        {
            get { return (bool)this["showMiniMap"]; }
            set { this["showMiniMap"] = value; }
        }
    }
}
