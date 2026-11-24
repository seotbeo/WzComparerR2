using System;
using System.Collections.Generic;

namespace WzComparerR2.UnityLib.Unity
{
    /// <summary>
    /// Unity classID 常數。
    /// </summary>
    public static class UnityClassIds
    {
        public const int Texture2D = 28;
        public const int TextAsset = 49;
        public const int AudioClip = 83;
        public const int MonoBehaviour = 114;
        public const int MonoScript = 115;
        public const int AssetBundle = 142;
        public const int Sprite = 213;
        public const int SpriteAtlas = 687078895;
    }

    /// <summary>
    /// Unity 6000.3.16f1 硬編物件佈局。
    /// <para>
    /// 欄位順序依據 AssetRipper/TypeTreeDumps 的
    /// <c>StructsDump/release/6000.3.16f1.dump</c>（官方 typetree dump）。
    /// 注意 6000.3 的 Texture2D 沒有 <c>m_ForcedFallbackFormat</c> /
    /// <c>m_DownscaleFallback</c>，且 <c>m_IsAlphaChannelOptional</c> 緊接在
    /// <c>m_Name</c> 之後。
    /// </para>
    /// <para>
    /// 這些 parse 函式都吃「單一物件的完整 bytes」（byte_size 長度）。
    /// </para>
    /// </summary>
    public static class UnityObjectParsers
    {
        // ── TextAsset(49) ──────────────────────────────────────────────

        public static UnityTextAsset ParseTextAsset(byte[] data, bool littleEndian)
        {
            UnityByteReader reader = new UnityByteReader(data, littleEndian);
            string name = reader.AlignedString();
            int length = checked((int)reader.U32());
            if (length < 0 || length > reader.Remaining)
            {
                throw new UnityException(string.Format(
                    "TextAsset {0} m_Script 길이 오류: {1}", name, length));
            }
            return new UnityTextAsset(name, reader.Take(length));
        }

        // ── AudioClip(83) ──────────────────────────────────────────────

        /// <summary>
        /// m_Name → 4×i32 → f32 → bool+align → i32 → 3×bool+align →
        /// StreamedResource(m_Source 對齊字串, m_Offset u64, m_Size u64)。
        /// </summary>
        public static UnityAudioClipInfo ParseAudioClip(byte[] data, bool littleEndian)
        {
            UnityByteReader reader = new UnityByteReader(data, littleEndian);
            string name = reader.AlignedString();
            reader.I32(); // m_LoadType
            int channels = reader.I32();
            int frequency = reader.I32();
            reader.I32(); // m_BitsPerSample
            float length = reader.F32();
            reader.Bool(); // m_IsTrackerFormat
            reader.Align4();
            reader.I32(); // m_SubsoundIndex
            reader.Bool(); // m_PreloadAudioData
            reader.Bool(); // m_LoadInBackground
            reader.Bool(); // m_Legacy3D
            reader.Align4();
            string source = reader.AlignedString();
            ulong resourceOffset = reader.U64();
            ulong resourceSize = reader.U64();
            if (channels <= 0 || channels > 8 || frequency <= 0)
            {
                throw new UnityException(string.Format(
                    "AudioClip {0} 오류: channels={1} frequency={2}", name, channels, frequency));
            }
            string resourceNode = string.Empty;
            if (!string.IsNullOrEmpty(source))
            {
                string[] parts = source.Split('/');
                resourceNode = parts[parts.Length - 1];
            }
            if (resourceNode.Length == 0)
            {
                throw new UnityException("AudioClip " + name + " m_Source 為空");
            }
            return new UnityAudioClipInfo(
                name, channels, frequency, length, resourceNode, resourceOffset, resourceSize);
        }

        // ── MonoScript(115) ────────────────────────────────────────────

        public static UnityMonoScript ParseMonoScript(byte[] data, bool littleEndian)
        {
            UnityByteReader reader = new UnityByteReader(data, littleEndian);
            string name = reader.AlignedString();
            reader.I32(); // m_ExecutionOrder
            reader.Skip(16); // m_PropertiesHash
            string className = reader.AlignedString();
            string @namespace = reader.AlignedString();
            string assemblyName = reader.AlignedString();
            return new UnityMonoScript(name, className, @namespace, assemblyName);
        }

        // ── AssetBundle(142) ───────────────────────────────────────────

        /// <summary>只解析到 m_Container 為止（後面 m_MainAsset / m_SceneHashes 等用不到）。</summary>
        public static UnityAssetBundleInfo ParseAssetBundle(byte[] data, bool littleEndian)
        {
            UnityByteReader reader = new UnityByteReader(data, littleEndian);
            string name = reader.AlignedString();

            int preloadCount = reader.I32();
            if (preloadCount < 0 || preloadCount > 1000000 || preloadCount > reader.Remaining / 12)
            {
                throw new UnityException("m_PreloadTable 개수 오류: " + preloadCount);
            }
            for (int i = 0; i < preloadCount; i++)
            {
                ReadPPtr(reader);
            }

            int containerCount = reader.I32();
            if (containerCount < 0 || containerCount > 1000000 || containerCount > reader.Remaining / 24)
            {
                throw new UnityException("m_Container 개수 오류: " + containerCount);
            }
            List<UnityContainerEntry> container = new List<UnityContainerEntry>(containerCount);
            for (int i = 0; i < containerCount; i++)
            {
                string path = reader.AlignedString();
                int preloadIndex = reader.I32();
                int preloadSize = reader.I32();
                UnityPPtr asset = ReadPPtr(reader);
                container.Add(new UnityContainerEntry(path, preloadIndex, preloadSize, asset));
            }
            return new UnityAssetBundleInfo(name, container);
        }

        // ── Texture2D(28) ──────────────────────────────────────────────

        public static UnityTexture2DInfo ParseTexture2D(byte[] data, bool littleEndian)
        {
            UnityByteReader reader = new UnityByteReader(data, littleEndian);
            string name = reader.AlignedString();
            reader.Bool(); // m_IsAlphaChannelOptional（MetaFlag align）
            reader.Align4();
            int width = reader.I32();
            int height = reader.I32();
            if (width <= 0 || height <= 0 || width > 32768 || height > 32768)
            {
                throw new UnityException(string.Format(
                    "Texture2D {0} 크기 오류: {1}x{2}", name, width, height));
            }
            uint completeImageSize = reader.U32();
            int mipsStripped = reader.I32();
            int textureFormat = reader.I32();
            int mipCount = reader.I32();
            reader.Bool(); // m_IsReadable
            reader.Bool(); // m_IsPreProcessed
            reader.Bool(); // m_IgnoreMipmapLimit（MetaFlag align）
            reader.Align4();
            reader.AlignedString(); // m_MipmapLimitGroupName
            reader.Bool(); // m_StreamingMipmaps（MetaFlag align）
            reader.Align4();
            reader.I32(); // m_StreamingMipmapsPriority
            reader.I32(); // m_ImageCount
            int textureDimension = reader.I32();

            // GLTextureSettings
            reader.I32(); // m_FilterMode
            reader.I32(); // m_Aniso
            reader.F32(); // m_MipBias
            reader.I32(); // m_WrapU
            reader.I32(); // m_WrapV
            reader.I32(); // m_WrapW
            reader.I32(); // m_LightmapFormat
            reader.I32(); // m_ColorSpace
            reader.ByteVector(); // m_PlatformBlob

            // image data（TypelessData：u32 size + raw bytes）
            uint imageDataSize = reader.U32();
            int imageDataOffset = reader.Position;
            if (imageDataSize > reader.Remaining)
            {
                throw new UnityException(string.Format(
                    "Texture2D {0} image data 길이 오류: {1}（남은 {2}）",
                    name, imageDataSize, reader.Remaining));
            }
            reader.Skip((int)imageDataSize);
            reader.Align4(); // MetaFlag 0x4001：align after

            // m_StreamData
            ulong streamOffset = reader.U64();
            uint streamSize = reader.U32();
            string streamPath = reader.AlignedString();

            return new UnityTexture2DInfo(
                name, width, height, completeImageSize, mipsStripped, textureFormat, mipCount,
                textureDimension, imageDataSize, imageDataOffset, streamOffset, streamSize, streamPath);
        }

        // ── Sprite(213) ────────────────────────────────────────────────

        public static UnitySpriteInfo ParseSprite(byte[] data, bool littleEndian)
        {
            UnityByteReader reader = new UnityByteReader(data, littleEndian);
            string name = reader.AlignedString();
            float rectX = reader.F32();
            float rectY = reader.F32();
            float rectWidth = reader.F32();
            float rectHeight = reader.F32();
            reader.Skip(8); // m_Offset Vector2f
            reader.Skip(16); // m_Border Vector4f
            reader.Skip(4);
            float pivotX = reader.F32();
            float pivotY = reader.F32();
            reader.Skip(4); // m_PixelsToUnits + m_Pivot + m_Extrude
            reader.U8(); // m_IsPolygon
            reader.Align4();
            string renderDataKey = Convert.ToBase64String(reader.Take(24));
            int tagCount = reader.I32();
            if (tagCount < 0 || tagCount > 4096)
            {
                throw new UnityException(string.Format(
                    "Sprite {0} m_AtlasTags 개수오류: {1}", name, tagCount));
            }
            for (int i = 0; i < tagCount; i++)
            {
                reader.AlignedString();
            }
            UnityPPtr atlas = ReadPPtr(reader);
            UnityPPtr texture = ReadPPtr(reader); // m_RD.texture
            return new UnitySpriteInfo(name, new UnityRectF(rectX, rectY, rectWidth, rectHeight),
                texture, pivotX, pivotY) { Atlas = atlas, RenderDataKey = renderDataKey };
        }

        internal static Dictionary<string, UnitySpriteInfo> ParseSpriteAtlas(byte[] data, bool littleEndian)
        {
            var reader = new UnityByteReader(data, littleEndian);
            reader.AlignedString(); // m_Name
            int spriteCount = reader.I32();
            if (spriteCount < 0 || spriteCount > reader.Remaining / 12)
                throw new UnityException("Invalid SpriteAtlas sprite count.");
            reader.Skip(spriteCount * 12);
            int nameCount = reader.I32();
            if (nameCount < 0 || nameCount > reader.Remaining / 4)
                throw new UnityException("Invalid SpriteAtlas name count.");
            for (int i = 0; i < nameCount; i++)
                reader.AlignedString();

            int count = reader.I32();
            if (count < 0 || count > reader.Remaining / 108)
                throw new UnityException("Invalid SpriteAtlas render data count.");
            var entries = new Dictionary<string, UnitySpriteInfo>(count, StringComparer.Ordinal);
            for (int i = 0; i < count; i++)
            {
                string key = Convert.ToBase64String(reader.Take(24));
                UnityPPtr texture = ReadPPtr(reader);
                UnityPPtr alphaTexture = ReadPPtr(reader);
                var rect = new UnityRectF(reader.F32(), reader.F32(), reader.F32(), reader.F32());
                reader.Skip(36); // textureRectOffset, atlasRectOffset, uvTransform, downscaleMultiplier
                uint settings = reader.U32();
                int secondaryCount = reader.I32();
                if (secondaryCount != 0)
                    throw new UnityException("Unsupported SpriteAtlas secondary texture layout.");
                int rotation = (int)((settings >> 2) & 0xf);
                if (texture.PathId != 0 && alphaTexture.PathId == 0 && rotation == 0)
                    entries[key] = new UnitySpriteInfo(null, rect, texture, 0, 0);
            }
            return entries;
        }

        // ── MonoBehaviour(114) ─────────────────────────────────────────

        /// <summary>
        /// 只解析共用 header 欄位；m_Name 之後的全部 bytes 是自訂 payload
        /// （WZJS v5 素材），原樣保留給上層解碼。
        /// </summary>
        public static UnityMonoBehaviourInfo ParseMonoBehaviour(byte[] data, bool littleEndian)
        {
            UnityByteReader reader = new UnityByteReader(data, littleEndian);
            UnityPPtr gameObject = ReadPPtr(reader);
            bool enabled = reader.Bool();
            reader.Align4();
            UnityPPtr script = ReadPPtr(reader);
            string name = reader.AlignedString();
            return new UnityMonoBehaviourInfo(gameObject, enabled, script, name, reader.Position);
        }

        private static UnityPPtr ReadPPtr(UnityByteReader reader)
        {
            return new UnityPPtr(reader.I32(), reader.I64());
        }
    }

    public struct UnityPPtr
    {
        public UnityPPtr(int fileId, long pathId)
        {
            this.FileId = fileId;
            this.PathId = pathId;
        }

        public int FileId { get; private set; }
        public long PathId { get; private set; }

        public override string ToString()
        {
            return string.Format("PPtr({0}, {1})", this.FileId, this.PathId);
        }
    }

    public struct UnityRectF
    {
        public UnityRectF(float x, float y, float width, float height)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
        }

        public float X { get; private set; }
        public float Y { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
    }

    public sealed class UnityTextAsset
    {
        internal UnityTextAsset(string name, byte[] script)
        {
            this.Name = name;
            this.Script = script;
        }

        public string Name { get; private set; }

        /// <summary>m_Script 原文（這批 bundle 是 UTF-8 JSON 明文）。</summary>
        public byte[] Script { get; private set; }
    }

    public sealed class UnityAudioClipInfo
    {
        internal UnityAudioClipInfo(
            string name, int channels, int frequency, float length,
            string resourceNode, ulong resourceOffset, ulong resourceSize)
        {
            this.Name = name;
            this.Channels = channels;
            this.Frequency = frequency;
            this.Length = length;
            this.ResourceNode = resourceNode;
            this.ResourceOffset = resourceOffset;
            this.ResourceSize = resourceSize;
        }

        public string Name { get; private set; }
        public int Channels { get; private set; }
        public int Frequency { get; private set; }

        /// <summary>長度（秒）。</summary>
        public float Length { get; private set; }

        /// <summary>m_Resource 指向的 resource node 名稱（m_Source 最後一段）。</summary>
        public string ResourceNode { get; private set; }

        /// <summary>聲音資料在 resource node 內的 offset。</summary>
        public ulong ResourceOffset { get; private set; }

        public ulong ResourceSize { get; private set; }
    }

    public sealed class UnityMonoScript
    {
        internal UnityMonoScript(string name, string className, string @namespace, string assemblyName)
        {
            this.Name = name;
            this.ClassName = className;
            this.Namespace = @namespace;
            this.AssemblyName = assemblyName;
        }

        public string Name { get; private set; }
        public string ClassName { get; private set; }
        public string Namespace { get; private set; }
        public string AssemblyName { get; private set; }
    }

    public sealed class UnityContainerEntry
    {
        internal UnityContainerEntry(string path, int preloadIndex, int preloadSize, UnityPPtr asset)
        {
            this.Path = path;
            this.PreloadIndex = preloadIndex;
            this.PreloadSize = preloadSize;
            this.Asset = asset;
        }

        /// <summary>資產邏輯路徑（例如 <c>assets/.../blank.json</c>）。</summary>
        public string Path { get; private set; }

        public int PreloadIndex { get; private set; }
        public int PreloadSize { get; private set; }
        public UnityPPtr Asset { get; private set; }
    }

    public sealed class UnityAssetBundleInfo
    {
        internal UnityAssetBundleInfo(string name, IReadOnlyList<UnityContainerEntry> container)
        {
            this.Name = name;
            this.Container = container;
        }

        public string Name { get; private set; }
        public IReadOnlyList<UnityContainerEntry> Container { get; private set; }
    }

    public sealed class UnityTexture2DInfo
    {
        internal UnityTexture2DInfo(
            string name, int width, int height, uint completeImageSize, int mipsStripped,
            int textureFormat, int mipCount, int textureDimension, uint imageDataSize,
            int imageDataOffset, ulong streamOffset, uint streamSize, string streamPath)
        {
            this.Name = name;
            this.Width = width;
            this.Height = height;
            this.CompleteImageSize = completeImageSize;
            this.MipsStripped = mipsStripped;
            this.TextureFormat = textureFormat;
            this.MipCount = mipCount;
            this.TextureDimension = textureDimension;
            this.ImageDataSize = imageDataSize;
            this.ImageDataOffset = imageDataOffset;
            this.StreamOffset = streamOffset;
            this.StreamSize = streamSize;
            this.StreamPath = streamPath;
        }

        public string Name { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public uint CompleteImageSize { get; private set; }
        public int MipsStripped { get; private set; }
        public int TextureFormat { get; private set; }
        public int MipCount { get; private set; }
        public int TextureDimension { get; private set; }

        /// <summary>內嵌 image data 的大小（streamed 時為 0）。</summary>
        public uint ImageDataSize { get; private set; }

        /// <summary>內嵌 image data 在 Texture2D 物件內的精確起點。</summary>
        public int ImageDataOffset { get; private set; }

        /// <summary>m_StreamData：像素在 .resS node 的 (offset, size, path)。</summary>
        public ulong StreamOffset { get; private set; }

        public uint StreamSize { get; private set; }
        public string StreamPath { get; private set; }
    }

    public sealed class UnitySpriteInfo
    {
        internal UnitySpriteInfo(string name, UnityRectF rect, UnityPPtr texture, float pivotX, float pivotY)
        {
            this.Name = name;
            this.Rect = rect;
            this.Texture = texture;
            this.PivotX = pivotX;
            this.PivotY = pivotY;
        }

        public string Name { get; private set; }

        /// <summary>m_Rect: (x, y, width, height)。</summary>
        public UnityRectF Rect { get; private set; }

        /// <summary>m_RD.texture（像素所在的 Texture2D）。</summary>
        public UnityPPtr Texture { get; private set; }
        internal UnityPPtr Atlas { get; set; }
        internal string RenderDataKey { get; set; }
        public float PivotX { get; }
        public float PivotY { get; }
    }

    public sealed class UnityMonoBehaviourInfo
    {
        internal UnityMonoBehaviourInfo(UnityPPtr gameObject, bool enabled, UnityPPtr script, string name, int headerLength)
        {
            this.GameObject = gameObject;
            this.Enabled = enabled;
            this.Script = script;
            this.Name = name;
            this.HeaderLength = headerLength;
        }

        public UnityPPtr GameObject { get; private set; }
        public bool Enabled { get; private set; }
        public UnityPPtr Script { get; private set; }
        public string Name { get; private set; }

        /// <summary>header 佔用的 bytes 數；payload = <c>data[HeaderLength..]</c>。</summary>
        public int HeaderLength { get; private set; }
    }
}
