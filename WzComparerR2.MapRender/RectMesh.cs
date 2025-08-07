using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender
{
    class RectMesh
    {
        public RectMesh(Rectangle rect, Color color)
            : this(rect, color, 1)
        {
        }

        public RectMesh(Rectangle rect, Color color, int thickness, double alpha = 0.5)
        {
            this.Rect = rect;
            this.Color = color;
            this.FillColor = new Color(color, (int)(color.A * alpha));
            this.Thickness = thickness;
        }

        public Rectangle Rect { get; set; }
        public int Thickness { get; set; } = 1;
        public Color Color { get; set; }
        public Color FillColor { get; set; }
    }
}
