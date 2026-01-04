using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using WzComparerR2.CharaSim;
using WzComparerR2.PluginBase;
using static WzComparerR2.CharaSimControl.RenderHelper;
using Resource = CharaSimResource.Resource;

namespace WzComparerR2.CharaSimControl
{
    public class WorldArchiveTooltipRender : TooltipRender
    {
        public WorldArchiveTooltipRender()
        {
            this.NpcID = -1;
            this.MobID = -1;
        }

        public string WorldArchiveMessage { get; set; }
        public string MonsterBookMessage { get; set; }
        public string NpcQuoteMessage { get; set; }
        public int NpcID { get; set; }
        public int MobID { get; set; }

        public override Bitmap Render()
        {
            if (string.IsNullOrEmpty(WorldArchiveMessage) && string.IsNullOrEmpty(MonsterBookMessage) && string.IsNullOrEmpty(NpcQuoteMessage))
                return null;

            if (string.IsNullOrEmpty(WorldArchiveMessage) && !string.IsNullOrEmpty(NpcQuoteMessage))
            {
                WorldArchiveMessage = "(정보 없음)";
            }

            Bitmap waMobBmp = GetSpecialMobBitmap(MobID);
            if (waMobBmp != null && waMobBmp.Width > 259)
            {
                double scale = (double)259 / waMobBmp.Width;
                Rectangle resizedRect = new Rectangle(0, 0, 259, (int)(waMobBmp.Height * scale));
                Bitmap resizedBmp = new Bitmap(resizedRect.Width, resizedRect.Height);
                using (Graphics g = Graphics.FromImage(resizedBmp))
                {
                    g.DrawImage(waMobBmp, resizedRect);
                }
                waMobBmp = resizedBmp;
            }
            Bitmap waNpcBmp = GetSpecialNpcBitmap(NpcID);

            int height = 30;
            Bitmap bmp1 = new Bitmap(1, 1);
            using (Graphics g = Graphics.FromImage(bmp1))
            {
                if (waMobBmp != null) height += waMobBmp.Height;
                if (!string.IsNullOrEmpty(WorldArchiveMessage))
                {
                    foreach (var i in SplitLine(WorldArchiveMessage))
                    {
                        GearGraphics.DrawPlainText(g, i, GearGraphics.ItemDetailFont, Color.White, 13, 272, ref height, 16);
                    }
                }
                if (!string.IsNullOrEmpty(WorldArchiveMessage) && !string.IsNullOrEmpty(MonsterBookMessage))
                    height += 15;
                if (!string.IsNullOrEmpty(MonsterBookMessage))
                {
                    GearGraphics.DrawPlainText(g, "[경고] 다음 정보는 오래되었으며 현재 버전의 상태를 반영하지 않습니다.", GearGraphics.ItemDetailFont, Color.White, 13, 272, ref height, 16);
                    height += 4;
                    foreach (var i in SplitLine(MonsterBookMessage))
                    {
                        GearGraphics.DrawPlainText(g, i, GearGraphics.ItemDetailFont, Color.White, 13, 272, ref height, 16);
                    }
                }
                if (!string.IsNullOrEmpty(WorldArchiveMessage) && !string.IsNullOrEmpty(NpcQuoteMessage))
                    height += 15;
                if (!string.IsNullOrEmpty(NpcQuoteMessage))
                {
                    GearGraphics.DrawPlainText(g, "NPC 대사", GearGraphics.ItemDetailFont, Color.White, 13, 272, ref height, 16);
                    height += 4;
                    foreach (var i in SplitLine(NpcQuoteMessage))
                    {
                        switch (i.Trim())
                        {
                            case "": break;
                            default:
                                GearGraphics.DrawPlainText(g, " - " + i, GearGraphics.ItemDetailFont, Color.White, 13, 272, ref height, 16);
                                break;
                        }
                    }
                }
            }
            height += 13;

            Bitmap bmp2 = new Bitmap(300, height);
            using (Graphics g = Graphics.FromImage(bmp2))
            {
                GearGraphics.DrawNewTooltipBack(g, 0, 0, bmp2.Width, bmp2.Height);
                int picH = 12;
                // GearGraphics.DrawPlainText(g, "World Archive", GearGraphics.ItemDetailFont, Color.FromArgb(255, 255, 255), 8, 130, ref picH, 13);
                g.DrawImage(Resource.WorldArchive, 14, picH, new Rectangle(0, 0, Resource.WorldArchive.Width, Resource.WorldArchive.Height), GraphicsUnit.Pixel);
                picH = 30;
                if (waMobBmp != null)
                {
                    g.DrawImage(waMobBmp, (300 - waMobBmp.Width) / 2, picH, new Rectangle(0, 0, waMobBmp.Width, waMobBmp.Height), GraphicsUnit.Pixel);
                    picH += waMobBmp.Height;
                }
                if (!string.IsNullOrEmpty(WorldArchiveMessage))
                {
                    foreach (var i in SplitLine(WorldArchiveMessage))
                    {
                        GearGraphics.DrawPlainText(g, i, GearGraphics.ItemDetailFont, Color.White, 13, 272, ref picH, 16);
                    }
                }
                if (!string.IsNullOrEmpty(WorldArchiveMessage) && !string.IsNullOrEmpty(MonsterBookMessage))
                {
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    picH += 3;
                    DrawV6SkillDotline(g, 12, 288, picH);
                    picH += 12;
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                }
                if (!string.IsNullOrEmpty(MonsterBookMessage))
                {
                    GearGraphics.DrawPlainText(g, "[경고] 다음 정보는 오래되었으며 현재 버전의 상태를 반영하지 않습니다.", GearGraphics.ItemDetailFont, Color.Orange, 13, 272, ref picH, 16);
                    picH += 4;
                    foreach (var i in SplitLine(MonsterBookMessage))
                    {
                        GearGraphics.DrawPlainText(g, i, GearGraphics.ItemDetailFont, Color.White, 13, 272, ref picH, 16);
                    }
                }
                if (!string.IsNullOrEmpty(WorldArchiveMessage) && !string.IsNullOrEmpty(NpcQuoteMessage))
                {
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    picH += 3;
                    DrawV6SkillDotline(g, 12, 288, picH);
                    picH += 12;
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                }
                if (!string.IsNullOrEmpty(NpcQuoteMessage))
                {
                    GearGraphics.DrawPlainText(g, "NPC 대사", GearGraphics.ItemDetailFont, Color.FromArgb(204, 255, 0), 13, 272, ref picH, 16);
                    picH += 4;
                    foreach (var i in SplitLine(NpcQuoteMessage))
                    {
                        switch (i.Trim())
                        {
                            case "": break;
                            default:
                                GearGraphics.DrawPlainText(g, " - " + i, GearGraphics.ItemDetailFont, Color.White, 13, 272, ref picH, 16);
                                break;
                        }
                    }
                }
            }
            if (waNpcBmp != null)
            {
                Bitmap bmp3 = new Bitmap(bmp2.Width + waNpcBmp.Width, Math.Max(bmp2.Width, waNpcBmp.Height));
                using (Graphics g = Graphics.FromImage(bmp3))
                {
                    g.DrawImage(bmp2, 0, 0, new Rectangle(0, 0, bmp2.Width, bmp2.Height), GraphicsUnit.Pixel);
                    g.DrawImage(waNpcBmp, bmp2.Width, 0, new Rectangle(0, 0, waNpcBmp.Width, waNpcBmp.Height), GraphicsUnit.Pixel);
                }
                return bmp3;
            }
            return bmp2;
        }

        private string[] SplitLine(string orgText)
        {
            return orgText.Split(new string[] { "\r\n", "\\r\\n", "\\r", "\\n", "\r", "\n" }, StringSplitOptions.None);
        }

        private void DrawV6SkillDotline(Graphics g, int x1, int x2, int y)
        {
            // here's a trick that we won't draw left and right part because it looks the same as background border.
            var picCenter = GearGraphics.is22aniStyle ? Resource.UIToolTipNew_img_Skill_Frame_dotline_c : Resource.UIToolTip_img_Skill_Frame_dotline_c;
            using (var brush = new TextureBrush(picCenter))
            {
                brush.TranslateTransform(x1, y);
                g.FillRectangle(brush, new Rectangle(x1, y, x2 - x1, picCenter.Height));
            }
        }

        private Bitmap GetSpecialMobBitmap(int mobID)
        {
            BitmapOrigin mobBitmap = BitmapOrigin.CreateFromNode(PluginManager.FindWz(@$"UI\UIworldArchive.img\image\mob\{mobID}"), PluginManager.FindWz, this.SourceWzFile);
            return mobBitmap.Bitmap;
        }

        private Bitmap GetSpecialNpcBitmap(int npcID)
        {
            BitmapOrigin npcBitmap = BitmapOrigin.CreateFromNode(PluginManager.FindWz(@$"UI\UIworldArchive.img\illust\npc\{npcID}"), PluginManager.FindWz, this.SourceWzFile);
            if (npcBitmap.Bitmap == null) return null;
            else
            {
                Bitmap npcBmp = npcBitmap.Bitmap;
                Bitmap specialNpcTooltip = new Bitmap(npcBmp.Width + 20, npcBmp.Height + Resource.WorldArchive.Height + 32);
                using (Graphics g = Graphics.FromImage(specialNpcTooltip))
                {
                    GearGraphics.DrawNewTooltipBack(g, 0, 0, specialNpcTooltip.Width, specialNpcTooltip.Height);
                    int picH = 12;
                    g.DrawImage(Resource.WorldArchive, 14, picH, new Rectangle(0, 0, Resource.WorldArchive.Width, Resource.WorldArchive.Height), GraphicsUnit.Pixel);
                    picH += 10 + Resource.WorldArchive.Height;
                    g.DrawImage(npcBmp, 10, picH, new Rectangle(0, 0, npcBmp.Width, npcBmp.Height), GraphicsUnit.Pixel);
                }
                return specialNpcTooltip;
            }
        }
    }
}
