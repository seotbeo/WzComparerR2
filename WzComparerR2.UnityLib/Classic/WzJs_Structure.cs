using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using WzComparerR2.WzLib;

namespace WzComparerR2.UnityLib.Classic
{
    public sealed class WzJs_Structure : IDisposable
    {
        public WzJs_Structure(string bundleDirectory, Wz_Structure structure)
        {
            this.BundleDirectory = ClassicCatalog.ResolveBundleDirectory(bundleDirectory);
            this.WzStructure = structure;
            this.Source = ClassicWzSource.Open(this.BundleDirectory);
            try
            {
                this.ClientVersion = UnityClientInfo.Resolve(this.BundleDirectory).Version;
                foreach (var entry in this.Source.Catalog.Entries)
                {
                    if (entry.Value.ClassId != Unity.UnityClassIds.AudioClip
                        || !entry.Key.StartsWith("Sound/", StringComparison.OrdinalIgnoreCase))
                        continue;

                    int separator = entry.Key.IndexOf('/', "Sound/".Length);
                    if (separator < 0)
                        continue;

                    string imageKey = entry.Key.Substring(0, separator);
                    if (!this.SoundImages.TryGetValue(imageKey, out var clips))
                        this.SoundImages.Add(imageKey, clips = new List<string>());

                    clips.Add(entry.Key);
                }

                this.Node = ClassicWzBridge.CreateCatalogNode(this.Source, false, false, this);
                var rootFile = new WzJs_File(this, this.Node, Wz_Type.Base);
                this.Node.Value = rootFile;
                this.filesByNode.Add(this.Node, rootFile);
                this.ImageCount = this.SoundImages.Count + this.Source.Catalog.Entries.Count(entry =>
                    entry.Value.ClassId == Unity.UnityClassIds.MonoBehaviour || entry.Value.ClassId == Unity.UnityClassIds.TextAsset);
            }
            catch
            {
                this.Source.Dispose();
                throw;
            }
        }

        public string BundleDirectory { get; }
        public Wz_Structure WzStructure { get; }
        public Wz_Node Node { get; }
        public int ImageCount { get; }
        internal string ClientVersion { get; }
        internal Dictionary<string, List<string>> SoundImages { get; } = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        internal ClassicWzSource Source { get; }
        internal object ReadLock { get; } = new object();

        private readonly Dictionary<Wz_Node, WzJs_File> filesByNode = new Dictionary<Wz_Node, WzJs_File>();
        public IEnumerable<WzJs_File> Files => this.filesByNode.Values;

        internal WzJs_File GetOrAddFile(Wz_Node category)
        {
            if (!this.filesByNode.TryGetValue(category, out var file))
            {
                file = new WzJs_File(this, category);
                this.filesByNode.Add(category, file);
                category.Value = file;
            }
            return file;
        }

        public void Dispose()
        {
            lock (this.ReadLock)
            {
                this.Source.Dispose();
            }
        }
    }
}
