using WzComparerR2.WzLib;

namespace WzComparerR2.UnityLib.Classic
{
    public sealed class WzJs_File : Wz_File
    {
        internal WzJs_File(WzJs_Structure owner, Wz_Node node, Wz_Type? type = null)
            : base(node, owner.WzStructure, type ?? ClassicWzTypeMap.GetWzType(node.Text), owner.ReadLock)
        {
            this.Owner = owner;
        }

        public string Version => this.Owner.ClientVersion;

        internal WzJs_Structure Owner { get; }
        internal ClassicWzSource Source => this.Owner.Source;
    }
}
