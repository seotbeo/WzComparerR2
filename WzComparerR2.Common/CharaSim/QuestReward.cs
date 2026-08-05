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
            this.AttrExps = new List<KeyValuePair<string, int>>();
            this.Items = new List<ItemIDnCount>();
        }

        public int Exp { get; private set; }
        public int Meso { get; private set; }
        public int Pop { get; private set; }
        public int PetTameness { get; private set; }
        public List<KeyValuePair<string, int>> AttrExps { get; private set; }
        public List<ItemIDnCount> Items { get; private set; }

        public int Count
        {
            get { return (this.Exp > 0 ? 1 : 0) + (this.Meso > 0 ? 1 : 0) + (this.Pop > 0 ? 1 : 0) + (this.PetTameness > 0 ? 1 : 0) + this.AttrExps.Count + this.Items.Count; }
        }

        public bool HasValues
        {
            get { return this.Count > 0; }
        }

        public string ExpString
        {
            get { return "경험치 " + this.Exp.ToString("N0"); }
        }

        public string MesoString
        {
            get { return "메소 " + this.Meso.ToString("N0"); }
        }

        public int TotalProb
        {
            get { return Math.Abs(this.Items.Sum(i => i.Prob != null ? i.Prob.Value : 0)); }
        }

        public static QuestReward CreateFromNode(Wz_Node rewardNode)
        {
            QuestReward questReward = new QuestReward();
            if (rewardNode == null) return questReward;

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

        public static QuestReward CreateFromAct1Node(Wz_Node act1Node)
        {
            QuestReward questReward = new QuestReward();
            if (act1Node == null) return questReward;

            questReward.Exp = act1Node.FindNodeByPath("exp").ResolveUol().GetValueEx<int>(0);
            questReward.Meso = act1Node.FindNodeByPath("money").ResolveUol().GetValueEx<int>(0);
            questReward.Pop = act1Node.FindNodeByPath("pop").ResolveUol().GetValueEx<int>(0);
            questReward.PetTameness = act1Node.FindNodeByPath("pettameness").ResolveUol().GetValueEx<int>(0);

            foreach (var attr in new[] { "charismaEXP", "insightEXP", "willEXP", "craftEXP", "senseEXP", "charmEXP" })
            {
                var exp = act1Node.FindNodeByPath(attr).ResolveUol().GetValueEx<int>(0);
                if (exp > 0)
                {
                    questReward.AttrExps.Add(new KeyValuePair<string, int>(attr, exp));
                }
            }

            Wz_Node itemNode = act1Node.FindNodeByPath("item").ResolveUol();
            foreach (var item in itemNode?.Nodes ?? Enumerable.Empty<Wz_Node>())
            {
                var id = item.FindNodeByPath("id").GetValueEx<int>(0);
                var count = item.FindNodeByPath("count").GetValueEx<int>(0);
                var prop = item.FindNodeByPath("prop").GetValueEx<int?>(null);
                var job = item.FindNodeByPath("job").GetValueEx<int>(0);
                var gender = item.FindNodeByPath("gender").GetValueEx<int>(2);
                if (id > 0 && count > 0)
                    questReward.Items.Add(new ItemIDnCount()
                    {
                        ID = id,
                        Count = count,
                        Prob = prop,
                        Job = job,
                        Gender = gender
                    });
            }

            return questReward;
        }

        public struct ItemIDnCount
        {
            public int ID;
            public int Count;
            public int? Prob;
            public int Job;
            public int Gender;
        }
    }
}
