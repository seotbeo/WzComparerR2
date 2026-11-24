namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// 單一物件在 catalog 中的位置（bundle + node + 物件資料的絕對 offset）。
    /// </summary>
    public sealed class UnityEntryLocation
    {
        public UnityEntryLocation(int bundleIndex, int nodeIndex, long absoluteOffset, uint byteSize, int classId)
        {
            this.BundleIndex = bundleIndex;
            this.NodeIndex = nodeIndex;
            this.AbsoluteOffset = absoluteOffset;
            this.ByteSize = byteSize;
            this.ClassId = classId;
        }

        public int BundleIndex { get; private set; }

        public int NodeIndex { get; private set; }

        /// <summary>物件資料在 node 內的絕對 offset（已含 SerializedFile dataOffset）。</summary>
        public long AbsoluteOffset { get; private set; }

        public uint ByteSize { get; private set; }

        /// <summary>資產種類：114 = MonoBehaviour(WZJS)、49 = TextAsset、83 = AudioClip、28 = Texture2D。</summary>
        public int ClassId { get; private set; }
    }
}
