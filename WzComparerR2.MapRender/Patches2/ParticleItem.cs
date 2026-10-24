using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.WzLib;
using WzComparerR2.MapRender.Particles;

namespace WzComparerR2.MapRender.Patches2
{
    class ParticleItem : SceneItem
    {
        public string ParticleName { get; set; }
        public string Path { get; set; }
        public int Rx { get; set; }
        public int Ry { get; set; }
        public int Z { get; set; }
        public SubParticleItem[] SubItems { get; set; }
        public List<QuestInfo> Quest { get; private set; } = new List<QuestInfo>();
        public ItemView View { get; set; }

        private long Depth => (long)this.Z + 1073741824;
        private bool UsesEffectLayer => this.Z >= 0 || (this.Depth > 242000 && (this.Depth < 270000 || this.Depth >= 1280000));

        internal ContainerNode GetRenderContainer(MapScene scene)
        {
            if (this.UsesEffectLayer)
            {
                return scene.Effect;
            }
            if (this.Depth < 0)
            {
                return scene.Back;
            }
            if (this.Depth >= 270000)
            {
                return scene.Front;
            }
            int layer = (int)(this.Depth / 30000);
            return layer < 8 ? ((LayerNode)scene.Layers.Nodes[layer]).Obj : scene.TopObj;
        }

        internal void GetRenderZ(out int z, out int order)
        {
            if (this.UsesEffectLayer)
            {
                z = this.Z;
                order = 0;
                return;
            }
            if (this.Depth < 0 || this.Depth >= 270000)
            {
                long relative = this.Depth < 0 ? this.Depth + 128000 : this.Depth - 270000;
                z = (int)Math.Floor(relative / 1000.0);
                order = (int)(relative - (long)z * 1000);
                return;
            }
            z = (int)(this.Depth % 30000) - 2000;
            order = int.MaxValue;
        }

        internal ParticleItem ForPlacement(SubParticleItem placement)
        {
            var item = new ParticleItem
            {
                Name = this.Name, ParticleName = this.ParticleName, Path = placement.Path ?? this.Path, Rx = placement.Rx, Ry = placement.Ry,
                Z = placement.Z, Tags = placement.Tags ?? this.Tags, SubItems = new[] { placement }
            };
            item.Quest.AddRange(this.Quest);
            return item;
        }

        public static ParticleItem LoadFromNode(Wz_Node node)
        {
            var item = new ParticleItem()
            {
                ParticleName = node.Text,
                Path = node.Nodes["path"].GetValueEx<string>(null),
                Rx = node.Nodes["rx"].GetValueEx(-100),
                Ry = node.Nodes["ry"].GetValueEx(-100),
                Z = node.Nodes["z"].GetValueEx(0),
                Tags = ReadTags(node)
            };

            if (node.Nodes["quest"] != null)
            {
                foreach (Wz_Node questNode in node.Nodes["quest"].Nodes)
                {
                    if (int.TryParse(questNode.Text, out int questID))
                    {
                        item.Quest.Add(new QuestInfo(questID, Convert.ToInt32(questNode.Value)));
                    }
                }
            }

            var subItems = new List<SubParticleItem>();
            for (int i = 0; ; i++)
            {
                var subNode = node.Nodes[i.ToString()];
                if (subNode == null)
                {
                    break;
                }
                var subitem = new SubParticleItem()
                {
                    X = subNode.Nodes["x"].GetValueEx(0),
                    Y = subNode.Nodes["y"].GetValueEx(0),
                    Rx = subNode.Nodes["rx"].GetValueEx(item.Rx),
                    Ry = subNode.Nodes["ry"].GetValueEx(item.Ry),
                    Z = subNode.Nodes["z"].GetValueEx(item.Z),
                    StartHidden = subNode.Nodes["startHidden"].GetValueEx(false),
                    Path = subNode.Nodes["path"].GetValueEx(item.Path),
                    Tags = ReadTags(subNode, item.Tags)
                };

                if (subNode.Nodes["quest"] != null)
                {
                    foreach (Wz_Node questNode in subNode.Nodes["quest"].Nodes)
                    {
                        if (int.TryParse(questNode.Text, out int questID))
                        {
                            subitem.Quest.Add(new QuestInfo(questID, Convert.ToInt32(questNode.Value)));
                        }
                    }
                }
                subItems.Add(subitem);
            }

            if (subItems.Count <= 0)
            {
                subItems.Add(new SubParticleItem()
                {
                    X = node.Nodes["x"].GetValueEx(0),
                    Y = node.Nodes["y"].GetValueEx(0),
                    Rx = item.Rx, Ry = item.Ry, Z = item.Z,
                    Tags = item.Tags,
                    Path = item.Path, StartHidden = node.Nodes["startHidden"].GetValueEx(false),
                });
            }
            item.SubItems = subItems.ToArray();
            return item;
        }

        private static string[] ReadTags(Wz_Node node, string[] fallback = null)
        {
            string tags = node.Nodes["tags"].GetValueEx<string>(null);
            return tags == null ? fallback : tags.Split(',').Select(tag => tag.Trim()).Where(tag => tag.Length > 0).Distinct().ToArray();
        }

        public class SubParticleItem
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Rx { get; set; }
            public int Ry { get; set; }
            public int Z { get; set; }
            public string Path { get; set; }
            public bool StartHidden { get; set; }
            public string[] Tags { get; set; }
            public List<QuestInfo> Quest { get; private set; } = new List<QuestInfo>();
        }

        public class ItemView
        {
            public ParticleSystem ParticleSystem { get; set; }
        }
    }
}
