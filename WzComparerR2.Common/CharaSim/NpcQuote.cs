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
            this.NpcID = -1;
            this.NQuote = new Dictionary<int, string>();
            this.FQuote = new Dictionary<int, string>();
            this.WQuote = new Dictionary<int, string>();
            this.DQuote = new Dictionary<int, string>();
            this.SpecialQuote = new Dictionary<int, string>();
        }

        public int NpcID { get; set; }
        public Dictionary<int, string> NQuote { get; set; }
        public Dictionary<int, string> FQuote { get; set; }
        public Dictionary<int, string> WQuote { get; set; }
        public Dictionary<int, string> DQuote { get; set; }
        public Dictionary<int, string> SpecialQuote { get; set; }

        public static NpcQuote CreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode, StringLinker stringLinker)
        {
            int npcID;
            if (!(Int32.TryParse(node.Text, out npcID)))
            {
                return null;
            }

            NpcQuote npcQuote = new NpcQuote();
            npcQuote.NpcID = npcID;

            int nQuoteIndex = 0;
            int fQuoteIndex = 0;
            int wQuoteIndex = 0;
            int dQuoteIndex = 0;

            if (node != null)
            {
                foreach (var quoteNode in node.Nodes)
                {
                    if (Regex.IsMatch(quoteNode.Text, @"^n\d+$"))
                    {
                        npcQuote.NQuote[nQuoteIndex] = stringParse(quoteNode.Value.ToString(), stringLinker);
                        nQuoteIndex++;
                    }
                    else if (Regex.IsMatch(quoteNode.Text, @"^f\d+$"))
                    {
                        npcQuote.FQuote[fQuoteIndex] = stringParse(quoteNode.Value.ToString(), stringLinker);
                        fQuoteIndex++;
                    }
                    else if (Regex.IsMatch(quoteNode.Text, @"^w\d+$"))
                    {
                        npcQuote.WQuote[wQuoteIndex] = stringParse(quoteNode.Value.ToString(), stringLinker);
                        wQuoteIndex++;
                    }
                    else if (Regex.IsMatch(quoteNode.Text, @"^d\d+$"))
                    {
                        npcQuote.DQuote[dQuoteIndex] = stringParse(quoteNode.Value.ToString(), stringLinker);
                        dQuoteIndex++;
                    }
                    else if (quoteNode.Text == "dialogue" || quoteNode.Text == "dialog")
                    {
                        foreach (var dialogueNode in quoteNode.Nodes)
                        {
                            if (Int32.TryParse(dialogueNode.Text, out int dialogueId))
                                npcQuote.SpecialQuote[dialogueId] = stringParse(dialogueNode.Value.ToString(), stringLinker);
                        }
                    }
                    else if (quoteNode.Text == "bubble")
                    {
                        foreach (var bubbleNode in quoteNode.Nodes)
                        {
                            foreach (var subNode in bubbleNode.Nodes)
                            {
                                foreach (var subNode2 in subNode.Nodes)
                                {
                                    npcQuote.NQuote[nQuoteIndex] = stringParse(subNode2.Value.ToString(), stringLinker);
                                    nQuoteIndex++;
                                }
                            }
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
