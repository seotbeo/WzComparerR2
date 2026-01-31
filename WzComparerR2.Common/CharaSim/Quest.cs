using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Quest : IDisposable
    {
        public Quest()
        {
            this.ID = -1;
            this.State = 0;
            this.Desc = new string[3];
            this.Reward = new QuestReward();
            this.Category = new List<int>();
            this.Check1Items = new Dictionary<string, Check1Item>();
            this.Check1Infoex = new Dictionary<string, bool>();
        }

        private int _state {  get; set; }
        public int ID { get; set; }
        public int Lvmin { get; set; }
        public int Lvmax { get; set; }
        public bool Blocked { get; set; }
        public bool LvLimit { get; set; }
        public bool RecommendExcept { get; set; }
        public string Name { get; set; }
        public string DemandBase { get; set; }
        public string DemandSummary { get; set; }
        public string PlaceSummary { get; set; }
        public string RewardSummary { get; set; }
        public string Summary { get; set; }
        public string[] Desc { get; set; }
        public Npc Check0Npc { get; set; }
        public int Check1NpcID { get; set; }
        public QuestReward Reward { get; set; }
        public int MedalCategory { get; set; }
        public List<int> Category { get; set; }
        public Dictionary<string, Check1Item> Check1Items { get; set; }
        public Dictionary<string, bool> Check1Infoex { get; set; }
        public int State
        {
            get
            {
                return this._state;
            }
            set
            {
                this._state = Math.Max(Math.Min(value, 2), 0);
            }
        }

        public static Quest CreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode, GlobalFindNodeFunction2 findNode2, Wz_File wzf = null, int? fromInfoNode = null)
        {
            if (node == null) return null;

            int questID = -1;
            if (fromInfoNode != null)
            {
                questID = fromInfoNode.Value;
            }
            else
            {
                Match m = Regex.Match(node.Text, @"^(\d+)\.img$");
                if (!(m.Success && Int32.TryParse(m.Result("$1"), out questID)))
                {
                    return null;
                }
            }

            Quest quest = new Quest();
            quest.ID = questID;
            Wz_Node infoNode = fromInfoNode == null ? node.FindNodeByPath("QuestInfo").ResolveUol() : node;
            Wz_Node recommendNode = null;
            Wz_Node demandNode = null;
            if (infoNode != null)
            {
                foreach (var propNode in infoNode.Nodes)
                {
                    switch (propNode.Text)
                    {
                        case "name":
                            quest.Name = propNode.GetValueEx<string>(null); break;
                        case "0":
                            quest.Desc[0] = propNode.GetValueEx<string>(null); break;
                        case "1":
                            quest.Desc[1] = propNode.GetValueEx<string>(null); break;
                        case "2":
                            quest.Desc[2] = propNode.GetValueEx<string>(null); break;
                        case "blocked":
                            quest.Blocked = propNode.GetValueEx<int>(0) == 1; break;
                        case "reward":
                            quest.Reward = QuestReward.CreateFromNode(propNode); break;
                        case "category":
                            var category = propNode.GetValueEx<string>("0|0");
                            foreach (var ct in category.Split('|'))
                            {
                                if (int.TryParse(ct, out var value))
                                {
                                    quest.Category.Add(value);
                                }
                            }
                            break;
                        case "medalCategory":
                            quest.MedalCategory = propNode.GetValueEx<int>(0); break;
                        case "recommend":
                            recommendNode = propNode.ResolveUol(); break;
                        case "demand":
                            demandNode = propNode.ResolveUol(); break;
                        case "demandSummary":
                            quest.DemandSummary = propNode.GetValueEx<string>(null); break;
                        case "placeSummary":
                            quest.PlaceSummary = propNode.GetValueEx<string>(null); break;
                        case "rewardSummary":
                            quest.RewardSummary = propNode.GetValueEx<string>(null); break;
                        case "summary":
                            quest.Summary = propNode.GetValueEx<string>(null); break;
                    }
                }
            }

            Wz_Node check0Node = fromInfoNode == null ? node.FindNodeByPath("Check\\0").ResolveUol()
                : findNode2.Invoke(string.Format("Quest/Check.img/{0}/0", quest.ID), wzf);
            if (check0Node != null)
            {
                foreach (var propNode in check0Node.Nodes)
                {
                    switch (propNode.Text)
                    {
                        case "npc":
                            var npcID = propNode.GetValueEx<int>(0);
                            Wz_Node npcNode = findNode2.Invoke(string.Format("Npc/{0:D7}.img", npcID), wzf);
                            quest.Check0Npc = Npc.CreateFromNode(npcNode, findNode, findNode2, wzf); break;
                        case "lvmin":
                            quest.Lvmin = propNode.GetValueEx<int>(0); break;
                        case "lvmax":
                            quest.Lvmax = propNode.GetValueEx<int>(0); break;
                    }
                }
            }

            Wz_Node check1Node = fromInfoNode == null ? node.FindNodeByPath("Check\\1").ResolveUol()
                : findNode2.Invoke(string.Format("Quest/Check.img/{0}/1", quest.ID), wzf);
            if (check1Node != null)
            {
                foreach (var propNode in check1Node.Nodes)
                {
                    switch (propNode.Text)
                    {
                        case "mob":
                        case "item":
                            foreach (var subNode in propNode.Nodes)
                            {
                                var type = propNode.Text;
                                var tag = subNode.Text;
                                var id = subNode.FindNodeByPath("id").GetValueEx<int>(0);
                                var count = subNode.FindNodeByPath("count").GetValueEx<int>(0);
                                quest.Check1Items.Add($"{type}{tag}", new Check1Item()
                                {
                                    ID = id,
                                    Count = count
                                });
                            }
                            break;
                        case "npc":
                            quest.Check1NpcID = propNode.GetValueEx<int>(0); break;
                        case "infoex":
                            foreach (var subNode in propNode.Nodes)
                            {
                                var exVariable = subNode.FindNodeByPath("exVariable").GetValueEx<string>("");
                                var cond = subNode.FindNodeByPath("cond").GetValueEx<int>(0);
                                if (!quest.Check1Infoex.ContainsKey(exVariable))
                                    quest.Check1Infoex.Add(exVariable, cond > 0 ? true : false);
                            }
                            break;

                    }
                }
            }

            if (recommendNode != null)
            {
                foreach (var propNode in recommendNode.Nodes)
                {
                    switch (propNode.Text)
                    {
                        case "lvmin":
                            quest.Lvmin = propNode.GetValueEx<int>(0); break;
                        case "lvmax":
                            quest.Lvmax = propNode.GetValueEx<int>(0);
                            quest.LvLimit = true;
                            break;
                        case "except":
                            quest.RecommendExcept = true; break;
                    }
                }
            }

            if (demandNode != null)
            {
                foreach (var propNode in demandNode.Nodes)
                {
                    switch (propNode.Text)
                    {
                        case "base":
                            quest.DemandBase = propNode.GetValueEx<string>(null); break;
                        case "explain":
                            break;
                    }
                }
            }

            return quest;
        }

        public void Dispose()
        {
            if (this.Check0Npc != null)
                this.Check0Npc.Dispose();
        }

        public struct Check1Item
        {
            public int ID;
            public int Count;
        }
    }
}