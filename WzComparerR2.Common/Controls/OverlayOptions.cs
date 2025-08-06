using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace WzComparerR2.Controls
{
    public class OverlayOptions
    {
        public int AniOffset { get; set; }
        public int AniStart {  get; set; }
        public int AniEnd { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }

        public int PngDelay { get; set; }

        public bool FullMove { get; set; }
        public int SpeedX { get; set; }
        public int SpeedY { get; set; }
        public int GoX { get; set; }
        public int GoY { get; set; }

        public bool RectAutoArea { get; set; }
        public Point RectLT { get; set; }
        public Point RectRB { get; set; }

        public int RectType { get; set; }
        public int RectRadius { get; set; }
        public int RectAlpha { get; set; }

        public bool RectGradation { get; set; }
        public int RectAlphaDst { get; set; }
        public int RectAlphaStart { get; set; }
        public int RectAlphaEnd { get; set; }
    }
}
