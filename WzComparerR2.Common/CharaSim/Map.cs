using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Map
    {
        public Map()
        {
            this.Mobs = new List<int>();
            this.Npcs = new List<int>();
        }

        public int MapID { get; set; }
        public int? Link { get; set; }
        public int Barrier { get; set; }
        public int BarrierArc { get; set; }
        public int BarrierAut { get; set; }
        public string MapMark { get; set; }
        public Wz_Node MiniMapNode { get; set; }
        public List<int> Mobs { get; set; }
        public List<int> Npcs { get; set; }

        public static Map CreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode)
        {
            Map map = new Map();
            int mapID;
            Match m = Regex.Match(node.Text, @"^(\d{9})\.img$");
            if (!(m.Success && Int32.TryParse(m.Result("$1"), out mapID)))
            {
                return null;
            }
            map.MapID = mapID;

            Wz_Node infoNode = node.FindNodeByPath("info").ResolveUol();
            if (infoNode != null)
            {
                foreach (var propNode in infoNode.Nodes)
                {
                    switch (propNode.Text)
                    {
                        case "mapMark": map.MapMark = propNode.GetValueEx<string>(null); break;
                        case "link": map.Link = propNode.GetValueEx<int?>(null); break;
                        case "barrier": map.Barrier = propNode.GetValueEx<int>(0); break;
                        case "barrierArc": map.BarrierArc = propNode.GetValueEx<int>(0); break;
                        case "barrierAut": map.BarrierAut = propNode.GetValueEx<int>(0); break;
                    }
                }
            }

            Wz_Node linkNode = null;
            if (map.Link != null && findNode != null)
            {
                linkNode = findNode(string.Format(@$"Map\Map\Map{map.Link / 100000000}\{map.Link:d9}.img"));
            }
            if (linkNode == null)
            {
                linkNode = node;
            }

            Wz_Node miniMapNode = linkNode.FindNodeByPath("miniMap").ResolveUol();
            map.MiniMapNode = miniMapNode;

            var mapInfo = findNode?.Invoke(string.Format($"Etc/MapObjectInfo.img/{map.MapID}"));
            if (mapInfo != null)
            {
                var mobNode = mapInfo.Nodes["mob"];
                if (mobNode != null)
                {
                    foreach (var valNode in mobNode.Nodes)
                    {
                        map.Mobs.Add(valNode.GetValue<int>());
                    }
                }
                var npcNode = mapInfo.Nodes["npc"];
                if (npcNode != null)
                {
                    foreach (var valNode in npcNode.Nodes)
                    {
                        map.Npcs.Add(valNode.GetValue<int>());
                    }
                }
            }
            else
            {
                Wz_Node lifeNode = linkNode.FindNodeByPath("life").ResolveUol();
                if (lifeNode != null)
                {
                    foreach (var life in lifeNode.Nodes)
                    {
                        var lifeType = life.FindNodeByPath("type").GetValueEx<string>(null);
                        var lifeId = life.FindNodeByPath("id").GetValueEx<int>(0);
                        if (lifeId != 0)
                        {
                            switch (lifeType)
                            {
                                case "n":
                                    if (!map.Npcs.Contains(lifeId))
                                    {
                                        map.Npcs.Add(lifeId);
                                    }
                                    break;
                                case "m":
                                    if (!map.Mobs.Contains(lifeId))
                                    {
                                        map.Mobs.Add(lifeId);
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }

            return map;
        }
    }
}
