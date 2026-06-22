using System;
using System.Collections;
using System.Collections.Generic;

namespace WzComparerR2.WzLib.Compatibility
{
    public class WzProfileCacheEntry
    {
        public WzProfileCacheEntry(string profileName, int wzVersion, ulong hashKey)
        {
            this.ProfileName = profileName;
            this.WzVersion = wzVersion;
            this.HashKey = hashKey;
            this.HitCount = 0;
            this.LastUsedUtc = DateTimeOffset.UtcNow;
        }

        public string ProfileName { get; }
        public int WzVersion { get; }
        public ulong HashKey { get; }
        public long HitCount { get; private set; }
        public DateTimeOffset LastUsedUtc { get; private set; }

        internal void MarkHit()
        {
            this.HitCount++;
            this.LastUsedUtc = DateTimeOffset.UtcNow;
        }
    }

    internal sealed class WzProfileDetectionCache : IEnumerable<WzProfileCacheEntry>
    {
        private const int MaxEntriesPerProfile = 32;
        private readonly Dictionary<string, List<WzProfileCacheEntry>> entriesByProfile = new(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<WzProfileCacheEntry> GetCandidates(string profileName)
        {
            if (profileName == null || !this.entriesByProfile.TryGetValue(profileName, out var entries))
                return Array.Empty<WzProfileCacheEntry>();
            return entries;
        }

        public void Upsert(WzProfileCacheEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (!this.entriesByProfile.TryGetValue(entry.ProfileName, out var entries))
            {
                entries = new List<WzProfileCacheEntry>();
                this.entriesByProfile.Add(entry.ProfileName, entries);
            }

            for (int i = 0; i < entries.Count; i++)
            {
                var existing = entries[i];
                if (existing.WzVersion == entry.WzVersion && existing.HashKey == entry.HashKey)
                {
                    existing.MarkHit();
                    return;
                }
            }

            entry.MarkHit();
            entries.Add(entry);
            Trim(entries);
        }

        public void MarkHit(WzProfileCacheEntry entry)
        {
            entry?.MarkHit();
        }

        public void Clear()
        {
            this.entriesByProfile.Clear();
        }

        public IEnumerator<WzProfileCacheEntry> GetEnumerator()
        {
            foreach (var entries in this.entriesByProfile.Values)
            {
                foreach (var entry in entries)
                {
                    yield return entry;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private static void Trim(List<WzProfileCacheEntry> entries)
        {
            while (entries.Count > MaxEntriesPerProfile)
            {
                int removeIndex = 0;
                for (int i = 1; i < entries.Count; i++)
                {
                    var candidate = entries[i];
                    var current = entries[removeIndex];
                    if (candidate.HitCount < current.HitCount
                        || (candidate.HitCount == current.HitCount && candidate.LastUsedUtc < current.LastUsedUtc))
                    {
                        removeIndex = i;
                    }
                }
                entries.RemoveAt(removeIndex);
            }
        }
    }
}
