using System;
using System.Collections.Generic;
using System.Drawing;
using WzComparerR2.CharaSim;
using static WzComparerR2.CharaSimControl.RenderHelper;

namespace WzComparerR2.CharaSimControl
{
    public class WorldArchiveTooltipRender : TooltipRender
    {
        public WorldArchiveTooltipRender()
        {
        }

        public string WorldArchiveMessage { get; set; }

        public override Bitmap Render()
        {
            if (string.IsNullOrEmpty(WorldArchiveMessage))
                return null;

            int height = 30;
            Bitmap bmp1 = new Bitmap(1, 1);
            using (Graphics g = Graphics.FromImage(bmp1))
            {
                foreach (var i in SplitLine(WorldArchiveMessage))
                {
                    GearGraphics.DrawPlainText(g, i, GearGraphics.ItemDetailFont, Color.White, 13, 272, ref height, 16);
                }
            }
            height += 13;

            Bitmap bmp2 = new Bitmap(300, height);
            using (Graphics g = Graphics.FromImage(bmp2))
            {
                GearGraphics.DrawNewTooltipBack(g, 0, 0, bmp2.Width, bmp2.Height);
                int picH = 8;
                GearGraphics.DrawPlainText(g, "월드 아카이브", GearGraphics.ItemDetailFont2, Color.FromArgb(255, 255, 255), 8, 130, ref picH, 13);
                picH = 30;
                foreach (var i in SplitLine(WorldArchiveMessage))
                {
                    GearGraphics.DrawPlainText(g, i, GearGraphics.ItemDetailFont, Color.White, 13, 272, ref picH, 16);
                }
            }
            return bmp2;
        }

        private string[] SplitLine(string orgText)
        {
            return orgText.Split(new string[] { "\r\n", "\\r\\n", "\\r", "\\n" }, StringSplitOptions.None);
        }
    }
}
