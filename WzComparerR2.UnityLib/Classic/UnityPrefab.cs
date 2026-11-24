using System;
using System.Collections.Generic;
using WzComparerR2.UnityLib.Unity;
using WzComparerR2.WzLib;

namespace WzComparerR2.UnityLib.Classic
{
    internal sealed class UnityPrefab
    {
        private readonly int bundleIndex;
        private readonly int nodeIndex;
        private readonly UnitySerializedFile file;
        private readonly Dictionary<long, UnityObjectInfo> objects;
        private readonly long rootId;

        public UnityPrefab(int bundleIndex, int nodeIndex, UnitySerializedFile file,
            Dictionary<long, UnityObjectInfo> objects, long rootId)
        {
            this.bundleIndex = bundleIndex;
            this.nodeIndex = nodeIndex;
            this.file = file;
            this.objects = objects;
            this.rootId = rootId;
        }

        internal UnitySerializedFile SerializedFile => this.file;

        internal void AppendImageAssets(Wz_Node root, ClassicWzSource source)
        {
            string bundle = source.Catalog.Bundles[this.bundleIndex].Name;
            string fileName = bundle + " [" + this.nodeIndex + "]";
            foreach (var obj in this.file.Objects)
            {
                if (obj.ClassId != 213 && obj.ClassId != 28)
                    continue;

                string folderName = obj.ClassId == 213 ? ClassicWzBridge.SpriteNodeName : ClassicWzBridge.TextureNodeName;
                Wz_Node folder = root.Nodes[folderName] ?? root.Nodes.Add(folderName);
                Wz_Node fileNode = folder.Nodes[fileName] ?? folder.Nodes.Add(fileName);
                UnityEntryLocation location = this.GetLocation(obj);
                string name = this.ReadImageName(source, location);
                string caption = string.IsNullOrWhiteSpace(name) ? obj.PathId.ToString() : name + " [" + obj.PathId + "]";
                string path = folderName + "/" + fileName + "/" + caption;
                fileNode.Nodes.Add(caption).Value = new UnityCanvas(source, path, () =>
                    obj.ClassId == 28 ? source.RenderPrefabTexture(location) : this.RenderSprite(source, location));
            }
        }

        private string ReadImageName(ClassicWzSource source, UnityEntryLocation location)
        {
            var header = new UnityEntryLocation(location.BundleIndex, location.NodeIndex,
                location.AbsoluteOffset, Math.Min(location.ByteSize, 1024u), location.ClassId);
            var reader = new UnityByteReader(source.ReadEntry(header), this.file.IsLittleEndian);
            try
            {
                return reader.AlignedString();
            }
            catch (Exception ex) when (ex is UnityException || ex is OverflowException)
            {
                return null;
            }
        }

        private byte[] RenderSprite(ClassicWzSource source, UnityEntryLocation location)
        {
            var sprite = UnityObjectParsers.ParseSprite(source.ReadEntry(location), this.file.IsLittleEndian);
            if (sprite.Texture.FileId != 0 || !this.objects.TryGetValue(sprite.Texture.PathId, out var texture)
                || texture.ClassId != 28)
                return null;

            return source.RenderPrefabSprite(location, this.GetLocation(texture));
        }

        public void Populate(Wz_Node target, ClassicWzSource source)
        {
            this.AppendGameObject(target, source, this.rootId, new HashSet<long>(), 0);
        }

        private void AppendGameObject(Wz_Node parent, ClassicWzSource source, long id, HashSet<long> visited, int depth)
        {
            if (depth >= 256 || !visited.Add(id))
                throw new UnityException("Invalid prefab GameObject hierarchy.");
            if (!this.objects.TryGetValue(id, out var obj) || obj.ClassId != 1)
                return;

            var reader = this.ReadObject(source, obj);
            int count = ReadPointerCount(reader);
            var components = new List<UnityPPtr>(count);
            for (int i = 0; i < count; i++)
                components.Add(ReadPointer(reader));

            int layer = reader.I32();
            Wz_Node node = parent.Nodes.Add(reader.AlignedString());
            var objectInfo = new UnityPrefabNodeInfo("GameObject", id, 0, obj.ClassId);
            objectInfo.Fields.Add("Name: " + node.Text);
            objectInfo.Fields.Add("Layer: " + layer);
            objectInfo.Fields.Add("Components: " + count);
            node.Value = objectInfo;
            Wz_Node componentList = node.Nodes.Add("_Components");
            Wz_Node children = node.Nodes.Add("_Children");
            foreach (var pointer in components)
            {
                if (pointer.FileId != 0 || !this.objects.TryGetValue(pointer.PathId, out var component))
                {
                    componentList.Nodes.Add(componentList.Nodes.Count.ToString()).Value =
                        new UnityPrefabNodeInfo("Unresolved reference", pointer.PathId, pointer.FileId);
                    continue;
                }

                string name = component.ClassId switch
                {
                    4 => "Transform",
                    114 => "MonoBehaviour",
                    222 => "CanvasRenderer",
                    224 => "RectTransform",
                    _ => "Class " + component.ClassId,
                };
                var info = new UnityPrefabNodeInfo(name, pointer.PathId, pointer.FileId, component.ClassId);
                Wz_Node componentNode = componentList.Nodes.Add(componentList.Nodes.Count.ToString());
                componentNode.Value = info;
                if (component.ClassId == 114)
                {
                    byte[] data = source.ReadEntry(this.GetLocation(component));
                    var mono = new UnityByteReader(data, this.file.IsLittleEndian);
                    mono.Skip(12);
                    info.Fields.Add("Enabled: " + mono.Bool());
                    mono.Align4();
                    var script = ReadPointer(mono);
                    info.Fields.Add("Script: " + script.FileId + "/" + script.PathId);
                    info.Fields.Add("Name: " + mono.AlignedString());
                    this.AppendImageReferences(componentNode, source, data, mono.Position);
                }
                if (component.ClassId != 4 && component.ClassId != 224)
                    continue;

                var transform = this.ReadObject(source, component);
                transform.Skip(12);
                info.Fields.Add(FormattableString.Invariant($"Local rotation (x, y, z, w): {transform.F32()}, {transform.F32()}, {transform.F32()}, {transform.F32()}"));
                info.Fields.Add(FormattableString.Invariant($"Local position: {transform.F32()}, {transform.F32()}, {transform.F32()}"));
                info.Fields.Add(FormattableString.Invariant($"Local scale: {transform.F32()}, {transform.F32()}, {transform.F32()}"));
                int childCount = ReadPointerCount(transform);
                for (int i = 0; i < childCount; i++)
                {
                    var child = ReadPointer(transform);
                    if (child.FileId != 0 || !this.objects.TryGetValue(child.PathId, out var childTransform)
                        || (childTransform.ClassId != 4 && childTransform.ClassId != 224))
                        continue;

                    var gameObject = ReadPointer(this.ReadObject(source, childTransform));
                    if (gameObject.FileId == 0)
                        this.AppendGameObject(children, source, gameObject.PathId, visited, depth + 1);
                }
            }
        }

        private void AppendImageReferences(Wz_Node node, ClassicWzSource source, byte[] data, int payloadOffset)
        {
            var seen = new HashSet<long>();
            // Without a script schema, look for aligned local pointers to indexed image objects.
            for (int offset = payloadOffset; offset <= data.Length - 12; offset += 4)
            {
                var pointer = ReadPointer(new UnityByteReader(data, offset, 12, this.file.IsLittleEndian));
                if (pointer.FileId != 0 || !this.objects.TryGetValue(pointer.PathId, out var image)
                    || (image.ClassId != 213 && image.ClassId != 28) || !seen.Add(pointer.PathId))
                    continue;

                UnityEntryLocation location = this.GetLocation(image);
                if (image.ClassId == 28)
                {
                    string name = this.ReadImageName(source, location) ?? string.Empty;
                    node.Nodes.Add(name).Value = new UnityCanvas(source, name, () => source.RenderPrefabTexture(location));
                    continue;
                }

                UnitySpriteInfo sprite;
                try
                {
                    sprite = UnityObjectParsers.ParseSprite(source.ReadEntry(location), this.file.IsLittleEndian);
                }
                catch (UnityException)
                {
                    continue;
                }

                if (sprite.Texture.FileId != 0 || !this.objects.TryGetValue(sprite.Texture.PathId, out var texture)
                    || texture.ClassId != 28)
                    continue;

                UnityEntryLocation textureLocation = this.GetLocation(texture);
                string spriteName = sprite.Name ?? string.Empty;
                Wz_Node spriteNode = node.Nodes.Add(spriteName);
                spriteNode.Value = new UnityCanvas(source, spriteName, () => source.RenderPrefabSprite(location, textureLocation));
                string textureName = this.ReadImageName(source, textureLocation) ?? string.Empty;
                spriteNode.Nodes.Add(textureName).Value = new UnityCanvas(source, textureName, () => source.RenderPrefabTexture(textureLocation));
            }
        }

        private UnityEntryLocation GetLocation(UnityObjectInfo obj)
            => new UnityEntryLocation(this.bundleIndex, this.nodeIndex,
                this.file.DataOffset + obj.ByteStart, obj.ByteSize, obj.ClassId);

        private UnityByteReader ReadObject(ClassicWzSource source, UnityObjectInfo obj)
            => new UnityByteReader(source.ReadEntry(this.GetLocation(obj)), this.file.IsLittleEndian);

        private static int ReadPointerCount(UnityByteReader reader)
        {
            int count = reader.I32();
            if (count < 0 || count > reader.Remaining / 12)
                throw new UnityException("Invalid prefab reference count.");
            return count;
        }

        private static UnityPPtr ReadPointer(UnityByteReader reader)
            => new UnityPPtr(reader.I32(), reader.I64());
    }
}
