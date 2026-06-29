using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Resource = CharaSimResource.Resource;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;
using WzComparerR2.WzLib;
using System.Text.RegularExpressions;

namespace WzComparerR2.CharaSimControl
{
    public class SkillTooltipRender2 : TooltipRender
    {
        public SkillTooltipRender2()
        {
        }

        public Skill Skill { get; set; }

        public override object TargetItem
        {
            get { return this.Skill; }
            set { this.Skill = value as Skill; }
        }

        public bool ShowProperties { get; set; } = true;
        public bool ShowDelay { get; set; }
        public bool ShowArea { get; set; }
        public bool ShowReqSkill { get; set; }
        public bool DisplayCooltimeMSAsSec { get; set; } = true;
        public bool DisplayPermyriadAsPercent { get; set; } = true;
        public bool IgnoreEvalError { get; set; } = false;
        public bool ShowSkillValuesByJob { get; set; } = false;
        public bool IsWideMode { get; set; } = true;
        public bool Enable22AniStyle { get; set; }
        public SkillLevelViewMode LevelViewMode { get; set; }
        public Dictionary<int, HashSet<string>> DiffSkillTags { get; set; } = new Dictionary<int, HashSet<string>>();
        public Wz_Node SourceWzNode { get; set; } = null;

        public TooltipRender LinkRidingGearRender { get; set; }
        public string ParsedHdesc { get; set; }

        private static readonly Dictionary<string, Bitmap> ImageTable = new Dictionary<string, Bitmap>()
        {
            { "0", Resource.UIToolTip_img_Skill_Icon_0 },
            { "1", Resource.UIToolTip_img_Skill_Icon_1 },
            { "2", Resource.UIToolTip_img_Skill_Icon_2 },
        };

        public override Bitmap Render()
        {
            return Render(false);
        }

        public Bitmap Render(bool doHighlight)
        {
            if (this.Skill == null)
            {
                return null;
            }

            CanvasRegion region = this.IsWideMode ? (this.Enable22AniStyle ? CanvasRegion._22AniWide : CanvasRegion.Wide) : (this.Enable22AniStyle ? CanvasRegion._22AniOriginal : CanvasRegion.Original);
            doHighlight = doHighlight && this.DiffSkillTags.ContainsKey(Skill.SkillID);

            int picHeight;
            List<int> splitterH;
            Bitmap originBmp = RenderSkill(region, out picHeight, out splitterH, doHighlight);
            Bitmap ridingGearBmp = null;
            Bitmap extraBmp = RenderExtra(out int extraWidth, out int extraHeight, doHighlight);

            int vehicleID = Skill.VehicleID;
            if (vehicleID == 0)
            {
                vehicleID = PluginBase.PluginManager.FindWz(string.Format(@"Skill\RidingSkillInfo.img\{0:D7}\vehicleID", Skill.SkillID), this.SourceWzFile).GetValueEx<int>(0);
            }
            if (vehicleID != 0)
            {
                Wz_Node imgNode = PluginBase.PluginManager.FindWz(string.Format(@"Character\TamingMob\{0:D8}.img", vehicleID), this.SourceWzFile);
                if (imgNode != null)
                {
                    Gear gear = Gear.CreateFromNode(imgNode, PluginBase.PluginManager.FindWz, this.SourceWzFile);
                    if (gear != null)
                    {
                        ridingGearBmp = RenderLinkRidingGear(gear);
                    }
                }
            }

            Size totalSize = new Size(originBmp.Width, picHeight);
            Point ridingGearOrigin = Point.Empty;
            Point extraBmpOrigin = Point.Empty;

            if (ridingGearBmp != null)
            {
                totalSize.Width += ridingGearBmp.Width;
                totalSize.Height = Math.Max(picHeight, ridingGearBmp.Height);
                ridingGearOrigin.X = originBmp.Width;
            }

            if (extraBmp != null)
            {
                extraBmpOrigin.X = totalSize.Width;
                totalSize.Width += extraWidth;
                totalSize.Height = Math.Max(picHeight, extraHeight);
            }

            Bitmap tooltip = new Bitmap(totalSize.Width, totalSize.Height);
            Graphics g = Graphics.FromImage(tooltip);

            //绘制背景区域
            GearGraphics.DrawNewTooltipBack(g, 0, 0, originBmp.Width, picHeight);
            if (splitterH != null && splitterH.Count > 0)
            {
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                foreach (var y in splitterH)
                {
                    RenderHelper.DrawV6SkillDotline(g, region.SplitterX1, region.SplitterX2, y, this.Enable22AniStyle);
                }
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            }

            //复制图像
            g.DrawImage(originBmp, 0, 0, new Rectangle(0, 0, originBmp.Width, picHeight), GraphicsUnit.Pixel);

            //左上角
            if (!Enable22AniStyle) g.DrawImage(Resource.UIToolTip_img_Skill_Frame_cover, 3, 3);

            if (this.ShowObjectID)
            {
                GearGraphics.DrawGearDetailNumber(g, 3, 3, Skill.SkillID.ToString("d7"), true);
            }

            if (ridingGearBmp != null)
            {
                g.DrawImage(ridingGearBmp, ridingGearOrigin.X, ridingGearOrigin.Y,
                    new Rectangle(Point.Empty, ridingGearBmp.Size), GraphicsUnit.Pixel);
            }

            if (extraBmp != null)
            {
                GearGraphics.DrawNewTooltipBack(g, extraBmpOrigin.X, extraBmpOrigin.Y, extraWidth, extraHeight);
                g.DrawImage(extraBmp, extraBmpOrigin.X, extraBmpOrigin.Y,
                    new Rectangle(0, 0, extraWidth, extraHeight), GraphicsUnit.Pixel);
            }

            if (originBmp != null)
                originBmp.Dispose();
            if (ridingGearBmp != null)
                ridingGearBmp.Dispose();
            if (extraBmp != null)
                extraBmp.Dispose();

            g.Dispose();
            return tooltip;
        }

        private Bitmap RenderSkill(CanvasRegion region, out int picH, out List<int> splitterH, bool doHighlight = false)
        {
            Bitmap bitmap = new Bitmap(region.Width, DefaultPicHeight);
            Graphics g = Graphics.FromImage(bitmap);
            StringFormat format = (StringFormat)StringFormat.GenericDefault.Clone();
            var v6SkillSummaryFontColorTable = new Dictionary<string, Color>()
            {
                { "c", GearGraphics.SkillSummaryOrangeTextColor },
                { "$g", GearGraphics.SkillHighlightColor }, // color for skill prop changes comparison
                { "$x", ((SolidBrush)GearGraphics.QuestBrushMap).Color }, // color for extra job props
            };

            //初始化 skillCommon
            Dictionary<string, string> skillCommon = new Dictionary<string, string>(Skill.Common);
            if (!ShowSkillValuesByJob && Skill.AttackInfo.Count > 0)
            {
                var perJobInfo = Skill.AttackInfo.ElementAt(Skill.PerJobIndex).Value;
                foreach (var prop in perJobInfo)
                {
                    skillCommon[prop.Key] = prop.Value;
                }
            }

            picH = 0;
            splitterH = new List<int>();

            //获取文字
            if (StringLinker == null || !(StringLinker.StringSkill.TryGetValue(Skill.SkillID, out var _sr) && _sr is StringResultSkill sr))
            {
                sr = new StringResultSkill();
                sr.Name = "(null)";
            }

            //绘制技能名称
            format.Alignment = StringAlignment.Center;
            TextRenderer.DrawText(g, sr.Name, GearGraphics.ItemNameFont2, new Point(bitmap.Width, 10), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);

            //绘制图标
            if (Skill.Icon.Bitmap != null)
            {
                picH = 33;
                g.DrawImage(Resource.UIToolTip_img_Skill_Frame_iconBackgrnd, 13, picH - 2);
                g.DrawImage(GearGraphics.EnlargeBitmap(Skill.Icon.Bitmap),
                15 + (1 - Skill.Icon.Origin.X) * 2,
                picH + (33 - Skill.Icon.Bitmap.Height) * 2);
            }

            // for 6th job skills
            if (Skill.Origin)
            {
                g.DrawImage(Resource.UIWindow2_img_Skill_skillTypeIcon_origin, 16, 11);
            }
            else if (Skill.Ascent)
            {
                g.DrawImage(Resource.UIWindow2_img_Skill_skillTypeIcon_ascent, 16, 11);
            }

            //绘制desc
            picH = 35;
            if (Skill.HyperStat)
                GearGraphics.DrawString(g, "[최대 레벨 : " + Skill.MaxLevel + "]", GearGraphics.ItemDetailFont2, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
            else if (!Skill.PreBBSkill)
                GearGraphics.DrawString(g, "[마스터 레벨 : " + Skill.MaxLevel + "]", GearGraphics.ItemDetailFont2, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);

            if (sr.Desc != null)
            {
                string hdesc = SummaryParser.GetSkillSummary(sr.Desc, Skill.Level, skillCommon, SummaryParams.Default);
                //string hStr = SummaryParser.GetSkillSummary(skill, skill.Level, sr, SummaryParams.Default);
                if (ShowReqSkill && Skill.ReqSkill.Count > 0)
                {
                    foreach (var kv in Skill.ReqSkill)
                    {
                        string skillName;
                        if (this.StringLinker != null && this.StringLinker.StringSkill.TryGetValue(kv.Key, out var sr2))
                        {
                            skillName = sr2.Name;
                        }
                        else
                        {
                            skillName = kv.Key.ToString();
                        }
                        hdesc += $"\n필요 스킬 : #c{skillName} {kv.Value}레벨 이상#";
                    }
                }
                GearGraphics.DrawString(g, hdesc, GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            if (Skill.TimeLimited)
            {
                DateTime time = DateTime.Now.AddDays(7d);
                string expireStr = time.ToString("유효기간 : yyyy년 M월 d일 HH시 mm분");
                GearGraphics.DrawString(g, "#c" + expireStr + "#", GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            if (Skill.RelationSkill != null)
            {
                StringResult sr2 = null;
                if (StringLinker == null || !StringLinker.StringSkill.TryGetValue(Skill.RelationSkill.Item1, out sr2))
                {
                    sr2 = new StringResultSkill();
                    sr2.Name = "(null)";
                }
                DateTime time = DateTime.Now.AddMinutes(Skill.RelationSkill.Item2);
                string expireStr = time.ToString("유효기간 : yyyy년 M월 d일 H시 m분");
                GearGraphics.DrawString(g, "#c" + sr2.Name + "의 " + expireStr + "#", GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            if (Skill.IsSequenceOn)
            {
                string colortag = "#c";
                if (doHighlight && DiffSkillTags[Skill.SkillID].Contains("isSequenceOn"))
                {
                    colortag = "#$g";
                }
                GearGraphics.DrawString(g, colortag + "스킬 시퀀스 등록 가능#", GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            if (Skill.IsPetAutoBuff)
            {
                string colortag = "#c";
                if (doHighlight && DiffSkillTags[Skill.SkillID].Contains("isPetAutoBuff"))
                {
                    colortag = "#$g";
                }
                GearGraphics.DrawString(g, colortag + "펫 버프 자동스킬 등록 가능#", GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            /*if (Skill.ReqLevel > 0)
            {
                GearGraphics.DrawString(g, "#c[要求等级：" + Skill.ReqLevel.ToString() + "]#", GearGraphics.ItemDetailFont2, region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            if (Skill.ReqAmount > 0)
            {
                GearGraphics.DrawString(g, "#c" + ItemStringHelper.GetSkillReqAmount(Skill.SkillID, Skill.ReqAmount) + "#", GearGraphics.ItemDetailFont2, region.SkillDescLeft, region.TextRight, ref picH, 16);
            }*/
            picH += 13;

            //delay rendering v6 splitter
            picH = Math.Max(picH, 114);
            splitterH.Add(picH);
            picH += this.Enable22AniStyle ? 16 : 15;

            var skillSummaryOptions = new SkillSummaryOptions
            {
                ConvertCooltimeMS = this.DisplayCooltimeMSAsSec,
                ConvertPerM = this.DisplayPermyriadAsPercent,
                IgnoreEvalError = this.IgnoreEvalError,
                EndColorOnNewLine = true,
                LevelViewMode = this.LevelViewMode,
            };

            if (Skill.Level > 0)
            {
                // set custom color for skill prop changes
                if (doHighlight)
                {
                    if (Skill.SkillID / 100000 == 4000)
                    {
                        if (Skill.VSkillValue == 2) Skill.Level = 60;
                        if (Skill.VSkillValue == 1) Skill.Level = 30;
                    }
                }
                string nowLevel = this.LevelViewMode == SkillLevelViewMode.CurrentAndSelected && Skill.Level != Skill.ComparisonLevel ?
                    $"[현재레벨 #$g{Skill.Level}{SummaryParams.Default.BracketIcon}{Skill.ComparisonLevel}#]" :
                    $"[현재레벨 {Skill.Level}]";
                string hStr = SummaryParser.GetSkillSummary(Skill, Skill.Level, sr, SummaryParams.Default, skillSummaryOptions, doHighlight, overrideSkillCommon: skillCommon, DiffSkillTags: this.DiffSkillTags, convertExtraProps: !this.ShowSkillValuesByJob);
                GearGraphics.DrawString(g, nowLevel, GearGraphics.ItemDetailFont, null, null, SkillTooltipRender2.ImageTable, region.LevelDescLeft, region.TextRight, ref picH, 16, ImageVerticalAlignment: GearGraphics.TRImageAlignment.Center);
                if (Skill.SkillID / 10000 / 1000 == 10 && Skill.ReqLevel > 0 &&
                    (this.LevelViewMode == SkillLevelViewMode.CurrentAndNext && Skill.Level == 1 || this.LevelViewMode == SkillLevelViewMode.CurrentAndSelected && Skill.ComparisonLevel == 1))
                {
                    GearGraphics.DrawPlainText(g, "[필요 레벨: " + Skill.ReqLevel.ToString() + "레벨 이상]", GearGraphics.ItemDetailFont2, GearGraphics.skillYellowColor, region.LevelDescLeft, region.TextRight, ref picH, 16);
                }
                if (hStr != null)
                {
                    ParsedHdesc = hStr;
                    GearGraphics.DrawString(g, hStr, GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, null, SkillTooltipRender2.ImageTable, region.LevelDescLeft, region.TextRight, ref picH, 16, ImageVerticalAlignment: GearGraphics.TRImageAlignment.Center);
                }
            }

            if ((this.LevelViewMode == SkillLevelViewMode.CurrentAndNext || Skill.Level == 0) &&
                Skill.Level < Skill.MaxLevel && !Skill.DisableNextLevelInfo)
            {
                int targetLevel = this.LevelViewMode == SkillLevelViewMode.CurrentAndSelected ? Skill.ComparisonLevel : Skill.Level + 1;
                skillSummaryOptions.LevelViewMode = SkillLevelViewMode.CurrentAndNext;
                string hStr = SummaryParser.GetSkillSummary(Skill, targetLevel, sr, SummaryParams.Default, skillSummaryOptions, overrideSkillCommon: skillCommon, convertExtraProps: !this.ShowSkillValuesByJob);
                skillSummaryOptions.LevelViewMode = this.LevelViewMode;

                GearGraphics.DrawString(g, "[다음레벨 " + targetLevel + "]", GearGraphics.ItemDetailFont, region.LevelDescLeft, region.TextRight, ref picH, 16);
                if (Skill.SkillID / 10000 / 1000 == 10 && targetLevel == 1 && Skill.ReqLevel > 0)
                {
                    GearGraphics.DrawPlainText(g, "[필요 레벨: " + Skill.ReqLevel.ToString() + "레벨 이상]", GearGraphics.ItemDetailFont2, GearGraphics.skillYellowColor, region.LevelDescLeft, region.TextRight, ref picH, 16);
                }
                if (hStr != null)
                {
                    GearGraphics.DrawString(g, hStr, GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, region.LevelDescLeft, region.TextRight, ref picH, 16);
                }
            }
            picH += 3;

            if (Skill.AddAttackToolTipDescSkill != 0)
            {
                //delay rendering v6 splitter
                splitterH.Add(picH);
                picH += 15;
                GearGraphics.DrawPlainText(g, "[콤비네이션 스킬]", GearGraphics.ItemDetailFont, Color.FromArgb(119, 204, 255), region.LevelDescLeft, region.TextRight, ref picH, 16);
                picH += 4;
                BitmapOrigin icon = new BitmapOrigin();
                Wz_Node skillNode = PluginBase.PluginManager.FindWz(string.Format(@"Skill\{0}.img\skill\{1}", Skill.AddAttackToolTipDescSkill / 10000, Skill.AddAttackToolTipDescSkill), this.SourceWzFile);
                if (skillNode != null)
                {
                    Skill skill = Skill.CreateFromNode(skillNode, PluginBase.PluginManager.FindWz, this.SourceWzFile);
                    icon = skill.Icon;
                }
                if (icon.Bitmap != null)
                {
                    g.DrawImage(icon.Bitmap, 13 - icon.Origin.X, picH + 32 - icon.Origin.Y);
                }
                string skillName;
                if (this.StringLinker != null && this.StringLinker.StringSkill.TryGetValue(Skill.AddAttackToolTipDescSkill, out var sr2))
                {
                    skillName = sr2.Name;
                }
                else
                {
                    skillName = Skill.AddAttackToolTipDescSkill.ToString();
                }
                picH += 10;
                GearGraphics.DrawString(g, skillName, GearGraphics.ItemDetailFont, region.LinkedSkillNameLeft, region.TextRight, ref picH, 16);
                picH += 6;
                picH += 13;
            }

            if (Skill.AssistSkillLink != 0)
            {
                //delay rendering v6 splitter
                splitterH.Add(picH);
                picH += 15;
                GearGraphics.DrawPlainText(g, "[어시스트 스킬]", GearGraphics.ItemDetailFont, GearGraphics.SkillSummaryOrangeTextColor, region.LevelDescLeft, region.TextRight, ref picH, 16);
                picH += 4;
                BitmapOrigin icon = new BitmapOrigin();
                Wz_Node skillNode = PluginBase.PluginManager.FindWz(string.Format(@"Skill\{0}.img\skill\{1}", Skill.AssistSkillLink / 10000, Skill.AssistSkillLink), this.SourceWzFile);
                if (skillNode != null)
                {
                    Skill skill = Skill.CreateFromNode(skillNode, PluginBase.PluginManager.FindWz, this.SourceWzFile);
                    icon = skill.Icon;
                }
                if (icon.Bitmap != null)
                {
                    g.DrawImage(icon.Bitmap, 13 - icon.Origin.X, picH + 32 - icon.Origin.Y);
                }
                string skillName;
                if (this.StringLinker != null && this.StringLinker.StringSkill.TryGetValue(Skill.AssistSkillLink, out var sr2))
                {
                    skillName = sr2.Name;
                }
                else
                {
                    skillName = Skill.AssistSkillLink.ToString();
                }
                picH += 10;
                GearGraphics.DrawString(g, skillName, GearGraphics.ItemDetailFont, region.LinkedSkillNameLeft, region.TextRight, ref picH, 16);
                picH += 6;
                picH += 13;
            }

            List<string> skillDescEx = new List<string>();
            if (ShowProperties)
            {
                List<string> attr = new List<string>();
                if (Skill.ReqLevel > 0)
                {
                    attr.Add("필요 레벨: " + Skill.ReqLevel);
                }
                if (Skill.Invisible)
                {
                    attr.Add("스킬창에 표시되지 않음");
                }
                if (Skill.Hyper != HyperSkillType.None)
                {
                    attr.Add("하이퍼스킬: " + Skill.Hyper);
                }
                if (Skill.CombatOrders)
                {
                    attr.Add("컴뱃오더스 적용 가능");
                }
                if (Skill.NotRemoved)
                {
                    attr.Add("버프 해제 불가");
                }
                if (Skill.MasterLevel > 0 && Skill.MasterLevel < Skill.MaxLevel)
                {
                    attr.Add("마스터리북 미사용시 마스터 레벨: Lv." + Skill.MasterLevel);
                }

                if (attr.Count > 0)
                {
                    skillDescEx.Add("#c" + string.Join(", ", attr.ToArray()) + "#");
                }
            }

            if (ShowDelay && Skill.Action.Count > 0)
            {
                foreach (string action in Skill.Action)
                {
                    string colortag = "";
                    if (doHighlight && DiffSkillTags[Skill.SkillID].Contains(action))
                    {
                        colortag = "#$g";
                    }
                    skillDescEx.Add("#c[딜레이] " + colortag + action + ": " + CharaSimLoader.GetActionDelay(action, this.SourceWzNode) + " ms#");
                }
            }

            if (ShowArea && Skill.Lt.Count > 0)
            {
                foreach (var kv in Skill.Lt)
                {
                    if (!Skill.Rb.ContainsKey(kv.Key))
                    {
                        continue;
                    }
                    string colortag = "";
                    if (doHighlight && (DiffSkillTags[Skill.SkillID].Contains("lt" + kv.Key) || DiffSkillTags[Skill.SkillID].Contains("rb" + kv.Key)))
                    {
                        colortag = "#$g";
                    }
                    skillDescEx.Add("#c[범위" + kv.Key + "(px)] " + colortag + "좌: " + kv.Value.X + ", 우: " + Skill.Rb[kv.Key].X + ", 상: " + kv.Value.Y + ", 하: " + Skill.Rb[kv.Key].Y + "" +
                        ", 영역: " + Math.Abs(Skill.Rb[kv.Key].X - kv.Value.X) + " x " + Math.Abs(kv.Value.Y - Skill.Rb[kv.Key].Y) + "#");
                }
            }
            
            if (!ShowSkillValuesByJob && Skill.AttackInfo.Count > 0)
            {
                int jobID = Skill.AttackInfo.ElementAt(Skill.PerJobIndex).Key;
                skillDescEx.Add($"#c[기준 직업] {ItemStringHelper.GetJobName(jobID)}({jobID})#");
            }

            if (skillDescEx.Count > 0)
            {
                //delay rendering v6 splitter
                splitterH.Add(picH);
                picH += 9;
                foreach (var descEx in skillDescEx)
                {
                    GearGraphics.DrawString(g, descEx, GearGraphics.ItemDetailFont, v6SkillSummaryFontColorTable, region.LevelDescLeft, region.TextRight, ref picH, 16);
                }
                picH += 3;
            }

            picH += 6;

            format.Dispose();
            g.Dispose();
            return bitmap;
        }

        private Bitmap RenderLinkRidingGear(Gear gear)
        {
            TooltipRender renderer = this.LinkRidingGearRender;
            if (renderer == null)
            {
                if (this.Enable22AniStyle)
                {
                    GearTooltipRender22 defaultRenderer = new GearTooltipRender22();
                    defaultRenderer.StringLinker = this.StringLinker;
                    defaultRenderer.ShowObjectID = false;
                    renderer = defaultRenderer;
                }
                else
                {
                    GearTooltipRender2 defaultRenderer = new GearTooltipRender2();
                    defaultRenderer.StringLinker = this.StringLinker;
                    defaultRenderer.ShowObjectID = false;
                    renderer = defaultRenderer;
                }
            }

            renderer.TargetItem = gear;
            return renderer.Render();
        }

        private Bitmap RenderExtra(out int extraWidth, out int extraHeight, bool doHighlight)
        {
            extraWidth = 0;
            extraHeight = 0;
            if (!ShowSkillValuesByJob || this.Skill.ExtraPropNames.Count == 0)
                return null;

            const int Margin = 14;
            const int Interval = 150;
            const int Line_Height = 15;
            const int Max_Height = 850;

            // calculate width and height
            var box = this.Skill.AttackInfo.Values.Select(list => list.Count + 1);
            int picH = Margin;
            extraWidth += Margin + Interval;
            List<int> rows = new List<int>();
            int count = 0;
            foreach (int h in box)
            {
                var addH = h * Line_Height;;
                if (picH + addH > Max_Height)
                {
                    extraWidth += Interval;
                    extraHeight = Math.Max(extraHeight, picH + Margin);
                    picH = Margin;
                    rows.Add(count);
                    count = 0;
                }
                picH += addH;
                count++;
            }
            rows.Add(count);

            Bitmap bitmap = new Bitmap(extraWidth, extraHeight);
            using Graphics g = Graphics.FromImage(bitmap);
            StringFormat format = (StringFormat)StringFormat.GenericDefault.Clone();
            var v6SkillSummaryFontColorTable = new Dictionary<string, Color>()
            {
                { "c", GearGraphics.SkillSummaryOrangeTextColor },
                { "$g", GearGraphics.SkillHighlightColor }, // color for skill prop changes comparison
                { "$x", ((SolidBrush)GearGraphics.QuestBrushMap).Color }, // color for extra job props
            };

            var skillSummaryOptions = new SkillSummaryOptions
            {
                ConvertCooltimeMS = this.DisplayCooltimeMSAsSec,
                ConvertPerM = this.DisplayPermyriadAsPercent,
                IgnoreEvalError = this.IgnoreEvalError,
                EndColorOnNewLine = true,
                LevelViewMode = this.LevelViewMode,
            };

            picH = Margin;
            int sx = 0;
            int col = 0;
            count = 0;
            foreach (var kv in Skill.AttackInfo)
            {
                GearGraphics.DrawString(g, $"#c[{Regex.Replace(ItemStringHelper.GetJobName(kv.Key), @"\s*\(\d{1,2}차\)$", "")}({kv.Key})]#", GearGraphics.EquipMDMoris9Font, v6SkillSummaryFontColorTable, sx + Margin, sx + Interval - Margin, ref picH, Line_Height);
                foreach (var prop in kv.Value)
                {
                    List<string> values = new List<string>();
                    bool showCurLv = Skill.Level > 0;
                    string tag = $"attackInfo/{kv.Key}/{prop.Key}";
                    bool containsTag = false;
                    if (doHighlight && DiffSkillTags[Skill.SkillID].Contains(tag))
                    {
                        containsTag = true;
                    }

                    if (showCurLv)
                    {
                        var val = SummaryParser.CalcSingleProp(Skill.Level, prop.Key, prop.Value, skillSummaryOptions);
                        if (this.LevelViewMode == SkillLevelViewMode.CurrentAndSelected && Skill.Level != Skill.ComparisonLevel)
                        {
                            var val2 = SummaryParser.CalcSingleProp(Skill.ComparisonLevel, prop.Key, prop.Value, skillSummaryOptions);
                            if (val != val2)
                            {
                                values.Add($"#$g{val}{SummaryParams.Default.BracketIcon}{val2}#");
                            }
                            else values.Add(val);
                        }
                        else values.Add(val);
                    }

                    string finalText = containsTag ?
                        $"    #$g{prop.Key} {string.Join(", ", values)}#" :
                        $"    #$x{prop.Key}# {string.Join(", ", values)}";
                    GearGraphics.DrawString(g, finalText, GearGraphics.EquipMDMoris9Font, v6SkillSummaryFontColorTable, null, SkillTooltipRender2.ImageTable, sx + Margin, sx + Interval * 2, ref picH, Line_Height, ImageVerticalAlignment: GearGraphics.TRImageAlignment.Center);
                }
                if (++count == rows[col])
                {
                    picH = Margin;
                    sx += Interval;
                    count = 0;
                    col++;
                }
            }

            return bitmap;
        }

        private class CanvasRegion
        {
            public int Width { get; private set; }
            public int TitleCenterX { get; private set; }
            public int SplitterX1 { get; private set; }
            public int SplitterX2 { get; private set; }
            public int SkillDescLeft { get; private set; }
            public int LinkedSkillNameLeft { get; private set; }
            public int LevelDescLeft { get; private set; }
            public int TextRight { get; private set; }

            public static CanvasRegion Original { get; } = new CanvasRegion()
            {
                Width = 290,
                TitleCenterX = 144,
                SplitterX1 = 4,
                SplitterX2 = 284,
                SkillDescLeft = 90,
                LinkedSkillNameLeft = 46,
                LevelDescLeft = 8,
                TextRight = 272,
            };

            public static CanvasRegion Wide { get; } = new CanvasRegion()
            {
                Width = 430,
                TitleCenterX = 215,
                SplitterX1 = 4,
                SplitterX2 = 424,
                SkillDescLeft = 92,
                LinkedSkillNameLeft = 49,
                LevelDescLeft = 13,
                TextRight = 411,
            };

            public static CanvasRegion _22AniOriginal { get; } = new CanvasRegion()
            {
                Width = 290,
                TitleCenterX = 144,
                SplitterX1 = 12,
                SplitterX2 = 276,
                SkillDescLeft = 90,
                LinkedSkillNameLeft = 46,
                LevelDescLeft = 8,
                TextRight = 272,
            };

            public static CanvasRegion _22AniWide { get; } = new CanvasRegion()
            {
                Width = 430,
                TitleCenterX = 215,
                SplitterX1 = 12,
                SplitterX2 = 416,
                SkillDescLeft = 92,
                LinkedSkillNameLeft = 49,
                LevelDescLeft = 13,
                TextRight = 411,
            };
        }
    }
}
