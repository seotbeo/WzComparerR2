using System;
using System.IO;
using System.Collections.Generic;
using WzComparerR2.WzLib;

namespace WzComparerR2.UnityLib.Classic
{
    public sealed class WzJs_Image : Wz_Image
    {
        private readonly WzJs_Structure structure;
        private bool extracted;
        private Dictionary<SpriteKey, SpriteReference> sprites;

        private readonly struct SpriteKey : IEquatable<SpriteKey>
        {
            public SpriteKey(string path, string region)
            {
                this.Path = path;
                this.Region = region;
            }

            public string Path { get; }
            public string Region { get; }

            public bool Equals(SpriteKey other) =>
                string.Equals(this.Path, other.Path, StringComparison.Ordinal)
                && string.Equals(this.Region, other.Region, StringComparison.Ordinal);

            public override bool Equals(object obj) => obj is SpriteKey other && this.Equals(other);

            public override int GetHashCode()
            {
                int pathHash = this.Path == null ? 0 : StringComparer.Ordinal.GetHashCode(this.Path);
                int regionHash = this.Region == null ? 0 : StringComparer.Ordinal.GetHashCode(this.Region);
                return unchecked((pathHash * 397) ^ regionHash);
            }
        }

        private readonly struct SpriteReference
        {
            public SpriteReference(string path, Wz_Vector origin, int delay)
            {
                this.Path = path;
                this.Origin = origin;
                this.Delay = delay;
            }

            public string Path { get; }
            public Wz_Vector Origin { get; }
            public int Delay { get; }
        }

        internal WzJs_Image(WzJs_Structure structure, WzJs_File file, Wz_Node owner, string key, int size)
            : base(owner.Text, size, 0, 0, 0, file)
        {
            this.structure = structure;
            this.OwnerNode = owner;
            this.CatalogKey = key;
            this.IgnoreChecksum = true;
        }

        internal UnityPrefab Prefab { get; set; }
        public bool IsPrefab => this.Prefab != null;
        public string CatalogKey { get; }
        public override bool IsExtracted => this.extracted;

        internal void AddSprite(string relativePath, string catalogPath, Wz_Vector origin, int delay, string region)
        {
            if (this.sprites == null)
                this.sprites = new Dictionary<SpriteKey, SpriteReference>();

            this.sprites[new SpriteKey(relativePath, region)] = new SpriteReference(catalogPath, origin, delay);
        }

        private void AppendSprites()
        {
            if (this.sprites == null)
                return;

            var delays = new Dictionary<Wz_Node, int>();
            foreach (var sprite in this.sprites)
            {
                Wz_Node node = this.Node;
                foreach (string part in sprite.Key.Path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
                    node = node.Nodes[part] ?? node.Nodes.Add(part);

                if (ReferenceEquals(node, this.Node))
                    node = node.Nodes[this.Name] ?? node.Nodes.Add(this.Name);

                if (node.Value == null)
                    node.Value = new UnityCanvas(this.structure.Source, sprite.Value.Path);

                if (node.Value is UnityCanvas canvas)
                {
                    if (sprite.Key.Region != null)
                    {
                        var variant = new UnityCanvas(this.structure.Source, sprite.Value.Path);
                        string region = string.Equals(sprite.Key.Region, "Common", StringComparison.OrdinalIgnoreCase)
                            ? "Common" : sprite.Key.Region;
                        if (canvas.AddVariant(region, variant))
                        {
                            if (sprite.Value.Origin != null)
                                (node.Nodes["origin"] ?? node.Nodes.Add("origin")).Value = sprite.Value.Origin;
                            if (sprite.Value.Delay > 0)
                                delays[node] = sprite.Value.Delay;
                            else
                                delays.Remove(node);
                        }
                    }
                    else
                    {
                        if (sprite.Value.Origin != null && node.Nodes["origin"] == null)
                            node.Nodes.Add("origin").Value = sprite.Value.Origin;
                        if (canvas.DefaultRegion == null && sprite.Value.Delay > 0)
                            delays[node] = sprite.Value.Delay;
                    }
                }
            }
            foreach (var delay in delays)
            {
                if (delay.Key.Nodes["delay"] == null)
                    delay.Key.Nodes.Add("delay").Value = delay.Value;
            }
        }

        public override bool TryExtract(out Exception error)
        {
            lock (this.structure.ReadLock)
            {
                error = null;
                if (this.extracted)
                    return true;

                try
                {
                    if (this.Prefab != null)
                    {
                        this.Prefab.Populate(this.Node, this.structure.Source);
                    }
                    else if (this.structure.SoundImages.TryGetValue(this.CatalogKey, out var clips))
                    {
                        foreach (string key in clips)
                        {
                            string[] segments = key.Substring(this.CatalogKey.Length + 1).Split('/');
                            Wz_Node parent = this.Node;
                            for (int i = 0; i < segments.Length - 1; i++)
                                parent = parent.Nodes[segments[i]] ?? parent.Nodes.Add(segments[i]);

                            parent.Nodes.Add(segments[segments.Length - 1]).Value = this.structure.Source.CreateSound(key, this);
                        }
                        this.AppendSoundAliases();
                    }
                    else
                    {
                        ClassicWzBridge.AppendEntryContents(this.Node, this.structure.Source, this.CatalogKey);
                    }
                    this.AppendSprites();
                    this.extracted = true;
                    return true;
                }
                catch (Exception ex) when (ex is Unity.UnityException || ex is Wzjs.WzjsException
                    || ex is ClassicAssetException || ex is IOException)
                {
                    this.Node.Nodes.Clear();
                    error = ex;
                    return false;
                }
            }
        }

        private void AppendSoundAliases()
        {
            string tableKey = this.CatalogKey + "Table";
            if (!this.structure.Source.Catalog.Entries.ContainsKey(tableKey))
                return;

            var table = UnityJson.Parse(this.structure.Source.ReadTextAsset(tableKey).Script, tableKey);
            this.AppendSoundAliases(table, this.Node, string.Empty);
        }

        private void AppendSoundAliases(Wz_Node table, Wz_Node sound, string relativePrefix)
        {
            foreach (Wz_Node entry in table.Nodes)
            {
                var existing = sound.Nodes[entry.Text];
                if (entry.Value is string target)
                {
                    string prefix = this.CatalogKey + "/";
                    if (existing != null || !target.StartsWith(prefix, StringComparison.Ordinal)
                        || !this.structure.Source.Catalog.Entries.TryGetValue(target, out var location)
                        || location.ClassId != Unity.UnityClassIds.AudioClip)
                        continue;

                    sound.Nodes.Add(entry.Text).Value = new Wz_Uol(relativePrefix + target.Substring(prefix.Length));
                }
                else if (entry.Nodes.Count > 0 && (existing == null || existing.Value == null))
                {
                    var folder = existing ?? new Wz_Node(entry.Text);
                    this.AppendSoundAliases(entry, folder, relativePrefix + "../");
                    if (existing == null && folder.Nodes.Count > 0)
                        sound.Nodes.Add(folder);
                }
            }
        }

        public override void Unextract()
        {
            lock (this.structure.ReadLock)
            {
                base.Unextract();
                this.extracted = false;
            }
        }

        public override Stream OpenRead()
        {
            lock (this.structure.ReadLock)
            {
                if (this.structure.SoundImages.ContainsKey(this.CatalogKey))
                    throw new NotSupportedException("Sound 이미지 그룹은 단일 데이터 스트림을 제공하지 않습니다.");

                if (!this.structure.Source.Catalog.Entries.TryGetValue(this.CatalogKey, out var location))
                    throw new NotSupportedException("A sprite group does not have a single source stream.");

                return new MemoryStream(this.structure.Source.ReadEntry(location), false);
            }
        }
    }
}
