using System;
using System.Collections.Generic;

namespace WzComparerR2.UnityLib.Unity
{
    /// <summary>
    /// SerializedFile v22 解析（header / types / objects / scriptTypes / externals）。
    /// <para>
    /// v22 header 佈局（擴充欄位一律 big-endian）：
    /// [0..4) legacy metadataSize u32；[4..8) legacy fileSize u32；[8..12) version u32 = 22；
    /// [12..16) legacy dataOffset u32；[16] endianness u8（0 = little）+ [17..20) reserved；
    /// [20..24) metadataSize u32；[24..32) fileSize i64；[32..40) dataOffset i64；
    /// [40..48) unknown i64。
    /// </para>
    /// <para>
    /// 之後的 metadata 本體改用 endianness byte 指定的位元序：
    /// unityVersion cstr | targetPlatform i32 | enableTypeTree u8 | types | objects |
    /// scriptTypes | externals。
    /// </para>
    /// <para>
    /// 這批 bundle 實測 enableTypeTree = false，因此 type 只有 classID 等基本欄位，
    /// 物件欄位全靠 <see cref="UnityObjectParsers"/> 的硬編佈局解析。
    /// </para>
    /// </summary>
    public sealed class UnitySerializedFile
    {
        public const uint SupportedVersion = 22;
        public const int HeaderSize = 48;

        private const int MaxMetadataSize = 256 * 1024 * 1024;
        private const int MaxTableEntries = 1000000;

        private readonly List<UnitySerializedType> types = new List<UnitySerializedType>();
        private readonly List<UnityObjectInfo> objects = new List<UnityObjectInfo>();
        private readonly List<UnityScriptType> scriptTypes = new List<UnityScriptType>();
        private readonly List<UnityExternal> externals = new List<UnityExternal>();

        private UnitySerializedFile()
        {
        }

        public string UnityVersion { get; private set; }

        public int TargetPlatform { get; private set; }

        public bool IsLittleEndian { get; private set; }

        /// <summary>物件資料在 node 內的起點（物件絕對位置 = DataOffset + ByteStart）。</summary>
        public long DataOffset { get; private set; }

        public IReadOnlyList<UnitySerializedType> Types
        {
            get { return this.types; }
        }

        public IReadOnlyList<UnityObjectInfo> Objects
        {
            get { return this.objects; }
        }

        public IReadOnlyList<UnityScriptType> ScriptTypes
        {
            get { return this.scriptTypes; }
        }

        public IReadOnlyList<UnityExternal> Externals
        {
            get { return this.externals; }
        }

        /// <summary>
        /// 讀 node 開頭 48 bytes，回傳 header + metadata 的總長度，
        /// 呼叫端據此決定要餵給 <see cref="Parse"/> 多少 bytes。
        /// </summary>
        public static int GetMetadataLength(byte[] head)
        {
            if (head == null)
            {
                throw new ArgumentNullException(nameof(head));
            }
            if (head.Length < HeaderSize)
            {
                throw new UnityException(string.Format(
                    "SerializedFile header is too short: {0} bytes（expected {1}）", head.Length, HeaderSize));
            }
            UnityByteReader reader = new UnityByteReader(head, false);
            reader.Skip(8);
            uint version = reader.U32();
            if (version != SupportedVersion)
            {
                throw new UnityException("Not supported SerializedFile（version = " + version + "）");
            }
            reader.Skip(8); // legacy dataOffset + endianness + reserved
            uint metadataSize = reader.U32();
            if (metadataSize > MaxMetadataSize)
            {
                throw new UnityException(string.Format(
                    "metadataSize exceeds max size: {0} > {1}", metadataSize, MaxMetadataSize));
            }
            return checked(HeaderSize + (int)metadataSize);
        }

        /// <summary>
        /// 從 node 開頭的 bytes 解析。資料至少要涵蓋 header + 全部 metadata
        /// （呼叫端先用 <see cref="GetMetadataLength"/> 決定要讀多少）。
        /// </summary>
        public static UnitySerializedFile Parse(byte[] head)
        {
            if (head == null)
            {
                throw new ArgumentNullException(nameof(head));
            }
            if (head.Length < HeaderSize)
            {
                throw new UnityException(string.Format(
                    "SerializedFile header is too short: {0} bytes（expected {1}）", head.Length, HeaderSize));
            }

            UnityByteReader headerReader = new UnityByteReader(head, 0, HeaderSize, false);
            headerReader.U32(); // legacy metadataSize
            headerReader.U32(); // legacy fileSize
            uint version = headerReader.U32();
            if (version != SupportedVersion)
            {
                throw new UnityException("Not supported SerializedFile（version = " + version + "）");
            }
            headerReader.U32(); // legacy dataOffset
            byte endianness = headerReader.U8();
            if (endianness > 1)
            {
                throw new UnityException("endianness error：" + endianness);
            }
            headerReader.Skip(3); // reserved
            uint metadataSize = headerReader.U32();
            if (metadataSize > MaxMetadataSize || metadataSize > head.Length - HeaderSize)
            {
                throw new UnityException(string.Format(
                    "metadataSize error: {0}（可用 {1} bytes）", metadataSize, head.Length - HeaderSize));
            }
            long fileSize = headerReader.I64();
            if (fileSize < 0)
            {
                throw new UnityException("fileSize cannot be negative：" + fileSize);
            }
            long dataOffset = headerReader.I64();
            if (dataOffset < 0)
            {
                throw new UnityException("dataOffset cannot be negative：" + dataOffset);
            }
            if (fileSize != 0 && dataOffset > fileSize)
            {
                throw new UnityException(string.Format(
                    "dataOffset {0} exceeds fileSize {1}", dataOffset, fileSize));
            }
            headerReader.I64(); // unknown

            UnitySerializedFile file = new UnitySerializedFile();
            file.IsLittleEndian = endianness == 0;
            file.DataOffset = dataOffset;

            UnityByteReader reader = new UnityByteReader(head, HeaderSize, (int)metadataSize, file.IsLittleEndian);
            file.UnityVersion = reader.CString();
            file.TargetPlatform = reader.I32();
            bool enableTypeTree = reader.Bool();

            int typeCount = ReadTableCount(reader, "types", 23);
            for (int i = 0; i < typeCount; i++)
            {
                int classId = reader.I32();
                bool isStripped = reader.Bool();       // v16+
                short scriptTypeIndex = reader.I16();  // v17+
                if (classId == 114)
                {
                    reader.Skip(16); // scriptID hash（MonoBehaviour）
                }
                reader.Skip(16); // oldTypeHash
                if (enableTypeTree)
                {
                    // 舊版用戶端（如最早期的 Classic beta）保留完整 type tree；
                    // 內容本專案用不到，只需正確跳過。
                    SkipTypeTree(reader, version);
                }
                file.types.Add(new UnitySerializedType(classId, isStripped, scriptTypeIndex));
            }

            int objectCount = ReadTableCount(reader, "objects", 24);
            for (int i = 0; i < objectCount; i++)
            {
                reader.Align4(); // v14+
                long pathId = reader.I64();
                long byteStart = reader.I64(); // v22+ 是 i64
                if (byteStart < 0)
                {
                    throw new UnityException(string.Format(
                        "object {0} byteStart cannot be negative: {1}", pathId, byteStart));
                }
                uint byteSize = reader.U32();
                int typeIndex = reader.I32(); // v16+ 是 types 表 index
                if (typeIndex < 0 || typeIndex >= file.Types.Count)
                {
                    throw new UnityException("object type index out of range：" + typeIndex);
                }
                UnitySerializedType type = file.types[typeIndex];
                if (fileSize != 0)
                {
                    long objectEnd = dataOffset + byteStart + byteSize;
                    if (objectEnd < dataOffset || objectEnd > fileSize)
                    {
                        throw new UnityException(string.Format(
                            "object {0} 結尾 {1} 超過 fileSize {2}", pathId, objectEnd, fileSize));
                    }
                }
                file.objects.Add(new UnityObjectInfo(
                    pathId, byteStart, byteSize, typeIndex, type.ClassId));
            }

            int scriptCount = ReadTableCount(reader, "scriptTypes", 12);
            for (int i = 0; i < scriptCount; i++)
            {
                int localSerializedFileIndex = reader.I32();
                reader.Align4();
                long localIdentifierInFile = reader.I64();
                file.scriptTypes.Add(new UnityScriptType(localSerializedFileIndex, localIdentifierInFile));
            }

            int externalCount = ReadTableCount(reader, "externals", 22);
            for (int i = 0; i < externalCount; i++)
            {
                reader.CString(); // tempEmpty（v6+）
                byte[] guid = reader.Take(16);
                int type = reader.I32();
                string pathName = reader.CString();
                file.externals.Add(new UnityExternal(guid, type, pathName));
            }
            // 之後可能還有 userInformation cstring，本專案用不到，不解析。
            return file;
        }

        /// <summary>依 classID 過濾物件。</summary>
        public IEnumerable<UnityObjectInfo> ObjectsByClass(int classId)
        {
            foreach (UnityObjectInfo obj in this.Objects)
            {
                if (obj.ClassId == classId)
                {
                    yield return obj;
                }
            }
        }

        private static int ReadTableCount(UnityByteReader reader, string label, int minEntrySize)        {
            int count = reader.I32();
            if (count < 0 || count > MaxTableEntries)
            {
                throw new UnityException(string.Format("{0} 개수 오류: {1}", label, count));
            }
            if (minEntrySize != 0 && count > reader.Remaining / minEntrySize)
            {
                throw new UnityException(string.Format(
                    "{0} 數量超出 metadata 剩餘空間: {1}（剩餘 {2} bytes）",
                    label, count, reader.Remaining));
            }
            return count;
        }

        /// <summary>
        /// 跳過一個 type tree blob（SerializedFile v13+ 的佈局）：
        /// nodeCount u32 | stringBufferSize u32 | nodes[nodeCount × 32] | stringBuffer |
        /// (v21+) refTypes[refCount × 12]。
        /// </summary>
        private static void SkipTypeTree(UnityByteReader reader, uint version)
        {
            const int TypeTreeNodeSize = 32;
            const int MaxTypeTreeNodes = 200000;

            int nodeCount = reader.I32();
            if (nodeCount < 0 || nodeCount > MaxTypeTreeNodes)
            {
                throw new UnityException("type tree node 개수 오류: " + nodeCount);
            }
            int stringBufferSize = reader.I32();
            if (stringBufferSize < 0)
            {
                throw new UnityException("type tree 文字列バッファーサイズが異常です: " + stringBufferSize);
            }
            long blobSize = (long)nodeCount * TypeTreeNodeSize + stringBufferSize;
            if (blobSize > reader.Remaining)
            {
                throw new UnityException(string.Format(
                    "type tree が metadata の残り領域を超えています: {0} > {1}", blobSize, reader.Remaining));
            }
            reader.Skip((int)blobSize);

            if (version >= 21)
            {
                int refCount = reader.I32();
                if (refCount < 0 || refCount > MaxTableEntries)
                {
                    throw new UnityException("type tree refType 数が異常です: " + refCount);
                }
                long refBytes = (long)refCount * 12;
                if (refBytes > reader.Remaining)
                {
                    throw new UnityException("type tree refType が metadata の残り領域を超えています");
                }
                reader.Skip((int)refBytes);
            }
        }
    }

    public sealed class UnitySerializedType
    {
        internal UnitySerializedType(int classId, bool isStripped, short scriptTypeIndex)
        {
            this.ClassId = classId;
            this.IsStripped = isStripped;
            this.ScriptTypeIndex = scriptTypeIndex;
        }

        public int ClassId { get; private set; }
        public bool IsStripped { get; private set; }
        public short ScriptTypeIndex { get; private set; }
    }

    public sealed class UnityObjectInfo
    {
        internal UnityObjectInfo(long pathId, long byteStart, uint byteSize, int typeIndex, int classId)
        {
            this.PathId = pathId;
            this.ByteStart = byteStart;
            this.ByteSize = byteSize;
            this.TypeIndex = typeIndex;
            this.ClassId = classId;
        }

        public long PathId { get; private set; }

        /// <summary>相對於 dataOffset 的起點。</summary>
        public long ByteStart { get; private set; }

        public uint ByteSize { get; private set; }

        /// <summary>types 表的 index。</summary>
        public int TypeIndex { get; private set; }

        public int ClassId { get; private set; }
    }

    public sealed class UnityScriptType
    {
        internal UnityScriptType(int localSerializedFileIndex, long localIdentifierInFile)
        {
            this.LocalSerializedFileIndex = localSerializedFileIndex;
            this.LocalIdentifierInFile = localIdentifierInFile;
        }

        public int LocalSerializedFileIndex { get; private set; }
        public long LocalIdentifierInFile { get; private set; }
    }

    public sealed class UnityExternal
    {
        internal UnityExternal(byte[] guid, int type, string pathName)
        {
            this.Guid = guid;
            this.Type = type;
            this.PathName = pathName;
        }

        public byte[] Guid { get; private set; }
        public int Type { get; private set; }
        public string PathName { get; private set; }
    }
}
