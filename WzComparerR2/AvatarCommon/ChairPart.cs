using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.WzLib;

namespace WzComparerR2.AvatarCommon
{
    public class ChairPart : AvatarPart
    {
        public ChairPart(Wz_Node node, BitmapOrigin forceIcon, int forceID, bool isSkill) : base(node, forceIcon, forceID, isSkill)
        {
            this.ForceAction = false;
            this.GroupCount = 0;
            this.GroupTamingID = new List<int>();
            this.GroupBodyRelMove = new List<Wz_Vector>();
            this.CustomChairAvatarScale = 100;
        }

        public Wz_Node GroupActionNode { get; set; }
        public int GroupCount { get; set; }
        public List<int> GroupTamingID { get; set; }
        public List<Wz_Vector> GroupBodyRelMove { get; set; }
        public Wz_Node CustomChairInfoNode { get; set; }
        public CustomChairType CustomChairType { get; set; }
        public int CustomChairAvatarScale { get; set; }
        public bool ForceAction { get; set; }

        public void LoadGroupTaming()
        {
            if (this.GroupActionNode != null)
            {
                this.GroupBodyRelMove.Clear();
                for (int i = 0; i <= Convert.ToInt32(this.GroupCount); i++)
                {
                    var groupNode = this.GroupActionNode.FindNodeByPath(i.ToString());
                    if (groupNode != null)
                    {
                        int tamingMobID = groupNode.FindNodeByPath("tamingMobM")?.GetValueEx<int>(0)
                        ?? groupNode.FindNodeByPath("tamingMobF")?.GetValueEx<int>(0)
                        ?? groupNode.FindNodeByPath("tamingMob")?.GetValueEx<int>(0) ?? 0;
                        var brm = groupNode.FindNodeByPath("bodyRelMove").GetValueEx<Wz_Vector>(null);

                        this.GroupTamingID.Add(tamingMobID);
                        this.GroupBodyRelMove.Add(brm);
                    }
                }
            }
        }
    }

    public enum CustomChairType
    {
        Unsupported = 0,
        ScaleAvatarChair = 1
    }
}
