using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WzComparerR2.WzLib
{
    public static class ImgNameContainer
    {
        private static readonly Dictionary<string, Queue<string>> Names = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object SyncRoot = new();

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

                string text = File.ReadAllText("imgs.bin");
                string[] values = text.Split('|');
                Names.Clear();

                for (int i = 0; i + 1 < values.Length; i += 2)
                {
                    string fullpath = NormalizeFullPath(values[i].Trim('\r', '\n'));
                    string name = values[i + 1].Trim('\r', '\n');
                    if (name.Length == 0)
                    {
                        continue;
                    }

                    if (!Names.TryGetValue(fullpath, out Queue<string> names))
                    {
                        names = new Queue<string>();
                        Names.Add(fullpath, names);
                    }

                    names.Enqueue(name);
                }
            }
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
