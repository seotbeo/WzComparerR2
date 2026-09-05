using DevComponents.DotNetBar;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WzComparerR2.CharaSim;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;

namespace WzComparerR2.DB2
{


    public partial class DB2Form : DevComponents.DotNetBar.Office2007Form
    {
        public DB2Form()
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
#endif
            Instance = this;
            Db2Theme.Apply(this);
        }
        public static DB2Form Instance;

        /// <summary>
        /// 取得指定索引分頁所附掛的面板，等同舊版的 tabControl1.TabPages[index]。
        /// </summary>
        private DevComponents.DotNetBar.SuperTabControlPanel TabPage(int index)
        {
            return ((DevComponents.DotNetBar.SuperTabItem)this.tabControl1.Tabs[index]).AttachedControl
                as DevComponents.DotNetBar.SuperTabControlPanel;
        }

        /// <summary>Form1_Load 建立完所有 DataViewer 之後才為 true。</summary>
        private bool gridsReady;

        /// <summary>依主程式目前的樣式重新套用配色，主程式切換樣式時由 Entry 呼叫。</summary>
        public void ApplyTheme()
        {
            Db2Theme.Apply(this);
            for (int i = 0; i < DataGrid.Length; i++)
            {
                Db2Theme.Apply(DataGrid[i]);
                Db2Theme.Apply(TempGrid[i]);
            }
        }
        List<string> ColList, ColList1, RowList;
        Dictionary<int, List<string>> RowList1;
        int Row1 = -1;
        int tabIndex = 0;
        DataViewer Grid;
        DataViewer SearchGrid;

        void Dump2(Wz_Node Entry)
        {
            if (Entry != null)
            {
                if (Entry.Value is Wz_Vector)
                {
                    var P = Entry.GetValue<Wz_Vector>();
                    ColList.Add(Entry.GetPathD() + "=" + P.X.ToString() + "," + P.Y.ToString() + ",  ");

                }
                else if (Entry.GetValue("Null") != "Null")
                    ColList.Add(Entry.GetPathD() + "=" + Entry.GetValueEx<string>("-") + ",  ");
                foreach (var E in Entry.Nodes)
                    if (!(E.Value is Wz_Png))
                        Dump2(E);
            }
        }

        void Delete(string s, int index, int count)
        {
            if ((index < 1) | (index > s.Length) | (count <= 0))
                return;
            if (index + count - 1 > s.Length)
                count = s.Length - index + 1;
            s = s.Remove(index - 1, count);
        }
        void DumpData2(Wz_Node Entry)
        {

            Dump2(Entry);
            string FinalStr = "";
            var S = Entry.GetPathD() + ".";
            for (int i = 0; i < ColList.Count; i++)
            {
                ColList[i] = ColList[i].Replace(S, "");
                FinalStr = FinalStr + ColList[i];
            }
            Delete(FinalStr, FinalStr.Length - 2, 1);
            RowList.Add(FinalStr);
            ColList.Clear();
        }

        void DumpData1()
        {
            ColList1 = new List<string>();
            Row1++;
            RowList1.Add(Row1, ColList1);

        }

        void PutGridData1(int Col)
        {

            string[] FinalStr = new string[RowList1.Count + 1];
            foreach (var i in RowList1.Keys)
            {
                for (int j = 0; j < RowList1[i].Count; j++)
                    FinalStr[i] = FinalStr[i] + RowList1[i][j];
                // Delete(FinalStr[i], inttostr(Length(FinalStr[i])) , 1);
                Grid[Col, i].Value = FinalStr[i];
                //RowList1[i].Free;
            }

            RowList1.Clear();
            //    SetLength(FinalStr, 0);
        }
        Wz_Node GetNode(string Path)
        {
            return Db2Host.GetNode(Path);
        }

        void LoadItem()
        {
            var ItemDir = TabPage(tabIndex).Name;
            Wz_Node Child;
            switch (ItemDir)
            {
                case "Etc":
                    Child = GetNode("String/Etc.img/Etc");
                    break;
                case "Install":
                    Child = GetNode("String/Ins.img");
                    break;

                default:
                    Child = GetNode("String/" + ItemDir + ".img");
                    break;

            }
            string ID;
            Bitmap Icon = null;


            foreach (var img in GetNode("Item/" + ItemDir).Nodes)
            {
                if (!Char.IsNumber(img.Text[0]))
                    continue;
                if (ItemDir == "Pet")
                {
                    ID = img.ImgID();
                    if (GetNode("Item/Pet/" + img.Text + "/info/iconD") != null)
                        Icon = GetNode("Item/Pet/" + img.Text + "/info/iconD").ExtractPng();
                    var Name = Child.GetValue2(ID + "/name", "  ");
                    var Desc = Child.GetValue2(ID + "/desc", "  ");
                    DumpData2(GetNode("Item/Pet/" + img.Text + "/info"));
                    Grid.Rows.Add(ID, Icon, Name, Desc, "");
                }
                else if (ItemDir == "Special")
                {
                    if (img.Text != "0910.img")
                        continue;

                    List<string> CashPackages = new List<string>();
                    foreach (var Iter in GetNode("Etc/CashPackage.img").Nodes)
                    {
                        CashPackages.Add(Iter.Text);

                    }

                    foreach (var Iter in GetNode("Item/Special/0910.img").Nodes)
                    {
                        if (CashPackages.Contains(Iter.Text))
                        {
                            ID = Iter.Text.IDString();
                            if (Iter.GetNode("icon") != null)
                                Icon = Iter.GetNode("icon").ExtractPng();
                            Grid.Rows.Add(ID, Icon, Iter.GetValue2("name", "  "), Iter.GetValue2("desc", "  "), " ");
                        }
                    }
                    CashPackages.Clear();
                }
                else
                {
                    foreach (var Iter in GetNode("Item/" + ItemDir + "/" + img.Text).Nodes)
                    {
                        DumpData2(Iter);
                        ID = Iter.Text.IDString();
                        if (Iter.GetNode("info/icon") != null)
                            Icon = Iter.GetNode("info/icon").ExtractPng();
                        Grid.Rows.Add(Iter.Text, Icon, Child.GetValue2(ID + "/name", "  "), Child.GetValue2(ID + "/desc", "  "), " ");
                    }
                }

            }
            for (int i = 0; i < RowList.Count; i++)
                Grid[4, i].Value = RowList[i];
            Grid.Sort(Grid.Columns[0], System.ComponentModel.ListSortDirection.Ascending);

        }


        string GetTypes(string ID)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("분류: ");
            if (int.TryParse(ID, out int gearID))
            {
                GearType type = Gear.GetGearType(gearID);
                sb.Append(ItemStringHelper.GetGearTypeString(type));
            }
            else
            {
                sb.Append("??");
            }
            sb.Append(", ");
            return sb.ToString();
        }

        string ToData(Wz_Node E)
        {
            if (E.Value is Wz_Png)
                return null;
            switch (E.Text)
            {
                case "tradeAvailable":
                    if (E.ValueToInt() == 1)
                        return "카르마의 가위 (일반)";
                    else if (E.ValueToInt() == 2)
                        return "카르마의 가위 (플래티넘)";
                    else break;


                case "reqJob":
                    int reqJob = E.ValueToInt();
                    if (reqJob <= 0)
                    {
                        switch (reqJob)
                        {
                            case -1: return "초보자";
                            case 0: return "공용";
                        }
                    }
                    else
                    {
                        char[] bits = Convert.ToString(reqJob, 2).ToCharArray();
                        List<string> returnList = new List<string>();
                        Array.Reverse(bits);
                        for (int i = 0; i < bits.Length; i++)
                        {
                            if (bits[i] == '1')
                            {
                                switch (i)
                                {
                                    case 0: returnList.Add("전사"); break;
                                    case 1: returnList.Add("마법사"); break;
                                    case 2: returnList.Add("궁수"); break;
                                    case 3: returnList.Add("도적"); break;
                                    case 4: returnList.Add("해적"); break;
                                }
                            }
                        }
                        return string.Join("&", returnList);
                    }
                    break;

                case "attackSpeed":
                    int attackSpeed = E.ValueToInt();
                    return $"{ItemStringHelper.GetAttackSpeedString(attackSpeed)}({attackSpeed})";

                case "bdR":
                case "incBDR":
                case "imdR":
                case "incIMDR":
                case "damR":
                case "nbdR":
                    return E.ValueToStr() + "%";
                case "cash":
                    if (E.ValueToInt() == 0)
                        return "";
                    else if (E.ValueToInt() == 1)
                        return "캐시 아이템";
                    else break;
                case "addition":
                    if (E.HasNode("mobcategory"))
                        return "몬스터 보너스";
                    else if (E.HasNode("boss"))
                        return "보스 보너스";
                    else if (E.HasNode("skill"))
                        return "공격 효과";
                    else if (E.HasNode("critical"))
                        return "크리티컬";
                    else if (E.HasNode("mobdie"))
                        return "처치 효과";
                    else if (E.HasNode("statinc"))
                        return "추가 능력치";
                    else break;
                case "variableStat":
                    if (E.HasNode("incPAD"))
                        return "공격력 증가율: " + E.GetNode("incPAD").Value.ToString();
                    else break;
                case "incRMAI":
                case "incRMAL":
                case "incRMAF":
                case "incRMAS":
                    return (E.ValueToInt() - 100).ToString() + "%";
                default:
                    return E.ValueToStr();
            }

            return null;
        }

        string StrJoin(string Separator, params string[] StringArray)
        {
            var Result = "";
            for (int i = 0; i < StringArray.Length; i++)
                Result = Result + StringArray[i] + Separator;
            Delete(Result, Result.Length, 1);
            return Result;

        }

        string GetJobID(string ID)
        {
            return (int.Parse(ID) / 10000).ToString();
        }

        string LeftStr(string s, int count)
        {
            if (count > s.Length)
                count = s.Length;
            return s.Substring(0, count);
        }

        void LoadCharacter()
        {
            var ToName = new Dictionary<string, string>();
            var ToNameBool = new Dictionary<string, string>();
            ToName.Add("price", "판매 가격: ");
            ToName.Add("reqLevel", "요구 레벨: ");
            ToName.Add("reqJob", "착용 직업: ");
            // ToName.Add("reqSpecJob", "");
            ToName.Add("reqSTR", "REQ STR: ");
            ToName.Add("reqDEX", "REQ DEX: ");
            ToName.Add("reqINT", "REQ INT: ");
            ToName.Add("reqLUK", "REQ LUK: ");
            ToName.Add("reqPOP", "요구 인기도: ");
            ToName.Add("attackSpeed", "공격 속도: ");
            ToName.Add("incSTR", "STR +");
            ToName.Add("incDEX", "DEX +");
            ToName.Add("incINT", "INT +");
            ToName.Add("incLUK", "LUK +");
            ToName.Add("incMHP", "최대 HP +");
            ToName.Add("incMMP", "최대 MP +");
            ToName.Add("incPAD", "공격력 +");
            ToName.Add("incMAD", "마력 +");
            ToName.Add("incPDD", "방어력 +");
            ToName.Add("incMDD", "마법방어력 +");
            ToName.Add("incACC", "명중치 +");
            ToName.Add("incEVA", "회피치 +");
            ToName.Add("incSpeed", "이동속도 +");
            ToName.Add("incJump", "점프력 +");
            ToName.Add("incCraft", "손재주 +");
            ToName.Add("craftEXP", "손재주 +");

            ToName.Add("incPVPDamage", "대난투 추가 공격력 +");
            ToName.Add("bdR", "보스 몬스터 공격 시 데미지 +");
            ToName.Add("imdR", "몬스터 방어율 무시 +");
            ToName.Add("willEXP", "의지 +");
            ToName.Add("charmEXP", "매력 +");
            ToName.Add("charismaEXP", "카리스마 +");
            ToName.Add("knockback", "직접 타격 시 넉백 확률: ");
            ToName.Add("reduceReq", "착용 레벨 감소: ");

            ToName.Add("incRMAI", "얼음 속성 마법 +");
            ToName.Add("incRMAL", "번개 속성 마법 +");
            ToName.Add("incRMAF", "불 속성 마법 +");
            ToName.Add("incRMAS", "독 속성 마법 +");
            ToName.Add("durability", "내구도: ");
            ToName.Add("tuc", "업그레이드 가능 횟수: ");

            ToName.Add("level", "");
            ToName.Add("head", "");
            ToName.Add("option", "");
            ToName.Add("addition", "");
            ToNameBool.Add("expireOnLogout", "로그아웃 시 소멸");
            ToNameBool.Add("tradeBlock", "교환 불가");
            ToNameBool.Add("notSale", "판매 불가");
            ToNameBool.Add("only", "중복 소지 불가");
            ToNameBool.Add("equipTradeBlock", "장착 시 교환 불가");
            ToNameBool.Add("epicItem", "레전드리 아이템");
            ToNameBool.Add("timeLimited", "기간제 아이템");
            ToNameBool.Add("noExtend", "유효기간 연장 불가");
            ToNameBool.Add("notExtend", "유효기간 연장 불가");
            ToNameBool.Add("quest", "퀘스트 아이템");
            ToNameBool.Add("accountSharable", "월드 내 나의 캐릭터 간 이동만 가능");
            ToNameBool.Add("exItem", "세트 아이템");
            ToNameBool.Add("randVariation", "randVariation");
            ToNameBool.Add("jokerToSetItem", "럭키 아이템");
            ToNameBool.Add("noPrism", "염색하기 불가");
            ToNameBool.Add("collabo", "콜라보 아이템");

            ToName.Add("tradeAvailable", "");
            ToName.Add("cash", "");

            ToName.Add("mobcategory", "몬스터 보너스");
            ToName.Add("boss", "보스 보너스");
            ToName.Add("skill", "공격 효과");
            ToName.Add("critical", "크리티컬");
            ToName.Add("mobdie", "처치 효과");
            ToName.Add("statinc", "추가 능력치");

            var Dir = TabPage(tabIndex).Name;
            if (GetNode("Character/" + Dir) == null)
            {
                MessageBoxEx.Show(this, Dir + "  항목을 찾을 수 없습니다");
                return;
            }

            Wz_Node Child = null;
            switch (Dir)
            {
                case "Totem":
                    Child = GetNode("String/Eqp.img/Eqp/Accessory");
                    break;
                case "TamingMob":
                    Child = GetNode("String/Eqp.img/Eqp/Taming");
                    break;
                default:
                    Child = GetNode("String/Eqp.img/Eqp/" + Dir);
                    break;
            }
            var Row = -1;
            string ID, Desc, h1;
            Bitmap Icon = null;
            string IName, Data = "", D;
            foreach (var img in GetNode("Character/" + Dir).Nodes)
            {
                if (LeftStr(img.Text, 1) != "0")
                    continue;
                Row += 1;
                DumpData2(img.GetNode("info"));
                DumpData1();
                switch (Dir)
                {
                    case "Hair":
                        if (img.GetNode("default/hairOverHead") != null)
                            Icon = img.GetNode("default/hairOverHead").ExtractPng();
                        break;
                    case "Face":
                        if (img.GetNode("default/face") != null)
                            Icon = img.GetNode("default/face").ExtractPng();
                        break;
                    default:
                        if (img.GetNode("info/icon") != null)
                            Icon = img.GetNode("info/icon").ExtractPng();
                        break;
                }
                ID = img.ImgID().IDString();
                ColList1.Add(GetTypes(ID));
                Desc = Child.GetValue2(ID + "/desc", "");
                h1 = Child.GetValue2(ID + "/h1", "");
                RowList[Row] += ", " + Desc + h1;

                Grid.Rows.Add(img.ImgID(), Icon, Child.GetValue2(ID + "/name", ""), "", RowList[Row]);
                foreach (var Iter in GetNode("Character/" + Dir + '/' + img.Text).Nodes)
                {
                    foreach (var Iter2 in Iter.Nodes)
                    {
                        if ((Iter.Text == "info") && (!(Iter2.Value is Wz_Png)))
                        {
                            if (ToName.ContainsKey(Iter2.Text))
                            {
                                IName = ToName[Iter2.Text];
                                Data = ToData(Iter2);
                                if ((Iter2.Text == "cash") && (Iter2.ValueToStr() == "0"))
                                    D = "";
                                else
                                    D = ", ";
                                if ((Data != "0") && (Data != ""))
                                    ColList1.Add(ToName[Iter2.Text] + Data + D);
                            }
                            else if (ToNameBool.ContainsKey(Iter2.Text))
                            {
                                ColList1.Add(ToNameBool[Iter2.Text] + ", ");
                            }
                            else
                            {
                                IName = Iter2.Text;
                                if (Iter2.Value is string)
                                    Data = Iter2.Value.ToString();
                                if (Data == "1")
                                    D = "=" + Data + ", ";
                                else
                                    D = ", ";

                                if ((Iter2.Text != "afterImage") && (Iter2.Text != "islot") && (Iter2.Text != "vslot")
                                  && (Iter2.Text != "walk") && (Iter2.Text != "stand") && (Iter2.Text != "sfx") && (Iter2.Text
                                     != "attack") && (Iter2.Text != "setItemID"))
                                    ColList1.Add(Iter2.Text + D);
                            }

                            foreach (var Iter3 in Iter2.Nodes)
                            {
                                if (ToName.ContainsKey(Iter3.Text))
                                    ColList1.Add(ToName[Iter3.Text] + ", ");
                            }

                        }

                    }

                }

                if (Desc != "")
                    ColList1.Add(" " + Desc);
                if (h1 != "")
                    ColList1.Add(" " + h1);
            }

            if (Dir == "TamingMob")
            {
                var Dict = new Dictionary<string, string>();
                for (int i = 11; i <= 28; i++)
                {
                    if (GetNode("Skill/8000" + i.ToString() + ".img") != null)
                    {
                        foreach (var Iter in GetNode("Skill/8000" + i.ToString() + ".img/skill").Nodes)
                        {
                            if ((Iter.GetNode("vehicleID") != null) && (!Dict.ContainsKey("0" + Iter.GetNode("vehicleID").Value.ToString())))
                                Dict.Add("0" + Iter.GetNode("vehicleID").Value.ToString(), Iter.Text);

                        }
                    }
                }
                for (int i = 0; i <= 9; i++)
                {
                    if (GetNode("Skill/80011" + i.ToString() + ".img") != null)
                    {
                        foreach (var Iter in GetNode("Skill/80011" + i.ToString() + ".img/skill").Nodes)
                        {
                            if ((Iter.GetNode("vehicleID") != null) && (!Dict.ContainsKey("0" + Iter.GetNode("vehicleID").Value.ToString())))
                                Dict.Add("0" + Iter.GetNode("vehicleID").Value.ToString(), Iter.Text);

                        }
                    }
                }

                for (int i = 0; i < Grid.RowCount - 1; i++)
                {
                    // if (Grid[0, i].Value is string)
                    {
                        var TamingID = Grid[0, i].Value.ToString();
                        // if ((Grid[2, i].Value == "") && (Dict.ContainsKey(TamingID)))
                        if (Dict.ContainsKey(TamingID))
                            if (GetNode("String/Skill.img/" + Dict[TamingID]) != null)
                                Grid[2, i].Value = GetNode("String/Skill.img/" + Dict[TamingID]).GetValue2("name", " ");
                    }

                }

            }

            PutGridData1(3);
            Grid.Sort(Grid.Columns[0], System.ComponentModel.ListSortDirection.Ascending);


        }

        void LoadMap(int Part)
        {

            var Links = new List<(string, int)>();
            var MapNames = new Dictionary<string, (string, string)>();
            string StreetName, MapName;
            foreach (var Iter in GetNode("String/Map.img").Nodes)
            {
                foreach (var Iter2 in Iter.Nodes)
                {
                    string ID = Iter2.Text.PadLeft(9, '0');
                    StreetName = Iter2.GetValue2("streetName", "");
                    MapName = Iter2.GetValue2("mapName", "");
                    if (!MapNames.ContainsKey(ID))
                        MapNames.Add(ID, (StreetName, MapName));
                }
            }
            Wz_Node MapDir;

            MapDir = GetNode("Map/Map");
            Bitmap Icon;
            ;
            var Row = -1;

            foreach (var Dir in MapDir.Nodes)
            {
                if (LeftStr(Dir.Text, 3) != "Map")
                    continue;
                switch (Part)
                {
                    case 1:
                        if ((Dir.Text != "Map0") && (Dir.Text != "Map1") && (Dir.Text != "Map2") && (Dir.Text != "Map3"))
                            continue;
                        break;
                    case 2:
                        if ((Dir.Text != "Map4") && (Dir.Text != "Map5") && (Dir.Text != "Map6") && (Dir.Text != "Map7") && (Dir.Text != "Map8"))
                            continue;
                        break;
                    case 3:
                        if (Dir.Text != "Map9")
                            continue;
                        break;
                }

                foreach (var img in Dir.Nodes)
                {
                    if (!Char.IsNumber(img.Text[0]))
                        continue;
                    Row += 1;
                    DumpData2(img.GetNode("info"));

                    if (MapNames.ContainsKey(img.ImgID()))
                    {
                        StreetName = MapNames[img.ImgID()].Item1;
                        MapName = MapNames[img.ImgID()].Item2;
                    }
                    else
                    {
                        StreetName = "";
                        MapName = "";
                    }

                    if (img.GetNode("miniMap/canvas") != null)
                        Icon = img.GetNode("miniMap/canvas").ExtractPng();
                    else
                        Icon = null;

                    Grid.Rows.Add(img.ImgID(), Icon, StreetName, MapName, "");
                    var Link = img.GetNode("info/link");
                    if (Link != null)
                        Links.Add(("Map" + LeftStr(Link.Value.ToString(), 1) + "/" + Link.Value.ToString() + ".img", Row));
                }

            }

            for (int i = 0; i < Links.Count; i++)
            {
                var Child = MapDir.GetNode(Links[i].Item1 + "/miniMap/canvas");
                if (Child != null)
                    Grid[1, Links[i].Item2].Value = Child.ExtractPng();
            }
            for (int i = 0; i < RowList.Count; i++)
            {
                Grid[4, i].Value = RowList[i];
            }
            Grid.Sort(Grid.Columns[0], System.ComponentModel.ListSortDirection.Ascending);

        }

        string s1(string S)
        {
            switch (S[0].ToString())
            {
                case "L":
                    return "번개";
                case "F":
                    return "불";
                case "I":
                    return "얼음";
                case "S":
                    return "독";
                case "D":
                    return "암흑";
                case "P":
                    return "물리";
                case "H":
                    return "성";
            }
            return null;
        }

        string s2(string S)
        {
            switch (S[1].ToString())
            {
                case "1":
                    return "무시";
                case "2":
                    return "반감";
                case "3":
                    return "약점";
            }
            return null;
        }

        string Copy(string s, int index, int count)
        {
            if (index < 1)
                index = 1;
            if ((index > s.Length) || (count <= 0))
            {
                return "";
                // exit;
            }
            if (index + count - 1 > s.Length)
                count = s.Length - index + 1;
            return s.Substring(index - 1, count);
        }

        string ElemName(string S)
        {
            string A, D;
            int Num;
            Num = S.Length / 2;
            string Result = "";
            for (int i = 1; i <= Num; i++)
            {
                A = Copy(S, i * 2 - 1, 2);
                if (i < Num)
                    D = "/";
                else
                    D = "";
                Result = Result + s1(A) + s2(A) + D;
            }
            return Result;
        }
        string RightStr(string s, int count)
        {
            if (count > s.Length)
                count = s.Length;
            return s.Substring(s.Length - count, count);

        }

        void LoadMob(int Part)
        {

            var ToName = new Dictionary<string, string>();
            var Category = new Dictionary<string, string>();
            Category.Add("1", "동물형");
            Category.Add("2", "식물형");
            Category.Add("3", "어류형");
            Category.Add("4", "파충류형");
            Category.Add("5", "정령형");
            Category.Add("6", "악마형");
            Category.Add("7", "불사형");
            Category.Add("8", "무형");

            ToName.Add("level", "레벨: ");
            ToName.Add("exp", "경험치: ");
            ToName.Add("maxMP", "MP: ");
            ToName.Add("maxHP", "HP: ");
            ToName.Add("speed", "이동속도: ");
            ToName.Add("acc", "명중치: ");
            ToName.Add("pushed", "넉백: ");
            ToName.Add("category", "분류: ");
            ToName.Add("eva", "회피치: ");
            ToName.Add("link", "링크: ");
            ToName.Add("elemAttr", "속성: ");
            ToName.Add("MADamage", "마법 공격력: ");
            ToName.Add("MDDamage", "마법 방어력: ");
            ToName.Add("PADamage", "물리 공격력: ");
            ToName.Add("PDDamage", "물리 방어력: ");
            ToName.Add("PDRate", "물리 방어율: ");
            ToName.Add("MDRate", "마법 방어율: ");
            ToName.Add("boss", "보스 ");
            ToName.Add("firstAttack", "선제공격");
            ToName.Add("charismaEXP", "카리스마 경험치: ");
            ToName.Add("hpRecovery", "HP 회복: ");
            ToName.Add("mpRecovery", "MP 회복: ");
            if (Db2Host.HasMob001)
            {
                var Links = new List<(string, int)>();
                Wz_Node Child = null;
                var Row = -1;
                string Data = "", D = "";
                Bitmap Icon = null;

                foreach (var Iter in GetNode("String/Mob.img").Nodes)
                {
                    var ID = Iter.Text.PadLeft(7, '0');
                    if (GetNode("Mob/" + ID + ".img") == null)
                        continue;

                    var LeftNum = LeftStr(ID, 1);
                    switch (Part)
                    {
                        case 1:
                            if (LeftNum == "8" || LeftNum == "9")
                                continue;
                            break;
                        case 2:
                            if (LeftNum == "0" || LeftNum == "1" || LeftNum == "2" || LeftNum == "3" || LeftNum == "4" || LeftNum == "5" || LeftNum == "6" || LeftNum == "7" || LeftNum == "9")
                                continue;
                            break;
                        case 3:
                            if (LeftNum == "0" || LeftNum == "1" || LeftNum == "2" || LeftNum == "3" || LeftNum == "4" || LeftNum == "5" || LeftNum == "6" || LeftNum == "7" || LeftNum == "8")
                                continue;
                            break;
                    }

                    Row += 1;
                    DumpData1();
                    DumpData2(GetNode("Mob/" + ID + ".img"));

                    if (GetNode("Mob/" + ID + ".img/info/link") != null)
                    {
                        Links.Add((GetNode("Mob/" + ID + ".img/info/link").Value.ToString(), Row));
                        //  continue;
                    }

                    if (GetNode("Mob/" + ID + ".img/stand/0") != null)
                        Child = GetNode("Mob/" + ID + ".img/stand/0");
                    else if (GetNode("Mob/" + ID + ".img/fly/0") != null)
                        Child = GetNode("Mob/" + ID + ".img/fly/0");
                    if (Child != null)
                        Icon = Child.ExtractPng();
                    Grid.Rows.Add(ID, Icon, GetNode("String/Mob.img/" + Iter.Text).GetValue2("name", ""), "");

                    foreach (var Iter2 in GetNode("Mob/" + ID + ".img" + "/info").Nodes)
                    {
                        if ((Iter2.Text == "category") && (Category.ContainsKey(Iter2.ValueToStr())))
                            Data = Category[Iter2.ValueToStr()];

                        else if (Iter2.Text == "elemAttr")
                            Data = ElemName(Iter2.ValueToStr());

                        else if ((Iter2.Text == "boss") || (Iter2.Text == "firstAttack"))
                            Data = "";
                        else
                            Data = Iter2.ValueToStr();

                        if ((Iter2.Text == "PDRate") || (Iter2.Text == "MDRate"))
                            D = "%";
                        else
                            D = "";

                        if (ToName.ContainsKey(Iter2.Text))
                            // Grid.Cells[4 + Col, Row] := ToName[Iter2.Name] + Data + D + ',  ';
                            ColList1.Add(ToName[Iter2.Text] + Data + D + ",  ");

                    }


                }

                for (int i = 0; i < Links.Count; i++)
                {
                    if (GetNode("Mob/" + Links[i].Item1 + ".img/stand/0") != null)
                        Child = GetNode("Mob/" + Links[i].Item1 + ".img/stand/0");
                    else if (GetNode("Mob/" + Links[i].Item1 + ".img/fly/0") != null)
                        Child = GetNode("Mob/" + Links[i].Item1 + ".img/fly/0");
                    Grid[1, Links[i].Item2].Value = Child.ExtractPng();

                }
                for (int i = 0; i < RowList.Count; i++)
                {
                    Grid[4, i].Value = RowList[i];
                }
                PutGridData1(3);
                Grid.Sort(Grid.Columns[0], System.ComponentModel.ListSortDirection.Ascending);


            }
            else
            {
                var Links = new List<(string, int)>();
                Wz_Node Child = null;
                var Row = -1;
                string Data = "", D = "";
                Bitmap Icon = null;
                foreach (var Iter in GetNode("Mob").Nodes)
                {
                    if (RightStr(Iter.Text, 4) != ".img")
                        continue;
                    var LeftNum = LeftStr(Iter.Text, 1);
                    switch (Part)
                    {
                        case 1:
                            if (LeftNum == "8" || LeftNum == "9")
                                continue;
                            break;
                        case 2:
                            if (LeftNum == "0" || LeftNum == "1" || LeftNum == "2" || LeftNum == "3" || LeftNum == "4" || LeftNum == "5" || LeftNum == "6" || LeftNum == "7" || LeftNum == "9")
                                continue;
                            break;
                        case 3:
                            if (LeftNum == "0" || LeftNum == "1" || LeftNum == "2" || LeftNum == "3" || LeftNum == "4" || LeftNum == "5" || LeftNum == "6" || LeftNum == "7" || LeftNum == "8")
                                continue;
                            break;
                    }

                    Row += 1;
                    if (GetNode("Mob/" + Iter.Text + "/stand/0") != null)
                        Child = GetNode("Mob/" + Iter.Text + "/stand/0");
                    else if (GetNode("Mob/" + Iter.Text + "/fly/0") != null)
                        Child = GetNode("Mob/" + Iter.Text + "/fly/0");
                    DumpData1();
                    DumpData2(GetNode(Iter.Text));
                    if (Child != null)
                        Icon = Child.ExtractPng();
                    Grid.Rows.Add(Iter.ImgID(), Icon, GetNode("String/Mob.img/" + Iter.ImgID().IDString()).GetValue2("name", ""), "");

                    //return;
                    var Link = Iter.GetNode("info/link");
                    if (Link != null)
                        Links.Add((Link.Value.ToString() + ".img", Row));

                    foreach (var Iter2 in GetNode("Mob/" + Iter.Text + "/info").Nodes)
                    {
                        if ((Iter2.Text == "category") && (Category.ContainsKey(Iter2.ValueToStr())))
                            Data = Category[Iter2.ValueToStr()];

                        else if (Iter2.Text == "elemAttr")
                            Data = ElemName(Iter2.ValueToStr());

                        else if ((Iter2.Text == "boss") || (Iter2.Text == "firstAttack"))
                            Data = "";
                        else
                            Data = Iter2.ValueToStr();

                        if ((Iter2.Text == "PDRate") || (Iter2.Text == "MDRate"))
                            D = "%";
                        else
                            D = "";

                        if (ToName.ContainsKey(Iter2.Text))
                            // Grid.Cells[4 + Col, Row] := ToName[Iter2.Name] + Data + D + ',  ';
                            ColList1.Add(ToName[Iter2.Text] + Data + D + ",  ");

                    }

                }

                for (int i = 0; i < Links.Count; i++)
                {
                    if (GetNode("Mob/" + Links[i].Item1 + "/stand/0") != null)
                        Child = GetNode("Mob/" + Links[i].Item1 + "/stand/0");
                    else if (GetNode("Mob/" + Links[i].Item1 + "/fly/0") != null)
                        Child = GetNode("Mob/" + Links[i].Item1 + "/fly/0");

                    Grid[1, Links[i].Item2].Value = Child.ExtractPng();
                }
                for (int i = 0; i < RowList.Count; i++)
                {
                    Grid[4, i].Value = RowList[i];
                }
                PutGridData1(3);
                Grid.Sort(Grid.Columns[0], System.ComponentModel.ListSortDirection.Ascending);
            }


        }

        // 原版使用 MapleStoryDB2 附帶的原生 Eval2.dll 計算公式；
        // 這裡改用 WzComparerR2 內建的 Calculator，可跨 x64 / ARM64 並免除原生相依。
        double GetFValue(string FormStr, int Level)
        {
            if (string.IsNullOrEmpty(FormStr))
                return 0;
            try
            {
                return (double)Calculator.Parse(FormStr, Level);
            }
            catch
            {
                return 0;
            }
        }

        Wz_Node Common;
        int MaxLev;
        string CommonMatch(System.Text.RegularExpressions.Match Match1)
        {

            string MatchName = Copy(Match1.Value, 2, 100);
            foreach (var Iter in Common.Nodes)
            {
                if (Iter.Text == MatchName)
                    return GetFValue(Iter.Value.ToString(), MaxLev).ToString();
            }
            return null;
        }
        void LoadSkill()
        {
            Bitmap Icon;
            foreach (var L1 in Db2Host.TreeNode.Nodes)
            {
                if (LeftStr(L1.Text, 5) != "Skill")
                    continue;

                foreach (var img in L1.Nodes)
                {
                    if (Char.IsNumber(img.Text, 0))
                    {
                        var LeftNum = LeftStr(img.Text, 1);

                        DumpData2(GetNode("Skill/" + img.Text + "/info"));

                        var BookID = img.ImgID();
                        string BookName = "";
                        if (GetNode("String/Skill.img/" + BookID) != null)
                            BookName = GetNode("String/Skill.img/" + BookID).GetValue2("bookName", "");

                        if (GetNode("Skill/" + img.Text + "/info/icon") != null)
                            Icon = GetNode("Skill/" + img.Text + "/info/icon").ExtractPng();
                        else
                            Icon = null;

                        Grid.Rows.Add(BookID, Icon, BookName, "", "");

                        foreach (var Iter in GetNode("Skill/" + img.Text).Nodes)
                        {
                            foreach (var Iter2 in Iter.Nodes)
                            {

                                if (Iter.Text == "skill")
                                {
                                    DumpData2(Iter2);
                                    var SkillID = Iter2.Text;
                                    if (Iter2.GetNode("icon") != null)
                                        Icon = Iter2.GetNode("icon").ExtractPng();
                                    string SkillName = "", Desc = "";
                                    if (GetNode("String/Skill.img/" + SkillID) != null)
                                    {
                                        SkillName = GetNode("String/Skill.img/" + SkillID).GetValue2("name", "");
                                        Desc = GetNode("String/Skill.img/" + SkillID).GetValue2("desc", "");
                                    }
                                    string hDesc = "";
                                    var Child = GetNode("String/Skill.img/" + SkillID);
                                    if (Child != null)
                                    {
                                        if (Child.GetNode("h") != null)
                                        {
                                            if (Child.GetNode("h").Value is string)
                                            {
                                                hDesc = Child.GetNode("h").Value.ToString();
                                            }
                                            hDesc = hDesc.Replace("mpConMP", "mpCon MP");
                                            hDesc = hDesc.Replace(",", " ,");
                                            Common = Iter2.GetNode("common");
                                            if (Common != null)
                                            {
                                                MaxLev = Common.GetValue2("maxLevel", 1);
                                                if (hDesc != "")
                                                {
                                                    hDesc = "Lv." + MaxLev.ToString() + "= " + Regex.Replace(hDesc, "\\#[0-9,_,a-z,A-Z,\\.]+", CommonMatch);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            for (int i = 1; i <= 30; i++)
                                            {
                                                if (Child.GetNode("h" + i.ToString()) != null)
                                                    hDesc = "Lv." + i.ToString() + "= " + Child.GetNode("h" + i.ToString()).Value.ToString();
                                            }
                                        }
                                    }

                                    Grid.Rows.Add(SkillID, Icon, SkillName, Desc, hDesc, "");
                                }
                            }
                        }
                    }
                }
            }
            for (int i = 0; i < RowList.Count; i++)
                Grid[5, i].Value = RowList[i];

            Grid.Sort(Grid.Columns[0], System.ComponentModel.ListSortDirection.Ascending);

        }
        void LoadNpc()
        {
            var Row = -1;
            var Links = new List<(string, int)>();
            Bitmap Icon = null;
            foreach (var Img in GetNode("Npc").Nodes)
            {
                if (!Char.IsNumber(Img.Text[0]))
                    continue;
                Row += 1;
                var ID = Img.ImgID();
                var Entry = GetNode("Npc/" + Img.Text);
                if (Entry.GetNode("stand/0") != null)
                    Icon = Entry.GetNode("stand/0").ExtractPng();
                if (Entry.GetNode("default/0") != null)
                    Icon = Entry.GetNode("default/0").ExtractPng();
                var Name = GetNode("String/Npc.img/" + ID.IDString()).GetValue2("name", "");
                DumpData2(GetNode("String/Npc.img/" + ID.IDString()));
                Grid.Rows.Add(ID, Icon, Name, "");
                var Link = GetNode("Npc/" + Img.Text + "/info/link");
                if (Link != null)
                    Links.Add((Link.Value.ToString() + ".img", Row));
            }
            Wz_Node Child = null;
            for (int i = 0; i < Links.Count; i++)
            {
                if (GetNode("Npc/" + Links[i].Item1 + "/stand/0") != null)
                    Child = GetNode("Npc/" + Links[i].Item1 + "/stand/0");
                else if (GetNode("Npc/" + Links[i].Item1 + "/default/0") != null)
                    Child = GetNode("Npc/" + Links[i].Item1 + "/default/0");
                Grid[1, Links[i].Item2].Value = Child.ExtractPng();
            }
            for (int i = 0; i < RowList.Count; i++)
                Grid[3, i].Value = RowList[i];
            ColList.Clear();
            RowList.Clear();
            foreach (var Img in GetNode("Npc").Nodes)
            {
                DumpData2(GetNode("Npc/" + Img.Text + "/info"));
            }
            for (int i = 0; i < RowList.Count; i++)
            {
                Grid[4, i].Value = RowList[i];
            }
            Grid.Sort(Grid.Columns[0], System.ComponentModel.ListSortDirection.Ascending);

        }
        void LoadMorph()
        {
            var Dict = new Dictionary<string, (string, string)>();
            var imgs = new List<string>();
            string Desc = "", Name = "";
            foreach (var Iter in GetNode("String/Consume.img").Nodes)
            {
                Desc = Iter.GetValue2("desc", "");
                Name = Iter.GetValue2("name", "");
                Dict.Add(Iter.Text, (Name, Desc));
            }
            foreach (var img in GetNode("Morph").Nodes)
            {
                if (!Char.IsNumber(img.Text[0]))
                    continue;
                imgs.Add(img.ImgID());
            }
            Bitmap Icon = null;
            Bitmap MorphPic = null;
            foreach (var Iter in GetNode("Item/Consume/0221.img").Nodes)
            {
                DumpData2(Iter);
                var ID = Iter.Text;
                if (Dict.ContainsKey(Iter.Text.IDString()))
                {
                    Name = Dict[Iter.Text.IDString()].Item1;
                    Desc = Dict[Iter.Text.IDString()].Item2;
                }
                if (Iter.GetNode("info/icon") != null)
                    Icon = Iter.GetNode("info/icon").ExtractPng();
                var MorphID = Iter.GetValue2("spec/morph", "").PadLeft(4, '0');
                if (imgs.Contains(MorphID))
                {
                    if (GetNode("Morph/" + MorphID + ".img/walk/0") != null)
                        MorphPic = GetNode("Morph/" + MorphID + ".img/walk/0").ExtractPng();
                }
                Grid.Rows.Add(ID, Icon, MorphID, MorphPic, Name, Desc, "");
            }

            for (int i = 0; i < RowList.Count; i++)
                Grid[6, i].Value = RowList[i];
        }

        void LoadFamiliar()
        {
            if (GetNode("Character/Familiar") == null)
            {
                MessageBoxEx.Show(this, "패밀리어를 찾을 수 없습니다");
                return;
            }
            Wz_Node CardEntry;
            Bitmap MobPic = null, Icon = null;
            string CardID = "", MobID = "";
            foreach (var img in GetNode("Character/Familiar").Nodes)
            {
                var ID = img.ImgID();
                var Entry = GetNode("Character/Familiar/" + img.Text);
                if (Entry.GetNode("info/MobID") != null)
                    MobID = Entry.GetNode("info/MobID").Value.ToString().PadLeft(7, '0');
                else if (GetNode("Etc/FamiliarInfo.img") != null)
                    MobID = GetNode("Etc/FamiliarInfo.img/" + ID).GetValue2("mob", "100100").PadLeft(7, '0');

                DumpData2(Entry.GetNode("info"));

                if (GetNode("Mob/" + MobID + ".img") != null)
                {
                    if (GetNode("Mob/" + MobID + ".img/stand/0") != null)
                        MobPic = GetNode("Mob/" + MobID + ".img/stand/0").ExtractPng();
                    else if (GetNode("Mob/" + MobID + ".img/fly/0") != null)
                        MobPic = GetNode("Mob/" + MobID + ".img/fly/0").ExtractPng();
                }

                string SkillID = "";
                if (Entry.GetNode("info/skill") != null)
                    SkillID = Entry.GetNode("info/skill").GetValue2("id", "");
                string SkillName = "", SkillDesc = "";
                if (GetNode("String/FamiliarSkill.img") != null)
                {
                    if (GetNode("String/FamiliarSkill.img/skill/" + SkillID) != null)
                    {
                        SkillName = SkillID + ":" + GetNode("String/FamiliarSkill.img/skill/" + SkillID).GetValue2("name", "");
                        SkillDesc = GetNode("String/FamiliarSkill.img/skill/" + SkillID).GetValue2("desc", "");
                    }
                }

                else if (GetNode("String/Familiar.img") != null)
                {
                    if (GetNode("String/Familiar.img/skill/" + SkillID) != null)
                    {
                        SkillName = SkillID + ":" + GetNode("String/Familiar.img/skill/" + SkillID).GetValue2("name", "");
                        SkillDesc = GetNode("String/Familiar.img/skill/" + SkillID).GetValue2("desc", "");
                    }
                }
                if (GetNode("Etc/FamiliarInfo.img") != null)
                {
                    if (GetNode("Etc/FamiliarInfo.img/" + ID) != null)
                        CardID = GetNode("Etc/FamiliarInfo.img/" + ID).GetValue2("consume", "");
                }
                else
                {
                    if (Entry.GetNode("info/monsterCardID") != null)
                        CardID = Entry.GetNode("info/monsterCardID").Value.ToString();
                    else
                        CardID = "";
                }

                if (GetNode("Item/Consume/0287.img/0" + CardID) != null)
                {
                    CardEntry = GetNode("Item/Consume/0287.img/0" + CardID);
                    if (CardEntry.GetNode("info/icon") != null)
                        Icon = CardEntry.GetNode("info/icon").ExtractPng();
                }
                else if (GetNode("Item/Consume/0238.img/0" + CardID) != null)
                {
                    CardEntry = GetNode("Item/Consume/0238.img/0" + CardID);
                    if (CardEntry.GetNode("info/iconRaw") != null)
                        Icon = CardEntry.GetNode("info/iconRaw").ExtractPng();
                }
                else
                    Icon = null;

                var CardName = GetNode("String/Consume.img/" + CardID).GetValue2("name", "");
                Grid.Rows.Add(ID, MobPic, "", SkillName, SkillDesc, CardID, Icon, CardName);
            }

            for (int i = 0; i < RowList.Count; i++)
                Grid[2, i].Value = RowList[i];
            Grid.Sort(Grid.Columns[0], System.ComponentModel.ListSortDirection.Ascending);
        }
        void LoadDamageSkin()
        {
            Bitmap Icon = null, Sample = null;
            Bitmap bb = new Bitmap(20, 20);

            foreach (var Iter in GetNode("String/Consume.img").Nodes)
            {
                var Name = Iter.GetValue2("name", "");
                if ((Name.Contains("字型")) || (Name.Contains("ジスキン")) || (Name.Contains("스킨")) || (Name.Contains
                  ("Damage Skin")) || (Name.Contains("字型")) || (Name.Contains("伤害皮肤")))
                {
                    var ID = "0" + Iter.Text;
                    string[] imgs = new string[] { "0243.img", "0263.img" };
                    for (int i = 0; i <= 1; i++)
                    {
                        var Entry = GetNode("Item/Consume/" + imgs[i] + "/0" + Iter.Text + "/info/icon");
                        if (Entry != null)
                            Icon = Entry.ExtractPng();

                        var Entry2 = GetNode("Item/Consume/" + imgs[i] + "/0" + Iter.Text + "/info/sample");
                        if (Entry2 != null)
                        {
                            if (Entry2.GetNode("0") != null)
                                Sample = Entry2.GetNode("0").ExtractPng();
                            else
                                Sample = Entry2.ExtractPng();
                        }

                    }

                    var Desc = Iter.GetValue2("desc", "");

                    Grid.Rows.Add(ID, Icon, Name, Sample, Desc);
                }
            }
            Grid.Sort(Grid.Columns[0], System.ComponentModel.ListSortDirection.Ascending);
        }
        void LoadReactor()
        {
            var Row = -1;
            var Links = new List<(string, int)>();
            Bitmap Icon = null;

            foreach (var img in GetNode("Reactor").Nodes)
            {
                if (!Char.IsNumber(img.Text[0]))
                    continue;
                Row += 1;
                DumpData2(GetNode("Reactor/" + img.Text + "/info"));
                var ID = img.ImgID();
                var Entry = GetNode("Reactor/" + img.Text + "/0/0");
                if (Entry != null)
                    Icon = Entry.ExtractPng();
                Entry = GetNode("Reactor/" + img.Text + "/info/link");
                if (Entry != null)
                    Links.Add((Entry.Value.ToString(), Row));
                Grid.Rows.Add(ID, Icon, "");
            }

            Wz_Node Child = null;
            for (int i = 0; i < Links.Count; i++)
            {
                if (GetNode("Reactor/" + Links[i].Item1 + ".img/0/0") != null)
                    Child = GetNode("Reactor/" + Links[i].Item1 + ".img/0/0");
                Grid[1, Links[i].Item2].Value = Child.ExtractPng();
            }
            for (int i = 0; i < RowList.Count; i++)
            {
                Grid[2, i].Value = RowList[i];
            }
            Grid.Sort(Grid.Columns[0], System.ComponentModel.ListSortDirection.Ascending);

        }

        void LoadMusic()
        {
            foreach (var Iter in Db2Host.TreeNode.Nodes)
            {
                if (LeftStr(Iter.Text, 5) == "Sound")
                {
                    foreach (var Iter2 in Iter.Nodes)
                    {
                        if (LeftStr(Iter2.Text, 3) == "Bgm" || LeftStr(Iter2.Text, 4) == "PL_3" || LeftStr(Iter2.Text, 4) == "PL_B" || LeftStr(Iter2.Text, 4) == "PL_C" || LeftStr(Iter2.Text, 4) == "PL_M")
                        {
                            var imgNode = GetNode(Iter2.FullPathToFile2());
                            if (imgNode != null)
                            {
                                AddMusicRows(imgNode);
                            }
                        }
                    }
                }
            }

            Grid.Sort(Grid.Columns[0], System.ComponentModel.ListSortDirection.Ascending);
        }

        /// <summary>
        /// 遞迴列出 img 底下所有音檔。
        /// PL_MONAD.img 這類 img 會把音檔再包一層資料夾（例如 effectSound/），
        /// 只取第一層的話會把資料夾當成音檔，點下去自然播不出來。
        /// </summary>
        void AddMusicRows(Wz_Node node)
        {
            foreach (var child in node.Nodes)
            {
                if (child.Value is Wz_Sound)
                {
                    Grid.Rows.Add(child.GetPath());
                }
                else if (child.Nodes.Count > 0)
                {
                    AddMusicRows(child);
                }
            }
        }
        DataViewer[] DataGrid = new DataViewer[39];
        DataViewer[] TempGrid = new DataViewer[39];

        void LoadBIN()
        {
            var BinFile = System.Environment.CurrentDirectory + "\\" + Grid.Parent.Name + ".BIN";
            if (System.IO.File.Exists(BinFile))
            {
                for (int i = 0; i <= 38; i++)
                {
                    DataGrid[i].Rows.Clear();
                    DataGrid[i].Refresh();
                    var Graphic = DataGrid[i].CreateGraphics();
                    var Font = new System.Drawing.Font(FontFamily.GenericSansSerif, 20, FontStyle.Bold);
                    Graphic.DrawString("불러오는 중...", Font, Brushes.Black, 300, 200);
                }
                Grid.LoadBin(BinFile);

            }
            else
                MessageBoxEx.Show(this, Grid.Parent.Name + ".BIN" + " 파일을 찾을 수 없습니다");
        }




        Wz_Node find(string c)
        {
            return null;
        }


        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!gridsReady)
            {
                return;
            }
            if (comboBox1.Text == "Load From BIN")
            {


                LoadButton.Visible = false;
                LoadBIN();
            }
            else
            {


                LoadButton.Visible = true;
                Grid.Rows.Clear();
                Grid.Refresh();
            }

        }

        private void tabControl1_SelectedTabChanged(object sender, DevComponents.DotNetBar.SuperTabStripSelectedTabChangedEventArgs e)
        {
            // NTMS 版的 FrmMapRender2 沒有公開的 form 欄位，切換分頁時不再隱藏地圖視窗。

            // SuperTabControl 在建立分頁的過程中就會觸發本事件，
            // 此時 Form1_Load 尚未建立 DataViewer，必須先擋掉。
            if (!gridsReady)
            {
                return;
            }

            Row1 = -1;
            tabIndex = tabControl1.SelectedTabIndex;
            Grid = DataGrid[tabIndex];
            SearchGrid = TempGrid[tabIndex];
            Grid.Visible = true;
            SearchGrid.Visible = false;
            SearchBox.Clear();
            comboBox4.SelectedIndex = tabControl1.SelectedTabIndex;

            switch (Grid.DefaultGridType)
            {
                case GridType.Normal:
                case GridType.Item:
                    Grid.RowTemplate.Height = 40;
                    break;
                case GridType.Map:
                    Grid.RowTemplate.Height = 60;
                    break;

                case GridType.Mob:
                case GridType.Reactor:
                    Grid.RowTemplate.Height = 80;
                    break;

                case GridType.Skill:
                    Grid.RowTemplate.Height = 60;
                    break;

                case GridType.Npc:
                case GridType.Morph:
                case GridType.Familiar:
                    Grid.RowTemplate.Height = 70;
                    break;

                case GridType.DamageSkin:
                    Grid.RowTemplate.Height = 50;
                    break;
            }

            if (comboBox1.Text == "Load From BIN")
                LoadBIN();

            SetGrid();


        }
        string GetWzFileName()
        {

            switch (tabIndex)
            {
                case 0:
                case 1:
                case 2:
                case 25:
                case 26:
                case 35:
                case 36:
                    return "Item.wz";

                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                case 12:
                case 13:
                case 14:
                case 15:
                case 16:
                case 27:
                case 28:
                case 29:
                case 30:
                case 31:
                case 32:
                case 34:
                    return "Character.wz";
                case 17:
                case 18:
                case 19:
                    return "Map.wz";
                case 20:
                    return "Mob.wz";
                case 21:
                    return "Mob001.wz";
                case 22:
                    return "Mob2.wz";
                case 23:
                    return "Skill.wz";

                case 24:
                    return "Npc.wz";
                case 33:
                    return "Morph.wz";
                case 37:
                    return "Reactor.wz";
            }
            return "";
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            if (PluginManager.FindWz(Wz_Type.Base) == null)
            {
                MessageBoxEx.Show(this, "Base.wz가 열려 있지 않습니다");
                return;
            }

            Row1 = -1;
            Grid.Rows.Clear();
            Grid.Refresh();
            RowList.Clear();
            ColList.Clear();
            var Graphic = Grid.CreateGraphics();
            var Font = new System.Drawing.Font(FontFamily.GenericSansSerif, 20, FontStyle.Bold);
            Graphic.DrawString("불러오는 중...", Font, Brushes.Black, 300, 200);
            switch (tabIndex)
            {
                case 0:
                case 1:
                case 2:
                case 25:
                case 26:
                case 36:
                    LoadItem();
                    break;

                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                case 12:
                case 13:
                case 14:
                case 15:
                case 16:
                case 27:
                case 28:
                case 29:
                case 30:
                case 31:
                case 32:
                    LoadCharacter();
                    break;
                case 17:
                    LoadMap(1);
                    break;

                case 18:
                    LoadMap(2);
                    break;
                case 19:
                    LoadMap(3);
                    break;
                case 20:
                    LoadMob(1);
                    break;
                case 21:
                    LoadMob(2);
                    break;
                case 22:
                    LoadMob(3);
                    break;
                case 23:
                    LoadSkill();
                    break;

                case 24:
                    LoadNpc();
                    break;
                case 33:
                    LoadMorph();
                    break;
                case 34:
                    LoadFamiliar();
                    break;
                case 35:
                    LoadDamageSkin();
                    break;
                case 37:
                    LoadReactor();
                    break;

                case 38:
                    LoadMusic();
                    break;


            }

        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            Grid.SaveBin(System.Environment.CurrentDirectory + "\\" + Grid.Parent.Name + ".BIN");
            MessageBoxEx.Show(this, Grid.Parent.Name + ".BIN 저장 완료");
        }
        string Trim(string s)
        {

            return s.Trim(' ');
        }
        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            var SearchStr = Trim(SearchBox.Text);
            if (SearchStr == "")
            {
                Grid.Visible = true;
                SearchGrid.Visible = false;
            }
            else
            {
                SearchGrid.Rows.Clear();
                var Row = new DataGridViewRow();
                for (int i = 0; i < Grid.RowCount; i++)
                {
                    for (int j = 0; j < Grid.Columns.Count; j++)
                    {
                        if (Grid.Rows[i].Cells[j].Value is string)
                        {
                            if (Grid.Rows[i].Cells[j].Value.ToString().IndexOf(SearchStr, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                Row = (DataGridViewRow)Grid.Rows[i].Clone();
                                for (int j2 = 0; j2 < Grid.Columns.Count; j2++)
                                    Row.Cells[j2].Value = Grid.Rows[i].Cells[j2].Value;
                                SearchGrid.Rows.Add(Row);
                                break;
                            }
                        }
                    }
                }
                Grid.Visible = false;
                SearchGrid.Visible = true;
                SearchGrid.Refresh();
            }

        }
        bool FileExists(string Name)
        {
            return System.IO.File.Exists(Name);

        }


        BassSoundPlayer soundPlayer;



        // Wz_Structure s;
        private void button1_Click(object sender, EventArgs e)
        {




        }
        string GetIDPath(string ID)
        {


            switch (tabIndex)
            {
                case 0:
                    return "Item/Cash/" + LeftStr(ID, 4) + ".img/" + ID;
                case 1:
                    return "Item/Consume/" + LeftStr(ID, 4) + ".img/" + ID;

                case 2:
                    return "Item/Special/0" + LeftStr(ID, 3) + ".img/" + ID;
                case 33:
                case 35:
                    return "Item/Consume/" + LeftStr(ID, 4) + ".img/" + ID;

                case 3:
                    return "Character/Weapon/" + ID + ".img";
                case 4:
                    return "Character/Cap/" + ID + ".img";
                case 5:
                    return "Character/Coat/" + ID + ".img";
                case 6:
                    return "Character/Longcoat/" + ID + ".img";

                case 7:
                    return "Character/Pants/" + ID + ".img";
                case 8:
                    return "Character/Shoes/" + ID + ".img";
                case 9:
                    return "Character/Glove/" + ID + ".img";
                case 10:
                    return "Character/Ring/" + ID + ".img";

                case 11:
                    return "Character/Cape/" + ID + ".img";

                case 12:
                    return "Character/Accessory/" + ID + ".img";
                case 13:
                    return "Character/Shield/" + ID + ".img";
                case 14:
                    return "Character/TamingMob/" + ID + ".img";
                case 15:
                    return "Character/Hair/" + ID + ".img";

                case 16:
                    return "Character/Face/" + ID + ".img";
                case 20:
                case 21:
                case 22:
                    return "Mob/" + ID + ".img";

                case 23:

                    var Left1 = LeftStr(ID, 1);
                    switch (Left1)
                    {
                        case "0":
                            return "Skill/000.img/skill/" + ID;
                        case "8":
                            return "Skill/" + (int.Parse(ID) / 100).ToString() + ".img/skill/" + ID;
                        default:
                            return "Skill/" + (int.Parse(ID) / 10000).ToString() + ".img/skill/" + ID;
                    }
                case 24:
                    return "Npc/" + ID + ".img";
                case 25:
                    return "Item/Pet/" + ID + ".img";

                case 26:
                    //   if(Arc.ItemWz == null)
                    //     return null;
                    if (GetNode("Item/Install/03010.img") != null)
                    {
                        switch (LeftStr(ID, 5))
                        {
                            case "03015":
                                return "Item/Install/" + LeftStr(ID, 6) + ".img/" + ID;
                            case "03010":
                            case "03011":
                            case "03012":
                            case "03013":
                            case "03014":
                            case "03016":
                            case "03017":
                            case "03018":
                                return "Item/Install/" + LeftStr(ID, 5) + ".img/" + ID;
                            default:
                                return "Item/Install/" + LeftStr(ID, 4) + ".img/" + ID;
                        }
                    }
                    else
                    {
                        return "Item/Install/" + LeftStr(ID, 4) + ".img/" + ID;
                    }

                case 27:
                    return "Character/Android/" + ID + ".img";
                case 28:
                    return "Character/Mechanic/" + ID + ".img";

                case 29:
                    return "Character/PetEquip/" + ID + ".img";

                case 30:
                    return "Character/Bits/" + ID + ".img";

                case 31:
                    return "Character/MonsterBattle/" + ID + ".img";

                case 32:
                    return "Character/Totem/" + ID + ".img";

                case 36:
                    return "Item/Etc/" + LeftStr(ID, 4) + ".img/" + ID;
            }
            /*
            switch (LeftStr(ID, 2))
            {
                case "05":
                    return "Item/Cash/" + LeftStr(ID, 4) + ".img/" + ID;
                    //return "Item/Cash/0501.img/05010000";
                    break;
                case "03":
                    if (Arc.ItemWz.GetNode("Install/03010.img") != null)
                    {
                        switch (LeftStr(ID, 5))
                        {
                            case "03015":
                                return "Item/Install/" + LeftStr(ID, 6) + ".img/" + ID;
                                break;
                            case "03010":
                            case "03011":
                            case "03012":
                            case "03013":
                            case "03014":
                            case "03016":
                            case "03017":
                            case "03018":
                                return "Item/Install/" + LeftStr(ID, 5) + ".img/" + ID;
                                break;
                            default:
                                return "Item/Install/" + LeftStr(ID, 4) + ".img/" + ID;
                                break;
                        }
                    }
                    else
                    {
                        return "Item/Install/" + LeftStr(ID, 4) + ".img/" + ID;
                    }
                    break;



            }
            switch (int.Parse(ID) / 10000)
            {
                case 2:
                    return "Character/Face/" + ID + ".img";
                case 3:
                case 4:
                case 6:
                    return "Character/Hair/" + ID + ".img";
                case 101:
                case 102:
                case 103:
                case 112:
                case 113:
                case 114:
                case 115:
                case 116:
                case 118:
                case 119:
                    return "Character/Accessory/" + ID + ".img";
                case 120:
                    return "Character/Totem/" + ID + ".img";
                case 100:
                    return "Character/Cap/" + ID + ".img";
                case 110:
                    return "Character/Cape/" + ID + ".img";
                case 104:
                    return "Character/Coat/" + ID + ".img";
                case 105:
                    return "Character/Longcoat/" + ID + ".img";
                case 106:
                    return "Character/Pants/" + ID + ".img";
                case 107:
                    return "Character/Shoes/" + ID + ".img";
                case 108:
                    return "Character/Glove/" + ID + ".img";
                case 109:
                    return "Character/Shield/" + ID + ".img";

                case 111:
                    return "Character/Ring/" + ID + ".img";

                case 161:
                    return "Character/Mechanic/" + ID + ".img";

                case 166:
                case 167:
                    return "Character/Android/" + ID + ".img";
                case 168:
                    return "Character/Bits/" + ID + ".img";
                case int n when (n >=121 && n <=170):
                    return "Character/Weapon/" + ID + ".img";
                case int n when (n >= 190 && n <= 199):
                    return "Character/TamingMob/" + ID + ".img";
                case 180:
                    return "Character/PetEquip/" + ID + ".img";
                case 996:
                case 997:
                    return "Character/Familiar/" + ID + ".img";
                case int n when (n >= 200 && n <= 294):
                    return  "Item/Consume/" + LeftStr(ID, 4) + ".img/" + ID;
                case int n when (n >= 400 && n <= 446):
                    return "Item/Etc/" + LeftStr(ID, 4) + ".img/" + ID;

                case 500:
                    return "Item/Pet/" + ID + ".img";


            }
            */
            return null;
        }

        /// <summary>
        /// 下拉選單改成 DropDownList 之後，未選取時 Text 會是空字串，
        /// 因此一律走安全解析，避免 int.Parse 直接丟例外。
        /// </summary>
        private static int ParseOr(string text, int fallback)
        {
            return int.TryParse(text, out int value) && value > 0 ? value : fallback;
        }

        private const int DefaultCellFontSize = 9;

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!gridsReady)
            {
                return;
            }
            var font = new System.Drawing.Font("굴림", ParseOr(comboBox2.Text, DefaultCellFontSize));
            Grid.DefaultCellStyle.Font = font;
            SearchGrid.DefaultCellStyle.Font = font;
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!gridsReady)
            {
                return;
            }
            Grid.RowTemplate.Height = ParseOr(comboBox3.Text, Grid.RowTemplate.Height);
            LoadButton_Click(sender, e);
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (comboBox4.SelectedIndex >= 0)
            {
                tabControl1.SelectedTabIndex = comboBox4.SelectedIndex;
            }

        }



        private void Form1_Resize(object sender, EventArgs e)
        {
            // if(WindowState == FormWindowState.Minimized)
            //  ToolTip2.tooltipQuickView.Visible = false;

        }
        void ShowMap(Wz_Node MapImg)
        {
            MapRenderLauncher.ShowMap(MapImg);
        }

        void CellClick(DataViewer DataGrid, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1)
                return;
            if (e.RowIndex >= Grid.RowCount)
                return;
            if (tabIndex == 37 || tabIndex == 34)
                return;

            if (e.RowIndex >= Grid.RowCount)
                return;
            string SelectID = "";
            if (DataGrid.Rows[e.RowIndex].Cells[0].Value is string)
            {
                SelectID = DataGrid.Rows[e.RowIndex].Cells[0].Value.ToString();

                if (tabIndex == 17 || tabIndex == 18 || tabIndex == 19)
                {
                    var imgNode = GetNode("Map/Map/Map" + LeftStr(SelectID, 1)).FindNodeByPath(SelectID + ".img");
                    ShowMap(imgNode);
                    if (imgNode != null)
                        Db2Host.SelectNode(imgNode);
                }
                else if (tabIndex == 38)
                {
                    // SelectID 是 img 內的相對路徑，例如 PL_MONAD.img/effectSound/Caravan_Bad。
                    var soundNode = GetNode("Sound/" + SelectID);
                    if (!(soundNode?.Value is Wz_Sound sound))
                    {
                        // 不是音檔（例如仍是資料夾節點）就不播放。
                        return;
                    }
                    soundPlayer.UnLoad();
                    byte[] data = sound.ExtractSound();
                    if (data == null || data.Length <= 0)
                    {
                        // Wz_SoundType.Binary 之類無法解析的資料，ExtractSound 會傳回 null。
                        return;
                    }
                    soundPlayer.PreLoad(data);
                    soundPlayer.Play();
                }
                else
                {
                    if (tabIndex == 38)
                        return;
                    Db2Host.Tooltip.Visible = true;
                    Db2Host.Tooltip.BringToFront();
                    var Node = PluginManager.FindWz(GetIDPath(SelectID));
                    if (Node != null)
                        Db2Host.SelectNode(Node);
                }
            }
        }

        void GridScroll()
        {
            var tooltip = Db2Host.Tooltip;
            if (tooltip != null)
                tooltip.Visible = false;

            MapRenderLauncher.Close();
        }
        /// <summary>
        /// 每個 DataViewer 只需執行一次的初始化：事件註冊與不隨分頁變動的外觀。
        /// 事件必須在此註冊，不能放在 SetGrid()，
        /// 否則每次切換分頁都會重複掛上一組處理器。
        /// </summary>
        void InitGrid(DataViewer grid)
        {
            grid.CellClick += (s, e) => CellClick((DataViewer)s, e);
            grid.Scroll += (s, e) => GridScroll();
            grid.MouseClick += (s, e) => GridMouseClick((DataViewer)s, e);

            grid.DefaultCellStyle.SelectionBackColor = Color.LightCyan;
            grid.DefaultCellStyle.SelectionForeColor = Color.Black;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = grid.ColumnHeadersDefaultCellStyle.BackColor;
            grid.RowHeadersVisible = false;
            grid.Dock = DockStyle.Fill;
            grid.ShowCellToolTips = false;
        }

        /// <summary>右鍵複製選取的儲存格。</summary>
        void GridMouseClick(DataViewer grid, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            ContextMenuStrip m = new ContextMenuStrip();
            m.Items.Add("복사");
            Db2Theme.Apply(m);
            m.Show(grid, new Point(e.X, e.Y));

            if (grid.GetCellCount(DataGridViewElementStates.Selected) > 0)
            {
                try
                {
                    Clipboard.SetDataObject(grid.GetClipboardContent());
                }
                catch (System.Runtime.InteropServices.ExternalException)
                {
                    MessageBoxEx.Show(this, "클립보드에 접근할 수 없습니다. 다시 시도해 주세요.");
                }
            }
        }

        /// <summary>切換分頁時套用的設定（可重複執行）。</summary>
        void SetGrid()
        {
            ApplyGridStyle(Grid);
            ApplyGridStyle(SearchGrid);
        }

        void ApplyGridStyle(DataViewer grid)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            grid.DefaultCellStyle.Font = new System.Drawing.Font("굴림", ParseOr(comboBox2.Text, DefaultCellFontSize));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormClosing += (s, e1) =>
           {
               this.Hide();
               e1.Cancel = true;
           };

            ColList = new List<string>();
            RowList = new List<string>();
            RowList1 = new Dictionary<int, List<string>>();
            soundPlayer = new BassSoundPlayer();
            if (!soundPlayer.Init())
            {
                //  Un4seen.Bass.BASSError error = soundPlayer.GetLastError();
                //  MessageBoxEx.Show(this, "Bass初始化失败！\r\n\r\nerrorCode : " + (int)error + "(" + error + ")","虫子");
            }
            for (int i = 0; i <= 38; i++)
            {
                switch (i)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 25:
                    case 26:
                    case 36:
                        DataGrid[i] = new DataViewer(GridType.Item);
                        DataGrid[i].Parent = TabPage(i);
                        TempGrid[i] = new DataViewer(GridType.Item);
                        TempGrid[i].Parent = TabPage(i);
                        break;

                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                    case 14:
                    case 15:
                    case 16:
                    case 27:
                    case 28:
                    case 29:
                    case 30:
                    case 31:
                    case 32:
                        DataGrid[i] = new DataViewer(GridType.Normal);
                        DataGrid[i].Parent = TabPage(i);
                        TempGrid[i] = new DataViewer(GridType.Normal);
                        TempGrid[i].Parent = TabPage(i);
                        break;

                    case 17:
                    case 18:
                    case 19:
                        DataGrid[i] = new DataViewer(GridType.Map);
                        DataGrid[i].Parent = TabPage(i);
                        TempGrid[i] = new DataViewer(GridType.Map);
                        TempGrid[i].Parent = TabPage(i);
                        break;
                    case 20:
                    case 21:
                    case 22:
                        DataGrid[i] = new DataViewer(GridType.Mob);
                        DataGrid[i].Parent = TabPage(i);
                        TempGrid[i] = new DataViewer(GridType.Mob);
                        TempGrid[i].Parent = TabPage(i);
                        break;
                    case 23:

                        DataGrid[i] = new DataViewer(GridType.Skill);
                        DataGrid[i].Parent = TabPage(i);
                        TempGrid[i] = new DataViewer(GridType.Skill);
                        TempGrid[i].Parent = TabPage(i);
                        break;
                    case 24:
                        DataGrid[i] = new DataViewer(GridType.Npc);
                        DataGrid[i].Parent = TabPage(i);
                        TempGrid[i] = new DataViewer(GridType.Npc);
                        TempGrid[i].Parent = TabPage(i);
                        break;
                    case 33:
                        DataGrid[i] = new DataViewer(GridType.Morph);
                        DataGrid[i].Parent = TabPage(i);
                        TempGrid[i] = new DataViewer(GridType.Morph);
                        TempGrid[i].Parent = TabPage(i);
                        break;
                    case 34:
                        DataGrid[i] = new DataViewer(GridType.Familiar);
                        DataGrid[i].Parent = TabPage(i);
                        TempGrid[i] = new DataViewer(GridType.Familiar);
                        TempGrid[i].Parent = TabPage(i);
                        break;
                    case 35:
                        DataGrid[i] = new DataViewer(GridType.DamageSkin);
                        DataGrid[i].Parent = TabPage(i);
                        TempGrid[i] = new DataViewer(GridType.DamageSkin);
                        TempGrid[i].Parent = TabPage(i);
                        break;
                    case 37:
                        DataGrid[i] = new DataViewer(GridType.Reactor);
                        DataGrid[i].Parent = TabPage(i);
                        TempGrid[i] = new DataViewer(GridType.Reactor);
                        TempGrid[i].Parent = TabPage(i);
                        break;

                    case 38:
                        DataGrid[i] = new DataViewer(GridType.Music);
                        DataGrid[i].Parent = TabPage(i);
                        TempGrid[i] = new DataViewer(GridType.Music);
                        TempGrid[i].Parent = TabPage(i);
                        break;


                }

            }

            // 事件只在此處註冊一次；SetGrid() 之後每次切換分頁都會呼叫，
            // 若把註冊寫在裡面，處理器會隨切換次數不斷累加。
            for (int i = 0; i <= 38; i++)
            {
                InitGrid(DataGrid[i]);
                InitGrid(TempGrid[i]);
            }

            Grid = DataGrid[0];

            SearchGrid = TempGrid[0];

            SetGrid();
            Grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;

            Graphics graphics = this.CreateGraphics();
            float dpiX = graphics.DpiX;
            double Size10 = ((double)96 / (double)dpiX) * 9;
            double Size11 = ((double)96 / (double)dpiX) * 9;
            double Size12 = ((double)96 / (double)dpiX) * 10;
            double Size13 = ((double)96 / (double)dpiX) * 11;

            comboBox1.Font = new Font("굴림", (float)Size10);
            comboBox2.Font = new Font("굴림", (float)Size10);
            comboBox3.Font = new Font("굴림", (float)Size10);
            comboBox4.Font = new Font("굴림", (float)Size10);

            label2.Font = new Font("굴림", (float)Size12);
            label3.Font = new Font("굴림", (float)Size12);
            label4.Font = new Font("굴림", (float)Size12);
            label6.Font = new Font("굴림", (float)Size12);

            gridsReady = true;
            ApplyTheme();

            SearchBox.Font = new Font("굴림", (float)Size11);
            LoadButton.Font = new Font("굴림", (float)Size11);
            SaveButton.Font = new Font("굴림", (float)Size11);
            tabControl1.TabFont = new Font("굴림", (float)Size13);
            tabControl1.SelectedTabFont = new Font("굴림", (float)Size13, FontStyle.Bold);

        }
    }
}
