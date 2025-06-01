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
            TextBlock barrierBlock = null;
            int[] barrierResourceWidth = [17, 18, 18];
            int barrierType = 0;
            int picY = 0;

            if (Map.MapID > -1)
            {
                string[] mapName = GetMapName(Map.MapID, Map.Link);
                var block = PrepareText(g, mapName[0], GearGraphics.ItemDetailFont, Brushes.White, 0, picY);
                titleBlocks.Add(block);
                int tw = block.Size.Width;

                block = PrepareText(g, mapName[1], GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 18);
                titleBlocks.Add(block);
                tw = Math.Max(block.Size.Width, tw);

                block = PrepareText(g, $"ID:{Map.MapID}", GearGraphics.ItemDetailFont, Brushes.White, tw + 8, picY);
                titleBlocks.Add(block);
            }

            if (Map.Barrier > 0)
            {
                barrierType = 1;
                barrierBlock = PrepareText(g, Map.Barrier.ToString(), GearGraphics.EquipDetailFont, GearGraphics.Equip22BrushEmphasis, 0, 0);
            }
            else if (Map.BarrierArc > 0)
            {
                barrierType = 2;
                barrierBlock = PrepareText(g, Map.BarrierArc.ToString(), GearGraphics.EquipDetailFont, GearGraphics.BarrierArcBrush, 0, 0);
            }
            else if (Map.BarrierAut > 0)
            {
                barrierType = 3;
                barrierBlock = PrepareText(g, Map.BarrierAut.ToString(), GearGraphics.EquipDetailFont, GearGraphics.BarrierAutBrush, 0, 0);
            }

            if (Map.Mobs.Count > 0)
            {
                picY = 0;
                foreach (var mob in Map.Mobs)
                {
                    string mobName = GetMobName(mob);
                    var mobLevel = PluginBase.PluginManager.FindWz(@$"Mob\{mob:D7}.img\info\level").GetValueEx<int?>(null);
                    var block = PrepareText(g, (mobName ?? "(null)") + ((mobLevel != null) ? $"(Lv.{mobLevel})" : ""), GearGraphics.ItemDetailFont, GearGraphics.BlockRedBrush, 0, picY);
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

            Rectangle barrierRect = barrierBlock?.Rectangle ?? new Rectangle();
            Rectangle markRect = new Rectangle(0, 0, mapMark?.Width ?? 0, mapMark?.Height ?? 0);
            Rectangle titleRect = Measure(titleBlocks);
            Rectangle miniMapRect = new Rectangle(0, 0, miniMap?.Width ?? 0, miniMap?.Height ?? 0);
            Rectangle mobRect = Measure(mobBlocks);
            Rectangle npcRect = Measure(npcBlocks);

            int width = 0;
            width = Math.Max(miniMapRect.Width, Math.Max(markRect.Width + 5 + titleRect.Width, Math.Max(mobRect.Width, npcRect.Width)));
            if (!markRect.IsEmpty)
                titleRect.X = markRect.Width + 5;
            miniMapRect.X = (width - miniMapRect.Width) / 2;

            int barrierResourceX = 0;
            if (!barrierRect.IsEmpty)
            {
                barrierResourceX = (width - (barrierResourceWidth[barrierType - 1] + 2 + barrierRect.Width)) / 2;
                barrierRect.X = barrierResourceX + barrierResourceWidth[barrierType - 1] + 2;
            }

            int titleHeight = Math.Max(markRect.Height, titleRect.Height);
            markRect.Y = barrierRect.Height + (titleHeight - markRect.Height) / 2;
            titleRect.Y = barrierRect.Height + (titleHeight - titleRect.Height) / 2;

            if (titleHeight != 0)
                titleHeight += 6;
            if (!miniMapRect.IsEmpty)
                miniMapRect.Height += 6;
            if (!mobRect.IsEmpty)
                mobRect.Height += 6;
            if (!npcRect.IsEmpty)
                npcRect.Height += 6;

            miniMapRect.Y = barrierRect.Height + titleHeight;
            mobRect.Y = miniMapRect.Y + miniMapRect.Height;
            npcRect.Y = mobRect.Y + mobRect.Height;

            int height = barrierRect.Height + titleHeight + miniMapRect.Height + mobRect.Height + npcRect.Height;

            Bitmap bmp2 = new Bitmap(width + 20, height + 20);
            using Graphics g2 = Graphics.FromImage(bmp2);
            barrierRect.Offset(10, 10);
            markRect.Offset(10, 10);
            titleRect.Offset(10, 10);
            miniMapRect.Offset(10, 10);
            mobRect.Offset(31, 12);
            npcRect.Offset(31, 12);

            GearGraphics.DrawNewTooltipBack(g2, 0, 0, bmp2.Width, bmp2.Height);
            if (barrierBlock != null)
            {
                switch (barrierType)
                {
                    case 1:
                        g2.DrawImage(Resource.UIWindow_img_ToolTip_WorldMap_StarForce, barrierResourceX + 10, 10);
                        break;
                    case 2:
                        g2.DrawImage(Resource.UIWindow_img_ToolTip_WorldMap_ArcaneForce, barrierResourceX + 10, 10);
                        break;
                    case 3:
                        g2.DrawImage(Resource.UIWindow_img_ToolTip_WorldMap_AuthenticForce, barrierResourceX + 10, 10);
                        break;
                }
                DrawText(g2, barrierBlock, barrierRect.Location);
            }
            if (mapMark != null)
            {
                g2.DrawImage(mapMark, markRect.X, markRect.Y);
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

        private string[] GetMapName(int mapID, int? linkID)
        {
            string[] ret = ["(null)", "(null)"];
            StringResult sr;
            if (this.StringLinker == null || !this.StringLinker.StringMap.TryGetValue(mapID, out sr))
            {
                if (!this.StringLinker.StringMap.TryGetValue(linkID ?? -1, out sr))
                {
                    return ret;
                }
            }

            ret[0] = sr.StreetName ?? "(null)";
            ret[1] = sr.MapName ?? "(null)";
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
