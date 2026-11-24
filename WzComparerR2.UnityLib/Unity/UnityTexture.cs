using System;
using System.IO;
using System.IO.Compression;

namespace WzComparerR2.UnityLib.Unity
{
    /// <summary>
    /// Texture2D → RGBA8 → PNG 的圖片管線核心。
    /// <para>
    /// 格式支援範圍 = 全量普查實測結果：整批遊戲資料只有 RGB24（fmt 3）與
    /// RGBA32（fmt 4）兩種，無 DXT/BC7/ETC。像素位置：Texture2D 本體
    /// （inline image data）或 <c>.resS</c> node（m_StreamData）。
    /// </para>
    /// </summary>
    public static class UnityTexture
    {
        public const int FormatAlpha8 = 1;
        public const int FormatRgb24 = 3;
        public const int FormatRgba32 = 4;
        public const int FormatArgb32 = 5;
        public const int FormatBgra32 = 14;

        private const int MaxTextureDimension = 32768;
        private const int MaxDecodedBytes = 512 * 1024 * 1024;

        /// <summary>把原始像素 bytes 轉成 RGBA8。輸入長度不符格式需求時擲出例外。</summary>
        public static byte[] DecodePixels(int format, int width, int height, byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            int pixelCount = GetPixelCount(width, height);
            switch (format)
            {
                case FormatRgba32:
                {
                    int needed = pixelCount * 4;
                    RequireLength(data, needed, "RGBA32");
                    byte[] output = new byte[needed];
                    Buffer.BlockCopy(data, 0, output, 0, needed);
                    return output;
                }

                case FormatRgb24:
                {
                    int needed = pixelCount * 3;
                    RequireLength(data, needed, "RGB24");
                    byte[] output = new byte[pixelCount * 4];
                    for (int i = 0, j = 0; i < needed; i += 3, j += 4)
                    {
                        output[j] = data[i];
                        output[j + 1] = data[i + 1];
                        output[j + 2] = data[i + 2];
                        output[j + 3] = 255;
                    }
                    return output;
                }

                case FormatArgb32:
                {
                    int needed = pixelCount * 4;
                    RequireLength(data, needed, "ARGB32");
                    byte[] output = new byte[needed];
                    for (int i = 0, j = 0; i < needed; i += 4, j += 4)
                    {
                        output[j] = data[i + 1];
                        output[j + 1] = data[i + 2];
                        output[j + 2] = data[i + 3];
                        output[j + 3] = data[i];
                    }
                    return output;
                }

                case FormatBgra32:
                {
                    int needed = pixelCount * 4;
                    RequireLength(data, needed, "BGRA32");
                    byte[] output = new byte[needed];
                    for (int i = 0, j = 0; i < needed; i += 4, j += 4)
                    {
                        output[j] = data[i + 2];
                        output[j + 1] = data[i + 1];
                        output[j + 2] = data[i];
                        output[j + 3] = data[i + 3];
                    }
                    return output;
                }

                case FormatAlpha8:
                {
                    RequireLength(data, pixelCount, "Alpha8");
                    byte[] output = new byte[pixelCount * 4];
                    for (int i = 0, j = 0; i < pixelCount; i++, j += 4)
                    {
                        output[j] = 255;
                        output[j + 1] = 255;
                        output[j + 2] = 255;
                        output[j + 3] = data[i];
                    }
                    return output;
                }

                default:
                    throw new UnityException("지원하지 않는 TextureFormat: " + format);
            }
        }

        /// <summary>
        /// 依 <see cref="UnityTexture2DInfo"/> 從 bundle 取像素（隨機存取，只解需要的 LZ4 block）。
        /// <para>
        /// imageDataSize &gt; 0 時像素內嵌在物件 bytes 裡（objectData 需為完整物件）；
        /// 否則依 m_StreamData 從 <c>.resS</c> node 取 (offset, size) 範圍。
        /// </para>
        /// <para>
        /// 注意：回傳的是原始（bottom-up）排列。Unity 序列化像素是 bottom-up
        /// （OpenGL 慣例），atlas rect 座標也是同一套空間，正確流程是先裁切、
        /// 再對最終小圖 <see cref="FlipVertical"/>。
        /// </para>
        /// </summary>
        public static byte[] FetchPixels(UnityFsBundle bundle, UnityTexture2DInfo texture, byte[] objectData)
        {
            if (bundle == null)
            {
                throw new ArgumentNullException(nameof(bundle));
            }
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }
            if (objectData == null)
            {
                throw new ArgumentNullException(nameof(objectData));
            }

            byte[] raw;
            if (texture.ImageDataSize > 0)
            {
                long imageEnd = texture.ImageDataOffset + (long)texture.ImageDataSize;
                if (imageEnd > objectData.Length)
                {
                    throw new UnityException(string.Format(
                        "inline image data 범위 초과: {0}..{1}（objectData {2} bytes）",
                        texture.ImageDataOffset, imageEnd, objectData.Length));
                }
                raw = new byte[texture.ImageDataSize];
                Buffer.BlockCopy(objectData, texture.ImageDataOffset, raw, 0, raw.Length);
            }
            else
            {
                if (string.IsNullOrEmpty(texture.StreamPath))
                {
                    throw new UnityException(string.Format(
                        "Texture2D {0} 無像素資料（inline=0 且無 stream path）", texture.Name));
                }
                string[] parts = texture.StreamPath.Split('/');
                string resourceName = parts[parts.Length - 1];
                int resourceNode = bundle.FindNode(resourceName);
                if (resourceNode < 0)
                {
                    throw new UnityException("Cannot find .resS node: " + resourceName);
                }
                raw = bundle.ReadNodeRange(resourceNode, (long)texture.StreamOffset, (int)texture.StreamSize);
            }
            return DecodePixels(texture.TextureFormat, texture.Width, texture.Height, raw);
        }

        /// <summary>整張貼圖（非 atlas）取「顯示用」top-down RGBA8。</summary>
        public static byte[] FetchPixelsFlipped(UnityFsBundle bundle, UnityTexture2DInfo texture, byte[] objectData)
        {
            byte[] rgba = FetchPixels(bundle, texture, objectData);
            FlipVertical(rgba, texture.Width, texture.Height);
            return rgba;
        }

        /// <summary>垂直翻轉（bottom-up → top-down）。</summary>
        public static void FlipVertical(byte[] rgba, int width, int height)
        {
            int pixelCount = GetPixelCount(width, height);
            int expected = pixelCount * 4;
            if (rgba.Length != expected)
            {
                throw new UnityException(string.Format(
                    "flip_vertical 길이 불일치: {0} != {1}", rgba.Length, expected));
            }
            int stride = width * 4;
            byte[] row = new byte[stride];
            for (int y = 0; y < height / 2; y++)
            {
                int top = y * stride;
                int bottom = (height - 1 - y) * stride;
                Buffer.BlockCopy(rgba, top, row, 0, stride);
                Buffer.BlockCopy(rgba, bottom, rgba, top, stride);
                Buffer.BlockCopy(row, 0, rgba, bottom, stride);
            }
        }

        /// <summary>RGBA8 → PNG。</summary>
        public static byte[] ToPng(int width, int height, byte[] rgba)
        {
            int pixelCount = GetPixelCount(width, height);
            int rowBytes = width * 4;
            if (rgba.Length != pixelCount * 4)
            {
                throw new UnityException(string.Format(
                    "to_png 길이 불일치: {0} != {1}x{2}x4", rgba.Length, width, height));
            }

            byte[] raw = new byte[(rowBytes + 1) * height];
            for (int y = 0; y < height; y++)
            {
                int source = y * rowBytes;
                int target = y * (rowBytes + 1);
                raw[target] = 0; // filter type 0
                Buffer.BlockCopy(rgba, source, raw, target + 1, rowBytes);
            }

            byte[] compressed = ZlibCompress(raw);
            using (MemoryStream output = new MemoryStream(compressed.Length + 64))
            {
                output.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, 0, 8);

                byte[] ihdr = new byte[13];
                WriteBigEndian(ihdr, 0, (uint)width);
                WriteBigEndian(ihdr, 4, (uint)height);
                ihdr[8] = 8; // bit depth
                ihdr[9] = 6; // colour type: RGBA
                WriteChunk(output, "IHDR", ihdr);
                WriteChunk(output, "IDAT", compressed);
                WriteChunk(output, "IEND", new byte[0]);
                return output.ToArray();
            }
        }

        /// <summary>從 RGBA8 圖切出子矩形（atlas 切圖用）。邊界超過時擲出例外。</summary>
        public static byte[] CropRgba(
            int imageWidth, int imageHeight, byte[] rgba, int x, int y, int width, int height)
        {
            int imagePixels = GetPixelCount(imageWidth, imageHeight);
            int expected = imagePixels * 4;
            if (rgba.Length != expected)
            {
                throw new UnityException(string.Format(
                    "crop source 길이 불일치: {0} != {1}", rgba.Length, expected));
            }
            if (width <= 0 || height <= 0
                || x < 0 || y < 0
                || x > imageWidth - width || y > imageHeight - height)
            {
                throw new UnityException(string.Format(
                    "crop 범위 초과: ({0},{1},{2}x{3}) 초과 {4}x{5}",
                    x, y, width, height, imageWidth, imageHeight));
            }

            int rowBytes = width * 4;
            byte[] output = new byte[GetPixelCount(width, height) * 4];
            for (int row = 0; row < height; row++)
            {
                int source = ((y + row) * imageWidth + x) * 4;
                Buffer.BlockCopy(rgba, source, output, row * rowBytes, rowBytes);
            }
            return output;
        }

        private static int GetPixelCount(int width, int height)
        {
            if (width <= 0 || height <= 0 || width > MaxTextureDimension || height > MaxTextureDimension)
            {
                throw new UnityException(string.Format("텍스처 크기 오류: {0}x{1}", width, height));
            }
            long pixels = (long)width * height;
            if (pixels * 4 > MaxDecodedBytes)
            {
                throw new UnityException(string.Format(
                    "貼圖解碼後大小超過上限: {0} > {1}", pixels * 4, MaxDecodedBytes));
            }
            return (int)pixels;
        }

        private static void RequireLength(byte[] data, int needed, string label)
        {
            if (data.Length < needed)
            {
                throw new UnityException(string.Format(
                    "{0} 長度不足: {1} < {2}", label, data.Length, needed));
            }
        }

        private static byte[] ZlibCompress(byte[] data)
        {
            using (MemoryStream compressed = new MemoryStream())
            {
                compressed.WriteByte(0x78);
                compressed.WriteByte(0x9C);
                using (DeflateStream deflate = new DeflateStream(compressed, CompressionMode.Compress, true))
                {
                    deflate.Write(data, 0, data.Length);
                }
                uint adler = Adler32(data);
                compressed.WriteByte((byte)(adler >> 24));
                compressed.WriteByte((byte)(adler >> 16));
                compressed.WriteByte((byte)(adler >> 8));
                compressed.WriteByte((byte)adler);
                return compressed.ToArray();
            }
        }

        private static void WriteChunk(Stream output, string type, byte[] data)
        {
            byte[] length = new byte[4];
            WriteBigEndian(length, 0, (uint)data.Length);
            output.Write(length, 0, 4);
            byte[] typeBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                typeBytes[i] = (byte)type[i];
            }
            output.Write(typeBytes, 0, 4);
            output.Write(data, 0, data.Length);

            uint crc = Crc32(typeBytes, data);
            byte[] crcBytes = new byte[4];
            WriteBigEndian(crcBytes, 0, crc);
            output.Write(crcBytes, 0, 4);
        }

        private static void WriteBigEndian(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        private static uint[] crcTable;

        private static uint Crc32(byte[] type, byte[] data)
        {
            uint[] table = crcTable;
            if (table == null)
            {
                table = new uint[256];
                for (uint i = 0; i < 256; i++)
                {
                    uint c = i;
                    for (int k = 0; k < 8; k++)
                    {
                        c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                    }
                    table[i] = c;
                }
                crcTable = table;
            }

            uint crc = 0xFFFFFFFFu;
            for (int i = 0; i < type.Length; i++)
            {
                crc = table[(crc ^ type[i]) & 0xFF] ^ (crc >> 8);
            }
            for (int i = 0; i < data.Length; i++)
            {
                crc = table[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
            }
            return crc ^ 0xFFFFFFFFu;
        }

        private static uint Adler32(byte[] data)
        {
            const uint Mod = 65521;
            uint a = 1;
            uint b = 0;
            for (int i = 0; i < data.Length; i++)
            {
                a = (a + data[i]) % Mod;
                b = (b + a) % Mod;
            }
            return (b << 16) | a;
        }
    }
}
