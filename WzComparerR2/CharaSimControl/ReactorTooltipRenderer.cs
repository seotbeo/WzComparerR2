using System;
using System.Collections.Generic;
using System.Drawing;
using WzComparerR2.CharaSim;
using static WzComparerR2.CharaSimControl.RenderHelper;

namespace WzComparerR2.CharaSimControl
{
    public class ReactorTooltipRenderer : TooltipRender
    {
        public ReactorTooltipRenderer()
        {
        }

        public override object TargetItem
        {
            get { return this.Reactor; }
            set { this.Reactor = value as Reactor; }
        }

        public Reactor Reactor { get; set; }


        public override Bitmap Render()
        {
            if (Reactor == null)
            {
                return null;
            }

            Bitmap image = Reactor.Thumbnail.Bitmap;
            const int Margin = 20;
            const int ContentGap = 8;
            const int LineGap = 4;
            List<TextBlock> textBlocks = new List<TextBlock>();
            using (Bitmap measureBitmap = new Bitmap(1, 1))
            using (Graphics measureGraphics = Graphics.FromImage(measureBitmap))
            {
                int textY = 0;

                string name = string.IsNullOrEmpty(Reactor.Name) ? "(null)" : Reactor.Name;
                TextBlock block = PrepareText(measureGraphics, $"이름: {name}", GearGraphics.ItemDetailFont, Brushes.White, 0, textY);
                textBlocks.Add(block);

                textY += block.Size.Height + LineGap;
                block = PrepareText(measureGraphics, $"클립: {Reactor.ClipCount}개", GearGraphics.ItemDetailFont, Brushes.White, 0, textY);
                textBlocks.Add(block);

                if (!string.IsNullOrEmpty(Reactor.Action))
                {
                    textY += block.Size.Height + LineGap;
                    textBlocks.Add(PrepareText(measureGraphics, $"액션: {Reactor.Action}", GearGraphics.ItemDetailFont, Brushes.White, 0, textY));
                }
            }

            Rectangle textRect = Measure(textBlocks);
            int imageWidth = image?.Width ?? 0;
            int imageHeight = image?.Height ?? 0;
            int gap = image != null && !textRect.IsEmpty ? ContentGap : 0;
            int contentWidth = imageWidth + gap + textRect.Width;
            int contentHeight = Math.Max(imageHeight, textRect.Height);
            Bitmap bmp = new Bitmap(Math.Max(1, contentWidth + Margin * 2), Math.Max(1, contentHeight + Margin * 2));
            using (Graphics g = Graphics.FromImage(bmp))
            {
                GearGraphics.DrawNewTooltipBack(g, 0, 0, bmp.Width, bmp.Height);
                if (image != null)
                {
                    g.DrawImage(image, Margin, Margin + (contentHeight - imageHeight) / 2);
                }

                Point textOffset = new Point(Margin + imageWidth + gap, Margin + (contentHeight - textRect.Height) / 2);
                foreach (TextBlock block in textBlocks)
                {
                    DrawText(g, block, textOffset);
                }

                if (this.ShowObjectID)
                {
                    GearGraphics.DrawGearDetailNumber(g, 3, 3, this.Reactor.ID.ToString("D7"), true);
                }
            }

            return bmp;
        }
    }
}
