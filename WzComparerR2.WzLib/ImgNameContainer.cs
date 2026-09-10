using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WzComparerR2.WzLib
{
    public static class ImgNameContainer
    {
        private static readonly Dictionary<string, Queue<string>> Names = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object SyncRoot = new();
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

        public static int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return Names.Values.Sum(names => names.Count);
                }
            }
        }

        public static void Load()
        {
            lock (SyncRoot)
            {
                if (!File.Exists("imgs.bin"))
                {
                    return;
                }

                var loadedNames = new Dictionary<string, Queue<string>>(StringComparer.OrdinalIgnoreCase);
                using var stream = File.OpenRead("imgs.bin");
                using var reader = new BinaryReader(stream, Utf8);
                int pathCount = reader.ReadInt32();
                if (pathCount < 0 || pathCount > stream.Length / 8)
                {
                    throw new InvalidDataException($"Invalid path count: {pathCount}");
                }

                for (int i = 0; i < pathCount; i++)
                {
                    string fullpath = NormalizeFullPath(ReadString(reader));
                    int nameCount = reader.ReadInt32();
                    if (nameCount < 0 || nameCount > (stream.Length - stream.Position) / 4)
                    {
                        throw new InvalidDataException($"Invalid name count: {nameCount}");
                    }

                    if (!loadedNames.TryGetValue(fullpath, out Queue<string> names))
                    {
                        names = new Queue<string>();
                        loadedNames.Add(fullpath, names);
                    }

                    for (int j = 0; j < nameCount; j++)
                    {
                        names.Enqueue(ReadString(reader));
                    }
                }

                if (stream.Position != stream.Length)
                {
                    throw new InvalidDataException("Unexpected trailing data in imgs.bin.");
                }

                Names.Clear();
                foreach (KeyValuePair<string, Queue<string>> entry in loadedNames)
                {
                    Names.Add(entry.Key, entry.Value);
                }
            }
        }

        private static string ReadString(BinaryReader reader)
        {
            int byteLength = reader.ReadInt32();
            long remainingLength = reader.BaseStream.Length - reader.BaseStream.Position;
            if (byteLength < 0 || byteLength > remainingLength)
            {
                throw new InvalidDataException($"Invalid string byte length: {byteLength}");
            }

            byte[] bytes = reader.ReadBytes(byteLength);
            if (bytes.Length != byteLength)
            {
                throw new EndOfStreamException();
            }

            return Utf8.GetString(bytes);
        }

        public static bool TryTake(string fullpath, int length, ReadOnlySpan<char> hints, out string name)
        {
            lock (SyncRoot)
            {
                name = null;
                if (!Names.TryGetValue(NormalizeFullPath(fullpath), out Queue<string> names) || names.Count == 0)
                {
                    return false;
                }

                int nameCount = names.Count;
                for (int i = 0; i < nameCount; i++)
                {
                    string candidate = names.Dequeue();
                    if (name == null && Matches(candidate, length, hints))
                    {
                        name = candidate;
                    }
                    else
                    {
                        names.Enqueue(candidate);
                    }
                }

                return name != null;
            }
        }

        public static string NormalizeFullPath(string fullpath)
        {
            string path = (fullpath ?? string.Empty).Replace('/', '\\');
            int dataIndex = path.IndexOf("\\Data\\", StringComparison.OrdinalIgnoreCase);
            string normalizedPath = (dataIndex >= 0 ? path.Substring(dataIndex + 1) : path).TrimEnd('\\');
            return Path.HasExtension(normalizedPath) ? Path.GetDirectoryName(normalizedPath) ?? string.Empty : normalizedPath;
        }

        private static bool Matches(string candidate, int length, ReadOnlySpan<char> hints)
        {
            if (candidate.Length != length)
            {
                return false;
            }

            int hintStart = Math.Max(0, length - 13);
            int hintEnd = Math.Max(0, length - 8);
            for (int i = hintStart; i < hintEnd; i++)
            {
                if (hints[i] != '\0' && candidate[i] != hints[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
