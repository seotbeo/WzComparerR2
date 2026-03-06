using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using WzComparerR2.WzLib;

namespace WzComparerR2.MapRender.Patches2
{
    public class FootholdItem : SceneItem
    {
        public int ID { get; set; }
        public int X1 { get; set; }
        public int Y1 { get; set; }
        public int X2 { get; set; }
        public int Y2 { get; set; }
        public int Prev { get; set; }
        public int Next { get; set; }
        public int Piece { get; set; }

        #region for FootholdManager
        public bool IsWall => this.Vertical || this.Reversed;
        public bool Vertical { get; set; }
        public bool Flat { get; set; }
        public bool Reversed { get; set; }
        public Rectangle FootholdArea { get; set; }
        public int GroupIndex { get; set; }
        public int LayerLevel { get; set; }
        #endregion

        public static FootholdItem LoadFromNode(Wz_Node node)
        {
            var item = new FootholdItem()
            {
                X1 = node.Nodes["x1"].GetValueEx(0),
                Y1 = node.Nodes["y1"].GetValueEx(0),
                X2 = node.Nodes["x2"].GetValueEx(0),
                Y2 = node.Nodes["y2"].GetValueEx(0),
                Prev = node.Nodes["prev"].GetValueEx(0),
                Next = node.Nodes["next"].GetValueEx(0),
                Piece = node.Nodes["piece"].GetValueEx(0),
            };
            item.Vertical = item.X1 == item.X2;
            item.Flat = item.Y1 == item.Y2;
            item.Reversed = item.X1 > item.X2;
            item.FootholdArea = new Rectangle(Math.Min(item.X1, item.X2), Math.Min(item.Y1, item.Y2), Math.Abs(item.X2 - item.X1), Math.Abs(item.Y2 - item.Y1));
            return item;
        }
    }
}
