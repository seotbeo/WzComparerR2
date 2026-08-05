using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WzComparerR2.Common;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class NpcQuote
    {
        public NpcQuote()
        {
            this.NQuote = new Dictionary<int, string>();
            this.FQuote = new Dictionary<int, string>();
            this.WQuote = new Dictionary<int, string>();
            this.DQuote = new Dictionary<int, string>();
            this.SpecialQuote = new Dictionary<int, string>();
        }

        public Dictionary<int, string> NQuote { get; set; }
        public Dictionary<int, string> FQuote { get; set; }
        public Dictionary<int, string> WQuote { get; set; }
        public Dictionary<int, string> DQuote { get; set; }
        public Dictionary<int, string> SpecialQuote { get; set; }

        public static NpcQuote CreateFromNode(int npcID, StringLinker stringLinker)
        {
            if (stringLinker == null || !stringLinker.StringNpc.TryGetValue(npcID, out StringResult sr))
            {
                return null;
            }
            NpcQuote npcQuote = new NpcQuote();

            int nQuoteIndex = -1;
            foreach (string type in new[] { "n", "f", "w", "d" })
            {
                for (int i = 0; i < 100; i++)
                {
                    string quoteValue = sr[$"{type}{i}"];
                    if (quoteValue != null)
                    {
                        string quoteText = stringParse(quoteValue, stringLinker);
                        switch (type)
                        {
                            case "n":
                                npcQuote.NQuote[i] = quoteText;
                                nQuoteIndex = i;
                                break;
                            case "f":
                                npcQuote.FQuote[i] = quoteText;
                                break;
                            case "w":
                                npcQuote.WQuote[i] = quoteText;
                                break;
                            case "d":
                                npcQuote.DQuote[i] = quoteText;
                                break;
                        }
                    }
                }
            }

            Wz_Node dialogueNode = stringLinker.FindNodeFromSource($"Npc.img\\{npcID}\\dialogue", Wz_Type.String) ??
                stringLinker.FindNodeFromSource($"Npc.img\\{npcID}\\dialog", Wz_Type.String);
            if (dialogueNode != null)
            {
                foreach (Wz_Node innerNode in dialogueNode.Nodes)
                {
                    if (Int32.TryParse(innerNode.Text, out int dialogueId))
                        npcQuote.SpecialQuote[dialogueId] = stringParse(Convert.ToString(innerNode.Value), stringLinker);
                }
            }

            Wz_Node bubbleNode = stringLinker.FindNodeFromSource($"Npc.img\\{npcID}\\bubble", Wz_Type.String);
            if (bubbleNode != null)
            {
                foreach (Wz_Node questID in bubbleNode.Nodes)
                {
                    foreach (Wz_Node questState in questID.Nodes)
                    {
                        foreach (Wz_Node innerNode in questState.Nodes)
                        {
                            npcQuote.NQuote[++nQuoteIndex] = stringParse(Convert.ToString(innerNode.Value), stringLinker);
                        }
                    }
                }
            }

            return npcQuote;
        }

        private static string stringParse(string text, StringLinker stringLinker)
        {
            if (stringLinker == null)
            {
                return text;
            }
            text = Regex.Replace(text, @$"#(p|o|m|t|q|i|v|y|illu)\s*(\d{{1,9}}).*?#", match => // id should be less than 1,000,000,000
            {
                string tag = match.Groups[1].Value;
                if (!int.TryParse(match.Groups[2].Value, out int id)) id = -1;
                StringResult sr;
                switch (tag)
                {
                    case "p":
                        stringLinker.StringNpc.TryGetValue(id, out sr);
                        return $"{sr?.Name ?? id.ToString()}";

                    case "o":
                        if (id >= 100000000)
                        {
                            stringLinker.StringMap.TryGetValue(id, out sr);
                            return $"{sr?.MapName ?? id.ToString()}";
                        }
                        else
                        {
                            stringLinker.StringMob.TryGetValue(id, out sr);
                            return $"{sr?.Name ?? id.ToString()}";
                        }

                    case "m":
                        stringLinker.StringMap.TryGetValue(id, out sr);
                        return $"{sr?.MapName ?? id.ToString()}";

                    case "t":
                        stringLinker.StringItem.TryGetValue(id, out sr);
                        if (sr == null)
                        {
                            stringLinker.StringEqp.TryGetValue(id, out sr);
                        }
                        return $"{sr?.Name ?? id.ToString()}";

                    case "q":
                        stringLinker.StringSkill.TryGetValue(id, out sr);
                        return $"{sr?.Name ?? id.ToString()}";

                    case "i":
                    case "v":
                        stringLinker.StringItem.TryGetValue(id, out sr);
                        if (sr == null)
                        {
                            stringLinker.StringEqp.TryGetValue(id, out sr);
                        }
                        return $"{sr?.Name ?? id.ToString()}";

                    case "y":
                        stringLinker.StringQuest.TryGetValue(id, out sr);
                        return $"{sr?.Name ?? id.ToString()}";

                    default:
                        return id.ToString();
                }
            });

            // 미사용 태그
            text = text.Replace("#b", ""); // 파란색
            text = text.Replace("#k", ""); // 기본색
            text = text.Replace("#kk", "");
            text = text.Replace("#K", "");
            text = text.Replace("#r", ""); // 빨간색
            text = text.Replace("#g", "");
            text = text.Replace("#l", "");
            text = text.Replace("#eqp#", "");
            text = text.Replace("#es", "#ＥＳ"); // plural suffix for English region
            text = text.Replace("#e", "");
            text = text.Replace("ＥＳ", "es");
            text = text.Replace("#E", "");
            text = text.Replace("#n", " ");

            return text;
        }
    }
}
