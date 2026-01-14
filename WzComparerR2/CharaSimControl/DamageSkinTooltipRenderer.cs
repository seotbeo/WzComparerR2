using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WzComparerR2.CharaSim;
using Resource = CharaSimResource.Resource;

namespace WzComparerR2.CharaSimControl
{
    public class DamageSkinTooltipRenderer : TooltipRender
    {
        public DamageSkinTooltipRenderer()
        {
        }

        private DamageSkin damageSkin;

        public DamageSkin DamageSkin
        {
            get { return damageSkin; }
            set { damageSkin = value; }
        }

        public override object TargetItem
        {
            get { return this.damageSkin; }
            set { this.damageSkin = value as DamageSkin; }
        }

        public bool UseMiniSize { get; set; }
        public bool AlwaysUseMseaFormat { get; set; }
        public bool DisplayUnitOnSingleLine { get; set; }
        public long DamageSkinNumber { get; set; }

        public override Bitmap Render()
        {
            if (this.damageSkin == null)
            {
                return null;
            }

            Bitmap customSampleNonCritical = GetCustomSample(DamageSkinNumber, UseMiniSize, false);
            Bitmap customSampleCritical = GetCustomSample(DamageSkinNumber, UseMiniSize, true);
            Bitmap extraBitmap = GetExtraEffect();
            Bitmap unitBitmap = null;

            int previewWidth = Math.Max(customSampleNonCritical.Width, customSampleCritical.Width);
            int previewHeight = customSampleNonCritical.Height + customSampleCritical.Height;

            if (extraBitmap != null)
            {
                previewWidth = Math.Max(previewWidth, extraBitmap.Width);
                previewHeight += extraBitmap.Height;
                if (DisplayUnitOnSingleLine)
                {
                    unitBitmap = GetUnit();
                    if (unitBitmap != null)
                    {
                        previewWidth = Math.Max(previewWidth, unitBitmap.Width);
                        previewHeight += unitBitmap.Height;
                    }
                }
            }

            int picH = 10;

            Bitmap tooltip = new Bitmap(previewWidth + 30, previewHeight + 30);
            Graphics g = Graphics.FromImage(tooltip);

            GearGraphics.DrawNewTooltipBack(g, 0, 0, tooltip.Width, tooltip.Height);

            if (this.ShowObjectID)
            {
                GearGraphics.DrawGearDetailNumber(g, 3, 3, $"{this.damageSkin.DamageSkinID.ToString()}", true);
            }

            g.DrawImage(customSampleNonCritical, 10, picH, new Rectangle(0, 0, customSampleNonCritical.Width, customSampleNonCritical.Height), GraphicsUnit.Pixel);

            picH += customSampleNonCritical.Height + 5;

            g.DrawImage(customSampleCritical, 10, picH, new Rectangle(0, 0, customSampleCritical.Width, customSampleCritical.Height), GraphicsUnit.Pixel);

            picH += customSampleCritical.Height + 5;

            if (unitBitmap != null)
            {
                g.DrawImage(unitBitmap, 10, picH, new Rectangle(0, 0, unitBitmap.Width, unitBitmap.Height), GraphicsUnit.Pixel);
                picH += unitBitmap.Height + 5;
            }

            if (extraBitmap != null)
            {
                g.DrawImage(extraBitmap, 10, picH, new Rectangle(0, 0, extraBitmap.Width, extraBitmap.Height), GraphicsUnit.Pixel);
            }

            customSampleNonCritical.Dispose();
            customSampleCritical.Dispose();
            g.Dispose();
            return tooltip;
        }

        public Bitmap GetCustomSample(long inputNumber, bool useMiniSize, bool isCritical)
        {
            string numberStr = "";
            if (DisplayUnitOnSingleLine)
            {
                numberStr = inputNumber.ToString();
            }
            else
            {
                switch (damageSkin.CustomType)
                {
                    case "hangul": // CJK Detailed
                        numberStr = ItemStringHelper.ToCJKNumberExpr(inputNumber, detailedExpr: true);
                        break;
                    case "hangulUnit": // CJK
                        numberStr = ItemStringHelper.ToCJKNumberExpr(inputNumber);
                        break;
                    case "glUnit": // GMS
                        numberStr = ItemStringHelper.ToThousandsNumberExpr(inputNumber, isMsea: this.AlwaysUseMseaFormat);
                        break;
                    case "glUnit2": // MSEA
                        numberStr = ItemStringHelper.ToThousandsNumberExpr(inputNumber, isMsea: true);
                        break;
                    default:
                        if (this.DamageSkin.MiniUnit.Count > 0) // Default to CJK format when units are available
                        {
                            numberStr = ItemStringHelper.ToCJKNumberExpr(inputNumber);
                        }
                        else
                        {
                            numberStr = inputNumber.ToString();
                        }
                        break;
                }
            }

            BitmapOrigin criticalSign = new BitmapOrigin();
            if (this.damageSkin.BigCriticalDigit.ContainsKey("effect3"))
            {
                criticalSign = this.damageSkin.BigCriticalDigit["effect3"];
            }

            int totalWidth = 0;
            int maxHeight = 0;

            var nums_1 = isCritical ? this.damageSkin.BigCriticalDigit : this.damageSkin.BigDigit;
            var nums_0 = isCritical ? this.damageSkin.MiniCriticalDigit : this.damageSkin.MiniDigit;
            var units_1 = isCritical ? this.damageSkin.BigCriticalUnit : this.damageSkin.BigUnit;
            var units_0 = isCritical ? this.damageSkin.MiniCriticalUnit : this.damageSkin.MiniUnit;
            var digitSpacing_1 = (isCritical ? this.damageSkin.BigCriticalDigitSpacing : this.damageSkin.BigDigitSpacing);
            var digitSpacing_0 = (isCritical ? this.damageSkin.MiniCriticalDigitSpacing : this.damageSkin.MiniDigitSpacing);
            var unitSpacing_1 = (isCritical ? this.damageSkin.BigCriticalUnitSpacing : this.damageSkin.BigUnitSpacing);
            var unitSpacing_0 = (isCritical ? this.damageSkin.MiniCriticalUnitSpacing : this.damageSkin.MiniUnitSpacing);
            var digitBottomFix_1 = (isCritical ? this.DamageSkin.BigCriticalDigitBottomFix : this.DamageSkin.BigDigitBottomFix);
            var digitBottomFix_0 = (isCritical ? this.DamageSkin.MiniCriticalDigitBottomFix : this.DamageSkin.MiniDigitBottomFix);
            var unitBottomFix_1 = (isCritical ? this.DamageSkin.BigCriticalUnitBottomFix : this.DamageSkin.BigUnitBottomFix);
            var unitBottomFix_0 = (isCritical ? this.DamageSkin.MiniCriticalUnitBottomFix : this.DamageSkin.MiniUnitBottomFix);
            var unitDotSpace_1 = (isCritical ? this.DamageSkin.BigCriticalUnitDotSpace : this.DamageSkin.BigUnitDotSpace);
            var unitDotSpace_0 = (isCritical ? this.DamageSkin.MiniCriticalUnitDotSpace : this.DamageSkin.MiniUnitDotSpace);

            int wave_height = 0;
            List<DamageSkinDrawInfo> drawOrder = new List<DamageSkinDrawInfo>();
            DamageSkinDrawInfo critInfo = null;

            if (isCritical && criticalSign.Bitmap != null)
            {
                critInfo = new DamageSkinDrawInfo(criticalSign, -criticalSign.Origin.X, -criticalSign.Origin.Y, false);
                maxHeight = Math.Max(maxHeight, critInfo.Y + critInfo.Height);
            }

            // Calculate total width and max height
            bool firstNum = true;
            bool firstUnit = true;
            bool bottomFix = false;
            int count = 0;
            int spacing = 0;
            BitmapOrigin tmpBmp = new BitmapOrigin();
            DamageSkinDrawInfo tmpInfo = null;
            foreach (char c in numberStr)
            {
                bool isUnit = true;
                string character = c.ToString();
                switch (character)
                {
                    case "0":
                    case "1":
                    case "2":
                    case "3":
                    case "4":
                    case "5":
                    case "6":
                    case "7":
                    case "8":
                    case "9":
                        if (firstNum)
                        {
                            firstNum = false;
                            tmpBmp = nums_1[character];
                            spacing = digitSpacing_1;
                            bottomFix = digitBottomFix_1;
                        }
                        else
                        {
                            tmpBmp = nums_0[character];
                            spacing = digitSpacing_0;
                            bottomFix = digitBottomFix_0;
                        }
                        isUnit = false;
                        break;

                    case "十":
                    case "십":
                    case ".":
                        if (firstUnit)
                        {
                            if (units_1.ContainsKey("0"))
                            {
                                firstUnit = false;
                                tmpBmp = units_1["0"];
                                if (character == ".") spacing = unitDotSpace_1;
                                else spacing = unitSpacing_1;
                                bottomFix = unitBottomFix_1;
                            }
                        }
                        else
                        {
                            if (units_0.ContainsKey("0"))
                            {
                                tmpBmp = units_1["0"];
                                if (character == ".") spacing = unitDotSpace_0;
                                else spacing = unitSpacing_0;
                                bottomFix = unitBottomFix_0;
                            }
                        }
                        break;

                    case "百":
                    case "백":
                    case "K":
                        if (firstUnit)
                        {
                            if (units_1.ContainsKey("1"))
                            {
                                firstUnit = false;
                                tmpBmp = units_1["1"];
                                spacing = unitSpacing_1;
                                bottomFix = unitBottomFix_1;
                            }
                        }
                        else
                        {
                            if (units_0.ContainsKey("1"))
                            {
                                tmpBmp = units_1["1"];
                                spacing = unitSpacing_0;
                                bottomFix = unitBottomFix_0;
                            }
                        }
                        break;

                    case "千":
                    case "천":
                    case "M":
                        if (firstUnit)
                        {
                            if (units_1.ContainsKey("2"))
                            {
                                firstUnit = false;
                                tmpBmp = units_1["2"];
                                spacing = unitSpacing_1;
                                bottomFix = unitBottomFix_1;
                            }
                        }
                        else
                        {
                            if (units_0.ContainsKey("2"))
                            {
                                tmpBmp = units_1["2"];
                                spacing = unitSpacing_0;
                                bottomFix = unitBottomFix_0;
                            }
                        }
                        break;

                    case "万":
                    case "萬":
                    case "만":
                    case "B":
                        if (firstUnit)
                        {
                            if (units_1.ContainsKey("3"))
                            {
                                firstUnit = false;
                                tmpBmp = units_1["3"];
                                spacing = unitSpacing_1;
                                bottomFix = unitBottomFix_1;
                            }
                        }
                        else
                        {
                            if (units_0.ContainsKey("3"))
                            {
                                tmpBmp = units_1["3"];
                                spacing = unitSpacing_0;
                                bottomFix = unitBottomFix_0;
                            }
                        }
                        break;

                    case "億":
                    case "亿":
                    case "억":
                    case "T":
                        if (firstUnit)
                        {
                            if (units_1.ContainsKey("4"))
                            {
                                firstUnit = false;
                                tmpBmp = units_1["4"];
                                spacing = unitSpacing_1;
                                bottomFix = unitBottomFix_1;
                            }
                            else if (character == "T")
                            {
                                tmpBmp = new BitmapOrigin(Resource.Unit_T, 20, 20);
                                spacing = 10;
                                bottomFix = true;
                            }
                        }
                        else
                        {
                            if (units_0.ContainsKey("4"))
                            {
                                tmpBmp = units_1["4"];
                                spacing = unitSpacing_0;
                                bottomFix = unitBottomFix_0;
                            }
                            else if (character == "T")
                            {
                                tmpBmp = new BitmapOrigin(Resource.Unit_T, 20, 20);
                                spacing = 10;
                                bottomFix = true;
                            }
                        }
                        break;


                    case "兆":
                    case "조":
                    case "Q":
                        if (firstUnit)
                        {
                            if (units_1.ContainsKey("5"))
                            {
                                firstUnit = false;
                                tmpBmp = units_1["5"];
                                spacing = unitSpacing_1;
                                bottomFix = unitBottomFix_1;
                            }
                            else if (character == "兆" || character == "조")
                            {
                                tmpBmp = new BitmapOrigin(Resource.Unit_E12, 20, 20);
                                spacing = 10;
                                bottomFix = true;
                            }
                            else if (character == "Q")
                            {
                                tmpBmp = new BitmapOrigin(Resource.Unit_Q, 20, 20);
                                spacing = 10;
                                bottomFix = true;
                            }
                        }
                        else
                        {
                            if (units_0.ContainsKey("5"))
                            {
                                tmpBmp = units_1["5"];
                                spacing = unitSpacing_0;
                                bottomFix = unitBottomFix_0;
                            }
                            else if (character == "兆" || character == "조")
                            {
                                tmpBmp = new BitmapOrigin(Resource.Unit_E12, 20, 20);
                                spacing = 10;
                                bottomFix = true;
                            }
                            else if (character == "Q")
                            {
                                tmpBmp = new BitmapOrigin(Resource.Unit_Q, 20, 20);
                                spacing = 10;
                                bottomFix = true;
                            }
                        }
                        break;

                    case "京":
                    case "경":
                        if (firstUnit)
                        {
                            if (units_1.ContainsKey("6"))
                            {
                                tmpBmp = units_1["6"];
                                spacing = unitSpacing_1;
                                bottomFix = unitBottomFix_1;
                            }
                            else if (character == "京" || character == "경")
                            {
                                tmpBmp = new BitmapOrigin(Resource.Unit_E16, 20, 20);
                                spacing = 10;
                                bottomFix = true;
                            }
                        }
                        else
                        {
                            if (units_0.ContainsKey("6"))
                            {
                                tmpBmp = units_1["6"];
                                spacing = unitSpacing_0;
                                bottomFix = unitBottomFix_0;
                            }
                            else if (character == "京" || character == "경")
                            {
                                tmpBmp = new BitmapOrigin(Resource.Unit_E16, 20, 20);
                                spacing = 10;
                                bottomFix = true;
                            }
                        }
                        break;

                    default:
                        tmpBmp.Bitmap = null;
                        break;
                }

                if (tmpBmp.Bitmap != null)
                {
                    tmpInfo = new DamageSkinDrawInfo(tmpBmp, totalWidth - tmpBmp.Origin.X, 0 - tmpBmp.Origin.Y - (count++ % 2 != 0 ? wave_height : 0), isUnit, bottomFix);
                    if (tmpInfo.BottomFix)
                        tmpInfo.Y = -tmpBmp.Bitmap.Height;

                    if (tmpInfo.IsUnit)
                        totalWidth += Math.Min(tmpInfo.Width, 50) + spacing;
                    else
                        totalWidth += Math.Min(tmpInfo.Width, 35) + spacing;
                    maxHeight = Math.Max(maxHeight, tmpInfo.Y + tmpInfo.Height);

                    drawOrder.Add(tmpInfo);
                }
            }

            var firstDraw = drawOrder.FirstOrDefault();
            int offsetX = -firstDraw.X;
            int offsetY = -drawOrder.Min(info => info.Y);
            int digitBaseY1 = drawOrder.Where(info => !info.IsUnit).Min(info => info.Y);
            int digitBaseY2 = drawOrder.Where(info => !info.IsUnit).Max(info => info.Y + info.Height);
            if (critInfo != null)
            {
                critInfo.X = firstDraw.X + firstDraw.OriginX + critInfo.X;
                critInfo.Y = firstDraw.Y + firstDraw.OriginY + critInfo.Y;
                offsetX = Math.Max(offsetX, -critInfo.X);
                offsetY = Math.Max(offsetY, -critInfo.Y);
            }
            totalWidth = drawOrder.LastOrDefault().X + drawOrder.LastOrDefault().Width;
            totalWidth += offsetX;
            maxHeight += offsetY;

            Bitmap finalBitmap = new Bitmap(totalWidth, maxHeight);

            using (Graphics g = Graphics.FromImage(finalBitmap))
            {
                g.Clear(Color.Transparent);
                if (critInfo != null)
                {
                    g.DrawImage(critInfo.Bmp, offsetX + critInfo.X, offsetY + critInfo.Y);
                }
                foreach (var drawInfo in drawOrder)
                {
                    if (drawInfo.IsUnit)
                    {
                        g.DrawImage(drawInfo.Bmp, offsetX + drawInfo.X, offsetY + Math.Min(Math.Max(drawInfo.Y, digitBaseY1), digitBaseY2 - drawInfo.Height));
                    }
                    else
                    {
                        g.DrawImage(drawInfo.Bmp, offsetX + drawInfo.X, offsetY + drawInfo.Y);
                    }
                }
            }
            return finalBitmap;
        }

        public Bitmap GetUnit()
        {
            Bitmap unitBitmap = null;

            int width = 0;
            int height = 0;

            if (damageSkin.BigUnit.Count > 0)
            {
                if (UseMiniSize)
                {
                    foreach (var unit in damageSkin.MiniUnit.Values)
                    {
                        width += unit.Bitmap.Width;
                        height = Math.Max(height, unit.Bitmap.Height);
                        width += this.damageSkin.MiniUnitSpacing;
                    }
                    foreach (var unit in damageSkin.MiniCriticalUnit.Values)
                    {
                        width += unit.Bitmap.Width;
                        height = Math.Max(height, unit.Bitmap.Height);
                        width += this.damageSkin.MiniCriticalUnitSpacing;
                    }
                    unitBitmap = new Bitmap(width, height);
                    using (Graphics g = Graphics.FromImage(unitBitmap))
                    {
                        g.Clear(Color.Transparent);
                        int offsetX = 0;
                        foreach (var unit in damageSkin.MiniUnit.Values)
                        {
                            g.DrawImage(unit.Bitmap, offsetX, height - unit.Bitmap.Height);
                            offsetX += unit.Bitmap.Width;
                            offsetX += this.damageSkin.MiniUnitSpacing;
                        }
                        foreach (var unit in damageSkin.MiniCriticalUnit.Values)
                        {
                            g.DrawImage(unit.Bitmap, offsetX, height - unit.Bitmap.Height);
                            offsetX += unit.Bitmap.Width;
                            offsetX += this.damageSkin.MiniCriticalUnitSpacing;
                        }
                    }
                }
                else
                {
                    foreach (var unit in damageSkin.BigUnit.Values)
                    {
                        width += unit.Bitmap.Width;
                        height = Math.Max(height, unit.Bitmap.Height);
                        width += this.damageSkin.BigUnitSpacing;
                    }
                    foreach (var unit in damageSkin.BigCriticalUnit.Values)
                    {
                        width += unit.Bitmap.Width;
                        height = Math.Max(height, unit.Bitmap.Height);
                        width += this.damageSkin.BigCriticalUnitSpacing;
                    }
                    unitBitmap = new Bitmap(width, height);
                    using (Graphics g = Graphics.FromImage(unitBitmap))
                    {
                        g.Clear(Color.Transparent);
                        int offsetX = 0;
                        foreach (var unit in damageSkin.BigUnit.Values)
                        {
                            g.DrawImage(unit.Bitmap, offsetX, height - unit.Bitmap.Height);
                            offsetX += unit.Bitmap.Width;
                            offsetX += this.damageSkin.BigUnitSpacing;
                        }
                        foreach (var unit in damageSkin.BigCriticalUnit.Values)
                        {
                            g.DrawImage(unit.Bitmap, offsetX, height - unit.Bitmap.Height);
                            offsetX += unit.Bitmap.Width;
                            offsetX += this.damageSkin.BigCriticalUnitSpacing;
                        }
                    }
                }
            }
            return unitBitmap;
        }

        public Bitmap GetExtraEffect()
        {

            Bitmap[] originalBitmaps = new Bitmap[3]
            {
                this.damageSkin.MiniDigit.ContainsKey("Miss") ? this.damageSkin.MiniDigit?["Miss"].Bitmap : null,
                this.damageSkin.MiniDigit.ContainsKey("guard") ? this.damageSkin.MiniDigit?["guard"].Bitmap : null,
                this.damageSkin.MiniDigit.ContainsKey("resist") ? this.damageSkin.MiniDigit?["resist"].Bitmap : null,
                //this.damageSkin.MiniDigit.ContainsKey("shot") ? this.damageSkin.MiniDigit?["shot"].Bitmap : null,
                //this.damageSkin.MiniDigit.ContainsKey("counter") ? this.damageSkin.MiniDigit?["counter"].Bitmap : null
            };

            int width = 0;
            int height = 0;

            foreach (var bo in originalBitmaps)
            {
                if (bo == null) continue;
                width += bo.Width;
                height = Math.Max(height, bo.Height);
            }

            Bitmap bitmap = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                int offsetX = 0;
                for (int j = 0; j < originalBitmaps.Count(); j++)
                {
                    if (originalBitmaps[j] == null) continue;
                    g.DrawImage(originalBitmaps[j], offsetX, height - originalBitmaps[j].Height);
                    offsetX += originalBitmaps[j].Width;
                }
            }

            return bitmap;
        }

        private class DamageSkinDrawInfo
        {
            public DamageSkinDrawInfo(BitmapOrigin bo, int x, int y, bool isUnit, bool bottomFix = false)
            {
                BO = bo;
                X = x;
                Y = y;
                IsUnit = isUnit;
                BottomFix = bottomFix;
            }

            public BitmapOrigin BO;
            public int X;
            public int Y;
            public bool BottomFix;
            public Bitmap Bmp { get { return this.BO.Bitmap; } }
            public int Width { get { return this.Bmp?.Width ?? 0; } }
            public int Height { get { return this.Bmp?.Height ?? 0; } }
            public int OriginX { get { return this.BO.Origin.X; } }
            public int OriginY { get { return this.BO.Origin.Y; } }
            public bool IsUnit;
        }
    }
}
