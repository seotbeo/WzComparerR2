using DevComponents.AdvTree;
using DevComponents.Editors;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WzComparerR2.CharaSim;
using WzComparerR2.CharaSimControl;
using WzComparerR2.Common;
using WzComparerR2.PluginBase;
using WzComparerR2.Rendering;
using WzComparerR2.WzLib;

namespace WzComparerR2
{
    public partial class FrmWorldArchiveBrowser : DevComponents.DotNetBar.Office2007Form
    {
        public FrmWorldArchiveBrowser(MainForm parent)
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
#endif
            cmbRegion.Items.AddRange(new[]
            {
                new ComboItem("메이플 월드"){ Value = 0 },
                new ComboItem("그란디스"){ Value = 1 },
                new ComboItem("아케인 리버"){ Value = 2 },
            });
            cmbType.Items.AddRange(new[]
            {
                new ComboItem("NPC"){ Value = 0 },
                new ComboItem("몬스터"){ Value = 1 },
            });
            this.picWorldArchiveImg.MouseDoubleClick += (s, e) =>
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    this.picWorldArchiveImg_Navigate();
                }
            };
            this.picWorldArchiveImg.MouseClick += (s, e) =>
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    this.picWorldArchiveImg_Save();
                }
            };

            this._mainForm = parent;
            this.FormClosed += FrmWorldArchiveBrowser_FormClosed;
        }

        private Wz_Node EtcWaNode { get; set; }
        private Wz_Node UiWaNode { get; set; }
        private Wz_Node MobNode { get; set; }
        private Wz_Node NpcNode { get; set; }
        private StringLinker stringLinker { get; set; }
        private MainForm _mainForm { get; }
        private bool DarkMode;
        private Bitmap unscaledBmp;
        private Bitmap illustMask;
        private Wz_Node currentExtraArtworkNode;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hwnd, UInt32 wMsg, IntPtr wParam, IntPtr lParam);
        private const int WM_SETREDRAW = 0xB;

        private int regionID
        {
            get
            {
                return ((cmbRegion.SelectedItem as ComboItem)?.Value as int?) ?? 0;
            }
            set
            {
                var items = cmbRegion.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as int? == value)
                    ?? items.Last();
                item.Value = value;
                cmbRegion.SelectedItem = item;
            }
        }

        private int typeID
        {
            get
            {
                return ((cmbType.SelectedItem as ComboItem)?.Value as int?) ?? 0;
            }
            set
            {
                var items = cmbType.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as int? == value)
                    ?? items.Last();
                item.Value = value;
                cmbType.SelectedItem = item;
            }
        }

        public void SetStringLinker(StringLinker sl)
        {
            this.stringLinker = sl;
        }

        public void SetWzNodes(Wz_Node etcWaNode, Wz_Node uiWaNode, Wz_Node mobNode, Wz_Node npcNode)
        {
            this.EtcWaNode = etcWaNode;
            this.UiWaNode = uiWaNode;
            this.MobNode = mobNode;
            this.NpcNode = npcNode;
        }

        public void ResetState()
        {
            regionID = 0;
            typeID = 0;
        }

        private async void btnExport_Click(object sender, EventArgs e)
        {
            // TBA
        }

        private void btnLocateExtraIllust_Click(object sender, EventArgs e)
        {
            _mainForm.RedirectToNode(currentExtraArtworkNode);
        }

        private void cmbRegion_SelectedValueChanged(object sender, EventArgs e)
        {
            this.advTreeMap.Nodes.Clear();
            this.advTreeLife.Nodes.Clear();
            this.richDescription.Clear();
            var mapNodes = EtcWaNode.FindNodeByPath($"collectionInfo\\{this.regionID}", true);
            if (mapNodes != null)
            {
                foreach (var mapNode in mapNodes.Nodes)
                {
                    bool hideRegion = mapNode.FindNodeByPath("hide").GetValueEx<int>(0) == 1;
                    Wz_Node regionNameNode = mapNode.FindNodeByPath("regionName");
                    if (regionNameNode != null)
                    {
                        string regionName = regionNameNode.Value.ToString();
                        if (hideRegion)
                        {
                            regionName += " (숨겨짐)";
                        var node = new Node(regionName);
                        node.Tag = mapNode;
                        this.advTreeMap.Nodes.Add(node);
                    }
                }
                Wz_Node worldDescNode = mapNodes.FindNodeByPath("worldDesc");
                if (worldDescNode != null)
                {
                    UpdateText(worldDescNode.GetValue<string>().Replace("\\r", "\r").Replace("\\n", "\n"));
                }
            }
            DisposeImages();
            var worldIllustNode = UiWaNode.FindNodeByPath($"regionSelect\\main\\world\\{this.regionID}", true);
            if (worldIllustNode != null)
            {
                BitmapOrigin bo = BitmapOrigin.CreateFromNode(worldIllustNode, PluginManager.FindWz);
                if (bo.Bitmap != null)
                {
                    this.picWorldArchiveImg.Image = ApplyMask(bo.Bitmap);
                    bo.Bitmap.Dispose();
                }
            }
        }

        private void cmbType_SelectedValueChanged(object sender, EventArgs e)
        {
            UpdateAdvTreeLife();
        }

        private void advTreeMap_AfterNodeSelect(object sender, EventArgs e)
        {
            UpdateMapImageInfo();
            UpdateAdvTreeLife();
        }

        private void advTreeLife_AfterNodeSelect(object sender, EventArgs e)
        {
            UpdateLifeImageInfo();
        }

        private void advTreeLife_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            picWorldArchiveImg_Navigate();
        }

        private void picWorldArchiveImg_Navigate()
        {
            if (this.advTreeLife.SelectedNode != null)
            {
                int LifeID = this.advTreeLife.SelectedNode.Tag is KeyValuePair<int, Wz_Node> kvp ? kvp.Key : -1;
                Wz_Node lifeNode = null;
                switch (this.typeID)
                {
                    case 0:
                        lifeNode = NpcNode?.FindNodeByPath($"{LifeID:D7}.img");
                        break;
                    case 1:
                        lifeNode = MobNode?.FindNodeByPath($"{LifeID:D7}.img");
                        break;
                }
                if (lifeNode == null && this.advTreeLife.SelectedNode.Tag is KeyValuePair<int, Wz_Node> kvp2)
                {
                    lifeNode = kvp2.Value;
                }
                if (lifeNode != null)
                {
                    _mainForm.RedirectToNode(lifeNode);
                }
            }
        }

        private void picWorldArchiveImg_Save()
        {
            if (this.picWorldArchiveImg.Image != null)
            {
                string fileName = "";
                var TypeID = this.typeID;
                if (this.advTreeLife.SelectedNode == null)
                {
                    fileName += this.advTreeMap.SelectedNode == null ? $"Map_{this.cmbRegion.SelectedItem.ToString()}" : $"Map_{this.advTreeMap.SelectedNode.Text}";
                }
                else
                {
                    switch (TypeID)
                    {
                        case 0: fileName += $"Npc_{advTreeLife.SelectedNode.Text}"; break;
                        case 1: fileName += $"Mob_{advTreeLife.SelectedNode.Text}"; break;
                    }
                }
                using (SaveFileDialog dlg = new SaveFileDialog())
                {
                    dlg.Filter = "PNG (*.png)|*.png|*.*|*.*";
                    dlg.FileName = fileName;

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        if (this.unscaledBmp != null)
                        {
                            this.unscaledBmp.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
                        }
                        else
                        {
                            this.picWorldArchiveImg.Image.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                }
            }
        }

        private void UpdateMapImageInfo()
        {
            this.advTreeLife.Nodes.Clear();
            int mapID = -1;
            if (this.advTreeMap.SelectedNode == null)
            {
                return;
            }
            else
            {
                Int32.TryParse((this.advTreeMap.SelectedNode.Tag as Wz_Node).Text, out mapID);
            }
            DisposeImages();
            Wz_Node illustNode = UiWaNode.FindNodeByPath($"detail\\main\\regionillust\\{this.regionID}\\{mapID}", true);
            if (illustNode != null)
            {
                BitmapOrigin bo = BitmapOrigin.CreateFromNode(illustNode, PluginManager.FindWz);
                if (bo.Bitmap != null)
                {
                    this.picWorldArchiveImg.Image = ApplyMask(bo.Bitmap);
                    bo.Bitmap.Dispose();
                }
            }
            Wz_Node descNode = EtcWaNode.FindNodeByPath($"collectionInfo\\{this.regionID}\\{mapID}\\regionDesc", true);
            if (descNode != null)
            {
                UpdateText(descNode.GetValue<string>().Replace("\\r", "\r").Replace("\\n", "\n"));
            }
            else
            {
                this.richDescription.Clear();
            }
        }

        private void UpdateLifeImageInfo()
        {
            var targetNode = this.advTreeLife.SelectedNode;
            if (targetNode == null)
            {
                return;
            }
            if (targetNode.Parent != null) targetNode = targetNode.Parent;
            KeyValuePair<int, Wz_Node> kvp = targetNode.Tag as KeyValuePair<int, Wz_Node>? ?? default;
            if (kvp.Value == null) return;

            int LifeID = kvp.Key;
            TryLocateExtraIllust(LifeID);

            double scale = 1.00;
            Wz_Node scaleNode = kvp.Value.FindNodeByPath("scale");
            if (scaleNode != null)
            {
                scale = scaleNode.GetValueEx<double>(100) / 100;
            }

            Point offset = default;
            Wz_Node offsetNode = kvp.Value.FindNodeByPath("offset");
            if (offsetNode != null)
            {
                offset = offsetNode.GetValueEx<Wz_Vector>(null);
            }

            WorldArchiveImageType imageType = WorldArchiveImageType.Stand;
            Wz_Node imageTypeNode = kvp.Value.FindNodeByPath("imageType");
            if (imageTypeNode != null)
            {
                imageType = (WorldArchiveImageType)imageTypeNode.GetValueEx<int>(-1);
            }

            Wz_Node descNode = kvp.Value.FindNodeByPath("desc");
            if (descNode != null)
            {
                UpdateText(descNode.GetValue<string>().Replace("\\r", "\r").Replace("\\n", "\n"));
            }
            else
            {
                this.richDescription.Clear();
            }

            Wz_Node lifeNode = null;
            Wz_Node altImageNode = null;
            switch (this.typeID)
            {
                case 0:
                    lifeNode = NpcNode?.FindNodeByPath($"{LifeID:D7}.img", true);
                    altImageNode = UiWaNode.FindNodeByPath($"image\\npc\\{LifeID:D7}", true);
                    break;

                case 1:
                    lifeNode = MobNode?.FindNodeByPath($"{LifeID:D7}.img", true);
                    altImageNode = UiWaNode.FindNodeByPath($"image\\mob\\{LifeID:D7}", true);
                    break;
            }

            BitmapOrigin bo = new BitmapOrigin();
            switch (imageType)
            {
                case WorldArchiveImageType.Stand:
                    bo = BitmapOrigin.CreateFromNode(lifeNode?.FindNodeByPath("stand\\0"), PluginManager.FindWz);
                    break;

                case WorldArchiveImageType.Illust:
                    bo = BitmapOrigin.CreateFromNode(lifeNode?.FindNodeByPath("info\\illustration2\\base"), PluginManager.FindWz);
                    break;

                case WorldArchiveImageType.Fly:
                    bo = BitmapOrigin.CreateFromNode(lifeNode?.FindNodeByPath("fly\\0"), PluginManager.FindWz);
                    break;

                case WorldArchiveImageType.Default:
                    bo = BitmapOrigin.CreateFromNode(lifeNode?.FindNodeByPath("info\\default"), PluginManager.FindWz);
                    break;

                case WorldArchiveImageType.Custom:
                    bo = BitmapOrigin.CreateFromNode(altImageNode, PluginManager.FindWz);
                    break;
            }
            if (bo.Bitmap == null) // case WorldArchiveImageType.Unknown or illust not founded
            {
                if (lifeNode != null)
                {
                    switch (this.typeID)
                    {
                        case 0:
                            Npc npc = Npc.CreateFromNode(lifeNode, PluginManager.FindWz);
                            bo = npc.Default;
                            break;

                        case 1:
                            Mob mob = Mob.CreateFromNode(lifeNode, PluginManager.FindWz);
                            bo = mob.Default;
                            break;
                    }
                }
            }

            DisposeImages();
            this.unscaledBmp = bo.Bitmap;
            Bitmap resized = ResizeImage(this.unscaledBmp, scale, offset);
            this.picWorldArchiveImg.Image = ApplyMask(resized);

            if (resized != null) resized.Dispose();
        }

        private void UpdateAdvTreeLife()
        {
            var TypeID = this.typeID;
            this.advTreeLife.Nodes.Clear();
            if (this.advTreeMap.SelectedNode == null)
            {
                return;
            }
            var lifeNode = this.advTreeMap.SelectedNode.Tag as Wz_Node;
            if (lifeNode != null)
            {
                Wz_Node lifeNodes = null;
                switch (TypeID)
                {
                    case 0:
                        lifeNodes = lifeNode.FindNodeByPath("npc");
                        break;
                    case 1:
                        lifeNodes = lifeNode.FindNodeByPath("mob");
                        break;
                }
                if (lifeNodes != null)
                {
                    foreach (var node in lifeNodes.Nodes)
                    {
                        bool hideLife = node.FindNodeByPath("hide").GetValueEx<int>(0) == 1;
                        Wz_Node idNode = node.FindNodeByPath("id");
                        if (idNode != null)
                        {
                            var newNode = new Node($"{node.Text}");
                            int count = 0;
                            foreach (var id in idNode.Nodes)
                            {
                                var lifeID = id.GetValue<int>();
                                StringResult sr;
                                switch (TypeID)
                                {
                                    case 0:
                                        if (this.stringLinker == null || !this.stringLinker.StringNpc.TryGetValue(lifeID, out sr))
                                        {
                                            sr = new StringResult();
                                            sr.Name = "(null)";
                                        }
                                        break;
                                    case 1:
                                        if (this.stringLinker == null || !this.stringLinker.StringMob.TryGetValue(lifeID, out sr))
                                        {
                                            sr = new StringResult();
                                            sr.Name = "(null)";
                                        }
                                        break;
                                    default:
                                        sr = new StringResult();
                                        sr.Name = "(null)";
                                        break;
                                }
                                string lifeName = $"{sr.Name} ({lifeID})";
                                if (hideLife)
                                {
                                    lifeName += " (숨겨짐)";
                                }
                                var childNode = new Node(lifeName);
                                childNode.Tag = new KeyValuePair<int, Wz_Node>(lifeID, node);
                                if (count++ == 0)
                                {
                                    newNode = childNode;
                                }
                                else
                                {
                                    newNode.Nodes.Add(childNode);
                                }
                            }
                            this.advTreeLife.Nodes.Add(newNode);
                        }
                    }
                }
            }
        }

        private void TryLocateExtraIllust(int npcID)
        {
            Wz_Node npcExtraArtworkNode = UiWaNode.FindNodeByPath($"illust\\npc\\{npcID}", true);
            this.currentExtraArtworkNode = npcExtraArtworkNode;
            this.btnLocateExtraIllust.Enabled = (npcExtraArtworkNode != null);
        }

        private Bitmap ResizeImage(Bitmap bmp, double scale, Point offset)
        {
            if (bmp == null) return null;
            if (scale == 0)
            {
                scale = Math.Min((double)this.picWorldArchiveImg.Width / bmp.Width, (double)this.picWorldArchiveImg.Height / bmp.Height);
            }

            int imgW = (int)(bmp.Width * scale);
            int imgH = (int)(bmp.Height * scale);

            int paddedW = imgW + Math.Abs(offset.X * 2);
            int paddedH = imgH + Math.Abs(offset.Y * 2);

            Bitmap result = new Bitmap(paddedW, paddedH);

            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = scale >= 1.00 ? System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor : System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

                g.DrawImage(bmp, new Rectangle(Math.Max(0, offset.X * 2), Math.Max(0, offset.Y * 2), imgW, imgH));
            }

            return result;
        }

        private Bitmap ApplyMask(Bitmap source)
        {
            if (source == null) return null;

            Bitmap mask = GetMaskBitmap();
            Point maskOffset = new Point((source.Width - mask.Width) / 2, (source.Height - mask.Height) / 2);
            Bitmap masked = BitmapUtils.ApplyAlphaMask_Format32bppArgb(source, mask, maskOffset);
            return masked;
        }

        private Bitmap GetMaskBitmap()
        {
            if (this.illustMask == null)
            {
                this.illustMask = BitmapOrigin.CreateFromNode(UiWaNode?.FindNodeByPath($"detail\\main\\mask_illust", true), PluginManager.FindWz).Bitmap;
            }
            return this.illustMask;
        }

        private void UpdateText(string text)
        {
            SendMessage(this.richDescription.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            try
            {
                this.richDescription.Clear();
                this.richDescription.AppendText(text);
                this.richDescription.Select(0, text.Length);
                this.richDescription.SelectionColor = DarkMode ? Color.LightGray : System.Drawing.SystemColors.ControlText;
                //this.richDescription.SelectionFont = new Font("Noto Sans KR", 14f);
                this.richDescription.SelectionFont = GearGraphics.WorldArchiveFont;
                this.richDescription.Rtf = Regex.Replace(
                    this.richDescription.Rtf,
                    "#s#(.*?)#s#",
                    "{\\strike $1\\strike0}",
                    RegexOptions.Singleline
                    );
                this.richDescription.Rtf = Regex.Replace(
                    this.richDescription.Rtf,
                    "#e(.*?)#n",
                    "{\\b $1\\b0}",
                    RegexOptions.Singleline
                    );
                this.richDescription.Select(0, 0);
            }
            finally
            {
                SendMessage(this.richDescription.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
                this.richDescription.Refresh();
            }
        }

        private void DisposeImages()
        {
            if (this.unscaledBmp != null)
            {
                this.unscaledBmp.Dispose();
                this.unscaledBmp = null;
            }
            if (this.picWorldArchiveImg.Image != null)
            {
                this.picWorldArchiveImg.Image.Dispose();
                this.picWorldArchiveImg.Image = null;
            }
        }

        private void FrmWorldArchiveBrowser_FormClosed(object sender, FormClosedEventArgs e)
        {
            DisposeImages();
            if (this.illustMask != null)
            {
                this.illustMask.Dispose();
            }
        }
    }
}
