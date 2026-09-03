using System;
using System.Collections.Generic;
using System.Drawing;
using WzComparerR2.Common;
using WzComparerR2.WzLib;

namespace WzComparerR2.DB2
{
    /// <summary>
    /// MapleStoryDB2 (DB2Form) 專用的 Wz_Node 擴充方法。
    /// 從 WzComparerR2-Plus 的 Wz_NodeExtension2 移植，
    /// 放在獨立的命名空間以免污染 WzComparerR2.Common。
    /// </summary>
    public static class DB2NodeExtension
    {
        public static string IDString(this string Str)
        {
            return int.Parse(Str).ToString();
        }

        public static string GetPathD(this Wz_Node Node)
        {
            if (Node != null)
            {
                Stack<string> Path = new Stack<string>();
                Wz_Node ThisNode = Node;
                do
                {
                    Path.Push(ThisNode.Text);
                    ThisNode = ThisNode.ParentNode;
                } while (ThisNode != null);
                return string.Join(".", Path.ToArray());
            }
            return null;
        }

        public static string GetPath(this Wz_Node Node)
        {
            Stack<string> Path = new Stack<string>();
            Wz_Node ThisNode = Node;
            do
            {
                Path.Push(ThisNode.Text);
                ThisNode = ThisNode.ParentNode;
            } while (ThisNode != null);
            return string.Join("/", Path.ToArray());
        }

        public static int ValueToInt(this Wz_Node Node)
        {
            return Node.GetValueEx<int>(0);
        }

        public static string ValueToStr(this Wz_Node Node)
        {
            return Node.GetValueEx<string>("");
        }

        public static bool HasNode(this Wz_Node Node, string Path)
        {
            return Node.GetNode(Path) != null;
        }

        public static string ImgName(this Wz_Node Node)
        {
            try
            {
                return Node.GetNodeWzImage().Name;
            }
            catch
            {
                return "(null)";
            }
        }

        public static string ImgID(this Wz_Node Node)
        {
            try
            {
                return Node.GetNodeWzImage().Name.Replace(".img", "");
            }
            catch
            {
                return "0";
            }
        }

        public static T GetValue2<T>(this Wz_Node Node, string Path, T DefaultValue)
        {
            var found = Node.FindNodeByPathA(Path, true);
            return found != null ? found.GetValueEx(DefaultValue) : DefaultValue;
        }

        public static Wz_Node FindNodeByPathA(this Wz_Node Node, string FullPath, bool ExtractImage)
        {
            string[] Pattern = FullPath.Split('/');
            if (Node == null) return null;
            return Node.FindNodeByPath(ExtractImage, Pattern);
        }

        public static string FullPathToFileEx(this Wz_Node Node)
        {
            Stack<string> path = new Stack<string>();
            Wz_Node node = Node;
            do
            {
                if (node.Value is Wz_File wzf && !wzf.IsSubDir)
                {
                    if (node.Text.EndsWith(".wz", StringComparison.OrdinalIgnoreCase))
                    {
                        path.Push(node.Text.Substring(0, node.Text.Length - 3));
                    }
                    else
                    {
                        path.Push(node.Text);
                    }
                    break;
                }

                path.Push(node.Text);

                var img = node.GetValue<Wz_Image>();
                if (img != null)
                {
                    node = img.OwnerNode;
                }

                if (node != null)
                {
                    node = node.ParentNode;
                }
            } while (node != null);
            return string.Join("/", path.ToArray());
        }

        /// <summary>
        /// 把 Map001/Mob001/Skill001/Sound001 之類的分割檔路徑正規化回主檔路徑。
        /// </summary>
        private static string NormalizeWzPath(string FullPath)
        {
            string[] Split = FullPath.Split('/');
            switch (Split[0])
            {
                case "Map001": return FullPath.Replace("Map001", "Map");
                case "Map002": return FullPath.Replace("Map002", "Map");
                case "Map2": return FullPath.Replace("Map2", "Map");
                case "Mob001": return FullPath.Replace("Mob001", "Mob");
                case "Mob002": return FullPath.Replace("Mob002", "Mob");
                case "Mob2": return FullPath.Replace("Mob2", "Mob");
                case "Skill001": return FullPath.Replace("Skill001", "Skill");
                case "Skill002": return FullPath.Replace("Skill002", "Skill");
                case "Skill003": return FullPath.Replace("Skill003", "Skill");
                case "Sound001": return FullPath.Replace("Sound001", "Sound");
                case "Sound002": return FullPath.Replace("Sound002", "Sound");
                case "Sound2": return FullPath.Replace("Sound2", "Sound");
            }
            return FullPath;
        }

        public static string FullPathToFile2(this Wz_Node Node)
        {
            return NormalizeWzPath(Node.FullPathToFileEx());
        }

        public static string FullPathToFile2D(this Wz_Node Node)
        {
            return FullPathToFile2(Node).Replace("/", ".");
        }

        public static Wz_Node GetNode(this Wz_Node Node, string Path)
        {
            var found = Node.FindNodeByPathA(Path, true);
            if (found != null)
            {
                if (found.Value is Wz_Uol)
                {
                    return found.ResolveUol();
                }
                else
                {
                    var FullPath = NormalizeWzPath(found.FullPathToFileEx());
                    var resolved = PluginBase.PluginManager.FindWz(FullPath);
                    return resolved?.GetLinkedSourceNode(PluginBase.PluginManager.FindWz);
                }
            }
            else
            {
                string[] Split = Path.Split('/');
                int Count = 0;
                string Str = "";
                string Path1 = "";
                string Path2 = "";
                bool HasUol = false;
                for (int i = 0; i < Split.Length; i++)
                {
                    if (i == 0)
                        Str = Str + Split[i];
                    else
                        Str += '/' + Split[i];
                    var probe = Node.FindNodeByPathA(Str, true);
                    if (probe != null && probe.Value is Wz_Uol)
                    {
                        HasUol = true;
                        Count = i;
                        Path1 = Str;
                        break;
                    }
                }

                if (HasUol)
                {
                    Str = "";
                    for (int i = Count + 1; i < Split.Length; i++)
                    {
                        if (i == Count + 1)
                            Str = Str + Split[i];
                        else
                            Str = Str + '/' + Split[i];
                        Path2 = Str;
                    }
                    return Node.FindNodeByPathA(Path1, true).ResolveUol()?.FindNodeByPathA(Path2, true);
                }
            }
            return null;
        }

        public static Bitmap ExtractPng(this Wz_Node Node)
        {
            return (Node?.Value as Wz_Png)?.ExtractPng();
        }
    }
}
