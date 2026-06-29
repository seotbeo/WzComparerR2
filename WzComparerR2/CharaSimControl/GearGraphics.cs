using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CharaSimResource;
using WzComparerR2.CharaSim;
using TR = System.Windows.Forms.TextRenderer;
using TextFormatFlags = System.Windows.Forms.TextFormatFlags;
using WzComparerR2.Text;
using WzComparerR2.WzLib;
using WzComparerR2.Common;

namespace WzComparerR2.CharaSimControl
{
    /// <summary>
    /// 提供一系列的静态Graphics工具，用来绘制物品tooltip。
    /// </summary>
    public static class GearGraphics
    {
        static GearGraphics()
        {
            TBrushes = new Dictionary<string, TextureBrush>();
            TBrushes22ani = new Dictionary<string, TextureBrush>();
            TBrushes["n"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame2_n, WrapMode.Tile);
            TBrushes["ne"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame2_ne, WrapMode.Clamp);
            TBrushes["e"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame2_e, WrapMode.Tile);
            TBrushes["se"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame2_se, WrapMode.Clamp);
            TBrushes["s"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame2_s, WrapMode.Tile);
            TBrushes["sw"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame2_sw, WrapMode.Clamp);
            TBrushes["w"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame2_w, WrapMode.Tile);
            TBrushes["nw"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame2_nw, WrapMode.Clamp);
            TBrushes["c"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame2_c, WrapMode.Tile);

            TBrushes22ani["n"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Common_frame_flexible_n, WrapMode.Tile);
            TBrushes22ani["ne"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Common_frame_flexible_ne, WrapMode.Clamp);
            TBrushes22ani["e"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Common_frame_flexible_e, WrapMode.Tile);
            TBrushes22ani["se"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Common_frame_flexible_se, WrapMode.Clamp);
            TBrushes22ani["s"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Common_frame_flexible_s, WrapMode.Tile);
            TBrushes22ani["sw"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Common_frame_flexible_sw, WrapMode.Clamp);
            TBrushes22ani["w"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Common_frame_flexible_w, WrapMode.Tile);
            TBrushes22ani["nw"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Common_frame_flexible_nw, WrapMode.Clamp);
            TBrushes22ani["c"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Common_frame_flexible_c, WrapMode.Tile);

            SetFontFamily("돋움");
        }

        public static bool is22aniStyle { get; set; }
        public static readonly Dictionary<string, TextureBrush> TBrushes;
        public static readonly Dictionary<string, TextureBrush> TBrushes22ani;
        public static readonly Font ItemNameFont = new Font("돋움", 14f, FontStyle.Bold, GraphicsUnit.Pixel);
        public static readonly Font ItemDetailFont = new Font("돋움", 12f, GraphicsUnit.Pixel);
        public static readonly Font EquipDetailFont = new Font("돋움", 11f, GraphicsUnit.Pixel);
        public static readonly Font EpicGearDetailFont = new Font("돋움", 11f, GraphicsUnit.Pixel);
        public static readonly Font TahomaFont = new Font("Tahoma", 12f, GraphicsUnit.Pixel);
        public static readonly Font SetItemPropFont = new Font("돋움", 11f, GraphicsUnit.Pixel);
        public static readonly Font ItemReqLevelFont = new Font("돋움", 11f, GraphicsUnit.Pixel);
        public static readonly Font EquipMDMoris9Font = new Font("돋움", 11f, GraphicsUnit.Pixel);
        public static readonly Font EquipMDMoris9FontBold = new Font("돋움", 11f, FontStyle.Bold, GraphicsUnit.Pixel);
        public static readonly Font EquipMDMoris9FontStrikeout = new Font("돋움", 11f, FontStyle.Strikeout, GraphicsUnit.Pixel);
        public static readonly Font ItemGulimFont = new Font("굴림", 12f, GraphicsUnit.Pixel);
        public static readonly Font ItemGulimFontBold = new Font("굴림", 14f, FontStyle.Bold, GraphicsUnit.Pixel);
        public static readonly Font NewCTFamiliarNameFont = new Font("Noto Sans SC", 14f, FontStyle.Bold, GraphicsUnit.Pixel);

        private static PrivateFontCollection _pfc = new PrivateFontCollection();
        public static Font ItemNameFont2 { get; private set; }
        public static Font ItemDetailFont2 { get; private set; }
        public static Font EquipDetailFont2 { get; private set; }
        public static Font AchievementTitleFont { get; private set; }
        public static Font WorldArchiveFont { get; private set; }
        public static Font FamiliarNameFont { get; private set; }
        public static Dictionary<string, Dictionary<char, Size>> FontMeasureCache { get; private set; } = new();

        public static void SetFontFamily(string fontName)
        {
            if (ItemNameFont2 != null)
            {
                ItemNameFont2.Dispose();
                ItemNameFont2 = null;
            }
            ItemNameFont2 = new Font(fontName, 14f, FontStyle.Bold, GraphicsUnit.Pixel);

            if (ItemDetailFont2 != null)
            {
                ItemDetailFont2.Dispose();
                ItemDetailFont2 = null;
            }
            ItemDetailFont2 = new Font(fontName, 12f, GraphicsUnit.Pixel);

            if (EquipDetailFont2 != null)
            {
                EquipDetailFont2.Dispose();
                EquipDetailFont2 = null;
            }
            EquipDetailFont2 = new Font(fontName, 11f, GraphicsUnit.Pixel);
        }

        private static void LoadNanumGothicExtraBoldFont()
        {
            try
            {
                if (AchievementTitleFont != null)
                {
                    AchievementTitleFont.Dispose();
                    AchievementTitleFont = null;
                }
                var bytes = Resource.NanumGothicExtraBold;
                IntPtr ptr = Marshal.AllocCoTaskMem(bytes.Length);
                Marshal.Copy(bytes, 0, ptr, bytes.Length);
                _pfc.AddMemoryFont(ptr, bytes.Length);

                var fm = _pfc.Families.FirstOrDefault();
                if (fm != null)
                {
                    AchievementTitleFont = new Font(fm, 16f, FontStyle.Regular, GraphicsUnit.Pixel);
                }
                else throw new Exception();
            }
            catch
            {
                AchievementTitleFont = new Font("Noto Sans KR", 16f, FontStyle.Bold, GraphicsUnit.Pixel);
            }
        }

        private static void LoadNotoSansKRBoldFont()
        {
            try
            {
                if (FamiliarNameFont != null)
                {
                    FamiliarNameFont.Dispose();
                    FamiliarNameFont = null;
                }
                if (WorldArchiveFont != null)
                {
                    WorldArchiveFont.Dispose();
                    WorldArchiveFont = null;
                }
                var bytes = Resource.NotoSansKRBold;
                IntPtr ptr = Marshal.AllocCoTaskMem(bytes.Length);
                Marshal.Copy(bytes, 0, ptr, bytes.Length);
                _pfc.AddMemoryFont(ptr, bytes.Length);

                var fm = _pfc.Families.FirstOrDefault();
                if (fm != null)
                {
                    FamiliarNameFont = new Font(fm, 15f, FontStyle.Bold, GraphicsUnit.Pixel);
                    WorldArchiveFont = new Font(fm, 12f, GraphicsUnit.Point);
                }
                else throw new Exception();
            }
            catch
            {
                FamiliarNameFont = new Font("Noto Sans KR", 15f, FontStyle.Bold, GraphicsUnit.Pixel);
                WorldArchiveFont = new Font("Noto Sans KR", 12f, GraphicsUnit.Point);
            }
        }

        public static void LoadFonts()
        {
            LoadNanumGothicExtraBoldFont();
            LoadNotoSansKRBoldFont();
        }

        public static readonly Color GearBackColor = Color.FromArgb(204, 0, 51, 85);
        public static readonly Color EpicGearBackColor = Color.FromArgb(170, 68, 0, 0);
        public static readonly Color GearIconBackColor = Color.FromArgb(238, 187, 204, 221);
        public static readonly Color EpicGearIconBackColor = Color.FromArgb(221, 204, 187, 187);

        public static readonly Brush GearBackBrush = new SolidBrush(GearBackColor);
        public static readonly Brush EpicGearBackBrush = new SolidBrush(EpicGearBackColor);
        public static readonly Pen GearBackPen = new Pen(GearBackColor);
        public static readonly Pen EpicGearBackPen = new Pen(EpicGearBackColor);
        public static readonly Brush GearIconBackBrush = new SolidBrush(GearIconBackColor);
        public static readonly Brush GearIconBackBrush2 = new SolidBrush(Color.FromArgb(187, 238, 238, 238));
        public static readonly Brush EpicGearIconBackBrush = new SolidBrush(EpicGearIconBackColor);
        public static readonly Brush StatDetailGrayBrush = new SolidBrush(Color.FromArgb(85, 85, 85));

        public static readonly Color OrangeBrushColor = Color.FromArgb(255, 153, 0);
        /// <summary>
        /// 表示物品说明中带有#c标识的橙色字体画刷。
        /// </summary>
        public static readonly Brush OrangeBrush = new SolidBrush(OrangeBrushColor);
        /// <summary>
        /// 表示物品附加属性中橙色字体画刷。
        /// </summary>
        public static readonly Brush OrangeBrush2 = new SolidBrush(Color.FromArgb(255, 170, 0));
        public static readonly Color OrangeBrush3Color = Color.FromArgb(255, 204, 0);
        /// <summary>
        /// 表示装备职业额外说明中使用的橙黄色画刷。
        /// </summary>
        public static readonly Brush OrangeBrush3 = new SolidBrush(OrangeBrush3Color);
        public static readonly Brush OrangeBrush4 = new SolidBrush(Color.FromArgb(255, 136, 17));
        /// <summary>
        /// 表示装备属性额外说明中使用的绿色画刷。
        /// </summary>
        public static readonly Brush GreenBrush2 = new SolidBrush(Color.FromArgb(204, 255, 0));
        public static readonly Color GrayColor2 = Color.FromArgb(153, 153, 153);
        /// <summary>
        /// 表示装备属性额外说明中使用的卷轴强化数值画刷。
        /// </summary>
        public static readonly Color ScrollEnhancementColor = Color.FromArgb(175, 173, 255);
        public static readonly Brush ScrollEnhancementBrush = new SolidBrush(ScrollEnhancementColor);
        /// <summary>
        /// 表示用于绘制“攻击力提升”文字的灰色画刷。
        /// </summary>
        public static readonly Brush GrayBrush2 = new SolidBrush(GrayColor2);
        /// <summary>
        /// 表示套装名字的绿色画刷。
        /// </summary>
        public static readonly Brush SetItemNameBrush = new SolidBrush(Color.FromArgb(119, 255, 0));
        /// <summary>
        /// 表示套装属性不可用的灰色画刷。
        /// </summary>
        public static readonly Brush SetItemGrayBrush = new SolidBrush(Color.FromArgb(119, 136, 153));
        /// <summary>
        /// 表示效果不可用的红色画刷。
        /// </summary>
        public static readonly Brush BlockRedBrush = new SolidBrush(Color.FromArgb(255, 0, 102));
        /// <summary>
        /// 表示装备tooltip中金锤子描述文字的颜色画刷。
        /// </summary>
        public static readonly Brush GoldHammerBrush = new SolidBrush(Color.FromArgb(255, 238, 204));
        /// <summary>
        /// 表示灰色品质的装备名字画刷，额外属性小于0。
        /// </summary>
        public static readonly Brush GearNameBrushA = new SolidBrush(Color.FromArgb(187, 187, 187));
        /// <summary>
        /// 表示白色品质的装备名字画刷，额外属性为0~5。
        /// </summary>
        public static readonly Brush GearNameBrushB = new SolidBrush(Color.FromArgb(255, 255, 255));
        /// <summary>
        /// 表示橙色品质的装备名字画刷，额外属性为0~5，并且已经附加卷轴。
        /// </summary>
        public static readonly Brush GearNameBrushC = new SolidBrush(Color.FromArgb(255, 170, 0));
        private static Color gearBlueColor = Color.FromArgb(102, 255, 255);
        /// <summary>
        /// 表示蓝色品质的装备名字画刷，额外属性为6~22。
        /// </summary>
        public static readonly Brush GearNameBrushD = new SolidBrush(gearBlueColor);
        private static Color gearPurpleColor = Color.FromArgb(153, 102, 255);
        /// <summary>
        /// 表示紫色品质的装备名字画刷，额外属性为23~39。
        /// </summary>
        public static readonly Brush GearNameBrushE = new SolidBrush(gearPurpleColor);
        private static Color gearGoldColor = Color.FromArgb(255, 205, 0);
        /// <summary>
        /// 表示金色品质的装备名字画刷，额外属性为40~54。
        /// </summary>
        public static readonly Brush GearNameBrushF = new SolidBrush(gearGoldColor);
        private static Color gearGreenColor = Color.FromArgb(204, 255, 0);
        /// <summary>
        /// 表示绿色品质的装备名字画刷，额外属性为55~69。
        /// </summary>
        public static readonly Brush GearNameBrushG = new SolidBrush(gearGreenColor);
        /// <summary>
        /// 表示红色品质的装备名字画刷，额外属性为70以上。
        /// </summary>
        public static readonly Brush GearNameBrushH = new SolidBrush(Color.FromArgb(255, 0, 102));
        public static readonly Brush BlueBrush = new SolidBrush(Color.FromArgb(0, 204, 255));

        public static readonly Color gearCyanColor = Color.FromArgb(102, 255, 255);
        /// <summary>
        /// 表示装备属性变化的青色画刷。
        /// </summary>
        public static readonly Brush GearPropChangeBrush = new SolidBrush(gearCyanColor);
        public static readonly Color skillYellowColor = Color.FromArgb(244, 244, 68);
        public static readonly Color itemPinkColor = Color.FromArgb(255, 102, 204);
        public static readonly Color itemPurpleColor = Color.FromArgb(187, 119, 255);

        public static readonly Color SkillSummaryOrangeTextColor = Color.FromArgb(255, 204, 0);
        public static readonly Brush SkillSummaryOrangeTextBrush = new SolidBrush(SkillSummaryOrangeTextColor);
        public static readonly Color SkillHighlightColor = Color.FromArgb(51, 255, 255);

        public static readonly Brush Equip22BrushGray = new SolidBrush(Color.FromArgb(183, 191, 197));
        public static readonly Brush Equip22BrushDarkGray = new SolidBrush(Color.FromArgb(133, 145, 159));
        public static readonly Brush Equip22BrushRed = new SolidBrush(Color.FromArgb(255, 138, 24));
        public static readonly Brush Equip22BrushEmphasis = new SolidBrush(Color.FromArgb(255, 204, 0));
        public static readonly Brush Equip22BrushScroll = new SolidBrush(Color.FromArgb(175, 173, 255));
        public static readonly Brush Equip22BrushBonusStat = new SolidBrush(Color.FromArgb(10, 227, 173));
        public static readonly Brush Equip22BrushRare = new SolidBrush(Color.FromArgb(102, 255, 255));
        public static readonly Brush Equip22BrushEpic = new SolidBrush(Color.FromArgb(187, 129, 255));
        public static readonly Brush Equip22BrushLegendary = new SolidBrush(Color.FromArgb(204, 255, 0));
        public static readonly Brush Equip22BrushExceptional = new SolidBrush(Color.FromArgb(255, 51, 51));
        public static readonly Brush Equip22BrushEmphasisBright = new SolidBrush(Color.FromArgb(255, 245, 77));

        public static readonly Brush QuestBrushDefault = new SolidBrush(Color.FromArgb(171, 181, 187));
        public static readonly Brush QuestBrushNpc = new SolidBrush(Color.FromArgb(102, 255, 255));
        public static readonly Brush QuestBrushMob = new SolidBrush(Color.FromArgb(255, 0, 102));
        public static readonly Brush QuestBrushMap = new SolidBrush(Color.FromArgb(221, 254, 1));
        public static readonly Brush QuestBrushItem = new SolidBrush(Color.FromArgb(204, 143, 255));
        public static readonly Brush QuestBrushEnd = new SolidBrush(Color.FromArgb(101, 117, 120));

        public static readonly Brush AchievementPeriodBrush = new SolidBrush(Color.FromArgb(255, 255, 204));
        public static readonly Brush AchievementRewardBrush = new SolidBrush(Color.FromArgb(221, 221, 221));

        public static readonly Brush BarrierArcBrush = new SolidBrush(Color.FromArgb(218, 161, 255));
        public static readonly Brush BarrierAutBrush = new SolidBrush(Color.FromArgb(218, 161, 255));

        public static readonly Brush LocationBrush = new SolidBrush(Color.FromArgb(209, 255, 50));

        public static readonly Brush ItemPriceBrush = new SolidBrush(Color.FromArgb(119, 204, 255));

        public static Brush GetGearNameBrush(int diff, bool up, bool cash = false, bool petEquip = false)
        {
            if (cash && !petEquip)
                return GearNameBrushB;
            if (diff < 0)
                return GearNameBrushA;
            if (diff < 6 || petEquip)
            {
                if (!up)
                    return GearNameBrushB;
                else
                    return GearNameBrushC;
            }
            if (diff < 23)
                return GearNameBrushD;
            if (diff < 40)
                return GearNameBrushE;
            if (diff < 55)
                return GearNameBrushF;
            if (diff < 70)
                return GearNameBrushG;
            return GearNameBrushH;
        }

        public static readonly Pen GearItemBorderPenC = new Pen(Color.FromArgb(255, 0, 102));
        public static readonly Pen GearItemBorderPenB = new Pen(gearBlueColor);
        public static readonly Pen GearItemBorderPenA = new Pen(gearPurpleColor);
        public static readonly Pen GearItemBorderPenS = new Pen(gearGoldColor);
        public static readonly Pen GearItemBorderPenSS = new Pen(gearGreenColor);
        public static Pen GetGearItemBorderPen(GearGrade grade)
        {
            switch (grade)
            {
                case GearGrade.B:
                    return GearItemBorderPenB;
                case GearGrade.A:
                    return GearItemBorderPenA;
                case GearGrade.S:
                    return GearItemBorderPenS;
                case GearGrade.SS:
                    return GearItemBorderPenSS;
                default:
                    return null;
            }
        }

        public static Brush GetPotentialTextBrush(GearGrade grade)
        {
            switch (grade)
            {
                default:
                case GearGrade.B: return GearPropChangeBrush;
                case GearGrade.A: return GearNameBrushE;
                case GearGrade.S: return GearNameBrushF;
                case GearGrade.SS: return GreenBrush2;
            }
        }

        /// <summary>
        /// 在指定区域绘制包含宏代码的字符串。
        /// </summary>
        /// <param Name="g">绘图所关联的graphics。</param>
        /// <param Name="s">要绘制的string。</param>
        /// <param Name="font">要绘制string的字体。</param>
        /// <param Name="x">起始的x坐标。</param>
        /// <param Name="X1">每行终止的x坐标。</param>
        /// <param Name="y">起始行的y坐标。</param>
        public static void DrawString(Graphics g, string s, Font font, int x, int x1, ref int y, int height, TextAlignment alignment = TextAlignment.Left)
        {
            DrawString(g, s, font, null, x, x1, ref y, height, alignment);
        }

        public static void DrawString(Graphics g, string s, Font font, IDictionary<string, Color> fontColorTable, int x, int x1, ref int y, int height, TextAlignment alignment = TextAlignment.Left, int strictlyAlignLeft = 0, Color defaultColor = default)
        {
            DrawString(g, s, font, fontColorTable, null, null, x, x1, ref y, height, alignment, strictlyAlignLeft, defaultColor);
        }

        public static void DrawString(Graphics g, string s, Font font, IDictionary<string, Color> fontColorTable, IDictionary<string, Font> fontTable, IDictionary<string, Bitmap> imageTable,
            int x, int x1, ref int y, int height, TextAlignment alignment = TextAlignment.Left, int strictlyAlignLeft = 0, Color defaultColor = default, TRImageAlignment ImageVerticalAlignment = TRImageAlignment.Top)
        {
            if (s == null)
                return;

            using (var r = new FormattedTextRenderer())
            {
                r.WordWrapEnabled = false;
                r.UseGDIRenderer = true;
                r.FontColorTable = fontColorTable;
                r.FontTable = fontTable;
                r.ImageTable = imageTable;
                r.ImageVerticalAlignment = ImageVerticalAlignment;
                r.StrictlyAlignLeft = strictlyAlignLeft;
                r.DrawString(g, s, font, x, x1, ref y, height, alignment, defaultColor);
            }
        }

        public static void DrawPlainText(Graphics g, string s, Font font, Color color, int x, int x1, ref int y, int height, TextAlignment alignment = TextAlignment.Left, int strictlyAlignLeft = 0)
        {
            if (s == null)
                return;

            using (var r = new FormattedTextRenderer())
            {
                r.WordWrapEnabled = false;
                r.UseGDIRenderer = true;
                r.StrictlyAlignLeft = strictlyAlignLeft;
                r.DrawPlainText(g, s, font, color, x, x1, ref y, height, alignment);
            }
        }

        public static Bitmap EnlargeBitmap(Bitmap bitmap)
        {
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            IntPtr p = data.Scan0;
            byte[] origin = new byte[bitmap.Width * bitmap.Height * 4];
            Marshal.Copy(p, origin, 0, origin.Length);
            bitmap.UnlockBits(data);
            byte[] newByte = new byte[origin.Length * 4];
            byte[] buffer = new byte[4];
            for (int i = 0; i < bitmap.Height; i++)
            {
                for (int j = 0; j < bitmap.Width; j++)
                {
                    Array.Copy(origin, getOffset(j, i, bitmap.Width), buffer, 0, 4);
                    Array.Copy(buffer, 0, newByte, getOffset(2 * j, 2 * i, bitmap.Width * 2), 4);
                    Array.Copy(buffer, 0, newByte, getOffset(2 * j + 1, 2 * i, bitmap.Width * 2), 4);
                }
                Array.Copy(newByte, getOffset(0, 2 * i, bitmap.Width * 2), newByte, getOffset(0, 2 * i + 1, bitmap.Width * 2), bitmap.Width * 8);
            }
            Bitmap newBitmap = new Bitmap(bitmap.Width * 2, bitmap.Height * 2);
            data = newBitmap.LockBits(new Rectangle(0, 0, newBitmap.Width, newBitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(newByte, 0, data.Scan0, newByte.Length);
            newBitmap.UnlockBits(data);
            return newBitmap;
        }

        private static int getOffset(int x, int y, int width, int unit = 4)
        {
            return (y * width + x) * unit;
        }

        public static Point[] GetBorderPath(int dx, int width, int height)
        {
            List<Point> pointList = new List<Point>(13);
            pointList.Add(new Point(dx + 1, 0));
            pointList.Add(new Point(dx + 1, 1));
            pointList.Add(new Point(dx + 0, 1));
            pointList.Add(new Point(dx + 0, height - 2));
            pointList.Add(new Point(dx + 1, height - 2));
            pointList.Add(new Point(dx + 1, height - 1));
            pointList.Add(new Point(dx + width - 2, height - 1));
            pointList.Add(new Point(dx + width - 2, height - 2));
            pointList.Add(new Point(dx + width - 1, height - 2));
            pointList.Add(new Point(dx + width - 1, 1));
            pointList.Add(new Point(dx + width - 2, 1));
            pointList.Add(new Point(dx + width - 2, 0));
            pointList.Add(new Point(dx + 1, 0));
            return pointList.ToArray();
        }

        public static Point[] GetIconBorderPath(int x, int y)
        {
            Point[] pointList = new Point[5];
            pointList[0] = new Point(x + 32, y + 31);
            pointList[1] = new Point(x + 32, y);
            pointList[2] = new Point(x, y);
            pointList[3] = new Point(x, y + 32);
            pointList[4] = new Point(x + 31, y + 32);
            return pointList;
        }

        public static void DrawGearDetailNumber(Graphics g, int x, int y, string num, bool can)
        {
            Bitmap bitmap;
            for (int i = 0; i < num.Length; i++)
            {
                switch (num[i])
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        string resourceName = (can ? "ToolTip_Equip_Can_" : "ToolTip_Equip_Cannot_") + num[i];
                        bitmap = (Bitmap)Resource.ResourceManager.GetObject(resourceName);
                        g.DrawImage(bitmap, x, y);
                        x += bitmap.Width + 1;
                        break;
                    case '-':
                        bitmap = can ? Resource.ToolTip_Equip_Can_none : Resource.ToolTip_Equip_Cannot_none;
                        g.DrawImage(bitmap, x, y + 3);
                        x += bitmap.Width + 1;
                        break;
                    case '%':
                        bitmap = can ? Resource.ToolTip_Equip_Can_percent : Resource.ToolTip_Equip_Cannot_percent;
                        g.DrawImage(bitmap, x + 1, y);
                        x += bitmap.Width + 2;
                        break;
                }
            }
        }

        public static void DrawGearGrowthNumber(Graphics g, int x, int y, string num, bool can)
        {
            Bitmap bitmap;
            for (int i = 0; i < num.Length; i++)
            {
                switch (num[i])
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        string resourceName = (can ? "ToolTip_Equip_GrowthEnabled_" : "ToolTip_Equip_Cannot_") + num[i];
                        bitmap = (Bitmap)Resource.ResourceManager.GetObject(resourceName);
                        g.DrawImage(bitmap, x, y);
                        x += bitmap.Width + 1;
                        break;
                    case '-':
                        bitmap = can ? Resource.ToolTip_Equip_GrowthDisabled_none : Resource.ToolTip_Equip_GrowthDisabled_none;
                        g.DrawImage(bitmap, x, y);
                        x += bitmap.Width + 1;
                        break;
                    case '%':
                        bitmap = can ? Resource.ToolTip_Equip_GrowthEnabled_percent : Resource.ToolTip_Equip_GrowthEnabled_percent;
                        g.DrawImage(bitmap, x + 7, y - 4);
                        x += bitmap.Width + 1;
                        break;
                    case 'm':
                        bitmap = can ? Resource.ToolTip_Equip_GrowthEnabled_max : Resource.ToolTip_Equip_GrowthEnabled_max;
                        g.DrawImage(bitmap, x, y);
                        x += bitmap.Width + 1;
                        break;
                }
            }
        }

        public static void DrawItemCountNumber(Graphics g, int x, int y, string num)
        {
            Bitmap bitmap;
            for (int i = 0; i < num.Length; i++)
            {
                string resourceName = $"Basic_img_ItemNo_{num[i]}";
                bitmap = (Bitmap)Resource.ResourceManager.GetObject(resourceName);
                if (bitmap != null)
                {
                    g.DrawImage(bitmap, x, y);
                    x += bitmap.Width;
                }
            }
        }

        public static void DrawNewTooltipBack(Graphics g, int x, int y, int width, int height)
        {
            Dictionary<string, TextureBrush> res = is22aniStyle ? TBrushes22ani : TBrushes;
            //测算准线
            int[] guideX = new int[4] { 0, res["w"].Image.Width, width - res["e"].Image.Width, width };
            int[] guideY = new int[4] { 0, res["n"].Image.Height, height - res["s"].Image.Height, height };
            for (int i = 0; i < guideX.Length; i++) guideX[i] += x;
            for (int i = 0; i < guideY.Length; i++) guideY[i] += y;
            //绘制四角
            FillRect(g, res["nw"], guideX, guideY, 0, 0, 1, 1);
            FillRect(g, res["ne"], guideX, guideY, 2, 0, 3, 1);
            FillRect(g, res["sw"], guideX, guideY, 0, 2, 1, 3);
            FillRect(g, res["se"], guideX, guideY, 2, 2, 3, 3);
            //填充上下区域
            if (guideX[2] > guideX[1])
            {
                FillRect(g, res["n"], guideX, guideY, 1, 0, 2, 1);
                FillRect(g, res["s"], guideX, guideY, 1, 2, 2, 3);
            }
            //填充左右区域
            if (guideY[2] > guideY[1])
            {
                FillRect(g, res["w"], guideX, guideY, 0, 1, 1, 2);
                FillRect(g, res["e"], guideX, guideY, 2, 1, 3, 2);
            }
            //填充中心
            if (guideX[2] > guideX[1] && guideY[2] > guideY[1])
            {
                FillRect(g, res["c"], guideX, guideY, 1, 1, 2, 2);
            }
        }

        private static void FillRect(Graphics g, TextureBrush brush, int[] guideX, int[] guideY, int x0, int y0, int x1, int y1)
        {
            brush.ResetTransform();
            brush.TranslateTransform(guideX[x0], guideY[y0]);
            g.FillRectangle(brush, guideX[x0], guideY[y0], guideX[x1] - guideX[x0], guideY[y1] - guideY[y0]);
        }

        public static string GetNameTagString(StringResult sr)
        {
            string res;
            string nickWithQR = sr["nickWithQR"];
            string nickWithQRex = sr["nickWithQRex"];
            string nickWithWSR = sr["nickWithWSR"];
            if (!string.IsNullOrEmpty(nickWithQR))
            {
                string qrDefault = sr["qrDefault"] ?? string.Empty;
                res = Regex.Replace(nickWithQR, "#qr.*?#", qrDefault);
            }
            else if (!string.IsNullOrEmpty(nickWithQRex))
            {
                string qrexDefault = sr["qrexDefault"] ?? string.Empty;
                res = Regex.Replace(nickWithQRex, "#qrex.*?#", qrexDefault);
            }
            else if (!string.IsNullOrEmpty(nickWithWSR))
            {
                string wsrDefault = sr["wsrDefault"] ?? string.Empty;
                res = Regex.Replace(nickWithWSR, "#wsr.*?#", wsrDefault);
            }
            else
            {
                res = sr.Name;
            }

            return res;
        }

        public static void DrawNameTag(Graphics g, Wz_Node resNode, string tagName, int picW, ref int picH)
        {
            DrawNameTag(g, resNode, tagName, picW, out Rectangle rectResult, ref picH);
        }

        public static void DrawNameTag(Graphics g, Wz_Node resNode, string tagName, int picW, out Rectangle rectResult, ref int picH)
        {
            rectResult = new Rectangle();
            if (g == null || resNode == null)
                return;

            //加载资源和文本颜色
            var wce = new[] { "w", "c", "e" }.Select(n =>
            {
                var node = resNode.FindNodeByPath(n);
                if (node == null)
                {
                    return new BitmapOrigin();
                }
                return BitmapOrigin.CreateFromNode(node, PluginBase.PluginManager.FindWz);
            }).ToArray();

            Color color = Color.FromArgb(resNode.FindNodeByPath("clr").GetValueEx(-1));
            BitmapOrigin ani0 = default;
            Wz_Node ani0Node = resNode.FindNodeByPath(false, "ani", "0");
            if (ani0Node != null)
            {
                ani0 = BitmapOrigin.CreateFromNode(ani0Node, PluginBase.PluginManager.FindWz);
            }
            else
            {
                // 명찰 애니메이션 체크
                foreach (var path in new[] { "2", "1", "0" })
                {
                    ani0Node = resNode.FindNodeByPath(false, path, "0");
                    if (ani0Node != null)
                    {
                        ani0 = BitmapOrigin.CreateFromNode(ani0Node, PluginBase.PluginManager.FindWz);
                        break;
                    }
                }
            }

            //测试y轴大小
            bool aniNameTag = resNode.FindNodeByPath("aniNameTag").GetValueEx(false);
            int offsetY = aniNameTag ? 0 : wce.Min(bmp => bmp.OpOrigin.Y);
            int height = aniNameTag ? 0 :wce.Max(bmp => bmp.Rectangle.Bottom);

            //测试宽度
            var font = GearGraphics.ItemDetailFont2;
            var fmt = StringFormat.GenericTypographic;
            //int nameWidth = string.IsNullOrEmpty(tagName) ? 0 : (int)Math.Ceiling(g.MeasureString(tagName, font, 261, fmt).Width);
            int nameWidth = string.IsNullOrEmpty(tagName) ? 0 : TextRenderer.MeasureText(g, tagName, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
            int center = picW / 2;

            if (ani0.Bitmap == null) // legacy mode
            {
                if (wce[1].Bitmap != null)
                {
                    nameWidth = (int)Math.Ceiling(1.0 * nameWidth / wce[1].Bitmap.Width) * wce[1].Bitmap.Width;
                }
                int left = center - nameWidth / 2;
                int right = left + nameWidth;

                rectResult.X = left;
                rectResult.Width = nameWidth;

                //开始绘制背景
                picH -= offsetY;
                if (wce[0].Bitmap != null)
                {
                    g.DrawImage(wce[0].Bitmap, left - wce[0].Origin.X, picH - wce[0].Origin.Y);

                    rectResult.X -= wce[0].Origin.X;
                    rectResult.Width += wce[0].Origin.X;
                }
                if (wce[1].Bitmap != null) //不用拉伸 用纹理平铺 看运气
                {
                    var brush = new TextureBrush(wce[1].Bitmap);
                    Rectangle rect = new Rectangle(left, picH - wce[1].Origin.Y, right - left, brush.Image.Height);
                    brush.TranslateTransform(rect.X, rect.Y);
                    g.FillRectangle(brush, rect);
                    brush.Dispose();
                }
                if (wce[2].Bitmap != null)
                {
                    g.DrawImage(wce[2].Bitmap, right - wce[2].Origin.X, picH - wce[2].Origin.Y);

                    rectResult.Width += (wce[2].Bitmap.Width - wce[2].Origin.X);
                }

                //绘制文字
                if (!string.IsNullOrEmpty(tagName))
                {
                    //using var brush = new SolidBrush(color);
                    //g.DrawString(tagName, font, brush, left, picH, fmt);
                    TextRenderer.DrawText(g, tagName, font, new Rectangle(left, picH, right - left, int.MaxValue), color, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
                }
            }
            else // ani mode
            {
                bool mixedAniMode = wce[1].Bitmap != null && (wce[1].Bitmap.Width > 1 || wce[1].Bitmap.Height > 1)
                    || aniNameTag;

                offsetY = Math.Min((!aniNameTag ? offsetY : 0), ani0.OpOrigin.Y);
                height = Math.Max((!aniNameTag ? height : 0), ani0.Rectangle.Bottom);

                int bgWidth = mixedAniMode ? wce[1].Bitmap.Width : nameWidth;
                int left = center - bgWidth / 2;
                int right = left + bgWidth;
                int nameLeft = center - nameWidth / 2;

                picH -= offsetY;

                int shiftX = aniNameTag ? 0 : wce[1].Origin.X;
                int shiftY = aniNameTag ? 0 : wce[1].Origin.Y;

                if (mixedAniMode)
                {
                    // draw legay center
                    // Note: item 1143360 (MILESTONE) does not render well, ignore it.
                    if (!aniNameTag) g.DrawImage(wce[1].Bitmap, left - wce[1].Origin.X, picH - wce[1].Origin.Y);
                    // draw ani0 based on bg center position
                    g.DrawImage(ani0.Bitmap, left - shiftX - ani0.Origin.X, picH - shiftY - ani0.Origin.Y);
                    if (!string.IsNullOrEmpty(tagName)) // draw name
                    {
                        using var brush = new SolidBrush(color);
                        // offsetX with bg for better alignment
                        g.DrawString(tagName, font, brush, nameLeft - shiftX, picH, fmt);
                    }

                    rectResult.X = left - shiftX - ani0.Origin.X;
                    rectResult.Width = ani0.Bitmap.Width;
                }
                else
                {
                    // draw ani0 only
                    g.DrawImage(ani0.Bitmap, left - ani0.Origin.X, picH - ani0.Origin.Y);

                    rectResult.X = left - ani0.Origin.X;
                    rectResult.Width = ani0.Bitmap.Width;
                }
            }

            picH += height;

            rectResult.Height = height - offsetY;
            rectResult.Y = picH - rectResult.Height;
        }

        public static void DrawChatBalloon(Graphics g, Wz_Node resNode, string tagName, int picW, ref int picH)
        {
            DrawChatBalloon(g, resNode, tagName, picW, out Rectangle rectResult, ref picH);
        }

        public static void DrawChatBalloon(Graphics g, Wz_Node resNode, string tagName, int picW, out Rectangle rectResult, ref int picH)
        {
            rectResult = new Rectangle();
            if (g == null || resNode == null)
                return;

            // 애니메이션 체크
            Wz_Node ani0Node = resNode.FindNodeByPath(false, "0");
            if (ani0Node != null)
            {
                resNode = ani0Node;
            }
            Color color = Color.FromArgb(resNode.FindNodeByPath("clr").GetValueEx(-1));

            //加载资源和文本颜色
            var wce = new[] { "nw", "n", "head", "ne", "w", "c", "e", "sw", "s", "arrow", "se" }.Select(n =>
            {
                var node = resNode.FindNodeByPath(n);
                if (node == null)
                {
                    return new BitmapOrigin();
                }
                return BitmapOrigin.CreateFromNode(node, PluginBase.PluginManager.FindWz);
            }).ToArray();

            // head, arrow가 없을 경우, 각각 n, s로 대체
            bool noHead = false;
            bool noArrow = false;
            if (wce[2].Bitmap == null)
            {
                wce[2] = wce[1];
                noHead = true;
            }
            if (wce[9].Bitmap == null)
            {
                wce[9] = wce[8];
                noArrow = true;
            }

            //测试y轴大小
            int offsetY = wce.Min(bmp => bmp.OpOrigin.Y);
            int height = wce.Max(bmp => bmp.Rectangle.Bottom);

            //测试宽度
            var font = GearGraphics.ItemDetailFont2;
            using var fmt = (StringFormat)StringFormat.GenericTypographic.Clone();
            fmt.Alignment = StringAlignment.Center;
            int nameWidth = string.IsNullOrEmpty(tagName) ? 0 : TextRenderer.MeasureText(g, tagName, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
            int dxn = wce[1].Bitmap?.Width ?? 0;
            int dxs = wce[8].Bitmap?.Width ?? 0;
            int dy = wce[5].Bitmap?.Height ?? 0;
            int stageWidth = (noHead ? wce[8].Bitmap?.Width ?? 0 : wce[1].Bitmap?.Width ?? 0) * 2;
            int centerWidth = noHead ? wce[9].Bitmap?.Width ?? 0 : wce[2].Bitmap?.Width ?? 0;
            int maxNameWidth = stageWidth * 3 + centerWidth;
            int center = picW / 2;
            int line = 1;
            int stageCount = 3;

            // 멀티라인 확인
            if (maxNameWidth > 0)
            {
                line += (nameWidth / maxNameWidth);
            }
            if (line == 1)
            {
                var tmpNameWidth = nameWidth;
                if (stageWidth > 0)
                {
                    while (tmpNameWidth - stageWidth > centerWidth + stageWidth * (stageCount - 1))
                    {
                        tmpNameWidth -= stageWidth;
                        stageCount--;
                    }
                }
            }

            int middleWidth = (stageWidth) * stageCount + centerWidth;
            int left = center - middleWidth / 2;
            int right = left + middleWidth;

            List<int> rectLeftPlus = new List<int>();
            List<int> rectRightPlus = new List<int>();
            rectResult.X = left;
            rectResult.Width = middleWidth;

            //开始绘制背景
            picH -= offsetY;

            // 상단
            if (wce[0].Bitmap != null) // nw
            {
                g.DrawImage(wce[0].Bitmap, left - wce[0].Origin.X, picH - wce[0].Origin.Y);

                rectResult.X = Math.Min(rectResult.X, left - wce[0].Origin.X);
                rectLeftPlus.Add(wce[0].Origin.X);
            }
            if (wce[1].Bitmap != null) // n
            {
                if (noHead)
                {
                    using var brush = new TextureBrush(wce[1].Bitmap);
                    Rectangle rect = new Rectangle(left, picH - wce[1].Origin.Y, right - wce[3].Origin.X - left, brush.Image.Height);
                    brush.TranslateTransform(rect.X, rect.Y);
                    g.FillRectangle(brush, rect);
                }
                else
                {
                    var pos1 = (center - centerWidth / 2) - wce[2].Origin.X;
                    var pos2 = (center - centerWidth / 2 + centerWidth);

                    using var brush = new TextureBrush(wce[1].Bitmap);
                    Rectangle rect = new Rectangle(left, picH - wce[1].Origin.Y, pos1 - left, brush.Image.Height);
                    brush.TranslateTransform(rect.X, rect.Y);
                    g.FillRectangle(brush, rect);

                    rect = new Rectangle(pos2, picH - wce[1].Origin.Y, right - wce[3].Origin.X - pos2, brush.Image.Height);
                    brush.ResetTransform();
                    brush.TranslateTransform(rect.X, rect.Y);
                    g.FillRectangle(brush, rect);

                    using var brushC = new TextureBrush(wce[2].Bitmap); // head
                    rect = new Rectangle(pos1, picH - wce[2].Origin.Y, pos2 - pos1, brushC.Image.Height);
                    brushC.TranslateTransform(rect.X, rect.Y);
                    g.FillRectangle(brushC, rect);
                }
            }
            if (wce[3].Bitmap != null) // ne
            {
                g.DrawImage(wce[3].Bitmap, right - wce[3].Origin.X, picH - wce[3].Origin.Y);

                rectRightPlus.Add(wce[3].Bitmap.Width - wce[3].Origin.X);
            }

            // 중단
            for (int i = 0; i < line; i++)
            {
                if (wce[4].Bitmap != null) // w
                {
                    g.DrawImage(wce[4].Bitmap, left - wce[4].Origin.X, picH - wce[4].Origin.Y);

                    rectResult.X = Math.Min(rectResult.X, left - wce[4].Origin.X);
                    rectLeftPlus.Add(wce[4].Origin.X);
                }
                if (wce[5].Bitmap != null) // c
                {
                    using var brush = new TextureBrush(wce[5].Bitmap);
                    Rectangle rect = new Rectangle(left, picH - wce[5].Origin.Y, right - wce[6].Origin.X - left, brush.Image.Height);
                    brush.TranslateTransform(rect.X, rect.Y);
                    g.FillRectangle(brush, rect);
                }
                if (wce[6].Bitmap != null) // e
                {
                    g.DrawImage(wce[6].Bitmap, right - wce[6].Origin.X, picH - wce[6].Origin.Y);

                    rectRightPlus.Add(wce[6].Bitmap.Width - wce[6].Origin.X);
                }
                picH += dy;
            }

            // 하단
            if (wce[7].Bitmap != null) // sw
            {
                g.DrawImage(wce[7].Bitmap, left - wce[7].Origin.X, picH - wce[7].Origin.Y);

                rectResult.X = Math.Min(rectResult.X, left - wce[7].Origin.X);
                rectLeftPlus.Add(wce[7].Origin.X);
            }
            if (wce[8].Bitmap != null) // s
            {
                if (noArrow)
                {
                    using var brush = new TextureBrush(wce[8].Bitmap);
                    Rectangle rect = new Rectangle(left, picH - wce[8].Origin.Y, right - wce[10].Origin.X - left, brush.Image.Height);
                    brush.TranslateTransform(rect.X, rect.Y);
                    g.FillRectangle(brush, rect);
                }
                else
                {
                    var pos1 = (center - centerWidth / 2) - wce[9].Origin.X;
                    var pos2 = (center - centerWidth / 2 + centerWidth);

                    using var brush = new TextureBrush(wce[8].Bitmap);
                    Rectangle rect = new Rectangle(left, picH - wce[8].Origin.Y, pos1 - left, brush.Image.Height);
                    brush.TranslateTransform(rect.X, rect.Y);
                    g.FillRectangle(brush, rect);

                    rect = new Rectangle(pos2, picH - wce[8].Origin.Y, right - wce[10].Origin.X - pos2, brush.Image.Height);
                    brush.ResetTransform();
                    brush.TranslateTransform(rect.X, rect.Y);
                    g.FillRectangle(brush, rect);

                    using var brushC = new TextureBrush(wce[9].Bitmap); // arrow
                    rect = new Rectangle(pos1, picH - wce[9].Origin.Y, pos2 - pos1, brushC.Image.Height);
                    brushC.TranslateTransform(rect.X, rect.Y);
                    g.FillRectangle(brushC, rect);
                }
            }
            if (wce[10].Bitmap != null) // se
            {
                g.DrawImage(wce[10].Bitmap, right - wce[10].Origin.X, picH - wce[10].Origin.Y);

                rectRightPlus.Add(wce[10].Bitmap.Width - wce[10].Origin.X);
            }

            // 텍스트 입력
            //绘制文字
            if (!string.IsNullOrEmpty(tagName))
            {
                using var brush = new SolidBrush(color);
                Rectangle rect = new Rectangle(left, picH - dy * line + 1, right - left, picH);
                g.DrawString(tagName, font, brush, rect, fmt);
            }

            picH += height;

            rectResult.Width += rectLeftPlus.DefaultIfEmpty(0).Max() + rectRightPlus.DefaultIfEmpty(0).Max();
            rectResult.Height = dy * line + height - offsetY;
            rectResult.Y = picH - rectResult.Height;
        }

        public static string GetFontKey(Font font)
        {
            return $"{font.Name}_{font.Size}_{font.Style}";
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hwnd, UInt32 wMsg, IntPtr wParam, IntPtr lParam);
        private const int WM_SETREDRAW = 0xB;

        public static void SetRedraw(System.Windows.Forms.Control control, bool enable)
        {
            if (control != null)
            {
                SendMessage(control.Handle, WM_SETREDRAW, new IntPtr(enable ? 1 : 0), IntPtr.Zero);
            }
        }

        private class FormattedTextRenderer : WzComparerR2.Text.TextRenderer<Font>, IDisposable
        {
            public FormattedTextRenderer()
            {
                fmt = (StringFormat)StringFormat.GenericTypographic.Clone();
            }

            public bool UseGDIRenderer { get; set; }
            public IDictionary<string, Color> FontColorTable { get; set; }
            public IDictionary<string, Font> FontTable { get; set; }
            public IDictionary<string, Bitmap> ImageTable { get; set; }
            public TRImageAlignment ImageVerticalAlignment { get; set; }

            const int MAX_RANGES = 32;
            StringFormat fmt;

            Graphics g;
            RectangleF infinityRect;
            int drawX;
            Color defaultColor;

            public void DrawString(Graphics g, string s, Font font, int x, int x1, ref int y, int height, TextAlignment alignment = TextAlignment.Left, Color defaultColor = default)
            {
                //初始化环境
                this.g = g;
                this.drawX = x;
                this.defaultColor = defaultColor == default ? Color.White : defaultColor;
                float fontLineHeight = GetFontLineHeight(font);
                this.infinityRect = new RectangleF(0, 0, ushort.MaxValue, fontLineHeight);

                base.DrawFormatString(s, font, x1 - x, ref y, height, alignment);
            }

            public void DrawPlainText(Graphics g, string s, Font font, Color color, int x, int x1, ref int y, int height, TextAlignment alignment = TextAlignment.Left)
            {
                //初始化环境
                this.g = g;
                this.drawX = x;
                this.defaultColor = color;
                float fontLineHeight = GetFontLineHeight(font);
                this.infinityRect = new RectangleF(0, 0, ushort.MaxValue, fontLineHeight);

                base.DrawPlainText(s, font, x1 - x, ref y, height, alignment);
                /*
                if (TextRenderer.MeasureText(g, s, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width <= x1 - x)
                {
                    TextRenderer.DrawText(g, s, font, new Point(x, y), color, TextFormatFlags.NoPadding);
                    y += height;
                }
                else
                {
                    base.DrawPlainText(s, font, x1 - x, ref y, height);
                }*/
            }

            private float GetFontLineHeight(Font font)
            {
                var ff = font.FontFamily;
                return (float)Math.Ceiling(1.0 * font.Height * ff.GetLineSpacing(font.Style) / ff.GetEmHeight(font.Style));
            }

            protected override void MeasureRuns(List<Run> runs)
            {
                List<Run> tempRuns = new List<Run>(MAX_RANGES);
                int sw = 0;

                foreach (var run in runs)
                {
                    tempRuns.Add(run);
                    if (tempRuns.Count >= MAX_RANGES)
                    {
                        MeasureBatch(tempRuns, sw);
                        var lastrun = tempRuns[tempRuns.Count - 1];
                        sw = lastrun.X + lastrun.Width;
                        tempRuns.Clear();
                    }
                }

                MeasureBatch(tempRuns, sw);

                //failed
                if (runs.Where(run => !run.IsBreakLine && run.Length > 0)
                    .All(run => run.Width == 0))
                {
                    float x = 0;
                    foreach (var run in runs.Where(r => !r.IsBreakLine))
                    {
                        run.X = (int)Math.Round(x);
                        float width = 0;
                        for (int i = 0; i < run.Length; i++)
                        {
                            var chr = this.sb[run.StartIndex + i];
                            width += chr > 0xff ? this.font.Size : (this.font.Size / 2);
                        }
                        run.Width = (int)Math.Round(x);
                        x += width;
                    }
                }
            }

            private void MeasureBatch(List<Run> runs, int sw = 0)
            {
                string text = sb.ToString();
                string currentFontID = "";
                Font currentFont = this.font;
                string fontKey = GearGraphics.GetFontKey(currentFont);
                Size koreanSize = default;
                Size spaceSize = default;
                Size numberSize = default;

                Func<int, bool> isSingleKoreanChar = (i) => i >= 0 && runs[i].Length == 1 && text[runs[i].StartIndex] >= '가' && text[runs[i].StartIndex] <= '힣';
                //var koreanSize = TR.MeasureText(g, "가", font, Size.Round(infinityRect.Size), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                Func<int, bool> isSpace = (i) => i >= 0 && runs[i].Length == 1 && text[runs[i].StartIndex] == ' ';
                //var spaceSize = TR.MeasureText(g, " ", font, Size.Round(infinityRect.Size), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                Func<int, bool> isNumber = (i) => i >= 0 && runs[i].Length == 1 && text[runs[i].StartIndex] >= '0' && text[runs[i].StartIndex] <= '9';
                //var numberSize = TR.MeasureText(g, "0", font, Size.Round(infinityRect.Size), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

                void UpdateSizes(string key)
                {
                    if (!GearGraphics.FontMeasureCache.ContainsKey(key))
                    {
                        GearGraphics.FontMeasureCache[key] = new();
                    }
                    if (!GearGraphics.FontMeasureCache[key].TryGetValue('가', out koreanSize))
                    {
                        koreanSize = TR.MeasureText(g, "가", currentFont, Size.Round(infinityRect.Size), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                        GearGraphics.FontMeasureCache[key]['가'] = koreanSize;
                    }
                    if (!GearGraphics.FontMeasureCache[key].TryGetValue(' ', out spaceSize))
                    {
                        spaceSize = TR.MeasureText(g, " ", currentFont, Size.Round(infinityRect.Size), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                        GearGraphics.FontMeasureCache[key][' '] = spaceSize;
                    }
                    if (!GearGraphics.FontMeasureCache[key].TryGetValue('0', out numberSize))
                    {
                        numberSize = TR.MeasureText(g, "0", currentFont, Size.Round(infinityRect.Size), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                        GearGraphics.FontMeasureCache[key]['0'] = numberSize;
                    }
                }
                UpdateSizes(fontKey);

                if (runs.Count > 0 && !runs.All(run => run.IsBreakLine))
                {
                    fmt.SetMeasurableCharacterRanges(runs.Select(r => new CharacterRange(r.StartIndex, r.Length)).ToArray());
                    var regions = g.MeasureCharacterRanges(text, font, infinityRect, fmt);
                    for (int i = 0; i < runs.Count; i++)
                    {
                        var layout = new RectangleF();
                        if (this.UseGDIRenderer)
                        {
                            if (runs[i].FontID != null && currentFontID != runs[i].FontID)
                            {
                                currentFontID = runs[i].FontID;
                                currentFont = GetFont(runs[i].FontID);
                                fontKey = GearGraphics.GetFontKey(currentFont);
                                UpdateSizes(fontKey);
                            }
                            var prefixLayout = new Point();
                            if (isSingleKoreanChar(i - 1))
                                prefixLayout = new Point(runs[i - 1].X + koreanSize.Width, 0);
                            else if (i > 0 && runs[i - 1].IsImage)
                                prefixLayout = new Point(runs[i - 1].X + runs[i - 1].ImageWidth, 0);
                            else if (isSpace(i - 1))
                                prefixLayout = new Point(runs[i - 1].X + spaceSize.Width, 0);
                            else if (isNumber(i - 1))
                                prefixLayout = new Point(runs[i - 1].X + numberSize.Width, 0);
                            else if (i > 0)
                                prefixLayout = new Point(runs[i - 1].X + runs[i - 1].Width, 0);
                            else
                                prefixLayout = new Point(sw, 0);

                            var currentLayout = new Size();
                            if (isSingleKoreanChar(i))
                                currentLayout = koreanSize;
                            else if (runs[i].IsImage)
                            {
                                currentLayout = new Size(runs[i].ImageWidth, runs[i].ImageHeight);
                            }
                            else if (isSpace(i))
                                currentLayout = spaceSize;
                            else if (isNumber(i))
                                currentLayout = numberSize;
                            else
                            {
                                //currentLayout = TR.MeasureText(g, text.Substring(runs[i].StartIndex, runs[i].Length), currentFont, Size.Round(infinityRect.Size), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                                var textSegment = text.Substring(runs[i].StartIndex, runs[i].Length);
                                if (textSegment.Length == 1)
                                {
                                    if (!GearGraphics.FontMeasureCache[fontKey].TryGetValue(textSegment[0], out currentLayout))
                                    {
                                        currentLayout = TR.MeasureText(g, textSegment, currentFont, Size.Round(infinityRect.Size), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                                        GearGraphics.FontMeasureCache[fontKey][textSegment[0]] = currentLayout;
                                    }
                                }
                                else
                                {
                                    currentLayout = TR.MeasureText(g, textSegment, currentFont, Size.Round(infinityRect.Size), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                                }
                            }

                            layout = new RectangleF(prefixLayout, currentLayout);
                        }
                        else
                            layout = regions[i].GetBounds(g);
                        runs[i].X = (int)Math.Round(layout.Left);
                        runs[i].Width = (int)Math.Round(layout.Width);
                        if (i < regions.Length)
                            regions[i].Dispose();
                    }
                }
            }

            protected override Rectangle[] MeasureChars(int startIndex, int length)
            {
                string word = sb.ToString(startIndex, length);
                Rectangle[] rects = new Rectangle[length];

                for (int i = 0; i < length; i += MAX_RANGES)
                { //批次
                    int chrCount = Math.Min(length - i, MAX_RANGES);
                    fmt.SetMeasurableCharacterRanges(
                        Enumerable.Range(i, chrCount)
                        .Select(start => new CharacterRange(start, 1))
                        .ToArray());
                    var regions = g.MeasureCharacterRanges(word, font, infinityRect, fmt);
                    for (int i1 = 0; i1 < regions.Length; i1++)
                    {
                        var rect = new RectangleF();
                        if (this.UseGDIRenderer)
                            rect = new RectangleF(new Point(0, 0), TR.MeasureText(g, "" + word[i + i1], font, Size.Round(infinityRect.Size), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix));
                        else
                            rect = regions[i1].GetBounds(g);
                        rects[i + i1] = new Rectangle(
                            (int)Math.Round(rect.Left),
                            (int)Math.Round(rect.Top),
                            (int)Math.Round(rect.Width),
                            (int)Math.Round(rect.Height)
                            );
                    }
                }

                //failed
                if (rects.All(rect => rect.Width == 0))
                {
                    float x = 0;
                    for (int i = 0; i < rects.Length; i++)
                    {
                        var chr = this.sb[startIndex + i];
                        var width = chr > 0xff ? this.font.Size : (this.font.Size / 2);
                        rects[i] = new Rectangle(
                            (int)Math.Round(x),
                            0,
                            (int)Math.Round(width),
                            font.Height
                            );
                    }
                }

                return rects;
            }

            protected override void Flush(StringBuilder sb, int startIndex, int length, int x, int y, string colorID, string fontID, string imageID, int imageWidth, int imageHeight)
            {
                string content = sb.ToString(startIndex, length);
                colorID = colorID ?? string.Empty;
                fontID = fontID ?? string.Empty;
                imageID = imageID ?? string.Empty;
                Color color = Color.Transparent; // VS2019 fix
                Font font = this.font;
                Bitmap bmp = null;
                if (!(this.FontColorTable?.TryGetValue(colorID, out color) ?? false))
                {
                    switch (colorID)
                    {
                        case "c": color = GearGraphics.OrangeBrushColor; break;
                        case "$g": color = GearGraphics.SkillHighlightColor; break;
                        default: color = this.defaultColor; break;
                    }
                }
                font = GetFont(fontID);
                if ((this.ImageTable?.TryGetValue(imageID, out bmp) ?? false) && bmp != null) // ImageTable로 전달된 이미지 그리기
                {
                    var dx = Math.Max((imageWidth - bmp.Width) / 2, 0);
                    int dy = font.Height - Math.Min(bmp.Height, imageHeight);
                    switch (this.ImageVerticalAlignment)
                    {
                        case TRImageAlignment.Top:
                            dy = Math.Min(dy, 0);
                            break;
                        case TRImageAlignment.Center:
                            dy = dy / 2 - 1;
                            break;
                        case TRImageAlignment.Bottom:
                            dy = Math.Max(dy, 0);
                            break;
                    }
                    g.DrawImage(bmp, this.drawX + x + dx, y + dy);
                    return;
                }

                if (this.UseGDIRenderer)
                {
                    TR.DrawText(g, content, font, new Point(this.drawX + x, y), color, TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                }
                else
                {
                    using (var brush = new SolidBrush(color))
                    {
                        g.DrawString(content, font, brush, this.drawX + x, y, fmt);
                    }
                }
            }

            private Font GetFont(string fontID)
            {
                if (!(this.FontTable?.TryGetValue(fontID, out var font) ?? false))
                {
                    switch (fontID)
                    {
                        default: font = this.font; break;
                    }
                }
                return font;
            }

            public void Dispose()
            {
                if (fmt != null)
                    fmt.Dispose();
            }
        }

        public enum TRImageAlignment
        {
            Top,
            Center,
            Bottom,
        }
    }
}
