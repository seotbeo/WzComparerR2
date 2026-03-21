using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using CharaSimResource;

namespace WzComparerR2.CharaSimControl
{
    class RenderHelper
    {
        private RenderHelper()
        {
        }

        public static TextBlock PrepareText(Graphics g, string text, Font font, Brush brush, int x, int y)
        {
            SizeF size = g.MeasureString(text, font, Int32.MaxValue, StringFormat.GenericTypographic);
            TextBlock block = new TextBlock()
            {
                Text = text,
                Font = font,
                Brush = brush,
                Position = new Point(x, y),
                Size = new Size((int)Math.Round(size.Width, MidpointRounding.AwayFromZero),
                    (int)Math.Round(size.Height, MidpointRounding.AwayFromZero))
            };
            return block;
        }

        public static void DrawText(Graphics g, TextBlock block, Point offset)
        {
            g.DrawString(block.Text, block.Font, block.Brush,
                block.Position.X + offset.X, block.Position.Y + offset.Y,
                StringFormat.GenericTypographic);
        }

        public static void DrawMultilineText(Graphics g, TextBlock block, Point offset, int width, int lineHeight, out int offsetY)
        {
            var x = block.Position.X + offset.X;
            var y = block.Position.Y + offset.Y;
            GearGraphics.DrawString(g, block.Text, block.Font, new Dictionary<string, Color>() { { "c", GearGraphics.SkillSummaryOrangeTextColor } }, x, width - 2, ref y, lineHeight, strictlyAlignLeft: 2, defaultColor: ((SolidBrush)block.Brush).Color);
            
            offsetY = y - (block.Position.Y + offset.Y) - lineHeight;
        }

        public static Rectangle Measure(IEnumerable<TextBlock> blocks)
        {
            Rectangle rect = Rectangle.Empty;

            foreach (var block in blocks)
            {
                var blockRect = block.Rectangle;
                if (!blockRect.IsEmpty)
                {
                    rect = Rectangle.Union(rect, blockRect);
                }
            }
            return rect;
        }

        public static void DrawV6SkillDotline(Graphics g, int x1, int x2, int y, bool use22Ani)
        {
            // here's a trick that we won't draw left and right part because it looks the same as background border.
            var picCenter = use22Ani ? Resource.UIToolTipNew_img_Skill_Frame_dotline_c : Resource.UIToolTip_img_Skill_Frame_dotline_c;
            using (var brush = new TextureBrush(picCenter))
            {
                brush.TranslateTransform(x1, y);
                g.FillRectangle(brush, new Rectangle(x1, y, x2 - x1, picCenter.Height));
            }
        }
    }
}
