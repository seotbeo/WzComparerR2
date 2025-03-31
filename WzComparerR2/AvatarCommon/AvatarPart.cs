using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using WzComparerR2.CharaSim;
using WzComparerR2.WzLib;
using System.Text.RegularExpressions;

namespace WzComparerR2.AvatarCommon
{
    public class AvatarPart
    {
        public AvatarPart(Wz_Node node)
        {
            this.Node = node;
            this.Visible = true;
            this.EffectVisible = true;
            this.LoadInfo();
            this.LoadMixNodes();
            this.MixColor = this.BaseColor;
            this.ForceAction = false;
            this.GroupCount = 0;
            this.GroupTamingID = new List<int>();
            this.GroupBodyRelMove = new List<Wz_Vector>();
        }

        public AvatarPart(Wz_Node node, BitmapOrigin forceIcon, int forceID, bool isSkill) : this (node)
        {
            this.Icon = forceIcon;
            this.ID = forceID;
            this.IsSkill = isSkill;
        }

        public Wz_Node Node { get; private set; }
        public Wz_Node GroupActionNode { get; set; }
        public int GroupCount { get; set; }
        public List<int> GroupTamingID { get; set; }
        public List<Wz_Vector> GroupBodyRelMove { get; set; }
        public string ISlot { get; private set; }
        public string VSlot { get; private set; }
        public BitmapOrigin Icon { get; private set; }
        public bool Visible { get; set; }
        public bool EffectVisible { get; set; }
        public int? ID { get; private set; }
        public bool IsSkill { get; private set; }
        public Wz_Vector BodyRelMove { get; set; }
        public bool ForceAction { get; set; }
        public Wz_Node[] MixNodes { get; set; }
        public int BaseColor
        {
            get
            {
                GearType type = Gear.GetGearType(ID.Value);
                if (Gear.IsFace(type))
                {
                    return ID.Value / 100 % 10;
                }
                if (Gear.IsHair(type))
                {
                    return ID.Value % 10;
                }
                return -1;
            } 
        }
        public int MixColor { get; set; }
        public int MixOpacity { get; set; }
        public bool IsMixing { get { return BaseColor != -1 && BaseColor != MixColor && MixOpacity > 0; } }
        public Wz_Node EffectNode { get; set; }

        private void LoadInfo()
        {
            var m = Regex.Match(Node.Text, @"^(\d+)\.img$");
            if (!m.Success)
            {
                m = Regex.Match(Node.FullPath, @"^\d+\.img\\(\d+)$");
            }
            if (m.Success)
            {
                this.ID = Convert.ToInt32(m.Result("$1"));
                GearType type = Gear.GetGearType(this.ID.Value);
                if (Gear.IsFace(type))
                {
                    Icon = BitmapOrigin.CreateFromNode(PluginBase.PluginManager.FindWz(@"Item\Install\0380.img\03801284\info\icon"), PluginBase.PluginManager.FindWz);
                }
                if (Gear.IsHair(type))
                {
                    Icon = BitmapOrigin.CreateFromNode(PluginBase.PluginManager.FindWz(@"Item\Install\0380.img\03801283\info\icon"), PluginBase.PluginManager.FindWz);
                }
                if (type == GearType.head)
                {
                    Icon = BitmapOrigin.CreateFromNode(PluginBase.PluginManager.FindWz(@"Item\Install\0380.img\03801577\info\icon"), PluginBase.PluginManager.FindWz);
                }
                this.EffectNode = PluginBase.PluginManager.FindWz("Effect/ItemEff.img/" + this.ID + "/effect");
            }

            Wz_Node infoNode = this.Node.FindNodeByPath("info");
            if (infoNode == null)
            {
                return;
            }

            foreach (var node in infoNode.Nodes)
            {
                switch (node.Text)
                {
                    case "islot":
                        this.ISlot = node.GetValue<string>();
                        break;

                    case "vslot":
                        this.VSlot = node.GetValue<string>();
                        break;

                    case "icon":
                        this.Icon = BitmapOrigin.CreateFromNode(node, PluginBase.PluginManager.FindWz);
                        break;
                }
            }
        }

        public void LoadChairEffectNode()
        {
            this.EffectNode = (this.ID / 10000 == 301) ? PluginBase.PluginManager.FindWz("Effect/ItemEff.img/" + this.ID) : this.EffectNode;
        }

        public void LoadEffectEffectNode()
        {
            this.EffectNode = this.Node;
        }

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

        private void LoadMixNodes()
        {
            this.MixNodes = new Wz_Node[8];

            string dir;
            int baseID;
            int multiplier;

            GearType type = Gear.GetGearType(this.ID.Value);
            if (Gear.IsFace(type))
            {
                dir = "Face";
                baseID = this.ID.Value / 1000 * 1000 + this.ID.Value % 100;
                multiplier = 100;
            }
            else if (Gear.IsHair(type))
            {
                dir = "Hair";
                baseID = this.ID.Value / 10 * 10;
                multiplier = 1;
            }
            else
            {
                return;
            }

            for (int i = 0; i <= 7; i++)
            {
                this.MixNodes[i] = PluginBase.PluginManager.FindWz(string.Format(@"Character\{0}\{1:D8}.img", dir, baseID + i * multiplier));
            }
            if (this.MixNodes[0] == null)
            {
                this.MixNodes[0] = PluginBase.PluginManager.FindWz(string.Format(@"Character\{0}\{1:D8}.img", dir, baseID + 8 * multiplier));
            }
        }
    }
}
