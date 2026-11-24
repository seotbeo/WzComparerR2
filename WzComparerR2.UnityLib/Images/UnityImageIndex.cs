using System;
using System.Collections.Generic;
using System.Linq;
using WzComparerR2.UnityLib.Classic;
using WzComparerR2.UnityLib.Unity;

namespace WzComparerR2.UnityLib.Images
{
    /// <summary>spritesheet 的一組索引與 atlas 頁。</summary>
    public sealed class UnitySheetEntry
    {
        public UnitySheetEntry()
        {
            this.AtlasPages = new List<KeyValuePair<int, UnityEntryLocation>>();
        }

        /// <summary>container 條目順序不保證 index 早於 atlas，建構途中允許尚未看到索引。</summary>
        public UnityEntryLocation IndexLocation { get; set; }

        /// <summary>新版圖集用 .asset 保存路徑與裁切資料；舊版仍使用 WZSS。</summary>
        public bool IndexIsAsset { get; set; }

        /// <summary>(page 序號, Texture2D 物件位置)，依頁號排序。</summary>
        public List<KeyValuePair<int, UnityEntryLocation>> AtlasPages { get; private set; }
    }

    /// <summary>spriteset 的一個 sprite：Sprite 物件位置 + 其 Texture2D 物件位置。</summary>
    public sealed class UnitySetSpriteEntry
    {
        public UnitySetSpriteEntry(UnityEntryLocation spriteLocation, UnityEntryLocation textureLocation)
        {
            this.SpriteLocation = spriteLocation;
            this.TextureLocation = textureLocation;
        }

        internal WzComparerR2.WzLib.Wz_Vector Origin { get; set; }
        internal int Delay { get; set; }
        internal UnityRectF? AtlasRect { get; set; }

        public UnityEntryLocation SpriteLocation { get; private set; }
        public UnityEntryLocation TextureLocation { get; private set; }
    }

    /// <summary>
    /// 圖片索引：邏輯路徑 → 圖片來源。
    /// <list type="bullet">
    /// <item>spritesheet_*：container 裡的 <c>.wzspritesheet</c>（舊 WZSS）或
    /// <c>.asset</c>（新版索引物件）+ <c>_N.png</c>（atlas 頁 Texture2D）。</item>
    /// <item>spriteset_*：container 裡的 <c>.asset</c>（sprite 清單 MonoBehaviour）+
    /// Sprite(213) 物件 + Texture2D。</item>
    /// </list>
    /// </summary>
    public sealed class UnityImageIndex
    {
        private const string SheetPrefix = "assets/wzassets/spritesheet/";
        private const string SetPrefix = "assets/wzassets/spriteset/";

        public UnityImageIndex()
        {
            this.SheetsByPath = new Dictionary<string, Dictionary<string, UnitySheetEntry>>(StringComparer.Ordinal);
            this.SetAssets = new Dictionary<string, Dictionary<string, UnitySetSpriteEntry>>(StringComparer.Ordinal);
        }

        /// <summary>邏輯路徑 → 語系 → 該語系的一組索引與圖頁。</summary>
        public Dictionary<string, Dictionary<string, UnitySheetEntry>> SheetsByPath { get; private set; }

        /// <summary>asset 路徑（去前綴去副檔名）→ sprite 清單（名稱 → sprite）。</summary>
        public Dictionary<string, Dictionary<string, UnitySetSpriteEntry>> SetAssets { get; private set; }

        /// <summary>
        /// 使用 Catalog 已解析的 SerializedFile 與 AssetBundle container 收錄圖片來源。
        /// </summary>
        internal void CollectFromContainer(
            UnityFsBundle bundle, int bundleIndex, string bundleStem, int nodeIndex,
            UnitySerializedFile serializedFile, IReadOnlyList<UnityContainerEntry> container,
            Dictionary<long, UnityObjectInfo> objectsByPathId,
            ClassicCatalog.ExternalResolver resolveExternal, Func<int, UnityFsBundle> openBundle)
        {
            // 早期 beta 的 bundle 名稱是 spritesheetgroup_*/spritesetgroup_*（Addressables
            // 群組前綴），因此不能只看檔名前綴，改以 container 路徑判斷。
            bool isSheet = bundleStem.StartsWith("spritesheet", StringComparison.Ordinal)
                || ContainsPathPrefix(container, SheetPrefix);
            bool isSet = bundleStem.StartsWith("spriteset", StringComparison.Ordinal)
                || ContainsPathPrefix(container, SetPrefix);
            if (!isSheet && !isSet)
            {
                return;
            }

            Func<long, UnityEntryLocation> locationOf = pathId =>
            {
                UnityObjectInfo obj;
                if (!objectsByPathId.TryGetValue(pathId, out obj))
                {
                    return null;
                }
                long absolute = serializedFile.DataOffset + obj.ByteStart;
                if (absolute < serializedFile.DataOffset)
                {
                    return null;
                }
                return new UnityEntryLocation(
                    bundleIndex, nodeIndex, absolute, obj.ByteSize, obj.ClassId);
            };

            if (isSheet)
            {
                this.CollectSheet(container, locationOf);
            }
            if (isSet)
            {
                this.CollectSet(bundle, bundleIndex, nodeIndex, serializedFile, container, locationOf, resolveExternal, openBundle);
            }
        }

        /// <summary>container 內是否有路徑以前綴開頭（不分大小寫；索引鍵為小寫）。</summary>
        private static bool ContainsPathPrefix(
            IReadOnlyList<UnityContainerEntry> container, string prefix)
        {
            foreach (UnityContainerEntry entry in container)
            {
                string path = entry.Path;
                if (path != null && path.Length >= prefix.Length
                    && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>優先使用目前語系，再使用共用圖集；仍找不到時取任一份，避免選錯地域導致圖片全數消失。</summary>
        public static UnitySheetEntry SelectLocalized(
            Dictionary<string, UnitySheetEntry> variants, string locale)
        {
            if (variants == null || variants.Count == 0)
            {
                return null;
            }
            if (variants.Count == 1)
            {
                foreach (UnitySheetEntry only in variants.Values)
                {
                    return only;
                }
            }
            UnitySheetEntry found;
            if (locale != null && variants.TryGetValue(locale.ToUpperInvariant(), out found))
            {
                return found;
            }
            if (variants.TryGetValue("COMMON", out found))
            {
                return found;
            }
            string firstKey = null;
            foreach (string key in variants.Keys)
            {
                if (firstKey == null || string.CompareOrdinal(key, firstKey) < 0)
                {
                    firstKey = key;
                }
            }
            return firstKey == null ? null : variants[firstKey];
        }

        private void CollectSheet(
            IReadOnlyList<UnityContainerEntry> container, Func<long, UnityEntryLocation> locationOf)
        {
            foreach (UnityContainerEntry entry in container)
            {
                string locale;
                string rest;
                if (!TrySplitSheetPath(entry.Path, out locale, out rest))
                {
                    continue;
                }

                string key;
                bool isAsset;
                int page;
                if (TryStripSuffix(rest, ".wzspritesheet", out key))
                {
                    isAsset = false;
                }
                else if (TryStripSuffix(rest, ".asset", out key))
                {
                    isAsset = true;
                }
                else if (TrySplitAtlasPage(rest, out key, out page))
                {
                    UnityEntryLocation pageLocation = locationOf(entry.Asset.PathId);
                    if (pageLocation == null)
                    {
                        continue;
                    }
                    UnitySheetEntry pageEntry = this.GetOrCreateSheet(key, locale);
                    bool exists = false;
                    foreach (KeyValuePair<int, UnityEntryLocation> item in pageEntry.AtlasPages)
                    {
                        if (item.Key == page)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        pageEntry.AtlasPages.Add(new KeyValuePair<int, UnityEntryLocation>(page, pageLocation));
                        pageEntry.AtlasPages.Sort((a, b) => a.Key.CompareTo(b.Key));
                    }
                    continue;
                }
                else
                {
                    continue;
                }

                UnityEntryLocation indexLocation = locationOf(entry.Asset.PathId);
                if (indexLocation == null)
                {
                    continue;
                }
                UnitySheetEntry sheet = this.GetOrCreateSheet(key, locale);
                sheet.IndexLocation = indexLocation;
                sheet.IndexIsAsset = isAsset;
            }
        }

        private void CollectSet(
            UnityFsBundle bundle, int bundleIndex, int nodeIndex, UnitySerializedFile serializedFile,
            IReadOnlyList<UnityContainerEntry> container, Func<long, UnityEntryLocation> locationOf,
            ClassicCatalog.ExternalResolver resolveExternal, Func<int, UnityFsBundle> openBundle)
        {
            UnityEntryLocation ResolveSprite(UnityPPtr pointer) => pointer.FileId == 0
                ? locationOf(pointer.PathId) : resolveExternal(serializedFile, pointer, out _);

            // .asset path → (名稱 → sprite pid)
            var assetMap = new Dictionary<string, Dictionary<string, UnityPPtr>>(StringComparer.Ordinal);
            var spriteNameSlots = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var frameDelays = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
            foreach (UnityContainerEntry entry in container)
            {
                string lower = entry.Path.ToLowerInvariant();
                if (!lower.StartsWith(SetPrefix, StringComparison.Ordinal) || !lower.EndsWith(".asset", StringComparison.Ordinal))
                {
                    continue;
                }
                string key = entry.Path.Substring(SetPrefix.Length, entry.Path.Length - SetPrefix.Length - ".asset".Length);
                UnityEntryLocation assetLocation = locationOf(entry.Asset.PathId);
                if (assetLocation == null)
                {
                    continue;
                }
                byte[] data = bundle.ReadNodeRange(
                    nodeIndex, assetLocation.AbsoluteOffset, (int)assetLocation.ByteSize);
                UnityMonoBehaviourInfo mono;
                try
                {
                    mono = UnityObjectParsers.ParseMonoBehaviour(data, serializedFile.IsLittleEndian);
                }
                catch (UnityException)
                {
                    continue;
                }
                if (mono.HeaderLength > data.Length)
                {
                    continue;
                }
                byte[] payload = new byte[data.Length - mono.HeaderLength];
                Buffer.BlockCopy(data, mono.HeaderLength, payload, 0, payload.Length);

                Dictionary<string, UnityPPtr> sprites;
                if (!assetMap.TryGetValue(key, out sprites))
                {
                    sprites = new Dictionary<string, UnityPPtr>(StringComparer.Ordinal);
                    assetMap[key] = sprites;
                }
                var items = ParseSetAssetPayload(payload, ResolveSprite, out var delays, out var extraSlots);
                foreach (var item in items)
                {
                    sprites[item.Key] = item.Value;
                    if (extraSlots?.Contains(item.Key) == true)
                    {
                        if (!spriteNameSlots.TryGetValue(key, out var namedSlots))
                            spriteNameSlots[key] = namedSlots = new HashSet<string>(StringComparer.Ordinal);
                        namedSlots.Add(item.Key);
                    }
                    else if (spriteNameSlots.TryGetValue(key, out var namedSlots))
                    {
                        namedSlots.Remove(item.Key);
                    }
                    if (delays != null && delays.TryGetValue(item.Key, out int delay))
                    {
                        if (!frameDelays.TryGetValue(key, out var setDelays))
                            frameDelays[key] = setDelays = new Dictionary<string, int>(StringComparer.Ordinal);
                        setDelays[item.Key] = delay;
                    }
                    else if (frameDelays.TryGetValue(key, out var setDelays))
                    {
                        setDelays.Remove(item.Key);
                    }
                }
            }

            // sprite pid → UnitySetSpriteEntry（解析 Sprite 拿 texture pid）
            var atlases = new Dictionary<Tuple<int, int, long>, Dictionary<string, UnitySpriteInfo>>();
            UnityEntryLocation ResolvePointer(UnitySerializedFile file, UnityEntryLocation location,
                UnityPPtr pointer, out UnitySerializedFile target)
            {
                target = file;
                if (pointer.PathId == 0)
                    return null;
                if (pointer.FileId != 0)
                    return resolveExternal(file, pointer, out target);
                if (ReferenceEquals(file, serializedFile))
                    return locationOf(pointer.PathId);
                var obj = file.Objects.FirstOrDefault(candidate => candidate.PathId == pointer.PathId);
                return obj == null ? null : new UnityEntryLocation(location.BundleIndex, location.NodeIndex,
                    file.DataOffset + obj.ByteStart, obj.ByteSize, obj.ClassId);
            }

            foreach (var asset in assetMap)
            {
                spriteNameSlots.TryGetValue(asset.Key, out var namedSlots);
                Dictionary<string, UnitySetSpriteEntry> output =
                    new Dictionary<string, UnitySetSpriteEntry>(StringComparer.Ordinal);
                foreach (var item in asset.Value)
                {
                    UnitySerializedFile spriteFile = serializedFile;
                    UnityEntryLocation spriteLocation = item.Value.FileId == 0
                        ? locationOf(item.Value.PathId) : resolveExternal(serializedFile, item.Value, out spriteFile);
                    if (spriteLocation == null || spriteLocation.ClassId != UnityClassIds.Sprite)
                    {
                        continue;
                    }
                    byte[] data;
                    try
                    {
                        using (var externalBundle = spriteLocation.BundleIndex == bundleIndex
                            ? null : openBundle(spriteLocation.BundleIndex))
                        {
                            data = (externalBundle ?? bundle).ReadNodeRange(
                                spriteLocation.NodeIndex, spriteLocation.AbsoluteOffset, (int)spriteLocation.ByteSize);
                        }
                    }
                    catch (UnityException)
                    {
                        continue;
                    }
                    UnitySpriteInfo sprite;
                    try
                    {
                        sprite = UnityObjectParsers.ParseSprite(data, spriteFile.IsLittleEndian);
                    }
                    catch (UnityException)
                    {
                        continue;
                    }
                    UnityEntryLocation textureLocation = ResolvePointer(spriteFile, spriteLocation, sprite.Texture, out _);
                    UnityRectF? atlasRect = null;
                    if (sprite.Texture.PathId == 0 && sprite.Atlas.PathId != 0)
                    {
                        var atlasLocation = ResolvePointer(spriteFile, spriteLocation, sprite.Atlas, out var atlasFile);
                        if (atlasLocation?.ClassId != UnityClassIds.SpriteAtlas)
                            continue;
                        var atlasKey = Tuple.Create(atlasLocation.BundleIndex, atlasLocation.NodeIndex,
                            atlasLocation.AbsoluteOffset);
                        if (!atlases.TryGetValue(atlasKey, out var atlas))
                        {
                            try
                            {
                                using var externalBundle = atlasLocation.BundleIndex == bundleIndex
                                    ? null : openBundle(atlasLocation.BundleIndex);
                                byte[] atlasData = (externalBundle ?? bundle).ReadNodeRange(atlasLocation.NodeIndex,
                                    atlasLocation.AbsoluteOffset, (int)atlasLocation.ByteSize);
                                atlas = UnityObjectParsers.ParseSpriteAtlas(atlasData, atlasFile.IsLittleEndian);
                            }
                            catch (UnityException)
                            {
                                // Keep indexing SpriteSets with supported render data.
                            }
                            atlases.Add(atlasKey, atlas);
                        }
                        if (atlas == null || !atlas.TryGetValue(sprite.RenderDataKey, out var frame))
                            continue;
                        textureLocation = ResolvePointer(atlasFile, atlasLocation, frame.Texture, out _);
                        atlasRect = frame.Rect;
                    }
                    if (textureLocation?.ClassId != UnityClassIds.Texture2D)
                    {
                        continue;
                    }
                    double x = Math.Round((double)sprite.PivotX * sprite.Rect.Width);
                    double y = Math.Round((1.0 - sprite.PivotY) * sprite.Rect.Height);
                    var entry = new UnitySetSpriteEntry(spriteLocation, textureLocation) { AtlasRect = atlasRect };
                    if (frameDelays.TryGetValue(asset.Key, out var setDelays)
                        && setDelays.TryGetValue(item.Key, out int delay))
                        entry.Delay = delay;
                    if (x >= int.MinValue && x <= int.MaxValue && y >= int.MinValue && y <= int.MaxValue)
                        entry.Origin = new WzComparerR2.WzLib.Wz_Vector((int)x, (int)y);

                    string name = item.Key;
                    if (namedSlots?.Contains(name) == true
                        && !string.IsNullOrWhiteSpace(sprite.Name)
                        && sprite.Name.IndexOf('/') < 0 && sprite.Name.IndexOf('\\') < 0
                        && !asset.Value.ContainsKey(sprite.Name) && !output.ContainsKey(sprite.Name))
                        name = sprite.Name;
                    output[name] = entry;
                }
                if (output.Count > 0)
                {
                    this.SetAssets[asset.Key] = output;
                }
            }
        }

        private UnitySheetEntry GetOrCreateSheet(string key, string locale)
        {
            Dictionary<string, UnitySheetEntry> variants;
            if (!this.SheetsByPath.TryGetValue(key, out variants))
            {
                variants = new Dictionary<string, UnitySheetEntry>(StringComparer.Ordinal);
                this.SheetsByPath[key] = variants;
            }
            string code = locale.ToUpperInvariant();
            UnitySheetEntry entry;
            if (!variants.TryGetValue(code, out entry))
            {
                entry = new UnitySheetEntry();
                variants[code] = entry;
            }
            return entry;
        }

        private static bool TrySplitSheetPath(string path, out string locale, out string rest)
        {
            locale = null;
            rest = null;
            if (path == null || path.Length <= SheetPrefix.Length)
            {
                return false;
            }
            if (string.Compare(path, 0, SheetPrefix, 0, SheetPrefix.Length, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }
            string tail = path.Substring(SheetPrefix.Length);
            int slash = tail.IndexOf('/');
            if (slash <= 0 || slash == tail.Length - 1)
            {
                return false;
            }
            locale = tail.Substring(0, slash);
            rest = tail.Substring(slash + 1);
            return true;
        }

        private static bool TryStripSuffix(string value, string suffix, out string result)
        {
            if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                result = value.Substring(0, value.Length - suffix.Length);
                return true;
            }
            result = null;
            return false;
        }

        /// <summary>"login_2.png" → ("login", 2)；無 _N 後綴 → false。</summary>
        private static bool TrySplitAtlasPage(string value, out string key, out int page)
        {
            key = null;
            page = 0;
            if (!TryStripSuffix(value, ".png", out string baseName))
            {
                return false;
            }
            int underscore = baseName.LastIndexOf('_');
            if (underscore <= 0 || underscore == baseName.Length - 1)
            {
                return false;
            }
            if (!int.TryParse(baseName.Substring(underscore + 1), System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out page) || page < 0)
            {
                return false;
            }
            key = baseName.Substring(0, underscore);
            return true;
        }

        internal static List<KeyValuePair<string, UnityPPtr>> ParseSetAssetPayload(
            byte[] payload, Func<UnityPPtr, UnityEntryLocation> locationOf, out Dictionary<string, int> delays,
            out HashSet<string> extraSlots)
        {
            delays = null;
            extraSlots = null;

            // Animated sets use an eight-byte settings header followed by the frame count.
            int frameCount = I32At(payload, 8);
            if (frameCount > 0 && payload.Length == 16 + 24L * frameCount)
            {
                var frames = new List<KeyValuePair<string, UnityPPtr>>(frameCount);
                var frameDelays = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int i = 0, offset = 12; i < frameCount; i++, offset += 24)
                {
                    var pointer = new UnityPPtr(I32At(payload, offset), I64At(payload, offset + 4));
                    if (pointer.PathId == 0
                        || locationOf(pointer)?.ClassId != UnityClassIds.Sprite)
                        break;

                    string name = i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    frames.Add(new KeyValuePair<string, UnityPPtr>(name, pointer));
                    double milliseconds = Math.Round(BitConverter.ToSingle(payload, offset + 12) * 1000.0);
                    if (milliseconds > 0 && milliseconds <= int.MaxValue)
                        frameDelays.Add(name, (int)milliseconds);
                }

                if (frames.Count == frameCount)
                {
                    delays = frameDelays;
                    return frames;
                }
            }

            // Two fixed Sprite slots followed by a flag.
            if (payload.Length == 28 && I32At(payload, 24) == 1)
            {
                var slots = ParseSpriteSlots(payload, 0, 2, locationOf);
                if (slots != null && slots.Count > 0)
                    return slots;
            }

            int count = I32At(payload, 0);

            // Number fonts store a digit count followed by digit and optional extra slots.
            int digitCount = I32At(payload, 4);
            if ((count == 0 || count == 1) && digitCount > 0
                && 8 + 12L * digitCount <= payload.Length)
            {
                var slots = ParseSpriteSlots(payload, 8, digitCount, locationOf);
                if (slots != null && slots.Count > 0)
                {
                    for (int i = digitCount, offset = 8 + 12 * digitCount;
                        offset <= payload.Length - 12; i++, offset += 12)
                    {
                        var pointer = new UnityPPtr(I32At(payload, offset), I64At(payload, offset + 4));
                        if (pointer.FileId == 0 && pointer.PathId == 0)
                            continue;
                        if (locationOf(pointer)?.ClassId != UnityClassIds.Sprite)
                            break;

                        string name = i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        slots.Add(new KeyValuePair<string, UnityPPtr>(name, pointer));
                        (extraSlots ??= new HashSet<string>(StringComparer.Ordinal)).Add(name);
                    }
                    return slots;
                }
            }

            // Some counted arrays end with an additional zero field.
            if (count > 0 && payload.Length == 8 + 12L * count
                && I32At(payload, payload.Length - 4) == 0)
            {
                var slots = ParseSpriteSlots(payload, 4, count, locationOf);
                if (slots != null && slots.Count > 0)
                    return slots;
            }

            if (count == 0)
            {
                // Four fixed Sprite slots followed by two zero fields.
                if (payload.Length == 56 && I64At(payload, 48) == 0)
                {
                    var slots = ParseSpriteSlots(payload, 0, 4, locationOf);
                    if (slots != null)
                        return slots;
                }

                // Three zero fields, a counted Sprite array, and a 12-byte footer.
                int slotCount = I32At(payload, 12);
                if (slotCount > 0 && payload.Length == 28 + 12L * slotCount
                    && I64At(payload, 4) == 0
                    && I64At(payload, payload.Length - 12) == 0
                    && I32At(payload, payload.Length - 4) == 1)
                {
                    var slots = ParseSpriteSlots(payload, 16, slotCount, locationOf);
                    if (slots != null)
                        return slots;
                }

                return ParseBarePPtrs(payload);
            }

            var output = new List<KeyValuePair<string, UnityPPtr>>();
            if (count < 0 || count > 100000)
                return output;

            if (payload.Length == 4 + 12L * count)
            {
                var slots = ParseSpriteSlots(payload, 4, count, locationOf);
                if (slots != null && slots.Count > 0)
                    return slots;
            }

            var managed = ParseManagedSpriteFrames(payload, count);
            if (managed != null)
                return managed;

            var withFileId = ParseNamedEntries(payload, count, true, locationOf, out var endsAtPayloadBoundary);
            if (withFileId.Count == count && endsAtPayloadBoundary)
                return withFileId;

            int nameLength = I32At(payload, 4);
            if (nameLength > 0 && nameLength <= payload.Length - 8)
            {
                if (withFileId.Count == count)
                    return withFileId;

                var withoutFileId = ParseNamedEntries(payload, count, false, locationOf, out _);
                return withoutFileId.Count > withFileId.Count ? withoutFileId : withFileId;
            }

            for (int i = 0, offset = 4; i < count && offset <= payload.Length - 12; i++, offset += 12)
            {
                var pointer = new UnityPPtr(I32At(payload, offset), I64At(payload, offset + 4));
                if (pointer.FileId != 0 && locationOf(pointer)?.ClassId != UnityClassIds.Sprite)
                    break;

                output.Add(new KeyValuePair<string, UnityPPtr>(
                    i.ToString(System.Globalization.CultureInfo.InvariantCulture), pointer));
            }
            return output;
        }

        private static List<KeyValuePair<string, UnityPPtr>> ParseSpriteSlots(
            byte[] payload, int offset, int count, Func<UnityPPtr, UnityEntryLocation> locationOf)
        {
            var slots = new List<KeyValuePair<string, UnityPPtr>>(count);
            for (int i = 0; i < count; i++, offset += 12)
            {
                var pointer = new UnityPPtr(I32At(payload, offset), I64At(payload, offset + 4));
                if (pointer.PathId == 0)
                {
                    if (pointer.FileId != 0)
                        return null;
                    continue;
                }
                if (locationOf(pointer)?.ClassId != UnityClassIds.Sprite)
                    return null;

                slots.Add(new KeyValuePair<string, UnityPPtr>(
                    i.ToString(System.Globalization.CultureInfo.InvariantCulture), pointer));
            }
            return slots;
        }

        private static List<KeyValuePair<string, UnityPPtr>> ParseManagedSpriteFrames(byte[] payload, int count)
        {
            // Each name has a length and a 64-bit managed reference ID.
            if (count > (payload.Length - 12) / 12)
                return null;

            var reader = new UnityByteReader(payload, 4, payload.Length - 4, true);
            try
            {
                var slots = new List<KeyValuePair<string, long>>();
                for (int i = 0; i < count; i++)
                {
                    string name = reader.AlignedString();
                    if (Array.Exists(name.ToCharArray(), char.IsControl) || name.IndexOf('\uFFFD') >= 0)
                        return null;
                    if (name.Length == 0)
                        name = i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    slots.Add(new KeyValuePair<string, long>(name, reader.I64()));
                }

                if (reader.I32() != 2)
                    return null;
                int frameCount = reader.I32();
                if (frameCount < 0 || frameCount > reader.Remaining / 32)
                    return null;

                var frames = new Dictionary<long, UnityPPtr>();
                for (int i = 0; i < frameCount; i++)
                {
                    long id = reader.I64();
                    if (id < 0 || frames.ContainsKey(id)
                        || reader.AlignedString() != "WzSpriteFrame"
                        || reader.AlignedString() != "Msc.Core.WzSprite"
                        || reader.AlignedString() != "Assembly-CSharp")
                        return null;
                    var pointer = new UnityPPtr(reader.I32(), reader.I64());
                    if (pointer.FileId < 0)
                        return null;
                    frames.Add(id, pointer);
                }
                if (reader.Remaining != 0)
                    return null;

                var output = new List<KeyValuePair<string, UnityPPtr>>();
                foreach (var slot in slots)
                {
                    if (slot.Value == -2)
                        continue;
                    if (!frames.TryGetValue(slot.Value, out var pointer))
                        return null;
                    if (pointer.PathId != 0)
                        output.Add(new KeyValuePair<string, UnityPPtr>(slot.Key, pointer));
                }
                return output;
            }
            catch (Exception ex) when (ex is UnityException || ex is OverflowException)
            {
                return null;
            }
        }

        private static List<KeyValuePair<string, UnityPPtr>> ParseNamedEntries(
            byte[] payload, int count, bool includeFileId, Func<UnityPPtr, UnityEntryLocation> locationOf, out bool endsAtPayloadBoundary)
        {
            var entries = new List<KeyValuePair<string, UnityPPtr>>();
            var reader = new UnityByteReader(payload, 4, payload.Length - 4, true);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    string name = reader.AlignedString();
                    // Binary PPtr data can also fit the length and alignment checks.
                    if (Array.Exists(name.ToCharArray(), char.IsControl) || name.IndexOf('\uFFFD') >= 0)
                        break;
                    int fileId = includeFileId ? reader.I32() : 0;
                    if (fileId < 0)
                        break;

                    var pointer = new UnityPPtr(fileId, reader.I64());
                    if (pointer.PathId == 0 || (fileId != 0 && locationOf(pointer)?.ClassId != UnityClassIds.Sprite))
                        break;

                    if (name.Length == 0)
                        name = i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    entries.Add(new KeyValuePair<string, UnityPPtr>(name, pointer));
                }
            }
            catch (Exception ex) when (ex is UnityException || ex is OverflowException)
            {
                // The other named layout may match more entries.
            }

            endsAtPayloadBoundary = reader.Remaining == 0;
            return entries;
        }

        /// <summary>
        /// 無 count 前綴的 PPtr 連排：(fileID i32, pathID i64)×n，
        /// fileID 非 0 或 pathID 為 0 時結束。
        /// </summary>
        private static List<KeyValuePair<string, UnityPPtr>> ParseBarePPtrs(byte[] payload)
        {
            List<KeyValuePair<string, UnityPPtr>> output = new List<KeyValuePair<string, UnityPPtr>>();
            int position = 0;
            while (position <= payload.Length - 12)
            {
                int fileId = I32At(payload, position);
                long pathId = I64At(payload, position + 4);
                if (fileId != 0 || pathId == 0)
                {
                    break;
                }
                output.Add(new KeyValuePair<string, UnityPPtr>(
                    output.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), new UnityPPtr(fileId, pathId)));
                position += 12;
            }
            return output;
        }

        private static uint U32At(byte[] data, int position)
        {
            if (position < 0 || position > data.Length - 4)
            {
                return 0;
            }
            return (uint)(data[position]
                | (data[position + 1] << 8)
                | (data[position + 2] << 16)
                | (data[position + 3] << 24));
        }

        private static int I32At(byte[] data, int position)
        {
            return unchecked((int)U32At(data, position));
        }

        private static long I64At(byte[] data, int position)
        {
            if (position < 0 || position > data.Length - 8)
            {
                return 0;
            }
            return data[position]
                | ((long)data[position + 1] << 8)
                | ((long)data[position + 2] << 16)
                | ((long)data[position + 3] << 24)
                | ((long)data[position + 4] << 32)
                | ((long)data[position + 5] << 40)
                | ((long)data[position + 6] << 48)
                | ((long)data[position + 7] << 56);
        }
    }
}
