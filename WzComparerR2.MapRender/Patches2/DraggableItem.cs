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

        public bool MouseHovering { get; set; }
        public bool ShowRect { get; set; } = true;
        public bool CanResize { get; set; } = false;
        public bool SnapOnFoothold { get; set; } = true;
        public bool FlipX { get; set; } = false;
        public int ResizeAreaIn { get; set; } = 15;
        public int ResizeAreaOut { get; set; } = 10;
        public int RectColorType { get; set; } = 1;
        public int X
        {
            get { return this.Position.X; }
            set
            {
                var pos = this.Position;
                pos.X = value;
                this.Position = pos;
            }
        }
        public int Y
        {
            get { return this.Position.Y; }
            set
            {
                var pos = this.Position;
                pos.Y = value;
                this.Position = pos;
            }
        }
        public int RenderY
        {
            get { return this.SnapY ?? this.Y; }
        }
        public int? SnapY { get; set; } = null;
        public Rectangle Rect { get; set; } = new Rectangle();
        public Point Position { get; set; } = Point.Zero;
        public Point MinRectSize { get; set; } = Point.Zero;
        /// <summary>
        /// 기본 사각형 색상
        /// </summary>
        public Color RectAreaColorNormal { get; set; }
        /// <summary>
        /// 기본 사각형 색상
        /// </summary>
        public Color RectAreaColorNormalMouseHover { get; set; }
        /// <summary>
        /// 삭제 사각형 색상
        /// </summary>
        public Color RectAreaColorDelete { get; set; }
        /// <summary>
        /// 기본 사각형 색상
        /// </summary>
        public Color RectAreaColorDeleteMouseHover { get; set; }
        public DraggableItemClickedPos ClickedPos { get; set; } = DraggableItemClickedPos.None;

        public Color GetRectAreaColor(int index)
        {
            if (this.MouseHovering)
            {
                switch (index)
                {
                    case 1:
                        return RectAreaColorNormalMouseHover;
                    case 2:
                        return RectAreaColorDeleteMouseHover;
                    default:
                        return Color.White;
                }
            }
            else
            {
                switch (index)
                {
                    case 1:
                        return RectAreaColorNormal;
                    case 2:
                        return RectAreaColorDelete;
                    default:
                        return Color.White;
                }
            }
        }
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
