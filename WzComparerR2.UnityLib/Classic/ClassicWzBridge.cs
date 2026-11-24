using System;
using System.Collections.Generic;
using System.Linq;
using WzComparerR2.UnityLib.Unity;
using WzComparerR2.UnityLib.Wzjs;
using WzComparerR2.WzLib;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// 以傳統 WZ 的 <see cref="Wz_Node"/> 形式表示一個 WZJS img。
    /// <para>
    /// 舊版 WZ 的 Canvas 值型別（<c>Wz_Png</c>）需要綁定 <c>Wz_Image</c> 與檔案資料流，
    /// 無法從記憶體憑空建立；因此這裡的 Canvas 節點以 <see cref="UnityCanvas"/>
    /// 作為 <see cref="Wz_Node.Value"/>，需要圖片時再呼叫其
    /// <see cref="UnityCanvas.GetPng"/> 解碼。其餘型別沿用 WzLib 既有表示：
    /// 整數 → <see cref="int"/>、長整數 → <see cref="long"/>、浮點 → <see cref="float"/>/
    /// <see cref="double"/>、向量 → <see cref="Wz_Vector"/>、UOL → <see cref="Wz_Uol"/>。
    /// </para>
    /// </summary>
    public sealed class UnityWzImage
    {
        private readonly ClassicWzSource source;
        private Wz_Node node;

        public UnityWzImage(ClassicWzSource source, string name)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            this.source = source;
            this.Name = name;
        }

        /// <summary>img 邏輯路徑，如 <c>Character/00002000</c>。</summary>
        public string Name { get; private set; }

        /// <summary>所屬資料集。</summary>
        public ClassicWzSource Source
        {
            get { return this.source; }
        }

        public WzjsNode Root
        {
            get { return this.source.GetTree(this.Name); }
        }

        /// <summary>取 img 內某節點的 PNG bytes；查無圖時回 null。</summary>
        public byte[] GetPng(string innerPath)
        {
            string path = string.IsNullOrEmpty(innerPath) ? this.Name : this.Name + "/" + innerPath;
            return this.source.GetImage(path);
        }

        /// <summary>以 WZ 風格路徑查詢同一資料集內的節點（可含 <c>.img</c> 後綴）。</summary>
        public Wz_Node FindNode(string wzPath)
        {
            return this.source.FindNode(wzPath);
        }

        /// <summary>以 WZ 節點樹形式取得內容（首次呼叫時轉換並快取）。</summary>
        public Wz_Node AsWzNode()
        {
            if (this.node == null)
            {
                this.node = ClassicWzBridge.CreateImageNode(this);
            }
            return this.node;
        }
    }

    /// <summary>
    /// Unity 資產樹 → WzComparerR2 <see cref="Wz_Node"/> 的橋接。
    /// </summary>
    public static class ClassicWzBridge
    {
        public const string WzssNodeName = "_WZSS";
        public const string SpriteSheetNodeName = "_SpriteSheet";
        public const string PrefabNodeName = "_Prefab";
        public const string SpriteNodeName = "_Sprite";
        public const string TextureNodeName = "_Texture2D";

        /// <summary>把一個 WZJS 節點（含子樹）轉成 WzLib 節點。</summary>
        public static Wz_Node ConvertNode(WzjsNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            Wz_Node root = new Wz_Node(node.Name);
            root.Value = ConvertValue(node.Value);
            foreach (WzjsNode child in node.Children)
            {
                root.Nodes.Add(ConvertNode(child));
            }
            return root;
        }

        /// <summary>建立單一 img 的 WZ 節點（含已解碼子樹與 Canvas PNG）。</summary>
        public static Wz_Node CreateImageNode(UnityWzImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }
            return CreateImageNode(image, image.Name);
        }

        /// <summary>
        /// 以 catalog 建出整棵「類 WZ」目錄樹：中繼節點是目錄，末端節點是 img
        /// （Value 為 <see cref="UnityWzImage"/>）、JSON（<see cref="UnityJsonAsset"/>）
        /// 或 AudioClip 資訊。
        /// </summary>
        public static Wz_Node CreateCatalogNode(ClassicWzSource source, bool includeTrees, bool includeImages, WzJs_Structure owner = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            Wz_Node root = new Wz_Node(nameof(Wz_Type.Base));
            Dictionary<string, Wz_Node> nodes = new Dictionary<string, Wz_Node>(StringComparer.Ordinal);
            nodes[string.Empty] = root;

            foreach (KeyValuePair<string, UnityEntryLocation> entry in source.Catalog.Entries)
            {
                if (owner != null && entry.Value.ClassId == UnityClassIds.AudioClip)
                {
                    int separator = entry.Key.IndexOf('/', "Sound/".Length);
                    if (entry.Key.StartsWith("Sound/", StringComparison.OrdinalIgnoreCase) && separator >= 0
                        && owner.SoundImages.ContainsKey(entry.Key.Substring(0, separator)))
                        continue;
                }

                bool isImageEntry = entry.Value.ClassId == UnityClassIds.MonoBehaviour
                    || (owner != null && entry.Value.ClassId == UnityClassIds.TextAsset);
                string[] segments = entry.Key.Split('/');
                string currentPath = string.Empty;
                Wz_Node current = root;
                WzJs_File categoryFile = null;
                for (int i = 0; i < segments.Length; i++)
                {
                    string name = segments[i];
                    if (i == segments.Length - 1 && isImageEntry
                        && !name.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
                    {
                        name += ".img";
                    }
                    currentPath = currentPath.Length == 0 ? name : currentPath + "/" + name;
                    Wz_Node child;
                    if (!nodes.TryGetValue(currentPath, out child))
                    {
                        child = current.Nodes.Add(name);
                        nodes[currentPath] = child;
                    }
                    current = child;
                    if (i == 0 && owner != null && (segments.Length > 1 || !isImageEntry))
                        categoryFile = owner.GetOrAddFile(child);
                }

                if (owner != null && isImageEntry)
                {
                    current.Value = new WzJs_Image(owner, categoryFile, current, entry.Key, checked((int)entry.Value.ByteSize));
                    continue;
                }

                switch (entry.Value.ClassId)
                {
                    case UnityClassIds.TextAsset:
                        current.Value = new UnityJsonAsset(source, entry.Key);
                        if (includeTrees)
                        {
                            try
                            {
                                UnityJson.Populate(current, source.ReadTextAsset(entry.Key).Script);
                            }
                            catch (Exception ex) when (ex is UnityException || ex is ClassicAssetException)
                            {
                                current.Nodes.Clear();
                            }
                        }
                        break;

                    case UnityClassIds.AudioClip:
                        try
                        {
                            current.Value = owner == null ? (object)source.GetAudioClip(entry.Key) : source.CreateSound(entry.Key, null);
                        }
                        catch (Exception ex) when (ex is UnityException || ex is ClassicAssetException)
                        {
                            current.Value = null;
                        }
                        break;

                    case UnityClassIds.MonoBehaviour:
                        current.Value = new UnityWzImage(source, entry.Key);
                        if (includeTrees)
                        {
                            try
                            {
                                WzjsNode tree = source.GetTree(entry.Key);
                                foreach (WzjsNode child in tree.Children)
                                {
                                    current.Nodes.Add(ConvertNode(
                                        child, entry.Key, includeImages ? source : null));
                                }
                            }
                            catch (Exception ex) when (ex is UnityException || ex is WzjsException || ex is ClassicAssetException)
                            {
                                current.Nodes.Clear();
                            }
                        }
                        break;
                }
            }
            if (owner != null)
            {
                foreach (var file in source.Catalog.Prefabs.Values.GroupBy(prefab => prefab.SerializedFile))
                    file.First().AppendImageAssets(root, source);

                foreach (var prefab in source.Catalog.Prefabs)
                {
                    Wz_Node node = root.Nodes[PrefabNodeName] ?? root.Nodes.Add(PrefabNodeName);
                    foreach (string part in prefab.Key.Split('/'))
                        node = node.Nodes[part] ?? node.Nodes.Add(part);

                    node.Value = new WzJs_Image(owner, null, node, prefab.Key, 0) { Prefab = prefab.Value };
                }

                foreach (string key in owner.SoundImages.Keys)
                {
                    string[] parts = key.Split('/');
                    Wz_Node category = root.Nodes[parts[0]] ?? root.Nodes.Add(parts[0]);
                    string name = parts[1].EndsWith(".img", StringComparison.OrdinalIgnoreCase)
                        ? parts[1] : parts[1] + ".img";
                    Wz_Node image = category.Nodes[name] ?? category.Nodes.Add(name);
                    image.Value = new WzJs_Image(owner, owner.GetOrAddFile(category), image, key, 0);
                }

                foreach (var sheet in source.Catalog.Image.SheetsByPath.OrderBy(sheet => sheet.Key, StringComparer.Ordinal))
                {
                    string[] parts = sheet.Key.Split('/');
                    if (!sheet.Value.Values.Any(entry => entry.IndexLocation != null))
                    {
                        continue;
                    }

                    Wz_Node category = parts.Length == 1 ? root : root.Nodes[parts[0]];
                    if (category != root && !(category?.Value is WzJs_File))
                    {
                        continue;
                    }

                    foreach (var variant in sheet.Value.OrderBy(variant => variant.Key, StringComparer.Ordinal))
                    {
                        if (variant.Value.IndexLocation == null || variant.Value.AtlasPages.Count == 0)
                            continue;

                        string folderName = variant.Value.IndexIsAsset ? SpriteSheetNodeName : WzssNodeName;
                        Wz_Node node = category.Nodes[folderName] ?? category.Nodes.Add(folderName);
                        for (int i = category == root ? 0 : 1; i < parts.Length; i++)
                            node = node.Nodes[parts[i]] ?? node.Nodes.Add(parts[i]);

                        string region = variant.Key == "COMMON" ? "Common" : variant.Key;
                        if (!(node.Value is UnityCanvas canvas))
                            node.Value = canvas = new UnityCanvas(source, sheet.Key);
                        canvas.AddVariant(region, new UnityCanvas(source, sheet.Key + "/" + region, variant.Value));
                    }
                }

                foreach (var asset in source.Catalog.Image.SetAssets.OrderBy(asset => asset.Key, StringComparer.Ordinal))
                {
                    string[] parts = asset.Key.Split('/');
                    int categoryIndex = root.Nodes[parts[0]]?.Value is WzJs_File ? 0 : 1;
                    if (parts.Length <= categoryIndex + 1
                        || !(root.Nodes[parts[categoryIndex]]?.Value is WzJs_File file))
                    {
                        continue;
                    }

                    string group = parts[categoryIndex + 1];
                    string imageKey = parts[categoryIndex] + "/" + group;
                    string imageName = group.EndsWith(".img", StringComparison.OrdinalIgnoreCase)
                        ? group : group + ".img";
                    Wz_Node imageNode = file.Node.Nodes[imageName] ?? file.Node.Nodes.Add(imageName);
                    var image = imageNode.Value as WzJs_Image;
                    if (image == null)
                    {
                        image = new WzJs_Image(owner, file, imageNode, imageKey, 0);
                        imageNode.Value = image;
                    }

                    var relativeParts = parts.Skip(categoryIndex + 2).ToList();

                    foreach (string sprite in asset.Value.Keys.OrderBy(key => key, StringComparer.Ordinal))
                    {
                        string spritePath = sprite.Length == 0 ? asset.Key : asset.Key + "/" + sprite;
                        string relative = string.Join("/", relativeParts.Concat(
                            sprite.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)));
                        image.AddSprite(relative, spritePath, asset.Value[sprite].Origin, asset.Value[sprite].Delay,
                            categoryIndex == 1 ? parts[0] : null);
                    }
                }
            }
            return root;
        }

        /// <summary>
        /// 把某個 catalog 資產的內容（WZJS 子樹或 JSON 屬性）附加到
        /// <paramref name="target"/> 底下。
        /// </summary>
        public static void AppendEntryContents(Wz_Node target, ClassicWzSource source, string imageKey)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            UnityEntryLocation location;
            if (!source.Catalog.Entries.TryGetValue(imageKey, out location))
            {
                return;
            }
            switch (location.ClassId)
            {
                case UnityClassIds.TextAsset:
                    UnityJson.Populate(target, source.ReadTextAsset(imageKey).Script);
                    break;

                case UnityClassIds.MonoBehaviour:
                    foreach (WzjsNode child in source.GetTree(imageKey).Children)
                    {
                        target.Nodes.Add(ConvertNode(child, imageKey, source));
                    }
                    break;
            }
        }

        /// <summary>
        /// 動作影格延遲總和（各影格 <c>delay</c> 的絕對值相加）；
        /// null 或沒有 delay 時回 0。
        /// </summary>
        public static int SumActionDelay(Wz_Node actionNode)
        {
            if (actionNode == null)
            {
                return 0;
            }
            int delay = 0;
            foreach (Wz_Node frameNode in actionNode.Nodes)
            {
                Wz_Node delayNode = frameNode.FindNodeByPath("delay");
                if (delayNode != null && delayNode.Value != null)
                {
                    delay += Math.Abs(delayNode.GetValueEx<int>(0));
                }
            }
            return delay;
        }

        /// <summary>把 WZJS 節點值轉成 WzLib 慣用的值型別。</summary>
        public static object ConvertValue(WzjsValue value)        {
            switch (value.Kind)
            {
                case WzjsValueKind.U8:
                case WzjsValueKind.U16:
                case WzjsValueKind.Int:
                    return (int)value.Integer;
                case WzjsValueKind.Long:
                    return value.Integer;
                case WzjsValueKind.Double:
                    return value.Real;
                case WzjsValueKind.Float2:
                    return value.X;
                case WzjsValueKind.Int2:
                    return value.Int2A;
                case WzjsValueKind.Vector:
                    return new Wz_Vector(
                        (int)Math.Round(value.X), (int)Math.Round(value.Y));
                case WzjsValueKind.String:
                    return value.Text;
                case WzjsValueKind.Uol:
                    return new Wz_Uol(value.Text);
                default:
                    return null;
            }
        }

        private static Wz_Node CreateImageNode(UnityWzImage image, string imageKey)
        {
            WzjsNode tree = image.Root;
            Wz_Node root = new Wz_Node(tree.Name);
            root.Value = image;
            foreach (WzjsNode child in tree.Children)
            {
                root.Nodes.Add(ConvertNode(child, imageKey, image.Source));
            }
            return root;
        }

        private static Wz_Node ConvertNode(WzjsNode node, string imageKey, ClassicWzSource imageSource)
        {
            return ConvertNode(node, imageKey, string.Empty, imageSource);
        }

        /// <summary>
        /// 轉換單一節點（含相對路徑追蹤），讓 Canvas 能查到對應 PNG。
        /// </summary>
        private static Wz_Node ConvertNode(WzjsNode node, string imageKey, string parentPath, ClassicWzSource imageSource)
        {
            string path = parentPath.Length == 0 ? node.Name : parentPath + "/" + node.Name;
            Wz_Node result = new Wz_Node(node.Name);
            result.Value = ConvertNodeValue(node, imageKey, path, imageSource);
            foreach (WzjsNode child in node.Children)
            {
                result.Nodes.Add(ConvertNode(child, imageKey, path, imageSource));
            }
            return result;
        }

        private static object ConvertNodeValue(
            WzjsNode node, string imageKey, string innerPath, ClassicWzSource imageSource)
        {
            if (node.IsCanvas)
            {
                if (imageSource == null)
                {
                    return null;
                }
                string fullPath = innerPath.Length == 0 ? imageKey : imageKey + "/" + innerPath;
                return new UnityCanvas(imageSource, fullPath);
            }
            return ConvertValue(node.Value);
        }
    }
}
