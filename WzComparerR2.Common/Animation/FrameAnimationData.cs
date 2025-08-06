using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.Rendering;
using WzComparerR2.WzLib;

namespace WzComparerR2.Animation
{
    public class FrameAnimationData 
    {
        public FrameAnimationData()
        {
            this.Frames = new List<Frame>();
        }

        public FrameAnimationData(IEnumerable<Frame> frames)
        {
            this.Frames = new List<Frame>(frames);
        }

        public List<Frame> Frames { get; private set; }

        public Rectangle GetBound()
        {
            Rectangle? bound = null;
            foreach (var frame in this.Frames)
            {
                bound = bound == null ? frame.Rectangle : Rectangle.Union(frame.Rectangle, bound.Value);
            }
            return bound ?? Rectangle.Empty;
        }

        public static FrameAnimationData CreateFromNode(Wz_Node node, GraphicsDevice graphicsDevice, FrameAnimationCreatingOptions options, GlobalFindNodeFunction findNode, bool loadTexture = true)
        {
            if (node == null)
                return null;
            var anime = new FrameAnimationData();
            if (options.HasFlag(FrameAnimationCreatingOptions.ScanAllChildrenFrames))
            {
                foreach(var frameNode in node.Nodes)
                {
                    Frame frame = Frame.CreateFromNode(frameNode, graphicsDevice, findNode, loadTexture);
                    if (frame != null)
                    {
                        anime.Frames.Add(frame);
                    }
                }
            }
            else
            {
                for (int i = 0; ; i++)
                {
                    Wz_Node frameNode = node.FindNodeByPath(i.ToString());

                    if (frameNode == null || frameNode.Value == null)
                        break;
                    Frame frame = Frame.CreateFromNode(frameNode, graphicsDevice, findNode, loadTexture);

                    if (frame == null)
                        break;
                    anime.Frames.Add(frame);
                }
            }

            if (anime.Frames.Count > 0)
                return anime;
            else
                return null;
        }


        public static FrameAnimationData CreateFromPngNode(Wz_Node node, GraphicsDevice graphicsDevice, GlobalFindNodeFunction findNode)
        {
            if (node == null || node.Value == null)
                return null;
            var anime = new FrameAnimationData();

            Frame frame = Frame.CreateFromNode(node, graphicsDevice, findNode);

            if (frame != null) anime.Frames.Add(frame);

            if (anime.Frames.Count > 0)
                return anime;
            else
                return null;
        }

        public static FrameAnimationData CreateRectData(GraphicsDevice graphicsDevice, System.Drawing.Color baseColor, IEnumerable<TimelineData> alphaTimeline)
        {
            var thickness = 2;

            var tmpFrameAnimationData = new FrameAnimationData();
            var outlineColor = baseColor.ToXnaColor();
            foreach (var item in alphaTimeline)
            {
                var alpha = item.Alpha;
                var length = item.Delay;
                var lt = item.LT;
                var rb = item.RB;
                var width = -lt.X + rb.X;
                var height = -lt.Y + rb.Y;
                Point origin = new Point(-lt.X, -lt.Y);

                if (length <= 0) continue;
                if (width <= 0 || height <= 0)
                {
                    tmpFrameAnimationData.Frames.Add(new Frame(null, origin, 0, length, true));
                    continue;
                }

                var fillColor = System.Drawing.Color.FromArgb((255 * alpha / 100), baseColor).ToXnaColor();

                using SpriteBatchEx spriteBatch = new SpriteBatchEx(graphicsDevice);
                Rectangle rectangle = new Rectangle(0, 0, width, height);

                RenderTarget2D renderTarget = new RenderTarget2D(graphicsDevice, width, height, false, SurfaceFormat.Bgra32, DepthFormat.None, 0, Microsoft.Xna.Framework.Graphics.RenderTargetUsage.DiscardContents);
                graphicsDevice.SetRenderTarget(renderTarget);
                graphicsDevice.Clear(Color.Transparent);

                spriteBatch.Begin();

                spriteBatch.FillRectangle(rectangle, fillColor);
                spriteBatch.DrawThickRectangle(rectangle, outlineColor, thickness);

                spriteBatch.End();
                graphicsDevice.SetRenderTarget(null);

                var tmpFrame = new Frame((Texture2D)renderTarget, origin, 0, length, true);
                tmpFrameAnimationData.Frames.Add(tmpFrame);
            }

            if (tmpFrameAnimationData.Frames.Count > 0)
                return tmpFrameAnimationData;
            else
                return null;
        }

        public static FrameAnimationData CreateCircleData(GraphicsDevice graphicsDevice, int radius, System.Drawing.Color baseColor, IEnumerable<TimelineData> alphaTimeline)
        {
            int thickness = 2;

            var tmpFrameAnimationData = new FrameAnimationData();
            var outlineColor = baseColor;
            foreach (var item in alphaTimeline)
            {
                var alpha = item.Alpha;
                var length = item.Delay;
                var pos = item.LT;
                var x = pos.X;
                var y = pos.Y;
                Point origin = new Point(-x + radius, -y + radius);

                if (length <= 0) continue;
                if (radius <= 0)
                {
                    tmpFrameAnimationData.Frames.Add(new Frame(null, origin, 0, length, true));
                    continue;
                }

                var fillColor = System.Drawing.Color.FromArgb((255 * alpha / 100), baseColor);

                using var bmp = new System.Drawing.Bitmap(radius * 2, radius * 2);
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using (var brush = new System.Drawing.SolidBrush(fillColor))
                    {
                        g.FillEllipse(brush, 0, 0, radius * 2, radius * 2);
                    }
                    using (var pen = new System.Drawing.Pen(outlineColor, thickness))
                    {
                        int inset = thickness / 2;
                        g.DrawEllipse(pen, inset, inset, radius * 2 - thickness, radius * 2 - thickness);
                    }
                }

                var tmpFrame = new Frame(bmp.ToTexture(graphicsDevice), origin, 0, length, true);
                tmpFrameAnimationData.Frames.Add(tmpFrame);
            }

            if (tmpFrameAnimationData.Frames.Count > 0)
                return tmpFrameAnimationData;
            else
                return null;
        }

        public static FrameAnimationData MergeAnimationData(FrameAnimationData baseData, FrameAnimationData addData, GraphicsDevice graphicsDevice, int delayOffset, int moveX, int moveY, int frameStart, int frameEnd)
        {
            var anime = new FrameAnimationData();
            int baseCount = 0;
            //int addCount = 0;
            int addCount = frameStart;
            int baseMax = baseData.Frames.Count;
            //int addMax = addData.Frames.Count;
            int addMax = frameEnd + 1;
            int baseDelayAll = 0;
            int addDelayAll = 0;
            int globalDelay = 0;

            // dispose useless textures
            for (int i = 0; i < frameStart; i++)
            {
                var frameD = addData.Frames[i];
                if (frameD.Texture != null && !frameD.Texture.IsDisposed)
                {
                    frameD.Texture.Dispose();
                }
            }
            for (int i = frameEnd + 1; i < addData.Frames.Count; i++)
            {
                var frameD = addData.Frames[i];
                if (frameD.Texture != null && !frameD.Texture.IsDisposed)
                {
                    frameD.Texture.Dispose();
                }
            }

            foreach (var frame in baseData.Frames)
            {
                baseDelayAll += frame.Delay;
            }
            for (int i = addCount; i < addMax; i++)
            {
                addDelayAll += addData.Frames[i].Delay;
                addData.Frames[i].Origin = new Point(addData.Frames[i].Origin.X - moveX, addData.Frames[i].Origin.Y - moveY);
            }
            /*
            foreach (var frame in addData.Frames)
            {
                addDelayAll += frame.Delay;
                frame.Origin = new Point(frame.Origin.X - moveX, frame.Origin.Y - moveY);
            }
            */

            if (baseDelayAll <= delayOffset) // base 애니메이션 후에 add 애니메이션 재생
            {
                for (int i = baseCount; i < baseMax; i++)
                {
                    if (baseData.Frames[i].Delay != 0)
                    {
                        anime.Frames.Add(baseData.Frames[i]);
                    }
                }

                if (baseDelayAll != delayOffset)
                {
                    Frame f = new Frame(null, Point.Zero, baseData.Frames[baseMax - 1].Z, delayOffset - baseDelayAll, baseData.Frames[baseMax - 1].Blend); // 더미 프레임
                    anime.Frames.Add(f);
                }

                for (int i = addCount; i < addMax; i++)
                {
                    if (addData.Frames[i].Delay != 0)
                    {
                        anime.Frames.Add(addData.Frames[i]);
                    }
                }
            }
            else // base 애니메이션 중에 add 애니메이션 재생
            {
                // delayOffset 처리
                int frontDelay = delayOffset;
                int baseDisposeStart = 0;
                while (frontDelay > 0)
                {
                    if (baseData.Frames[baseCount].Delay > frontDelay)
                    {
                        var curFrame = baseData.Frames[baseCount];
                        Frame f = new Frame(curFrame.Texture, curFrame.Origin, curFrame.Z, frontDelay, curFrame.Blend);
                        anime.Frames.Add(f);

                        baseData.Frames[baseCount].Delay -= frontDelay;
                        frontDelay = 0;
                        baseDisposeStart++;
                    }
                    else
                    {
                        var curFrame = baseData.Frames[baseCount];
                        Frame f = new Frame(curFrame.Texture, curFrame.Origin, curFrame.Z, curFrame.Delay, curFrame.Blend);
                        anime.Frames.Add(f);

                        frontDelay -= baseData.Frames[baseCount].Delay;
                        baseCount++;
                    }
                }
                baseDisposeStart += baseCount;

                // 프레임 합성
                int maxDelay = Math.Min(baseDelayAll, addDelayAll);
                if (maxDelay > 0)
                {
                    while (baseCount < baseMax && addCount < addMax)
                    {
                        int thisDelay = Math.Min(baseData.Frames[baseCount].Delay, addData.Frames[addCount].Delay);
                        Point newOrigin;
                        globalDelay += thisDelay;

                        Frame thisFrame = new Frame(MergeFrameTextures(baseData.Frames[baseCount], addData.Frames[addCount], graphicsDevice, out newOrigin),
                            newOrigin, baseData.Frames[baseCount].Z, thisDelay, baseData.Frames[baseCount].Blend);

                        anime.Frames.Add(thisFrame);

                        baseData.Frames[baseCount].Delay -= thisDelay;
                        addData.Frames[addCount].Delay -= thisDelay;

                        if (baseData.Frames[baseCount].Delay <= 0)
                        {
                            baseCount++;
                        }
                        if (addData.Frames[addCount].Delay <= 0)
                        {
                            addCount++;
                        }
                        if (globalDelay >= maxDelay) break;
                    }
                }

                // dispose textures which is not needed anymore
                for (int i = baseDisposeStart; i < baseCount; i++)
                {
                    var frameD = baseData.Frames[i];
                    if (frameD.Texture != null && !frameD.Texture.IsDisposed)
                    {
                        frameD.Texture.Dispose();
                    }
                }
                for (int i = frameStart; i < addCount; i++)
                {
                    var frameD = addData.Frames[i];
                    if (frameD.Texture != null && !frameD.Texture.IsDisposed)
                    {
                        frameD.Texture.Dispose();
                    }
                }

                // 남은 프레임 붙여넣기
                if (baseCount < baseMax)
                {
                    for (int i = baseCount; i < baseMax; i++)
                    {
                        anime.Frames.Add(baseData.Frames[i]);
                    }
                }
                else if (addCount < addMax)
                {
                    for (int i = addCount; i < addMax; i++)
                    {
                        anime.Frames.Add(addData.Frames[i]);
                    }
                }
            }

            if (anime.Frames.Count > 0)
                return anime;
            else
                return null;
        }

        private static Texture2D MergeFrameTextures(Frame frame1, Frame frame2, GraphicsDevice graphicsDevice, out Point newOrigin)
        {
            Texture2D texture1 = frame1.Texture;
            Texture2D texture2 = frame2.Texture;

            if (texture1 == null)
            {
                newOrigin = new Point(frame2.Origin.X, frame2.Origin.Y);
                return CopyTexture(graphicsDevice, texture2);
            }
            else if (texture2 == null)
            {
                newOrigin = new Point(frame1.Origin.X, frame1.Origin.Y);
                return CopyTexture(graphicsDevice, texture1);
            }

            int dl = Math.Max(frame2.Origin.X - frame1.Origin.X, 0);
            int dt = Math.Max(frame2.Origin.Y - frame1.Origin.Y, 0);
            int dr = Math.Max((-frame2.Origin.X + texture2.Width) - (-frame1.Origin.X + texture1.Width), 0);
            int db = Math.Max((-frame2.Origin.Y + texture2.Height) - (-frame1.Origin.Y + texture1.Height), 0);

            int width = texture1.Width + dl + dr;
            int height = texture1.Height + dt + db;
            newOrigin = new Point(frame1.Origin.X + dl, frame1.Origin.Y + dt);
            var offsetX = newOrigin.X - frame2.Origin.X - dl;
            var offsetY = newOrigin.Y - frame2.Origin.Y - dt;

            RenderTarget2D renderTarget = new RenderTarget2D(graphicsDevice, width, height, false, SurfaceFormat.Bgra32, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            using SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            using PngEffect pngEffect = new PngEffect(graphicsDevice);
            pngEffect.Overlay = true;

            graphicsDevice.SetRenderTarget(renderTarget);
            graphicsDevice.Clear(Color.Transparent);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            spriteBatch.Draw(texture1, new Vector2(dl, dt), null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            spriteBatch.End();

            pngEffect.Parameters["TextureDst"].SetValue(texture1);
            pngEffect.Parameters["scaler"].SetValue(new Vector2((float)texture2.Width / texture1.Width, (float)texture2.Height / texture1.Height));
            pngEffect.Parameters["offset"].SetValue(new Vector2((float)offsetX / texture1.Width, (float)offsetY / texture1.Height));

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, null, null, null, pngEffect, null);
            spriteBatch.Draw(texture2, new Vector2(newOrigin.X - frame2.Origin.X, newOrigin.Y - frame2.Origin.Y), null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            spriteBatch.End();

            graphicsDevice.SetRenderTarget(null);

            return renderTarget;
        }

        private static Texture2D CopyTexture(GraphicsDevice graphicsDevice, Texture2D texture)
        {
            if (texture == null) return null;

            RenderTarget2D renderTarget = new RenderTarget2D(graphicsDevice, texture.Width, texture.Height, false, SurfaceFormat.Bgra32, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            using SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);

            graphicsDevice.SetRenderTarget(renderTarget);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            spriteBatch.Draw(texture, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            spriteBatch.End();

            graphicsDevice.SetRenderTarget(null);

            return renderTarget;
        }

        public static void ApplyMovement(GraphicsDevice graphicsDevice, FrameAnimationData data, int speedX, int speedY, int goX, int goY, bool fullMove, int start, ref int end)
        {
            var result = new List<Frame>();
            var dispose = new List<Frame>();
            var interval = 30;
            var dx = speedX / 1000f * interval;
            var dy = speedY / 1000f * interval;
            var count = 0;
            var e = end;
            bool repeated = false;

            for (var i = 0; i < start; i++)
            {
                var frame = data.Frames[i];
                result.Add(frame);
            }
            for (var i = start; i < e + 1; i++)
            {
                var oFrame = data.Frames[i];
                var frame = new Frame(oFrame.Texture, oFrame.Origin, oFrame.Z, oFrame.Delay, oFrame.Blend);
                while (frame.Delay > 0)
                {
                    bool finishX = Math.Abs(dx * count) >= goX;
                    bool finishY = Math.Abs(dy * count) >= goY;
                    int x = finishX ? frame.Origin.X - (dx >= 0 ? goX : -goX) : (int)(frame.Origin.X - dx * count);
                    int y = finishY ? frame.Origin.Y - (dy >= 0 ? goY : -goY) : (int)(frame.Origin.Y - dy * count);
                    var temp = new Frame(CopyTexture(graphicsDevice, frame.Texture), new Point(x, y), frame.Z, interval, frame.Blend);
                    result.Add(temp);

                    frame.Delay -= interval;
                    if (finishX && finishY)
                    {
                        temp = new Frame(CopyTexture(graphicsDevice, frame.Texture), new Point(x, y), frame.Z, frame.Delay, frame.Blend);
                        result.Add(temp);
                        e = i;
                        break;
                    }
                    count++;
                    if (i == e && fullMove && frame.Delay <= 0 && (!finishX || !finishY))
                    {
                        i = start - 1;
                        repeated = true;
                    }
                }
                dispose.Add(oFrame);
            }
            if (!repeated)
            {
                for (var i = e + 1; i < data.Frames.Count; i++)
                {
                    var frame = data.Frames[i];
                    int x = frame.Origin.X - (dx >= 0 ? goX : -goX);
                    int y = frame.Origin.Y - (dy >= 0 ? goY : -goY);
                    frame.Origin = new Point(x, y);
                    result.Add(frame);
                }
            }

            foreach (var frame in dispose)
            {
                if (frame.Texture != null && !frame.Texture.IsDisposed)
                {
                    frame.Texture.Dispose();
                }
            }

            end += result.Count - data.Frames.Count;
            data.Frames = result;
        }

        public struct TimelineData
        {
            public Point LT;
            public Point RB;
            public int Alpha;
            public int Delay;
        }
    }

    [Flags]
    public enum FrameAnimationCreatingOptions
    {
        None = 0,
        FindFrameNameInOrdinalNumber = 1 << 0,
        ScanAllChildrenFrames = 1 << 1,
    }
}
