using CharaSimResource;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
    public class QuestTooltipRenderer : TooltipRender
    {
        static QuestTooltipRenderer()
        {
            res = new Dictionary<string, TextureBrush>();
            res["top"] = new TextureBrush(Resource.Quest_img_Main_questInfo_backgrnd_custom_top, WrapMode.Clamp);
            res["center"] = new TextureBrush(Resource.Quest_img_Main_questInfo_backgrnd_custom_center, WrapMode.Tile);
            res["bottom"] = new TextureBrush(Resource.Quest_img_Main_questInfo_backgrnd_custom_bottom, WrapMode.Clamp);
        }

        private static Dictionary<string, TextureBrush> res;
        private static readonly Regex RegexTrimNewLinesAtEnd = new Regex(@"(\r\n)?$", RegexOptions.Compiled);

        public QuestTooltipRenderer()
        {
            this.sourceWzFile = null;
            this.RewardRectnItems = new List<Tuple<Rectangle, object>>();
            this.ImageTable = new Dictionary<string, Bitmap>();
            this.DefaultState = 0;
            this.Margin_top = 0;
            this.Margin_right = 0;
        }

        public int DefaultState { get; set; }
        public int Margin_top { get; set; }
        public int Margin_right { get; set; }
        public bool CompareMode { get; set; } = false;
        public Quest Quest { get; set; }
        public Wz_File sourceWzFile { get; set; }
        public List<Tuple<Rectangle, object>> RewardRectnItems { get; set; }
        public Dictionary<string, Bitmap> ImageTable { get; set; }
        private AvatarCanvasManager avatar { get; set; }

        public override object TargetItem
        {
            get
            {
                return this.Quest;
            }
            set
            {
                this.Quest = value as Quest;
                this.Quest.State = this.DefaultState;
            }
        }

        public override Bitmap Render()
        {
            if (this.Quest == null)
            {
                return null;
            }

            var bmp = RenderBase();

            return bmp;
        }

        private Bitmap RenderBase()
        {
            var width = 322;
            var picH = 135;
            var left = 29;
            var state = this.Quest.State;
            this.Margin_top = 0;
            this.Margin_right = 0;
            this.RewardRectnItems.Clear();

            var questColorTable = new Dictionary<string, Color>()
            {
                { "$w", Color.White },
                { "c", ((SolidBrush)GearGraphics.QuestBrushDefault).Color },
                { "$d", ((SolidBrush)GearGraphics.QuestBrushDefault).Color },
                { "$p", ((SolidBrush)GearGraphics.QuestBrushNpc).Color },
                { "$o", ((SolidBrush)GearGraphics.QuestBrushMob).Color },
                { "$m", ((SolidBrush)GearGraphics.QuestBrushMap).Color },
                { "$t", ((SolidBrush)GearGraphics.QuestBrushItem).Color },
                { "$e", ((SolidBrush)GearGraphics.QuestBrushEnd).Color },
            };
            var questFontTable = new Dictionary<string, Font>()
            {
                { "^b", GearGraphics.EquipMDMoris9FontBold },
            };

            // 전경
            using Bitmap fg = new Bitmap(width, DefaultPicHeight);
            using Graphics g = Graphics.FromImage(fg);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            // 카테고리
            var category = GetCategoryResource();
            if (category != null)
            {
                g.DrawImage(category, 21, 42);
            }

            // 이름
            if (!string.IsNullOrEmpty(this.Quest.Name))
            {
                TextRenderer.DrawText(g, Compact(g, this.Quest.Name, 200), GearGraphics.EquipMDMoris9Font, new Point(left, 72), Color.White, TextFormatFlags.NoPadding);
            }
            // 권장 레벨
            if (this.Quest.Lvmin > 0)
            {
                if (this.Quest.RecommendExcept && !this.Quest.LvLimit)
                {
                    TextRenderer.DrawText(g, $"권장 레벨 : {this.Quest.Lvmin} 이상", GearGraphics.EquipMDMoris9Font, new Point(left, 90), ((SolidBrush)GearGraphics.QuestBrushDefault).Color, TextFormatFlags.NoPadding);
                }
                else
                {
                    var lvMax = this.Quest.Lvmax > 0 ? this.Quest.Lvmax : this.Quest.Lvmin + 4;
                    TextRenderer.DrawText(g, $"권장 레벨 : {this.Quest.Lvmin} ~ {lvMax}", GearGraphics.EquipMDMoris9Font, new Point(left, 90), ((SolidBrush)GearGraphics.QuestBrushDefault).Color, TextFormatFlags.NoPadding);
                }
            }
            // npc 이미지 계산
            BitmapOrigin npcImage = new BitmapOrigin();
            if (this.Quest.Check0Npc != null)
            {
                if (this.Quest.Check0Npc.IsComponentNPC)
                {
                    if (this.avatar == null)
                    {
                        this.avatar = new AvatarCanvasManager();
                    }

                    foreach (var node in this.Quest.Check0Npc.Component.Nodes)
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
                        if (this.Quest.Check0Npc.Default.Bitmap != null)
                        {
                            this.Quest.Check0Npc.Default.Bitmap.Dispose();
                        }
                        this.Quest.Check0Npc.Default = img;
                    }

                    this.avatar.ClearCanvas();
                }

                npcImage = this.Quest.Check0Npc.Default;
                if (npcImage.Bitmap != null)
                {
                    Margin_top = Math.Max(npcImage.Origin.Y - 108, Margin_top);
                    Margin_right = Math.Max(npcImage.Bitmap.Width + (263 - npcImage.Origin.X) - width, Margin_right);
                    g.DrawImage(Resource.Quest_img_Main_questInfo_title_layer_npcShadow, 242, 103);
                }
            }

            // 내용
            if (state == 1)
            {
                var target0 = new List<string>();
                var target1 = new List<string>();
                var target2 = new List<string>();
                var targetr = new List<string>();
                var targetNpcPrefix = "";

                if (!string.IsNullOrEmpty(this.Quest.Summary)) target0.Add(RegexTrimNewLinesAtEnd.Replace(this.Quest.Summary, ""));

                if (!string.IsNullOrEmpty(this.Quest.DemandBase))
                {
                    target1.Add(RegexTrimNewLinesAtEnd.Replace(this.Quest.DemandBase, ""));
                    targetNpcPrefix = "> ";
                }
                else if (!string.IsNullOrEmpty(this.Quest.DemandSummary))
                {
                    target1.Add(RegexTrimNewLinesAtEnd.Replace(this.Quest.DemandSummary, ""));
                    targetNpcPrefix = "> ";
                }
                if (this.Quest.Check1NpcID > 0) target1.Add($@"{targetNpcPrefix}#$p{GetNpcName(this.Quest.Check1NpcID)}#에게 가자");

                if (!string.IsNullOrEmpty(this.Quest.PlaceSummary)) target2.Add($@"수행 장소 :\n{this.Quest.PlaceSummary}");

                targetr.Add(string.Join(@"\n", target0.Where(t => !string.IsNullOrEmpty(t))));
                targetr.Add(string.Join(@"\n", target1.Where(t => !string.IsNullOrEmpty(t))));
                targetr.Add(string.Join(@"\n", target2.Where(t => !string.IsNullOrEmpty(t))));
                targetr = targetr.Where(t => !string.IsNullOrEmpty(t)).ToList();

                if (targetr.Count > 0)
                {
                    g.DrawImage(Resource.Quest_img_Main_questInfo_summary_canvas_target, 16, picH);
                    picH += 35;

                    var targetText = string.Join(@"\n\n", targetr);
                    var replaced = ReplaceQuestString(targetText);
                    GearGraphics.DrawString(g, replaced, GearGraphics.EquipMDMoris9Font, questColorTable, questFontTable, this.ImageTable, 29, 293, ref picH, 18, alignment: Text.TextAlignment.Left, defaultColor: ((SolidBrush)GearGraphics.QuestBrushDefault).Color);
                    ClearImageTable();

                    picH += 11;
                }
                g.DrawImage(Resource.Quest_img_Main_questInfo_summary_canvas_detail, 16, picH);
                picH += 35;
            }
            else
            {
                g.DrawImage(Resource.Quest_img_Main_questInfo_summary_canvas_detail, 16, picH);
                picH += 35;
            }
            var desc = this.Quest.Desc[state];
            if (!string.IsNullOrEmpty(desc))
            {
                var replaced = ReplaceQuestString(desc);
                GearGraphics.DrawString(g, replaced, GearGraphics.EquipMDMoris9Font, questColorTable, questFontTable, this.ImageTable, 29, 293, ref picH, 18, alignment: Text.TextAlignment.Left, defaultColor: ((SolidBrush)GearGraphics.QuestBrushDefault).Color);
                ClearImageTable();

                picH += 11;
            }

            // 보상
            if (state != 2)
            {
                if (this.Quest.Reward.HasValues)
                {
                    var rewardPos = picH;
                    if (this.Quest.Reward.Count <= 6)
                    {
                        g.DrawImage(Resource.Quest_img_Main_questInfo_summary_box_layer_boxS, 16, rewardPos);
                        g.DrawImage(Resource.Quest_img_Main_questInfo_summary_reward_layer_reward, 30, rewardPos + 19);
                        picH += 46;
                    }
                    else
                    {
                        g.DrawImage(Resource.Quest_img_Main_questInfo_summary_box_layer_boxL, 16, rewardPos);
                        g.DrawImage(Resource.Quest_img_Main_questInfo_summary_reward_layer_reward, 30, rewardPos + 19);
                        picH += 126;
                    }
                    DrawRewardItems(g, this.Quest.Reward, rewardPos + 42);
                }
                else if (!string.IsNullOrEmpty(this.Quest.RewardSummary))
                {
                    /*
                    var replaced = ReplaceQuestString(this.Quest.RewardSummary);
                    var rewardIcon = Resource.UIWindow2_img_Quest_quest_info_summary_icon_reward;
                    replaced = $"\n#@{this.ImageTable.Count}/{rewardIcon?.Width ?? 0}/{rewardIcon?.Height ?? 0}@\n" + replaced;
                    this.ImageTable.Add(this.ImageTable.Count.ToString(), rewardIcon);
                    GearGraphics.DrawString(g, replaced, GearGraphics.EquipMDMoris9Font, questColorTable, questFontTable, this.ImageTable, 29, 293, ref picH, 18, alignment: Text.TextAlignment.Left, defaultColor: ((SolidBrush)GearGraphics.QuestBrushDefault).Color);
                    ClearImageTable();
                    */
                }
            }
            var bottomPoint = picH;
            picH += 49;

            // 배경
            Bitmap bg = new Bitmap(width + Margin_right, Math.Max(picH + Margin_top, (108 + Margin_top) - npcImage.Origin.Y + (npcImage.Bitmap?.Height ?? 0)));
            using Graphics g2 = Graphics.FromImage(bg);
            g2.DrawImage(res["top"].Image, 0, Margin_top);
            FillRect(g2, res["center"], 0, 166 + Margin_top, bottomPoint + Margin_top);
            g2.DrawImage(res["bottom"].Image, 0, bottomPoint + Margin_top);


            // 중첩
            g2.DrawImage(fg, 0, 0 + Margin_top);
            // npc 이미지
            if (npcImage.Bitmap != null)
            {
                g2.DrawImage(npcImage.Bitmap, 263 - npcImage.Origin.X, (108 + Margin_top) - npcImage.Origin.Y);
            }

            // ID 표시
            if (this.ShowObjectID)
            {
                GearGraphics.DrawGearDetailNumber(g2, 3, 3 + Margin_top, $"{this.Quest.ID.ToString()}-{this.Quest.State}", true);
            }
            // 상태
            var stateText = new string[] { "시작 가능", "진행 중", "완료" };
            TextRenderer.DrawText(g2, $"상태: {stateText[state]}" + (this.Quest.Blocked ? " / 퀘스트 시작 불가" : ""), GearGraphics.EquipMDMoris9Font, new Point(21, bg.Height - 26), ((SolidBrush)GearGraphics.QuestBrushEnd).Color, TextFormatFlags.NoPadding);

            return bg;
        }

        private void FillRect(Graphics g, TextureBrush brush, int x, int y0, int y1)
        {
            brush.ResetTransform();
            brush.TranslateTransform(x, y0);
            g.FillRectangle(brush, x, y0, brush.Image.Width, y1 - y0);
        }

        private Bitmap GetCategoryResource()
        {
            var p1 = 0;
            var p2 = 0;
            if (this.Quest.Category.Count == 2)
            {
                p1 = this.Quest.Category[0];
                p2 = this.Quest.Category[1];
            }
            var path = $"QuestCategory_img_{p1}_{p2}_questUI_tag";

            if (this.Quest.MedalCategory > 0)
            {
                path = $"UIWindow8_img_Title_TitleCategory_{this.Quest.MedalCategory}";
            }

            try
            {
                return (Bitmap)Resource.ResourceManager.GetObject(path);
            }
            catch
            {
                return Resource.QuestCategory_img_0_0_questUI_tag;
            }
        }

        private void DrawRewardItems(Graphics g, QuestReward r, int h)
        {
            var hcount = 0;
            var vcount = 0;
            var dx = 35;
            var dy = 50;
            if (r.Exp > 0)
            {
                var bmp = Resource.Quest_img_Main_questInfo_summary_reward_icon_exp;
                var x = 65 + dx * hcount++;
                var y = h;
                g.DrawImage(bmp, x, y - 23);
                if (!CompareMode)
                {
                    var rectW = Math.Max(bmp.Width, 32);
                    var rectH = Math.Max(bmp.Height, 32);
                    this.RewardRectnItems.Add(new Tuple<Rectangle, object>(new Rectangle(x, y - rectH, rectW, rectH), new TooltipHelp("", r.ExpString, true)));
                }
            }
            if (r.Meso > 0)
            {
                var bmp = Resource.Quest_img_Main_questInfo_summary_reward_icon_meso;
                var x = 65 + dx * hcount++;
                var y = h;
                g.DrawImage(bmp, x, y - 32);
                if (!CompareMode)
                {
                    var rectW = Math.Max(bmp.Width, 32);
                    var rectH = Math.Max(bmp.Height, 32);
                    this.RewardRectnItems.Add(new Tuple<Rectangle, object>(new Rectangle(x, y - rectH, Math.Max(bmp.Width, 32), rectH), new TooltipHelp("", r.MesoString, true)));
                }
            }
            if (r.Items.Count > 0)
            {
                foreach (var item in r.Items)
                {
                    var node = FindItemNode(item.ID);
                    var bmp = GetIcon(item.ID, itemNode: node, raw: true);
                    if (bmp.Bitmap != null)
                    {
                        var x = 65 + dx * hcount++;
                        var y = h + dy * vcount;
                        g.DrawImage(bmp.Bitmap, x - bmp.Origin.X - 1, y - bmp.Origin.Y);
                        if (!CompareMode)
                        {
                            var rectW = Math.Min(Math.Max(bmp.Bitmap.Width, 32), dx -1);
                            var rectH = Math.Max(bmp.Bitmap.Height, 32);
                            this.RewardRectnItems.Add(new Tuple<Rectangle, object>(new Rectangle(x, y - rectH, rectW, rectH), GetItemBase(item.ID, node)));
                        }
                        bmp.Bitmap.Dispose();
                        if (item.ID >= 2000000)
                        {
                            GearGraphics.DrawItemCountNumber(g, x, y - 12, item.Count.ToString());
                        }
                    }
                    if (hcount >= 6)
                    {
                        hcount = 0;
                        vcount++;
                    }
                    if (vcount >= 2) break;
                }
            }
        }

        private string ReplaceQuestString(string text)
        {
            text = Regex.Replace(text, @$"#(p|o|m|t|a{this.Quest.ID}|i|v|y)\s?(\d+?)[:;]?#", match =>
            {
                string tag = match.Groups[1].Value;
                int id = int.Parse(match.Groups[2].Value);
                StringResult sr;
                switch (tag)
                {
                    case "p":
                        StringLinker.StringNpc.TryGetValue(id, out sr);
                        return $"#$p{sr?.Name ?? id.ToString()}#";

                    case "o":
                        StringLinker.StringMob.TryGetValue(id, out sr);
                        return $"#$o{sr?.Name ?? id.ToString()}#";

                    case "m":
                        StringLinker.StringMap.TryGetValue(id, out sr);
                        return $"#$m{sr?.MapName ?? id.ToString()}#";

                    case "t":
                        StringLinker.StringItem.TryGetValue(id, out sr);
                        if (sr == null)
                        {
                            StringLinker.StringEqp.TryGetValue(id, out sr);
                        }
                        return $"#$t{sr?.Name ?? id.ToString()}#";

                    case "i":
                    case "v":
                        StringLinker.StringItem.TryGetValue(id, out sr);
                        if (sr == null)
                        {
                            StringLinker.StringEqp.TryGetValue(id, out sr);
                        }
                        var bmp = GetIconBitmap(id);
                        var ret = $"#@{this.ImageTable.Count}/{Math.Max(32, bmp?.Width ?? 0)}/{Math.Max(32, bmp?.Height ?? 0)}@";
                        this.ImageTable.Add(this.ImageTable.Count.ToString(), bmp);
                        return ret;

                    case "y":
                        StringLinker.StringQuest.TryGetValue(id, out sr);
                        return $"{sr?.Name ?? id.ToString()}";

                    default:
                        if (tag.StartsWith("a"))
                        {
                            if (this.Quest.Check1Items.TryGetValue($"mob{id - 1}", out var value))
                            {
                                return $"#$^b#$w0# / {value.Count.ToString()}#$$";
                            }
                            return $"#$^b#$w0# / 0#$$";
                        }
                        return id.ToString();
                }
            });
            text = Regex.Replace(text, @"#(questorder|j|c|R|x|MD|M|u|fs|fn|f|a|W)(.+?)#", match =>
            {
                string tag = match.Groups[1].Value;
                string info = match.Groups[2].Value;
                switch (tag)
                {
                    case "questorder":
                        if (int.TryParse(info, out int cnt) && cnt > 1)
                        {
                            return "> ";
                        }
                        return "";

                    case "j":
                        return info;

                    case "x":
                        return "x%";

                    case "c":
                    case "R":
                        return "0";
                        //return "미완";

                    case "u":
                        return "미완";

                    case "M":
                        return "몬스터";

                    case "MD":
                        Wz_Node stringNode = PluginManager.FindWz($@"String\mirrorDungeon.img\{info}\name", this.SourceWzFile);
                        var retMD = stringNode.GetValueEx<string>(null);
                        return retMD ?? "거울세계";

                    case "W":
                        var path = $"UIWindow2_img_Quest_quest_info_summary_icon_{info}";
                        var bmpW = (Bitmap)Resource.ResourceManager.GetObject(path);
                        var retW = $"#@{this.ImageTable.Count}/{bmpW?.Width ?? 0}/{bmpW?.Height ?? 0}@";
                        this.ImageTable.Add(this.ImageTable.Count.ToString(), bmpW);
                        return retW;

                    case "f":
                        var bmp = GetIconByPath(info);
                        var ret = $"#@{this.ImageTable.Count}/{bmp?.Width ?? 0}/{bmp?.Height ?? 0}@";
                        this.ImageTable.Add(this.ImageTable.Count.ToString(), bmp);
                        return ret;

                    case "fs":
                    case "fn":
                        return "";

                    case "a":
                        return $"#$^b#$w0# / 0#$$";
                }
                return info;
            });

            // 미사용 태그
            text = text.Replace("#b", ""); // 파란색
            text = text.Replace("#k", ""); // 기본색
            text = text.Replace("#r", ""); // 빨간색
            text = text.Replace("#eqp#", "");
            text = text.Replace("#e", "");
            text = text.Replace("#n", " ");

            return text;
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

        private Bitmap GetIconByPath(string path)
        {
            Wz_Node icon = PluginManager.FindWz(path, this.SourceWzFile);
            if (icon != null)
            {
                return BitmapOrigin.CreateFromNode(icon, PluginManager.FindWz, this.SourceWzFile).Bitmap;
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

        private string GetGearOrItemName(int id)
        {
            StringResult sr;
            if (this.StringLinker == null || !(this.StringLinker.StringItem.TryGetValue(id, out sr) || this.StringLinker.StringEqp.TryGetValue(id, out sr)))
            {
                return null;
            }
            return sr.Name;
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

        private static string Compact(Graphics g, string text, int width) // https://www.codeproject.com/Articles/37503/Auto-Ellipsis
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

        private void ClearImageTable()
        {
            foreach (var kv in this.ImageTable)
            {
                var bmp = kv.Value;
                if (bmp != null)
                {
                    bmp.Dispose();
                }
            }
            this.ImageTable.Clear();
        }
    }
}