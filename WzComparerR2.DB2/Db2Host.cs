using DevComponents.DotNetBar;
using WzComparerR2.Common;
using WzComparerR2.Controls;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;

namespace WzComparerR2.DB2
{
    /// <summary>
    /// 各表單與主程式之間的橋接層。
    ///
    /// 移植自 WzComparerR2-Plus 時，這些功能原本是 MainForm 上的靜態成員；
    /// 改成外掛後一律改走 PluginContext 或 PluginManager，外掛不再需要碰主視窗內部。
    /// </summary>
    internal static class Db2Host
    {
        public static PluginContext Context { get; set; }

        /// <summary>主程式的快速預覽視窗。</summary>
        public static AlphaForm Tooltip => Context?.DefaultTooltipWindow;

        /// <summary>
        /// 已開啟 WZ 的根節點，子節點為 Base.wz / Sound.wz / Skill001.wz 之類。
        /// </summary>
        public static Wz_Node TreeNode
        {
            get
            {
                // 由 Base 節點往上走到根節點。
                // 傳統版面 Base.wz 掛在根節點底下，KMST1125 的 Data 資料夾版面
                // 則是 Base.wz 本身就是根節點，往上走可同時涵蓋兩者。
                var node = PluginManager.FindWz(Wz_Type.Base);
                while (node?.ParentNode != null)
                {
                    node = node.ParentNode;
                }
                return node;
            }
        }

        /// <summary>是否有載入 Mob001.wz（分割式怪物檔）。</summary>
        public static bool HasMob001 => PluginManager.FindWz("Mob001") != null;

        /// <summary>是否有載入 Skill001.wz（分割式技能檔）。</summary>
        public static bool HasSkill001 => PluginManager.FindWz("Skill001") != null;

        /// <summary>在主視窗的 WZ 樹狀圖中選取指定節點。</summary>
        public static void SelectNode(Wz_Node node)
        {
            if (node != null)
            {
                Context?.SelectNode(node);
            }
        }

        /// <summary>Base.wz 未開啟時提示並傳回 false。</summary>
        public static bool RequireBaseWz()
        {
            if (PluginManager.FindWz(Wz_Type.Base) == null)
            {
                MessageBoxEx.Show("Base.wz가 열려 있지 않습니다");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 以完整 WZ 路徑取得節點，並解析 UOL / _inlink / _outlink。
        /// 移植自 WzComparerR2-Plus 的 MainForm.GetNode。
        /// </summary>
        public static Wz_Node GetNode(string path)
        {
            Wz_Node node = PluginManager.FindWz(path);
            if (node != null)
            {
                if (node.Value is Wz_Uol)
                {
                    Wz_Node resolved = node.ResolveUol();
                    if (resolved == null)
                    {
                        return null;
                    }

                    var outlink = resolved.FindNodeByPath("_outlink", true);
                    if (outlink?.Value != null)
                    {
                        var linkStr = outlink.Value.ToString();
                        string[] split = linkStr.Split('/');
                        switch (split[0])
                        {
                            case "Mob":
                            case "Map":
                                return PluginManager.FindWz(linkStr);
                            case "Skill":
                                return GetNode(linkStr);
                        }
                    }
                    return resolved;
                }

                return node.GetLinkedSourceNode(PluginManager.FindWz);
            }
            else
            {
                string[] split = path.Split('/');
                var root = PluginManager.FindWz(split[0]);
                if (root == null)
                {
                    return null;
                }

                int count = 0;
                string str = "";
                string path1 = "";
                bool hasUol = false;
                for (int i = 1; i < split.Length; i++)
                {
                    str = i == 1 ? split[i] : str + '/' + split[i];
                    var probe = root.FindNodeByPath(true, str.Split('/'));
                    if (probe != null && probe.Value is Wz_Uol)
                    {
                        hasUol = true;
                        count = i;
                        path1 = str;
                        break;
                    }
                }

                if (hasUol)
                {
                    str = "";
                    for (int i = count + 1; i < split.Length; i++)
                    {
                        str = i == count + 1 ? split[i] : str + '/' + split[i];
                    }
                    var uolTarget = root.FindNodeByPath(true, path1.Split('/'))?.ResolveUol();
                    return string.IsNullOrEmpty(str)
                        ? uolTarget
                        : uolTarget?.FindNodeByPath(true, str.Split('/'));
                }
            }

            return null;
        }
    }
}
