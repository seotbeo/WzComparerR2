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
        public OverlayOptions()
        {
            this.Vertices = new List<Point>();
        }

        public int AniOffset { get; set; }
        public int AniStartIndex {  get; set; }
        public int AniEndIndex { get; set; }
        public int AniStartTime { get; set; }
        public int AniEndTime { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }

        public int PngDelay { get; set; }

        public bool FullMove { get; set; }
        public int SpeedX { get; set; }
        public int SpeedY { get; set; }
        public int GoX { get; set; }
        public int GoY { get; set; }

        public bool FlipX { get; set; }
        public bool FlipY { get; set; }
        public int Angle { get; set; }

        public bool RectAutoArea { get; set; }
        public Point RectLT { get; set; }
        public Point RectRB { get; set; }

        public OverlayShapeType ShapeType { get; set; }
        public int RectRadius { get; set; }

        public Color Color { get; set; }

        public int Alpha { get; set; }
        public bool AlphaGradation { get; set; }
        public int AlphaDst { get; set; }
        public int AlphaStart { get; set; }
        public int AlphaEnd { get; set; }

        public List<Point> Vertices { get; set; }
    }

    public enum OverlayShapeType
    {
        Rectangle = 0,
        Circle = 1,
        Polygon = 2,
    }
}
