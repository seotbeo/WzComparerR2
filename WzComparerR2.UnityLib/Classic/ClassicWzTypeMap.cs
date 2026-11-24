using System;
using WzComparerR2.WzLib;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// Classic 資料集的目錄 → <see cref="Wz_Type"/> 對應。
    /// <para>
    /// Unity 用戶端沒有 WZ 檔，但許多既有功能只依 <c>Wz_File.Type</c> 判斷節點種類
    /// （道具／裝備／地圖／任務工具提示等）。這裡把資產路徑的第一段目錄對應成
    /// 傳統 WZ 的型別，讓那些功能也能處理 Classic 節點。
    /// </para>
    /// <para>
    /// 對應關係：<c>Character/*</c> → Character、<c>Item/*</c> → Item、<c>Mob/*</c> → Mob、
    /// <c>Npc/*</c> → Npc、<c>Map/*</c> → Map、<c>Skill/*</c> → Skill、<c>Quest/*</c> → Quest、
    /// <c>String/*</c> → String、<c>Sound/*</c> → Sound…以此類推。
    /// </para>
    /// </summary>
    public static class ClassicWzTypeMap
    {
        /// <summary>
        /// 依資產路徑（如 <c>Character/Cap/01002000</c> 或 <c>Character\Cap\01002000</c>）
        /// 或單一目錄名取得對應的 <see cref="Wz_Type"/>；無法對應時回 <see cref="Wz_Type.Unknown"/>。
        /// </summary>
        public static Wz_Type GetWzType(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return Wz_Type.Unknown;
            }

            int slash = assetPath.IndexOfAny(new[] { '/', '\\' });
            string root = slash < 0 ? assetPath : assetPath.Substring(0, slash);
            switch (root.ToLowerInvariant())
            {
                case "character":
                    return Wz_Type.Character;
                case "effect":
                    return Wz_Type.Effect;
                case "etc":
                    return Wz_Type.Etc;
                case "item":
                    return Wz_Type.Item;
                case "language":
                    return Wz_Type.Language;
                case "map":
                    return Wz_Type.Map;
                case "mob":
                    return Wz_Type.Mob;
                case "morph":
                    return Wz_Type.Morph;
                case "npc":
                    return Wz_Type.Npc;
                case "quest":
                    return Wz_Type.Quest;
                case "reactor":
                    return Wz_Type.Reactor;
                case "skill":
                    return Wz_Type.Skill;
                case "sound":
                    return Wz_Type.Sound;
                case "string":
                    return Wz_Type.String;
                case "tamingmob":
                    return Wz_Type.TamingMob;
                case "ui":
                    return Wz_Type.UI;
                default:
                    return Wz_Type.Unknown;
            }
        }

        /// <summary>
        /// <see cref="GetWzType(string)"/> 的可空版本：無法對應時回 null（供外部型別解析器使用）。
        /// </summary>
        public static Wz_Type? TryGetWzType(string assetPath)
        {
            Wz_Type type = GetWzType(assetPath);
            return type == Wz_Type.Unknown ? (Wz_Type?)null : type;
        }
    }
}
