using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using Resource = CharaSimResource.Resource;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;
using WzComparerR2.WzLib;

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
        public bool ShowReqSkill { get; set; } = true;
        public bool DisplayCooltimeMSAsSec { get; set; } = true;
        public bool DisplayPermyriadAsPercent { get; set; } = true;
        public bool IgnoreEvalError { get; set; } = false;
        public bool IsWideMode { get; set; } = true;

        public TooltipRender LinkRidingGearRender { get; set; }

        public override Bitmap Render()
        {
            if (this.Skill == null)
            {
                return null;
            }

            CanvasRegion region = this.IsWideMode ? CanvasRegion.Wide : CanvasRegion.Original;

            int picHeight;
            List<int> splitterH;
            Bitmap originBmp = RenderSkill(region, out picHeight, out splitterH);
            Bitmap ridingGearBmp = null;

            int vehicleID = Skill.VehicleID;
            if (vehicleID == 0)
            {
                vehicleID = PluginBase.PluginManager.FindWz(string.Format(@"Skill\RidingSkillInfo.img\{0:D7}\vehicleID", Skill.SkillID)).GetValueEx<int>(0);
            }
            if (vehicleID != 0)
            {
                Wz_Node imgNode = PluginBase.PluginManager.FindWz(string.Format(@"Character\TamingMob\{0:D8}.img", vehicleID));
                if (imgNode != null)
                {
                    Gear gear = Gear.CreateFromNode(imgNode, path => PluginBase.PluginManager.FindWz(path));
                    if (gear != null)
                    {
                        ridingGearBmp = RenderLinkRidingGear(gear);
                    }
                }
            }

            Size totalSize = new Size(originBmp.Width, picHeight);
            Point ridingGearOrigin = Point.Empty;

            if (ridingGearBmp != null)
            {
                totalSize.Width += ridingGearBmp.Width;
                totalSize.Height = Math.Max(picHeight, ridingGearBmp.Height);
                ridingGearOrigin.X = originBmp.Width;
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
                    DrawV6SkillDotline(g, region.SplitterX1, region.SplitterX2, y);
                }
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            }

            //复制图像
            //g.DrawImage(originBmp, 0, 0, new Rectangle(0, 0, 500, picHeight), GraphicsUnit.Pixel);
            g.DrawImage(originBmp, 0, 0, new Rectangle(0, 0, originBmp.Width, picHeight), GraphicsUnit.Pixel);

            //左上角
            g.DrawImage(Resource.UIToolTip_img_Skill_Frame_cover, 3, 3);

            if (this.ShowObjectID)
            {
                GearGraphics.DrawGearDetailNumber(g, 3, 3, Skill.SkillID.ToString("d7"), true);
            }

            if (ridingGearBmp != null)
            {
                g.DrawImage(ridingGearBmp, ridingGearOrigin.X, ridingGearOrigin.Y,
                    new Rectangle(Point.Empty, ridingGearBmp.Size), GraphicsUnit.Pixel);
            }

            if (originBmp != null)
                originBmp.Dispose();
            if (ridingGearBmp != null)
                ridingGearBmp.Dispose();

            g.Dispose();
            return tooltip;
        }

        private Bitmap RenderSkill(CanvasRegion region, out int picH, out List<int> splitterH)
        {
            Bitmap bitmap = new Bitmap(region.Width, DefaultPicHeight);
            Graphics g = Graphics.FromImage(bitmap);
            StringFormat format = (StringFormat)StringFormat.GenericDefault.Clone();
            var v6SkillSummaryFontColorTable = new Dictionary<string, Color>()
            {
                { "c", GearGraphics.SkillSummaryOrangeTextColor },
            };

            picH = 0;
            splitterH = new List<int>();

            //获取文字
            StringResult sr;
            if (StringLinker == null || !StringLinker.StringSkill.TryGetValue(Skill.SkillID, out sr))
            {
                sr = new StringResultSkill();
                sr.Name = "(null)";
            }

            if (!Skill.Origin)
            {
                g.DrawImage(Resource.ToolTip_Equip_Dot_0, 9, picH + 15);//GMS Version blue dot in SKILLS
                format.Alignment = StringAlignment.Near;
                TextRenderer.DrawText(g, sr.Name, GearGraphics.ItemNameFont2, new Point(13, 10), Color.White, TextFormatFlags.Left | TextFormatFlags.NoPrefix);
            }

            // for 6th job skills
            if (Skill.Origin)
            {
                g.DrawImage(Resource.UIWindow2_img_Skill_skillTypeIcon_origin, 20, 11);
                g.DrawImage(Resource.ToolTip_Equip_Dot_0, 92, picH + 15);//GMS Version blue dot in SKILLS
                format.Alignment = StringAlignment.Near;
                TextRenderer.DrawText(g, sr.Name, GearGraphics.ItemNameFont2, new Point(96, 10), Color.White, TextFormatFlags.Left | TextFormatFlags.NoPrefix);
            }

            //UPDATE THIS NEXT
            //绘制技能名称
            //g.DrawImage(Resource.ToolTip_Equip_Dot_0, 9, picH + 15);//GMS Version blue dot in SKILLS
            //format.Alignment = StringAlignment.Near;
            //TextRenderer.DrawText(g, sr.Name, GearGraphics.ItemNameFont2, new Point(13, 10), Color.White, TextFormatFlags.Left | TextFormatFlags.NoPrefix);

            //绘制图标
            if (Skill.Icon.Bitmap != null)
            {
                picH = 33;
                g.DrawImage(Resource.UIToolTip_img_Skill_Frame_iconBackgrnd, 13, picH - 2);
                g.DrawImage(GearGraphics.EnlargeBitmap(Skill.Icon.Bitmap),
                15 + (1 - Skill.Icon.Origin.X) * 2,
                picH + (33 - Skill.Icon.Bitmap.Height) * 2);
            }

            //绘制desc
            picH = 35;
            if (Skill.HyperStat)
                //GearGraphics.DrawString(g, "[Master Level: " + Skill.MaxLevel + "]", GearGraphics.ItemDetailFont2, 10, 485, ref picH, 16);
                GearGraphics.DrawString(g, "[Master Level: " + Skill.MaxLevel + "]", GearGraphics.ItemDetailFont2, region.LevelDescLeft, region.TextRight, ref picH, 16);
            else if (!Skill.PreBBSkill)
                //GearGraphics.DrawString(g, "[Master Level: " + Skill.MaxLevel + "]", GearGraphics.ItemDetailFont2, 86, 485, ref picH, 16);//original values: 90, 272
                GearGraphics.DrawString(g, "[Master Level: " + Skill.MaxLevel + "]", GearGraphics.ItemDetailFont2, region.SkillDescLeft, region.TextRight, ref picH, 16);

            if (sr.Desc != null)
            {
                string hdesc = SummaryParser.GetSkillSummary(sr.Desc, Skill.Level, Skill.Common, SummaryParams.Default);
                //string hStr = SummaryParser.GetSkillSummary(skill, skill.Level, sr, SummaryParams.Default);
                //GearGraphics.DrawString(g, hdesc, GearGraphics.ItemDetailFont2, 86, 485, ref picH, 16);//original values: 90, 272
                GearGraphics.DrawString(g, hdesc, GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            if (Skill.TimeLimited)
            {
                DateTime time = DateTime.Now.AddDays(7d);
                string expireStr = "Expiration Date: " + time.ToString("M\\/d\\/yyyy HH:mm");
                //GearGraphics.DrawString(g, "#c" + expireStr + "#", GearGraphics.ItemDetailFont2, 86, 485, ref picH, 26);//original values, 92, 274, 16. '400' is GMS sync
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
                string expireStr = " Expiration Date: " + time.ToString(@"M\/d\/yyyy HH\:mm") + " UTC"; ;//Change when Permanent Thunder Horse is given to players.
                //GearGraphics.DrawString(g, "#c" + sr2.Name + expireStr + "#", GearGraphics.ItemDetailFont2, Skill.Icon.Bitmap == null ? 10 : 86, 485, ref picH, 16);
                GearGraphics.DrawString(g, "#c" + sr2.Name + expireStr + "#", GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            if (Skill.IsPetAutoBuff)
            {
                GearGraphics.DrawString(g, "#cCan add Auto Buff Skill#", GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            if (Skill.SkillID / 10000 / 1000 == 10 && Skill.ReqLevel > 0)
            {
                GearGraphics.DrawString(g, "#c[Level Required: " + Skill.ReqLevel.ToString() + " or above]#", GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 40);
            }
            if (Skill.ReqAmount > 0)
            {
                GearGraphics.DrawString(g, "#c" + ItemStringHelper.GetSkillReqAmount(Skill.SkillID, Skill.ReqAmount) + "#", GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            picH += 13;

            //delay rendering v6 splitter
            picH = Math.Max(picH, 114);
            splitterH.Add(picH);
            picH += 15;

            if (Skill.Level > 0)
            {
                string hStr = SummaryParser.GetSkillSummary(Skill, Skill.Level, sr, SummaryParams.Default, new SkillSummaryOptions
                {
                    ConvertCooltimeMS = this.DisplayCooltimeMSAsSec,
                    ConvertPerM = this.DisplayPermyriadAsPercent,
                    IgnoreEvalError = this.IgnoreEvalError,
                });
                //GearGraphics.DrawString(g, "[Current Level " + Skill.Level + "]", GearGraphics.ItemDetailFont, 9, 485, ref picH, 16);//original values: 10, 274
                GearGraphics.DrawString(g, "[Current Level " + Skill.Level + "]", GearGraphics.ItemDetailFont, region.LevelDescLeft, region.TextRight, ref picH, 16);
                if (Skill.SkillID / 10000 / 1000 == 10 && Skill.Level == 1 && Skill.ReqLevel > 0)
                {
                    //GearGraphics.DrawPlainText(g, "(Level Required: " + Skill.ReqLevel.ToString() + " or above)", GearGraphics.ItemDetailFont2, GearGraphics.skillYellowColor, 10, 485, ref picH, 16); *Related to Zero skills
                    //GearGraphics.DrawPlainText(g, "[필요 레벨: " + Skill.ReqLevel.ToString() + "레벨 이상]", GearGraphics.ItemDetailFont2, GearGraphics.skillYellowColor, region.LevelDescLeft, region.TextRight, ref picH, 16);
                }
                if (hStr != null)
                {
                    //GearGraphics.DrawString(g, hStr, GearGraphics.ItemDetailFont2, 10, 485, ref picH, 16);//original values: 10, 274
                    GearGraphics.DrawString(g, hStr, GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, region.LevelDescLeft, region.TextRight, ref picH, 16);
                }
            }

            if (Skill.Level < Skill.MaxLevel && !Skill.DisableNextLevelInfo)
            {
                string hStr = SummaryParser.GetSkillSummary(Skill, Skill.Level + 1, sr, SummaryParams.Default, new SkillSummaryOptions
                {
                    ConvertCooltimeMS = this.DisplayCooltimeMSAsSec,
                    ConvertPerM = this.DisplayPermyriadAsPercent,
                    IgnoreEvalError = this.IgnoreEvalError,
                });
                //GearGraphics.DrawString(g, "[Next Level " + (Skill.Level + 1) + "]", GearGraphics.ItemDetailFont, 9, 485, ref picH, 16);//original values: 10, 274
                GearGraphics.DrawString(g, "[Next Level " + (Skill.Level + 1) + "]", GearGraphics.ItemDetailFont, region.LevelDescLeft, region.TextRight, ref picH, 16);
                if (Skill.SkillID / 10000 / 1000 == 10 && (Skill.Level + 1) == 1 && Skill.ReqLevel > 0)
                {
                    //GearGraphics.DrawPlainText(g, "(Level Required: " + Skill.ReqLevel.ToString() + " or above]", GearGraphics.ItemDetailFont2, GearGraphics.skillYellowColor, 10, 485, ref picH, 16);
                    GearGraphics.DrawPlainText(g, "[Level Required: " + Skill.ReqLevel.ToString() + " or above]", GearGraphics.ItemDetailFont2, GearGraphics.skillYellowColor, region.LevelDescLeft, region.TextRight, ref picH, 16);
                }
                if (hStr != null)
                {
                    GearGraphics.DrawString(g, hStr, GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, region.LevelDescLeft, region.TextRight, ref picH, 16);
                }
            }
            picH += 3;//original value: 9

            if (Skill.AddAttackToolTipDescSkill != 0)
            {
                //g.DrawLine(Pens.White, 6, picH, 493, picH);//original values: 6, 283
                //delay rendering v6 splitter
                splitterH.Add(picH);
                picH += 15;
                //GearGraphics.DrawPlainText(g, "[Combo Skill]", GearGraphics.ItemDetailFont, Color.FromArgb(119, 204, 255), 10, 485, ref picH, 16);
                GearGraphics.DrawPlainText(g, "[Combo Skill]", GearGraphics.ItemDetailFont, Color.FromArgb(119, 204, 255), region.LevelDescLeft, region.TextRight, ref picH, 16);
                picH += 4;
                BitmapOrigin icon = new BitmapOrigin();
                Wz_Node skillNode = PluginBase.PluginManager.FindWz(string.Format(@"Skill\{0}.img\skill\{1}", Skill.AddAttackToolTipDescSkill / 10000, Skill.AddAttackToolTipDescSkill));
                if (skillNode != null)
                {
                    Skill skill = Skill.CreateFromNode(skillNode, PluginBase.PluginManager.FindWz);
                    icon = skill.Icon;
                }
                if (icon.Bitmap != null)
                {
                    g.DrawImage(icon.Bitmap, 13 - icon.Origin.X, picH + 32 - icon.Origin.Y);
                }
                string skillName;
                if (this.StringLinker != null && this.StringLinker.StringSkill.TryGetValue(Skill.AddAttackToolTipDescSkill, out sr))
                {
                    skillName = sr.Name;
                }
                else
                {
                    skillName = Skill.AddAttackToolTipDescSkill.ToString();
                }
                picH += 10;
                //GearGraphics.DrawString(g, skillName, GearGraphics.ItemDetailFont, 46, 485, ref picH, 16);
                GearGraphics.DrawString(g, skillName, GearGraphics.ItemDetailFont, region.LinkedSkillNameLeft, region.TextRight, ref picH, 16);
                picH += 6;
                picH += 13;
            }

            if (Skill.AssistSkillLink != 0)
            {
                //g.DrawLine(Pens.White, 6, picH, 493, picH);//original values: 6, 283
                //delay rendering v6 splitter
                splitterH.Add(picH);
                picH += 15;
                //GearGraphics.DrawPlainText(g, "[Assist Skill]", GearGraphics.ItemDetailFont, ((SolidBrush)GearGraphics.OrangeBrush).Color, 10, 485, ref picH, 16);
                GearGraphics.DrawPlainText(g, "[Assist Skill]", GearGraphics.ItemDetailFont, GearGraphics.SkillSummaryOrangeTextColor, region.LevelDescLeft, region.TextRight, ref picH, 16);
                picH += 4;
                BitmapOrigin icon = new BitmapOrigin();
                Wz_Node skillNode = PluginBase.PluginManager.FindWz(string.Format(@"Skill\{0}.img\skill\{1}", Skill.AssistSkillLink / 10000, Skill.AssistSkillLink));
                if (skillNode != null)
                {
                    Skill skill = Skill.CreateFromNode(skillNode, PluginBase.PluginManager.FindWz);
                    icon = skill.Icon;
                }
                if (icon.Bitmap != null)
                {
                    g.DrawImage(icon.Bitmap, 13 - icon.Origin.X, picH + 32 - icon.Origin.Y);
                }
                string skillName;
                if (this.StringLinker != null && this.StringLinker.StringSkill.TryGetValue(Skill.AssistSkillLink, out sr))
                {
                    skillName = sr.Name;
                }
                else
                {
                    skillName = Skill.AssistSkillLink.ToString();
                }
                picH += 10;
                //GearGraphics.DrawString(g, skillName, GearGraphics.ItemDetailFont, 46, 485, ref picH, 16);
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
                    attr.Add("[Lv. " + Skill.ReqLevel + " required]");
                }
                if (Skill.Invisible)
                {
                    attr.Add("[Hidden Skill]");
                }
                if (Skill.Hyper != HyperSkillType.None)
                {
                    attr.Add("[Hyper Skill: " + Skill.Hyper + "]");
                }
                if (Skill.CombatOrders)
                {
                    attr.Add("[Compatible with Combat Orders]");
                }
                if (Skill.NotRemoved)
                {
                    attr.Add("[Undispellable]");
                }
                if (Skill.MasterLevel > 0 && Skill.MasterLevel < Skill.MaxLevel)
                {
                    attr.Add("[Mastery Book required to upgrade beyond Lv. " + Skill.MasterLevel + "]");
                }
                if (Skill.NotIncBuffDuration)
                {
                    attr.Add("[Unaffected by buff duration increases]");
                }
                if (Skill.NotCooltimeReset)
                {
                    attr.Add("[Unaffected by cooldown reset effects]");
                }
                if (Skill.NotCooltimeReduce)
                {
                    attr.Add("[Unaffected by cooldown reduction effects]");
                }

                if (attr.Count > 0)
                {
                    //skillDescEx.Add("#c" + string.Join(", ", attr.ToArray()) + "#"); //comma after new attribute
                    skillDescEx.Add("#c" + string.Join("\n", attr.ToArray()) + "#");
                }
            }

            if (ShowDelay && Skill.Action.Count > 0)
            {
                foreach (string action in Skill.Action)
                {
                    skillDescEx.Add("#c[Delay] " + action + ": " + CharaSimLoader.GetActionDelay(action) + " ms#");
                }
            }

            if (ShowReqSkill && Skill.ReqSkill.Count > 0)
            {
                foreach (var kv in Skill.ReqSkill)
                {
                    string skillName;
                    if (this.StringLinker != null && this.StringLinker.StringSkill.TryGetValue(kv.Key, out sr))
                    {
                        skillName = sr.Name;
                    }
                    else
                    {
                        skillName = kv.Key.ToString();
                    }
                    skillDescEx.Add("#c[Lv. " + kv.Value + " " + skillName + " required]#");
                }
            }

            if (skillDescEx.Count > 0)
            {
                //g.DrawLine(Pens.White, 6, picH, 493, picH);//original values: 6, 283
                //delay rendering v6 splitter
                splitterH.Add(picH);
                picH += 9;
                foreach (var descEx in skillDescEx)
                {
                    //GearGraphics.DrawString(g, descEx, GearGraphics.ItemDetailFont, 10, 485, ref picH, 16);//original values 8, 266
                    GearGraphics.DrawString(g, descEx, GearGraphics.ItemDetailFont, region.LevelDescLeft, region.TextRight, ref picH, 16);
                }
                picH += 3;//original value: 9
            }

            picH += 6;

            format.Dispose();
            g.Dispose();
            return bitmap;
        }

        private void DrawV6SkillDotline(Graphics g, int x1, int x2, int y)
        {
            // here's a trick that we won't draw left and right part because it looks the same as background border.
            var picCenter = Resource.UIToolTip_img_Skill_Frame_dotline_c;
            using (var brush = new TextureBrush(picCenter))
            {
                brush.TranslateTransform(x1, y);
                g.FillRectangle(brush, new Rectangle(x1, y, x2 - x1, picCenter.Height));
            }
        }

        private Bitmap RenderLinkRidingGear(Gear gear)
        {
            TooltipRender renderer = this.LinkRidingGearRender;
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
                Width = 505,
                TitleCenterX = 253,
                SplitterX1 = 4,
                SplitterX2 = 501,
                SkillDescLeft = 88,
                LinkedSkillNameLeft = 46,
                LevelDescLeft = 11,
                TextRight = 485,
            };
        }
    }
}