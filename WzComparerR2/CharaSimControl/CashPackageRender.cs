using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Resource = CharaSimResource.Resource;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;

namespace WzComparerR2.CharaSimControl
{
    public class CashPackageTooltipRender : TooltipRender
    {
        public CashPackageTooltipRender()
        {
        }

        public bool Enable22AniStyle { get; set; }
        public CashPackage CashPackage { get; set; }

        public override object TargetItem
        {
            get { return this.CashPackage; }
            set { this.CashPackage = value as CashPackage; }
        }

        public override Bitmap Render()
        {
            int picHeight;
            List<int> splitterH;
            Bitmap originBmp = RenderCashPackage(out picHeight, out splitterH);
            Bitmap tooltip = new Bitmap(originBmp.Width, picHeight);
            Graphics g = Graphics.FromImage(tooltip);

            //绘制背景区域
            GearGraphics.DrawNewTooltipBack(g, 0, 0, tooltip.Width, tooltip.Height);
            if (splitterH != null && splitterH.Count > 0)
            {
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                var margin = 6;
                foreach (var y in splitterH)
                {
                    DrawDotline(g, margin, tooltip.Width - margin, y);
                }
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            }

            //复制图像
            g.DrawImage(originBmp, 0, 0, new Rectangle(0, 0, tooltip.Width, picHeight), GraphicsUnit.Pixel);

            if (originBmp != null)
                originBmp.Dispose();

            if (this.ShowObjectID)
            {
                GearGraphics.DrawGearDetailNumber(g, 3, 3, CashPackage.ItemID.ToString("d8"), true);
            }

            g.Dispose();
            return tooltip;
        }

        private void DrawDotline(Graphics g, int x1, int x2, int y)
        {
            var picCenter = Resource.UIToolTipNew_img_Skill_Frame_dotline_c;
            using (var brush = new TextureBrush(picCenter))
            {
                brush.TranslateTransform(x1, y);
                g.FillRectangle(brush, new Rectangle(x1, y, x2 - x1, picCenter.Height));
            }
        }

        private Bitmap RenderCashPackage(out int picH, out List<int> splitterH)
        {
            var cashPackageColorTable = new Dictionary<string, Color>()
            {
                { "$r", ((SolidBrush)GearGraphics.OrangeBrush4).Color },
            };
            var cashPackage22ColorTable = new Dictionary<string, Color>()
            {
                { "c", ((SolidBrush)GearGraphics.Equip22BrushEmphasis).Color },
                { "$r", ((SolidBrush)GearGraphics.Equip22BrushRed).Color },
                { "$g", ((SolidBrush)GearGraphics.Equip22BrushLegendary).Color },
            };
            splitterH = new List<int>();

            var colorTable = this.Enable22AniStyle ? cashPackage22ColorTable : cashPackageColorTable;
            var detailFont = this.Enable22AniStyle ? GearGraphics.ItemGulimFont : GearGraphics.ItemDetailFont;
            var onlyCashTrade = false;

            const int DefaultWidth = 300;
            Bitmap cashBitmap = new Bitmap(DefaultWidth, DefaultPicHeight);
            Graphics g = Graphics.FromImage(cashBitmap);
            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;

            int totalPrice = 0, totalOriginalPrice = 0;
            Commodity commodityPackage = new Commodity();
            if (CharaSimLoader.LoadedCommoditiesByItemId.ContainsKey(CashPackage.ItemID))
                commodityPackage = CharaSimLoader.LoadedCommoditiesByItemId[CashPackage.ItemID];

            int fullWidth = Math.Max(DefaultWidth, TextRenderer.MeasureText(g, CashPackage.name, GearGraphics.ItemNameFont2, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix).Width + 12 * 2);
            int[] columnWidth = { CashPackage.SN.Count < 8 ? fullWidth : 34, 34, 34 };

            for (int i = 0; i < CashPackage.SN.Count; ++i)
            {
                Commodity commodity = CharaSimLoader.LoadedCommoditiesBySN[CashPackage.SN[i]];
                string name = null;

                StringResult sr = null;
                if (StringLinker != null)
                {
                    if (StringLinker.StringEqp.TryGetValue(commodity.ItemId, out sr))
                    {
                        name = sr.Name;
                    }
                    else if (StringLinker.StringItem.TryGetValue(commodity.ItemId, out sr))
                    {
                        name = sr.Name;
                    }
                    else
                    {
                        name = "(null)";
                    }
                }
                if (sr == null)
                {
                    name = "(null)";
                }

                int nameWidth = TextRenderer.MeasureText(g, name.Replace(Environment.NewLine, ""), detailFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
                if (commodity.Bonus == 0)
                {
                    if (commodity.originalPrice > 0 && commodity.Price < commodity.originalPrice)
                        nameWidth += 55 + 31 + 6 + 8 + (this.Enable22AniStyle ? 4 : 0);
                    else
                        nameWidth += 55 + 9;
                }
                else
                {
                    nameWidth += 55 + 38 + 6 + 8 + (this.Enable22AniStyle ? 4 : 0);
                    if (commodity.Bonus == 2)
                    {
                        onlyCashTrade = true;
                    }
                }

                if (CashPackage.SN.Count < 8)
                {
                    columnWidth[0] = Math.Max(columnWidth[0], nameWidth);
                }
                else if (CashPackage.SN.Count < 15)
                {
                    if (i < 7)
                        columnWidth[0] = Math.Max(columnWidth[0], nameWidth);
                    else
                        columnWidth[1] = Math.Max(columnWidth[1], nameWidth);
                }
                else
                {
                    if (i < Math.Max(7, CashPackage.SN.Count / 3))
                        columnWidth[0] = Math.Max(columnWidth[0], nameWidth);
                    else if (i < Math.Max(14, 2 * CashPackage.SN.Count / 3))
                        columnWidth[1] = Math.Max(columnWidth[1], nameWidth);
                    else
                        columnWidth[2] = Math.Max(columnWidth[2], nameWidth);
                }
            }

            if (CashPackage.SN.Count < 8)
            {
                fullWidth = Math.Max(fullWidth, columnWidth[0]);
            }
            else if (CashPackage.SN.Count < 15)
            {
                fullWidth = Math.Max(fullWidth, columnWidth[0] + columnWidth[1] + (this.Enable22AniStyle ? 0 : -4));
            }
            else
            {
                fullWidth = Math.Max(fullWidth, columnWidth[0] + columnWidth[1] + columnWidth[2] + (this.Enable22AniStyle ? 0 : -8));
            }

            if (fullWidth > DefaultWidth)
            {
                //重构大小
                g.Dispose();
                cashBitmap.Dispose();

                cashBitmap = new Bitmap(fullWidth, DefaultPicHeight);
                g = Graphics.FromImage(cashBitmap);
            }

            picH = 10;
            TextRenderer.DrawText(g, CashPackage.name, GearGraphics.ItemNameFont2, new Point(cashBitmap.Width + 2, picH), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
            picH += 14;
            var topAttrList = new List<string>();
            if (commodityPackage.termStart > 0 || commodityPackage.termEnd != null)
            {
                string term = "< 판매기간 :";
                if (commodityPackage.termStart > 0)
                    term += string.Format(" {0}년 {1}월 {2}일", commodityPackage.termStart / 1000000, (commodityPackage.termStart / 10000) % 100, (commodityPackage.termStart / 100) % 100);

                if (commodityPackage.termStart > 0 && commodityPackage.termEnd != null)
                    term += "\n~";
                else
                    term += " ~";

                if (commodityPackage.termEnd != null)
                {
                    int termEndDate = Convert.ToInt32(commodityPackage.termEnd.Split('/')[0]);
                    int termEndTime = Convert.ToInt32(commodityPackage.termEnd.Split('/')[1]);
                    term += string.Format(" {0}년 {1}월 {2}일 {3}시 {4}분 {5}초", termEndDate / 10000, (termEndDate / 100) % 100, termEndDate % 100, termEndTime / 10000, (termEndTime / 100) % 100, termEndTime % 100);
                }
                term += " >";

                topAttrList.Add($"#$r{term}#");
            }
            if (commodityPackage.LimitMax > 0)
            {
                string limit = null;
                switch (commodityPackage.LimitMax)
                {
                    case 1:
                        limit = "메이플 ID";
                        break;
                    case 2:
                        limit = "월드";
                        break;
                    case 3:
                        limit = "넥슨 ID";
                        break;
                    default:
                        limit = commodityPackage.LimitMax.ToString();
                        break;
                }
                if (!string.IsNullOrEmpty(limit))
                {
                    topAttrList.Add($"#$r< {limit} 당 구매 제한 >#");
                }
            }
            else if (commodityPackage.Limit > 0)
            {
                string limit = null;
                switch (commodityPackage.Limit)
                {
                    case 2:
                        //최초구매
                        break;
                    case 3:
                        limit = "넥슨 ID";
                        break;
                    case 4:
                        limit = "캐릭터";
                        break;
                    default:
                        limit = commodityPackage.Limit.ToString();
                        break;
                }
                if (!string.IsNullOrEmpty(limit))
                {
                    topAttrList.Add($"#$r< {limit} 한정판매 >#");
                }
            }
            if (topAttrList.Count > 0)
            {
                picH += 8;
                foreach (var attr in topAttrList)
                {
                    GearGraphics.DrawString(g, attr, detailFont, colorTable, 0, cashBitmap.Width, ref picH, 12, alignment: Text.TextAlignment.Center);
                }
            }
            picH += 4;
            if (this.Enable22AniStyle)
            {
                splitterH.Add(picH);
                picH -= 1;
            }

            // ----------------------------------------------------------------------
            picH += 15;

            int descLeft = this.Enable22AniStyle ? 15 : 11;
            int descRight = cashBitmap.Width - (this.Enable22AniStyle ? 26 : 18);
            var desc = CashPackage.desc;
            if (desc != null && desc.Length > 0)
                desc += "\n";
            if (CashPackage.onlyCash == 0)
            {
                var bonus = onlyCashTrade ? "(보너스 아이템 포함)" : "(보너스 아이템 제외)";
                desc += this.Enable22AniStyle ? $"#$r넥슨캐시로 구매 시 사용 전 타인과 1회 교환 가능 {bonus}#"
                    : $"#넥슨캐시로 구매하면 사용 전 1회에 한해 타인과 교환 할 수 있습니다. {bonus}#";
            }
            else
            {
                desc += this.Enable22AniStyle ? "#$r넥슨캐시로만 구매 가능#"
                    : "#넥슨캐시로만 구매할 수 있습니다.#";
                if (onlyCashTrade)
                {
                    desc += this.Enable22AniStyle ? $"\n#$r넥슨캐시로 구매 시 사용 전 타인과 1회 교환 가능 (보너스 아이템 포함)#"
                    : $"\n#넥슨캐시로 구매하면 사용 전 1회에 한해 타인과 교환 할 수 있습니다. (보너스 아이템 포함)#";
                }
            }
            GearGraphics.DrawString(g, desc, detailFont, colorTable, descLeft, descRight, ref picH, 16, strictlyAlignLeft: 2);

            bool hasLine = false;
            picH -= 4;

            int picStartH = picH, picEndH = 0, columnLeft = 0, columnRight = columnWidth[0];
            if (this.Enable22AniStyle)
            {
                columnLeft += 4;
            }

            for (int i = 0; i < CashPackage.SN.Count; ++i)
            {
                if (CashPackage.SN.Count >= 8 && CashPackage.SN.Count < 15)
                {
                    if (i == 7)
                    {
                        hasLine = false;
                        picEndH = picH;
                        picH = picStartH;
                        columnLeft = columnWidth[0] + (this.Enable22AniStyle ? 4 : -2);
                        columnRight = columnWidth[0] + columnWidth[1] + (this.Enable22AniStyle ? 0 : -4);
                    }
                }
                else if (CashPackage.SN.Count >= 15)
                {
                    if (i == Math.Max(7, CashPackage.SN.Count / 3))
                    {
                        hasLine = false;
                        picEndH = picH;
                        picH = picStartH;
                        columnLeft = columnWidth[0] + (this.Enable22AniStyle ? 4 : -2);
                        columnRight = columnWidth[0] + columnWidth[1] + (this.Enable22AniStyle ? 0 : -4);
                    }
                    else if (i == 2 * Math.Max(7, CashPackage.SN.Count / 3))
                    {
                        hasLine = false;
                        picEndH = picH;
                        picH = picStartH;
                        columnLeft = columnWidth[0] + columnWidth[1] + (this.Enable22AniStyle ? 8 : -6);
                        columnRight = columnWidth[0] + columnWidth[1] + columnWidth[2] + (this.Enable22AniStyle ? 0 : -8);
                    }
                }

                if (hasLine)
                {
                    g.DrawImage(Resource.CSDiscount_Line, columnLeft + 13, picH);
                    picH += 1;
                }

                Commodity commodity = CharaSimLoader.LoadedCommoditiesBySN[CashPackage.SN[i]];
                string name = null, info = null, time = null;
                BitmapOrigin IconRaw = new BitmapOrigin();

                StringResult sr = null;
                if (StringLinker != null)
                {
                    Wz_Node iconNode = null;
                    if (StringLinker.StringEqp.TryGetValue(commodity.ItemId, out sr))
                    {
                        name = sr.Name;
                        string[] fullPaths = sr.FullPath.Split('\\');
                        iconNode = PluginBase.PluginManager.FindWz(string.Format(@"Character\{0}\{1:D8}.img\info\iconRaw", String.Join("\\", new List<string>(fullPaths).GetRange(2, fullPaths.Length - 3).ToArray()), commodity.ItemId));
                    }
                    else if (StringLinker.StringItem.TryGetValue(commodity.ItemId, out sr))
                    {
                        name = sr.Name;
                        if (Regex.IsMatch(sr.FullPath, @"^(Cash|Consume|Etc|Ins).img\\.+$"))
                        {
                            string itemType = null;
                            if (Regex.IsMatch(sr.FullPath, @"^Cash.img\\.+$"))
                                itemType = "Cash";
                            else if (Regex.IsMatch(sr.FullPath, @"^Consume.img\\.+$"))
                                itemType = "Consume";
                            else if (Regex.IsMatch(sr.FullPath, @"^Etc.img\\.+$"))
                                itemType = "Etc";
                            else if (Regex.IsMatch(sr.FullPath, @"^Ins.img\\.+$"))
                                itemType = "Install";
                            iconNode = PluginBase.PluginManager.FindWz(string.Format(@"Item\{0}\{1:D4}.img\{2:D8}\info\iconRaw", itemType, commodity.ItemId / 10000, commodity.ItemId));
                        }
                        else if (Regex.IsMatch(sr.FullPath, @"^Pet.img\\.+$"))
                        {
                            iconNode = PluginBase.PluginManager.FindWz(string.Format(@"Item\Pet\{0:D7}.img\info\iconRaw", commodity.ItemId));
                        }
                    }
                    else
                    {
                        name = "(null)";
                    }
                    if (iconNode != null)
                    {
                        IconRaw = BitmapOrigin.CreateFromNode(iconNode, PluginBase.PluginManager.FindWz);
                    }
                }
                if (sr == null)
                {
                    name = "(null)";
                }

                if (commodity.Bonus == 0)
                {
                    if (commodity.Count > 1)
                        info += commodity.Count + "개 ";
                    if (commodity.originalPrice == 0)
                    {
                        foreach (var commodity2 in CharaSimLoader.LoadedCommoditiesBySN.Values)
                        {
                            if (commodity2.ItemId == commodity.ItemId && commodity2.Count == commodity.Count && commodity2.Period == commodity.Period && commodity2.gameWorld == commodity.gameWorld && commodity2.Price > commodity.originalPrice)
                                commodity.originalPrice = commodity2.Price;
                        }
                        if (commodity.originalPrice == commodity.Price)
                            commodity.originalPrice = 0;
                    }
                    if (commodity.originalPrice > 0 && commodity.Price < commodity.originalPrice)
                    {
                        info += commodity.originalPrice + "캐시      ";
                        totalOriginalPrice += commodity.originalPrice;
                    }
                    else
                    {
                        totalOriginalPrice += commodity.Price;
                    }
                    info += commodity.Price + "캐시";
                    totalPrice += commodity.Price;
                }
                else
                {
                    info += commodity.Count + "개 ";
                    if (commodity.originalPrice > 0)
                    {
                        info += commodity.originalPrice + "캐시";
                        totalOriginalPrice += commodity.originalPrice;
                    }
                    else
                    {
                        info += commodity.Price + "캐시";
                        totalOriginalPrice += commodity.Price;
                    }
                }

                if (commodity.Period > 0)
                {
                    time = commodity.Period + "일동안 사용 가능";
                }

                g.DrawImage(Resource.CSDiscount_backgrnd, columnLeft + 13, picH + 12);
                if (IconRaw.Bitmap != null)
                {
                    //g.DrawImage(IconRaw.Bitmap, columnLeft + 13 + 1 - IconRaw.Origin.X, picH + 12 + 33 - IconRaw.Origin.Y);
                    g.DrawImage(IconRaw.Bitmap, columnLeft + 30 - (IconRaw.Bitmap.Width + 1) / 2, picH + 29 - (IconRaw.Bitmap.Height + 1) / 2);
                }
                if (time == null)
                {
                    TextRenderer.DrawText(g, name.TrimEnd(Environment.NewLine.ToCharArray()), detailFont, new Point(columnLeft + 55, picH + 17), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    if (commodity.Bonus == 0)
                    {
                        TextRenderer.DrawText(g, info, detailFont, new Point(columnLeft + 55, picH + 33), Color.White, TextFormatFlags.NoPadding);
                        if (commodity.originalPrice > 0 && commodity.Price < commodity.originalPrice)
                        {
                            int width = TextRenderer.MeasureText(g, info.Substring(0, info.IndexOf("      ")), detailFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
                            g.DrawLine(Pens.White, columnLeft + 55, picH + 33 + 4, columnLeft + 55 + width + 1, picH + 33 + 4);
                            g.DrawImage(Resource.CSDiscount_arrow, columnLeft + 55 + width + 10, picH + 33 + 1);
                            DrawDiscountNum(g, "-" + (int)(100 - 100.0 * commodity.Price / commodity.originalPrice) + "%", columnRight - 40, picH + 16, StringAlignment.Near);
                        }
                    }
                    else
                    {
                        TextRenderer.DrawText(g, info, detailFont, new Point(columnLeft + 55, picH + 33), Color.Red, TextFormatFlags.NoPadding);
                        g.DrawImage(Resource.CSDiscount_bonus, columnRight - 47, picH + 29);
                    }
                }
                else
                {
                    TextRenderer.DrawText(g, name.Replace(Environment.NewLine, ""), detailFont, new Point(columnLeft + 55, picH + 8), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    if (commodity.Bonus == 0)
                    {
                        TextRenderer.DrawText(g, info, detailFont, new Point(columnLeft + 55, picH + 24), Color.White, TextFormatFlags.NoPadding);
                        if (commodity.originalPrice > 0 && commodity.Price < commodity.originalPrice)
                        {
                            int width = TextRenderer.MeasureText(g, info.Substring(0, info.IndexOf("      ")), detailFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
                            g.DrawLine(Pens.White, columnLeft + 55, picH + 24 + 4, columnLeft + 55 + width + 1, picH + 24 + 4);
                            g.DrawImage(Resource.CSDiscount_arrow, columnLeft + 55 + width + 10, picH + 24 + 1);
                            DrawDiscountNum(g, "-" + (int)(100 - 100.0 * commodity.Price / commodity.originalPrice) + "%", columnRight - 40, picH + 7, StringAlignment.Near);
                        }
                    }
                    else
                    {
                        TextRenderer.DrawText(g, info, detailFont, new Point(columnLeft + 55, picH + 24), Color.Red, TextFormatFlags.NoPadding);
                        g.DrawImage(Resource.CSDiscount_bonus, columnRight - 47, picH + 20);
                    }
                    TextRenderer.DrawText(g, time, detailFont, new Point(columnLeft + 55, picH + 39), Color.White, TextFormatFlags.NoPadding);
                }
                picH += 57;

                hasLine = true;
            }

            if (picEndH != 0)
                picH = picEndH;
            if (CashPackage.SN.Count == 0)
                picH += 4;

            g.DrawLine(Pens.White, descLeft + 2, picH, cashBitmap.Width - (this.Enable22AniStyle ? 14 : 8), picH);
            picH += 11;

            g.DrawImage(Resource.CSDiscount_total, descLeft - 2, picH + 1);
            if (totalOriginalPrice == totalPrice)
            {
                TextRenderer.DrawText(g, totalPrice + "캐시", detailFont, new Point(descLeft + 42, picH), Color.White, TextFormatFlags.NoPadding);
            }
            else
            {
                TextRenderer.DrawText(g, totalOriginalPrice + "캐시     " + totalPrice + "캐시", detailFont, new Point(descLeft + 42, picH), Color.White, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, totalOriginalPrice + "캐시", detailFont, new Point(descLeft + 42, picH), Color.Red, TextFormatFlags.NoPadding);
                g.DrawImage(Resource.CSDiscount_arrow, (descLeft + 42) + TextRenderer.MeasureText(g, totalOriginalPrice + "캐시", detailFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width + 5, picH + 1);
                DrawDiscountNum(g, "-" + (int)((100 - 100.0 * totalPrice / totalOriginalPrice)) + "%", cashBitmap.Width - 40, picH - 1, StringAlignment.Near);
            }
            picH += 11;

            picH += 13;
            format.Dispose();
            g.Dispose();
            return cashBitmap;
        }

        private void DrawDiscountNum(Graphics g, string numString, int x, int y, StringAlignment align)
        {
            if (g == null || numString == null)
                return;
            bool near = align == StringAlignment.Near;

            for (int i = 0; i < numString.Length; i++)
            {
                char c = near ? numString[i] : numString[numString.Length - i - 1];
                Image image = null;
                Point origin = Point.Empty;
                switch (c)
                {
                    case '-':
                        image = Resource.ResourceManager.GetObject("CSDiscount_w") as Image;
                        break;
                    case '%':
                        image = Resource.ResourceManager.GetObject("CSDiscount_e") as Image;
                        break;
                    default:
                        if ('0' <= c && c <= '9')
                        {
                            image = Resource.ResourceManager.GetObject("CSDiscount_" + c) as Image;
                        }
                        break;
                }

                if (image != null)
                {
                    if (near)
                    {
                        g.DrawImage(image, x + origin.X, y + origin.Y);
                        x += image.Width + origin.X;
                    }
                    else
                    {
                        x -= image.Width + origin.X;
                        g.DrawImage(image, x + origin.X, y + origin.Y);
                    }
                }
            }
        }
    }
}
