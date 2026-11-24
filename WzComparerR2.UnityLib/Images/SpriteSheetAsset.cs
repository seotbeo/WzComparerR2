using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.UnityLib.Unity;

namespace WzComparerR2.UnityLib.Images
{
    /// <summary>
    /// 新版 SpriteSheet <c>.asset</c> 的 MonoBehaviour payload。
    /// <para>
    /// 依序為 img 路徑、atlas 頁 GUID 清單、實際 sprite（路徑 / 頁 / rect / pivot），
    /// 以及別名（路徑 → sprite 序號）。字串採 Unity 的 4-byte 對齊格式。
    /// </para>
    /// </summary>
    public sealed class SpriteSheetAsset
    {
        private const int MaxItems = 1000000;
        private const int MaxStringLength = 1000000;

        private readonly List<WzssSprite> sprites;
        private readonly Dictionary<string, int?> paths;

        private SpriteSheetAsset(string path, List<WzssSprite> sprites, Dictionary<string, int?> paths)
        {
            this.Path = path;
            this.sprites = sprites;
            this.paths = paths;
        }

        public string Path { get; private set; }

        public bool TryGetSpriteForNode(string innerPath, out WzssSprite sprite)
        {
            sprite = default(WzssSprite);
            int? index;
            if (innerPath == null || !this.paths.TryGetValue(innerPath, out index) || !index.HasValue)
            {
                return false;
            }
            if (index.Value < 0 || index.Value >= this.sprites.Count)
            {
                return false;
            }
            sprite = this.sprites[index.Value];
            return true;
        }

        public bool TryGetFirstSprite(out WzssSprite sprite)
        {
            if (this.sprites.Count > 0)
            {
                sprite = this.sprites[0];
                return true;
            }
            sprite = default(WzssSprite);
            return false;
        }

        public static SpriteSheetAsset Parse(byte[] payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }
            Cursor cursor = new Cursor(payload);
            string path = cursor.ReadString();

            int pageCount = cursor.ReadCount();
            for (int i = 0; i < pageCount; i++)
            {
                cursor.ReadString(); // atlas GUID
                cursor.Take(8);      // 頁的保留欄位
            }

            int spriteCount = cursor.ReadCount();
            List<WzssSprite> sprites = new List<WzssSprite>(spriteCount);
            Dictionary<string, int?> paths = new Dictionary<string, int?>(spriteCount, StringComparer.Ordinal);
            for (int i = 0; i < spriteCount; i++)
            {
                string name = cursor.ReadString();
                int page = cursor.ReadInt32();
                if (page < 0 || page >= pageCount)
                {
                    throw new UnityException(string.Format(
                        "SpriteSheet asset 頁號越界: {0}/{1}", page, pageCount));
                }
                WzssRect rect = new WzssRect(
                    cursor.ReadPixel(true), cursor.ReadPixel(true),
                    cursor.ReadPixel(false), cursor.ReadPixel(false));
                cursor.Take(8); // pivot (2 × f32)，裁圖不需使用
                sprites.Add(new WzssSprite(page, rect));
                paths[name] = i;
            }

            int aliasCount = cursor.ReadCount();
            for (int i = 0; i < aliasCount; i++)
            {
                string name = cursor.ReadString();
                int index = cursor.ReadInt32();
                // spriteCount 是空圖片 sentinel，對應 WZSS 的既有慣例。
                if (index < 0 || index > spriteCount)
                {
                    throw new UnityException(string.Format(
                        "SpriteSheet asset 別名序號越界: {0}/{1}", index, spriteCount));
                }
                paths[name] = index != spriteCount ? index : (int?)null;
            }

            ReadAliasesWithPivot(cursor, spriteCount, paths);
            return new SpriteSheetAsset(path, sprites, paths);
        }

        private static void ReadAliasesWithPivot(Cursor cursor, int spriteCount, Dictionary<string, int?> paths)
        {
            if (cursor.Remaining < 4)
                return;

            var aliases = new Dictionary<string, int>(StringComparer.Ordinal);
            try
            {
                int count = cursor.ReadCount();
                if (count > cursor.Remaining / 16)
                    return;

                for (int i = 0; i < count; i++)
                {
                    string name = cursor.ReadString();
                    int index = cursor.ReadInt32();
                    cursor.Take(8); // Pivot override (two floats).
                    if (name.Length == 0 || index < 0 || index >= spriteCount)
                        return;
                    aliases[name] = index;
                }
            }
            catch (UnityException)
            {
                return;
            }

            if (cursor.Remaining == 0 || (cursor.Remaining == 4 && cursor.ReadInt32() == 0))
            {
                foreach (var alias in aliases)
                {
                    if (!paths.ContainsKey(alias.Key))
                        paths.Add(alias.Key, alias.Value);
                }
            }
        }

        private sealed class Cursor
        {
            private readonly byte[] bytes;
            private int position;
            public int Remaining => this.bytes.Length - this.position;

            public Cursor(byte[] bytes)
            {
                this.bytes = bytes;
            }

            public byte[] Take(int count)
            {
                if (count < 0 || this.position > this.bytes.Length - count)
                {
                    throw new UnityException("SpriteSheet asset 資料越界");
                }
                byte[] result = new byte[count];
                Buffer.BlockCopy(this.bytes, this.position, result, 0, count);
                this.position += count;
                return result;
            }

            public int ReadInt32()
            {
                byte[] b = this.Take(4);
                return b[0] | (b[1] << 8) | (b[2] << 16) | (b[3] << 24);
            }

            public int ReadPixel(bool allowZero)
            {
                float value = BitConverter.ToSingle(this.Take(4), 0);
                float rounded = (float)Math.Round(value);
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0
                    || (!allowZero && value == 0)
                    || Math.Abs(value - rounded) > 0.001
                    || rounded > int.MaxValue)
                {
                    throw new UnityException(string.Format(
                        "SpriteSheet asset rect 不是有效整數像素: {0}", value));
                }
                return (int)rounded;
            }

            public int ReadCount()
            {
                int count = this.ReadInt32();
                if (count < 0 || count > MaxItems)
                {
                    throw new UnityException("SpriteSheet asset 項目過多: " + count);
                }
                return count;
            }

            public string ReadString()
            {
                int length = this.ReadInt32();
                if (length < 0 || length > MaxStringLength)
                {
                    throw new UnityException("SpriteSheet asset 字串過長: " + length);
                }
                string value = Encoding.UTF8.GetString(this.Take(length), 0, length);
                int padding = (4 - length % 4) % 4;
                this.Take(padding);
                return value;
            }
        }
    }
}
