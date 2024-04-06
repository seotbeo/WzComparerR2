using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.Common;
using WzComparerR2.Rendering;

namespace WzComparerR2.MapRender.UI
{

    public static class TooltipHelper
    {
        private static string ToCJKNumberExpr(long value)
        {
            var sb = new StringBuilder(16);
            bool firstPart = true;
            if (value >= 1_0000_0000_0000)
            {
                long part = value / 1_0000_0000;
                sb.AppendFormat("{0}兆", part); // Korean: 조, Chinese+Japanese: 兆
                value -= part * 1_0000_0000;
                firstPart = false;
            }
            if (value >= 1_0000_0000)
            {
                long part = value / 1_0000_0000;
                sb.AppendFormat("{0}億", part); // Korean: 억, TradChinese+Japanese: 億, SimpChinese: 亿
                value -= part * 1_0000_0000;
                firstPart = false;
            }
            if (value >= 1_0000)
            {
                long part = value / 1_0000;
                sb.Append(firstPart ? null : " ");
                sb.AppendFormat("{0}万", part); // Korean: 만, TradChinese: 萬, SimpChinese+Japanese: 万
                value -= part * 1_0000;
                firstPart = false;
            }
            if (value > 0)
            {
                sb.Append(firstPart ? null : " ");
                sb.AppendFormat("{0}", value);
            }

            return sb.Length > 0 ? sb.ToString() : "0";
        }
        public static TextBlock PrepareTextBlock(IWcR2Font font, string text, ref Vector2 pos, Color color)
        {
            Vector2 size = font.MeasureString(text);

            TextBlock block = new TextBlock();
            block.Font = font;
            block.Text = text;
            block.Position = pos;
            block.ForeColor = color;

            pos.X += size.X;
            return block;
        }

        public static TextBlock PrepareTextLine(IWcR2Font font, string text, ref Vector2 pos, Color color, ref float maxWidth)
        {
            Vector2 size = font.MeasureString(text);

            TextBlock block = new TextBlock();
            block.Font = font;
            block.Text = text;
            block.Position = pos;
            block.ForeColor = color;

            maxWidth = Math.Max(pos.X + size.X, maxWidth);
            pos.X = 0;
            pos.Y += font.LineHeight;

            if (size.Y >= font.LineHeight)
            {
                pos.Y += size.Y - font.Size;
            }

            return block;
        }

        public static TextBlock[] PrepareFormatText(IWcR2Font font, string formatText, ref Vector2 pos, int width, ref float maxWidth, int height)
        {
            var layouter = new TextLayouter();
            int y = (int)pos.Y;
            var blocks = layouter.LayoutFormatText(font, formatText, width, ref y, height);
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i].Position.X += pos.X;
                var blockWidth = blocks[i].Font.MeasureString(blocks[i].Text).X;
                maxWidth = Math.Max(maxWidth, blocks[i].Position.X + blockWidth);
            }
            pos.X = 0;
            pos.Y = y;
            return blocks;
        }

        public static TextBlock[] Prepare(LifeInfo info, MapRenderFonts fonts, out Vector2 size)
        {
            var blocks = new List<TextBlock>();
            var current = Vector2.Zero;
            size = Vector2.Zero;

            blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "レベル: " + info.level + (info.boss ? " (ボス)" : null), ref current, Color.White, ref size.X));


            blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "HP: " + ToCJKNumberExpr(info.maxHP), ref current, Color.White, ref size.X));
            blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "MP: " + ToCJKNumberExpr(info.maxMP), ref current, Color.White, ref size.X));
            blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "物理ダメージ: " + ToCJKNumberExpr(info.PADamage), ref current, Color.White, ref size.X));
            blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "魔法ダメージ: " + ToCJKNumberExpr(info.MADamage), ref current, Color.White, ref size.X));
            blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "物理防御率: " + info.PDRate + "%", ref current, Color.White, ref size.X));
            blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "魔法防御率: " + info.MDRate + "%", ref current, Color.White, ref size.X));
            blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "経験値: " + ToCJKNumberExpr(info.exp), ref current, Color.White, ref size.X));
            if (info.undead) blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "不死型", ref current, Color.White, ref size.X));
            StringBuilder sb;
            if ((sb = GetLifeElemAttrString(ref info.elemAttr)).Length > 0)
                blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "エレメント: " + sb.ToString(), ref current, Color.White, ref size.X));
            size.Y = current.Y;

            return blocks.ToArray();
        }

        public static StringBuilder GetLifeElemAttrString(ref LifeInfo.ElemAttr elemAttr)
        {
            StringBuilder sb = new StringBuilder(14);
            sb.Append(GetElemResistanceString("氷", elemAttr.I));
            sb.Append(GetElemResistanceString("雷", elemAttr.L));
            sb.Append(GetElemResistanceString("火", elemAttr.F));
            sb.Append(GetElemResistanceString("毒", elemAttr.S));
            sb.Append(GetElemResistanceString("聖", elemAttr.H));
            sb.Append(GetElemResistanceString("闇", elemAttr.D));
            sb.Append(GetElemResistanceString("物", elemAttr.P));
            return sb;
        }

        public static string GetElemResistanceString(string elemName, LifeInfo.ElemResistance resist)
        {
            string e = null;
            switch (resist)
            {
                case LifeInfo.ElemResistance.Immune: e = "× "; break;
                case LifeInfo.ElemResistance.Resist: e = "△ "; break;
                case LifeInfo.ElemResistance.Normal: e = null; break;
                case LifeInfo.ElemResistance.Weak: e = "◎ "; break;
            }
            return e != null ? (elemName + e) : null;
        }

        public static string GetPortalTypeString(int pType)
        {
            switch (pType)
            {
                case 0: return "地図の誕生点";
                case 1: return "一般ポータル (非表示)";
                case 2: return "一般ポータル";
                case 3: return "一般ポータル (タッチ)";
                case 6: return "時空ゲートポイント";
                case 7: return "スクリプトポータル";
                case 8: return "スクリプトポータル (非表示)";
                case 9: return "スクリプトポータル (タッチ)";
                case 10: return "マップ内のポータル";
                case 12: return "弾性デバイス";
                default: return null;
            }
        }

        public struct TextBlock
        {
            public Vector2 Position;
            public Color ForeColor;
            public IWcR2Font Font;
            public string Text;
        }

        public class TextLayouter : WzComparerR2.Text.TextRenderer<IWcR2Font>
        {
            public TextLayouter() : base()
            {

            }

            List<TextBlock> blocks;

            public TextBlock[] LayoutFormatText(IWcR2Font font, string s, int width, ref int y, int height)
            {
                this.blocks = new List<TextBlock>();
                //base.DrawFormatString(s, font, width, ref y, (int)Math.Ceiling(font.LineHeight));
                base.DrawFormatString(s, font, width, ref y, height);
                return this.blocks.ToArray();
            }

            protected override void Flush(StringBuilder sb, int startIndex, int length, int x, int y, string colorID)
            {
                this.blocks.Add(new TextBlock()
                {
                    Position = new Vector2(x, y),
                    ForeColor = this.GetColor(colorID),
                    Font = this.font,
                    Text = sb.ToString(startIndex, length),
                });
            }

            protected override void MeasureRuns(List<WzComparerR2.Text.Run> runs)
            {
                int x = 0;
                foreach (var run in runs)
                {
                    if (run.IsBreakLine)
                    {
                        run.X = x;
                        run.Length = 0;
                    }
                    else
                    {
                        var size = base.font.MeasureString(this.sb.ToString(run.StartIndex, run.Length));
                        run.X = x;
                        run.Width = (int)size.X;
                        x += run.Width;
                    }
                }
            }

            protected override System.Drawing.Rectangle[] MeasureChars(int startIndex, int length)
            {
                var regions = new System.Drawing.Rectangle[length];
                int x = 0;
                for (int i = 0; i < length; i++)
                {
                    var text = this.sb[startIndex + i].ToString();
                    var size = this.font.MeasureString(text);
                    regions[i] = new System.Drawing.Rectangle(x, 0, (int)size.X, (int)size.Y);
                    x += (int)size.X;
                }
                return regions;
            }

            public virtual Color GetColor(string colorID)
            {
                switch (colorID)
                {
                    case "c":
                        return new Color(255, 153, 0);
                    default:
                        return Color.White;
                }
            }


        }
    }
}
