using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace WzComparerR2.Config
{
    public class CharaSimQuestConfig : ConfigurationElement
    {
        [ConfigurationProperty("showID", DefaultValue = true)]
        public bool ShowID
        {
            get { return (bool)this["showID"]; }
            set { this["showID"] = value; }
        }

        [ConfigurationProperty("defaultState", DefaultValue = 1)]
        public int DefaultState
        {
            get { return Math.Min(Math.Max((int)this["defaultState"], 0), 2); }
            set { this["defaultState"] = Math.Min(Math.Max(value, 0), 2); }
        }
    }
}
