using System;
using System.Collections.Generic;
using System.Linq;
using WzComparerR2.CharaSim;
using WzComparerR2.WzLib;

namespace WzComparerR2.Common
{
    public partial class StringLinker
    {
        private bool Load(Wz_Node stringNode, Wz_Node itemNode, Wz_Node etcNode, Wz_Node questNode, Wz_Node reactorNode, string langType, bool update = false)
        {
            stringNode = stringNode.FindNodeByPath(langType);

            if (update)
            {
                this.SourceStringUpdateNode = stringNode;
                this.SourceItemUpdateNode = itemNode;
                this.SourceEtcUpdateNode = etcNode;
                this.SourceQuestUpdateNode = questNode;
                this.SourceReactorUpdateNode = reactorNode;
            }
            else
            {
                this.SourceStringNode = stringNode;
                this.SourceItemNode = itemNode;
                this.SourceEtcNode = etcNode;
                this.SourceQuestNode = questNode;
                this.SourceReactorNode = reactorNode;
            }

            int id;
            foreach (Wz_Node node in stringNode?.Nodes ?? Enumerable.Empty<Wz_Node>())
            {
                Wz_Image image = node.Value as Wz_Image;
                Wz_Node imgNode = node;
                switch (node.Text)
                {
                    case "Pet.img":
                        if (image != null)
                        {
                            if (!image.TryExtract()) break;
                            imgNode = image.Node;
                        }
                        foreach (Wz_Node tree in imgNode.Nodes)
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
                                if (!update) strResult.FullPath = tree.FullPath;

                                AddAllValue(strResult, linkNode);
                                stringItem[id] = strResult;
                            }
                        }
                        break;
                    case "Item.img":
                        if (image != null)
                        {
                            if (!image.TryExtract()) break;
                            imgNode = image.Node;
                        }
                        foreach (Wz_Node tree in imgNode.Nodes)
                        {
                            if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                            {
                                var targetDict = id >= 2000000 ? stringItem : stringEqp;
                                StringResult strResult = null;
                                if (update)
                                {
                                    try { strResult = targetDict[id]; }
                                    catch { }
                                }
                                if (strResult == null) strResult = new StringResult();

                                strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;
                                strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                                if (!update) strResult.FullPath = GetItemFullPath(id, tree.FullPath);

                                AddAllValue(strResult, linkNode);
                                targetDict[id] = strResult;
                            }
                        }
                        break;
                    case "Mob.img":
                        if (image != null)
                        {
                            if (!image.TryExtract()) break;
                            imgNode = image.Node;
                        }
                        foreach (Wz_Node tree in imgNode.Nodes)
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
                                if (!update) strResult.FullPath = tree.FullPath;

                                AddAllValue(strResult, linkNode);
                                stringMob[id] = strResult;
                            }
                        }
                        break;
                    case "Npc.img":
                        if (image != null)
                        {
                            if (!image.TryExtract()) break;
                            imgNode = image.Node;
                        }
                        foreach (Wz_Node tree in imgNode.Nodes)
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
                                if (!update) strResult.FullPath = tree.FullPath;

                                AddAllValue(strResult, linkNode);
                                stringNpc[id] = strResult;
                            }
                        }
                        break;
                    case "Map.img":
                        if (image != null)
                        {
                            if (!image.TryExtract()) break;
                            imgNode = image.Node;
                        }
                        foreach (Wz_Node tree in imgNode.Nodes)
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
                                if (!update) strResult.FullPath = tree.FullPath;

                                AddAllValue(strResult, linkNode);
                                stringMap[id] = strResult;
                            }
                        }
                        break;
                    case "Skill.img":
                        if (image != null)
                        {
                            if (!image.TryExtract()) break;
                            imgNode = image.Node;
                        }
                        foreach (Wz_Node tree in imgNode.Nodes)
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

                            strResult.Name = GetDefaultString(linkNode, "Name") ?? strResult.Name ?? string.Empty;//?? GetDefaultString(tree, "bookName");
                            strResult.Desc = GetDefaultString(linkNode, "Desc") ?? strResult.Desc;
                            strResult.Pdesc = GetDefaultString(linkNode, "PDesc") ?? strResult.Pdesc;

                            var h = GetDefaultString(linkNode, "H");
                            if (update && h != null)
                            {
                                strResult.SkillH.Clear();
                            }
                            strResult.SkillH.Add(h);

                            h = GetDefaultString(linkNode, "Ph");
                            if (update && h != null)
                            {
                                strResult.SkillpH.Clear();
                                strResult.SkillpH.Add(h);
                            }
                            else if (!update) strResult.SkillpH.Add(h);

                            h = GetDefaultString(linkNode, "Hch");
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

                                Wz_Node levelDescNode = linkNode.FindNodeByPath("LevelDesc");
                                if (levelDescNode != null)
                                {
                                    for (int i = 1; ; i++)
                                    {
                                        string hi = GetDefaultString(levelDescNode, i.ToString());
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
                            }
                            // KMST1196, add h_ prefix strings
                            foreach (Wz_Node child in linkNode.Nodes)
                            {
                                if (child.Text.StartsWith("H_") && int.TryParse(child.Text.Substring(2), out int level) && level > 0 && child.Value != null)
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
                            if (!update) strResult.FullPath = tree.FullPath;

                            AddAllValue(strResult, linkNode);
                            if (tree.Text.Length >= 7 && Int32.TryParse(tree.Text, out id))
                            {
                                stringSkill[id] = strResult;
                            }
                            stringSkill2[tree.Text] = strResult;
                        }
                        break;
                }
            }

            Wz_Node qDataNode = stringNode?.FindNodeByPath("QuestData.img", true);
            Wz_Node qInfoNode = null;
            foreach (Wz_Node node in qDataNode?.Nodes ?? Enumerable.Empty<Wz_Node>())
            {
                Wz_Node tree = node;
                if (node.Value is Wz_Image image)
                {
                    if (image == null)
                        continue;

                    if (!image.TryExtract()) continue;
                    tree = image.Node;
                }
                qInfoNode = tree;
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
                    var infoNode = linkNode.FindNodeByPath("Info");
                    if (infoNode != null)
                    {
                        strResult.Desc = GetDefaultString(infoNode, "0") ?? strResult.Desc;
                        strResult.Quest_DemandBase = GetDefaultString(infoNode, "demand\\base") ?? strResult.Quest_DemandBase;
                        AddAllValue(strResult, infoNode);
                    }
                    else
                    {
                        strResult.Desc = GetDefaultString(linkNode, "0") ?? strResult.Desc;
                        strResult.Quest_DemandBase = GetDefaultString(linkNode, "demand\\base") ?? strResult.Quest_DemandBase;
                    }
                    if (!update) strResult.FullPath = "QuestInfo.img\\" + tree.FullPath;

                    stringQuest[id] = strResult;
                    AddAllValue(strResult, linkNode);
                }
            }

            foreach (Wz_Node node in reactorNode?.Nodes ?? Enumerable.Empty<Wz_Node>())
            {
                Wz_Image image = node.Value as Wz_Image;
                Wz_Node tree = node;
                if (image != null)
                {
                    if (!image.TryExtract()) continue;
                    tree = image.Node;
                }
                Wz_Node infoNode = tree.FindNodeByPath("info");
                if (Int32.TryParse(tree.Text.Replace(".img", ""), out id) && infoNode.ResolveUol() is Wz_Node linkNode && linkNode != null)
                {
                    StringResult strResult = null;
                    if (update)
                    {
                        try { strResult = stringReactor[id]; }
                        catch { }
                    }
                    if (strResult == null) strResult = new StringResult();

                    strResult.Name = GetDefaultString(linkNode, "viewName") ??
                        GetDefaultString(linkNode, "name") ??
                        GetDefaultString(linkNode, "info") ??
                        strResult.Name ?? string.Empty;
                    if (!update) strResult.FullPath = "Reactor\\" + tree.FullPath;

                    //AddAllValue(strResult, linkNode);
                    stringReactor[id] = strResult;
                }
            }

            return this.HasValues;
        }

        private static string GetItemFullPath(int id, string fullPath)
        {
            if (id >= 2000000)
            {
                ItemType type = Item.GetItemType(id);
                return type switch
                {
                    ItemType.Consume => "Consume.img\\" + id,
                    ItemType.Install => "Ins.img\\" + id,
                    ItemType.Etc => "Etc.img\\Etc\\" + id,
                    ItemType.Pet => "Pet.img\\" + id,
                    ItemType.Cash => "Cash.img\\" + id,
                    ItemType.CashPackage => "0910.img\\" + id,
                    _ => fullPath
                };
            }
            else
            {
                GearType type = Gear.GetGearType(id);
                return type switch
                {
                    GearType.body or GearType.head => "Eqp.img\\Eqp\\Skin\\" + id,
                    GearType.faceAccessory or GearType.eyeAccessory or GearType.earrings or GearType.pendant or
                    GearType.belt or GearType.medal or GearType.shoulderPad => "Eqp.img\\Eqp\\Accessary\\" + id,
                    GearType.cap => "Eqp.img\\Eqp\\Cap\\" + id,
                    GearType.cape => "Eqp.img\\Eqp\\Cape\\" + id,
                    GearType.coat => "Eqp.img\\Eqp\\Coat\\" + id,
                    GearType.dragonMask or GearType.dragonPendant or GearType.dragonTail or GearType.dragonWings => "Eqp.img\\Eqp\\Dragon\\" + id,
                    GearType.machineArms or GearType.machineBody or GearType.machineEngine or GearType.machineLegs or GearType.machineTransistors => "Eqp.img\\Eqp\\Mechanic\\" + id,
                    GearType.face or GearType.face2 or GearType.face3 => "Eqp.img\\Eqp\\Face\\" + id,
                    GearType.glove => "Eqp.img\\Eqp\\Glove\\" + id,
                    GearType.hair or GearType.hair2 or GearType.hair3 or GearType.hair4 => "Eqp.img\\Eqp\\Hair\\" + id,
                    GearType.longcoat => "Eqp.img\\Eqp\\Longcoat\\" + id,
                    GearType.pants => "Eqp.img\\Eqp\\Pants\\" + id,
                    GearType.petEquip => "Eqp.img\\Eqp\\PetEquip\\" + id,
                    GearType.ring => "Eqp.img\\Eqp\\Ring\\" + id,
                    GearType.shield or GearType.demonShield or GearType.soulShield => "Eqp.img\\Eqp\\Shield\\" + id,
                    GearType.shoes => "Eqp.img\\Eqp\\Shoes\\" + id,
                    GearType.taming or GearType.taming2 or GearType.taming3 or GearType.tamingChair or GearType.saddle => "Eqp.img\\Eqp\\Taming\\" + id,
                    _ => "Eqp.img\\Eqp\\Weapon\\" + id,
                };
            }
        }
    }
}
