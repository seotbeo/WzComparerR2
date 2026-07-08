using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.WzLib;

namespace WzComparerR2.MapRender.Patches2
{
    public class SkillItem : DraggableItem
    {
        public SkillItem()
        {
            this.ShowRect = true;
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

        public static SkillItem Create(string name, int id, int x, int y, int l, int t, int r, int b, int index, bool flip, Wz_Node aniNode)
        {
            var item = new SkillItem()
            {
                Name = name,
                ID = id,
                Position = new Point(x, y),
                Index = index,
                FlipX = flip,
                AniNode = aniNode,
            };

            if (l < r && t < b)
            {
                item.Rect = new Rectangle(l, t, r - l, b - t);
            }

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
