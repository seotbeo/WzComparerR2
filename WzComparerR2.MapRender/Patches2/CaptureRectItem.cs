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
            this.CanResize = true;
            this.MinSize = new Microsoft.Xna.Framework.Vector2(25, 25);
        }
    }
}
