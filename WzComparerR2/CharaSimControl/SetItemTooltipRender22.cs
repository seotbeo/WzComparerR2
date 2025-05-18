using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Resource = CharaSimResource.Resource;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using System.Text.RegularExpressions;
using WzComparerR2.CharaSim;

namespace WzComparerR2.CharaSimControl
{
    public class SetItemTooltipRender22 : TooltipRender
    {
        public SetItemTooltipRender22()
        {
            res = new Dictionary<string, TextureBrush>();
            res["top"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Equip_frame_set_top, WrapMode.Clamp);
            res["mid"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Equip_frame_set_mid, WrapMode.Tile);
            res["line"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Equip_frame_set_line, WrapMode.Clamp);
            res["btm"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Equip_frame_set_btm, WrapMode.Clamp);

            res["category_w"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Equip_frame_common_category_w, WrapMode.Clamp);
            res["category_c"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Equip_frame_common_category_c, WrapMode.Tile);
            res["category_e"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Equip_frame_common_category_e, WrapMode.Clamp);
        }

        private static Dictionary<string, TextureBrush> res;

        public SetItem SetItem { get; set; }

        public override object TargetItem
        {
            get { return this.SetItem; }
            set { this.SetItem = value as SetItem; }
        }

        public bool IsCombineProperties { get; set; } = true;
        private List<int> linePos;

        public override Bitmap Render()
        {
            if (this.SetItem == null)
            {
                return null;
            }

            bool specialPetSetEffectName = this.SetItem.ItemIDs.Parts.Any(p => p.Value.ItemIDs.Any(i => isSpecialPet(i.Key)));

            linePos = new List<int>();
            int width = 298;
            int picHeight1;
            Bitmap originBmp = RenderSetItem(specialPetSetEffectName, out picHeight1);
            int picHeight2 = 0;
            Bitmap effectBmp = null;

            //if (this.SetItem.ExpandToolTip)
            if (false)
            {
                effectBmp = RenderEffectPart(specialPetSetEffectName, out picHeight2);
                width += 298;
            }

            Bitmap tooltip = new Bitmap(width, Math.Max(picHeight1, picHeight2));
            Graphics g = Graphics.FromImage(tooltip);

            DrawBG(g, 0, picHeight1, 0);
            g.DrawImage(originBmp, 0, 0, new Rectangle(0, 0, originBmp.Width, picHeight1), GraphicsUnit.Pixel);

            //绘制右侧
            if (effectBmp != null)
            {
                GearGraphics.DrawNewTooltipBack(g, originBmp.Width, 0, effectBmp.Width, picHeight2);
                g.DrawImage(effectBmp, originBmp.Width, 0, new Rectangle(0, 0, effectBmp.Width, picHeight2), GraphicsUnit.Pixel);
            }

            originBmp?.Dispose();
            effectBmp?.Dispose();
            g.Dispose();
            return tooltip;
        }

        private void DrawBG(Graphics g, int startX, int endY, int target)
        {
            int[] startYList = [res[$"top"].Image.Height];
            int startY = startYList[target];

            for (int i = 0; i < linePos.Count; i += 2)
            {
                if (linePos[i] == target)
                {
                    if (startY == startYList[target])
                    {
                        if (linePos[i + 1] < startY)
                            g.DrawImage(res[$"top"].Image, startX, 0, res[$"top"].Image.Width, linePos[i + 1]);
                        else
                            g.DrawImage(res[$"top"].Image, startX, 0);
                    }

                    FillRect(g, res[$"mid"], startX, startY, linePos[i + 1]);
                    g.DrawImage(res[$"line"].Image, startX, linePos[i + 1]);
                    startY = linePos[i + 1] + 3;
                }
            }
            FillRect(g, res[$"mid"], startX, startY, endY - 13);
            g.DrawImage(res[$"btm"].Image, startX, endY - 13);
        }

        private void FillRect(Graphics g, TextureBrush brush, int x, int y0, int y1)
        {
            brush.ResetTransform();
            brush.TranslateTransform(x, y0);
            g.FillRectangle(brush, x, y0, brush.Image.Width, y1 - y0);
        }

        private void AddLines(int target, int spacing, ref int picH, bool condition = true)
        {
            if (condition)
            {
                linePos.Add(target);
                linePos.Add(picH);
                picH += spacing;
            }
        }

        private bool isSpecialPet(int itemID)
        {
            if (itemID / 1000000 != 5)
            {
                return false;
            }
            Wz_Node itemNode = PluginBase.PluginManager.FindWz(string.Format(@"Item\Pet\{0:D7}.img", itemID));
            if (itemNode != null)
            {
                var item = Item.CreateFromNode(itemNode, PluginManager.FindWz);
                return item.Props.TryGetValue(ItemPropType.wonderGrade, out long value) && (value == 1 || value == 4 || value == 5 || value == 6);
            }
            return false;
        }

        private Bitmap RenderSetItem(bool specialPetSetEffectName, out int picHeight)
        {
            Bitmap setBitmap = new Bitmap(298, DefaultPicHeight);
            Graphics g = Graphics.FromImage(setBitmap);
            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;

            picHeight = 21;
            g.DrawImage(Resource.UIToolTipNew_img_Item_Equip_textIcon_set_normal, 14, picHeight - 1);
            TextRenderer.DrawText(g, this.SetItem.SetItemName, GearGraphics.EquipMDMoris9Font, new Point(46, picHeight), Color.White, TextFormatFlags.NoPadding);
            DrawCategory(g, $"0 #$g/ {(this.SetItem.Parts ? this.SetItem.ItemIDs.Parts.Count : this.SetItem.Effects.Keys.Max())}", picHeight - 2);
            picHeight += 29;

            format.Alignment = StringAlignment.Far;
            Wz_Node characterWz = PluginManager.FindWz(Wz_Type.Character);

            if (this.SetItem.SetItemID > 0)
            {
                HashSet<string> partNames = new HashSet<string>();

                foreach (var setItemPart in this.SetItem.ItemIDs.Parts)
                {
                    string itemName = setItemPart.Value.RepresentName;
                    string typeName = setItemPart.Value.TypeName;

                    // for 'parts' setitem, detect typeName by the first itemID per part
                    //if (string.IsNullOrEmpty(typeName) && SetItem.Parts)
                    //{
                    //    typeName = "장비";
                    //}

                    ItemBase itemBase = null;
                    bool cash = false;

                    if (setItemPart.Value.ItemIDs.Count > 0)
                    {
                        var itemID = setItemPart.Value.ItemIDs.First().Key;

                        switch (itemID / 1000000)
                        {
                            case 0: //avatar
                            case 1: //gear
                                if (characterWz != null)
                                {
                                    foreach (Wz_Node typeNode in characterWz.Nodes)
                                    {
                                        Wz_Node itemNode = typeNode.FindNodeByPath(string.Format("{0:D8}.img", itemID), true);
                                        if (itemNode != null)
                                        {
                                            var gear = Gear.CreateFromNode(itemNode, PluginManager.FindWz);
                                            cash = gear.Cash;
                                            itemBase = gear;
                                            break;
                                        }
                                    }
                                }
                                break;

                            case 5: //Pet
                                {
                                    Wz_Node itemNode = PluginBase.PluginManager.FindWz(string.Format(@"Item\Pet\{0:D7}.img", itemID));
                                    if (itemNode != null)
                                    {
                                        var item = Item.CreateFromNode(itemNode, PluginManager.FindWz);
                                        cash = item.Cash;
                                        itemBase = item;
                                    }
                                }
                                break;
                        }
                    }

                    if (string.IsNullOrEmpty(itemName) || string.IsNullOrEmpty(typeName))
                    {
                        if (setItemPart.Value.ItemIDs.Count > 0)
                        {
                            var itemID = setItemPart.Value.ItemIDs.First().Key;
                            StringResult sr = null; ;
                            if (this.StringLinker != null)
                            {
                                if (this.StringLinker.StringEqp.TryGetValue(itemID, out sr))
                                {
                                    itemName = sr.Name;
                                    if (typeName == null)
                                    {
                                        typeName = ItemStringHelper.GetSetItemGearTypeString(Gear.GetGearType(itemID));
                                    }
                                    switch (Gear.GetGender(itemID))
                                    {
                                        case 0: itemName += " (남)"; break;
                                        case 1: itemName += " (여)"; break;
                                    }
                                }
                                else if (this.StringLinker.StringItem.TryGetValue(itemID, out sr)) //兼容宠物
                                {
                                    itemName = sr.Name;
                                    //if (typeName == null)
                                    {
                                        if (itemID / 10000 == 500)
                                        {
                                            typeName = "펫";
                                        }
                                        else
                                        {
                                            typeName = "";
                                        }
                                    }
                                }
                            }
                            if (sr == null)
                            {
                                itemName = "(null)";
                            }
                        }
                    }

                    itemName = itemName ?? string.Empty;
                    typeName = typeName ?? "장비";

                    /*
                    var match = Regex.Match(typeName, @"^(\((.*)\)|（(.*)）|\[(.*)\])$");
                    if (match.Success)
                    {
                        typeName = match.Groups[2].Success ? match.Groups[2].Value :
                               match.Groups[3].Success ? match.Groups[3].Value :
                               match.Groups[4].Value;
                    }

                    if (this.SetItem.Effects.Count > 1 && this.SetItem.ItemIDs.Parts.Count == 1)
                    {
                        typeName += "  [0/3]";
                    }
                    */

                    if (!partNames.Contains(itemName + typeName))
                    {
                        partNames.Add(itemName + typeName);
                        Brush brush = setItemPart.Value.Enabled ? Brushes.White : GearGraphics.Equip22BrushDarkGray;
                        if (!cash)
                        {
                            int typeWidth = TextRenderer.MeasureText(g, typeName, GearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
                            TextRenderer.DrawText(g, typeName, GearGraphics.EquipMDMoris9Font, new Point(14, picHeight), ((SolidBrush)brush).Color, TextFormatFlags.NoPadding);
                            TextRenderer.DrawText(g, Compact(g, itemName, 200), GearGraphics.EquipMDMoris9Font, new Point(90, picHeight), ((SolidBrush)brush).Color, TextFormatFlags.NoPadding);
                            picHeight += 15;
                        }
                        else
                        {
                            g.DrawImage(Resource.UIToolTip_img_Item_ItemIcon_canvas_Backgrnd, 13, picHeight - 2);
                            g.DrawImage(Resource.Item_shadow, 15 + 2 + 3, picHeight + 1 + 32 - 6);
                            if (itemBase?.IconRaw.Bitmap != null)
                            {
                                var icon = itemBase.IconRaw;
                                g.DrawImage(icon.Bitmap, 15 + 2 - icon.Origin.X, picHeight + 1 + 32 - icon.Origin.Y);
                            }
                            g.DrawImage(Resource.CashItem_0, 15 + 2 + 20, picHeight + 1 + 32 - 12);
                            int typeWidth = TextRenderer.MeasureText(g, typeName, GearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
                            TextRenderer.DrawText(g, Compact(g, itemName, 261 - 10 - typeWidth - 52), GearGraphics.EquipMDMoris9Font, new Point(60, picHeight), ((SolidBrush)brush).Color, TextFormatFlags.NoPadding);
                            GearGraphics.DrawString(g, typeName, GearGraphics.EquipMDMoris9Font, new Dictionary<string, Color>() { { string.Empty, ((SolidBrush)GearGraphics.Equip22BrushGray).Color } }, 30, 283, ref picHeight, 0, Text.TextAlignment.Right);
                            var tempHeight = picHeight;
                            if (setItemPart.Value.ByGender)
                            {
                                picHeight += 15;
                                foreach (var itemID in setItemPart.Value.ItemIDs.Keys)
                                {
                                    StringResult sr = null; ;
                                    if (this.StringLinker != null)
                                    {
                                        if (this.StringLinker.StringEqp.TryGetValue(itemID, out sr))
                                        {
                                            itemName = sr.Name;
                                            switch (Gear.GetGender(itemID))
                                            {
                                                case 0: itemName += " (남)"; break;
                                                case 1: itemName += " (여)"; break;
                                            }
                                        }
                                        else if (this.StringLinker.StringItem.TryGetValue(itemID, out sr)) //兼容宠物
                                        {
                                            itemName = sr.Name;
                                        }
                                    }
                                    if (sr == null)
                                    {
                                        itemName = "(null)";
                                    }
                                    TextRenderer.DrawText(g, "- " + itemName, GearGraphics.EquipDetailFont2, new Point(60, picHeight), ((SolidBrush)brush).Color, TextFormatFlags.NoPadding);
                                    picHeight += 15;
                                }
                            }
                            picHeight = tempHeight + 50;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < this.SetItem.CompleteCount; ++i)
                {
                    TextRenderer.DrawText(g, "(없음)", GearGraphics.EquipMDMoris9Font, new Point(14, picHeight), ((SolidBrush)GearGraphics.Equip22BrushGray).Color, TextFormatFlags.NoPadding);
                    GearGraphics.DrawString(g, "미착용", GearGraphics.EquipMDMoris9Font, new Dictionary<string, Color>() { { string.Empty, ((SolidBrush)GearGraphics.Equip22BrushGray).Color } }, 30, 283, ref picHeight, 15, Text.TextAlignment.Right);
                }
            }

            picHeight += 5;
            AddLines(0, 6, ref picHeight);

            //if (!this.SetItem.ExpandToolTip)
            if (true)
            {
                picHeight += 6;
                RenderEffect(g, specialPetSetEffectName, ref picHeight);
            }
            picHeight += 11;

            format.Dispose();
            g.Dispose();
            return setBitmap;
        }

        public static string Compact(Graphics g, string text, int width) // https://www.codeproject.com/Articles/37503/Auto-Ellipsis
        {
            Size s = TextRenderer.MeasureText(g, text, GearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);

            // control is large enough to display the whole text 
            if (s.Width <= width)
                return text;

            int len = 0;
            int seg = text.Length;
            string fit = "";

            // find the longest string that fits into
            // the control boundaries using bisection method 
            while (seg > 1)
            {
                seg -= seg / 2;

                int left = len + seg;

                if (left > text.Length)
                    continue;

                // build and measure a candidate string with ellipsis
                string tst = text.Substring(0, left) + "..";

                s = TextRenderer.MeasureText(g, tst, GearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);

                // candidate string fits into control boundaries, 
                // try a longer string
                // stop when seg <= 1 
                if (s.Width <= width)
                {
                    len += seg;
                    fit = tst;
                }
            }   

            return fit;
        }

        private Bitmap RenderEffectPart(bool specialPetSetEffectName, out int picHeight)
        {
            Bitmap effBitmap = new Bitmap(298, DefaultPicHeight);
            Graphics g = Graphics.FromImage(effBitmap);
            picHeight = 9;
            RenderEffect(g, specialPetSetEffectName, ref picHeight);
            picHeight += 11;
            g.Dispose();
            return effBitmap;
        }

        /// <summary>
        /// 绘制套装属性。
        /// </summary>
        private void RenderEffect(Graphics g, bool specialPetSetEffectName, ref int picHeight)
        {
            foreach (KeyValuePair<int, SetItemEffect> effect in this.SetItem.Effects)
            {
                string effTitle;
                bool worldSetEff = false;
                int dx = 76;
                //Brush brush = effect.Value.Enabled ? Brushes.White : GearGraphics.GrayBrush2;
                var color = effect.Value.Enabled ? Color.White : ((SolidBrush)GearGraphics.Equip22BrushDarkGray).Color;
                if (this.SetItem.SetItemID < 0)
                {
                    effTitle = $"[ 월드 내 중복 착용 효과 ({effect.Key} / {this.SetItem.CompleteCount}) ]";
                    worldSetEff = true ;
                }
                else if (specialPetSetEffectName && this.SetItem.SetItemName.EndsWith(" 세트"))
                {
                    effTitle = $"{Regex.Replace(this.SetItem.SetItemName, " 세트$", "")} {effect.Key}세트 효과";
                }
                else
                {
                    effTitle = effect.Key + "세트효과";
                }
                TextRenderer.DrawText(g, effTitle, GearGraphics.EquipMDMoris9Font, new Point(14 - (worldSetEff ? 1 : 0), picHeight), color, TextFormatFlags.NoPadding);
                if (worldSetEff)
                {
                    picHeight += 15;
                    dx = 0;
                }

                //T116 合并套装
                var props = IsCombineProperties ? Gear.CombineProperties(effect.Value.PropsV5) : effect.Value.PropsV5;
                foreach (KeyValuePair<GearPropType, object> prop in props)
                {
                    if (prop.Key == GearPropType.Option)
                    {
                        List<Potential> ops = (List<Potential>)prop.Value;
                        foreach (Potential p in ops)
                        {
                            GearGraphics.DrawString(g, p.ConvertSummary(), GearGraphics.EquipMDMoris9Font, new Dictionary<string, Color>() { { string.Empty, color } }, 14 + dx, 290, ref picHeight, 15);
                        }
                    }
                    else if (prop.Key == GearPropType.OptionToMob)
                    {
                        List<SetItemOptionToMob> ops = (List<SetItemOptionToMob>)prop.Value;
                        foreach (SetItemOptionToMob p in ops)
                        {
                            GearGraphics.DrawPlainText(g, p.ConvertSummary(), GearGraphics.EquipMDMoris9Font, color, 14 + dx, 290, ref picHeight, 15);
                        }
                    }
                    else if (prop.Key == GearPropType.activeSkill)
                    {
                        List<SetItemActiveSkill> ops = (List<SetItemActiveSkill>)prop.Value;
                        foreach (SetItemActiveSkill p in ops)
                        {
                            StringResult sr;
                            if (StringLinker == null || !StringLinker.StringSkill.TryGetValue(p.SkillID, out sr))
                            {
                                sr = new StringResult();
                                sr.Name = p.SkillID.ToString();
                            }
                            string summary = $"[{sr.Name.Replace(Environment.NewLine, "")}] 스킬 사용 가능";
                            GearGraphics.DrawPlainText(g, summary, GearGraphics.EquipMDMoris9Font, color, 14 + dx, 290, ref picHeight, 15);
                        }
                    }
                    else if (prop.Key == GearPropType.bonusByTime)
                    {
                        var ops = (List<SetItemBonusByTime>)prop.Value;
                        foreach (SetItemBonusByTime p in ops)
                        {
                            GearGraphics.DrawPlainText(g, $"{p.TermStart}小时后", GearGraphics.EquipMDMoris9Font, color, 10, 290, ref picHeight, 15);
                            foreach (var bonusProp in p.Props)
                            {
                                var summary = ItemStringHelper.GetGearPropString(bonusProp.Key, Convert.ToInt32(bonusProp.Value));
                                GearGraphics.DrawPlainText(g, summary, GearGraphics.EquipMDMoris9Font, color, 14 + dx, 290, ref picHeight, 15);
                            }
                        }
                    }
                    else
                    {
                        var summary = ItemStringHelper.GetGearPropString(prop.Key, Convert.ToInt32(prop.Value)).Replace(":","");
                        GearGraphics.DrawPlainText(g, summary, GearGraphics.EquipMDMoris9Font, color, 14 + dx, 290, ref picHeight, 15);
                    }
                }
                picHeight += 7;
            }
        }

        private void DrawCategory(Graphics g, string text, int picH)
        {
            List<string> categories = new List<string>();

            categories.Add(text);

            if (categories.Count <= 0) return;

            var font = GearGraphics.EquipMDMoris9FontBold;
            var ww = res["category_w"].Image.Width;
            var ew = res["category_e"].Image.Width;
            var ch = res["category_c"].Image.Height;
            var sp = 283;

            for (int i = categories.Count - 1; i >= 0; i--)
            {
                var length = TextRenderer.MeasureText(g, categories[i].Replace("#$g", ""), font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;

                g.DrawImage(res["category_w"].Image, sp - ew - length - ww, picH);
                g.FillRectangle(res["category_c"], sp - ew - length, picH, length, ch);
                picH += 2;
                GearGraphics.DrawString(g, categories[i], font, new Dictionary<string, Color>() { { "$g", ((SolidBrush)GearGraphics.Equip22BrushGray).Color } }, sp - ew - length, 290, ref picH, 0);
                picH -= 2;
                g.DrawImage(res["category_e"].Image, sp - ew, picH);

                sp -= (3 + ew + length + ww);
            }
        }
    }
}
