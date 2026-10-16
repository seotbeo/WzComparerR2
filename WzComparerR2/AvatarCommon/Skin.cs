using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using WzComparerR2.WzLib;

namespace WzComparerR2.AvatarCommon
{
    public class Skin
    {
        public string Name { get; set; }
        public BitmapOrigin Image { get; set; }
        public Point Offset { get; set; }
        public string Z { get; set; }
        public int ZIndex { get; set; }
        public bool Flip { get; set; }
    }
}
