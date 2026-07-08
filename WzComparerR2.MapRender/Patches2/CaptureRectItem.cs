using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.MapRender.Patches2
{
    public class CaptureRectItem : DraggableItem
    {
        public CaptureRectItem()
        {
            this.ShowRect = true;
            this.CanResize = true;
            this.SnapOnFoothold = false;
            this.MinRectSize = new Point(25, 25);
            this.RectAreaColorNormal = new Color(204, 204, 204);
        }
    }
}
