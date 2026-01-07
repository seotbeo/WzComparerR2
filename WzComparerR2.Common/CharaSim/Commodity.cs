using System;
using System.Collections.Generic;
using System.Text;

using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Commodity
    {
        public Commodity()
        {
            gameWorlds = new List<int>();
        }
        public int SN;
        public int ItemId;
        public int Count;
        public int Price;
        public int Bonus;
        public int Period;
        public int Priority;
        public int ReqPOP;
        public int ReqLEV;
        public int Gender;
        public int OnSale;
        public int Class;
        public int Limit;
        public string gameWorld;
        public List<int> gameWorlds;
        public int LimitMax;
        public int LimitQuestID;
        public int originalPrice;
        public int discount;
        public int PbCash;
        public int PbPoint;
        public int PbGift;
        public int Refundable;
        public int WebShop;
        public int termStart;
        public string termEnd;
        public CommodityPriceInfo PriceInfo;

        public static Commodity CreateFromNode(Wz_Node commodityNode)
        {
            if (commodityNode == null)
                return null;

            Commodity commodity = new Commodity();

            foreach (Wz_Node subNode in commodityNode.Nodes)
            {
                int value;
                Int32.TryParse(Convert.ToString(subNode.Value), out value);
                switch (subNode.Text)
                {
                    case "SN":
                        commodity.SN = value;
                        break;
                    case "ItemId":
                        commodity.ItemId = value;
                        break;
                    case "Count":
                        commodity.Count = value;
                        break;
                    case "Price":
                        commodity.Price = value;
                        break;
                    case "Bonus":
                        commodity.Bonus = value;
                        break;
                    case "Period":
                        commodity.Period = value;
                        break;
                    case "Priority":
                        commodity.Priority = value;
                        break;
                    case "ReqPOP":
                        commodity.ReqPOP = value;
                        break;
                    case "ReqLEV":
                        commodity.ReqLEV = value;
                        break;
                    case "Gender":
                        commodity.Gender = value;
                        break;
                    case "OnSale":
                        commodity.OnSale = value;
                        break;
                    case "Class":
                        commodity.Class = value;
                        break;
                    case "Limit":
                        commodity.Limit = value;
                        break;
                    case "LimitMax":
                        commodity.LimitMax = value;
                        break;
                    case "gameWorld":
                        commodity.gameWorld = Convert.ToString(subNode.Value);
                        string[] worlds = Convert.ToString(subNode.Value).Split('/');
                        foreach (var i in worlds)
                        {
                            if (int.TryParse(i, out int tmp))
                            {
                                commodity.gameWorlds.Add(tmp);
                            }
                        }
                        break;
                    case "originalPrice":
                        commodity.originalPrice = value;
                        break;
                    case "discount":
                        commodity.discount = value;
                        break;
                    case "PbCash":
                        commodity.PbCash = value;
                        break;
                    case "PbPoint":
                        commodity.PbPoint = value;
                        break;
                    case "PbGift":
                        commodity.PbGift = value;
                        break;
                    case "Refundable":
                        commodity.Refundable = value;
                        break;
                    case "WebShop":
                        commodity.WebShop = value;
                        break;
                    case "termStart":
                        commodity.termStart = value;
                        break;
                    case "termEnd":
                        if (value != 0)
                            commodity.termEnd = string.Format("{0:D8}/{1:D2}0000", value / 100, value % 100);
                        else
                            commodity.termEnd = Convert.ToString(subNode.Value);
                        break;
                }
            }

            commodity.PriceInfo = new CommodityPriceInfo(
                commodity.Count,
                commodity.Price,
                commodity.SN / 10000000 == 8,
                commodity.gameWorlds.Contains(45) && (!commodity.gameWorlds.Contains(1) || !commodity.gameWorlds.Contains(0))
                );

            return commodity;
        }
    }

    public readonly struct CommodityPriceInfo : IEquatable<CommodityPriceInfo>, IComparable<CommodityPriceInfo>
    {
        public readonly int Count;
        public readonly int Price;
        public readonly bool Meso;
        public readonly bool Reboot;

        public CommodityPriceInfo(int count, int price, bool meso, bool reboot)
        {
            Count = count;
            Price = price;
            Meso = meso;
            Reboot = reboot;
        }

        public int CompareTo(CommodityPriceInfo other)
        {
            int c = Reboot.CompareTo(other.Reboot);
            if (c != 0) return c;

            c = Count.CompareTo(other.Count);
            if (c != 0) return c;

            c = Meso.CompareTo(other.Meso);
            if (c != 0) return c;

            return Price.CompareTo(other.Price);
        }

        public bool Equals(CommodityPriceInfo other)
        {
            return Count == other.Count && Price == other.Price && Meso == other.Meso && Reboot == other.Reboot;
        }

        public override bool Equals(object obj)
        {
            return obj is CommodityPriceInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Count.GetHashCode();
                hash = hash * 31 + Price.GetHashCode();
                hash = hash * 31 + Meso.GetHashCode();
                hash = hash * 31 + Reboot.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(CommodityPriceInfo left, CommodityPriceInfo right) => left.Equals(right);

        public static bool operator !=(CommodityPriceInfo left, CommodityPriceInfo right) => !left.Equals(right);
    }
}
