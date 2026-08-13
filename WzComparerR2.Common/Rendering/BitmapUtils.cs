using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.Rendering
{
    public static class BitmapUtils
    {
        public static Bitmap ApplyAlphaMask_Format32bppArgb(Bitmap source, Bitmap mask, Point maskOffset)
        {
            if (source == null) return null;
            if (mask == null) return new Bitmap(source);

            if (source.PixelFormat != PixelFormat.Format32bppArgb || mask.PixelFormat != PixelFormat.Format32bppArgb)
            {
                return new Bitmap(source);
            }

            int width = source.Width;
            int height = source.Height;

            Rectangle rect = new Rectangle(0, 0, width, height);

            Bitmap maskTranslated = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(maskTranslated))
            {
                g.DrawImage(mask, new Rectangle(maskOffset.X, maskOffset.Y, mask.Width, mask.Height));
            }

            BitmapData srcData = null;
            BitmapData maskData = null;
            BitmapData dstData = null;

            try
            {
                srcData = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                maskData = maskTranslated.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                dstData = result.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                int srcStride = srcData.Stride;
                int maskStride = maskData.Stride;
                int dstStride = dstData.Stride;

                unsafe
                {
                    byte* srcBase = (byte*)srcData.Scan0;
                    byte* maskBase = (byte*)maskData.Scan0;
                    byte* dstBase = (byte*)dstData.Scan0;

                    for (int y = 0; y < height; y++)
                    {
                        byte* srcRow = srcBase + (y * srcStride);
                        byte* maskRow = maskBase + (y * maskStride);
                        byte* dstRow = dstBase + (y * dstStride);

                        for (int x = 0; x < width; x++)
                        {
                            int i = x * 4;

                            byte srcB = srcRow[i + 0];
                            byte srcG = srcRow[i + 1];
                            byte srcR = srcRow[i + 2];
                            byte srcA = srcRow[i + 3];

                            byte maskAlpha = maskRow[i + 3];

                            byte outA = (byte)(srcA * maskAlpha / 255);

                            dstRow[i + 0] = srcB;
                            dstRow[i + 1] = srcG;
                            dstRow[i + 2] = srcR;
                            dstRow[i + 3] = outA;
                        }
                    }
                }
            }
            finally
            {
                if (srcData != null) source.UnlockBits(srcData);
                if (maskData != null) maskTranslated.UnlockBits(maskData);
                if (dstData != null) result.UnlockBits(dstData);

                maskTranslated.Dispose();
            }

            return result;
        }

        public static BitmapOrigin ResizeBitmap(BitmapOrigin bitmapOrigin, float scale)
        {
            if (bitmapOrigin.Bitmap == null || scale <= 0 || scale == 1f) return bitmapOrigin;

            int newWidth = Math.Max(1, (int)Math.Round(bitmapOrigin.Bitmap.Width * scale));
            int newHeight = Math.Max(1, (int)Math.Round(bitmapOrigin.Bitmap.Height * scale));

            Bitmap newBitmap = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighSpeed;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.SmoothingMode = SmoothingMode.None;

                g.DrawImage(bitmapOrigin.Bitmap, new Rectangle(0, 0, newWidth, newHeight), new Rectangle(0, 0, bitmapOrigin.Bitmap.Width, bitmapOrigin.Bitmap.Height), GraphicsUnit.Pixel);
            }
            Point newOrigin = new Point((int)Math.Round(bitmapOrigin.Origin.X * scale), (int)Math.Round(bitmapOrigin.Origin.Y * scale));

            return new BitmapOrigin(newBitmap, newOrigin);
        }
    }
}
