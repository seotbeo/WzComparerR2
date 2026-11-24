using System;
using System.Collections.Generic;
using System.IO;
using K4os.Compression.LZ4;

namespace WzComparerR2.UnityLib.Unity
{
    /// <summary>
    /// UnityFS bundle 解析 + LZ4 區塊隨機存取。
    /// <para>
    /// UnityFS header（全部 big-endian）：
    /// "UnityFS\0" | version u32 | unityVersion cstr | unityRevision cstr
    /// | size i64 | compressedBlocksInfoSize u32 | uncompressedBlocksInfoSize u32 | flags u32
    /// </para>
    /// <para>
    /// flags 重點位元：0x3F = blocksInfo 壓縮方式（0=raw, 1=LZMA, 2/3=LZ4/LZ4HC）；
    /// 0x40 = blocksInfo 與 directory 合併；0x80 = blocksInfo 放在檔案尾端；
    /// 0x200 = blocksInfo 之後資料區需 align(16)。
    /// </para>
    /// <para>
    /// version >= 7 時 header 固定欄位結束後要先 align(16) 才是 blocksInfo。
    /// blocksInfo blob（big-endian）：hash[16] | blockCount u32 |
    /// blocks[(uncompressedSize u32, compressedSize u32, flags u16)] |
    /// nodeCount u32 | nodes[(offset i64, size i64, flags u32, name cstr)]。
    /// </para>
    /// <para>
    /// node 的 (offset, size) 以「所有 block 解壓後串接起來的位址空間」為基準，
    /// 因此用 uncompressed size 前綴和即可把任一 byte range 映射到少數 block。
    /// </para>
    /// </summary>
    public sealed class UnityFsBundle : IDisposable
    {
        /// <summary>blocksInfo 放在檔案尾端。</summary>
        private const uint FlagBlocksInfoAtEnd = 0x80;

        /// <summary>blocksInfo 之後資料區需 align(16)。</summary>
        private const uint FlagBlockInfoNeedPaddingAtStart = 0x200;

        private const uint SupportedFileVersion = 8;
        private const int MaxBlocksInfoSize = 256 * 1024 * 1024;
        private const int MaxBlockCount = 1000000;
        private const int MaxNodeCount = 1000000;
        private const uint MaxBlockSize = 512 * 1024 * 1024;
        private const int MaxReadSize = 1024 * 1024 * 1024;
        private const int BlockCacheCapacity = 8;

        private readonly IUnityDataStream source;
        private readonly bool ownsSource;
        private readonly List<BundleBlock> blocks;
        private readonly List<long> blockFileOffsets;
        private readonly List<long> blockDataPrefix;
        private readonly List<BundleNode> nodes;
        private readonly object cacheLock = new object();
        private readonly Dictionary<int, byte[]> blockCache = new Dictionary<int, byte[]>();
        private readonly List<int> blockCacheOrder = new List<int>();

        private UnityFsBundle(IUnityDataStream source, bool ownsSource)
        {
            this.source = source;
            this.ownsSource = ownsSource;
            this.blocks = new List<BundleBlock>();
            this.blockFileOffsets = new List<long>();
            this.blockDataPrefix = new List<long>();
            this.nodes = new List<BundleNode>();
            this.Parse();
        }

        /// <summary>開啟磁碟上的 bundle 檔。</summary>
        public static UnityFsBundle Open(string path)
        {
            return new UnityFsBundle(new UnityFileDataStream(path), true);
        }

        /// <summary>從任意隨機讀取來源開啟 bundle。</summary>
        public static UnityFsBundle Open(IUnityDataStream source, bool ownsSource = false)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return new UnityFsBundle(source, ownsSource);
        }

        public IReadOnlyList<BundleNode> Nodes
        {
            get { return this.nodes; }
        }

        /// <summary>解壓後資料區的總大小。</summary>
        public long DecompressedSize
        {
            get { return this.blockDataPrefix.Count == 0 ? 0 : this.blockDataPrefix[this.blockDataPrefix.Count - 1]; }
        }

        /// <summary>依名稱尋找 node（區分大小寫），回傳 index；找不到回 -1。</summary>
        public int FindNode(string name)
        {
            for (int i = 0; i < this.nodes.Count; i++)
            {
                if (this.nodes[i].Name == name)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>讀整個 node。</summary>
        public byte[] ReadNode(int nodeIndex)
        {
            BundleNode node = this.GetNode(nodeIndex);
            if (node.Size > int.MaxValue)
            {
                throw new UnityException(string.Format("node {0} 太大，無法讀入記憶體", node.Name));
            }
            return this.ReadNodeRange(nodeIndex, 0, (int)node.Size);
        }

        /// <summary>讀取 node 內 [offset, offset+length) 的資料，只解壓落到的 block。</summary>
        public byte[] ReadNodeRange(int nodeIndex, long offset, int length)
        {
            BundleNode node = this.GetNode(nodeIndex);
            if (length < 0 || length > MaxReadSize)
            {
                throw new UnityException("單次讀取超過上限：" + length);
            }
            if (offset < 0 || offset > node.Size - length)
            {
                throw new UnityException(string.Format(
                    "node {0} 讀取越界: offset={1} len={2} size={3}", node.Name, offset, length, node.Size));
            }
            return this.ReadDataRange(node.Offset + offset, length);
        }

        /// <summary>以解壓後位址空間為基準讀取 [offset, offset+length)。</summary>
        public byte[] ReadDataRange(long offset, int length)
        {
            if (length == 0)
            {
                return new byte[0];
            }
            if (length < 0 || length > MaxReadSize)
            {
                throw new UnityException("單次讀取超過上限：" + length);
            }
            int first;
            int last;
            if (!TryGetBlocksForRange(this.blockDataPrefix, offset, length, out first, out last))
            {
                throw new UnityException(string.Format(
                    "資料範圍越界: offset={0} len={1}", offset, length));
            }

            byte[] output = new byte[length];
            int written = 0;
            long cursor = offset;
            long remaining = length;
            for (int blockIndex = first; blockIndex <= last; blockIndex++)
            {
                byte[] block = this.DecompressBlock(blockIndex);
                int inner = checked((int)(cursor - this.blockDataPrefix[blockIndex]));
                int available = block.Length - inner;
                if (available < 0)
                {
                    throw new UnityException("block 內 offset 越界");
                }
                int take = (int)Math.Min(remaining, available);
                Buffer.BlockCopy(block, inner, output, written, take);
                cursor += take;
                written += take;
                remaining -= take;
            }
            return output;
        }

        public void Dispose()
        {
            if (this.ownsSource)
            {
                this.source.Dispose();
            }
        }

        /// <summary>
        /// 計算 [offset, offset+length) 落在哪些 block（回傳起訖 index，含頭尾）。
        /// </summary>
        internal static bool TryGetBlocksForRange(
            IReadOnlyList<long> prefix, long offset, int length, out int first, out int last)
        {
            first = -1;
            last = -1;
            if (prefix == null || prefix.Count < 2 || length <= 0)
            {
                return false;
            }
            long end = offset + length;
            if (end < offset || end > prefix[prefix.Count - 1])
            {
                return false;
            }
            first = FindPrefixIndex(prefix, offset);
            last = FindPrefixIndex(prefix, end - 1);
            return first >= 0 && last >= first;
        }

        /// <summary>最後一個 prefix[i] &lt;= value 的 i。</summary>
        private static int FindPrefixIndex(IReadOnlyList<long> prefix, long value)
        {
            int low = 0;
            int high = prefix.Count - 1;
            while (low < high)
            {
                int mid = (low + high + 1) / 2;
                if (prefix[mid] <= value)
                {
                    low = mid;
                }
                else
                {
                    high = mid - 1;
                }
            }
            return low;
        }

        internal static long AlignUp(long value, long alignment)
        {
            return ((value + alignment - 1) / alignment) * alignment;
        }

        private BundleNode GetNode(int index)
        {
            if (index < 0 || index >= this.nodes.Count)
            {
                throw new UnityException("node index 越界：" + index);
            }
            return this.nodes[index];
        }

        private void Parse()
        {
            long fileLength = this.source.Length;
            int headLength = (int)Math.Min(fileLength, 256);
            byte[] head = new byte[headLength];
            this.source.ReadAt(0, head, 0, headLength);

            UnityByteReader reader = new UnityByteReader(head, false);
            byte[] signature = reader.Take(8);
            if (!BytesEqual(signature, new byte[] { 0x55, 0x6E, 0x69, 0x74, 0x79, 0x46, 0x53, 0x00 }))
            {
                throw new UnityException("不是 UnityFS 檔案（signature = " + ToPrintable(signature) + "）");
            }

            uint fileVersion = reader.U32();
            if (fileVersion != SupportedFileVersion)
            {
                throw new UnityException("不支援的 UnityFS 版本: " + fileVersion);
            }
            reader.CString(); // unityVersion
            reader.CString(); // unityRevision
            long bundleSize = reader.I64();
            if (bundleSize < 0 || bundleSize > fileLength)
            {
                throw new UnityException(string.Format(
                    "bundle size 異常: {0}（檔案 {1} bytes）", bundleSize, fileLength));
            }

            long compressedInfoSize = reader.U32();
            long uncompressedInfoSize = reader.U32();
            if (compressedInfoSize == 0 || compressedInfoSize > MaxBlocksInfoSize
                || uncompressedInfoSize == 0 || uncompressedInfoSize > MaxBlocksInfoSize)
            {
                throw new UnityException(string.Format(
                    "blocksInfo 大小異常: compressed={0}, uncompressed={1}",
                    compressedInfoSize, uncompressedInfoSize));
            }

            uint flags = reader.U32();
            long headerEnd = AlignUp(reader.Position, 16);
            if (headerEnd > bundleSize)
            {
                throw new UnityException("bundle header 超過 bundle size");
            }

            long blocksInfoPosition = (flags & FlagBlocksInfoAtEnd) != 0
                ? bundleSize - compressedInfoSize
                : headerEnd;
            long blocksInfoEnd = blocksInfoPosition + compressedInfoSize;
            if (blocksInfoPosition < headerEnd || blocksInfoEnd > bundleSize)
            {
                throw new UnityException(string.Format(
                    "blocksInfo range 越界: {0}..{1}, bundle={2}", blocksInfoPosition, blocksInfoEnd, bundleSize));
            }

            byte[] compressedInfo = new byte[compressedInfoSize];
            this.source.ReadAt(blocksInfoPosition, compressedInfo, 0, compressedInfo.Length);
            byte[] info;
            switch (flags & 0x3F)
            {
                case 0:
                    if (compressedInfo.Length != uncompressedInfoSize)
                    {
                        throw new UnityException(string.Format(
                            "raw blocksInfo 大小不一致: {0} != {1}", compressedInfo.Length, uncompressedInfoSize));
                    }
                    info = compressedInfo;
                    break;

                case 2:
                case 3:
                    info = new byte[uncompressedInfoSize];
                    if (LZ4Codec.Decode(compressedInfo, 0, compressedInfo.Length, info, 0, info.Length) < 0)
                    {
                        throw new UnityException("LZ4 解壓失敗: blocksInfo");
                    }
                    break;

                default:
                    throw new UnityException("不支援的 blocksInfo 壓縮方式: " + (flags & 0x3F));
            }

            this.ParseBlocksInfo(info, out long decompressedDataSize);

            // ── 資料區起點：0x200 旗標要求 align(16) ──
            long dataBase = (flags & FlagBlocksInfoAtEnd) != 0
                ? headerEnd
                : headerEnd + compressedInfoSize;
            long dataStart = (flags & FlagBlockInfoNeedPaddingAtStart) != 0
                ? AlignUp(dataBase, 16)
                : dataBase;

            long filePosition = dataStart;
            long decompressedPosition = 0;
            this.blockDataPrefix.Add(0);
            for (int i = 0; i < this.blocks.Count; i++)
            {
                BundleBlock block = this.blocks[i];
                this.blockFileOffsets.Add(filePosition);
                filePosition += block.CompressedSize;
                decompressedPosition += block.UncompressedSize;
                this.blockDataPrefix.Add(decompressedPosition);
            }

            long dataLimit = (flags & FlagBlocksInfoAtEnd) != 0 ? blocksInfoPosition : bundleSize;
            if (dataStart > dataLimit || filePosition > dataLimit)
            {
                throw new UnityException(string.Format(
                    "block 資料區越界: {0}..{1}, limit={2}", dataStart, filePosition, dataLimit));
            }
            if (decompressedPosition != decompressedDataSize)
            {
                throw new UnityException("block 解壓後大小不一致");
            }

            foreach (BundleNode node in this.nodes)
            {
                if (node.Offset > decompressedPosition - node.Size)
                {
                    throw new UnityException(string.Format(
                        "node {0} range {1}..{2} 超過解壓資料大小 {3}",
                        node.Name, node.Offset, node.Offset + node.Size, decompressedPosition));
                }
            }
        }

        private void ParseBlocksInfo(byte[] info, out long decompressedDataSize)
        {
            UnityByteReader reader = new UnityByteReader(info, false);
            reader.Skip(16); // hash

            uint blockCountRaw = reader.U32();
            if (blockCountRaw == 0 || blockCountRaw > MaxBlockCount
                || blockCountRaw > (uint)(reader.Remaining / 10))
            {
                throw new UnityException("block count 異常：" + blockCountRaw);
            }
            int blockCount = (int)blockCountRaw;

            for (int i = 0; i < blockCount; i++)
            {
                uint uncompressedSize = reader.U32();
                uint compressedSize = reader.U32();
                ushort blockFlags = reader.U16();
                if (uncompressedSize == 0 || compressedSize == 0
                    || uncompressedSize > MaxBlockSize || compressedSize > MaxBlockSize)
                {
                    throw new UnityException(string.Format(
                        "block 大小異常: compressed={0}, uncompressed={1}", compressedSize, uncompressedSize));
                }
                switch (blockFlags & 0x3F)
                {
                    case 0:
                        if (compressedSize != uncompressedSize)
                        {
                            throw new UnityException(string.Format(
                                "raw block 大小不一致: {0} != {1}", compressedSize, uncompressedSize));
                        }
                        break;
                    case 2:
                    case 3:
                        break;
                    default:
                        throw new UnityException(string.Format(
                            "不支援的 block 壓縮方式: {0}（僅支援 raw / LZ4 / LZ4HC）", blockFlags & 0x3F));
                }
                this.blocks.Add(new BundleBlock(uncompressedSize, compressedSize, blockFlags));
            }

            uint nodeCountRaw = reader.U32();
            if (nodeCountRaw > MaxNodeCount || nodeCountRaw > (uint)(reader.Remaining / 21))
            {
                throw new UnityException("node count 異常：" + nodeCountRaw);
            }
            int nodeCount = (int)nodeCountRaw;
            for (int i = 0; i < nodeCount; i++)
            {
                long offset = reader.I64();
                long size = reader.I64();
                if (offset < 0 || size < 0)
                {
                    throw new UnityException(string.Format(
                        "node offset/size 為負: offset={0}, size={1}", offset, size));
                }
                uint nodeFlags = reader.U32();
                string name = reader.CString();
                this.nodes.Add(new BundleNode(offset, size, nodeFlags, name));
            }

            decompressedDataSize = 0;
            foreach (BundleBlock block in this.blocks)
            {
                decompressedDataSize += block.UncompressedSize;
            }
        }

        private byte[] DecompressBlock(int index)
        {
            lock (this.cacheLock)
            {
                byte[] cached;
                if (this.blockCache.TryGetValue(index, out cached))
                {
                    this.blockCacheOrder.Remove(index);
                    this.blockCacheOrder.Add(index);
                    return cached;
                }
            }

            if (index < 0 || index >= this.blocks.Count)
            {
                throw new UnityException("block index 越界：" + index);
            }
            BundleBlock block = this.blocks[index];
            byte[] raw = new byte[block.CompressedSize];
            this.source.ReadAt(this.blockFileOffsets[index], raw, 0, raw.Length);

            byte[] data;
            switch (block.Flags & 0x3F)
            {
                case 0:
                    data = raw;
                    break;

                case 2:
                case 3:
                    data = new byte[block.UncompressedSize];
                    if (LZ4Codec.Decode(raw, 0, raw.Length, data, 0, data.Length) < 0)
                    {
                        throw new UnityException("LZ4 解壓失敗: block " + index);
                    }
                    break;

                default:
                    throw new UnityException("不支援的 block 壓縮方式: " + (block.Flags & 0x3F));
            }

            lock (this.cacheLock)
            {
                if (this.blockCache.ContainsKey(index))
                {
                    return this.blockCache[index];
                }
                if (this.blockCacheOrder.Count >= BlockCacheCapacity)
                {
                    int evict = this.blockCacheOrder[0];
                    this.blockCacheOrder.RemoveAt(0);
                    this.blockCache.Remove(evict);
                }
                this.blockCache[index] = data;
                this.blockCacheOrder.Add(index);
            }
            return data;
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static string ToPrintable(byte[] bytes)
        {
            char[] chars = new char[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i] = bytes[i] >= 0x20 && bytes[i] < 0x7F ? (char)bytes[i] : '?';
            }
            return new string(chars);
        }

        private sealed class BundleBlock
        {
            public BundleBlock(uint uncompressedSize, uint compressedSize, ushort flags)
            {
                this.UncompressedSize = uncompressedSize;
                this.CompressedSize = compressedSize;
                this.Flags = flags;
            }

            public uint UncompressedSize { get; private set; }
            public uint CompressedSize { get; private set; }
            public ushort Flags { get; private set; }
        }

        /// <summary>bundle 內的一個 node（SerializedFile 或 .resS 等資源檔）。</summary>
        public sealed class BundleNode
        {
            internal BundleNode(long offset, long size, uint flags, string name)
            {
                this.Offset = offset;
                this.Size = size;
                this.Flags = flags;
                this.Name = name;
            }

            /// <summary>以解壓後資料區起點為基準的 offset。</summary>
            public long Offset { get; private set; }

            public long Size { get; private set; }

            public uint Flags { get; private set; }

            public string Name { get; private set; }

            /// <summary>node flags bit 2 (0x4)：此 node 是 SerializedFile（否則是 .resS 等資源檔）。</summary>
            public bool IsSerializedFile
            {
                get { return (this.Flags & 4) != 0; }
            }
        }
    }
}
