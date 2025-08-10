using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.CharaSim
{
    public class TooltipHelp : ICloneable
    {
        public TooltipHelp(string title, string desc)
        {
            this.Title = title;
            this.Desc = desc;
        }

        public TooltipHelp(string title, string desc, bool flexibleWidth) : this(title, desc)
        {
            FlexibleWidth = flexibleWidth;
        }

        public string Title { get; set; }
        public string Desc { get; set; }
        public bool FlexibleWidth { get; set; }
        public bool Empty
        {
            get
            {
                return string.IsNullOrEmpty(this.Title) || string.IsNullOrEmpty(this.Desc);
            }
        }

        public virtual object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
