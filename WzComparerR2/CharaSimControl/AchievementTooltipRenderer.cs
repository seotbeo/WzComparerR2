using CharaSimResource;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using WzComparerR2.AvatarCommon;
using WzComparerR2.CharaSim;
using WzComparerR2.Common;
using WzComparerR2.PluginBase;
using WzComparerR2.Properties;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSimControl
{
    public class AchievementTooltipRenderer : TooltipRender
    {
        static AchievementTooltipRenderer()
        {
        }

        public AchievementTooltipRenderer()
        {
            this.sourceWzFile = null;
            this.RewardRectnItems = new List<Tuple<Rectangle, object>>();
        }

        public bool CompareMode { get; set; } = false;
        public Achievement Achievement { get; set; }
        public Wz_File sourceWzFile { get; set; }
        public List<Tuple<Rectangle, object>> RewardRectnItems { get; set; }

        public override object TargetItem
        {
            get
            {
                return this.Achievement;
            }
            set
            {
                this.Achievement = value as Achievement;
            }
        }

        public override Bitmap Render()
        {
            if (this.Achievement == null)
            {
                return null;
            }

            // 리소스 준비
            this.RewardRectnItems.Clear();
            StringFormat format = (StringFormat)StringFormat.GenericTypographic.Clone();
            var bg_top = Resource.UIAchievement_img_achievement_pages_normalCategory_achievementForm_all_incomplete_top;
            var bg_pattern = Resource.UIAchievement_img_achievement_pages_normalCategory_achievementForm_all_incomplete_pattern;
            var bg_bottom = this.Achievement.HasRewards ? Resource.UIAchievement_img_achievement_pages_normalCategory_achievementForm_all_incomplete_bottom
                : Resource.UIAchievement_img_achievement_pages_normalCategory_achievementForm_mission_incomplete_bottom;

            int patternCnt = (this.Achievement.ShowMissions ? (this.Achievement.Missions.Count + 1) / 2 : 0);
            var categoryList = new List<string>() { this.Achievement.MainCategory, this.Achievement.SubCategory };
            var category = string.Join(" / ", categoryList.Where(t => !string.IsNullOrEmpty(t)));
            if (!string.IsNullOrEmpty(category)) patternCnt++;
            if (this.Achievement.PriorIDs.Count > 0) patternCnt++;
            if (this.Achievement.Hide) patternCnt++;

            Bitmap bmp = new Bitmap(bg_top.Width, bg_top.Height + bg_pattern.Height * patternCnt + bg_bottom.Height);
            using Graphics g = Graphics.FromImage(bmp);

            /************/
            // 상단
            var picH = 0;
            g.DrawImage(bg_top, 0, picH);
            picH += bg_top.Height;

            // 이름
            var id = this.Achievement.ID;
            StringResult sr;
            if (StringLinker == null || !StringLinker.StringAchievement.TryGetValue(id, out sr))
            {
                sr = new StringResult();
                sr.Name = "(null)";
            }
            var name = sr.Name;
            g.DrawString(Compact(g, name, 450, GearGraphics.AchievementTitleFont), GearGraphics.AchievementTitleFont, new SolidBrush(Color.White), new Point(84, 12), format);
            //TextRenderer.DrawText(g, sr.Name, GearGraphics.AchievementTitleFont, new Point(84, 12), Color.White, TextFormatFlags.NoPadding);

            // 등급
            var difficulty = !string.IsNullOrEmpty(this.Achievement.Difficulty) ? this.Achievement.Difficulty : "normal";
            var gradePath = $"UIAchievement_img_achievement_pages_normalCategory_achievementForm_basic_difficultyIcon_{difficulty}";
            Bitmap grade = (Bitmap)Resource.ResourceManager.GetObject(gradePath);
            if (grade != null)
            {
                if (difficulty == "normal")
                {
                    g.DrawImage(grade, 28, 18);
                }
                else g.DrawImage(grade, 23, 15);
            }
            // 점수
            if (this.Achievement.Score > 0)
            {
                DrawScore(g, 42, 50, this.Achievement.Score.ToString());
            }

            // 설명
            var picHD = 40;
            var timeParseFormat = "yyyyMMddHHmmss";
            var timeConvertFormat = "yyyy년 MM월 dd일 HH시 mm분";
            if (!string.IsNullOrEmpty(this.Achievement.Start) && !string.IsNullOrEmpty(this.Achievement.End)
                && DateTime.TryParseExact(this.Achievement.Start, timeParseFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start)
                && DateTime.TryParseExact(this.Achievement.End, timeParseFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
            {
                var time = $"{start.ToString(timeConvertFormat)} ~ {end.ToString(timeConvertFormat)}";
                GearGraphics.DrawString(g, time, GearGraphics.EquipMDMoris9Font, null, 84, 530, ref picHD, 15, defaultColor: ((SolidBrush)GearGraphics.AchievementPeriodBrush).Color);
            }
            var desc = sr.Desc;
            if (!string.IsNullOrEmpty(desc))
            {
                GearGraphics.DrawString(g, desc, GearGraphics.EquipMDMoris9Font, null, 84, 530, ref picHD, 15, defaultColor: GearGraphics.GrayColor2);
            }

            /************/
            // 중단
            var picHP = picH;
            for (int i = 0; i < patternCnt; i++)
            {
                g.DrawImage(bg_pattern, 0, picHP);
                picHP += bg_pattern.Height;
            }
            picH += 9;

            // 미션
            if (this.Achievement.ShowMissions)
            {
                Bitmap checkComplete = Resource.UIAchievement_img_achievement_pages_normalCategory_achievementForm_mission_mission_complete;
                var col = 0;
                var dx = 270;

                foreach (var mission in this.Achievement.Missions)
                {
                    g.DrawImage(checkComplete, 16 + dx * col, picH);
                    TextRenderer.DrawText(g, Compact(g, mission, 235), GearGraphics.EquipMDMoris9Font, new Point(32 + dx * col, picH + 1), Color.White, TextFormatFlags.NoPadding);

                    picH += bg_pattern.Height * col;
                    col = (++col) % 2;
                }
                picH += bg_pattern.Height * col;
            }

            // 카테고리
            Bitmap checkIncomplete = Resource.UIAchievement_img_achievement_pages_normalCategory_achievementForm_mission_mission_incomplete;
            if (!string.IsNullOrEmpty(category))
            {
                g.DrawImage(checkIncomplete, 16, picH);
                GearGraphics.DrawString(g, $"카테고리: {category}", GearGraphics.EquipMDMoris9Font, null, 32, 530, ref picH, bg_pattern.Height, defaultColor: GearGraphics.GrayColor2);
            }

            // 선행 업적
            if (this.Achievement.PriorIDs.Count > 0)
            {
                g.DrawImage(checkIncomplete, 16, picH);
                GearGraphics.DrawString(g, Compact(g, $"선행 업적: {string.Join(", ", this.Achievement.PriorIDs.Select(id =>
                {
                    if (StringLinker == null || !StringLinker.StringAchievement.TryGetValue(id, out var sr))
                    {
                        sr = new StringResult();
                        sr.Name = "null";
                    }
                    return $"{sr.Name}({id:D5})";
                }))}", 500), GearGraphics.EquipMDMoris9Font, null, 32, 530, ref picH, bg_pattern.Height, defaultColor: GearGraphics.GrayColor2);
            }

            // hide
            if (this.Achievement.Hide)
            {
                g.DrawImage(checkIncomplete, 16, picH);
                GearGraphics.DrawString(g, "업적창에 표시되지 않음", GearGraphics.EquipMDMoris9Font, null, 32, 530, ref picH, bg_pattern.Height, defaultColor: GearGraphics.GrayColor2);
            }

            /************/
            // 하단
            picH = picHP;
            g.DrawImage(bg_bottom, 0, picH);

            // 보상
            if (this.Achievement.HasRewards)
            {
                var reward = this.Achievement.Rewards[0];
                var itemNode = FindItemNode(reward.ID);
                var icon = GetIcon(reward.ID, itemNode);
                if (icon.Bitmap != null)
                {
                    var x = 18 - icon.Origin.X;
                    var y = picH + 49 - icon.Origin.Y;
                    g.DrawImage(icon.Bitmap, x, y);

                    if (!CompareMode)
                    {
                        var rectW = icon.Bitmap.Width;
                        var rectH = icon.Bitmap.Height;
                        var rect = new Rectangle(x, y, rectW, rectH);
                        this.RewardRectnItems.Add(new Tuple<Rectangle, object>(rect, GetItemBase(reward.ID, itemNode)));
                    }
                    icon.Bitmap.Dispose();
                }
                TextRenderer.DrawText(g, Compact(g, reward.Desc, 448), GearGraphics.EquipMDMoris9Font, new Point(104, picH + 28), ((SolidBrush)GearGraphics.AchievementRewardBrush).Color, TextFormatFlags.NoPadding);
            }

            // ID 표시
            if (this.ShowObjectID)
            {
                GearGraphics.DrawGearDetailNumber(g, 3, 3, this.Achievement.ID.ToString("D5"), true);
            }

            return bmp;
        }

        private static void DrawScore(Graphics g, int x, int y, string num)
        {
            x -= 5 * num.Length;
            for (int i = 0; i < num.Length; i++)
            {
                string resourceName = $"UIAchievement_img_numbers_scoreSmall_{num[i]}";
                Bitmap bitmap = (Bitmap)Resource.ResourceManager.GetObject(resourceName);
                if (bitmap != null)
                {
                    g.DrawImage(bitmap, x, y);
                    x += bitmap.Width;
                }
            }
        }

        private Bitmap GetIconBitmap(int id, Wz_Node itemNode = null, bool raw = false)
        {
            return GetIcon(id, null, raw).Bitmap;
        }

        private BitmapOrigin GetIcon(int id, Wz_Node itemNode = null, bool raw = false)
        {
            if (itemNode == null)
                itemNode = FindItemNode(id);

            Wz_Node iconNode = itemNode?.FindNodeByPath($@"info\icon{(raw ? "Raw" : "")}");
            if (iconNode != null)
            {
                return BitmapOrigin.CreateFromNode(iconNode, PluginManager.FindWz, this.SourceWzFile);
            }

            return new BitmapOrigin();
        }

        private Wz_Node FindItemNode(int id)
        {
            if (id >= 2000000)
            {
                // item
                string itemType = Item.GetItemType(id).ToString();
                string nodePath = (id / 10000 == 500) ? $@"{id:D7}.img"
                    : (id / 1000 == 3015) ? $@"{(id / 100):D6}.img\{id:D8}"
                    : (id / 10000 == 301) ? $@"{(id / 1000):D5}.img\{id:D8}"
                    : $@"{(id / 10000):D4}.img\{id:D8}";
                return PluginManager.FindWz($@"Item\{itemType}\{nodePath}", this.SourceWzFile);
            }
            else
            {
                // eqp
                string nodePath = $@"{id:D8}.img";
                Wz_Node CharaWzNode = PluginManager.FindWz(Wz_Type.Character, this.SourceWzFile);
                Wz_Node gearNode = null;
                foreach (var category in CharaWzNode?.Nodes ?? new Wz_Node.WzNodeCollection(null))
                {
                    if (category.Text.ToLower().Contains("canvas")) continue;

                    if (category.Text.Contains(".img") && category.Text == nodePath)
                    {
                        var img = category.GetValueEx<Wz_Image>(null);
                        if (img != null)
                        {
                            gearNode = img.TryExtract() ? img.Node : null;
                        }
                        return gearNode;
                    }

                    gearNode = category.FindNodeByPath(nodePath);
                    if (gearNode != null)
                    {
                        var img = gearNode.GetValueEx<Wz_Image>(null);
                        if (img != null)
                        {
                            gearNode = img.TryExtract() ? img.Node : null;
                        }
                        return gearNode;
                    }
                }
            }
            return null;
        }

        private object GetItemBase(int id, Wz_Node node)
        {
            if (id >= 2000000)
            {
                // item
                return Item.CreateFromNode(node, PluginManager.FindWz, this.SourceWzFile);
            }
            else
            {
                // eqp
                return Gear.CreateFromNode(node, PluginManager.FindWz, this.SourceWzFile);
            }

            return null;
        }

        private static string Compact(Graphics g, string text, int width, Font font = null) // https://www.codeproject.com/Articles/37503/Auto-Ellipsis
        {
            if (font == null) font = GearGraphics.EquipMDMoris9Font;
            Size s = TextRenderer.MeasureText(g, text, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);

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

                s = TextRenderer.MeasureText(g, tst, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);

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
    }
}