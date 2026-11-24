using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using WzComparerR2.UnityLib.Unity;

namespace WzComparerR2.UnityLib.Images
{
    /// <summary>WZSS 索引內的一個 sprite rect（座標是「單頁內」的）。</summary>
    public struct WzssRect : IEquatable<WzssRect>
    {
        public WzssRect(int x, int y, int width, int height)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
        }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public bool Equals(WzssRect other)
        {
            return this.X == other.X && this.Y == other.Y
                && this.Width == other.Width && this.Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is WzssRect && this.Equals((WzssRect)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = this.X;
                hash = (hash * 397) ^ this.Y;
                hash = (hash * 397) ^ this.Width;
                hash = (hash * 397) ^ this.Height;
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2}x{3})", this.X, this.Y, this.Width, this.Height);
        }
    }

    /// <summary>一個 frame 在 atlas 上的完整位置：哪一頁 + 該頁內的 rect。</summary>
    public struct WzssSprite : IEquatable<WzssSprite>
    {
        public WzssSprite(int page, WzssRect rect)
        {
            this.Page = page;
            this.Rect = rect;
        }

        public int Page { get; private set; }
        public WzssRect Rect { get; private set; }

        public bool Equals(WzssSprite other)
        {
            return this.Page == other.Page && this.Rect.Equals(other.Rect);
        }

        public override bool Equals(object obj)
        {
            return obj is WzssSprite && this.Equals((WzssSprite)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (this.Page * 397) ^ this.Rect.GetHashCode();
            }
        }
    }

    /// <summary>WZSS 中明確保存的 UOL → sprite frame 關係。</summary>
    public sealed class WzssUolLink
    {
        internal WzssUolLink(string uol, int? frameIndex)
        {
            this.Uol = uol;
            this.FrameIndex = frameIndex;
        }

        public string Uol { get; private set; }

        /// <summary><c>null</c> 是 WZSS 以 frame_count 表示的空圖片 sentinel。</summary>
        public int? FrameIndex { get; private set; }
    }

    /// <summary>atlas 的一頁（已解碼 RGBA，仍是 Unity 的 bottom-up 排列）。</summary>
    public sealed class AtlasPage
    {
        public AtlasPage(int index, int width, int height, byte[] rgba)
        {
            if (rgba == null)
            {
                throw new ArgumentNullException(nameof(rgba));
            }
            this.Index = index;
            this.Width = width;
            this.Height = height;
            this.Rgba = rgba;
        }

        /// <summary>檔名 <c>_N</c> 後綴的頁號，即 frame 記錄裡的頁索引。</summary>
        public int Index { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public byte[] Rgba { get; private set; }
    }

    /// <summary>
    /// WZSS 索引解析與 atlas 裁切。
    /// <para>
    /// WZSS header 是 20 個 u32，所有 offset 以 magic 位置為基準：
    /// [0] atlas 頁數；[1] 常數 8；[2..5) UOL 字串池（count / blobOff / tableOff）；
    /// [5..7) sprite pivot（count / off，8B/筆）；[7..9) sprite rect（count / off，
    /// 16B/筆 = u32 x,y,w,h）；[9..11) frame（count / off，20B/筆；第 3 個 i32 是
    /// WZSS atlas 頁號、第 4 個是 rect index）；[11..13) link（count / off，
    /// 16B/筆 = 槽號 / UOL 字串 index(-1) / ? / frame index）；
    /// [13..16) 節點鍵池；[16..19) 每個 frame 的 64 hex 池；[19] path 長度，
    /// 接著 path 字串（= img 邏輯路徑）。
    /// </para>
    /// <para>
    /// magic + 8 起是 <c>頁數</c> 個 u32 的頁對照表：第 i 項 = 「WZSS 頁 i」
    /// 對應的 atlas 檔名 <c>_N</c> 頁號。兩套編號常常不同，少了這層換算會整頁切錯圖。
    /// </para>
    /// </summary>
    public sealed class WzssIndex
    {
        private const int MaxRects = 1000000;
        private const int MaxFrames = 1000000;
        private const int MaxLinks = 1000000;
        private const int MaxStrings = 1000000;

        private readonly List<WzssRect> rects;
        private readonly List<WzssUolLink> uolLinks;
        private readonly string[] nodeKeys;
        private readonly FrameEntry[] frames;
        private readonly List<KeyValuePair<int, int?>> aliasFrames;
        private readonly int[] pageMap;

        private WzssIndex(
            string path, List<WzssRect> rects, List<WzssUolLink> uolLinks, string[] nodeKeys,
            FrameEntry[] frames, List<KeyValuePair<int, int?>> aliasFrames, int[] pageMap)
        {
            this.Path = path;
            this.rects = rects;
            this.uolLinks = uolLinks;
            this.nodeKeys = nodeKeys;
            this.frames = frames;
            this.aliasFrames = aliasFrames;
            this.pageMap = pageMap;
        }

        public string Path { get; private set; }

        public IReadOnlyList<WzssRect> Rects
        {
            get { return this.rects; }
        }

        public IReadOnlyList<WzssUolLink> UolLinks
        {
            get { return this.uolLinks; }
        }

        /// <summary>
        /// 節點鍵 = <c>Guid(MD5(UTF8("&lt;img 路徑&gt;/&lt;節點相對路徑&gt;"))).ToString("N")</c>。
        /// .NET <see cref="Guid(byte[])"/> 的前 4/2/2 bytes 反轉慣例正好與 Unity 資料一致。
        /// </summary>
        public static string ComputeNodeKey(string fullPath)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] digest = md5.ComputeHash(Encoding.UTF8.GetBytes(fullPath));
                return new Guid(digest).ToString("N");
            }
        }

        /// <summary>由 img 內相對路徑精確定位 frame。查無鍵時回 false。</summary>
        public bool TryGetFrameIndexForNode(string innerPath, out int frameIndex)
        {
            frameIndex = -1;
            int slot;
            if (!this.TryFindSlot(ComputeNodeKey(this.Path + "/" + innerPath), out slot))
            {
                return false;
            }
            if (slot < this.frames.Length)
            {
                frameIndex = slot;
                return true;
            }
            for (int i = 0; i < this.aliasFrames.Count; i++)
            {
                if (this.aliasFrames[i].Key == slot)
                {
                    if (this.aliasFrames[i].Value.HasValue)
                    {
                        frameIndex = this.aliasFrames[i].Value.Value;
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        /// <summary>frame → atlas 頁（已換算成檔名 <c>_N</c> 的頁號）+ 頁內 rect。</summary>
        public bool TryGetSprite(int frameIndex, out WzssSprite sprite)
        {
            sprite = default(WzssSprite);
            if (frameIndex < 0 || frameIndex >= this.frames.Length)
            {
                return false;
            }
            FrameEntry entry = this.frames[frameIndex];
            if (entry.Page < 0 || entry.Page >= this.pageMap.Length
                || entry.Rect < 0 || entry.Rect >= this.rects.Count)
            {
                return false;
            }
            sprite = new WzssSprite(this.pageMap[entry.Page], this.rects[entry.Rect]);
            return true;
        }

        /// <summary>從 sprite 指定的 atlas 頁（bottom-up RGBA）依 rect 切出 sprite 並翻正。</summary>
        public static UnityRgbaImage CutSprite(IReadOnlyList<AtlasPage> atlas, WzssSprite sprite)
        {
            AtlasPage page = null;
            for (int i = 0; i < atlas.Count; i++)
            {
                if (atlas[i].Index == sprite.Page)
                {
                    page = atlas[i];
                    break;
                }
            }
            if (page == null)
            {
                throw new UnityException(string.Format(
                    "atlas 缺少第 {0} 頁（共 {1} 頁）", sprite.Page, atlas.Count));
            }
            WzssRect rect = sprite.Rect;
            byte[] cropped = UnityTexture.CropRgba(
                page.Width, page.Height, page.Rgba, rect.X, rect.Y, rect.Width, rect.Height);
            UnityTexture.FlipVertical(cropped, rect.Width, rect.Height);
            return new UnityRgbaImage(rect.Width, rect.Height, cropped);
        }

        /// <summary>解析 WZSS 索引 MonoBehaviour payload（已去 Unity 標頭）。</summary>
        public static WzssIndex Parse(byte[] payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }
            if (payload.Length < 80)
            {
                throw new UnityException("WZSS payload 太短");
            }
            Func<int, uint> header = index => (uint)(payload[index * 4]
                | (payload[index * 4 + 1] << 8)
                | (payload[index * 4 + 2] << 16)
                | (payload[index * 4 + 3] << 24));

            // magic 前面是 path 與每頁 44 bytes 的 GUID 清單，頁數多時可以推到 1KB 以外。
            long scanLength = 128L + header(19) + (long)header(0) * 64;
            int scan = (int)Math.Min(scanLength, payload.Length);
            int magic = -1;
            for (int i = 0; i + 4 <= scan; i++)
            {
                if (payload[i] == (byte)'W' && payload[i + 1] == (byte)'Z'
                    && payload[i + 2] == (byte)'S' && payload[i + 3] == (byte)'S')
                {
                    magic = i;
                    break;
                }
            }
            if (magic < 0)
            {
                throw new UnityException("WZSS magic 不存在");
            }

            long rectCount = header(7);
            if (rectCount > MaxRects)
            {
                throw new UnityException(string.Format("WZSS rect 數量異常: {0}", rectCount));
            }
            long rectStart = magic + (long)header(8);
            long rectEnd = rectStart + rectCount * 16;
            if (rectStart < 0 || rectEnd > payload.Length)
            {
                throw new UnityException(string.Format(
                    "WZSS rect 區越界: count={0} off={1} len={2}", rectCount, header(8), payload.Length));
            }
            List<WzssRect> rects = new List<WzssRect>((int)rectCount);
            for (int i = 0; i < rectCount; i++)
            {
                int position = checked((int)(rectStart + (long)i * 16));
                rects.Add(new WzssRect(
                    ReadI32(payload, position),
                    ReadI32(payload, position + 4),
                    ReadI32(payload, position + 8),
                    ReadI32(payload, position + 12)));
            }

            int[] pageMap = ParsePageMap(payload, magic, header);
            FrameEntry[] frames = ParseFrames(payload, magic, header, rects.Count, pageMap.Length);
            List<WzssUolLink> uolLinks;
            List<KeyValuePair<int, int?>> aliasFrames;
            ParseUolLinks(payload, magic, header, frames.Length, out uolLinks, out aliasFrames);
            string[] nodeKeys = ReadStringPool(
                payload, magic, header(13), header(14), header(15));

            long pathEnd = 80L + header(19);
            string path = pathEnd <= payload.Length
                ? Encoding.UTF8.GetString(payload, 80, (int)(pathEnd - 80))
                : string.Empty;

            return new WzssIndex(path, rects, uolLinks, nodeKeys, frames, aliasFrames, pageMap);
        }

        private bool TryFindSlot(string key, out int slot)
        {
            slot = Array.BinarySearch(this.nodeKeys, key, StringComparer.Ordinal);
            if (slot >= 0)
            {
                return true;
            }
            for (int i = 0; i < this.nodeKeys.Length; i++)
            {
                if (string.Equals(this.nodeKeys[i], key, StringComparison.Ordinal))
                {
                    slot = i;
                    return true;
                }
            }
            return false;
        }

        private static void ParseUolLinks(
            byte[] payload, int magic, Func<int, uint> header, int frameCount,
            out List<WzssUolLink> links, out List<KeyValuePair<int, int?>> aliases)
        {
            string[] strings = ReadStringPool(payload, magic, header(2), header(3), header(4));

            long recordCount = header(11);
            if (recordCount > MaxLinks)
            {
                throw new UnityException(string.Format("WZSS link 數量異常: {0}", recordCount));
            }
            long recordStart = magic + (long)header(12);
            long recordEnd = recordStart + recordCount * 16;
            if (recordStart < 0 || recordEnd > payload.Length)
            {
                throw new UnityException(string.Format(
                    "WZSS link 區越界: count={0} len={1}", recordCount, payload.Length));
            }

            links = new List<WzssUolLink>();
            aliases = new List<KeyValuePair<int, int?>>((int)recordCount);
            for (int i = 0; i < recordCount; i++)
            {
                int position = checked((int)(recordStart + (long)i * 16));
                int slot = ReadI32(payload, position);
                int stringIndex = ReadI32(payload, position + 4);
                int frameIndex = ReadI32(payload, position + 12);
                // frame_count 本身是「空圖片」sentinel，超過才算越界。
                if (frameIndex < 0 || frameIndex > frameCount)
                {
                    throw new UnityException(string.Format(
                        "WZSS link frame 索引越界: {0}/{1}", frameIndex, frameCount));
                }
                int? frame = frameIndex < frameCount ? frameIndex : (int?)null;
                if (slot >= 0)
                {
                    aliases.Add(new KeyValuePair<int, int?>(slot, frame));
                }
                // -1 是「相同圖片去重」記錄，只有非負值才引用上面的 UOL 字串池。
                if (stringIndex < 0)
                {
                    continue;
                }
                if (stringIndex >= strings.Length)
                {
                    throw new UnityException(string.Format(
                        "WZSS link 字串索引越界: {0}/{1}", stringIndex, strings.Length));
                }
                links.Add(new WzssUolLink(strings[stringIndex], frame));
            }
        }

        private static int[] ParsePageMap(byte[] payload, int magic, Func<int, uint> header)
        {
            long count = header(0);
            if (count == 0 || count > 4096)
            {
                throw new UnityException("WZSS atlas 頁數異常: " + count);
            }
            long start = magic + 8L;
            long end = start + count * 4;
            if (end > payload.Length)
            {
                throw new UnityException(string.Format(
                    "WZSS 頁對照表越界: count={0} len={1}", count, payload.Length));
            }
            int[] map = new int[count];
            for (int i = 0; i < map.Length; i++)
            {
                int position = checked((int)(start + i * 4L));
                int page = ReadI32(payload, position);
                if (page < 0 || page >= count)
                {
                    throw new UnityException(string.Format(
                        "WZSS 頁對照表項越界: {0}/{1}", page, count));
                }
                map[i] = page;
            }
            return map;
        }

        private static FrameEntry[] ParseFrames(
            byte[] payload, int magic, Func<int, uint> header, int rectCount, int pageCount)
        {
            long frameCount = header(9);
            if (frameCount > MaxFrames)
            {
                throw new UnityException("WZSS frame 數量異常: " + frameCount);
            }
            long frameStart = magic + (long)header(10);
            long frameEnd = frameStart + frameCount * 20;
            if (frameStart < 0 || frameEnd > payload.Length)
            {
                throw new UnityException(string.Format(
                    "WZSS frame 區越界: count={0} len={1}", frameCount, payload.Length));
            }

            FrameEntry[] frames = new FrameEntry[frameCount];
            for (int i = 0; i < frames.Length; i++)
            {
                int position = checked((int)(frameStart + (long)i * 20));
                // WzSpriteFrame 的第 3 個 i32 是 atlas 頁索引、第 4 個是 sprite rect 索引。
                int page = ReadI32(payload, position + 8);
                int rectIndex = ReadI32(payload, position + 12);
                if (rectIndex < 0 || rectIndex >= rectCount)
                {
                    throw new UnityException(string.Format(
                        "WZSS frame 的 rect 索引越界: {0}/{1}", rectIndex, rectCount));
                }
                if (page < 0 || page >= pageCount)
                {
                    throw new UnityException(string.Format(
                        "WZSS frame 的 atlas 頁索引越界: {0}/{1}", page, pageCount));
                }
                frames[i] = new FrameEntry(page, rectIndex);
            }
            return frames;
        }

        private static string[] ReadStringPool(
            byte[] payload, int magic, uint count, uint blobOffset, uint tableOffset)
        {
            if (count > MaxStrings)
            {
                throw new UnityException("WZSS 字串數量異常: " + count);
            }
            long tableStart = magic + (long)tableOffset;
            long tableBytes = ((long)count + 1) * 4;
            long tableEnd = tableStart + tableBytes;
            if (tableStart < 0 || tableEnd > payload.Length)
            {
                throw new UnityException("WZSS 字串表越界");
            }
            long blobStart = magic + (long)blobOffset;

            long[] offsets = new long[count + 1];
            for (int i = 0; i < offsets.Length; i++)
            {
                offsets[i] = (uint)ReadI32(payload, checked((int)(tableStart + i * 4L)));
            }

            string[] strings = new string[count];
            for (int i = 0; i < strings.Length; i++)
            {
                if (offsets[i] > offsets[i + 1])
                {
                    throw new UnityException("WZSS 字串表 offset 不是遞增順序");
                }
                long start = blobStart + offsets[i];
                long end = blobStart + offsets[i + 1];
                if (start < 0 || end > payload.Length)
                {
                    throw new UnityException("WZSS 字串 blob 越界");
                }
                strings[i] = Encoding.UTF8.GetString(payload, (int)start, (int)(end - start));
            }
            return strings;
        }

        private static int ReadI32(byte[] data, int position)
        {
            if (position < 0 || position > data.Length - 4)
            {
                throw new UnityException("WZSS 資料截斷");
            }
            return data[position]
                | (data[position + 1] << 8)
                | (data[position + 2] << 16)
                | (data[position + 3] << 24);
        }

        private struct FrameEntry
        {
            public FrameEntry(int page, int rect)
            {
                this.Page = page;
                this.Rect = rect;
            }

            /// <summary>atlas 頁索引（第 3 個 i32）。</summary>
            public int Page { get; private set; }

            /// <summary>sprite rect 索引（第 4 個 i32）。</summary>
            public int Rect { get; private set; }
        }
    }
}
