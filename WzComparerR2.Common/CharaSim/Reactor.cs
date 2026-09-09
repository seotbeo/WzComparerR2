using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Reactor : IDisposable
    {
        public Reactor()
        {
        }

        public int ID { get; set; }
        public string Name { get; set; }
        public string Action { get; set; }
        public int ClipCount { get; set; } = 0;
        public BitmapOrigin Thumbnail { get; set; }

        public static Reactor CreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode, Wz_File wzf = null)
        {
            return InnerCreateFromNode(node, findNode, wzf, new HashSet<int>());
        }

        private static Reactor InnerCreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode, Wz_File wzf, HashSet<int> visited)
        {
            if (node == null) return null;

            Match m = Regex.Match(node.Text, @"^(\d+)\.img$");
            if (!(m.Success && Int32.TryParse(m.Result("$1"), out var reactorID)))
            {
                return null;
            }

            Reactor reactor = new Reactor();

            Wz_Node infoNode = node.FindNodeByPath("info").ResolveUol();
            if (infoNode != null)
            {
                Wz_Node linkNode = null;
                int linkID = -1;
                if ((linkNode = infoNode.FindNodeByPath("link")) != null &&
                    (linkID = linkNode.GetValueEx<int>(-1)) >= 0)
                {
                    if (!visited.Contains(reactorID))
                    {
                        visited.Add(reactorID);
                        reactor = Reactor.InnerCreateFromNode(findNode?.Invoke($@"Reactor/{linkID}.img", wzf), findNode, wzf, visited) ?? reactor;
                    }
                }

                reactor.Name = infoNode.FindNodeByPath("viewName").GetValueEx<string>(null) ??
                    infoNode.FindNodeByPath("name").GetValueEx<string>(null) ??
                    infoNode.FindNodeByPath("info").GetValueEx<string>(null);
            }

            reactor.ID = reactorID;

            reactor.Action = node.FindNodeByPath("action").GetValueEx<string>(null);

            if (reactor.ClipCount <= 0)
            {
                Wz_Node clipNode = null;
                while (true)
                {
                    if ((clipNode = node.FindNodeByPath(reactor.ClipCount.ToString())) != null)
                    {
                        reactor.ClipCount++;
                        if (reactor.Thumbnail.Bitmap == null)
                        {
                            Wz_Node candidateNode = clipNode.FindNodeByPath("0");
                            if (candidateNode != null)
                            {
                                BitmapOrigin tmpThumbnail = BitmapOrigin.CreateFromNode(candidateNode, findNode, wzf);
                                if (tmpThumbnail.Bitmap == null)
                                {
                                    continue;
                                }

                                if (tmpThumbnail.Bitmap.Width <= 1 && tmpThumbnail.Bitmap.Height <= 1)
                                {
                                    tmpThumbnail.Bitmap.Dispose();
                                }
                                else
                                {
                                    reactor.Thumbnail = tmpThumbnail;
                                }
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return reactor;
        }

        public void Dispose()
        {
            if (this.Thumbnail.Bitmap != null)
                this.Thumbnail.Bitmap.Dispose();
        }
    }
}
