using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        private List<int> linePos;

        public override Bitmap Render()
        {
            if (string.IsNullOrEmpty(WorldArchiveMessage) && string.IsNullOrEmpty(MonsterBookMessage) && string.IsNullOrEmpty(NpcQuoteMessage))
                return null;

            linePos = new List<int>();
            Bitmap baseBmp = RenderBase(out int picH);
            Bitmap waNpcBmp = GetSpecialNpcBitmap(NpcID);

            var finalWidth = (baseBmp?.Width ?? 0) + (waNpcBmp?.Width ?? 0);
            var finalHeight = Math.Max(picH, waNpcBmp?.Height ?? 0);

            Bitmap finalBmp = new Bitmap(finalWidth, finalHeight);
            using (Graphics g = Graphics.FromImage(finalBmp))
            {
                var sx = 0;
                if (baseBmp != null)
                {
                    GearGraphics.DrawNewTooltipBack(g, sx, 0, baseBmp.Width, picH);
                    g.DrawImage(baseBmp, sx, 0, new Rectangle(0, 0, baseBmp.Width, picH), GraphicsUnit.Pixel);
                    foreach (var pos in linePos)
                    {
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                        DrawV6SkillDotline(g, 12, baseBmp.Width - 12, pos);
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                    }
                    sx += baseBmp.Width;
                    baseBmp.Dispose();
                }

                if (waNpcBmp != null)
                {
                    g.DrawImage(waNpcBmp, sx, 0, new Rectangle(0, 0, waNpcBmp.Width, waNpcBmp.Height), GraphicsUnit.Pixel);
                    waNpcBmp.Dispose();
                }
            }

            return finalBmp;
        }

        private Bitmap RenderBase(out int picH)
        {
            var waDesc = WorldArchiveMessage;
            if (string.IsNullOrEmpty(WorldArchiveMessage) && !string.IsNullOrEmpty(NpcQuoteMessage))
            {
                waDesc = "(정보 없음)";
            }
            waDesc = waDesc.Replace("#e", "#$^e").Replace("#n", "#$$");

            var waColorTable = new Dictionary<string, Color>()
            {
                { "c", ((SolidBrush)GearGraphics.QuestBrushDefault).Color },
            };
            var waFontTable = new Dictionary<string, Font>()
            {
                { "^e", GearGraphics.EquipMDMoris9FontBold },
            };

            bool drawLines = false;
            Bitmap bmp = new Bitmap(324, DefaultPicHeight);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                //GearGraphics.DrawNewTooltipBack(g, 0, 0, bmp2.Width, bmp2.Height);
                picH = 12;
                // GearGraphics.DrawPlainText(g, "World Archive", GearGraphics.ItemDetailFont, Color.FromArgb(255, 255, 255), 8, 130, ref picH, 13);
                g.DrawImage(Resource.WorldArchive, 14, picH, new Rectangle(0, 0, Resource.WorldArchive.Width, Resource.WorldArchive.Height), GraphicsUnit.Pixel);
                picH = 30;

                Bitmap waMobBmp = GetSpecialMobBitmap(MobID);
                if (waMobBmp != null)
                {
                    if (waMobBmp.Width > 259)
                    {
                        double scale = (double)259 / waMobBmp.Width;
                        Rectangle resizedRect = new Rectangle(0, 0, 259, (int)(waMobBmp.Height * scale));
                        Bitmap resizedBmp = new Bitmap(resizedRect.Width, resizedRect.Height);
                        using (Graphics g2 = Graphics.FromImage(resizedBmp))
                        {
                            g2.DrawImage(waMobBmp, resizedRect);
                        }
                        waMobBmp.Dispose();
                        waMobBmp = resizedBmp;
                    }
                    g.DrawImage(waMobBmp, (bmp.Width - waMobBmp.Width) / 2, picH, new Rectangle(0, 0, waMobBmp.Width, waMobBmp.Height), GraphicsUnit.Pixel);
                    picH += waMobBmp.Height;
                    waMobBmp.Dispose();
                }

                if (!string.IsNullOrEmpty(waDesc))
                {
                    foreach (var i in SplitLine(waDesc))
                    {
                        GearGraphics.DrawString(g, i, GearGraphics.EquipMDMoris9Font, waColorTable, waFontTable, null, 13, 300, ref picH, 16);
                    }
                    drawLines = true;
                }
                if (!string.IsNullOrEmpty(MonsterBookMessage))
                {
                    AddLines(3, 12, ref picH, drawLines);
                    GearGraphics.DrawPlainText(g, "[몬스터북]", GearGraphics.ItemDetailFont, Color.Orange, 13, 300, ref picH, 16);
                    picH += 4;
                    foreach (var i in SplitLine(MonsterBookMessage))
                    {
                        GearGraphics.DrawPlainText(g, i, GearGraphics.EquipMDMoris9Font, Color.White, 13, 300, ref picH, 16);
                    }
                    drawLines = true;
                }
                if (!string.IsNullOrEmpty(NpcQuoteMessage))
                {
                    AddLines(3, 12, ref picH, drawLines);
                    GearGraphics.DrawPlainText(g, "NPC 대사", GearGraphics.ItemDetailFont, Color.FromArgb(204, 255, 0), 13, 300, ref picH, 16);
                    picH += 4;
                    foreach (var i in SplitLine(NpcQuoteMessage))
                    {
                        switch (i.Trim())
                        {
                            case "": break;
                            default:
                                GearGraphics.DrawPlainText(g, " - " + i, GearGraphics.EquipMDMoris9Font, Color.White, 13, 300, ref picH, 16);
                                break;
                        }
                    }
                }

                picH += 13;
            }

            return bmp;
        }

        private void AddLines(int preSpacing, int spacing, ref int picH, bool condition = true)
        {
            if (condition)
            {
                picH += preSpacing;
                linePos.Add(picH);
                picH += spacing;
            }
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
            BitmapOrigin mobBitmap = BitmapOrigin.CreateFromNode(PluginManager.FindWz(@$"UI\UIworldArchive.img\image\mob\{mobID}", this.SourceWzFile), PluginManager.FindWz, this.SourceWzFile);
            return mobBitmap.Bitmap;
        }

        private Bitmap GetSpecialNpcBitmap(int npcID)
        {
            BitmapOrigin npcBitmap = BitmapOrigin.CreateFromNode(PluginManager.FindWz(@$"UI\UIworldArchive.img\illust\npc\{npcID}", this.SourceWzFile), PluginManager.FindWz, this.SourceWzFile);
            if (npcBitmap.Bitmap == null) return null;
            else
            {
                Bitmap npcBmp = npcBitmap.Bitmap;
                Bitmap specialNpcTooltip = new Bitmap(npcBmp.Width / 2 + 20, npcBmp.Height / 2 + Resource.WorldArchive.Height + 32);
                using (Graphics g = Graphics.FromImage(specialNpcTooltip))
                {
                    GearGraphics.DrawNewTooltipBack(g, 0, 0, specialNpcTooltip.Width, specialNpcTooltip.Height);
                    int picH = 12;
                    g.DrawImage(Resource.WorldArchive, 14, picH, new Rectangle(0, 0, Resource.WorldArchive.Width, Resource.WorldArchive.Height), GraphicsUnit.Pixel);
                    picH += 10 + Resource.WorldArchive.Height;
                    //g.DrawImage(npcBmp, 10, picH, new Rectangle(0, 0, npcBmp.Width, npcBmp.Height), GraphicsUnit.Pixel);
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.DrawImage(npcBmp, new Rectangle(10, picH, npcBmp.Width / 2, npcBmp.Height / 2));
                }
                npcBmp.Dispose();
                return specialNpcTooltip;
            }
        }
    }
}
