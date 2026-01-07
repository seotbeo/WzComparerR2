using System;
using System.Collections.Generic;
using System.Linq;
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
using WzComparerR2.AvatarCommon;

namespace WzComparerR2.CharaSimControl
{
    public class ItemTooltipRender2 : TooltipRender
    {
        public ItemTooltipRender2()
        {
        }

        private Item item;

        public Item Item
        {
            get { return item; }
            set { item = value; }
        }

        public override object TargetItem
        {
            get
            {
                return this.item;
            }
            set
            {
                this.item = value as Item;
            }
        }


        public bool LinkRecipeInfo { get; set; }
        public bool LinkRecipeItem { get; set; }
        public bool ShowLevelOrSealed { get; set; }
        public bool ShowNickTag { get; set; }
        public bool ShowCashPurchasePrice { get; set; }
        public bool CompareMode { get; set; } = false;
        public int CosmeticHairColor { get; set; }
        public int CosmeticFaceColor { get; set; }
        public int LoadedCommoditiesSlot { get; set; } = 0;
        public bool ShowDamageSkin { get; set; }
        public bool ShowDamageSkinID { get; set; }
        public bool UseMiniSizeDamageSkin { get; set; }
        public bool AlwaysUseMseaFormatDamageSkin { get; set; }
        public bool DisplayUnitOnSingleLine { get; set; }
        public long DamageSkinNumber { get; set; }
        private bool WillDrawNickTag { get; set; }
        private int DLeft { get; set; }
        private int DTop { get; set; }
        private int DLeft_Set { get; set; }
        private int DTop_Set { get; set; }
        private Wz_Node NickResNode { get; set; }
        private Bitmap ItemSample { get; set; }

        public TooltipRender LinkRecipeInfoRender { get; set; }
        public TooltipRender LinkRecipeGearRender { get; set; }
        public TooltipRender LinkRecipeItemRender { get; set; }
        public TooltipRender LinkDamageSkinRender { get; set; }
        public TooltipRender FamiliarRender { get; set; }
        public TooltipRender SetItemRender { get; set; }
        public TooltipRender CashPackageRender { get; set; }
        private AvatarCanvasManager avatar { get; set; }

        public override Bitmap Render()
        {
            if (this.item == null)
            {
                return null;
            }
            InitSampleResources();
            //绘制道具
            int picHeight;
            Bitmap itemBmp = RenderItem(out picHeight);
            List<Bitmap> recipeInfoBmps = new();
            List<Bitmap> recipeItemBmps = new();
            Bitmap setItemBmp = null;
            Bitmap levelBmp = null;
            int levelHeight = 0;
            this.DLeft = 0;
            this.DTop = 0;
            this.DLeft_Set = 0;
            this.DTop_Set = 0;
            if (this.ShowLevelOrSealed)
            {
                levelBmp = RenderLevel(out levelHeight);
            }

            if (this.item.ItemID / 10000 == 910)
            {
                Wz_Node itemNode = PluginBase.PluginManager.FindWz(string.Format(@"Item\Special\{0:D4}.img\{1}", this.item.ItemID / 10000, this.item.ItemID), this.SourceWzFile);
                Wz_Node cashPackageNode = PluginBase.PluginManager.FindWz(string.Format(@"Etc\CashPackage.img\{0}", this.item.ItemID), this.SourceWzFile);
                CashPackage cashPackage = CashPackage.CreateFromNode(itemNode, cashPackageNode, PluginBase.PluginManager.FindWz, this.SourceWzFile);
                return RenderCashPackage(cashPackage);
            }

            Action<int> AppendGearOrItem = (int itemID) =>
            {
                int itemIDClass = itemID / 1000000;
                if (itemIDClass == 1) //通过ID寻找装备
                {
                    Wz_Node charaWz = PluginManager.FindWz(Wz_Type.Character, this.SourceWzFile);
                    if (charaWz != null)
                    {
                        string imgName = itemID.ToString("d8") + ".img";
                        foreach (Wz_Node node0 in charaWz.Nodes)
                        {
                            Wz_Node imgNode = node0.FindNodeByPath(imgName, true);
                            if (imgNode != null)
                            {
                                Gear gear = Gear.CreateFromNode(imgNode, path => PluginManager.FindWz(path), this.SourceWzFile);
                                if (gear != null)
                                {
                                    gear.Props[GearPropType.timeLimited] = 0;
                                    long tuc, tucCnt;
                                    if (Item.Props.TryGetValue(ItemPropType.addTooltip_tuc, out tuc) && Item.Props.TryGetValue(ItemPropType.addTooltip_tucCnt, out tucCnt))
                                    {
                                        Wz_Node itemWz = PluginManager.FindWz(Wz_Type.Item, this.SourceWzFile);
                                        if (itemWz != null)
                                        {
                                            string imgClass = (tuc / 10000).ToString("d4") + ".img\\" + tuc.ToString("d8") + "\\info";
                                            foreach (Wz_Node node1 in itemWz.Nodes)
                                            {
                                                Wz_Node infoNode = node1.FindNodeByPath(imgClass, true);
                                                if (infoNode != null)
                                                {
                                                    gear.Upgrade(infoNode, (int)tucCnt);

                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    recipeItemBmps.Add(RenderLinkRecipeGear(gear));
                                }

                                break;
                            }
                        }
                    }
                }
                else if (itemIDClass >= 2 && itemIDClass <= 5) //通过ID寻找道具
                {
                    Wz_Node itemWz = PluginManager.FindWz(Wz_Type.Item, this.SourceWzFile);
                    if (itemWz != null)
                    {
                        string imgClass = (itemID / 10000).ToString("d4") + ".img\\" + itemID.ToString("d8");
                        foreach (Wz_Node node0 in itemWz.Nodes)
                        {
                            Wz_Node imgNode = node0.FindNodeByPath(imgClass, true);
                            if (imgNode != null)
                            {
                                Item item = Item.CreateFromNode(imgNode, PluginManager.FindWz, this.SourceWzFile);
                                item.Props[ItemPropType.timeLimited] = 0;
                                if (item != null)
                                {
                                    recipeItemBmps.Add(RenderLinkRecipeItem(item));
                                }

                                break;
                            }
                        }
                    }
                }
            };

            //图纸相关
            if (this.item.Recipes.Count > 0)
            {
                foreach (int recipeID in this.item.Recipes)
                {
                    int recipeSkillID = recipeID / 10000;
                    Recipe recipe = null;
                    //寻找配方
                    Wz_Node recipeNode = PluginBase.PluginManager.FindWz(string.Format(@"Skill\Recipe_{0}.img\{1}", recipeSkillID, recipeID), this.SourceWzFile);
                    if (recipeNode != null)
                    {
                        recipe = Recipe.CreateFromNode(recipeNode);
                    }
                    //生成配方图像
                    if (recipe != null)
                    {
                        if (this.LinkRecipeInfo)
                        {
                            recipeInfoBmps.Add(RenderLinkRecipeInfo(recipe));
                        }

                        if (this.LinkRecipeItem)
                        {
                            int itemID = recipe.MainTargetItemID;
                            AppendGearOrItem(itemID);
                        }
                    }
                }
            }

            long value;
            if (this.item.Props.TryGetValue(ItemPropType.dressUpgrade, out value))
            {
                long itemID = value;
                AppendGearOrItem((int)itemID);
            }
            if (this.item.Props.TryGetValue(ItemPropType.tamingMob, out value))
            {
                long itemID = value;
                AppendGearOrItem((int)itemID);
            }

            if (this.item.AddTooltips.Count > 0)
            {
                foreach (int itemID in item.AddTooltips)
                {
                    AppendGearOrItem(itemID);
                }
            }

            if (this.item.Props.TryGetValue(ItemPropType.setItemID, out long setID))
            {
                SetItem setItem;
                if (CompareMode)
                {
                    setItem = CharaSimLoader.LoadSetItem((int)setID, this.SourceWzFile);
                    if (setItem != null)
                        setItemBmp = RenderSetItem(setItem);
                }
                else if (CharaSimLoader.LoadedSetItems.TryGetValue((int)setID, out setItem))
                {
                    setItemBmp = RenderSetItem(setItem);
                }
            }

            if (this.item.DamageSkinID != null && ShowDamageSkin)
            {
                DamageSkin damageSkin = DamageSkin.CreateFromNode(PluginManager.FindWz($@"Etc\DamageSkin.img\{item.DamageSkinID}", this.SourceWzFile), PluginManager.FindWz) ?? DamageSkin.CreateFromNode(PluginManager.FindWz($@"Effect\DamageSkin.img\{item.DamageSkinID}", this.SourceWzFile), PluginManager.FindWz);
                if (damageSkin != null)
                {
                    setItemBmp = RenderDamageSkin(damageSkin);
                }
            }
            
            if (this.item.FamiliarID != null)
            {
                Familiar familiar = Familiar.CreateFromNode(PluginManager.FindWz($@"Character\Familiar\{item.FamiliarID}.img", this.SourceWzFile), PluginManager.FindWz, this.SourceWzFile);
                if (familiar != null)
                {
                    setItemBmp = RenderFamiliar(familiar);
                    familiar.Dispose();
                }
            }

            //计算布局
            Size totalSize = new Size(itemBmp.Width, picHeight);
            Point recipeInfoOrigin = Point.Empty;
            Point recipeItemOrigin = Point.Empty;
            Point setItemOrigin = Point.Empty;
            Point levelOrigin = Point.Empty;

            if (recipeItemBmps.Count > 0)
            {
                // layout:
                //   item        |  recipeItem
                //   recipeInfo  |
                recipeItemOrigin.X = totalSize.Width;
                totalSize.Width += recipeItemBmps.Max(bmp => bmp.Width);

                if (recipeInfoBmps.Count > 0)
                {
                    recipeInfoOrigin.X = itemBmp.Width - recipeInfoBmps.Max(bmp => bmp.Width);
                    recipeInfoOrigin.Y = picHeight;
                    totalSize.Height = Math.Max(picHeight + recipeInfoBmps.Sum(bmp => bmp.Height), recipeItemBmps.Sum(bmp => bmp.Height));
                }
                else
                {
                    totalSize.Height = Math.Max(picHeight, recipeItemBmps.Sum(bmp => bmp.Height));
                }
            }
            else if (recipeInfoBmps.Count > 0)
            {
                // layout:
                //   item  |  recipeInfo
                totalSize.Width += recipeInfoBmps.Max(bmp => bmp.Width);
                totalSize.Height = Math.Max(picHeight, recipeInfoBmps.Sum(bmp => bmp.Height));
                recipeInfoOrigin.X = itemBmp.Width;
            }
            if (setItemBmp != null)
            {
                this.DLeft = Math.Max(DLeft_Set - totalSize.Width, 0);
                this.DTop = this.DTop_Set;

                setItemOrigin = new Point(totalSize.Width - this.DLeft_Set, 0 - this.DTop_Set);
                totalSize.Width += setItemBmp.Width;
                totalSize.Height = Math.Max(totalSize.Height, setItemBmp.Height);
            }
            if (levelBmp != null)
            {
                levelOrigin = new Point(totalSize.Width, 0);
                totalSize.Width += levelBmp.Width;
                totalSize.Height = Math.Max(totalSize.Height, levelHeight);
            }

            //开始绘制
            totalSize.Width += this.DLeft;
            totalSize.Height += this.DTop;
            Bitmap tooltip = new Bitmap(totalSize.Width, totalSize.Height);
            Graphics g = Graphics.FromImage(tooltip);

            if (itemBmp != null)
            {
                //绘制背景区域
                GearGraphics.DrawNewTooltipBack(g, this.DLeft, this.DTop, itemBmp.Width, picHeight);
                //复制图像
                g.DrawImage(itemBmp, this.DLeft, this.DTop, new Rectangle(0, 0, itemBmp.Width, picHeight), GraphicsUnit.Pixel);
                //左上角
                g.DrawImage(Resource.UIToolTip_img_Item_Frame2_cover, 3, 3);

                if (this.ShowObjectID)
                {
                    GearGraphics.DrawGearDetailNumber(g, 3 + this.DLeft, 3 + this.DTop, item.ItemID.ToString("d8"), true);
                }
            }

            //绘制配方
            if (recipeInfoBmps.Count > 0)
            {
                for (int i = 0, y = recipeInfoOrigin.Y; i < recipeInfoBmps.Count; i++)
                {
                    g.DrawImage(recipeInfoBmps[i], recipeInfoOrigin.X + this.DLeft, y + this.DTop,
                        new Rectangle(Point.Empty, recipeInfoBmps[i].Size), GraphicsUnit.Pixel);
                    y += recipeInfoBmps[i].Height;
                }
            }

            //绘制产出道具
            if (recipeItemBmps.Count > 0)
            {
                for (int i = 0, y = recipeItemOrigin.Y; i < recipeItemBmps.Count; i++)
                {
                    g.DrawImage(recipeItemBmps[i], recipeItemOrigin.X + this.DLeft, y + this.DTop,
                        new Rectangle(Point.Empty, recipeItemBmps[i].Size), GraphicsUnit.Pixel);
                    y += recipeItemBmps[i].Height;
                }
            }

            //绘制套装
            if (setItemBmp != null)
            {
                g.DrawImage(setItemBmp, setItemOrigin.X + this.DLeft, setItemOrigin.Y + this.DTop,
                    new Rectangle(Point.Empty, setItemBmp.Size), GraphicsUnit.Pixel);
            }

            if (levelBmp != null)
            {
                //绘制背景区域
                GearGraphics.DrawNewTooltipBack(g, levelOrigin.X + this.DLeft, levelOrigin.Y + this.DTop, levelBmp.Width, levelHeight);
                //复制图像
                g.DrawImage(levelBmp, levelOrigin.X + this.DLeft, levelOrigin.Y + this.DTop, new Rectangle(0, 0, levelBmp.Width, levelHeight), GraphicsUnit.Pixel);
            }

            if (itemBmp != null)
                itemBmp.Dispose();
            if (recipeInfoBmps.Count > 0)
                recipeInfoBmps.ForEach(bmp => bmp.Dispose());
            if (recipeItemBmps.Count > 0)
                recipeItemBmps.ForEach(bmp => bmp.Dispose());
            if (setItemBmp != null)
                setItemBmp.Dispose();
            if (levelBmp != null)
                levelBmp.Dispose();

            g.Dispose();
            return tooltip;
        }

        private Bitmap RenderItem(out int picH)
        {
            StringFormat format = (StringFormat)StringFormat.GenericDefault.Clone();
            picH = 0;
            long value;
            int intvalue;

            //物品标题
            StringResult sr;
            if (StringLinker == null || !StringLinker.StringItem.TryGetValue(item.ItemID, out sr))
            {
                sr = new StringResult();
                sr.Name = "(null)";
            }
            string itemName = sr.Name.Replace(Environment.NewLine, "");
            string nameAdd = item.ItemID / 10000 == 313 || item.ItemID / 10000 == 501 ? "OFF" : null;
            if (!string.IsNullOrEmpty(nameAdd))
            {
                itemName += " (" + nameAdd + ")";
            }

            // calculate image width
            const int DefualtWidth = 290;
            int tooltipWidth = DefualtWidth;

            if (int.TryParse(sr["fixWidth"], out int fixWidth) && fixWidth > 0)
            {
                tooltipWidth = fixWidth;
            }
            {
                using (Bitmap dummyImg = new Bitmap(1, 1))
                using (Graphics tempG = Graphics.FromImage(dummyImg))
                {
                    SizeF titleSize = TextRenderer.MeasureText(tempG, itemName, GearGraphics.ItemNameFont2, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix);
                    titleSize.Width += 12.5F;
                    if (titleSize.Width > tooltipWidth)
                    {
                        tooltipWidth = (int)Math.Ceiling(titleSize.Width);
                    }

                    if (CompareMode && tooltipWidth - titleSize.Width < 32)
                    {
                        picH += 14;
                    }
                }
            }

            Bitmap tooltip = new Bitmap(tooltipWidth, DefaultPicHeight);
            Graphics g = Graphics.FromImage(tooltip);
            picH += 10;

            //绘制标题
            bool hasPart2 = false;
            format.Alignment = StringAlignment.Center;
            TextRenderer.DrawText(g, itemName, GearGraphics.ItemNameFont2, new Point(tooltip.Width + 2, picH), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
            picH += 21;

            if (Item.Props.TryGetValue(ItemPropType.wonderGrade, out value) && value > 0)
            {
                switch (value)
                {
                    case 1:
                        TextRenderer.DrawText(g, "원더 블랙", GearGraphics.EquipDetailFont, new Point(tooltip.Width + 2, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.HorizontalCenter);
                        break;
                    case 4:
                        TextRenderer.DrawText(g, "루나 스윗", GearGraphics.EquipDetailFont, new Point(tooltip.Width + 2, picH), GearGraphics.itemPinkColor, TextFormatFlags.HorizontalCenter);
                        break;
                    case 5:
                        TextRenderer.DrawText(g, "루나 드림", GearGraphics.EquipDetailFont, new Point(tooltip.Width + 2, picH), ((SolidBrush)GearGraphics.BlueBrush).Color, TextFormatFlags.HorizontalCenter);
                        break;
                    case 6:
                        TextRenderer.DrawText(g, "루나 쁘띠", GearGraphics.EquipDetailFont, new Point(tooltip.Width + 2, picH), GearGraphics.itemPurpleColor, TextFormatFlags.HorizontalCenter);
                        break;
                    default:
                        picH -= 15;
                        break;
                }
                picH += 15;
            }
            else if (Item.Props.TryGetValue(ItemPropType.BTSLabel, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "BTS 라벨", GearGraphics.EquipDetailFont, new Point(tooltip.Width + 2, picH), Color.FromArgb(187, 102, 238), TextFormatFlags.HorizontalCenter);
                picH += 15;
            }
            else if (Item.Props.TryGetValue(ItemPropType.BLACKPINKLabel, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "BLACKPINK 라벨", GearGraphics.EquipDetailFont, new Point(tooltip.Width + 2, picH), Color.FromArgb(255, 136, 170), TextFormatFlags.HorizontalCenter);
                picH += 15;
            }

            //额外特性
            var attrList = GetItemAttributeString();
            if (attrList.Count > 0)
            {
                var font = GearGraphics.ItemDetailFont;
                string attrStr = null;
                for (int i = 0; i < attrList.Count; i++)
                {
                    var newStr = (attrStr != null ? (attrStr + ", ") : null) + attrList[i];
                    if (TextRenderer.MeasureText(g, newStr, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width > tooltip.Width - 7 || (attrList[i].Contains('\n') && attrStr != null))
                    {
                        TextRenderer.DrawText(g, attrStr, GearGraphics.ItemDetailFont, new Point(tooltip.Width + 2, picH), ((SolidBrush)GearGraphics.OrangeBrush4).Color, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
                        picH += 16;
                        attrStr = attrList[i];
                    }
                    else
                    {
                        attrStr = newStr;
                    }
                }
                if (!string.IsNullOrEmpty(attrStr))
                {
                    foreach (string attrLine in attrStr.Split('\n'))
                    {
                        TextRenderer.DrawText(g, attrLine, GearGraphics.ItemDetailFont, new Point(tooltip.Width + 2, picH), ((SolidBrush)GearGraphics.OrangeBrush4).Color, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
                        picH += 19;
                    }
                    picH -= 3;
                }
                hasPart2 = true;
            }

            string expireTime = null;
            if (item.TimeLimited)
            {
                DateTime time = DateTime.Now.AddDays(7d);
                if (!item.Cash)
                {
                    expireTime = time.ToString("yyyy년 M월 d일 HH시 mm분까지 사용가능");
                }
                else
                {
                    expireTime = time.ToString("yyyy년 M월 d일 HH시까지 사용가능");
                }
            }
            else if (item.ConsumableFrom != null || item.EndUseDate != null)
            {
                expireTime = "";
                if (item.ConsumableFrom != null)
                {
                    expireTime += string.Format("\n{0}년 {1}월 {2}일 {3:D2}시 {4:D2}분부터 사용가능", Convert.ToInt32(item.ConsumableFrom.Substring(0, 4)), Convert.ToInt32(item.ConsumableFrom.Substring(4, 2)), Convert.ToInt32(item.ConsumableFrom.Substring(6, 2)), Convert.ToInt32(item.ConsumableFrom.Substring(8, 2)), Convert.ToInt32(item.ConsumableFrom.Substring(10, 2)));
                }
                if (item.EndUseDate != null)
                {
                    expireTime += string.Format("\n{0}년 {1}월 {2}일 {3:D2}시 {4:D2}분까지 사용가능", Convert.ToInt32(item.EndUseDate.Substring(0, 4)), Convert.ToInt32(item.EndUseDate.Substring(4, 2)), Convert.ToInt32(item.EndUseDate.Substring(6, 2)), Convert.ToInt32(item.EndUseDate.Substring(8, 2)), Convert.ToInt32(item.EndUseDate.Substring(10, 2)));
                }
            }
            else if ((item.Props.TryGetValue(ItemPropType.permanent, out value) && value != 0) || (item.ItemID / 10000 == 500 && item.Props.TryGetValue(ItemPropType.life, out value) && value == 0))
            {
                picH -= 3;
                if (value == 0)
                {
                    value = 1;
                }
                expireTime = ItemStringHelper.GetItemPropString(ItemPropType.permanent, value);
            }
            else if (item.ItemID / 10000 == 500 && item.Props.TryGetValue(ItemPropType.limitedLife, out value) && value > 0)
            {
                picH -= 3;
                expireTime = string.Format("마법의 시간: {0}시간 {1}분", value / 3600, (value % 3600) / 60);
            }
            else if (item.ItemID / 10000 == 500 && item.Props.TryGetValue(ItemPropType.life, out value) && value > 0)
            {
                picH -= 3;
                DateTime time = DateTime.Now.AddDays(value);
                expireTime = time.ToString("마법의 시간: yyyy년 M월 d일 HH시까지");
            }
            if (!string.IsNullOrEmpty(expireTime))
            {
                if (attrList.Count > 0)
                {
                    picH += 3;
                }
                //g.DrawString(expireTime, GearGraphics.ItemDetailFont, Brushes.White, tooltip.Width / 2, picH, format);
                foreach (string expireTimeLine in expireTime.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    TextRenderer.DrawText(g, expireTimeLine, GearGraphics.ItemDetailFont, new Point(tooltip.Width + 2, picH), Color.White, TextFormatFlags.HorizontalCenter);
                    picH += 16;
                }
                if (expireTime.Contains('\n'))
                {
                    picH += 4;
                }
                hasPart2 = true;
            }

            if (hasPart2)
            {
                picH += 4;
            }

            //绘制图标
            picH += 1;
            int iconY = picH;
            int iconX = 14;
            g.DrawImage(Resource.UIToolTip_img_Item_ItemIcon_base, iconX, picH);
            if (item.Icon.Bitmap != null)
            {
                g.DrawImage(GearGraphics.EnlargeBitmap(item.Icon.Bitmap),
                iconX + 6 + (1 - item.Icon.Origin.X) * 2,
                picH + 6 + (33 - item.Icon.Origin.Y) * 2);
            }
            if (item.Cash)
            {
                Bitmap cashImg = null;
                Point cashOrigin = new Point(12, 12);

                if (item.Props.TryGetValue(ItemPropType.wonderGrade, out value) && value > 0)
                {
                    string resKey = $"CashShop_img_CashItem_label_{value + 3}";
                    cashImg = Resource.ResourceManager.GetObject(resKey) as Bitmap;
                }
                else if (Item.Props.TryGetValue(ItemPropType.BTSLabel, out value) && value > 0)
                {
                    cashImg = Resource.CashShop_img_CashItem_label_10;
                    cashOrigin = new Point(cashImg.Width, cashImg.Height);
                }
                else if (Item.Props.TryGetValue(ItemPropType.BLACKPINKLabel, out value) && value > 0)
                {
                    cashImg = Resource.CashShop_img_CashItem_label_11;
                    cashOrigin = new Point(cashImg.Width, cashImg.Height);
                }
                if (cashImg == null) //default cashImg
                {
                    cashImg = Resource.CashItem_0;
                }

                g.DrawImage(GearGraphics.EnlargeBitmap(cashImg),
                    iconX + 6 + 68 - cashOrigin.X * 2 - 2,
                    picH + 6 + 68 - cashOrigin.Y * 2 - 2);
            }
            g.DrawImage(Resource.UIToolTip_img_Item_ItemIcon_new, iconX + 7, picH + 7);
            g.DrawImage(Resource.UIToolTip_img_Item_ItemIcon_cover, iconX + 4, picH + 4); //绘制左上角cover

            value = 0;
            if (item.Props.TryGetValue(ItemPropType.reqLevel, out value) && value > 0)
            {
                //picH += 4;
                g.DrawImage(Resource.ToolTip_Equip_Can_reqLEV, 100, picH);
                GearGraphics.DrawGearDetailNumber(g, 150, picH, value.ToString(), true);
                picH += 15;
            }
            else
            {
                picH += 3;
            }

            int right = tooltip.Width - 30;

            string desc = null;
            if (item.Level > 0)
            {
                desc += $"[LV.{item.Level}] ";
            }
            desc += sr.Desc;
            if (item.ItemID / 10000 == 500)
            {
                if (item.Props.TryGetValue(ItemPropType.wonderGrade, out value) && value > 0)
                {
                    if (item.Props.TryGetValue(ItemPropType.setItemID, out long setID))
                    {
                        SetItem setItem;
                        if ((!CompareMode && CharaSimLoader.LoadedSetItems.TryGetValue((int)setID, out setItem))
                            || (CompareMode && (setItem = CharaSimLoader.LoadSetItem((int)setID, this.SourceWzFile)) != null))
                        {
                            string wonderGradeString = null;
                            string setItemName = setItem.SetItemName;
                            string setSkillName = "";
                            switch (value)
                            {
                                case 1:
                                    wonderGradeString = "원더 블랙";
                                    foreach (KeyValuePair<GearPropType, object> prop in setItem.Effects.Values.SelectMany(f => f.PropsV5))
                                    {
                                        if (prop.Key == GearPropType.activeSkill)
                                        {
                                            SetItemActiveSkill p = ((List<SetItemActiveSkill>)prop.Value)[0];
                                            StringResult sr2;
                                            if (StringLinker == null || !StringLinker.StringSkill.TryGetValue(p.SkillID, out sr2))
                                            {
                                                sr2 = new StringResult();
                                                sr2.Name = p.SkillID.ToString();
                                            }
                                            setSkillName = Regex.Replace(sr2.Name, " Lv.\\d", "");
                                            break;
                                        }
                                    }
                                    break;
                                case 4:
                                    wonderGradeString = "루나 스윗";
                                    setSkillName = "루나 스윗";
                                    break;
                                case 5:
                                    wonderGradeString = "루나 드림";
                                    setSkillName = "루나 드림";
                                    break;
                            }
                            if (wonderGradeString != null)
                            {
                                desc += $"\n#c{wonderGradeString}# 등급의 #c{setItemName}# 펫 장착시 #c{setSkillName}# 세트 효과를 얻게 됩니다. (최대 3단계)\n세트 효과는 장착한 #c{setItemName}# 펫의 종류에 따라 3세트까지 강화됩니다.";
                            }
                        }
                    }
                }
                desc += "\n#c스킬:메소 줍기";
                if (item.Props.TryGetValue(ItemPropType.pickupItem, out value) && value > 0)
                {
                    desc += ", 아이템 줍기";
                }
                if (item.Props.TryGetValue(ItemPropType.longRange, out value) && value > 0)
                {
                    desc += ", 이동반경 확대";
                }
                if (item.Props.TryGetValue(ItemPropType.sweepForDrop, out value) && value > 0)
                {
                    desc += ", 자동 줍기";
                }
                if (item.Props.TryGetValue(ItemPropType.pickupAll, out value) && value > 0)
                {
                    desc += ", 소유권 없는 아이템&메소 줍기";
                }
                if (item.Props.TryGetValue(ItemPropType.consumeHP, out value) && value > 0)
                {
                    desc += ", HP 물약충전";
                }
                if (item.Props.TryGetValue(ItemPropType.consumeMP, out value) && value > 0)
                {
                    desc += ", MP 물약충전";
                }
                if (item.Props.TryGetValue(ItemPropType.autoBuff, out value) && value > 0)
                {
                    desc += ", 버프 스킬 자동 사용";
                }
                if (item.Props.TryGetValue(ItemPropType.giantPet, out value) && value > 0)
                {
                    desc += ", 펫 자이언트 스킬";
                }
                desc += "#";
            }
            desc = ReplaceDescTags(desc);

            if (!string.IsNullOrEmpty(desc))
            {
                GearGraphics.DrawString(g, desc, GearGraphics.ItemDetailFont2, 100, right, ref picH, 16);
            }
            if (!string.IsNullOrEmpty(sr.AutoDesc))
            {
                GearGraphics.DrawString(g, sr.AutoDesc, GearGraphics.ItemDetailFont2, 100, right, ref picH, 16);
            }
            if (item.Props.TryGetValue(ItemPropType.tradeAvailable, out value) && value > 0)
            {
                string attr = ItemStringHelper.GetItemPropString(ItemPropType.tradeAvailable, value);
                if (!string.IsNullOrEmpty(attr))
                    GearGraphics.DrawString(g, "#c" + attr + "#", GearGraphics.ItemDetailFont2, 100, right, ref picH, 16);
            }
            if (item.Props.TryGetValue(ItemPropType.pointCost, out value) && value > 0)
            {
                picH += 16;
                GearGraphics.DrawString(g, "· " + value + " 포인트", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
            }
            if (item.Specs.TryGetValue(ItemSpecType.recipeValidDay, out value) && value > 0)
            {
                GearGraphics.DrawString(g, "( 제작 가능 기간 : " + value + "일 )", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
            }
            if (item.Specs.TryGetValue(ItemSpecType.recipeUseCount, out value) && value > 0)
            {
                GearGraphics.DrawString(g, "( 제작 가능 횟수 : " + value + "회 )", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
            }
            if (item.ItemID / 1000 == 5533)
            {
                GearGraphics.DrawString(g, "\n#c더블 클릭 시 미리보기에서 상자 속 아이템들을 3초마다 차례로 확인할 수 있습니다.\n\n캐시 보관함에서 더블 클릭하여 사용 가능하며, 상자는 교환할 수 없습니다.\n상자에서 획득한 보상품은 타인과 교환할 수 없습니다.#", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
            }
            if (item.Cash)
            {
                if (item.Props.TryGetValue(ItemPropType.noMoveToLocker, out value) && value > 0)
                {
                    GearGraphics.DrawString(g, "\n#c캐시 보관함으로 이동시킬 수 없는 아이템입니다.#", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
                }
                if (item.Props.TryGetValue(ItemPropType.onlyCash, out value) && value > 0)
                {
                    GearGraphics.DrawString(g, "\n#c넥슨캐시로만 구매할 수 있습니다.#", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
                }
                if (item.Props.TryGetValue(ItemPropType.cashTradeBlock, out value) && value > 0)
                {
                    GearGraphics.DrawString(g, "\n#c이 아이템은 넥슨캐시로 구매 후에도 타인과의 교환이 제한됩니다.#", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
                }
                else if ((!item.Props.TryGetValue(ItemPropType.tradeBlock, out value) || value == 0))
                {
                    if (!(item.ItemID / 10000 == 501 || item.ItemID / 10000 == 502 || item.ItemID / 10000 == 516 || item.ItemID / 1000 == 5157 || item.ItemID / 1000 == 5158))
                    {
                        GearGraphics.DrawString(g, "\n#c넥슨캐시로 구매하면 사용 전 1회에 한해 타인과 교환 할 수 있습니다.#", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
                    }
                }
            }
            if (item.Props.TryGetValue(ItemPropType.flatRate, out value) && value > 0)
            {
                GearGraphics.DrawString(g, "\n#c기간 정액제 아이템입니다.#", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
            }
            if (item.Props.TryGetValue(ItemPropType.noScroll, out value) && value > 0)
            {
                GearGraphics.DrawString(g, "#c펫 스킬 주문서와 펫작명하기를 사용할 수 없습니다.#", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
            }
            if (item.Props.TryGetValue(ItemPropType.noRevive, out value) && value > 0)
            {
                GearGraphics.DrawString(g, "#c생명의 물을 사용할 수 없습니다.#", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
            }

            if (item.ItemID / 10000 == 500)
            {
                Wz_Node petDialog = PluginManager.FindWz("String\\PetDialog.img\\" + item.ItemID, this.SourceWzFile);
                Dictionary<string, int> commandLev = new Dictionary<string, int>();
                foreach (Wz_Node commandNode in PluginManager.FindWz("Item\\Pet\\" + item.ItemID + ".img\\interact", this.SourceWzFile).Nodes)
                {
                    foreach (string command in petDialog?.Nodes[commandNode.Nodes["command"].GetValue<string>()].GetValueEx<string>(null)?.Split('|') ?? Enumerable.Empty<string>())
                    {
                        int l0;
                        if (!commandLev.TryGetValue(command, out l0))
                        {
                            commandLev.Add(command, commandNode.Nodes["l0"].GetValue<int>());
                        }
                        else
                        {
                            commandLev[command] = Math.Min(l0, commandNode.Nodes["l0"].GetValue<int>());
                        }
                    }
                }

                GearGraphics.DrawString(g, "[사용 가능한 명령어]", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
                foreach (int l0 in commandLev.Values.OrderBy(i => i).Distinct())
                {
                    GearGraphics.DrawString(g, "Lv. " + l0 + " 이상 : " + string.Join(", ", commandLev.Where(i => i.Value == l0).Select(i => i.Key).OrderBy(s => s)), GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
                }
                GearGraphics.DrawString(g, "Tip. 펫의 레벨이 15가 되면 특정 말을 하도록 시킬 수 있습니다. 펫이 하는 말은 다른 유저에게 보이지 않습니다.", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
                GearGraphics.DrawString(g, "#c예) /펫 [할 말]#", GearGraphics.ItemDetailFont, new Dictionary<string, Color>() { { "c", ((SolidBrush)GearGraphics.OrangeBrush4).Color } }, 100, right, ref picH, 16);
            }

            string incline = null;
            ItemPropType[] inclineTypes = new ItemPropType[]{
                    ItemPropType.charismaEXP,
                    ItemPropType.insightEXP,
                    ItemPropType.willEXP,
                    ItemPropType.craftEXP,
                    ItemPropType.senseEXP,
                    ItemPropType.charmEXP };

            string[] inclineString = new string[]{
                    "카리스마","통찰력","의지","손재주","감성","매력"};

            for (int i = 0; i < inclineTypes.Length; i++)
            {
                if (item.Props.TryGetValue(inclineTypes[i], out value) && value > 0)
                {
                    incline += ", " + inclineString[i] + " " + value;
                }
            }

            if (!string.IsNullOrEmpty(incline))
            {
                GearGraphics.DrawString(g, "#c장착 시 1회에 한해 " + incline.Substring(2) + "의 경험치를 얻을 수 있습니다.(일일제한, 최대치 초과 시 제외)#", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
            }

            picH += 3;

            string descLeftAlign = sr["desc_leftalign"];
            long minLev = 0, maxLev = 0;
            bool willDrawExp = item.Props.TryGetValue(ItemPropType.exp_minLev, out minLev) && item.Props.TryGetValue(ItemPropType.exp_maxLev, out maxLev);

            if (!string.IsNullOrEmpty(descLeftAlign) || item.CoreSpecs.Count > 0 || item.Sample.Bitmap != null || item.DamageSkinID != null || item.SamplePath != null || item.ShowCosmetic || this.WillDrawNickTag || willDrawExp)
            {
                if (picH < iconY + 84)
                {
                    picH = iconY + 84;
                }
                if (!string.IsNullOrEmpty(descLeftAlign))
                {
                    picH += 12;
                    GearGraphics.DrawString(g, descLeftAlign, GearGraphics.ItemDetailFont, 14, right, ref picH, 16);
                }
                if (item.CoreSpecs.Count > 0)
                {
                    g.DrawLine(Pens.White, 6, picH - 1, tooltip.Width - 7, picH - 1);
                    picH += 9;
                    foreach (KeyValuePair<ItemCoreSpecType, Wz_Node> p in item.CoreSpecs)
                    {
                        string coreSpec;
                        intvalue = 0;
                        switch (p.Key)
                        {
                            case ItemCoreSpecType.Ctrl_addMob:
                                StringResult srMob;
                                if (StringLinker == null || !StringLinker.StringMob.TryGetValue(Convert.ToInt32(p.Value.Nodes["mobID"].Value), out srMob))
                                {
                                    srMob = new StringResult();
                                    srMob.Name = "(null)";
                                }
                                foreach (Wz_Node addMobNode in p.Value.Nodes)
                                {
                                    if (int.TryParse(addMobNode.Text, out intvalue))
                                    {
                                        break;
                                    }
                                }
                                coreSpec = ItemStringHelper.GetItemCoreSpecString(ItemCoreSpecType.Ctrl_addMob, intvalue, srMob.Name);
                                break;

                            default:
                                try
                                {
                                    coreSpec = ItemStringHelper.GetItemCoreSpecString(p.Key, Convert.ToInt32(p.Value.Value), Convert.ToString(p.Value.Nodes["desc"]?.Value));
                                }
                                finally
                                {
                                }
                                break;
                        }
                        GearGraphics.DrawString(g, "· " + coreSpec, GearGraphics.ItemDetailFont, 14, right, ref picH, 16);
                    }
                }

                if (item.Sample.Bitmap != null)
                {
                    g.DrawImage(item.Sample.Bitmap, (tooltip.Width - item.Sample.Bitmap.Width) / 2, picH);
                    picH += item.Sample.Bitmap.Height;
                    picH += 2;
                }
                else if (item.DamageSkinID != null)
                {
                    Wz_Node sampleNode = PluginManager.FindWz($@"Etc\DamageSkin.img\{item.DamageSkinID}\sample", this.SourceWzFile);
                    if (sampleNode != null)
                    {
                        BitmapOrigin sample = BitmapOrigin.CreateFromNode(sampleNode, PluginManager.FindWz, this.SourceWzFile);
                        g.DrawImage(sample.Bitmap, (tooltip.Width - sample.Bitmap.Width) / 2, picH);
                        picH += sample.Bitmap.Height;
                        picH += 2;

                        this.ItemSample = new Bitmap(sample.Bitmap);
                    }
                }
                else if (this.item.Specs.TryGetValue(ItemSpecType.cosmetic, out value) && value > 0)
                {
                    if (this.avatar == null)
                    {
                        this.avatar = new AvatarCanvasManager(this.SourceWzFile);
                    }

                    this.avatar.SetCosmeticColor(this.CosmeticHairColor, this.CosmeticFaceColor);

                    if (value < 1000)
                    {
                        this.avatar.AddBodyFromSkin((int)value);
                    }
                    else
                    {
                        this.avatar.AddBodyFromSkin(2015);
                        this.avatar.AddHairOrFace((int)value, true);
                    }

                    this.avatar.AddGears([1042194, 1062153]);

                    var frame = this.avatar.GetBitmapOrigin();
                    if (frame.Bitmap != null)
                    {
                        picH += 9;
                        g.DrawImage(frame.Bitmap, tooltip.Width / 2 - frame.Origin.X, picH);
                        picH += frame.Bitmap.Height;
                        picH += 2;

                        if (this.ItemSample != null) this.ItemSample.Dispose();
                        this.ItemSample = new Bitmap(frame.Bitmap);
                    }

                    this.avatar.ClearCanvas();
                    frame.Bitmap.Dispose();
                }
                else if (item.SamplePath != null)
                {
                    Wz_Node sampleNode = PluginManager.FindWz(item.SamplePath, this.SourceWzFile);
                    // Workaround for KMST 1.2.1184
                    if (sampleNode == null && item.SamplePath.Contains("ChatEmoticon.img"))
                    {
                        sampleNode = PluginManager.FindWz(item.SamplePath.Replace("ChatEmoticon.img/", "ChatEmoticon.img/Emoticon/"), this.SourceWzFile);
                    }

                    if (sampleNode != null)
                    {
                        if (sampleNode?.Text == "effect")
                        {
                            Wz_Node effectNode = sampleNode.Nodes["0"];
                            BitmapOrigin effect = BitmapOrigin.CreateFromNode(effectNode, PluginManager.FindWz, this.SourceWzFile);
                            g.DrawImage(effect.Bitmap, 38 + (85 - effect.Bitmap.Width - 1) / 2, picH - 8 + (62 - effect.Bitmap.Height - 1) / 2);
                            picH += 73;
                        }
                        else
                        {
                            int sampleW = 15;
                            for (int i = 1; ; i++)
                            {
                                Wz_Node effectNode = sampleNode.FindNodeByPath(string.Format("{0}{1:D4}\\effect\\0", sampleNode.Text, i));
                                if (effectNode == null)
                                {
                                    break;
                                }

                                BitmapOrigin effect = BitmapOrigin.CreateFromNode(effectNode, PluginManager.FindWz, this.SourceWzFile);
                                if (sampleW + 87 >= tooltip.Width)
                                {
                                    picH += 62;
                                    sampleW = 15;
                                }
                                g.DrawImage(effect.Bitmap, sampleW + (85 - effect.Bitmap.Width - 1) / 2, picH + (62 - effect.Bitmap.Height - 1) / 2);
                                sampleW += 87;
                            }
                            picH += 62;
                        }
                    }
                }
                else if (this.NickResNode != null)
                {
                    //获取称号名称
                    string nickName = GearGraphics.GetNameTagString(sr);
                    GearGraphics.DrawNameTag(g, this.NickResNode, nickName, tooltip.Width, ref picH);
                    picH += 4;
                }

                if (minLev > 0 && maxLev > 0)
                {
                    long totalExp = 0;

                    for (int i = (int)minLev; i < (int)maxLev; i++)
                        totalExp += Character.ExpToNextLevel(i);

                    g.DrawLine(Pens.White, 6, picH, tooltip.Width - 7, picH);
                    picH += 8;

                    TextRenderer.DrawText(g, "총  경험치량 :" + totalExp, GearGraphics.ItemDetailFont2, new Point(10, picH), ((SolidBrush)GearGraphics.OrangeBrush4).Color, TextFormatFlags.NoPadding);
                    picH += 16;

                    TextRenderer.DrawText(g, "잔여 경험치량:" + totalExp, GearGraphics.ItemDetailFont2, new Point(10, picH), Color.Red, TextFormatFlags.NoPadding);
                    picH += 16;

                    string cantAccountSharable = null;
                    Wz_Node itemWz = PluginManager.FindWz(Wz_Type.Item, this.SourceWzFile);
                    if (itemWz != null)
                    {
                        string imgClass = (item.ItemID / 10000).ToString("d4") + ".img\\" + item.ItemID.ToString("d8");
                        foreach (Wz_Node node0 in itemWz.Nodes)
                        {
                            Wz_Node imgNode = node0.FindNodeByPath(imgClass, true);
                            if (imgNode != null)
                            {
                                cantAccountSharable = imgNode.FindNodeByPath("info\\cantAccountSharable\\tooltip").GetValueEx<string>(null);
                                break;
                            }
                        }
                    }

                    if (cantAccountSharable != null)
                    {
                        TextRenderer.DrawText(g, cantAccountSharable, GearGraphics.ItemDetailFont2, new Point(10, picH), ((SolidBrush)GearGraphics.SetItemNameBrush).Color, TextFormatFlags.NoPadding);
                        picH += 16;
                        picH += 16;
                    }
                }
            }


            //绘制配方需求
            if (item.Recipes.Count > 0)
            {
                long reqSkill, reqSkillLevel;
                if (!item.Specs.TryGetValue(ItemSpecType.reqSkill, out reqSkill))
                {
                    reqSkill = item.Recipes[0] / 10000 * 10000;
                }

                if (!item.Specs.TryGetValue(ItemSpecType.reqSkillLevel, out reqSkillLevel))
                {
                    reqSkillLevel = 1;
                }

                picH = Math.Max(picH, iconY + 107);
                g.DrawLine(Pens.White, 6, picH, 283, picH);//分割线
                picH += 10;
                TextRenderer.DrawText(g, "< 사용 제한조건 >", GearGraphics.ItemDetailFont, new Point(8, picH), ((SolidBrush)GearGraphics.SetItemNameBrush).Color, TextFormatFlags.NoPadding);
                picH += 17;

                //技能标题
                if (StringLinker == null || !StringLinker.StringSkill.TryGetValue((int)reqSkill, out sr))
                {
                    sr = new StringResult();
                    sr.Name = "(null)";
                }
                switch (sr.Name)
                {
                    case "장비제작": sr.Name = "장비 제작"; break;
                    case "장신구제작": sr.Name = "장신구 제작"; break;
                }
                TextRenderer.DrawText(g, string.Format("· {0} {1}레벨 이상", sr.Name, reqSkillLevel), GearGraphics.ItemDetailFont, new Point(13, picH), ((SolidBrush)GearGraphics.SetItemNameBrush).Color, TextFormatFlags.NoPadding);
                picH += 16;
                picH += 6;
            }

            if (ShowCashPurchasePrice)
            {
                List<string> priceList = new List<string>();
                if (CharaSimLoader.LoadedCommodityPricesByItemId[LoadedCommoditiesSlot].ContainsKey(item.ItemID))
                {
                    foreach (var i in CharaSimLoader.LoadedCommodityPricesByItemId[LoadedCommoditiesSlot][item.ItemID])
                    {
                        if (i.Price == 0) continue;
                        string currency = i.Meso ? "메소" : "캐시";
                        string rebootWorld = i.Reboot ? " (리부트 월드)" : "";
                        priceList.Add(string.Format("#$S - {0}개: {1} {2}{3}#", i.Count, ItemStringHelper.ToCJKNumberExpr(i.Price), currency, rebootWorld));
                    }
                }
                if (priceList.Count > 0)
                {
                    var itemPriceColorTable = new Dictionary<string, Color>()
                    {
                        { "$S", ((SolidBrush)GearGraphics.ItemPriceBrush).Color },
                    };
                    picH += 29;
                    switch (priceList.Count)
                    {
                        /*
                        case 1:
                            GearGraphics.DrawString(g, " - 판매가격: " + priceList[0].Replace(" - 1개: ", "").Replace(" - ", ""), GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
                            break;
                        */
                        default:
                            GearGraphics.DrawString(g, "#$S판매가격 #", GearGraphics.ItemDetailFont, itemPriceColorTable, 100, right, ref picH, 16);
                            foreach (var i in priceList)
                                GearGraphics.DrawString(g, i, GearGraphics.ItemDetailFont, itemPriceColorTable, 100, right, ref picH, 16);
                            break;
                    }
                }
            }

            picH = Math.Max(iconY + 103, picH + 15);
            return tooltip;
        }

        private List<string> GetItemAttributeString()
        {
            long value, value2;
            List<string> tags = new List<string>();

            if (item.Props.TryGetValue(ItemPropType.only, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.only, value));
            }
            if (item.Props.TryGetValue(ItemPropType.quest, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.quest, value));
            }
            if (item.Props.TryGetValue(ItemPropType.pquest, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.pquest, value));
            }
            if (item.Props.TryGetValue(ItemPropType.tradeBlock, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.tradeBlock, value));
            }
            if (item.Props.TryGetValue(ItemPropType.useTradeBlock, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.useTradeBlock, value));
            }
            else if (item.ItemID / 10000 == 501 || item.ItemID / 10000 == 502 || item.ItemID / 10000 == 516)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.tradeBlock, 1));
            }
            if (item.Props.TryGetValue(ItemPropType.accountSharable, out value) && value != 0)
            {
                if (item.Props.TryGetValue(ItemPropType.exp_minLev, out value2) && value2 != 0)
                {
                    tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.useTradeBlock, 1));
                }
                if (item.Props.TryGetValue(ItemPropType.sharableOnce, out value2) && value2 != 0)
                {
                    tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.sharableOnce, value2));
                }
                else
                {
                    tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.accountSharable, value));
                }
            }
            if (item.Props.TryGetValue(ItemPropType.exchangeableOnce, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.exchangeableOnce, value));
            }
            if (item.Props.TryGetValue(ItemPropType.multiPet, out value))
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.multiPet, value));
            }
            else if (item.ItemID / 10000 == 500)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.multiPet, 0));
            }
            if (item.Props.TryGetValue(ItemPropType.accountSharableAfterExchange, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.accountSharableAfterExchange, value));
            }
            if (item.Props.TryGetValue(ItemPropType.mintable, out value))
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.mintable, value));
            }

            return tags;
        }

        private Bitmap RenderDamageSkin(DamageSkin damageSkin)
        {
            TooltipRender renderer = this.LinkDamageSkinRender;
            if (renderer == null)
            {
                DamageSkinTooltipRenderer defaultRenderer = new DamageSkinTooltipRenderer();
                defaultRenderer.StringLinker = this.StringLinker;
                defaultRenderer.ShowObjectID = this.ShowDamageSkinID;
                defaultRenderer.UseMiniSize = this.UseMiniSizeDamageSkin;
                defaultRenderer.AlwaysUseMseaFormat = this.AlwaysUseMseaFormatDamageSkin;
                defaultRenderer.DisplayUnitOnSingleLine = this.DisplayUnitOnSingleLine;
                defaultRenderer.DamageSkinNumber = this.DamageSkinNumber;
                renderer = defaultRenderer;
                defaultRenderer.DamageSkin = damageSkin;
            }
            renderer.TargetItem = damageSkin;
            return renderer.Render();
        }

        private Bitmap RenderFamiliar(Familiar familiar)
        {
            TooltipRender renderer = this.FamiliarRender;
            if (renderer == null)
            {
                FamiliarTooltipRender defaultRenderer = new FamiliarTooltipRender();
                defaultRenderer.StringLinker = this.StringLinker;
                defaultRenderer.ShowObjectID = this.ShowObjectID;
                defaultRenderer.AllowOutOfBounds = false;
                defaultRenderer.ItemID = this.item.ItemID;
                defaultRenderer.FamiliarTier = this.item.Grade;
                defaultRenderer.UseAssembleUI = false;
                renderer = defaultRenderer;
            }
            renderer.TargetItem = familiar;

            var ret = renderer.Render();
            DLeft_Set = (renderer as FamiliarTooltipRender).DLeft;
            DTop_Set = (renderer as FamiliarTooltipRender).DTop;

            return ret;
        }

        private Bitmap RenderLinkRecipeInfo(Recipe recipe)
        {
            TooltipRender renderer = this.LinkRecipeInfoRender;
            if (renderer == null)
            {
                RecipeTooltipRender defaultRenderer = new RecipeTooltipRender();
                defaultRenderer.StringLinker = this.StringLinker;
                defaultRenderer.ShowObjectID = false;
                defaultRenderer.Enable22AniStyle = false;
                renderer = defaultRenderer;
            }

            renderer.TargetItem = recipe;
            return renderer.Render();
        }

        private Bitmap RenderLinkRecipeGear(Gear gear)
        {
            TooltipRender renderer = this.LinkRecipeGearRender;
            if (renderer == null)
            {
                GearTooltipRender2 defaultRenderer = new GearTooltipRender2();
                defaultRenderer.StringLinker = this.StringLinker;
                defaultRenderer.ShowObjectID = false;
                renderer = defaultRenderer;
            }

            renderer.TargetItem = gear;
            return renderer.Render();
        }

        private Bitmap RenderLinkRecipeItem(Item item)
        {
            TooltipRender renderer = this.LinkRecipeItemRender;
            if (renderer == null)
            {
                ItemTooltipRender2 defaultRenderer = new ItemTooltipRender2();
                defaultRenderer.StringLinker = this.StringLinker;
                defaultRenderer.ShowObjectID = false;
                renderer = defaultRenderer;
            }

            renderer.TargetItem = item;
            return renderer.Render();
        }

        private Bitmap RenderSetItem(SetItem setItem)
        {
            TooltipRender renderer = this.SetItemRender;
            if (renderer == null)
            {
                var defaultRenderer = new SetItemTooltipRender();
                defaultRenderer.StringLinker = this.StringLinker;
                defaultRenderer.ShowObjectID = false;
                renderer = defaultRenderer;
            }

            renderer.TargetItem = setItem;
            return renderer.Render();
        }

        private Bitmap RenderCashPackage(CashPackage cashPackage)
        {
            TooltipRender renderer = this.CashPackageRender;
            if (renderer == null)
            {
                var defaultRenderer = new CashPackageTooltipRender();
                defaultRenderer.StringLinker = this.StringLinker;
                defaultRenderer.ShowObjectID = this.ShowObjectID;
                defaultRenderer.Enable22AniStyle = false;
                renderer = defaultRenderer;
            }

            renderer.TargetItem = cashPackage;
            return renderer.Render();
        }

        private Bitmap RenderLevel(out int picHeight)
        {
            Bitmap level = null;
            Graphics g = null;
            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;
            picHeight = 0;
            if (Item.Levels != null)
            {
                if (level == null)
                {
                    level = new Bitmap(261, DefaultPicHeight);
                    g = Graphics.FromImage(level);
                }
                picHeight += 13;
                TextRenderer.DrawText(g, "레벨 정보", GearGraphics.EquipDetailFont, new Point(261, picHeight), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.HorizontalCenter);
                picHeight += 15;

                for (int i = 0; i < Item.Levels.Count; i++)
                {
                    var info = Item.Levels[i];
                    TextRenderer.DrawText(g, "레벨 " + info.Level + (i >= Item.Levels.Count - 1 ? "(MAX)" : null), GearGraphics.EquipDetailFont, new Point(10, picHeight), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.NoPadding);
                    picHeight += 15;
                    foreach (var kv in info.BonusProps)
                    {
                        GearLevelInfo.Range range = kv.Value;

                        string propString = ItemStringHelper.GetGearPropString(kv.Key, kv.Value.Min);
                        if (propString != null)
                        {
                            if (range.Max != range.Min)
                            {
                                propString += " ~ " + kv.Value.Max + (propString.EndsWith("%") ? "%" : null);
                            }
                            TextRenderer.DrawText(g, propString, GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding);
                            picHeight += 15;
                        }
                    }
                    if (info.Skills.Count > 0)
                    {
                        string title = string.Format("{2:P2}({0}/{1}) 확률로 스킬 강화 옵션 추가 :", info.Prob, info.ProbTotal, info.Prob * 1.0 / info.ProbTotal);
                        TextRenderer.DrawText(g, title, GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding);
                        picHeight += 15;
                        foreach (var kv in info.Skills)
                        {
                            StringResult sr = null;
                            if (this.StringLinker != null)
                            {
                                this.StringLinker.StringSkill.TryGetValue(kv.Key, out sr);
                            }
                            string text = string.Format(" {0} +{2}레벨", sr == null ? null : sr.Name, kv.Key, kv.Value);
                            TextRenderer.DrawText(g, text, GearGraphics.EquipDetailFont, new Point(10, picHeight), ((SolidBrush)GearGraphics.OrangeBrush).Color, TextFormatFlags.NoPadding);
                            picHeight += 15;
                        }
                    }
                    if (info.EquipmentSkills.Count > 0)
                    {
                        string title;
                        if (info.Prob < info.ProbTotal)
                        {
                            title = string.Format("{2:P2}({0}/{1}) 확률로 스킬 사용 가능 :", info.Prob, info.ProbTotal, info.Prob * 1.0 / info.ProbTotal);
                        }
                        else
                        {
                            title = "스킬 사용 가능 :";
                        }
                        TextRenderer.DrawText(g, title, GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding);
                        picHeight += 15;
                        foreach (var kv in info.EquipmentSkills)
                        {
                            StringResult sr = null;
                            if (this.StringLinker != null)
                            {
                                this.StringLinker.StringSkill.TryGetValue(kv.Key, out sr);
                            }
                            string text = string.Format(" {0} {2}레벨", sr == null ? null : sr.Name, kv.Key, kv.Value);
                            TextRenderer.DrawText(g, text, GearGraphics.EquipDetailFont, new Point(10, picHeight), ((SolidBrush)GearGraphics.OrangeBrush).Color, TextFormatFlags.NoPadding);
                            picHeight += 15;
                        }
                    }
                    if (info.Exp > 0)
                    {
                        TextRenderer.DrawText(g, "단위 경험치 : " + info.Exp + "%", GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding);
                        picHeight += 15;
                    }

                    picHeight += 2;
                }
            }


            format.Dispose();
            if (g != null)
            {
                g.Dispose();
                picHeight += 13;
            }
            return level;
        }

        private bool TryGetNickResource(long nickTag, out Wz_Node resNode)
        {
            resNode = PluginBase.PluginManager.FindWz("UI/NameTag.img/nick/" + nickTag, this.SourceWzFile);
            return resNode != null;
        }

        private string ReplaceDescTags(string text)
        {
            if (text.Contains("#cosmetic_EULO#"))
            {
                this.Item.Specs.TryGetValue(ItemSpecType.cosmetic, out long cosmeticID);
                var gender = Gear.GetGender((int)cosmeticID);
                var name = "";
                if (StringLinker == null || !StringLinker.StringEqp.TryGetValue((int)cosmeticID, out var sr))
                {
                    sr = new StringResult();
                    sr.Name = "(null)";
                }
                name = $"#c{sr.Name}{(gender == 0 ? "(남)" : (gender == 1 ? "(여)" : ""))}#";

                int last = (name.LastOrDefault(c => c >= '가' && c <= '힣') - '가') % 28;
                name += ((last == 0 || last == 8 ? "" : "으") + "로");

                foreach (var color in AvatarCanvas.HairColor)
                {
                    name = name.Replace($"{color} ", "");
                }

                text = text.Replace("#cosmetic_EULO#", name);
            }

            return text;
        }

        private void InitSampleResources()
        {
            Wz_Node _nickResNode = null;

            this.NickResNode = null;
            if (this.ItemSample != null)
            {
                this.ItemSample.Dispose();
                this.ItemSample = null;
            }

            long value;
            this.WillDrawNickTag = this.Item.Props.TryGetValue(ItemPropType.nickTag, out value)
                && this.TryGetNickResource(value, out _nickResNode);

            this.NickResNode = _nickResNode;
        }

        public bool HasSamples()
        {
            return this.Item.Sample.Bitmap != null || this.NickResNode != null || this.ItemSample != null;
        }

        public Bitmap GetSampleBitmap()
        {
            if (this.WillDrawNickTag)
            {
                Rectangle rect = new Rectangle();
                int block = 300;
                using Bitmap tempBitmap = new Bitmap(block, block);
                using Graphics tempG = Graphics.FromImage(tempBitmap);
                tempG.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                int h = block / 2;

                StringResult sr;
                if (StringLinker == null || !StringLinker.StringItem.TryGetValue(item.ItemID, out sr))
                {
                    sr = new StringResult();
                    sr.Name = "(null)";
                }

                string nickName = GearGraphics.GetNameTagString(sr);
                GearGraphics.DrawNameTag(tempG, this.NickResNode, nickName, tempBitmap.Width, out rect, ref h);

                Bitmap resBitmap = new Bitmap(rect.Width, rect.Height);
                using Graphics g = Graphics.FromImage(resBitmap);
                g.DrawImage(tempBitmap, 0, 0, rect, GraphicsUnit.Pixel);

                return resBitmap;
            }
            else if (this.Item.Sample.Bitmap != null)
            {
                return new Bitmap(this.Item.Sample.Bitmap);
            }
            else if (this.ItemSample != null)
            {
                return new Bitmap(this.ItemSample);
            }

            return null;
        }
    }
}
