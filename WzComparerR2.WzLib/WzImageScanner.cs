using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using WzComparerR2.WzLib.Utilities;

#if NET6_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace WzComparerR2.WzLib
{
    public static class WzImageScanner
    {
        public static List<Tuple<long, long>> FindAllHits(Wz_File file, int maxSearchCount = int.MaxValue)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (maxSearchCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSearchCount));
            }

            lock (file.ReadLock)
            {
                long originalPosition = file.FileStream.Position;
                try
                {
                    file.FileStream.Position = 0;
                    var reader = new WzBinaryReader(file.FileStream, false);
                    byte[] target = { 0x73, 0xF8, 0xFA, 0xD9, 0xC3 };
                    var offsets = FindAllPatterns(reader, target, maxSearchCount);
                    var sizes = DiffAdjacent(offsets);
                    var images = new List<Tuple<long, long>>(sizes.Count);
                    for (int i = 0; i < sizes.Count; i++)
                    {
                        images.Add(Tuple.Create(offsets[i], sizes[i]));
                    }
                    return images;
                }
                finally
                {
                    file.FileStream.Position = originalPosition;
                }
            }
        }

        public static unsafe List<long> FindAllPatterns(WzBinaryReader reader, byte[] pattern, int maxSearchCount)
        {
            if (pattern == null || pattern.Length == 0)
            {
                throw new ArgumentException("pattern must not be empty");
            }

            const int BufferSize = 256 * 1024;

            int m = pattern.Length;
            int overlap = m - 1;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize + overlap);
            var positions = new List<long>();

            long filePos = 0;
            bool found = false;

            try
            {
                int bytesRead;

#if NET6_0_OR_GREATER
                if (Avx2.IsSupported && m >= 4)
                {
                    byte last = pattern[m - 1];
                    Vector256<byte> lastVec = Vector256.Create(last);

                    while ((bytesRead = reader.BaseStream.Read(buffer, overlap, BufferSize)) > 0)
                    {
                        int total = bytesRead + overlap;
                        int limit = total - m;

                        fixed (byte* pBuffer = buffer)
                        fixed (byte* pPattern = pattern)
                        {
                            int i = 0;

                            while (i <= limit - 31)
                            {
                                Vector256<byte> data = Avx.LoadVector256(pBuffer + i + m - 1);
                                Vector256<byte> cmp = Avx2.CompareEqual(data, lastVec);

                                uint mask = (uint)Avx2.MoveMask(cmp);

                                while (mask != 0)
                                {
                                    int bit = BitOperations.TrailingZeroCount(mask);
                                    int pos = i + bit;

                                    if (FastMatch(pBuffer + pos, pPattern, m))
                                    {
                                        long realPos = filePos + pos - overlap;
                                        if (realPos >= 0)
                                        {
                                            positions.Add(realPos);
                                            if (positions.Count >= maxSearchCount)
                                            {
                                                found = true;
                                                break;
                                            }
                                        }
                                    }

                                    mask &= mask - 1;
                                }

                                i += 32;
                                if (found) break;
                            }

                            while (i <= limit)
                            {
                                if (pBuffer[i + m - 1] == last && FastMatch(pBuffer + i, pPattern, m))
                                {
                                    long realPos = filePos + i - overlap;
                                    if (realPos >= 0)
                                    {
                                        positions.Add(realPos);
                                        if (positions.Count >= maxSearchCount)
                                        {
                                            found = true;
                                            break;
                                        }
                                    }
                                }

                                i++;
                            }
                        }

                        if (found) break;

                        if (overlap > 0)
                        {
                                Buffer.BlockCopy(buffer, total - overlap, buffer, 0, overlap);
                        }

                        filePos += bytesRead;
                    }

                    positions.Add(reader.BaseStream.Length);
                    return positions;
                }
                else
#endif
                {
                    Span<int> skip = stackalloc int[256];

                    for (int i = 0; i < 256; i++)
                    {
                            skip[i] = m;
                    }

                    for (int i = 0; i < m - 1; i++)
                    {
                            skip[pattern[i]] = m - 1 - i;
                    }

                    while ((bytesRead = reader.BaseStream.Read(buffer, overlap, BufferSize)) > 0)
                    {
                        int total = bytesRead + overlap;
                        int limit = total - m;

                        int pos = 0;

                        while (pos <= limit)
                        {
                            int j = m - 1;

                            while (j >= 0 && buffer[pos + j] == pattern[j])
                            {
                                    j--;
                            }

                            if (j < 0)
                            {
                                long realPos = filePos + pos - overlap;
                                if (realPos >= 0)
                                {
                                    positions.Add(realPos);
                                    if (positions.Count >= maxSearchCount)
                                    {
                                        found = true;
                                        break;
                                    }
                                }

                                pos++;
                            }
                            else
                            {
                                pos += skip[buffer[pos + m - 1]];
                            }
                        }

                        if (found) break;

                        if (overlap > 0)
                        {
                                Buffer.BlockCopy(buffer, total - overlap, buffer, 0, overlap);
                        }

                        filePos += bytesRead;
                    }

                    positions.Add(reader.BaseStream.Length);
                    return positions;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

#if NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool FastMatch(byte* data, byte* pattern, int length)
        {
            int i = 0;

            while (length - i >= 8)
            {
                if (*(ulong*)(data + i) != *(ulong*)(pattern + i))
                {
                        return false;
                }

                i += 8;
            }

            if (length - i >= 4)
            {
                if (*(uint*)(data + i) != *(uint*)(pattern + i))
                {
                        return false;
                }

                i += 4;
            }

            while (i < length)
            {
                if (data[i] != pattern[i])
                {
                        return false;
                }

                i++;
            }

            return true;
        }
#endif

        static List<long> DiffAdjacent(List<long> list)
        {
            var diffs = new List<long>();
            if (list.Count <= 1)
            {
                return diffs;
            }

            for (int i = 1; i < list.Count; i++)
            {
                diffs.Add(list[i] - list[i - 1]);
            }

            return diffs;
        }

    }
}
