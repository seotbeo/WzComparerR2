using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class QuestReward
    {
        public QuestReward()
        {
            this.Items = new List<ItemIDnCount>();
        }

        public int Exp;
        public int Meso;
        public List<ItemIDnCount> Items;

        public int Count
        {
            get { return (this.Exp > 0 ? 1 : 0) + (this.Meso > 0 ? 1 : 0) + this.Items.Count; }
        }

        public bool HasValues
        {
            get { return this.Count > 0; }
        }

        public static QuestReward CreateFromNode(Wz_Node rewardNode)
        {
            if (rewardNode == null) return null;

            QuestReward questReward = new QuestReward();
            questReward.Exp = rewardNode.FindNodeByPath("exp").ResolveUol().GetValueEx<int>(0);
            questReward.Meso = rewardNode.FindNodeByPath("meso").ResolveUol().GetValueEx<int>(0);

            Wz_Node itemNode = rewardNode.FindNodeByPath("item").ResolveUol();
            foreach (var item in itemNode?.Nodes ?? Enumerable.Empty<Wz_Node>())
            {
                var id = item.FindNodeByPath("id").GetValueEx<int>(0);
                var count = item.FindNodeByPath("count").GetValueEx<int>(0);
                if (id > 0 && count > 0)
                    questReward.Items.Add(new ItemIDnCount() { ID = id, Count = count });
            }

            return questReward;
        }

        public struct ItemIDnCount
        {
            public int ID;
            public int Count;
        }
    }
}
