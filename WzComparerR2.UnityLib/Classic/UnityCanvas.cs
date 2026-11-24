using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using WzComparerR2.WzLib;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// 一個 WZ-like 的 Canvas 節點值：代表「某個 Unity 資產路徑上的圖片」，
    /// PNG 於第一次使用時才解碼（等同傳統 WZ 的 <c>Wz_Png</c> 角色）。
    /// </summary>
    public sealed class UnityCanvas : Wz_Png
    {
        private static readonly byte[] EmptyPng = Unity.UnityTexture.ToPng(1, 1, new byte[4]);

        private readonly ClassicWzSource source;
        private readonly object syncRoot = new object();
        private byte[] png;
        private readonly Images.UnitySheetEntry sheet;
        private readonly Func<byte[]> imageProvider;
        private readonly bool isSheetAtlas;
        private static readonly string[] PreferredRegions = { "Common", "CN", "TW", "JP", "KR" };
        private Dictionary<string, UnityCanvas> variants;
        private UnityCanvas defaultVariant;
        private int defaultRegionPriority = int.MaxValue;

        public IEnumerable<string> Regions => this.variants == null
            ? Array.Empty<string>()
            : this.variants.Keys.OrderBy(GetRegionPriority);
        public string DefaultRegion { get; private set; }

        public bool TryGetVariant(string region, out UnityCanvas canvas)
        {
            canvas = null;
            return region != null && this.variants != null && this.variants.TryGetValue(region, out canvas);
        }

        internal bool AddVariant(string region, UnityCanvas canvas)
        {
            this.variants ??= new Dictionary<string, UnityCanvas>(StringComparer.OrdinalIgnoreCase);
            this.variants.TryGetValue(region, out var previous);
            this.variants[region] = canvas;
            int priority = GetRegionPriority(region);
            if (this.defaultVariant == previous || priority < this.defaultRegionPriority)
            {
                this.defaultVariant = canvas;
                this.defaultRegionPriority = priority;
                this.DefaultRegion = region;
                this.Pages = canvas.Pages;
                return true;
            }
            return false;
        }

        private static int GetRegionPriority(string region)
        {
            int priority = Array.FindIndex(PreferredRegions,
                candidate => string.Equals(candidate, region, StringComparison.OrdinalIgnoreCase));
            return priority < 0 ? PreferredRegions.Length : priority;
        }

        public UnityCanvas(ClassicWzSource source, string path)
            : this(source, path, isSheetAtlas: false)
        {
        }

        internal UnityCanvas(ClassicWzSource source, string path, Func<byte[]> imageProvider)
            : this(source, path)
        {
            this.imageProvider = imageProvider;
        }

        internal UnityCanvas(ClassicWzSource source, string path, Images.UnitySheetEntry sheet)
            : this(source, path, () => source.RenderPrefabTexture(sheet.AtlasPages[0].Value))
        {
            this.sheet = sheet;
            this.Pages = sheet.AtlasPages.Count;
        }


        internal UnityCanvas(ClassicWzSource source, string path, bool isSheetAtlas)
            : base(0, 0, 0, Wz_TextureFormat.ARGB8888, 0, 1, 0, 0, null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            this.source = source;
            this.Path = path;
            this.isSheetAtlas = isSheetAtlas;
            if (isSheetAtlas)
                this.Pages = source.GetSheetAtlasPageCount(path);
        }

        /// <summary>邏輯路徑，如 <c>Character/00002000/stand1/0/body</c>。</summary>
        public string Path { get; private set; }

        /// <summary>Returns the PNG or a transparent pixel when the image is missing.</summary>
        public byte[] GetPng()
        {
            if (this.defaultVariant != null)
                return this.defaultVariant.GetPng();

            byte[] cached = this.png;
            if (cached != null)
            {
                return cached;
            }
            lock (this.syncRoot)
            {
                if (this.png == null)
                {
                    this.png = (this.imageProvider != null ? this.imageProvider() : this.isSheetAtlas
                        ? this.source.GetSheetAtlas(this.Path, 0)
                        : this.source.GetImage(this.Path)) ?? (byte[])EmptyPng.Clone();
                }
                return this.png;
            }
        }

        public override int Width
        {
            get => this.ReadDimension(16);
            set => throw new NotSupportedException();
        }

        public override int Height
        {
            get => this.ReadDimension(20);
            set => throw new NotSupportedException();
        }

        private int ReadDimension(int offset)
        {
            byte[] data = this.GetPng();
            if (data == null || data.Length < 24)
                return 0;

            return (data[offset] << 24) | (data[offset + 1] << 16)
                | (data[offset + 2] << 8) | data[offset + 3];
        }

        public override Bitmap ExtractPng(int page)
        {
            if (this.defaultVariant != null)
                return this.defaultVariant.ExtractPng(page);

            if (page < 0 || page >= this.ActualPages)
                throw new ArgumentOutOfRangeException(nameof(page));

            byte[] data = page == 0 ? this.GetPng() : (this.sheet != null ? this.source.RenderPrefabTexture(this.sheet.AtlasPages[page].Value) : this.source.GetSheetAtlas(this.Path, page)) ?? EmptyPng;

            using (var stream = new MemoryStream(data, false))
            using (var bitmap = new Bitmap(stream))
            {
                return new Bitmap(bitmap);
            }
        }

        public override int GetRawData(int skipBytes, Span<byte> buffer)
        {
            if (skipBytes < 0)
                throw new ArgumentOutOfRangeException(nameof(skipBytes));

            using (MemoryStream stream = this.OpenPixels())
            {
                stream.Position = skipBytes;
                byte[] pixels = stream.GetBuffer();
                int count = (int)Math.Max(0, Math.Min(buffer.Length, stream.Length - stream.Position));
                if (count > 0)
                    pixels.AsSpan(skipBytes, count).CopyTo(buffer);
                return count;
            }
        }

        public override Stream UnsafeOpenRead() => this.OpenPixels();

        private MemoryStream OpenPixels()
        {
            using (Bitmap bitmap = this.ExtractPng(0))
            {
                var bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                BitmapData data = bitmap.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    int rowBytes = checked(bitmap.Width * 4);
                    byte[] pixels = new byte[checked(rowBytes * bitmap.Height)];
                    for (int y = 0; y < bitmap.Height; y++)
                        Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), pixels, y * rowBytes, rowBytes);

                    return new MemoryStream(pixels, 0, pixels.Length, false, true);
                }
                finally
                {
                    bitmap.UnlockBits(data);
                }
            }
        }

        public override string ToString()
        {
            return "canvas " + this.Path;
        }
    }
}
