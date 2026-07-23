using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.WzLib;

namespace WzComparerR2.AvatarCommon
{
    public class AvatarFrameData
    {
        public AvatarFrameData(Wz_Node frameNode, Wz_Node mixFrameNode, int mixRatio, AvatarPart part,
            Dictionary<string, string> customOriginMap = null, bool isBodyPart = false, bool applyAvatarScale = true)
        {
            this.FrameNode = frameNode;
            this.MixFrameNode = mixFrameNode;
            this.MixRatio = mixRatio;
            this.Part = part;
            this.IsBodyPart = isBodyPart;
            this.ApplyAvatarScale = applyAvatarScale;
        }

        public AvatarPart Part { get; private set; }
        public Wz_Node FrameNode { get; private set; }
        public Wz_Node MixFrameNode { get; private set; }
        public int MixRatio { get; private set; }
        public PrismDataCollection PrismData { get { return this.Part?.PrismData ?? new PrismDataCollection(); } }
        public bool IsBodyPart { get; private set; }
        public bool ApplyAvatarScale { get; private set; }
        public Dictionary<string, string> CustomOriginMap { get { return this.Part?.CustomOriginMap ?? new(); } }
    }
}
