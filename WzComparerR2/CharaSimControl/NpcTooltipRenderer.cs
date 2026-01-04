using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using WzComparerR2.CharaSim;
using WzComparerR2.Common;
using WzComparerR2.AvatarCommon;
using WzComparerR2.WzLib;
using WzComparerR2.PluginBase;
using static WzComparerR2.CharaSimControl.RenderHelper;

namespace WzComparerR2.CharaSimControl
{
    public class NpcTooltipRenderer : TooltipRender
    {
        public NpcTooltipRenderer()
        {

        }


        public override object TargetItem
        {
            get { return this.NpcInfo; }
            set { this.NpcInfo = value as Npc; }
        }

        public Npc NpcInfo { get; set; }
        public bool ShowAllIllustAtOnce { get; set; }
        public bool EnableWorldArchive { get; set; }
        public bool ShowNpcQuotes { get; set; }
        private AvatarCanvasManager avatar { get; set; }
        private WorldArchiveTooltipRender WorldArchiveRender { get; set; }

        public override Bitmap Render()
        {
            if (NpcInfo == null)
            {
                return null;
            }
            Bitmap bmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp);

            //预绘制
            List<TextBlock> titleBlocks = new List<TextBlock>();
            List<TextBlock> propBlocks = new List<TextBlock>();
            int picY = 0;

            if (NpcInfo.ID > -1)
            {
                string mobName = GetNpcName(NpcInfo.ID);
                var block = PrepareText(g, mobName ?? "(null)", GearGraphics.ItemNameFont2, Brushes.White, 0, 0);
                titleBlocks.Add(block);
                block = PrepareText(g, "ID:" + NpcInfo.ID, GearGraphics.ItemDetailFont, Brushes.White, block.Size.Width + 4, 4);
                titleBlocks.Add(block);
            }

            propBlocks.Add(PrepareText(g, "등장위치 :", GearGraphics.ItemDetailFont, GearGraphics.GearNameBrushG, 0, 0));
            if (NpcInfo?.ID != null)
            {
                var locNode = PluginBase.PluginManager.FindWz("Etc\\NpcLocation.img\\" + NpcInfo.ID.ToString(), this.SourceWzFile);
                if (locNode != null)
                {
                    foreach (var locMapNode in locNode.Nodes)
                    {
                        int mapID;
                        string mapName = null;
                        if (int.TryParse(locMapNode.Text, out mapID))
                        {
                            mapName = GetMapName(mapID);
                        }
                        string npcLoc = string.Format(" {0}({1})", mapName ?? "null", locMapNode.Text);

                        propBlocks.Add(PrepareText(g, npcLoc, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
                    }
                }
            }

            if (propBlocks.Count == 1) //获取地区失败
            {
                propBlocks.Add(PrepareText(g, " 불명", GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            }

            //计算大小
            Rectangle titleRect = Measure(titleBlocks);
            Rectangle imgRect = Rectangle.Empty;
            Rectangle textRect = Measure(propBlocks);
            Bitmap npcImg = NpcInfo.Default.Bitmap;
            if (NpcInfo.IsComponentNPC)
            {
                if (this.avatar == null)
                {
                    this.avatar = new AvatarCanvasManager();
                }
                
                foreach (var node in NpcInfo.Component.Nodes)
                {
                    switch (node.Text)
                    {
                        case "skin":
                            var skin = node.GetValueEx<int>(0);
                            this.avatar.AddBodyFromSkin(skin);
                            break;

                        case "ear":
                            var type = node.GetValueEx<int>(0);
                            this.avatar.SetEarType(type);
                            break;

                        default:
                            var gearID = node.GetValueEx<int>(0);
                            this.avatar.AddGear(gearID);
                            break;
                    }
                }

                var img = this.avatar.GetBitmapOrigin();
                if (img.Bitmap != null)
                {
                    if (NpcInfo.Default.Bitmap != null)
                    {
                        NpcInfo.Default.Bitmap.Dispose();
                    }
                    NpcInfo.Default = img;
                    npcImg = img.Bitmap;
                }

                this.avatar.ClearCanvas();
            }
            if (npcImg != null)
            {
                if (npcImg.Width > 250 || npcImg.Height > 300) //进行缩放
                {
                    double scale = Math.Min((double)250 / npcImg.Width, (double)300 / npcImg.Height);
                    imgRect = new Rectangle(0, 0, (int)(npcImg.Width * scale), (int)(npcImg.Height * scale));
                }
                else
                {
                    imgRect = new Rectangle(0, 0, npcImg.Width, npcImg.Height);
                }
            }

            Bitmap illustration2Tooltip = drawIllustration2SetTooltip(NpcInfo.Illustration2Bitmaps, 8, 4, NpcInfo.IllustIndex);

            //布局 
            //水平排列
            int width = 0;
            if (!imgRect.IsEmpty)
            {
                textRect.X = imgRect.Width + 4;
            }
            width = Math.Max(titleRect.Width, Math.Max(imgRect.Right, textRect.Right));
            titleRect.X = (width - titleRect.Width) / 2;

            //垂直居中
            int height = Math.Max(imgRect.Height, textRect.Height);
            imgRect.Y = (height - imgRect.Height) / 2;
            textRect.Y = (height - textRect.Height) / 2;
            if (!titleRect.IsEmpty)
            {
                height += titleRect.Height + 4;
                imgRect.Y += titleRect.Bottom + 4;
                textRect.Y += titleRect.Bottom + 4;
            }

            //绘制
            bmp = new Bitmap(width + 20, height + 20);
            titleRect.Offset(10, 10);
            imgRect.Offset(10, 10);
            textRect.Offset(10, 10);
            g = Graphics.FromImage(bmp);
            //绘制背景
            GearGraphics.DrawNewTooltipBack(g, 0, 0, bmp.Width, bmp.Height);
            //绘制标题
            foreach (var item in titleBlocks)
            {
                DrawText(g, item, titleRect.Location);
            }
            //绘制图像
            if (npcImg != null && !imgRect.IsEmpty)
            {
                g.DrawImage(npcImg, imgRect);
            }
            //绘制文本
            foreach (var item in propBlocks)
            {
                DrawText(g, item, textRect.Location);
            }
            g.Dispose();
            if (illustration2Tooltip != null)
            {
                Point illustration2Origin = new Point(bmp.Width, 0);
                int totalWidth = bmp.Width + illustration2Tooltip.Width;
                int totalHeight = Math.Max(bmp.Height, illustration2Tooltip.Height);
                Bitmap newTooltip = new Bitmap(totalWidth, totalHeight, PixelFormat.Format32bppArgb);
                Graphics g2 = Graphics.FromImage(newTooltip);
                g2.DrawImage(bmp, 0, 0);
                g2.DrawImage(illustration2Tooltip, illustration2Origin);
                g2.Dispose();
                bmp.Dispose();
                illustration2Tooltip.Dispose();
                bmp = newTooltip;
            }
            string worldArchiveDesc = EnableWorldArchive ? GetWorldArchiveDesc(NpcInfo.ID) : null;
            string npcQuoteMessage = ShowNpcQuotes ? GetNpcQuote(NpcInfo.ID) : null;
            if (!string.IsNullOrEmpty(worldArchiveDesc) || !string.IsNullOrEmpty(npcQuoteMessage) && EnableWorldArchive)
            {
                WorldArchiveRender = new WorldArchiveTooltipRender();
                WorldArchiveRender.WorldArchiveMessage = worldArchiveDesc;
                WorldArchiveRender.NpcQuoteMessage = npcQuoteMessage;
                WorldArchiveRender.NpcID = NpcInfo.ID;
                Bitmap waBitmap = WorldArchiveRender.Render();
                Bitmap appendWaBitmap = new Bitmap(bmp.Width + waBitmap.Width, Math.Max(bmp.Height, waBitmap.Height));
                using (g = Graphics.FromImage(appendWaBitmap))
                {
                    g.DrawImage(bmp, 0, 0, new Rectangle(0, 0, bmp.Width, bmp.Height), GraphicsUnit.Pixel);
                    g.DrawImage(waBitmap, bmp.Width, 0, new Rectangle(0, 0, waBitmap.Width, waBitmap.Height), GraphicsUnit.Pixel);
                }
                bmp = appendWaBitmap;
            }
            return bmp;
        }

        private Bitmap drawIllustration2SetTooltip(List<Bitmap> bitmaps, int margin, int perLineCount, int npcIndex)
        {
            if (bitmaps == null || bitmaps.Count == 0)
            {
                return null;
            }

            if (ShowAllIllustAtOnce)
            {
                int requiredLines = (int)Math.Ceiling(bitmaps.Count / (double)perLineCount);

                int width = 0;
                int height = 0;

                int currentLineWidth = 0;
                int currentLineHeight = 0;
                int lineCount = 0;
                List<int> maxLineHeights = new List<int>();
                foreach (var bmp in bitmaps)
                {
                    if (bmp != null)
                    {
                        currentLineWidth += bmp.Width + margin;
                        currentLineHeight = Math.Max(currentLineHeight, bmp.Height);
                    }
                    if (bitmaps.IndexOf(bmp) % perLineCount == perLineCount - 1)
                    {
                        width = Math.Max(width, currentLineWidth);
                        height += currentLineHeight + margin;

                        maxLineHeights.Add(currentLineHeight + margin);
                        currentLineWidth = 0;
                        currentLineHeight = 0;
                        lineCount++;
                    }
                }
                if (lineCount < requiredLines)
                {
                    width = Math.Max(width, currentLineWidth);
                    height += currentLineHeight + margin;
                    currentLineWidth = 0;
                }
                maxLineHeights.Add(currentLineHeight + margin);
                Bitmap result = new Bitmap(width + 30, height + 30, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(result))
                {
                    GearGraphics.DrawNewTooltipBack(g, 0, 0, result.Width, result.Height);

                    int x = 15;
                    int y = 15;
                    int row = 0;
                    int maxLineHeight = maxLineHeights[0];
                    foreach (var bmp in bitmaps)
                    {
                        if (bmp != null)
                        {
                            g.DrawImage(bmp, x, y + maxLineHeight - bmp.Height);
                            x += bmp.Width + margin;
                        }
                        if (bitmaps.IndexOf(bmp) % perLineCount == perLineCount - 1)
                        {
                            x = 15;
                            y += maxLineHeight;
                            if (++row <= maxLineHeights.Count - 1)
                                maxLineHeight = maxLineHeights[row];
                        }
                    }

                    // Draw Illust Info
                    int picH = 2;
                    GearGraphics.DrawPlainText(g, $"일러스트: {bitmaps.Count}장", GearGraphics.ItemDetailFont, Color.FromArgb(255, 255, 255), 2, 80, ref picH, 13);
                }
                return result;
            }
            else
            {
                Bitmap targetIllust = bitmaps[npcIndex];
                Bitmap result = new Bitmap(targetIllust.Width + 30, targetIllust.Height + 60, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(result))
                {
                    GearGraphics.DrawNewTooltipBack(g, 0, 0, result.Width, result.Height);
                    g.DrawImage(targetIllust, 15, 15);

                    // Draw Illust Info
                    int picH = 2;
                    GearGraphics.DrawPlainText(g, $"일러스트: {npcIndex + 1} / {bitmaps.Count}", GearGraphics.ItemDetailFont, Color.FromArgb(255, 255, 255), 2, 130, ref picH, 13);
                    picH += targetIllust.Height + 12;
                    if (bitmaps.Count > 1) GearGraphics.DrawPlainText(g, $"- + 로 일러스트 전환 가능", GearGraphics.ItemDetailFont, Color.FromArgb(255, 255, 255), 12, 260, ref picH, 13);
                }
                return result;
            }
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

        private string GetMapName(int mapID)
        {
            StringResult sr;
            if (this.StringLinker == null || !this.StringLinker.StringMap.TryGetValue(mapID, out sr))
            {
                return null;
            }
            return sr.Name;
        }

        private string GetWorldArchiveDesc(int npcID)
        {
            StringResult sr;
            if (this.StringLinker == null || !this.StringLinker.StringWorldArchiveNpc.TryGetValue(npcID, out sr))
            {
                return null;
            }
            return sr.Desc;
        }

        private string GetNpcQuote(int npcID)
        {
            NpcQuote quote = NpcQuote.CreateFromNode(PluginManager.FindWz($@"String\Npc.img\{npcID}"), PluginManager.FindWz, this.StringLinker);
            if (quote == null)
                return null;
            else
            {
                HashSet<string> npcQuoteList = new HashSet<string>();
                foreach (var kvp in quote.NQuote)
                    npcQuoteList.Add(kvp.Value);
                foreach (var kvp in quote.FQuote)
                    npcQuoteList.Add(kvp.Value);
                foreach (var kvp in quote.WQuote)
                    npcQuoteList.Add(kvp.Value);
                foreach (var kvp in quote.DQuote)
                    npcQuoteList.Add(kvp.Value);
                foreach (var kvp in quote.SpecialQuote)
                    npcQuoteList.Add(kvp.Value);
                return string.Join("\r\n", npcQuoteList);
            }
        }
    }
}
