using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.UnityLib.Images;
using WzComparerR2.UnityLib.Unity;
using WzComparerR2.UnityLib.Wzjs;

namespace WzComparerR2.UnityLib.Classic
{
    public enum ClassicChildKind
    {
        Category,
        Image,
        Node,
    }

    /// <summary><see cref="ClassicWzSource.ListChildren"/> 的一筆結果。</summary>
    public sealed class ClassicChildEntry
    {
        internal ClassicChildEntry(
            ClassicChildKind kind, string name, string path,
            int imageCount, int classId, int typeTag, bool hasChildren)
        {
            this.Kind = kind;
            this.Name = name;
            this.Path = path;
            this.ImageCount = imageCount;
            this.ClassId = classId;
            this.TypeTag = typeTag;
            this.HasChildren = hasChildren;
        }

        public ClassicChildKind Kind { get; private set; }
        public string Name { get; private set; }
        public string Path { get; private set; }

        /// <summary>目錄節點：其下的 img 總數。</summary>
        public int ImageCount { get; private set; }

        /// <summary>img 節點：Unity classID（114 = WZJS、49 = TextAsset、83 = AudioClip）。</summary>
        public int ClassId { get; private set; }

        /// <summary>WZJS 樹內節點的 type tag。</summary>
        public int TypeTag { get; private set; }

        public bool HasChildren { get; private set; }
    }

    public struct ClassicNodeChild
    {
        public ClassicNodeChild(string name, int typeTag, bool hasChildren)
        {
            this.Name = name;
            this.TypeTag = typeTag;
            this.HasChildren = hasChildren;
        }

        public string Name { get; private set; }
        public int TypeTag { get; private set; }
        public bool HasChildren { get; private set; }
    }

    /// <summary>單一節點的詳細內容（右欄顯示用）。</summary>
    public sealed class ClassicNodeDetail
    {
        internal ClassicNodeDetail(
            string path, string name, int typeTag, WzjsValue value, string nodePath,
            IReadOnlyList<ClassicNodeChild> children, string textAsset)
        {
            this.Path = path;
            this.Name = name;
            this.TypeTag = typeTag;
            this.Value = value;
            this.NodePath = nodePath;
            this.Children = children;
            this.TextAsset = textAsset;
        }

        public string Path { get; private set; }
        public string Name { get; private set; }
        public int TypeTag { get; private set; }
        public WzjsValue Value { get; private set; }

        /// <summary>WZJS pool B 路徑（img 內相對路徑）。</summary>
        public string NodePath { get; private set; }

        public IReadOnlyList<ClassicNodeChild> Children { get; private set; }

        /// <summary>TextAsset（class 49）的 JSON 明文；WZJS 節點為 null。</summary>
        public string TextAsset { get; private set; }
    }

    /// <summary>
    /// 開啟的 Classic World 資料集：catalog + bundle 控制代碼 + 已解碼 WZJS 樹的快取。
    /// <para>
    /// 對外公開的邏輯路徑：img 之前是目錄（由索引鍵分組產生），命中索引鍵的那個
    /// 節點是 img，img 之後的段落由 WZJS 樹導航（如
    /// <c>Character/00020116/angry/0/face</c>）。
    /// </para>
    /// <para>
    /// 圖片來源（WZSS / SpriteSheet asset / spriteset）由 <c>unity::texture</c>
    /// 的管線解碼成 PNG；AudioClip 只解析中繼資料，FSB5 → Ogg 轉封裝不在本專案範圍。
    /// </para>
    /// </summary>
    public sealed partial class ClassicWzSource : IDisposable
    {
        private const int TreeCacheCapacity = 32;

        private static readonly string[] PreferredLocales = new[] { "TW", "CN", "JP", "KR" };

        private readonly List<UnityFsBundle> bundles;
        private readonly object treeCacheLock = new object();
        private readonly Dictionary<string, WzjsNode> treeCache = new Dictionary<string, WzjsNode>(StringComparer.Ordinal);
        private readonly List<string> treeCacheOrder = new List<string>();
        private readonly Dictionary<string, UnityWzImage> imageCache = new Dictionary<string, UnityWzImage>(StringComparer.Ordinal);
        private readonly UnityImageRenderer renderer;
        private string locale;

        private ClassicWzSource(ClassicCatalog catalog, List<UnityFsBundle> bundles, string locale, IReadOnlyList<string> locales)
        {
            this.Catalog = catalog;
            this.bundles = bundles;
            this.locale = locale;
            this.Locales = locales;
            this.renderer = new UnityImageRenderer(catalog, bundles, this.GetTree, locale);
        }

        public ClassicCatalog Catalog { get; private set; }

        public IReadOnlyList<UnityFsBundle> Bundles
        {
            get { return this.bundles; }
        }

        /// <summary>資料集內偵測到的語系代碼（TW/CN/JP/KR…），偏好順序在前。</summary>
        public IReadOnlyList<string> Locales { get; private set; }

        /// <summary>目前作用中的語系代碼（字串／Etc 資產路徑的第二段）。</summary>
        public string Locale
        {
            get { return this.locale; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (string.Equals(this.locale, value, StringComparison.Ordinal))
                {
                    return;
                }
                this.locale = value;
                this.renderer.Locale = value;
                this.renderer.ClearCache();
            }
        }

        /// <summary>開啟資料目錄：掃描所有 .bundle 建索引，再逐一開啟。</summary>
        public static ClassicWzSource Open(string rootDirectory)
        {
            ClassicCatalog catalog = ClassicCatalog.Build(rootDirectory);
            List<UnityFsBundle> bundles = new List<UnityFsBundle>(catalog.Bundles.Count);
            try
            {
                foreach (ClassicBundleMeta meta in catalog.Bundles)
                {
                    bundles.Add(UnityFsBundle.Open(
                        System.IO.Path.Combine(catalog.RootPath, meta.Name + ".bundle")));
                }
            }
            catch
            {
                foreach (UnityFsBundle bundle in bundles)
                {
                    bundle.Dispose();
                }
                throw;
            }

            List<string> locales = DetectLocales(catalog);
            return new ClassicWzSource(catalog, bundles, locales[0], locales);
        }

        public void Dispose()
        {
            foreach (UnityFsBundle bundle in this.bundles)
            {
                bundle.Dispose();
            }
        }

        /// <summary>以目前語系組出字串表路徑，如 <c>String/TW/Item</c>。</summary>
        public string StringPath(string leaf)
        {
            return "String/" + this.locale + "/" + leaf;
        }

        /// <summary>以目前語系組出 Etc 資產路徑，如 <c>Etc/TW/Commodity</c>。</summary>
        public string EtcPath(string leaf)
        {
            return "Etc/" + this.locale + "/" + leaf;
        }

        /// <summary>讀取 <see cref="UnityEntryLocation"/> 指向的物件 bytes。</summary>
        public byte[] ReadEntry(UnityEntryLocation location)
        {
            return this.renderer.ReadEntry(location);
        }

        /// <summary>列出 path 的直接子節點（"" = 頂層目錄）。</summary>
        public IReadOnlyList<ClassicChildEntry> ListChildren(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            List<ClassicChildEntry> output = new List<ClassicChildEntry>();

            UnityEntryLocation location;
            if (!this.Catalog.Entries.TryGetValue(path, out location))
            {
                string ignoredKey;
                string ignoredInner;
                if (!this.Catalog.TrySplitImagePrefix(path, out ignoredKey, out ignoredInner))
                {
                    foreach (ClassicIndexChild child in this.Catalog.ListIndexChildren(path))
                    {
                        if (child.IsImage)
                        {
                            int classId = 0;
                            UnityEntryLocation imageLocation;
                            if (this.Catalog.Entries.TryGetValue(child.Path, out imageLocation))
                            {
                                classId = imageLocation.ClassId;
                            }
                            output.Add(new ClassicChildEntry(
                                ClassicChildKind.Image, child.Name, child.Path, child.ImageCount, classId, 0, false));
                        }
                        else
                        {
                            output.Add(new ClassicChildEntry(
                                ClassicChildKind.Category, child.Name, child.Path, child.ImageCount, 0, 0, false));
                        }
                    }
                    return output;
                }
            }

            // img 或 img 內部
            string imageKey;
            string inner;
            if (!this.Catalog.TrySplitImagePrefix(path, out imageKey, out inner))
            {
                throw new ClassicAssetException("경로가 없거나 해석할 수 없습니다: " + path);
            }
            UnityEntryLocation imageLocation2;
            if (this.Catalog.Entries.TryGetValue(imageKey, out imageLocation2)
                && (imageLocation2.ClassId == UnityClassIds.TextAsset || imageLocation2.ClassId == UnityClassIds.AudioClip))
            {
                return output; // TextAsset / AudioClip 沒有子節點
            }

            WzjsNode root = this.GetTree(imageKey);
            WzjsNode node = inner.Length == 0
                ? root
                : root.Find(inner);
            if (node == null)
            {
                throw new ClassicAssetException("경로가 없거나 해석할 수 없습니다: " + path);
            }
            string basePath = inner.Length == 0 ? imageKey : imageKey + "/" + inner;

            List<WzjsNode> children = new List<WzjsNode>(node.Children);
            children.Sort((a, b) => ClassicCatalog.CompareNodeNames(a.Name, b.Name));
            foreach (WzjsNode child in children)
            {
                output.Add(new ClassicChildEntry(
                    ClassicChildKind.Node, child.Name, basePath + "/" + child.Name,
                    0, UnityClassIds.MonoBehaviour, child.TypeTag, child.Children.Count > 0));
            }
            return output;
        }

        /// <summary>取單一節點的詳細內容（右欄顯示用）。</summary>
        public ClassicNodeDetail GetNode(string path)
        {
            string imageKey;
            string inner;
            if (!this.Catalog.TrySplitImagePrefix(path, out imageKey, out inner))
            {
                throw new ClassicAssetException("경로가 없거나 해석할 수 없습니다: " + path);
            }
            UnityEntryLocation location;
            if (!this.Catalog.Entries.TryGetValue(imageKey, out location))
            {
                throw new ClassicAssetException("경로가 없거나 해석할 수 없습니다: " + imageKey);
            }

            if (location.ClassId == UnityClassIds.TextAsset)
            {
                if (inner.Length > 0)
                {
                    throw new ClassicAssetException("경로가 없거나 해석할 수 없습니다: " + path);
                }
                UnityTextAsset textAsset = this.ReadTextAsset(imageKey);
                return new ClassicNodeDetail(
                    path, textAsset.Name, UnityClassIds.TextAsset, WzjsValue.SubTree, null,
                    new ClassicNodeChild[0], Encoding.UTF8.GetString(textAsset.Script));
            }

            if (location.ClassId == UnityClassIds.AudioClip)
            {
                if (inner.Length > 0)
                {
                    throw new ClassicAssetException("경로가 없거나 해석할 수 없습니다: " + path);
                }
                UnityAudioClipInfo clip = this.GetAudioClip(imageKey);
                return new ClassicNodeDetail(
                    path, clip.Name, UnityClassIds.AudioClip, WzjsValue.SubTree, null,
                    new ClassicNodeChild[0], null);
            }

            WzjsNode root = this.GetTree(imageKey);
            WzjsNode node = inner.Length == 0 ? root : root.Find(inner);
            if (node == null)
            {
                throw new ClassicAssetException("경로가 없거나 해석할 수 없습니다: " + path);
            }
            List<ClassicNodeChild> children = new List<ClassicNodeChild>(node.Children.Count);
            foreach (WzjsNode child in node.Children)
            {
                children.Add(new ClassicNodeChild(child.Name, child.TypeTag, child.Children.Count > 0));
            }
            return new ClassicNodeDetail(
                path, node.Name, node.TypeTag, node.Value, node.Path, children, null);
        }

        /// <summary>讀 AudioClip 物件本身（不含聲音資料）。</summary>
        public UnityAudioClipInfo GetAudioClip(string path)
        {
            string imageKey;
            string inner;
            if (!this.Catalog.TrySplitImagePrefix(path, out imageKey, out inner) || inner.Length > 0)
            {
                throw new ClassicAssetException("경로가 없거나 해석할 수 없습니다: " + path);
            }
            UnityEntryLocation location;
            if (!this.Catalog.Entries.TryGetValue(imageKey, out location))
            {
                throw new ClassicAssetException("경로가 없거나 해석할 수 없습니다: " + imageKey);
            }
            byte[] data = this.ReadEntry(location);
            return UnityObjectParsers.ParseAudioClip(data, true);
        }

        internal WzLib.Wz_Sound CreateSound(string key, WzLib.Wz_Image image)
        {
            UnityEntryLocation location = this.Catalog.Entries[key];
            UnityAudioClipInfo clip = UnityObjectParsers.ParseAudioClip(this.ReadEntry(location), true);
            int duration = (int)Math.Round(clip.Length * 1000);
            return new WzLib.Wz_Sound(0, 0, duration, null, image)
            {
                ExternalChannels = clip.Channels,
                ExternalFrequency = clip.Frequency,
                ExternalSoundType = WzLib.Wz_SoundType.Ogg,
                DataProvider = () =>
                {
                    UnityFsBundle bundle = this.bundles[location.BundleIndex];
                    int node = bundle.FindNode(clip.ResourceNode);
                    if (node < 0)
                        throw new UnityException("오디오 리소스를 찾을 수 없습니다: " + clip.ResourceNode);
                    if (clip.ResourceSize <= 0 || clip.ResourceSize > int.MaxValue)
                        throw new UnityException("오디오 리소스 크기가 유효하지 않습니다: " + clip.ResourceSize);

                    byte[] fsb = bundle.ReadNodeRange(node, checked((long)clip.ResourceOffset), (int)clip.ResourceSize);
                    return Audio.Fsb5ToOgg.ToOgg(fsb);
                },
            };
        }

        /// <summary>讀 TextAsset（class 49）：直接回傳 JSON 明文。</summary>
        public UnityTextAsset ReadTextAsset(string imageKey)
        {
            UnityEntryLocation location;
            if (!this.Catalog.Entries.TryGetValue(imageKey, out location))
            {
                throw new ClassicAssetException("경로가 없거나 해석할 수 없습니다: " + imageKey);
            }
            byte[] data = this.ReadEntry(location);
            return UnityObjectParsers.ParseTextAsset(data, true);
        }

        /// <summary>取 canvas 或 uol 節點的圖片（PNG bytes）。查無圖時回 null。</summary>
        public byte[] GetImage(string path)
        {
            return this.renderer.GetImage(path);
        }

        /// <summary>精確 frame 無法解出時的整頁 atlas 預覽（PNG bytes）。</summary>
        internal byte[] RenderPrefabTexture(UnityEntryLocation location) => this.renderer.RenderAtlasPage(location);
        internal byte[] RenderPrefabSprite(UnityEntryLocation sprite, UnityEntryLocation texture)
            => this.renderer.RenderSetSprite(new Images.UnitySetSpriteEntry(sprite, texture));

        internal int GetSheetAtlasPageCount(string sheetKey) => this.renderer.GetSheetAtlasPageCount(sheetKey);

        internal byte[] GetSheetAtlas(string sheetKey, int page) => this.renderer.GetSheetAtlas(sheetKey, page);

        public byte[] GetCanvasAtlasFallback(string path)
        {
            return this.renderer.GetCanvasAtlasFallback(path);
        }

        /// <summary>解碼（或從快取取）一個 img 的 WZJS 樹。</summary>
        public WzjsNode GetTree(string imageKey)
        {
            if (imageKey == null)
            {
                throw new ArgumentNullException(nameof(imageKey));
            }
            lock (this.treeCacheLock)
            {
                WzjsNode cached;
                if (this.treeCache.TryGetValue(imageKey, out cached))
                {
                    this.treeCacheOrder.Remove(imageKey);
                    this.treeCacheOrder.Add(imageKey);
                    return cached;
                }
            }

            UnityEntryLocation location;
            if (!this.Catalog.Entries.TryGetValue(imageKey, out location))
            {
                throw new ClassicAssetException("경로가 없거나 해석할 수 없습니다: " + imageKey);
            }
            if (location.ClassId != UnityClassIds.MonoBehaviour)
            {
                throw new ClassicAssetException(string.Format(
                    "{0} 不是 WZJS（class {1}），無法展開", imageKey, location.ClassId));
            }

            byte[] payload = this.ReadEntry(location);
            UnityMonoBehaviourInfo mono = UnityObjectParsers.ParseMonoBehaviour(payload, true);
            if (mono.HeaderLength > payload.Length)
            {
                throw new UnityException("MonoBehaviour Header 범위 초과: " + imageKey);
            }
            byte[] wzjsPayload = new byte[payload.Length - mono.HeaderLength];
            Buffer.BlockCopy(payload, mono.HeaderLength, wzjsPayload, 0, wzjsPayload.Length);
            WzjsNode tree = WzjsImage.Parse(wzjsPayload);

            lock (this.treeCacheLock)
            {
                WzjsNode existing;
                if (this.treeCache.TryGetValue(imageKey, out existing))
                {
                    return existing;
                }
                if (this.treeCacheOrder.Count >= TreeCacheCapacity)
                {
                    string evict = this.treeCacheOrder[0];
                    this.treeCacheOrder.RemoveAt(0);
                    this.treeCache.Remove(evict);
                }
                this.treeCacheOrder.Add(imageKey);
                this.treeCache[imageKey] = tree;
            }
            return tree;
        }

        /// <summary>
        /// 動作影格延遲總和：<c>&lt;imgKey&gt;/&lt;actionName&gt;/&lt;frame&gt;/delay</c>
        /// 的絕對值加總；查無 img 或動作時回傳 null。
        /// </summary>
        public int? GetActionDelay(string imageKey, string actionName)
        {
            if (string.IsNullOrEmpty(imageKey) || string.IsNullOrEmpty(actionName))
            {
                return null;
            }
            UnityEntryLocation location;
            if (!this.Catalog.Entries.TryGetValue(imageKey, out location)
                || location.ClassId != UnityClassIds.MonoBehaviour)
            {
                return null;
            }

            WzjsNode actionNode = this.GetTree(imageKey).FindChild(actionName);
            if (actionNode == null)
            {
                return null;
            }
            return ClassicWzBridge.SumActionDelay(ClassicWzBridge.ConvertNode(actionNode));
        }

        /// <summary>
        /// 取得 img 的 <see cref="UnityWzImage"/>（內部快取；非 WZJS img 回 null）。
        /// </summary>
        public UnityWzImage GetWzImage(string imageKey)
        {
            if (string.IsNullOrEmpty(imageKey))
            {
                return null;
            }
            lock (this.imageCache)
            {
                UnityWzImage cached;
                if (this.imageCache.TryGetValue(imageKey, out cached))
                {
                    return cached;
                }
            }

            UnityEntryLocation location;
            if (!this.Catalog.Entries.TryGetValue(imageKey, out location)
                || location.ClassId != UnityClassIds.MonoBehaviour)
            {
                return null;
            }

            UnityWzImage image = new UnityWzImage(this, imageKey);
            lock (this.imageCache)
            {
                UnityWzImage existing;
                if (this.imageCache.TryGetValue(imageKey, out existing))
                {
                    return existing;
                }
                this.imageCache[imageKey] = image;
            }
            return image;
        }

        /// <summary>
        /// 以 WZ 風格路徑查詢節點（可含 <c>.img</c> 後綴、可用 <c>\</c> 分隔），
        /// 例如 <c>Map\Map\Map0\100000000.img\miniMap</c>；查無回 null。
        /// </summary>
        public WzLib.Wz_Node FindNode(string wzPath)
        {
            string normalized = NormalizeWzPath(wzPath);
            if (normalized == null)
            {
                return null;
            }

            string imageKey, inner;
            if (!this.Catalog.TrySplitImagePrefix(normalized, out imageKey, out inner))
            {
                // 目錄配置與 WZ 不同（如 Character/<id>、Item/<type>/<group>/<id>）：
                // 改以 id 產生 catalog 路徑候選。
                return this.FindNodeByCandidates(normalized);
            }
            UnityWzImage image = this.GetWzImage(imageKey);
            if (image == null)
            {
                return null;
            }

            WzLib.Wz_Node node = image.AsWzNode();
            if (string.IsNullOrEmpty(inner))
            {
                return node;
            }
            foreach (string segment in inner.Split('/'))
            {
                if (segment.Length == 0)
                {
                    continue;
                }
                node = FindChildByName(node, segment);
                if (node == null)
                {
                    return null;
                }
            }
            return node;
        }

        /// <summary>
        /// 以路徑末段（道具／裝備／技能…的 id）產生 catalog 路徑候選再查找，
        /// 用於 WZ 與 Unity 目錄配置不同的情況。
        /// </summary>
        private WzLib.Wz_Node FindNodeByCandidates(string normalized)
        {
            int slash = normalized.LastIndexOf('/');
            if (slash <= 0)
            {
                return null;
            }
            int id;
            if (!int.TryParse(normalized.Substring(slash + 1), out id))
            {
                return null;
            }

            int rootEnd = normalized.IndexOf('/');
            List<string> candidates = new List<string>();
            switch (normalized.Substring(0, rootEnd))
            {
                case "Item":
                    candidates.AddRange(this.Catalog.GetItemPathCandidates(id));
                    candidates.AddRange(this.Catalog.GetEquipPathCandidates(id)); // 裝備在 Character，但少數呼叫端會組出 Item 路徑
                    break;
                case "Character":
                    candidates.AddRange(this.Catalog.GetEquipPathCandidates(id));
                    break;
                case "Mob":
                    candidates.AddRange(this.Catalog.GetMobPathCandidates(id));
                    break;
                case "Npc":
                    candidates.AddRange(this.Catalog.GetNpcPathCandidates(id));
                    break;
                case "Map":
                    candidates.AddRange(this.Catalog.GetMapPathCandidates(id));
                    break;
                case "Quest":
                    candidates.AddRange(this.Catalog.GetQuestPathCandidates(id));
                    break;
                case "Skill":
                    candidates.AddRange(this.Catalog.GetSkillPathCandidates(id));
                    break;
            }

            foreach (string candidate in candidates)
            {
                string imageKey, inner;
                if (!this.Catalog.TrySplitImagePrefix(candidate, out imageKey, out inner))
                {
                    continue;
                }
                UnityWzImage image = this.GetWzImage(imageKey);
                if (image == null)
                {
                    continue;
                }
                WzLib.Wz_Node node = image.AsWzNode();
                foreach (string segment in inner.Split('/'))
                {
                    if (segment.Length == 0)
                    {
                        continue;
                    }
                    node = FindChildByName(node, segment);
                    if (node == null)
                    {
                        break;
                    }
                }
                if (node != null)
                {
                    return node;
                }
            }
            return null;
        }

        /// <summary>把 WZ 風格路徑正規化成 catalog 索引鍵風格（去 <c>.img</c>、統一斜線）。</summary>
        private static string NormalizeWzPath(string wzPath)
        {
            if (string.IsNullOrEmpty(wzPath))
            {
                return null;
            }
            string[] segments = wzPath.Replace('\\', '/').Split('/');
            List<string> output = new List<string>(segments.Length);
            foreach (string segment in segments)
            {
                string name = segment;
                if (name.Length == 0)
                {
                    continue;
                }
                if (name.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - 4);
                }
                output.Add(name);
            }
            return output.Count == 0 ? null : string.Join("/", output);
        }

        private static WzLib.Wz_Node FindChildByName(WzLib.Wz_Node parent, string name)
        {
            WzLib.Wz_Node found = parent.FindNodeByPath(name);
            if (found != null)
            {
                return found;
            }

            // id 節點的補零位數可能不同（02000000 / 2000000）。
            long number;
            bool nameIsNumber = long.TryParse(name, out number);
            foreach (WzLib.Wz_Node child in parent.Nodes)
            {
                if (string.Equals(child.Text, name, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
                long childNumber;
                if (nameIsNumber && long.TryParse(child.Text, out childNumber) && childNumber == number)
                {
                    return child;
                }
            }
            return null;
        }

        private static List<string> DetectLocales(ClassicCatalog catalog)
        {
            SortedSet<string> found = new SortedSet<string>(StringComparer.Ordinal);
            const string prefix = "String/";
            foreach (string key in catalog.Entries.Keys)
            {
                if (!key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }
                string rest = key.Substring(prefix.Length);
                int slash = rest.IndexOf('/');
                if (slash <= 0 || slash == rest.Length - 1)
                {
                    continue;
                }
                found.Add(rest.Substring(0, slash));
            }

            List<string> ordered = new List<string>();
            foreach (string code in PreferredLocales)
            {
                if (found.Remove(code))
                {
                    ordered.Add(code);
                }
            }
            ordered.AddRange(found);
            if (ordered.Count == 0)
            {
                ordered.Add("TW");
            }
            return ordered;
        }
    }
}
