using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Npc : IDisposable
    {
        public Npc(GetSpineDefaultFunc getSpineDefaultFunc)
        {
            this.ID = -1;
            //this.Animates = new LifeAnimateCollection();
            this.Illustration2Bitmaps = new List<Bitmap>();
            this.Illustration2BaseBitmap = null;
            this.illustIndex = 0;
            this.GetSpineDefault = getSpineDefaultFunc;
        }

        public int ID { get; set; }
        public bool Shop { get; set; }

        public int? Link { get; set; }
        private int illustIndex;
        private GetSpineDefaultFunc GetSpineDefault { get; set; }

        public int IllustIndex
        {
            get { return illustIndex; }
            set
            {
                if (this.Illustration2Bitmaps.Count == 0)
                {
                    illustIndex = 0;
                }
                else
                {
                    illustIndex = Math.Max(0, Math.Min(value, this.Illustration2Bitmaps.Count - 1));
                }
            }
        }

        public BitmapOrigin Default { get; set; }
        public List<Bitmap> Illustration2Bitmaps { get; set; }
        public Bitmap Illustration2BaseBitmap { get; set; }

        public Wz_Node Component { get; set; }

        public bool IsComponentNPC
        {
            get
            {
                return this.Component != null;
            }
        }

        //public LifeAnimateCollection Animates { get; private set; }

        public static Npc CreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode, GlobalFindNodeFunction2 findNode2, Wz_File wzf = null, GetSpineDefaultFunc getSpineDefaultFunc = null)
        {
            if (node == null) return null;

            int npcID;
            Match m = Regex.Match(node.Text, @"^(\d{7})\.img$");
            if (!(m.Success && Int32.TryParse(m.Result("$1"), out npcID)))
            {
                return null;
            }

            Npc npcInfo = new Npc(getSpineDefaultFunc);
            npcInfo.ID = npcID;
            Wz_Node infoNode = node.FindNodeByPath("info").ResolveUol();

            Point baseOrigin = Point.Empty;

            //加载基础属性
            if (infoNode != null)
            {
                foreach (var propNode in infoNode.Nodes)
                {
                    switch (propNode.Text)
                    {
                        case "shop": npcInfo.Shop = propNode.GetValueEx<int>(0) != 0; break;
                        case "link": npcInfo.Link = propNode.GetValueEx<int>(0); break;
                        case "component": npcInfo.Component = propNode; break;
                        case "default": npcInfo.Default = BitmapOrigin.CreateFromNode(propNode, findNode, wzf); break;
                        case "illustration2":
                            foreach (var imgNode in propNode.Nodes)
                            {
                                switch (imgNode.Text)
                                {
                                    case "base":
                                        var bmpOrigin = BitmapOrigin.CreateFromNode(imgNode, findNode, wzf);
                                        if (bmpOrigin.Bitmap != null && bmpOrigin.Bitmap.Size != new Size(1, 1))
                                        {
                                            npcInfo.Illustration2BaseBitmap = bmpOrigin.Bitmap;
                                            baseOrigin = bmpOrigin.Origin;
                                        }
                                        break;
                                    case "face":
                                        string spine = null;
                                        if (npcInfo.GetSpineDefault != null)
                                        {
                                            spine = imgNode.Nodes["spine"]?.Value as string;
                                        }
                                        foreach (var faceNode in imgNode.Nodes)
                                        {
                                            try
                                            {
                                                if (!string.IsNullOrEmpty(spine))
                                                {
                                                    string[] suffixes = { ".atlas", ".json", ".skel" };
                                                    if (!(faceNode.Text.StartsWith(spine) && suffixes.Any(s => faceNode.Text.EndsWith(s, StringComparison.OrdinalIgnoreCase)))) continue;

                                                    List<Bitmap> spineDefault = npcInfo.GetSpineDefault(faceNode);
                                                    if (spineDefault != null)
                                                    {
                                                        foreach (var bmp in npcInfo.Illustration2Bitmaps)
                                                        {
                                                            bmp?.Dispose();
                                                        }
                                                        npcInfo.Illustration2Bitmaps.Clear();
                                                        npcInfo.Illustration2Bitmaps.AddRange(spineDefault);
                                                        break;
                                                    }
                                                }

                                                var faceBmpOrigin = BitmapOrigin.CreateFromNode(faceNode, findNode, wzf);
                                                if (faceBmpOrigin.Bitmap != null && faceBmpOrigin.Bitmap.Size != new Size(1, 1))
                                                {
                                                    if (baseOrigin != Point.Empty)
                                                    {
                                                        Bitmap combinedBmp = new Bitmap(npcInfo.Illustration2BaseBitmap.Width, npcInfo.Illustration2BaseBitmap.Height);
                                                        using (Graphics g = Graphics.FromImage(combinedBmp))
                                                        {
                                                            g.DrawImageUnscaled(npcInfo.Illustration2BaseBitmap, 0, 0);
                                                            g.DrawImageUnscaled(faceBmpOrigin.Bitmap, baseOrigin.X - faceBmpOrigin.Origin.X, baseOrigin.Y - faceBmpOrigin.Origin.Y);
                                                        }
                                                        npcInfo.Illustration2Bitmaps.Add(combinedBmp);
                                                    }
                                                    else
                                                    {
                                                        npcInfo.Illustration2Bitmaps.Add(faceBmpOrigin.Bitmap);
                                                    }
                                                }
                                            }
                                            catch
                                            {
                                                continue;
                                            }
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }
            }

            //读取默认图片
            if (npcInfo.Default.Bitmap == null)
            {
                Wz_Node linkNode = null;
                if (npcInfo.Link != null && findNode2 != null)
                {
                    linkNode = findNode2(string.Format("Npc\\{0:d7}.img", npcInfo.Link), wzf);
                }
                if (linkNode == null)
                {
                    linkNode = node;
                }

                var imageFrame = new BitmapOrigin();

                foreach (var action in new[] { "stand", "move", "fly" })
                {
                    var actNode = linkNode.FindNodeByPath(action + @"\0");
                    if (actNode != null)
                    {
                        imageFrame = BitmapOrigin.CreateFromNode(actNode, findNode, wzf);
                        if (imageFrame.Bitmap != null)
                        {
                            break;
                        }
                    }
                }

                npcInfo.Default = imageFrame;
            }

            return npcInfo;
        }

        public void Dispose()
        {
            if (this.Default.Bitmap != null)
                this.Default.Bitmap.Dispose();

            foreach (var bmp in this.Illustration2Bitmaps)
            {
                if (bmp != null)
                    bmp.Dispose();
            }
        }
    }
}
