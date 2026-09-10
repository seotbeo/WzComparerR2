using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.WzLib;

namespace WzComparerR2.MapRender.Patches2
{
    public class DraggableAniItem : DraggableItem
    {
        public DraggableAniItem()
        {
            this.ShowRect = false;
            this.CanResize = false;
            this.SnapOnFoothold = true;
            this.RectAreaColorNormal = Color.Bisque;
            this.RectAreaColorNormalMouseHover = new Color(107, 182, 255);
            this.RectAreaColorDelete = new Color(231, 76, 60);
            this.RectAreaColorDeleteMouseHover = new Color(255, 135, 125);
        }

        public int ID { get; set; }
        public Wz_Node AniNode { get; set; }

        public ItemView View { get; set; }

        public static DraggableAniItem Create(int x, int y, int index, bool flip, Wz_Node aniNode)
        {
            var item = new DraggableAniItem()
            {
                Position = new Point(x, y),
                Index = index,
                FlipX = flip,
                AniNode = aniNode,
                Rect = Rectangle.Empty
            };

            return item;
        }

        public class ItemView
        {
            /// <summary>
            /// 时间关联，单位为毫秒。
            /// </summary>
            public int Time { get; set; }

            /// <summary>
            /// 动画资源。
            /// </summary>
            public object Animator { get; set; }
        }
    }
}
