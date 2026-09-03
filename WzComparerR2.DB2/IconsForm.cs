using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;

namespace WzComparerR2.DB2
{
    public partial class IconsForm : DevComponents.DotNetBar.Office2007Form
    {
        public IconsForm()
        {
            InitializeComponent();
            Instance = this;
            Db2Theme.Apply(this);
        }
        public static IconsForm Instance;

        /// <summary>依主程式目前的樣式重新套用配色。</summary>
        public void ApplyTheme()
        {
            Db2Theme.Apply(this);
            foreach (var grid in ImageGrids)
            {
                Db2Theme.Apply(grid);
            }
        }
        Wz_Node GetNode(string Path)
        {
            return Db2Host.GetNode(Path);
        }
        List<(Bitmap, string)> ImageList;
        private FormWindowState lastWindowState;
        bool[] HasLoaded = new bool[34];
        DataGridView[] ImageGrids = new DataGridView[34];
        DataGridView ShowImageGrid;

        void LoadItem(string ItemDir)
        {
            foreach (var img in GetNode("Item/" + ItemDir).Nodes)
            {
                if (!Char.IsNumber(img.Text[0]))
                    continue;
                if (ItemDir == "Pet")
                {
                    var ID = img.ImgID();
                    if (GetNode("Item/Pet/" + img.Text + "/info/iconD") != null)
                        ImageList.Add((GetNode("Item/Pet/" + img.Text + "/info/iconD").ExtractPng(), ID));
                }
                else
                {
                    foreach (var Iter in GetNode("Item/" + ItemDir + "/" + img.Text).Nodes)
                    {
                        var ID = Iter.Text;
                        if (Iter.GetNode("info/icon") != null)
                            ImageList.Add((Iter.GetNode("info/icon").ExtractPng(), ID));
                    }
                }
            }
        }
        string LeftStr(string s, int count)
        {
            if (count > s.Length)
                count = s.Length;
            return s.Substring(0, count);
        }
        void LoadCharacter(string Dir)
        {
            foreach (var img in GetNode("Character/" + Dir).Nodes)
            {
                if (LeftStr(img.Text, 1) != "0")
                    continue;
                var ID = img.ImgID();
                switch (Dir)
                {
                    case "Hair":
                        if (img.GetNode("default/hairOverHead") != null)
                            ImageList.Add((img.GetNode("default/hairOverHead").ExtractPng(), ID));
                        break;
                    case "Face":
                        if (img.GetNode("default/face") != null)
                            ImageList.Add((img.GetNode("default/face").ExtractPng(), ID));
                        break;
                    default:
                        if (img.GetNode("info/icon") != null)
                            ImageList.Add((img.GetNode("info/icon").ExtractPng(), ID));
                        break;
                }
            }
        }
        void LoadMap()
        {
            var Links = new List<(string, string)>();
            foreach (var Dir in GetNode("Map/Map").Nodes)
            {
                if (LeftStr(Dir.Text, 3) != "Map")
                    continue;
                foreach (var img in Dir.Nodes)
                {
                    if (!Char.IsNumber(img.Text[0]))
                        continue;
                    var ID = img.ImgID();
                    if (img.GetNode("miniMap/canvas") != null)
                        ImageList.Add((img.GetNode("miniMap/canvas").ExtractPng(), ID));
                    var Link = img.GetNode("info/link");
                    if (Link != null)
                        Links.Add(("Map" + LeftStr(Link.Value.ToString(), 1) + "/" + Link.Value.ToString() + ".img", ID));
                }
            }
            for (int i = 0; i < Links.Count; i++)
            {
                var Child = GetNode("Map/Map/" + Links[i].Item1 + "/miniMap/canvas");
                if (Child != null)
                    ImageList.Add((Child.ExtractPng(), Links[i].Item2));

            }
            ImageList.Sort((x, y) => x.Item2.CompareTo(y.Item2));
        }
        string RightStr(string s, int count)
        {
            if (count > s.Length)
                count = s.Length;
            return s.Substring(s.Length - count, count);
        }
        void LoadMob()
        {
            var Links = new List<(string, string)>();
            Wz_Node Child = null;
            foreach (var Iter in GetNode("String/Mob.img").Nodes)
            {
                var ID = Iter.Text.PadLeft(7, '0');
                if (GetNode("Mob/" + ID + ".img") == null)
                    continue;

                if (GetNode("Mob/" + ID + ".img/info/link") != null)
                {
                    Links.Add((GetNode("Mob/" + ID + ".img/info/link").Value.ToString(), ID));
                    continue;
                }

                if (GetNode("Mob/" + ID + ".img/stand/0") != null)
                    Child = GetNode("Mob/" + ID + ".img/stand/0");
                else if (GetNode("Mob/" + ID + ".img/fly/0") != null)
                    Child = GetNode("Mob/" + ID + ".img/fly/0");
                if (Child != null)
                    ImageList.Add((Child.ExtractPng(), ID));
            }

            for (int i = 0; i < Links.Count; i++)
            {
                if (GetNode("Mob/" + Links[i].Item1 + ".img/stand/0") != null)
                    Child = GetNode("Mob/" + Links[i].Item1 + ".img/stand/0");
                else if (GetNode("Mob/" + Links[i].Item1 + ".img/fly/0") != null)
                    Child = GetNode("Mob/" + Links[i].Item1 + ".img/fly/0");
                ImageList.Add((Child.ExtractPng(), Links[i].Item2));
            }
            ImageList.Sort((x, y) => x.Item2.CompareTo(y.Item2));
        }
        string GetIDPath(string ID)
        {
            var Left1 = LeftStr(ID, 1);
            switch (Left1)
            {
                case "0":
                    return "Skill/000.img/skill/" + ID;
                case "8":
                    return "Skill/" + (int.Parse(ID) / 100).ToString() + ".img/skill/" + ID;
                default:
                    return "Skill/" + (int.Parse(ID) / 10000).ToString() + ".img/skill/" + ID;
            }
        }
        void LoadSkill()
        {
            Wz_Node Child;
            if (Db2Host.HasSkill001)
            {
                foreach (var Iter in GetNode("String/Skill.img").Nodes)
                {
                    var ID = Iter.Text;
                    //  if(GetNode(GetIDPath(ID)) == null)
                    //   continue;
                    if (GetNode("Skill/" + ID + ".img/info/icon") != null)
                        ImageList.Add((GetNode("Skill/" + ID + ".img/info/icon").ExtractPng(), ID));
                    Child = GetNode(GetIDPath(ID) + "/icon");
                    if (Child != null)
                        ImageList.Add((Child.ExtractPng(), ID));
                }
            }
            else
            {
                foreach (var img in GetNode("Skill").Nodes)
                {
                    if (Char.IsNumber(img.Text, 0))
                    {
                        var BookID = img.ImgID();
                        if (GetNode("Skill/" + img.Text + "/info/icon") != null)
                            ImageList.Add((GetNode("Skill/" + img.Text + "/info/icon").ExtractPng(), BookID));
                        foreach (var Iter in GetNode("Skill/" + img.Text).Nodes)
                        {
                            foreach (var Iter2 in Iter.Nodes)
                            {
                                if (Iter.Text == "skill")
                                {
                                    var SkillID = Iter2.Text;
                                    if (Iter2.GetNode("icon") != null)
                                        ImageList.Add((Iter2.GetNode("icon").ExtractPng(), SkillID));
                                }
                            }
                        }
                    }

                }

            }
            ImageList.Sort((x, y) => x.Item2.CompareTo(y.Item2));
        }
        void LoadNpc()
        {
            var Links = new List<(string, string)>();
            Bitmap Icon = null;
            foreach (var Img in GetNode("Npc").Nodes)
            {
                if (!Char.IsNumber(Img.Text[0]))
                    continue;
                var ID = Img.ImgID();
                var Link = GetNode("Npc/" + Img.Text + "/info/link");
                if (Link != null)
                {
                    Links.Add((Link.Value.ToString() + ".img", ID));
                    continue;
                }
                var Entry = GetNode("Npc/" + Img.Text);
                if (Entry.GetNode("stand/0") != null)
                    Icon = Entry.GetNode("stand/0").ExtractPng();
                if (Entry.GetNode("default/0") != null)
                    Icon = Entry.GetNode("default/0").ExtractPng();
                ImageList.Add((Icon, ID));
            }
            Wz_Node Child = null;

            for (int i = 0; i < Links.Count; i++)
            {
                if (GetNode("Npc/" + Links[i].Item1 + "/stand/0") != null)
                    Child = GetNode("Npc/" + Links[i].Item1 + "/stand/0");
                else if (GetNode("Npc/" + Links[i].Item1 + "/default/0") != null)
                    Child = GetNode("Npc/" + Links[i].Item1 + "/default/0");
                ImageList.Add((Child.ExtractPng(), Links[i].Item2));
            }
            ImageList.Sort((x, y) => x.Item2.CompareTo(y.Item2));
        }

        void LoadMorph()
        {
            foreach (var Img in GetNode("Morph").Nodes)
            {
                if (!Char.IsNumber(Img.Text[0]))
                    continue;
                var ID = Img.ImgID();
                Bitmap MorphPic = null;
                if (GetNode("Morph/" + ID + ".img/walk/0") != null)
                    MorphPic = GetNode("Morph/" + ID + ".img/walk/0").ExtractPng();
                if (MorphPic != null)
                    ImageList.Add((MorphPic, ID));
            }

        }

        void LoadFamiliar()
        {
            if (GetNode("Character/Familiar") == null)
            {
                MessageBox.Show("패밀리어를 찾을 수 없습니다");
                return;
            }
            Wz_Node CardEntry;
            Bitmap Icon = null;
            string CardID = "";
            foreach (var img in GetNode("Character/Familiar").Nodes)
            {
                if (!Char.IsNumber(img.Text[0]))
                    continue;
                var ID = img.ImgID();
                var Entry = GetNode("Character/Familiar/" + img.Text);

                if (GetNode("Etc/FamiliarInfo.img") != null)
                {
                    if (GetNode("Etc/FamiliarInfo.img/" + ID) != null)
                        CardID = GetNode("Etc/FamiliarInfo.img/" + ID).GetValue2("consume", "");
                }
                else
                {
                    if (Entry.GetNode("info/monsterCardID") != null)
                        CardID = Entry.GetNode("info/monsterCardID").Value.ToString();
                    else
                        CardID = "";
                }

                if (GetNode("Item/Consume/0287.img/0" + CardID) != null)
                {
                    CardEntry = GetNode("Item/Consume/0287.img/0" + CardID);
                    if (CardEntry.GetNode("info/icon") != null)
                        Icon = CardEntry.GetNode("info/icon").ExtractPng();
                }
                else if (GetNode("Item/Consume/0238.img/0" + CardID) != null)
                {
                    CardEntry = GetNode("Item/Consume/0238.img/0" + CardID);
                    if (CardEntry.GetNode("info/iconRaw") != null)
                        Icon = CardEntry.GetNode("info/iconRaw").ExtractPng();
                }
                else
                    Icon = null;

                // var CardName = GetNode("String/Consume.img/" + CardID).GetValue2("name", "");
                if (string.IsNullOrEmpty(CardID) || Icon == null) continue;
                ImageList.Add((Icon, "0" + CardID));
            }
        }

        void LoadDamageSkin()
        {
            Bitmap Icon = null;
            foreach (var Iter in GetNode("String/Consume.img").Nodes)
            {
                var Name = Iter.GetValue2("name", "");
                if ((Name.Contains("字型")) || (Name.Contains("ジスキン")) || (Name.Contains("스킨")) || (Name.Contains
                  ("Damage Skin")) || (Name.Contains("字型")) || (Name.Contains("伤害皮肤")))
                {
                    var ID = "0" + Iter.Text;
                    string[] imgs = new string[] { "0243.img", "0263.img" };

                    for (int i = 0; i <= 1; i++)
                    {
                        var Entry = GetNode("Item/Consume/" + imgs[i] + "/0" + Iter.Text + "/info/icon");
                        if (Entry != null)
                            Icon = Entry.ExtractPng();
                    }
                    ImageList.Add((Icon, ID));
                }
            }
        }

        void LoadReactor()
        {
            var Links = new List<(string, string)>();
            Bitmap Icon = null;
            foreach (var img in GetNode("Reactor").Nodes)
            {
                if (!Char.IsNumber(img.Text[0]))
                    continue;
                var ID = img.ImgID();
                var Link = GetNode("Reactor/" + ID + "/info/link");
                if (Link != null)
                {
                    Links.Add((Link.Value.ToString() + ".img", ID));
                    continue;
                }
                var Entry = GetNode("Reactor/" + img.Text + "/0/0");
                if (Entry != null)
                    Icon = Entry.ExtractPng();
                ImageList.Add((Icon, ID));
            }

            Wz_Node Child = null;
            for (int i = 0; i < Links.Count; i++)
            {
                if (GetNode("Reactor/" + Links[i].Item1 + ".img/0/0") != null)
                    Child = GetNode("Reactor/" + Links[i].Item1 + ".img/0/0");
                ImageList.Add((Child.ExtractPng(), Links[i].Item2));
            }

            ImageList.Sort((x, y) => x.Item2.CompareTo(y.Item2));
        }


        void LoadImages(DataGridView dataViewImages, int GridSize, bool Resize = false)
        {
            // dataViewImages.Rows.Clear();
            // dataViewImages.Columns.Clear();
            // dataViewImages.Refresh();
            // 記住格子邊長，視窗縮放時 ReflowGrid 需要它重新排版。
            dataViewImages.Tag = GridSize;
            int numColumnsForWidth = Math.Max(1, (dataViewImages.ClientSize.Width - ScrollBarAllowance) / (GridSize + 20));
            int numRows = 0;
            int numImages = ImageList.Count;
            numRows = numImages / numColumnsForWidth;
            // Do we have a an overfill for a row
            if (numImages % numColumnsForWidth > 0)
            {
                numRows += 1;
            }
            // Catch when we have less images than the maximum number of columns for the DataGridView width
            if (numImages < numColumnsForWidth)
            {
                numColumnsForWidth = numImages;
            }
            int numGeneratedCells = numRows * numColumnsForWidth;
            // Dynamically create the columns
            for (int index = 0; index < numColumnsForWidth; index++)
            {
                DataGridViewImageColumn dataGridViewColumn = new DataGridViewImageColumn();
                dataViewImages.Columns.Add(dataGridViewColumn);
                dataViewImages.Columns[index].Width = GridSize + 20;
            }
            // Create the rows
            for (int index = 0; index < numRows; index++)
            {
                dataViewImages.Rows.Add();
                dataViewImages.Rows[index].Height = GridSize + 20;
            }

            int columnIndex = 0;
            int rowIndex = 0;
            Image image;
            for (int index = 0; index < ImageList.Count; index++)
            {
                image = ImageList[index].Item1;
                if (Resize)
                {
                    if (image.Width > 90 || image.Height > 90)
                        image = ResizeImage2(image, 70, 70);
                }
                dataViewImages.Rows[rowIndex].Cells[columnIndex].Value = image;
                dataViewImages.Rows[rowIndex].Cells[columnIndex].ToolTipText = ImageList[index].Item2;

                // Have we reached the end column? if so then start on the next row
                if (columnIndex == numColumnsForWidth - 1)
                {
                    rowIndex++;
                    columnIndex = 0;
                }
                else
                {
                    columnIndex++;
                }
            }
        }
        void ShowMap(Wz_Node MapImg)
        {
            MapRenderLauncher.ShowMap(MapImg);
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            this.FormClosing += (s, e1) =>
            {
                this.Hide();
                e1.Cancel = true;
            };
            ImageList = new List<(Bitmap, string)>();
            for (int i = 0; i < 34; i++)
            {
                ImageGrids[i] = new DataGridView();
                ImageGrids[i].ColumnHeadersVisible = false;
                ImageGrids[i].RowHeadersVisible = false;
                ImageGrids[i].ScrollBars = ScrollBars.Vertical;
                SizeGrid(ImageGrids[i]);
                Db2Theme.Apply(ImageGrids[i]);

                ImageGrids[i].CellClick += (s, e2) =>
                {
                    var Path = ShowImageGrid.Rows[e2.RowIndex].Cells[e2.ColumnIndex].ToolTipText;
                    if (listBox1.SelectedIndex == 16)
                    {
                        var imgNode = GetNode("Map/Map/Map" + LeftStr(Path, 1)).FindNodeByPath(Path + ".img");
                        ShowMap(imgNode);
                        if (imgNode != null)
                            Db2Host.SelectNode(imgNode);
                    }
                    else
                    {
                        Db2Host.Tooltip.Visible = true;
                        Db2Host.Tooltip.BringToFront();
                        Path = GetIDPath2(Path);
                        var Node = PluginManager.FindWz(Path);
                        if (Node != null)
                            Db2Host.SelectNode(Node);
                        else
                            Db2Host.Tooltip.Visible = false;
                    }
                };
                ImageGrids[i].Scroll += (s, e1) =>
                {
                    Db2Host.Tooltip.Visible = false;
                };
            }
            ShowImageGrid = ImageGrids[0];
            Db2Theme.Apply(this.listBox1);

            // 清單靠左撐滿高度，表格填滿剩下的空間。
            listBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
            this.lastWindowState = this.WindowState;
            this.Resize += (s, e1) =>
            {
                LayoutChildren();
                // 最大化／還原不會觸發 ResizeEnd，這裡補一次重排。
                if (this.WindowState != this.lastWindowState)
                {
                    this.lastWindowState = this.WindowState;
                    ReflowGrid(ShowImageGrid);
                }
            };
            // 拖曳邊框的過程只調整大小，放開滑鼠才重排，避免中途卡頓。
            this.ResizeEnd += (s, e1) => ReflowGrid(ShowImageGrid);
            LayoutChildren();

            if (!System.Windows.Forms.SystemInformation.TerminalServerSession)
            {
                var dgvType = ShowImageGrid.GetType();
                var pi = dgvType.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                pi.SetValue(ShowImageGrid, true, null);
            }



        }
        void Click2(object sender, EventArgs e)
        {


        }
        void CellClick(DataGridView DataGrid, DataGridViewCellEventArgs e)
        {


        }



        Image ResizeImage(int index, int Width, int Height)
        {
            using (Image image = ImageList[index].Item1)
            {
                Image NewImage = image.GetThumbnailImage(Width, Height, null, IntPtr.Zero);

                return NewImage;
            }
        }

        public static Bitmap ResizeImage2(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);
            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return destImage;
        }


        string GetIDPath2(string ID)
        {
            switch (listBox1.SelectedIndex)
            {
                case 0:
                    return "Item/Cash/" + LeftStr(ID, 4) + ".img/" + ID;
                case 1:
                    return "Item/Consume/" + LeftStr(ID, 4) + ".img/" + ID;
                case 2:
                    return "Character/Weapon/" + ID + ".img";
                case 3:
                    return "Character/Cap/" + ID + ".img";
                case 4:
                    return "Character/Coat/" + ID + ".img";
                case 5:
                    return "Character/Longcoat/" + ID + ".img";
                case 6:
                    return "Character/Pants/" + ID + ".img";
                case 7:
                    return "Character/Shoes/" + ID + ".img";
                case 8:
                    return "Character/Glove/" + ID + ".img";
                case 9:
                    return "Character/Ring/" + ID + ".img";
                case 10:
                    return "Character/Cape/" + ID + ".img";
                case 11:
                    return "Character/Accessory/" + ID + ".img";
                case 12:
                    return "Character/Shield/" + ID + ".img";
                case 13:
                    return "Character/TamingMob/" + ID + ".img";
                case 14:
                    return "Character/Hair/" + ID + ".img";
                case 15:
                    return "Character/Face/" + ID + ".img";
                case 17:
                    return "Mob/" + ID + ".img";
                case 18:
                    string Left1 = LeftStr(ID, 1);
                    if (Left1 != "")
                    {
                        switch (Left1)
                        {
                            case "0":
                                return "Skill/000.img/skill/" + ID;
                            case "8":
                                return "Skill/" + (int.Parse(ID) / 100).ToString() + ".img/skill/" + ID;
                            default:
                                return "Skill/" + (int.Parse(ID) / 10000).ToString() + ".img/skill/" + ID;
                        }
                    }
                    break;
                case 19:
                    return "Npc/" + ID + ".img";

                case 20:
                    return "Item/Pet/" + ID + ".img";
                case 21:
                    if (GetNode("Item/Install/03010.img") != null)
                    {
                        switch (LeftStr(ID, 5))
                        {
                            case "03015":
                                return "Item/Install/" + LeftStr(ID, 6) + ".img/" + ID;
                            case "03010":
                            case "03011":
                            case "03012":
                            case "03013":
                            case "03014":
                            case "03016":
                            case "03017":
                            case "03018":
                                return "Item/Install/" + LeftStr(ID, 5) + ".img/" + ID;
                            default:
                                return "Item/Install/" + LeftStr(ID, 4) + ".img/" + ID;
                        }
                    }
                    else
                    {
                        return "Item/Install/" + LeftStr(ID, 4) + ".img/" + ID;
                    }

                case 22:
                    return "Character/Android/" + ID + ".img";
                case 23:
                    return "Character/Mechanic/" + ID + ".img";

                case 24:
                    return "Character/PetEquip/" + ID + ".img";

                case 25:
                    return "Character/Bits/" + ID + ".img";

                case 26:
                    return "Character/MonsterBattle/" + ID + ".img";

                case 27:
                    return "Character/Totem/" + ID + ".img";
                case 29:
                case 30:
                    return "Item/Consume/" + LeftStr(ID, 4) + ".img/" + ID;

                case 31:
                    return "Item/Etc/" + LeftStr(ID, 4) + ".img/" + ID;

            }

            return null;
        }

        private void Form2_Click(object sender, EventArgs e)
        {
            ShowImageGrid.Focus();
        }

        /// <summary>捲軸與外框預留的寬度。</summary>
        private const int ScrollBarAllowance = 24;

        private const int GridMargin = 10;

        /// <summary>把清單與圖示表格撐滿目前的視窗。</summary>
        private void LayoutChildren()
        {
            if (this.ClientSize.Width <= 0 || this.ClientSize.Height <= 0)
                return;

            listBox1.Top = GridMargin;
            listBox1.Height = Math.Max(50, this.ClientSize.Height - GridMargin * 2);

            SizeGrid(ShowImageGrid);
        }

        private void SizeGrid(DataGridView grid)
        {
            if (grid == null || this.ClientSize.Width <= 0)
                return;

            int left = listBox1.Right + 8;
            grid.Left = left;
            grid.Top = GridMargin;
            grid.Width = Math.Max(100, this.ClientSize.Width - left - GridMargin);
            grid.Height = Math.Max(100, this.ClientSize.Height - GridMargin * 2);
        }

        /// <summary>
        /// 依表格目前的寬度重新排版圖示。
        /// 圖片本身已經放在儲存格裡，所以直接把既有內容取出來重排，不必重讀 WZ。
        /// </summary>
        private void ReflowGrid(DataGridView grid)
        {
            if (grid == null || !(grid.Tag is int gridSize) || grid.Columns.Count == 0)
                return;

            int cell = gridSize + 20;
            int columns = Math.Max(1, (grid.ClientSize.Width - ScrollBarAllowance) / cell);
            if (columns == grid.Columns.Count)
                return;

            var items = new List<(object Value, string Tip)>();
            foreach (DataGridViewRow row in grid.Rows)
                foreach (DataGridViewCell cellItem in row.Cells)
                    if (cellItem.Value != null)
                        items.Add((cellItem.Value, cellItem.ToolTipText));

            if (items.Count == 0)
                return;

            grid.SuspendLayout();
            grid.Rows.Clear();
            grid.Columns.Clear();

            for (int i = 0; i < columns; i++)
            {
                grid.Columns.Add(new DataGridViewImageColumn());
                grid.Columns[i].Width = cell;
            }

            int rows = (items.Count + columns - 1) / columns;
            for (int i = 0; i < rows; i++)
            {
                grid.Rows.Add();
                grid.Rows[i].Height = cell;
            }

            for (int i = 0; i < items.Count; i++)
            {
                var target = grid.Rows[i / columns].Cells[i % columns];
                target.Value = items[i].Value;
                target.ToolTipText = items[i].Tip;
            }

            grid.ResumeLayout();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PluginManager.FindWz(Wz_Type.Base) == null)
            {
                MessageBox.Show("Base.wz가 열려 있지 않습니다");
                return;
            }
            var SelectIndex = listBox1.SelectedIndex;
            ShowImageGrid.Parent = null;

            // 先把即將載入的表格調整成目前視窗大小，
            // LoadImages 是依表格寬度決定欄數的，順序反了就會排錯。
            SizeGrid(ImageGrids[SelectIndex]);
            var Graphic = this.CreateGraphics();
            var Font = new System.Drawing.Font(FontFamily.GenericSansSerif, 20, FontStyle.Bold);
            Graphic.DrawString("불러오는 중...", Font, Brushes.Black, 100, 100);
            // ShowImageGrid.Rows.Clear();
            // ShowImageGrid.Refresh();

            if (!HasLoaded[SelectIndex])
            {
                ImageList.Clear();
                switch (SelectIndex)
                {

                    case 0:
                        LoadItem("Cash");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 1:
                        LoadItem("Consume");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 2:
                        LoadCharacter("Weapon");
                        LoadImages(ImageGrids[SelectIndex], 25);
                        break;
                    case 3:
                        LoadCharacter("Cap");
                        LoadImages(ImageGrids[SelectIndex], 22);
                        break;
                    case 4:
                        LoadCharacter("Coat");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 5:
                        LoadCharacter("Longcoat");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 6:
                        LoadCharacter("Pants");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 7:
                        LoadCharacter("Shoes");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 8:
                        LoadCharacter("Glove");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 9:
                        LoadCharacter("Ring");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 10:
                        LoadCharacter("Cape");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 11:
                        LoadCharacter("Accessory");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 12:
                        LoadCharacter("Shield");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 13:
                        LoadCharacter("TamingMob");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 14:
                        LoadCharacter("Hair");
                        LoadImages(ImageGrids[SelectIndex], 28);
                        break;
                    case 15:
                        LoadCharacter("Face");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 16:
                        LoadMap();
                        LoadImages(ImageGrids[SelectIndex], 80);
                        break;
                    case 17:
                        LoadMob();
                        LoadImages(ImageGrids[SelectIndex], 60, true);
                        break;
                    case 18:
                        LoadSkill();
                        LoadImages(ImageGrids[SelectIndex], 22);
                        break;

                    case 19:
                        LoadNpc();
                        LoadImages(ImageGrids[SelectIndex], 50, true);
                        break;
                    case 20:
                        LoadItem("Pet");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 21:
                        LoadItem("Install");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;

                    case 22:
                        LoadCharacter("Android");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 23:
                        LoadCharacter("Mechanic");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 24:
                        LoadCharacter("PetEquip");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 25:
                        LoadCharacter("Bits");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;

                    case 26:
                        LoadCharacter("MonsterBattle");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 27:
                        LoadCharacter("Totem");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 28:
                        LoadMorph();
                        LoadImages(ImageGrids[SelectIndex], 50, true);
                        break;
                    case 29:
                        LoadFamiliar();
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 30:
                        LoadDamageSkin();
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 31:
                        LoadItem("Etc");
                        LoadImages(ImageGrids[SelectIndex], 20);
                        break;
                    case 32:
                        LoadReactor();
                        LoadImages(ImageGrids[SelectIndex], 60, true);
                        break;
                }
                HasLoaded[SelectIndex] = true;
                this.Focus();
            }
            //  ImageGrids[SelectIndex].ResumeLayout();
            ShowImageGrid = ImageGrids[SelectIndex];
            ShowImageGrid.Parent = this;
            ShowImageGrid.AllowUserToResizeColumns = false;
            ShowImageGrid.AllowUserToResizeRows = false;
            SizeGrid(ShowImageGrid);
            ReflowGrid(ShowImageGrid);
            ShowImageGrid.Focus();


        }
    }


}
