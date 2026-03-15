using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Morph
    {
        public Morph()
        {
        }

        public int ID { get; set; }
        public int Speed { get; set; }
        public int Jump { get; set; }
        public int Swim { get; set; }
        public int Fs { get; set; }
        public int MorphEffect { get; set; }
        public bool NoCancelDamage { get; set; }
        public bool NoCancelMouse { get; set; }
        public bool Superman { get; set; }
        public bool Kaiser { get; set; }
        public BitmapOrigin Default { get; set; }

        public static Morph CreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode, GlobalFindNodeFunction2 findNode2, Wz_File wzf = null)
        {
            if (node == null) return null;

            Match m = Regex.Match(node.Text, @"^(\d+)\.img$");
            if (!(m.Success && Int32.TryParse(m.Result("$1"), out var morphID)))
            {
                return null;
            }

            Morph morph = new Morph();
            morph.ID = morphID;
            Wz_Node infoNode = node.FindNodeByPath("info").ResolveUol();
            if (infoNode != null)
            {
                foreach (var propNode in infoNode.Nodes)
                {
                    switch (propNode.Text)
                    {
                        case "speed":
                            morph.Speed = propNode.GetValueEx<int>(0); break;
                        case "jump":
                            morph.Jump = propNode.GetValueEx<int>(0); break;
                        case "swim":
                            morph.Swim = propNode.GetValueEx<int>(0); break;
                        case "fs":
                            morph.Fs = propNode.GetValueEx<int>(0); break;

                        case "morphEffect":
                            morph.MorphEffect = propNode.GetValueEx<int>(0); break;

                        case "noCancelDamage":
                            morph.NoCancelDamage = propNode.GetValueEx<int>(0) != 0; break;
                        case "noCancelMouse":
                            morph.NoCancelMouse = propNode.GetValueEx<int>(0) != 0; break;
                        case "kaiser":
                            morph.Kaiser = propNode.GetValueEx<int>(0) != 0; break;
                        case "superman":
                            morph.Superman = propNode.GetValueEx<int>(0) != 0; break;
                    }
                }
            }

            //读取怪物默认动作
            {
                var imageFrame = new BitmapOrigin();

                foreach (var action in new[] { @"stand\0", @"walk\0", @"fly\0" })
                {
                    var actNode = node.FindNodeByPath(action);
                    imageFrame = BitmapOrigin.CreateFromNode(actNode, findNode, wzf);
                    if (imageFrame.Bitmap != null && !(imageFrame.Bitmap.Width == 1 && imageFrame.Bitmap.Height == 1))
                    {
                        break;
                    }
                }

                morph.Default = imageFrame;
            }

            return morph;
        }

        public void Dispose()
        {
            if (this.Default.Bitmap != null)
                this.Default.Bitmap.Dispose();
        }
    }
}