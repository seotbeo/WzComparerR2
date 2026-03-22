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
        }

        private Wz_Node EtcWaNode { get; set; }
        private Wz_Node UiWaNode { get; set; }
        private Wz_Node MobNode { get; set; }
        private Wz_Node NpcNode { get; set; }
        private StringLinker stringLinker { get; set; }
        private MainForm _mainForm { get; }
        private bool DarkMode;
        private Bitmap unscaledBmp;
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
                    Wz_Node regionNameNode = mapNode.FindNodeByPath("regionName");
                    if (regionNameNode != null)
                    {
                        var node = new Node(regionNameNode.Value.ToString());
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
            var worldIllustNode = UiWaNode.FindNodeByPath($"regionSelect\\main\\world\\{this.regionID}", true);
            if (worldIllustNode != null)
            {
                BitmapOrigin bo = BitmapOrigin.CreateFromNode(worldIllustNode, PluginManager.FindWz);
                this.picWorldArchiveImg.Image = bo.Bitmap;
                this.unscaledBmp = null;
            }
            else
            {
                this.picWorldArchiveImg.Image = null;
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
                        lifeNode = PluginManager.FindWz(Wz_Type.Npc)?.FindNodeByPath($"{LifeID:D7}.img");
                        break;
                    case 1:
                        lifeNode = PluginManager.FindWz(Wz_Type.Mob)?.FindNodeByPath($"{LifeID:D7}.img");
                        break;
                }
                _mainForm.RedirectToNode(lifeNode ?? (this.advTreeLife.SelectedNode.Tag as Wz_Node));
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
            Wz_Node illustNode = UiWaNode.FindNodeByPath($"detail\\main\\regionillust\\{this.regionID}\\{mapID}", true);
            if (illustNode != null)
            {
                BitmapOrigin bo = BitmapOrigin.CreateFromNode(illustNode, PluginManager.FindWz);
                this.picWorldArchiveImg.Image = bo.Bitmap;
                this.unscaledBmp = null;
            }
            else
            {
                this.picWorldArchiveImg = null;
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
            if (this.advTreeLife.SelectedNode == null)
            {
                return;
            }
            var TypeID = this.typeID;
            double scale = 1.00;
            KeyValuePair<int, Wz_Node> kvp = this.advTreeLife.SelectedNode.Tag as KeyValuePair<int, Wz_Node>? ?? default;
            if (kvp.Value == null) return;
            Wz_Node scaleNode = kvp.Value.FindNodeByPath("scale");
            if (scaleNode != null)
            {
                scale = scaleNode.GetValueEx<double>(100) / 100;
            }
            Wz_Node descNode = kvp.Value.FindNodeByPath("desc");
            int LifeID = kvp.Key;
            TryLocateExtraIllust(LifeID);
            if (descNode != null)
            {
                UpdateText(descNode.GetValue<string>().Replace("\\r", "\r").Replace("\\n", "\n"));
            }
            else
            {
                this.richDescription.Clear();
            }
            Bitmap bmp = null;
            Bitmap altBmp = null;
            Wz_Node lifeNode;
            Wz_Node altImageNode;
            switch (TypeID)
            {
                case 0:
                    lifeNode = PluginManager.FindWz(Wz_Type.Npc)?.FindNodeByPath($"{LifeID:D7}.img", true);
                    if (lifeNode != null)
                    {
                        Npc npc = Npc.CreateFromNode(lifeNode, PluginManager.FindWz);
                        bmp = npc.Default.Bitmap;
                    }
                    altImageNode = UiWaNode.FindNodeByPath($"image\\npc\\{LifeID:D7}", true);
                    if (altImageNode != null)
                    {
                        BitmapOrigin bo = BitmapOrigin.CreateFromNode(altImageNode, PluginManager.FindWz);
                        altBmp = bo.Bitmap;
                    }
                    break;
                case 1:
                    lifeNode = PluginManager.FindWz(Wz_Type.Mob)?.FindNodeByPath($"{LifeID:D7}.img", true);
                    if (lifeNode != null)
                    {
                        Mob mob = Mob.CreateFromNode(lifeNode, PluginManager.FindWz);
                        bmp = mob.Default.Bitmap;
                    }
                    altImageNode = UiWaNode.FindNodeByPath($"image\\mob\\{LifeID:D7}", true);
                    if (altImageNode != null)
                    {
                        BitmapOrigin bo = BitmapOrigin.CreateFromNode(altImageNode, PluginManager.FindWz);
                        altBmp = bo.Bitmap;
                    }
                    break;
            }
            this.unscaledBmp = altBmp ?? bmp;
            this.picWorldArchiveImg.Image = ResizeImage(this.unscaledBmp, scale);
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
                        Wz_Node idNode = node.FindNodeByPath("id");
                        if (idNode != null)
                        {
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
                                var newNode = new Node($"{sr.Name} ({lifeID})");
                                newNode.Tag = new KeyValuePair<int, Wz_Node>(lifeID, node);
                                this.advTreeLife.Nodes.Add(newNode);
                            }
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

        private Bitmap ResizeImage(Bitmap bmp, double scale)
        {
            if (bmp == null) return null;
            if (scale == 0)
            {
                scale = Math.Min((double)this.picWorldArchiveImg.Width / bmp.Width, (double)this.picWorldArchiveImg.Height / bmp.Height);
            }

            int w = (int)(bmp.Width * scale);
            int h = (int)(bmp.Height * scale);

            Bitmap result = new Bitmap(w, h);

            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = scale >= 1.00 ? System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor : System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

                g.DrawImage(bmp, new Rectangle(0, 0, w, h));
            }

            return result;
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
    }
}
