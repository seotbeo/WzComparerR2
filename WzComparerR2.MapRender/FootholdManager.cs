using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.MapRender.Patches2;

namespace WzComparerR2.MapRender
{
    public class FootholdManager
    {
        public List<FootholdGroup>[] FootholdGroups { get; set; } = Enumerable.Range(0, 8).Select(_ => new List<FootholdGroup>()).ToArray();
        public List<FootholdGroup> AllFootholdGroups { get; set; }
        public Dictionary<int, FootholdGroup> AllFootholdGroupsByID { get; set; }
        public Dictionary<int, FootholdItem> AllFootholdByID { get; set; } = new();
        public Rectangle Area { get; set; } = Rectangle.Empty;

        public void Build()
        {

            AllFootholdGroups = FootholdGroups.SelectMany(g => g).ToList();
            AllFootholdGroupsByID = AllFootholdGroups.ToDictionary(g => g.Index);
            AllFootholdByID.Clear();
            foreach (var fh in AllFootholdGroups.SelectMany(g => g.Footholds))
            {
                AllFootholdByID[fh.ID] = fh;
            }

            for (int i = 0; i <= 7; i++)
            {
                Dictionary<int, FootholdItem> fhRef = new();
                foreach (var fh in FootholdGroups[i].SelectMany(g => g.Footholds))
                {
                    fhRef[fh.ID] = fh;
                }
                foreach (var group in FootholdGroups[i])
                {
                    group.Build(fhRef, AllFootholdByID);
                }
            }
        }

        public void Add(FootholdGroup item, int layer)
        {
            FootholdGroups[layer].Add(item);
            if (this.Area == Rectangle.Empty)
            {
                this.Area = item.GroupArea;
            }
            else this.Area = Rectangle.Union(this.Area, item.GroupArea);
        }

        public int GetGroupIndexByFootholdIndex(int index)
        {
            if (GetFootholdByID(index, out var fh)) return GetGroupIndexByFoothold(fh);
            return -1;
        }

        public int GetGroupIndexByFoothold(FootholdItem item)
        {
            return item?.GroupIndex ?? -1;
        }

        public FootholdGroup GetGroupByIndex(int index)
        {
            AllFootholdGroupsByID.TryGetValue(index, out FootholdGroup group);
            return group;
        }

        public int GetLayerByFoothold(FootholdItem item)
        {
            return item?.LayerLevel ?? -1;
        }

        public bool GetFootholdByID(int index, out FootholdItem fh)
        {
            return AllFootholdByID.TryGetValue(index, out fh);
        }

        public bool TryGetY(FootholdGroup group, int x, out int y)
        {
            y = group.GroupArea.Bottom;
            if (x < group.GroupArea.Left || x > group.GroupArea.Right)
                return false;

            var fh = group.Footholds.FirstOrDefault(fh => fh.X1 <= x && fh.X2 >= x);
            if (fh != null)
            {
                y = GetYOnFoothold(fh, x);
                return true;
            }
            return false;
        }

        public int GetYOnFoothold(FootholdItem fh, float x)
        {
            if (!fh.Vertical)
            {
                float dx = fh.X2 - fh.X1;
                float t = (x - fh.X1) / dx;
                return (int)(fh.Y1 + (fh.Y2 - fh.Y1) * t);
            }
            return Math.Min(fh.Y1, fh.Y2);
        }

        public static bool GetCandidateGroups(FootholdGroup group, Vector2 pos1, Vector2 pos2, int margin = 20)
        {
            var target = new Rectangle((int)Math.Min(pos1.X, pos2.X) - margin, (int)Math.Min(pos1.Y, pos2.Y) - margin,
                (int)Math.Abs(pos1.X - pos2.X) + 2 * margin, (int)Math.Abs(pos1.Y - pos2.Y) + 2 * margin);
            return group.GroupArea.Intersects(target);
        }

        public static bool GetCandidateFootholds(FootholdItem item, Vector2 pos1, Vector2 pos2, int margin = 20)
        {
            var target = new Rectangle((int)Math.Min(pos1.X, pos2.X) - margin, (int)Math.Min(pos1.Y, pos2.Y) - margin,
                (int)Math.Abs(pos1.X - pos2.X) + 2 * margin, (int)Math.Abs(pos1.Y - pos2.Y) + 2 * margin);
            return item.FootholdArea.Intersects(target);
        }

        public static bool Intersects(FootholdItem item, Vector2 pos1, Vector2 pos2, bool onSegment = true)
        {
            var fpos1 = new Vector2(item.X1, item.Y1);
            var fpos2 = new Vector2(item.X2, item.Y2);
            return intersects(fpos1, fpos2, pos1, pos2, onSegment);
        }

        public static bool intersects(Vector2 a, Vector2 b, Vector2 c, Vector2 d, bool onSegment = true)
        {
            int ab_c = CCW(a, b, c);
            int ab_d = CCW(a, b, d);
            int cd_a = CCW(c, d, a);
            int cd_b = CCW(c, d, b);

            if (ab_c * ab_d < 0 && cd_a * cd_b < 0) return true;
            if (!onSegment) return false;
            if (ab_c == 0 && OnSegment(a, b, c)) return true;
            if (ab_d == 0 && OnSegment(a, b, d)) return true;
            if (cd_a == 0 && OnSegment(c, d, a)) return true;
            if (cd_b == 0 && OnSegment(c, d, b)) return true;

            return false;
        }

        private static bool OnSegment(Vector2 a, Vector2 b, Vector2 c)
        {
            return Math.Min(a.X, b.X) <= c.X && c.X <= Math.Max(a.X, b.X) &&
                   Math.Min(a.Y, b.Y) <= c.Y && c.Y <= Math.Max(a.Y, b.Y);
        }

        private static int CCW(Vector2 a, Vector2 b, Vector2 c)
        {
            float area = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
            if (area > 0) return 1;
            if (area < 0) return -1;
            return 0;
        }
    }

    public class FootholdGroup
    {
        public FootholdGroup(int index)
        {
            this.Footholds = new List<FootholdItem>();
            this.GroupArea = Rectangle.Empty;
            this.Index = index;
        }

        public List<FootholdItem> Footholds { get; set; }
        public Rectangle GroupArea { get; set; }
        public int Index { get; private set; }

        public void Add(FootholdItem item)
        {
            Footholds.Add(item);
            item.GroupIndex = this.Index;
            if (this.GroupArea == Rectangle.Empty)
            {
                this.GroupArea = item.FootholdArea;
            }
            else this.GroupArea = Rectangle.Union(this.GroupArea, item.FootholdArea);
        }

        public void Build(Dictionary<int, FootholdItem> layerFootholdsByID, Dictionary<int, FootholdItem> allFootholdsByID)
        {
            foreach (var fh in Footholds)
            {
                FootholdItem prevFH = null;
                FootholdItem nextFH = null;
                if (fh.Prev != 0)
                {
                    if (layerFootholdsByID.TryGetValue(fh.Prev, out prevFH) || allFootholdsByID.TryGetValue(fh.Prev, out prevFH))
                        fh.PrevFH = prevFH;
                }
                if (fh.Next != 0)
                {
                    if (layerFootholdsByID.TryGetValue(fh.Next, out nextFH) || allFootholdsByID.TryGetValue(fh.Next, out nextFH))
                        fh.NextFH = nextFH;
                }
            }
        }
    }
}
