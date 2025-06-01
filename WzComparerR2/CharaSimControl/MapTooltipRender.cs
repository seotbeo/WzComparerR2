using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using WzComparerR2.CharaSim;
using WzComparerR2.Common;
using WzComparerR2.WzLib;
using static WzComparerR2.CharaSimControl.RenderHelper;
using CharaSimResource;

namespace WzComparerR2.CharaSimControl
{
    public class MapTooltipRenderer : TooltipRender
    {
        public MapTooltipRenderer()
        {
        }

        public Map Map { get; set; }
        public bool ShowMiniMap { get; set; }

        public override object TargetItem
        {
            get
            {
                return this.Map;
            }
            set
            {
                this.Map = value as Map;
            }
        }

        public override Bitmap Render()
        {
            if (this.Map == null)
            {
                return null;
            }
            using Bitmap bmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            using Graphics g = Graphics.FromImage(bmp);

            List<TextBlock> titleBlocks = new List<TextBlock>();
            List<TextBlock> mobBlocks = new List<TextBlock>();
            List<TextBlock> npcBlocks = new List<TextBlock>();
            int picY = 0;

            if (Map.MapID > -1)
            {
                string[] mapName = GetMapName(Map.MapID);
                var block = PrepareText(g, mapName[0], GearGraphics.ItemDetailFont, Brushes.White, 0, picY);
                titleBlocks.Add(block);
                int tw = block.Size.Width;

                block = PrepareText(g, mapName[1], GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 18);
                titleBlocks.Add(block);
                tw = Math.Max(block.Size.Width, tw);

                block = PrepareText(g, "ID:" + Map.MapID, GearGraphics.ItemDetailFont, Brushes.White, tw + 4, picY);
                titleBlocks.Add(block);
            }

            if (Map.Mobs.Count > 0)
            {
                picY = 0;
                foreach (var mob in Map.Mobs)
                {
                    string mobName = GetMobName(mob);
                    var block = PrepareText(g, mobName ?? "(null)", GearGraphics.ItemDetailFont, GearGraphics.BlockRedBrush, 0, picY);
                    mobBlocks.Add(block);
                    picY += 18;
                }
            }

            if (Map.Npcs.Count > 0)
            {
                picY = 0;
                foreach (var npc in Map.Npcs)
                {
                    string npcName = GetNpcName(npc);
                    var block = PrepareText(g, npcName ?? "(null)", GearGraphics.ItemDetailFont, GearGraphics.Equip22BrushRare, 0, picY);
                    npcBlocks.Add(block);
                    picY += 18;
                }
            }

            Bitmap mapMark = null;
            if (!string.IsNullOrEmpty(Map.MapMark))
            {
                var mapMarkNode = PluginBase.PluginManager.FindWz(@$"Map\MapHelper.img\mark\{Map.MapMark}");
                if (mapMarkNode != null)
                {
                    mapMark = BitmapOrigin.CreateFromNode(mapMarkNode, PluginBase.PluginManager.FindWz).Bitmap;
                }
            }

            Bitmap miniMap = null;
            if (ShowMiniMap && Map.MiniMapNode != null)
            {
                miniMap = BitmapOrigin.CreateFromNode(Map.MiniMapNode.FindNodeByPath("canvas"), PluginBase.PluginManager.FindWz).Bitmap;
            }

            Rectangle markRect = new Rectangle(0, 0, mapMark?.Width ?? 0, mapMark?.Height ?? 0);
            Rectangle titleRect = Measure(titleBlocks);
            Rectangle miniMapRect = new Rectangle(0, 0, miniMap?.Width ?? 0, miniMap?.Height ?? 0);
            Rectangle mobRect = Measure(mobBlocks);
            Rectangle npcRect = Measure(npcBlocks);

            int width = 0;
            width = Math.Max(miniMapRect.Width, Math.Max(markRect.Width + 5 + titleRect.Width, Math.Max(mobRect.Right, npcRect.Right)));
            if (!markRect.IsEmpty)
                titleRect.X = markRect.Width + 5;
            miniMapRect.X = (width - miniMapRect.Width) / 2;

            int titleHeight = Math.Max(markRect.Height, titleRect.Height) + 4;
            markRect.Y = (titleHeight - markRect.Height) / 2;
            titleRect.Y = (titleHeight - titleRect.Height) / 2;

            if (!miniMapRect.IsEmpty)
                miniMapRect.Height += 4;
            if (!mobRect.IsEmpty)
                mobRect.Height += 4;
            if (!npcRect.IsEmpty)
                npcRect.Height += 4;

            int height = miniMapRect.Height + mobRect.Height + npcRect.Height;
            mobRect.Y = miniMapRect.Height;
            npcRect.Y = miniMapRect.Height + mobRect.Height;
            if (titleHeight != 0)
            {
                height += titleHeight + 4;
                miniMapRect.Y += titleHeight + 4;
                mobRect.Y += titleHeight + 4;
                npcRect.Y += titleHeight + 4;
            }

            Bitmap bmp2 = new Bitmap(width + 20, height + 20);
            using Graphics g2 = Graphics.FromImage(bmp2);
            titleRect.Offset(10, 10);
            miniMapRect.Offset(10, 10);
            mobRect.Offset(31, 12);
            npcRect.Offset(31, 12);

            GearGraphics.DrawNewTooltipBack(g2, 0, 0, bmp2.Width, bmp2.Height);
            if (mapMark != null)
            {
                g2.DrawImage(mapMark, 10, 10);
                mapMark.Dispose();
            }
            foreach (var item in titleBlocks)
            {
                DrawText(g2, item, titleRect.Location);
            }

            if (miniMap != null)
            {
                g2.DrawImage(miniMap, miniMapRect.X, miniMapRect.Y);
                miniMap.Dispose();
            }

            if (mobBlocks.Count > 0)
            {
                g2.DrawImage(Resource.UIWindow_img_ToolTip_WorldMap_Mob, mobRect.X - 21, mobRect.Y - 2);
            }
            foreach (var item in mobBlocks)
            {
                DrawText(g2, item, mobRect.Location);
            }

            if (npcBlocks.Count > 0)
            {
                g2.DrawImage(Resource.UIWindow_img_ToolTip_WorldMap_Npc, npcRect.X - 21, npcRect.Y - 2);
            }
            foreach (var item in npcBlocks)
            {
                DrawText(g2, item, npcRect.Location);
            }

            return bmp2;
        }

        private string[] GetMapName(int mapID)
        {
            string[] ret = ["(null)", "(null)"];
            StringResult sr;
            if (this.StringLinker == null || !this.StringLinker.StringMap.TryGetValue(mapID, out sr))
            {
                return ret;
            }
            var nameList = sr.Name.Split(':');
            if (nameList.Length == 2)
            {
                var streetName = nameList[0].Trim();
                var mapName = nameList[1].Trim();
                if (!string.IsNullOrEmpty(streetName))
                    ret[0] = streetName;
                if (!string.IsNullOrEmpty(mapName))
                    ret[1] = mapName;
            }
            return ret;
        }

        private string GetMobName(int mobID)
        {
            StringResult sr;
            if (this.StringLinker == null || !this.StringLinker.StringMob.TryGetValue(mobID, out sr))
            {
                return null;
            }
            return sr.Name;
        }

        private string GetNpcName(int npcID)
        {
            StringResult sr;
            if (this.StringLinker == null || !this.StringLinker.StringNpc.TryGetValue(npcID, out sr))
            {
                return null;
            }
            return sr.Name;
        }
    }
}
