using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.Common
{
    public class StringLinker
    {
        public StringLinker()
        {
            stringEqp = new Dictionary<int, StringResult>();
            stringFamiliarSkill = new Dictionary<int, StringResult>();
            stringItem = new Dictionary<int, StringResult>();
            stringMap = new Dictionary<int, StringResult>();
            stringMob = new Dictionary<int, StringResult>();
            stringNpc = new Dictionary<int, StringResult>();
            stringSkill = new Dictionary<int, StringResult>();
            stringSkill2 = new Dictionary<string, StringResult>();
            stringSetItem = new Dictionary<int, StringResult>();
            stringQuest = new Dictionary<int, StringResult>();
            stringAchievement = new Dictionary<int, StringResult>();
            stringWorldArchiveMob = new Dictionary<int, StringResult>();
            stringWorldArchiveNpc = new Dictionary<int, StringResult>();
            stringWorldArchiveMobByPath = new Dictionary<string, StringResult>();
            stringWorldArchiveNpcByPath = new Dictionary<string, StringResult>();
            stringMonsterBook = new Dictionary<int, StringResult>();
        }

        public bool Update(Wz_Node stringNode, Wz_Node itemNode, Wz_Node etcNode, Wz_Node questNode)
        {
            if (stringNode == null && itemNode == null && etcNode == null && questNode == null)
                return true;

            return Load(stringNode, itemNode, etcNode, questNode, update: true);
        }

        public bool Load(Wz_File stringWz, Wz_File itemWz, Wz_File etcWz)
        {
            return Load(stringWz, itemWz, etcWz, null);
        }

        public bool Load(Wz_File stringWz, Wz_File itemWz, Wz_File etcWz, Wz_File questWz)
        {
            //if (stringWz == null || stringWz.Node == null ||
                //itemWz == null || itemWz.Node == null ||
                //etcWz == null || etcWz.Node == null)
                //return false;
            this.Clear();

            return Load(stringWz?.Node, itemWz?.Node, etcWz?.Node, questWz?.Node);
        }

        public bool Load(Wz_Node stringNode, Wz_Node itemNode, Wz_Node etcNode, Wz_Node questNode, bool update = false)
        {
            int id;
            foreach (Wz_Node node in stringNode?.Nodes ?? new Wz_Node.WzNodeCollection(null))
            {
                Wz_Image image = node.Value as Wz_Image;
                if (image == null)
                    continue;
                switch (node.Text)
                {
                    case "Pet.img":
                    case "Cash.img":
                    case "Ins.img":
                    case "Consume.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                            {
                                StringResult strResult = null;
                                if (update)
                                {
                                    try { strResult = stringItem[id]; }
                                    catch { }
                                }
                                if (strResult == null) strResult = new StringResult();

                                strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;
                                strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                                strResult.AutoDesc = GetDefaultString(linkNode, "autodesc") ?? strResult.AutoDesc;
                                strResult.FullPath = tree.FullPath; // always use the original node path

                                AddAllValue(strResult, linkNode);
                                stringItem[id] = strResult;
                            }
                        }
                        break;
                    case "Etc.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree0 in image.Node.Nodes)
                        {
                            foreach (Wz_Node tree in tree0.Nodes)
                            {
                                if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                                {
                                    StringResult strResult = null;
                                    if (update)
                                    {
                                        try { strResult = stringItem[id]; }
                                        catch { }
                                    }
                                    if (strResult == null) strResult = new StringResult();

                                    strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;
                                    strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                                    strResult.FullPath = tree.FullPath;

                                    AddAllValue(strResult, linkNode);
                                    stringItem[id] = strResult;
                                }
                            }
                        }
                        break;
                    case "Familiar.img":
                    case "FamiliarSkill.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree0 in image.Node.Nodes)
                        {
                            if (tree0.Text == "skill")
                            {
                                foreach (Wz_Node tree1 in tree0.Nodes)
                                {
                                    if (Int32.TryParse(tree1.Text, out id) && tree1.ResolveUol() is Wz_Node linkNode)
                                    {
                                        StringResult strResult = null;
                                        if (strResult == null) strResult = new StringResult();

                                        strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;
                                        strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                                        strResult.FullPath = tree1.FullPath;

                                        AddAllValue(strResult, linkNode);
                                        stringFamiliarSkill[id] = strResult;
                                    }
                                }
                            }
                        }
                        break;
                    case "Mob.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                            {
                                StringResult strResult = null;
                                if (update)
                                {
                                    try { strResult = stringMob[id]; }
                                    catch { }
                                }
                                if (strResult == null) strResult = new StringResult();

                                strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;
                                strResult.FullPath = tree.FullPath;

                                AddAllValue(strResult, linkNode);
                                stringMob[id] = strResult;
                            }
                        }
                        break;
                    case "MonsterBook.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            if (Int32.TryParse(tree.Text, out id))
                            {
                                if (stringMob.ContainsKey(id))
                                {
                                    Wz_Node messageNode = tree.FindNodeByPath("episode");
                                    if (messageNode != null)
                                    {
                                        StringResult mbSr = null;
                                        if (update)
                                        {
                                            try { mbSr = stringMonsterBook[id]; }
                                            catch { }
                                        }
                                        if (mbSr == null) mbSr = new StringResult();

                                        mbSr.Name = stringMob[id].Name;
                                        mbSr.Desc = messageNode.Value.ToString();
                                        stringMonsterBook[id] = mbSr;
                                    }
                                }
                            }
                        }
                        break;
                    case "Npc.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                            {
                                StringResult strResult = null;
                                if (update)
                                {
                                    try { strResult = stringNpc[id]; }
                                    catch { }
                                }
                                if (strResult == null) strResult = new StringResult();

                                strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;
                                strResult.Desc = GetDefaultString(linkNode, "func") ?? strResult.Desc;
                                strResult.FullPath = tree.FullPath;

                                AddAllValue(strResult, linkNode);
                                stringNpc[id] = strResult;
                            }
                        }
                        break;
                    case "Map.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree0 in image.Node.Nodes)
                        {
                            foreach (Wz_Node tree in tree0.Nodes)
                            {
                                if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                                {
                                    StringResult strResult = null;
                                    if (update)
                                    {
                                        try { strResult = stringMap[id]; }
                                        catch { }
                                    }
                                    if (strResult == null) strResult = new StringResult();

                                    var streetName = GetDefaultString(linkNode, "streetName");
                                    var mapName = GetDefaultString(linkNode, "mapName");
                                    strResult.Name = string.Format("{0} : {1}",
                                        streetName,
                                        mapName) ?? strResult.Name;
                                    strResult.StreetName = streetName ?? strResult.StreetName ?? string.Empty;
                                    strResult.MapName = mapName ?? strResult.MapName ?? string.Empty;
                                    strResult.Desc = GetDefaultString(linkNode, "mapDesc") ?? strResult.Desc;
                                    strResult.FullPath = tree.FullPath;

                                    AddAllValue(strResult, linkNode);
                                    stringMap[id] = strResult;
                                }
                            }
                        }
                        break;
                    case "Skill.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            if (tree.ResolveUol() is not Wz_Node linkNode)
                            {
                                continue;
                            }
                            StringResultSkill strResult = null;
                            if (update)
                            {
                                try
                                {
                                    if (tree.Text.Length >= 7 && Int32.TryParse(tree.Text, out id))
                                    {
                                        strResult = (StringResultSkill)stringSkill[id];
                                    }
                                    strResult = (StringResultSkill)stringSkill2[tree.Text];
                                }
                                catch { }
                            }
                            if (strResult == null) strResult = new StringResultSkill();

                            strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;//?? GetDefaultString(tree, "bookName");
                            strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                            strResult.Pdesc = GetDefaultString(linkNode, "pdesc") ?? strResult.Pdesc;

                            var h = GetDefaultString(linkNode, "h");
                            if (update && h != null)
                            {
                                strResult.SkillH.Clear();
                            }
                            strResult.SkillH.Add(h);

                            h = GetDefaultString(linkNode, "ph");
                            if (update && h != null)
                            {
                                strResult.SkillpH.Clear();
                                strResult.SkillpH.Add(h);
                            }
                            else if (!update) strResult.SkillpH.Add(h);

                            h = GetDefaultString(linkNode, "hch");
                            if (update && h != null)
                            {
                                strResult.SkillhcH.Clear();
                                strResult.SkillhcH.Add(h);
                            }
                            else if (!update) strResult.SkillhcH.Add(h);

                            if (strResult.SkillH.Count > 0 && strResult.SkillH.Last() == null)
                            {
                                strResult.SkillH.RemoveAt(strResult.SkillH.Count - 1);
                                bool cleared = false;

                                for (int i = 1; ; i++)
                                {
                                    string hi = GetDefaultString(linkNode, "h" + i);
                                    if (string.IsNullOrEmpty(hi))
                                        break;
                                    else if (update && !cleared)
                                    {
                                        strResult.SkillH.Clear();
                                        cleared = true;
                                    }
                                    strResult.SkillH.Add(hi);
                                }
                            }
                            // KMST1196, add h_ prefix strings
                            foreach (Wz_Node child in linkNode.Nodes)
                            {
                                if (child.Text.StartsWith("h_") && int.TryParse(child.Text.Substring(2), out int level) && level > 0 && child.Value != null)
                                {
                                    strResult.SkillExtraH.RemoveAll(x => x.Key == level);
                                    strResult.SkillExtraH.Add(new KeyValuePair<int, string>(level, child.GetValue<string>()));
                                }
                            }
                            if (strResult.SkillExtraH.Count > 1)
                            {
                                strResult.SkillExtraH.Sort((left, right) => left.Key.CompareTo(right.Key));
                            }
                            strResult.SkillH.TrimExcess();
                            strResult.SkillpH.TrimExcess();
                            strResult.FullPath = tree.FullPath;

                            AddAllValue(strResult, linkNode);
                            if (tree.Text.Length >= 7 && Int32.TryParse(tree.Text, out id))
                            {
                                stringSkill[id] = strResult;
                            }
                            stringSkill2[tree.Text] = strResult;
                        }
                        break;
                    case "Eqp.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree0 in image.Node.Nodes)
                        {
                            foreach (Wz_Node tree1 in tree0.Nodes)
                            {
                                foreach (Wz_Node tree in tree1.Nodes)
                                {
                                    if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                                    {
                                        StringResult strResult = null;
                                        if (update)
                                        {
                                            try { strResult = stringEqp[id]; }
                                            catch { }
                                        }
                                        if (strResult == null) strResult = new StringResult();

                                        strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;
                                        strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                                        strResult.FullPath = tree.FullPath;

                                        AddAllValue(strResult, linkNode);
                                        stringEqp[id] = strResult;
                                    }
                                }
                            }
                        }
                        break;
                }
            }

            foreach (Wz_Node node in itemNode?.FindNodeByPath("Special")?.Nodes ?? new Wz_Node.WzNodeCollection(null))
            {
                Wz_Image image = node.Value as Wz_Image;
                if (image == null)
                    continue;
                switch (node.Text)
                {
                    case "0910.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                            {
                                StringResult strResult = null;
                                if (update)
                                {
                                    try { strResult = stringItem[id]; }
                                    catch { }
                                }
                                if (strResult == null) strResult = new StringResult();

                                strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;
                                strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                                strResult.FullPath = tree.FullPath;

                                AddAllValue(strResult, linkNode);
                                stringItem[id] = strResult;
                            }
                        }
                        break;
                }
            }

            foreach (Wz_Node node in etcNode?.Nodes ?? new Wz_Node.WzNodeCollection(null))
            {
                Wz_Image image = node.Value as Wz_Image;
                if (image == null)
                    continue;
                switch (node.Text)
                {
                    case "SetItemInfo.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                            {
                                StringResult strResult = null;
                                if (update)
                                {
                                    try { strResult = stringSetItem[id]; }
                                    catch { }
                                }
                                if (strResult == null) strResult = new StringResult();

                                strResult.Name = GetDefaultString(linkNode, "setItemName") ?? strResult.Name ?? string.Empty;
                                strResult.FullPath = tree.FullPath;

                                AddAllValue(strResult, linkNode);
                                stringSetItem[id] = strResult;
                            }
                        }
                        break;
                }
            }

            var achievementNode = etcNode?.FindNodeByPath("Achievement\\AchievementData");
            foreach (Wz_Node node in achievementNode?.Nodes ?? new Wz_Node.WzNodeCollection(null))
            {
                Wz_Image image = node.Value as Wz_Image;
                if (image == null || !image.TryExtract())
                    continue;
                Wz_Node tree = image.Node;
                Wz_Node infoNode = tree.FindNodeByPath("info");
                if (Int32.TryParse(tree.Text.Replace(".img", ""), out id) && infoNode.ResolveUol() is Wz_Node linkNode && linkNode != null)
                {
                    StringResult strResult = null;
                    if (update)
                    {
                        try { strResult = stringAchievement[id]; }
                        catch { }
                    }
                    if (strResult == null) strResult = new StringResult();

                    strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;
                    strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                    strResult.FullPath = "AchievementData\\" + tree.FullPath;

                    //AddAllValue(strResult, linkNode);
                    stringAchievement[id] = strResult;
                }
            }

            var worldArchiveNode = etcNode?.FindNodeByPath("worldArchive.img");
            if (worldArchiveNode != null)
            {
                Wz_Image worldArchiveImg = worldArchiveNode.Value as Wz_Image;
                if (worldArchiveImg != null && worldArchiveImg.TryExtract())
                {
                    Wz_Node targetNode = worldArchiveImg.Node;
                    Wz_Node infoNode = targetNode.FindNodeByPath("collectionInfo");
                    foreach (Wz_Node node in infoNode?.Nodes ?? new Wz_Node.WzNodeCollection(null))
                    {
                        foreach (Wz_Node subNode in node.Nodes)
                        {
                            switch (subNode.Text)
                            {
                                case "worldName":
                                case "worldDesc": break;
                                default:
                                    if (int.TryParse(subNode.Text, out _))
                                    {
                                        foreach (Wz_Node subNode2 in subNode.Nodes)
                                        {
                                            switch (subNode2.Text)
                                            {
                                                case "mob":
                                                    foreach (Wz_Node mobNode in subNode2.Nodes)
                                                    {
                                                        List<int> mobIDs = new List<int>();
                                                        foreach (Wz_Node idNode in mobNode.FindNodeByPath("id")?.Nodes ?? new Wz_Node.WzNodeCollection(null))
                                                        {
                                                            var mobID = idNode.GetValueEx<int>(0);
                                                            if (mobID != 0) mobIDs.Add(mobID);
                                                        }
                                                        var desc = mobNode.FindNodeByPath("desc").GetValueEx<string>(null);
                                                        if (string.IsNullOrEmpty(desc))
                                                        {
                                                            desc = "(null)";
                                                        }

                                                        StringResult strResult = null;
                                                        string path = $"{node.Text}_{subNode.Text}_{subNode2.Text}_{mobNode.Text}";
                                                        if (update)
                                                        {
                                                            try { strResult = stringWorldArchiveMobByPath[path]; }
                                                            catch { }
                                                        }
                                                        if (strResult == null) strResult = new StringResult();

                                                        strResult.Desc = desc ?? strResult.Desc;
                                                        stringWorldArchiveMobByPath[path] = strResult;

                                                        foreach (var mobID in mobIDs)
                                                        {
                                                            if (!stringMob.ContainsKey(mobID))
                                                            {
                                                                continue;
                                                            }
                                                            strResult.Name = stringMob[mobID].Name;
                                                            stringWorldArchiveMob[mobID] = strResult;
                                                        }
                                                    }
                                                    break;
                                                case "npc":
                                                    foreach (Wz_Node npcNode in subNode2.Nodes)
                                                    {
                                                        List<int> npcIDs = new List<int>();
                                                        foreach (Wz_Node idNode in npcNode.FindNodeByPath("id")?.Nodes ?? new Wz_Node.WzNodeCollection(null))
                                                        {
                                                            var npcID = idNode.GetValueEx<int>(0);
                                                            if (npcID != 0) npcIDs.Add(npcID);
                                                        }
                                                        var desc = npcNode.FindNodeByPath("desc").GetValueEx<string>(null);
                                                        if (string.IsNullOrEmpty(desc))
                                                        {
                                                            desc = "(null)";
                                                        }

                                                        StringResult strResult = null;
                                                        string path = $"{node.Text}_{subNode.Text}_{subNode2.Text}_{npcNode.Text}";
                                                        if (update)
                                                        {
                                                            try { strResult = stringWorldArchiveNpcByPath[path]; }
                                                            catch { }
                                                        }
                                                        if (strResult == null) strResult = new StringResult();

                                                        strResult.Desc = desc ?? strResult.Desc;
                                                        stringWorldArchiveNpcByPath[path] = strResult;

                                                        foreach (var npcID in npcIDs)
                                                        {
                                                            if (!stringNpc.ContainsKey(npcID))
                                                            {
                                                                continue;
                                                            }
                                                            strResult.Name = stringNpc[npcID].Name;
                                                            stringWorldArchiveNpc[npcID] = strResult;
                                                        }
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }

            }

            Wz_Node qDataNode = questNode?.FindNodeByPath("QuestData");
            Wz_Node qInfoNode = null;
            bool newQuestDir = true;
            if (qDataNode == null)
            {
                qDataNode = questNode?.FindNodeByPath("QuestInfo.img");
                if (qDataNode != null)
                {
                    Wz_Image image = qDataNode.Value as Wz_Image;
                    if (image != null && image.TryExtract())
                    {
                        qDataNode = image.Node;
                        newQuestDir = false;
                    }
                }
            }
            foreach (Wz_Node node in qDataNode?.Nodes ?? new Wz_Node.WzNodeCollection(null))
            {
                Wz_Node tree = node;
                if (node.Value is Wz_Image image)
                {
                    if (image == null)
                        continue;

                    if (!image.TryExtract()) continue;
                    tree = image.Node;
                }
                qInfoNode = newQuestDir ? tree.FindNodeByPath("QuestInfo") : tree;
                if (Int32.TryParse(tree.Text.Replace(".img", ""), out id) && qInfoNode.ResolveUol() is Wz_Node linkNode && linkNode != null)
                {
                    StringResult strResult = null;
                    if (update)
                    {
                        try { strResult = stringQuest[id]; }
                        catch { }
                    }
                    if (strResult == null) strResult = new StringResult();

                    strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;
                    strResult.Desc = GetDefaultString(linkNode, "0") ?? strResult.Desc;
                    strResult.Quest_DemandBase = GetDefaultString(linkNode, "demand\\base") ?? strResult.Quest_DemandBase;
                    strResult.FullPath = (newQuestDir ? "QuestData\\" : "") + tree.FullPath;

                    AddAllValue(strResult, linkNode);
                    stringQuest[id] = strResult;
                }
            }

            return this.HasValues;
        }

        public void Clear()
        {
            stringEqp.Clear();
            stringFamiliarSkill.Clear();
            stringItem.Clear();
            stringMob.Clear();
            stringMap.Clear();
            stringNpc.Clear();
            stringSkill.Clear();
            stringSkill2.Clear();
            stringSetItem.Clear();
            stringQuest.Clear();
            stringAchievement.Clear();
            stringWorldArchiveMob.Clear();
            stringWorldArchiveNpc.Clear();
            stringWorldArchiveMobByPath.Clear();
            stringWorldArchiveNpcByPath.Clear();
            stringMonsterBook.Clear();
        }

        public bool HasValues
        {
            get
            {
                return (stringEqp.Count + stringItem.Count + stringMap.Count +
                    stringMob.Count + stringNpc.Count + stringSkill.Count + stringSetItem.Count + stringQuest.Count + stringAchievement.Count > 0);
            }
        }

        private Dictionary<int, StringResult> stringEqp;
        private Dictionary<int, StringResult> stringFamiliarSkill;
        private Dictionary<int, StringResult> stringItem;
        private Dictionary<int, StringResult> stringMap;
        private Dictionary<int, StringResult> stringMob;
        private Dictionary<int, StringResult> stringNpc;
        private Dictionary<int, StringResult> stringSkill;
        private Dictionary<string, StringResult> stringSkill2;
        private Dictionary<int, StringResult> stringSetItem;
        private Dictionary<int, StringResult> stringQuest;
        private Dictionary<int, StringResult> stringAchievement;
        private Dictionary<int, StringResult> stringWorldArchiveMob;
        private Dictionary<int, StringResult> stringWorldArchiveNpc;
        private Dictionary<string, StringResult> stringWorldArchiveMobByPath;
        private Dictionary<string, StringResult> stringWorldArchiveNpcByPath;
        private Dictionary<int, StringResult> stringMonsterBook;

        private string GetDefaultString(Wz_Node node, string searchNodeText)
        {
            node = node.FindNodeByPath(searchNodeText);
            return node == null ? null : Convert.ToString(node.Value);
        }

        private void AddAllValue(StringResult sr, Wz_Node node)
        {
            foreach (Wz_Node child in node.Nodes)
            {
                if (child.Value != null)
                {
                    sr[child.Text] = child.GetValue<string>();
                }
            }
        }

        public Dictionary<int, StringResult> StringEqp
        {
            get { return stringEqp; }
        }

        public Dictionary<int, StringResult> StringFamiliarSkill
        {
            get { return stringFamiliarSkill; }
        }

        public Dictionary<int, StringResult> StringItem
        {
            get { return stringItem; }
        }

        public Dictionary<int, StringResult> StringMap
        {
            get { return stringMap; }
        }

        public Dictionary<int, StringResult> StringMob
        {
            get { return stringMob; }
        }

        public Dictionary<int, StringResult> StringNpc
        {
            get { return stringNpc; }
        }

        public Dictionary<int, StringResult> StringSkill
        {
            get { return stringSkill; }
        }

        public Dictionary<string, StringResult> StringSkill2
        {
            get { return stringSkill2; }
        }

        public Dictionary<int, StringResult> StringSetItem
        {
            get { return stringSetItem; }
        }

        public Dictionary<int, StringResult> StringQuest
        {
            get { return stringQuest; }
        }

        public Dictionary<int, StringResult> StringAchievement
        {
            get { return stringAchievement; }
        }

        public Dictionary<int, StringResult> StringWorldArchiveMob
        {
            get { return stringWorldArchiveMob;  }
        }

        public Dictionary<int, StringResult> StringWorldArchiveNpc
        {
            get { return stringWorldArchiveNpc; }
        }

        public Dictionary<int, StringResult> StringMonsterBook
        {
            get { return stringMonsterBook; }
        }
    }
}
