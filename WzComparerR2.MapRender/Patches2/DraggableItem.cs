using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.MapRender.Patches2
{
    public class DraggableItem : SceneItem
    {
        public DraggableItem()
        {
        }

        public bool CanResize { get; set; } = false;
        public int ResizeAreaIn { get; set; } = 15;
        public int ResizeAreaOut { get; set; } = 10;
        public Rectangle Rect { get; set; } = new Rectangle();
        public Vector2 Origin { get; set; } = Vector2.Zero;
        public Vector2 Position { get; set; } = Vector2.Zero;
        public Vector2 MinSize { get; set; } = Vector2.Zero;

        public DraggableItemClickedPos ClickedPos { get; set; } = DraggableItemClickedPos.None;
    }

    public enum DraggableItemClickedPos
    {
        None = -1,
        Center = 0,
        TopLeft = 1,
        TopRight = 2,
        BottomRight = 3,
        BottomLeft = 4,
        Top = 5,
        Right = 6,
        Bottom = 7,
        Left = 8
    }
}
