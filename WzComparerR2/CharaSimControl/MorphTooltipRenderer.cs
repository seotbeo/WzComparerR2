using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.CharaSim;

namespace WzComparerR2.CharaSimControl
{
    public class MorphTooltipRenderer : TooltipRender
    {
        public MorphTooltipRenderer()
        {
        }

        public override object TargetItem
        {
            get { return this.MorphInfo; }
            set { this.MorphInfo = value as Morph; }
        }

        public Morph MorphInfo { get; set; }


        public override Bitmap Render()
        {
            if (MorphInfo == null)
            {
                return null;
            }

            Bitmap image = MorphInfo.Default.Bitmap;

            const int Margin = 20;
            int width = image?.Width ?? 0;
            int height = image?.Height ?? 0;
            Bitmap bmp = new Bitmap(width + Margin * 2, height + Margin * 2);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                GearGraphics.DrawNewTooltipBack(g, 0, 0, bmp.Width, bmp.Height);
                if (image != null) g.DrawImage(image, Margin, Margin);

                if (this.ShowObjectID)
                {
                    GearGraphics.DrawGearDetailNumber(g, 3, 3, this.MorphInfo.ID.ToString("D4"), true);
                }
            }

            return bmp;
        }
    }
}
