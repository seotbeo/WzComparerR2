using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using WzComparerR2.WzLib;
using WzComparerR2.PluginBase;
using System.Linq;

namespace WzComparerR2.CharaSim
{
    public static class CharaSimLoader
    {
        static CharaSimLoader()
        {
            LoadedSetItems = new Dictionary<int, SetItem>();
            LoadedAstraSubWeapons = new Dictionary<int, AstraSubWeaponInfo>();
            LoadedDestinyWeapons = new Dictionary<int, List<int>>();
            LoadedExclusiveEquips = new Dictionary<int, ExclusiveEquip>();
            LoadedCommoditiesBySN = new Dictionary<int, Commodity>();
            LoadedCommoditiesByItemId = new Dictionary<int, Commodity>();
            LoadedCommodityPricesByItemId = new List<Dictionary<int, List<CommodityPriceInfo>>>();
            for (int i = 0; i < 2; i++) // 2 slots
                LoadedCommodityPricesByItemId.Add(new Dictionary<int, List<CommodityPriceInfo>>());
        }

        public static Dictionary<int, SetItem> LoadedSetItems { get; private set; }
        public static Dictionary<int, AstraSubWeaponInfo> LoadedAstraSubWeapons { get; private set; }
        public static Dictionary<int, List<int>> LoadedDestinyWeapons { get; private set; }

        public static Dictionary<int, ExclusiveEquip> LoadedExclusiveEquips { get; private set; }
        public static Dictionary<int, Commodity> LoadedCommoditiesBySN { get; private set; }
        public static Dictionary<int, Commodity> LoadedCommoditiesByItemId { get; private set; }
        public static List<Dictionary<int, List<CommodityPriceInfo>>> LoadedCommodityPricesByItemId { get; private set; }

        public static void LoadSetItemsIfEmpty(Wz_File sourceWzFile = null)
        {
            if (LoadedSetItems.Count == 0)
            {
                LoadSetItems(sourceWzFile);
            }
        }

        public static void LoadSetItems(Wz_File sourceWzFile)
        {
            //搜索setItemInfo.img
            Wz_Node etcWz = PluginManager.FindWz(Wz_Type.Etc, sourceWzFile, true);
            if (etcWz == null)
                return;
            Wz_Node setItemNode = etcWz.FindNodeByPath("SetItemInfo.img", true);
            if (setItemNode == null)
                return;

            //搜索ItemOption.img
            Wz_Node itemWz = PluginManager.FindWz(Wz_Type.Item, sourceWzFile, true);
            if (itemWz == null)
                return;
            Wz_Node optionNode = itemWz.FindNodeByPath("ItemOption.img", true);
            if (optionNode == null)
                return;

            LoadedSetItems.Clear();
            foreach (Wz_Node node in setItemNode.Nodes)
            {
                int setItemIndex;
                if (Int32.TryParse(node.Text, out setItemIndex))
                {
                    SetItem setItem = SetItem.CreateFromNode(node, optionNode);
                    if (setItem != null)
                        LoadedSetItems[setItemIndex] = setItem;
                }
            }
        }

        public static void LoadAstraSubWeaponsIfEmpty(Wz_File sourceWzFile = null)
        {
            if (LoadedAstraSubWeapons.Count == 0)
            {
                LoadAstraSubWeapons(sourceWzFile);
            }
        }

        public static void LoadAstraSubWeapons(Wz_File sourceWzFile)
        {
            //搜索setItemInfo.img
            Wz_Node etcWz = PluginManager.FindWz(Wz_Type.Etc, sourceWzFile);
            if (etcWz == null)
                return;
            Wz_Node astraNode = etcWz.FindNodeByPath("SubWeaponTransferData.img\\Job", true);
            if (astraNode == null)
                return;

            LoadedAstraSubWeapons.Clear();
            List<int> idSet = new List<int>();
            Action<int> insert = (int jobID) =>
            {
                idSet.Sort();
                for (int i = 0; i < idSet.Count; i++)
                {
                    LoadedAstraSubWeapons[idSet[i]] = new AstraSubWeaponInfo(idSet[i], i, jobID);
                }
                idSet.Clear();
            };

            foreach (var job in astraNode.Nodes)
            {
                if (!int.TryParse(job.Text, out var jobID))
                    continue;

                var targetNode = job.FindNodeByPath("target");
                foreach (var target in targetNode?.Nodes ?? new Wz_Node.WzNodeCollection(null))
                {
                    if (int.TryParse(target.Text, out var id))
                    {
                        idSet.Add(id);
                    }
                    else
                    {
                        insert(jobID);
                        foreach (var inner_target in target?.Nodes ?? new Wz_Node.WzNodeCollection(null))
                        {
                            if (int.TryParse(inner_target.Text, out var inner_id))
                            {
                                idSet.Add(inner_id);
                            }
                        }
                        insert(jobID);
                    }
                }
                insert(jobID);
            }
        }


        public static void LoadDestinyWeaponsIfEmpty(Wz_File sourceWzFile = null)
        {
            if (LoadedDestinyWeapons.Count == 0)
            {
                LoadDestinyWeapons(sourceWzFile);
            }
        }

        public static void LoadDestinyWeapons(Wz_File sourceWzFile)
        {
            Wz_Node uiWz = PluginManager.FindWz(Wz_Type.UI, sourceWzFile);
            if (uiWz == null)
                return;
            Wz_Node topUINode = uiWz.FindNodeByPath("UIWeaponQuest.img", true);
            if (topUINode == null)
                return;

            LoadedDestinyWeapons.Clear();

            List<KeyValuePair<int, Wz_Node>> destinyWeaponPhases = new List<KeyValuePair<int, Wz_Node>>()
            {
                new KeyValuePair<int, Wz_Node>(1, topUINode.FindNodeByPath("DestinyWeaponQuest\\rewardList\\3")),
                new KeyValuePair<int, Wz_Node>(2, topUINode.FindNodeByPath("DestinySecondWeaponQuest\\rewardList\\3")),
            };

            foreach (var phase in destinyWeaponPhases)
            {
                if (phase.Value != null)
                {
                    List<int> temp = new List<int>();
                    foreach (var job in phase.Value.Nodes)
                    {
                        temp.AddRange(job.Nodes.Select(n => n.GetValueEx<int>(0)));
                    }
                    LoadedDestinyWeapons[phase.Key] = temp;
                }
            }
        }

        public static SetItem LoadSetItem(int setID, Wz_File sourceWzFile)
        {
            //搜索setItemInfo.img
            Wz_Node etcWz = PluginManager.FindWz(Wz_Type.Etc, sourceWzFile);
            if (etcWz == null)
                return null;
            Wz_Node setItemNode = etcWz.FindNodeByPath("SetItemInfo.img", true);
            if (setItemNode == null)
                return null;

            //搜索ItemOption.img
            Wz_Node itemWz = PluginManager.FindWz(Wz_Type.Item, sourceWzFile);
            if (itemWz == null)
                return null;
            Wz_Node optionNode = itemWz.FindNodeByPath("ItemOption.img", true);
            if (optionNode == null)
                return null;

            foreach (Wz_Node node in setItemNode.Nodes)
            {
                int setItemIndex;
                if (Int32.TryParse(node.Text, out setItemIndex) && setItemIndex == setID)
                {
                    SetItem setItem = SetItem.CreateFromNode(node, optionNode);
                    if (setItem != null)
                        return setItem;
                    else
                        return null;
                }
            }
            return null;
        }

        public static void LoadExclusiveEquipsIfEmpty(Wz_File sourceWzFile = null)
        {
            if (LoadedExclusiveEquips.Count == 0)
            {
                LoadExclusiveEquips(sourceWzFile);
            }
        }

        public static void LoadExclusiveEquips(Wz_File sourceWzFile)
        {
            Wz_Node exclusiveNode = PluginManager.FindWz("Etc/ExclusiveEquip.img", sourceWzFile);
            if (exclusiveNode == null)
                return;

            LoadedExclusiveEquips.Clear();
            foreach (Wz_Node node in exclusiveNode.Nodes)
            {
                int exclusiveEquipIndex;
                if (Int32.TryParse(node.Text, out exclusiveEquipIndex))
                {
                    ExclusiveEquip exclusiveEquip = ExclusiveEquip.CreateFromNode(node);
                    if (exclusiveEquip != null)
                        LoadedExclusiveEquips[exclusiveEquipIndex] = exclusiveEquip;
                }
            }
        }

        public static void LoadCommoditiesIfEmpty(Wz_File sourceWzFile = null, int slotIdx = 0)
        {
            if (LoadedCommoditiesBySN.Count == 0 && LoadedCommoditiesByItemId.Count == 0)
            {
                LoadCommodities(sourceWzFile, slotIdx);
            }
        }

        public static void LoadCommodities(Wz_File sourceWzFile, int slotIdx = 0)
        {
            Wz_Node commodityNode = PluginManager.FindWz("Etc/Commodity.img", sourceWzFile);
            if (commodityNode == null)
                return;

            LoadedCommoditiesBySN.Clear();
            LoadedCommoditiesByItemId.Clear();
            LoadedCommodityPricesByItemId[slotIdx].Clear();
            foreach (Wz_Node node in commodityNode.Nodes)
            {
                int commodityIndex;
                if (Int32.TryParse(node.Text, out commodityIndex))
                {
                    Commodity commodity = Commodity.CreateFromNode(node);
                    if (commodity != null)
                    {
                        LoadedCommoditiesBySN[commodity.SN] = commodity;
                        if (commodity.ItemId / 10000 == 910)
                            LoadedCommoditiesByItemId[commodity.ItemId] = commodity;

                        if (commodity.OnSale > 0)
                        {
                            if (!LoadedCommodityPricesByItemId[slotIdx].ContainsKey(commodity.ItemId))
                            {
                                LoadedCommodityPricesByItemId[slotIdx][commodity.ItemId] = new List<CommodityPriceInfo>();
                            }
                            if (commodity.Price > 0) LoadedCommodityPricesByItemId[slotIdx][commodity.ItemId].Add(commodity.PriceInfo);
                        }
                    }
                }
            }

            foreach (var kv in LoadedCommodityPricesByItemId[slotIdx])
            {
                kv.Value.Sort();
            }
        }

        public static void ClearAll()
        {
            LoadedSetItems.Clear();
            LoadedAstraSubWeapons.Clear();
            LoadedDestinyWeapons.Clear();
            LoadedExclusiveEquips.Clear();
            LoadedCommoditiesBySN.Clear();
            LoadedCommoditiesByItemId.Clear();
            foreach (var dict in LoadedCommodityPricesByItemId)
            {
                dict.Clear();
            }
        }

        public static int GetActionDelay(string actionName, Wz_Node wzNode = null)
        {
            if (string.IsNullOrEmpty(actionName))
            {
                return 0;
            }
            Wz_Node actionNode = wzNode == null ? PluginManager.FindWz("Character/00002000.img/" + actionName) :
                PluginManager.FindWz("Character/00002000.img/" + actionName, wzNode.GetNodeWzFile());
            if (actionNode == null)
            {
                return 0;
            }

            int delay = 0;
            foreach (Wz_Node frameNode in actionNode.Nodes)
            {
                Wz_Node delayNode = frameNode.Nodes["delay"];
                if (delayNode != null)
                {
                    delay += Math.Abs(delayNode.GetValue<int>());
                }
            }

            return delay;
        }
    }
}
