using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;
using WzComparerR2.Controls;
using WzComparerR2.PluginBase;
using System.Linq;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSimControl
{
    public class AfrmTooltip : AlphaForm
    {
        public AfrmTooltip()
        {
            this.menu = new ContextMenuStrip();
            this.InitMenu();
            this.SaveSample = new ToolStripMenuItem("샘플 저장(&R)", null, tsmiSaveSample_Click);
            this.ContextMenuStrip = this.menu;

            this.Size = new Size(1, 1);
            this.HideOnHover = true;
            this.FamiliarRender = new FamiliarTooltipRender();
            this.GearRender = new GearTooltipRender2();
            this.GearRender22 = new GearTooltipRender22();
            this.ItemRender = new ItemTooltipRender2();
            this.ItemRender22 = new ItemTooltipRender22();
            this.SkillRender = new SkillTooltipRender2();
            this.RecipeRender = new RecipeTooltipRender();
            this.MapRender = new MapTooltipRenderer();
            this.MobRender = new MobTooltipRenderer();
            this.NpcRender = new NpcTooltipRenderer();
            this.QuestRender = new QuestTooltipRenderer();
            this.HelpRender = new HelpTooltipRender();
            this.SetItemRender = new SetItemTooltipRender();
            this.SetItemRender22 = new SetItemTooltipRender22();
            this.AchievementRender = new AchievementTooltipRenderer();
            this.SizeChanged += AfrmTooltip_SizeChanged;

            this.MouseClick += AfrmTooltip_MouseClick;
        }

        private object item;

        private ContextMenuStrip menu;
        private bool showMenu;
        private bool showID;
        private bool enable22AniStyle;

        public Object TargetItem
        {
            get { return item; }
            set { item = value; }
        }

        public StringLinker StringLinker { get; set; }
        public Character Character { get; set; }

        
        public FamiliarTooltipRender FamiliarRender { get; private set; }
        public GearTooltipRender2 GearRender { get; private set; }
        public GearTooltipRender22 GearRender22 { get; private set; }
        public ItemTooltipRender2 ItemRender { get; private set; }
        public ItemTooltipRender22 ItemRender22 { get; private set; }
        public SkillTooltipRender2 SkillRender { get; private set; }
        public RecipeTooltipRender RecipeRender { get; private set; }
        public MapTooltipRenderer MapRender { get; private set; }
        public MobTooltipRenderer MobRender { get; private set; }
        public NpcTooltipRenderer NpcRender { get; private set; }
        public QuestTooltipRenderer QuestRender { get; private set; }
        public HelpTooltipRender HelpRender { get; private set; }
        public SetItemTooltipRender SetItemRender { get; private set; }
        public SetItemTooltipRender22 SetItemRender22 { get; private set; }
        public AchievementTooltipRenderer AchievementRender { get; private set; }

        public string ImageFileName { get; set; }
        public string NodeName { get; set; }
        public string Desc { get; set; }
        public string Pdesc { get; set; }
        public string AutoDesc { get; set; }
        public string Hdesc { get; set; }
        public string DescLeftAlign { get; set; }
        public string QuestAvailable { get; set; }
        public string QuestProgress { get; set; }
        public string QuestComplete { get; set; }
        public int NodeID { get; set; }
        public int PreferredStringCopyMethod { get; set; }
        public bool CopyParsedSkillString { get; set; }

        private ToolStripMenuItem SaveSample {  get; set; }

        public event ObjectMouseEventHandler ObjectMouseMove;
        public event EventHandler ObjectMouseLeave;

        public bool ShowID
        {
            get { return this.showID; }
            set
            {
                this.showID = value;
                this.FamiliarRender.ShowObjectID = value;
                this.GearRender.ShowObjectID = value;
                this.GearRender22.ShowObjectID = value;
                this.ItemRender.ShowObjectID = value;
                this.ItemRender22.ShowObjectID = value;
                this.QuestRender.ShowObjectID = value;
                this.SkillRender.ShowObjectID = value;
                this.RecipeRender.ShowObjectID = value;
                this.AchievementRender.ShowObjectID = value;
            }
        }

        public bool ShowMenu
        {
            get { return showMenu; }
            set { showMenu = value; }
        }

        public bool Enable22AniStyle
        {
            get { return enable22AniStyle; }
            set
            {
                this.enable22AniStyle = value;
                this.SkillRender.Enable22AniStyle = value;
                this.RecipeRender.Enable22AniStyle = value;
            }
        }

        public override void Refresh()
        {
            this.InitMenu();
            this.PreRender();
            if (this.Bitmap != null)
            {
                this.SetBitmap(Bitmap);
                this.CaptionRectangle = new Rectangle(0, 0, Bitmap.Width, Bitmap.Height);
                base.Refresh();
            }

            if (this.item != null)
            {
                if (this.item is Gear)
                {
                    if (Enable22AniStyle)
                    {
                        if (this.GearRender22.HasSamples())
                        {
                            this.SaveSample.Tag = new Dictionary<string, object>
                            {
                                ["type"] = "gear",
                                ["renderer"] = this.GearRender22
                            };
                            this.menu.Items.Add(this.SaveSample);
                        }
                    }
                }
                else if (this.item is Item)
                {
                    if (Enable22AniStyle)
                    {
                        if (this.ItemRender22.HasSamples())
                        {
                            this.SaveSample.Tag = new Dictionary<string, object>
                            {
                                ["type"] = "item",
                                ["renderer"] = this.ItemRender22
                            };
                            this.menu.Items.Add(this.SaveSample);
                        }
                    }
                    else
                    {
                        if (this.ItemRender.HasSamples())
                        {
                            this.SaveSample.Tag = new Dictionary<string, object>
                            {
                                ["type"] = "item",
                                ["renderer"] = this.ItemRender
                            };
                            this.menu.Items.Add(this.SaveSample);
                        }
                    }
                }
            }
        }

        private void InitMenu()
        {
            this.menu.Items.Clear();
            this.menu.Items.Add(new ToolStripMenuItem("복사(&C)", null, tsmiCopy_Click));
            this.menu.Items.Add(new ToolStripMenuItem("저장(&S)", null, tsmiSave_Click));
            this.menu.Items.Add(new ToolStripMenuItem("텍스트 복사(&T)", null, tsmiCopyText_Click));
        }

        public void PreRender()
        {
            if (this.item == null)
                return;

            if (Bitmap != null)
                Bitmap.Dispose();

            TooltipRender renderer;
            if (item is Item)
            {
                if (Enable22AniStyle)
                {
                    renderer = ItemRender22;
                    ItemRender22.Item = this.item as Item;
                }
                else
                {
                    renderer = ItemRender;
                    ItemRender.Item = this.item as Item;
                }
            }
            else if (item is Familiar)
            {
                renderer = FamiliarRender;
                FamiliarRender.Familiar = this.item as Familiar;
                // FamiliarRender.UseAssembleUI = EnableAssembleTooltip;
            }
            else if (item is Gear)
            {
                if (Enable22AniStyle)
                {
                    renderer = GearRender22;
                    GearRender22.Gear = this.TargetItem as Gear;
                }
                else
                {
                    renderer = GearRender;
                    GearRender.Gear = this.TargetItem as Gear;
                }

                if (false)
                {
                    Gear g = GearRender.Gear;
                    if (this.StringLinker.StringEqp.ContainsKey(g.ItemID))
                    {
                        this.StringLinker.StringEqp[g.ItemID].Name = "暴君之高卡文黑锅";
                        this.StringLinker.StringEqp[g.ItemID].Desc = @"""#c这个锅 我背了！#"" ————gaokawen";
                    }
                    g.Star = 25;
                    g.Grade = GearGrade.SS;
                    g.AdditionGrade = GearGrade.B;
                    g.Props[GearPropType.reqLevel] = 250;
                    g.Props[GearPropType.reqSTR] = 6;
                    g.Props[GearPropType.reqDEX] = 6;
                    g.Props[GearPropType.reqINT] = 6;
                    g.Props[GearPropType.reqLUK] = 6;
                    g.Props[GearPropType.reqPOP] = 666;
                    g.Props[GearPropType.level] = 1;
                    g.Props[GearPropType.reqJob] = 0;
                    g.Props[GearPropType.incPAD] = 6;
                    g.Props[GearPropType.incMAD] = 6;
                    g.Props[GearPropType.incPDD] = 666;
                    g.Props[GearPropType.incMDD] = 666;
                    g.Props[GearPropType.tuc] = 66;
                    g.Props[GearPropType.superiorEqp] = 1;
                    g.Props[GearPropType.tradeAvailable] = 2;
                    //g.Props[GearPropType.charismaEXP] = 88;
                    //g.Props[GearPropType.willEXP] = 88;
                    //g.Props[GearPropType.charmEXP] = 88;
                    g.Props[GearPropType.nActivatedSocket] = 1;
                    //g.Props[GearPropType.setItemID] = 135;
                    //g.Options[0] = Potential.LoadFromWz(60001, 3);
                    //g.Options[1] = Potential.LoadFromWz(60001, 3);
                    //g.Options[2] = Potential.LoadFromWz(60001, 3);
                    //g.AdditionalOptions[0] = Potential.LoadFromWz(32086, 10);
                    //g.AdditionalOptions[1] = Potential.LoadFromWz(32086, 10);
                    //g.AdditionalOptions[2] = Potential.LoadFromWz(32086, 10);
                }
            }
            else if (item is Skill)
            {
                renderer = SkillRender;
                SkillRender.Skill = this.item as Skill;
            }
            else if (item is Recipe)
            {
                renderer = RecipeRender;
                RecipeRender.Recipe = this.item as Recipe;
            }
            else if (item is Map)
            {
                renderer = MapRender;
                MapRender.Map = this.item as Map;
            }
            else if (item is Mob)
            {
                renderer = MobRender;
                MobRender.MobInfo = this.item as Mob;
            }
            else if (item is Npc)
            {
                renderer = NpcRender;
                NpcRender.NpcInfo = this.item as Npc;
            }
            else if (item is Quest)
            {
                renderer = QuestRender;
                QuestRender.Quest = this.item as Quest;
            }
            else if (item is TooltipHelp)
            {
                renderer = HelpRender;
                HelpRender.Pair = this.item as TooltipHelp;
            }
            else if (item is SetItem)
            {
                if (Enable22AniStyle)
                {
                    renderer = SetItemRender22;
                    SetItemRender22.SetItem = this.item as SetItem;
                }
                else
                {
                    renderer = SetItemRender;
                    SetItemRender.SetItem = this.item as SetItem;
                }
            }
            else if (item is Achievement)
            {
                renderer = AchievementRender;
                AchievementRender.Achievement = this.item as Achievement;
            }
            else
            {
                this.Bitmap = null;
                renderer = null;
                return;
            }
            renderer.StringLinker = StringLinker;
            this.Bitmap = renderer.Render();
        }

        void AfrmTooltip_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && this.showMenu)
            {
                this.menu.Show(this, e.Location);
            }
        }

        public object GetPairByPoint(Point point)
        {
            Point p = point;
            switch (this.item)
            {
                case Quest:
                    if ((this.QuestRender?.RewardRectnItems?.Count ?? 0) > 0)
                    {
                        foreach (var ri in this.QuestRender.RewardRectnItems)
                        {
                            if (ri.Item1.Contains(p))
                            {
                                return ri.Item2;
                            }
                        }
                    }
                    break;
                case Achievement:
                    if ((this.AchievementRender?.RewardRectnItems?.Count ?? 0) > 0)
                    {
                        foreach (var ri in this.AchievementRender.RewardRectnItems)
                        {
                            if (ri.Item1.Contains(p))
                            {
                                return ri.Item2;
                            }
                        }
                    }
                    break;
            }
            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            object obj = GetPairByPoint(e.Location);
            if (obj != null)
                this.OnObjectMouseMove(new ObjectMouseEventArgs(e, obj));
            else
                this.OnObjectMouseLeave(EventArgs.Empty);
        }

        protected virtual void OnObjectMouseMove(ObjectMouseEventArgs e)
        {
            if (this.ObjectMouseMove != null)
                this.ObjectMouseMove(this, e);
        }

        protected virtual void OnObjectMouseLeave(EventArgs e)
        {
            if (this.ObjectMouseLeave != null)
                this.ObjectMouseLeave(this, e);
        }

        void tsmiCopy_Click(object sender, EventArgs e)
        {
            if (this.Bitmap != null)
            {
                using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
                {
                    var dataObj = new DataObject();
                    dataObj.SetData(DataFormats.Bitmap, this.Bitmap);
                    Byte[] dibData = ConvertToDib(this.Bitmap);
                    stream.Write(dibData, 0, dibData.Length);
                    dataObj.SetData(DataFormats.Dib, stream);
                    Clipboard.SetDataObject(dataObj, true);
                }
            }
        }

        void tsmiCopyText_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            if (String.IsNullOrEmpty(this.Desc)) this.Desc = "";
            if (String.IsNullOrEmpty(this.Pdesc)) this.Pdesc = "";
            if (String.IsNullOrEmpty(this.AutoDesc)) this.AutoDesc = "";
            if (String.IsNullOrEmpty(this.Hdesc)) this.Hdesc = "";
            if (String.IsNullOrEmpty(this.DescLeftAlign)) this.DescLeftAlign = "";
            if (this.CopyParsedSkillString && item is Skill) this.Hdesc = this.SkillRender.ParsedHdesc;

            if (this.item is Quest)
            {
                Desc = ReplaceQuestString(Desc, this.item as Quest);
                Pdesc = ReplaceQuestString(Pdesc, this.item as Quest);
                AutoDesc = ReplaceQuestString(AutoDesc, this.item as Quest);
                Hdesc = ReplaceQuestString(Hdesc, this.item as Quest);
                DescLeftAlign = ReplaceQuestString(DescLeftAlign, this.item as Quest);
                QuestAvailable = ReplaceQuestString(QuestAvailable, this.item as Quest);
                QuestProgress = ReplaceQuestString(QuestProgress, this.item as Quest);
                QuestComplete = ReplaceQuestString(QuestComplete, this.item as Quest);
            }
            else
            {
                if (this.PreferredStringCopyMethod == 2) sb.AppendLine(this.NodeID.ToString());
                if (!String.IsNullOrEmpty(this.NodeName)) sb.AppendLine(this.NodeName);
            }
            switch (this.PreferredStringCopyMethod)
            {
                default:
                case 0:
                    if (!String.IsNullOrEmpty(this.Desc)) sb.AppendLine(this.Desc);
                    if (!String.IsNullOrEmpty(this.Pdesc)) sb.AppendLine(this.Pdesc);
                    if (!String.IsNullOrEmpty(this.AutoDesc)) sb.AppendLine(this.AutoDesc);
                    if (!String.IsNullOrEmpty(this.Hdesc)) sb.AppendLine(this.Hdesc);
                    if (!String.IsNullOrEmpty(this.DescLeftAlign)) sb.AppendLine(this.DescLeftAlign);
                    break;
                case 1:
                    if ((this.Desc + this.Pdesc + this.AutoDesc).Contains("\\n"))
                    {
                        foreach (string i in (this.Desc + this.Pdesc + this.AutoDesc).Split(new string[] { "\\n" }, StringSplitOptions.None))
                        {
                            sb.AppendLine(i.Replace("\\r", "").Replace("#c", "").Replace("#", ""));
                        }
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(this.Desc)) sb.AppendLine(this.Desc);
                        if (!String.IsNullOrEmpty(this.Pdesc)) sb.AppendLine(this.Pdesc);
                        if (!String.IsNullOrEmpty(this.AutoDesc)) sb.AppendLine(this.AutoDesc);
                    }
                    if (this.Hdesc.Contains("\\n"))
                    {
                        foreach (string i in this.Hdesc.Split(new string[] { "\\n" }, StringSplitOptions.None))
                        {
                            if (this.CopyParsedSkillString)
                            {
                                sb.AppendLine(i.Replace("\\r", "").Replace("#c", "").Replace("#", ""));
                            }
                            else
                            {
                                sb.AppendLine(i.Replace("\\r", ""));
                            }
                        }
                    }
                    else
                    {
                        if (this.CopyParsedSkillString)
                        {
                            sb.AppendLine(this.Hdesc.Replace("#c", "").Replace("#", ""));
                        }
                        else
                        {
                            sb.AppendLine(this.Hdesc);
                        }
                    }
                    break;
                case 2:
                    if (this.item is Quest)
                    {
                        int check0npcID = (this.item as Quest).Check0Npc != null ? (this.item as Quest).Check0Npc.ID : 0;
                        int check1npcID = (this.item as Quest).Check1NpcID;
                        string check0npcName = "(null)";
                        string check1npcName = "(null)";
                        if (StringLinker.StringNpc.TryGetValue(check0npcID, out StringResult sr))
                        {
                            check0npcName = sr?.Name;
                        }
                        if (StringLinker.StringNpc.TryGetValue(check1npcID, out sr))
                        {
                            check1npcName = sr?.Name;
                        }
                        sb.AppendLine($"{{{{DISPLAYTITLE|{this.NodeName}}}}}");
                        sb.AppendLine($"{{{{Quest<!--{this.NodeID}-->");
                        sb.AppendLine($"|name={this.NodeName}");
                        sb.AppendLine($"|npc={check0npcName}");
                        sb.AppendLine((check0npcName == "(null)") ? "|npcimg=" : $"|npcimg=[[File:NPC {check0npcName}.png]]");
                        sb.AppendLine($"|repeat=");
                        sb.AppendLine($"|req=");
                        sb.AppendLine($"|cat=");
                        sb.AppendLine($"|type=");
                        sb.AppendLine(string.IsNullOrEmpty(this.QuestAvailable) ? $"|avail={this.Desc}" : $"|avail={this.QuestAvailable}");
                        sb.AppendLine($"|prog={this.QuestProgress}");
                        sb.AppendLine($"|comp={this.QuestComplete}");
                        sb.AppendLine($"|pro={this.Hdesc}");
                        sb.AppendLine($"|reward={this.Pdesc}");
                        sb.AppendLine($"|select=");
                        sb.AppendLine($"|prob=");
                        sb.AppendLine($"|nextquest={this.AutoDesc}");
                        sb.AppendLine($"}}}}");
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(this.Desc)) sb.AppendLine(this.Desc.Replace("\\r", "").Replace("\\n", "<br />").Replace("#c", "<span class=\"darkorange-text\">").Replace("#", "</span>"));
                        if (!String.IsNullOrEmpty(this.Pdesc)) sb.AppendLine(this.Pdesc.Replace("\\r", "").Replace("\\n", "<br />").Replace("#c", "<span class=\"darkorange-text\">").Replace("#", "</span>"));
                        if (!String.IsNullOrEmpty(this.AutoDesc)) sb.AppendLine(this.AutoDesc.Replace("\\r", "").Replace("\\n", "<br />").Replace("#c", "<span class=\"darkorange-text\">").Replace("#", "</span>"));
                        if (this.CopyParsedSkillString)
                        {
                            if (!String.IsNullOrEmpty(this.Hdesc)) sb.AppendLine(this.Hdesc.Replace("\\r", "").Replace("\\n", "<br />").Replace("#c", "<span class=\"darkorange-text\">").Replace("#", "</span>"));
                        }
                        else
                        {
                            if (!String.IsNullOrEmpty(this.Hdesc)) sb.AppendLine(this.Hdesc.Replace("\\r", "").Replace("\\n", "<br />"));
                        }
                        if (!String.IsNullOrEmpty(this.DescLeftAlign)) sb.AppendLine(this.DescLeftAlign.Replace("\\r", "").Replace("\\n", "<br />").Replace("#c", "<span class=\"darkorange-text\">").Replace("#", "</span>"));
                    }
                    break;
            }
            Clipboard.SetText(sb.ToString());
            sb.Clear();
        }

        private Byte[] ConvertToDib(Image image) // https://stackoverflow.com/a/46424800
        {
            Byte[] bm32bData;
            Int32 width = image.Width;
            Int32 height = image.Height;
            // Ensure image is 32bppARGB by painting it on a new 32bppARGB image.
            using (Bitmap bm32b = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics gr = Graphics.FromImage(bm32b))
                    gr.DrawImage(image, new Rectangle(0, 0, bm32b.Width, bm32b.Height));
                // Bitmap format has its lines reversed.
                bm32b.RotateFlip(RotateFlipType.Rotate180FlipX);
                Int32 stride;
                bm32bData = GetImageData(bm32b, out stride);
            }
            // BITMAPINFOHEADER struct for DIB.
            Int32 hdrSize = 0x28;
            Byte[] fullImage = new Byte[hdrSize + 12 + bm32bData.Length];
            //Int32 biSize;
            WriteIntToByteArray(fullImage, 0x00, 4, true, (UInt32)hdrSize);
            //Int32 biWidth;
            WriteIntToByteArray(fullImage, 0x04, 4, true, (UInt32)width);
            //Int32 biHeight;
            WriteIntToByteArray(fullImage, 0x08, 4, true, (UInt32)height);
            //Int16 biPlanes;
            WriteIntToByteArray(fullImage, 0x0C, 2, true, 1);
            //Int16 biBitCount;
            WriteIntToByteArray(fullImage, 0x0E, 2, true, 32);
            //BITMAPCOMPRESSION biCompression = BITMAPCOMPRESSION.BITFIELDS;
            WriteIntToByteArray(fullImage, 0x10, 4, true, 3);
            //Int32 biSizeImage;
            WriteIntToByteArray(fullImage, 0x14, 4, true, (UInt32)bm32bData.Length);
            // These are all 0. Since .net clears new arrays, don't bother writing them.
            //Int32 biXPelsPerMeter = 0;
            //Int32 biYPelsPerMeter = 0;
            //Int32 biClrUsed = 0;
            //Int32 biClrImportant = 0;

            // The aforementioned "BITFIELDS": colour masks applied to the Int32 pixel value to get the R, G and B values.
            WriteIntToByteArray(fullImage, hdrSize + 0, 4, true, 0x00FF0000);
            WriteIntToByteArray(fullImage, hdrSize + 4, 4, true, 0x0000FF00);
            WriteIntToByteArray(fullImage, hdrSize + 8, 4, true, 0x000000FF);
            Array.Copy(bm32bData, 0, fullImage, hdrSize + 12, bm32bData.Length);
            return fullImage;
        }

        private Byte[] GetImageData(Bitmap sourceImage, out Int32 stride) // https://stackoverflow.com/a/43706643
        {
            System.Drawing.Imaging.BitmapData sourceData = sourceImage.LockBits(new Rectangle(0, 0, sourceImage.Width, sourceImage.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, sourceImage.PixelFormat);
            stride = sourceData.Stride;
            Byte[] data = new Byte[stride * sourceImage.Height];
            System.Runtime.InteropServices.Marshal.Copy(sourceData.Scan0, data, 0, data.Length);
            sourceImage.UnlockBits(sourceData);
            return data;
        }

        private void WriteIntToByteArray(Byte[] data, Int32 startIndex, Int32 bytes, Boolean littleEndian, UInt32 value) // https://stackoverflow.com/a/46424800
        {
            Int32 lastByte = bytes - 1;
            if (data.Length < startIndex + bytes)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to write a " + bytes + "-byte value at offset " + startIndex + ".");
            for (Int32 index = 0; index < bytes; index++)
            {
                Int32 offs = startIndex + (littleEndian ? index : lastByte - index);
                data[offs] = (Byte)(value >> (8 * index) & 0xFF);
            }
        }

        void tsmiSave_Click(object sender, EventArgs e)
        {
            if (this.Bitmap != null && this.item != null)
            {
                using (SaveFileDialog dlg = new SaveFileDialog())
                {
                    dlg.Filter = "PNG (*.png)|*.png|*.*|*.*";
                    dlg.FileName = this.ImageFileName;

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        this.Bitmap.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
            }
        }

        void tsmiSaveSample_Click(object sender, EventArgs e)
        {
            if (this.Bitmap != null && this.item != null)
            {
                Bitmap bitmap;
                var tag = (Dictionary<string, object>)(sender as ToolStripMenuItem).Tag;
                switch (tag["type"])
                {
                    case "gear":
                        if (this.Enable22AniStyle)
                        {
                            bitmap = (tag["renderer"] as GearTooltipRender22).GetSampleBitmap();
                        }
                        else return;
                        break;

                    case "item":
                        if (this.Enable22AniStyle)
                        {
                            bitmap = (tag["renderer"] as ItemTooltipRender22).GetSampleBitmap();
                        }
                        else
                        {
                            bitmap = (tag["renderer"] as ItemTooltipRender2).GetSampleBitmap();
                        }
                        break;

                    default:
                        return;
                }

                if (bitmap == null) return;

                using (SaveFileDialog dlg = new SaveFileDialog())
                {
                    dlg.Filter = "PNG (*.png)|*.png|*.*|*.*";
                    dlg.FileName = this.ImageFileName.Replace(@".png", @"_Sample.png");

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        bitmap.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                bitmap.Dispose();
            }
        }

        void AfrmTooltip_SizeChanged(object sender, EventArgs e)
        {
            if (this.Bitmap != null)
                this.SetClientSizeCore(this.Bitmap.Width, this.Bitmap.Height);
        }

        private string ReplaceQuestString(string text, Quest quest)
        {
            bool isMapleWiki = (this.PreferredStringCopyMethod == 2);
            text = Regex.Replace(text, @$"#(p|o|m|t|q|a{this.NodeID}|i|v|y|illu)\s*(\d{{1,9}}).*?#", match => // id should be less than 1,000,000,000
            {
                string tag = match.Groups[1].Value;
                if (!int.TryParse(match.Groups[2].Value, out int id)) id = -1;
                StringResult sr;
                switch (tag)
                {
                    case "p":
                        StringLinker.StringNpc.TryGetValue(id, out sr);
                        return isMapleWiki ? $"{{{{QuestTextHighlight|npc|{sr?.Name ?? id.ToString()}}}}}" : $"{sr?.Name ?? id.ToString()}";

                    case "o":
                        if (id >= 100000000)
                        {
                            StringLinker.StringMap.TryGetValue(id, out sr);
                            return isMapleWiki ? $"{{{{QuestTextHighlight|map|{sr?.MapName ?? id.ToString()}}}}}" : $"{sr?.MapName ?? id.ToString()}";
                        }
                        else
                        {
                            StringLinker.StringMob.TryGetValue(id, out sr);
                            return isMapleWiki ? $"{{{{QuestTextHighlight|mob|{sr?.Name ?? id.ToString()}}}}}" : $"{sr?.Name ?? id.ToString()}";
                        }

                    case "m":
                        StringLinker.StringMap.TryGetValue(id, out sr);
                        return isMapleWiki ? $"{{{{QuestTextHighlight|map|{sr?.MapName ?? id.ToString()}}}}}" : $"{sr?.MapName ?? id.ToString()}";

                    case "t":
                        StringLinker.StringItem.TryGetValue(id, out sr);
                        if (sr == null)
                        {
                            StringLinker.StringEqp.TryGetValue(id, out sr);
                        }
                        return isMapleWiki ? $"{{{{QuestTextHighlight|item|{sr?.Name ?? id.ToString()}}}}}" : $"{sr?.Name ?? id.ToString()}";

                    case "q":
                        StringLinker.StringSkill.TryGetValue(id, out sr);
                        return $"{sr?.Name ?? id.ToString()}";

                    case "i":
                    case "v":
                        string itemIconPrefix = "[[File:Item ";
                        StringLinker.StringItem.TryGetValue(id, out sr);
                        if (sr == null)
                        {
                            StringLinker.StringEqp.TryGetValue(id, out sr);
                            itemIconPrefix = "[[File:Eqp ";
                        }
                        return isMapleWiki ? itemIconPrefix + $"{sr?.Name ?? id.ToString()}.png]] " : $"{sr?.Name ?? id.ToString()}";

                    case "y":
                        StringLinker.StringQuest.TryGetValue(id, out sr);
                        return isMapleWiki ? $"[[{sr?.Name ?? id.ToString()}]]" : $"{sr?.Name ?? id.ToString()}";

                    default:
                        if (tag.StartsWith("a"))
                        {
                            if (quest.Check1Items.TryGetValue($"mob{id - 1}", out var value))
                            {
                                return $"0 / {value.Count.ToString()}";
                            }
                            return $"0 / 0";
                        }
                        return id.ToString();
                }
            });
            text = Regex.Replace(text, @"#(questorder|j|c|R|x|MD|M|u|fs|fn|fc|f|a|W|o9101069f|DL|h0)(.+?)#", match =>
            {
                string tag = match.Groups[1].Value;
                string info = match.Groups[2].Value;
                StringResult sr;
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
                        return "未完";

                    case "h0":
                        return "プレイヤー";

                    case "o9101069f":
                        Wz_Node stringNodeMF = PluginManager.FindWz($@"String\MobFilter.img\{info}");
                        var retMF = stringNodeMF.GetValueEx<string>(null);
                        if (retMF != null) return $"#$o{retMF}#";

                        StringLinker.StringMob.TryGetValue(9101069, out sr);
                        return $"#$o{sr?.Name ?? "9101069"}#";

                    case "M":
                        return "モンスター";

                    case "MD":
                        Wz_Node stringNode = PluginManager.FindWz($@"String\mirrorDungeon.img\{info}\name");
                        var retMD = stringNode.GetValueEx<string>(null);
                        return retMD ?? "鏡の世界";

                    case "fc":
                    case "fs":
                    case "fn":
                        return "";

                    case "a":
                        return $"#$^b#$w0# / 0#$$";

                    case "DL":
                        return ConvertDateWZ2(info);
                }
                return info;
            });

            // 미사용 태그
            text = text.Replace("#b", ""); // 파란색
            text = text.Replace("#k", ""); // 기본색
            text = text.Replace("#kk", "");
            text = text.Replace("#K", "");
            text = text.Replace("#r", ""); // 빨간색
            text = text.Replace("#g", "");
            text = text.Replace("#l", "");
            text = text.Replace("#eqp#", "");
            text = text.Replace("#es", "#ＥＳ"); // plural suffix for English region
            text = text.Replace("#e", "");
            text = text.Replace("ＥＳ", "es");
            text = text.Replace("#E", "");
            text = text.Replace("#n", " ");

            return text;
        }

        private string ConvertDateWZ2(string info)
        {
            var para = info.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (para.Length < 2)
            {
                return info;
            }

            var timeParseFormat = "yyyyMMddHHmm";
            string timeConvertFormat = null;
            Wz_Node datelistNode = PluginManager.FindWz($@"Etc\DateListWZ2.img");
            Wz_Node datetimeNode = datelistNode?.FindNodeByPath($@"DateList\{para[0]}\{para[1]}") ?? null;
            var datetime = datetimeNode.GetValueEx<string>(null);
            if (datetime == null || !DateTime.TryParseExact(datetime, timeParseFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            {
                return info;
            }

            if (para.Length == 3)
            {
                var returnType = para[2];
                Wz_Node returnTypeNode = datelistNode?.FindNodeByPath($@"returnType\{returnType}") ?? null;
                timeConvertFormat = returnTypeNode.GetValueEx<string>(null);
            }
            if (string.IsNullOrEmpty(timeConvertFormat))
            {
                timeConvertFormat = "{YYYY}年 {MM}月 {DD}日 {hh}時 {mm}分";
            }

            var datedict = new Dictionary<string, string>
            {
                { "YYYY", "yyyy" },
                { "YY", "yy" },
                { "MM", "MM" },
                { "M", "M" },
                { "DD", "dd" },
                { "D", "d" },
                { "hh", "HH" },
                { "h", "H" },
                { "mm", "mm" },
                { "m", "m" },
            };

            timeConvertFormat = Regex.Replace(timeConvertFormat, @"\{(.*?)\}|([^{}]+)", match =>
            {
                if (match.Value.StartsWith("{") && match.Value.EndsWith("}"))
                {
                    string format = match.Groups[1].Value;
                    return datedict.ContainsKey(format) ? datedict[format] : match.Value;
                }
                else
                {
                    return $@"'{match.Value}'";
                }
            });

            return time.ToString(timeConvertFormat);
        }
    }
}
