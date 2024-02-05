using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.WzLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.Animation
{
    public class FrameAnimationData 
    {
        public FrameAnimationData()
        {
            this.Frames = new List<Frame>();
            this.Origin = new Point();
        }

        public FrameAnimationData(IEnumerable<Frame> frames)
        {
            this.Frames = new List<Frame>(frames);
            this.Origin = new Point();
        }

        public List<Frame> Frames { get; private set; }

        public Point Origin { get; private set; }

        public Rectangle GetBound()
        {
            Rectangle? bound = null;
            foreach (var frame in this.Frames)
            {
                //bound = bound == null ? frame.Rectangle : Rectangle.Union(frame.Rectangle, bound.Value);
                if (bound == null)
                {
                    bound = frame.Rectangle;
                    this.Origin = frame.Origin;
                }
                else
                {
                    bound = Rectangle.Union(frame.Rectangle, bound.Value);
                }
            }
            return bound ?? Rectangle.Empty;
        }

        public static FrameAnimationData CreateFromNode(Wz_Node node, GraphicsDevice graphicsDevice, GlobalFindNodeFunction findNode)
        {
            if (node == null)
                return null;
            var anime = new FrameAnimationData();
            for (int i = 0; ; i++)
            {
                Wz_Node frameNode = node.FindNodeByPath(i.ToString());

                if (frameNode == null || frameNode.Value == null)
                    break;
                Frame frame = Frame.CreateFromNode(frameNode, graphicsDevice, findNode);

                if (frame == null)
                    break;
                anime.Frames.Add(frame);
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

        public static FrameAnimationData MergeAnimationData(FrameAnimationData baseData, FrameAnimationData addData, GraphicsDevice graphicsDevice, Color bgColor, int delayOffset, int moveX, int moveY, int frameStart, int frameEnd)
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
                    anime.Frames.Add(baseData.Frames[i]);
                }

                if (baseDelayAll != delayOffset)
                {
                    Frame f = new Frame(); // 더미 프레임
                    f.Origin = new Point(0, 0);
                    f.Z = baseData.Frames[baseMax - 1].Z;
                    f.Delay = delayOffset - baseDelayAll;
                    f.Blend = baseData.Frames[baseMax - 1].Blend;
                    anime.Frames.Add(f);
                }

                for (int i = addCount; i < addMax; i++)
                {
                    anime.Frames.Add(addData.Frames[i]);
                }
            }
            else // base 애니메이션 중에 add 애니메이션 재생
            {
                // delayOffset 처리
                int frontDelay = delayOffset;
                while (frontDelay > 0)
                {
                    if (baseData.Frames[baseCount].Delay > frontDelay)
                    {
                        Frame f = new Frame(baseData.Frames[baseCount].Texture);
                        f.Origin = baseData.Frames[baseCount].Origin;
                        f.Z = baseData.Frames[baseCount].Z;
                        f.Delay = frontDelay;
                        f.Blend = baseData.Frames[baseCount].Blend;
                        anime.Frames.Add(f);

                        baseData.Frames[baseCount].Delay -= frontDelay;
                        frontDelay = 0;
                    }
                    else
                    {
                        anime.Frames.Add(baseData.Frames[baseCount]);
                        frontDelay -= baseData.Frames[baseCount].Delay;
                        baseCount++;
                    }
                }

                // 프레임 합성
                int maxDelay = Math.Min(baseDelayAll, addDelayAll);
                if (maxDelay > 0)
                {
                    while (baseCount < baseMax && addCount < addMax)
                    {
                        int thisDelay = Math.Min(baseData.Frames[baseCount].Delay, addData.Frames[addCount].Delay);
                        Point newOrigin;
                        globalDelay += thisDelay;

                        Frame thisFrame = new Frame(MergeFrameTextures(baseData.Frames[baseCount], addData.Frames[addCount], graphicsDevice, out newOrigin, bgColor));
                        thisFrame.Origin = newOrigin;
                        thisFrame.Z = baseData.Frames[baseCount].Z;
                        thisFrame.Delay = thisDelay;
                        thisFrame.Blend = baseData.Frames[baseCount].Blend;

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

        private static Texture2D MergeFrameTextures(Frame frame1, Frame frame2, GraphicsDevice graphicsDevice, out Point newOrigin, Color bgColor)
        {
            Texture2D texture1 = frame1.Texture;
            Texture2D texture2 = frame2.Texture;

            if (texture1 == null)
            {
                newOrigin = new Point(frame2.Origin.X, frame2.Origin.Y);
                return texture2;
            }

            int dl = Math.Max(frame2.Origin.X - frame1.Origin.X, 0);
            int dt = Math.Max(frame2.Origin.Y - frame1.Origin.Y, 0);
            int dr = Math.Max((-frame2.Origin.X + texture2.Width) - (-frame1.Origin.X + texture1.Width), 0);
            int db = Math.Max((-frame2.Origin.Y + texture2.Height) - (-frame1.Origin.Y + texture1.Height), 0);

            int width = texture1.Width + dl + dr;
            int height = texture1.Height + dt + db;
            newOrigin = new Point(frame1.Origin.X + dl, frame1.Origin.Y + dt);

            //1
            
            RenderTarget2D renderTarget = new RenderTarget2D(graphicsDevice, width, height, false, SurfaceFormat.Bgra32, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);

            graphicsDevice.SetRenderTarget(renderTarget);
            graphicsDevice.Clear(bgColor);

            spriteBatch.Begin(SpriteSortMode.Deferred, new BlendState()
                {
                    AlphaSourceBlend = Blend.One,
                    AlphaDestinationBlend = Blend.InverseSourceAlpha,
                    AlphaBlendFunction = BlendFunction.Add,
                    ColorSourceBlend = Blend.SourceAlpha,
                    ColorDestinationBlend = Blend.InverseSourceAlpha,
                    ColorBlendFunction = BlendFunction.Add,
                }
            );

            spriteBatch.Draw(texture1, new Vector2(dl, dt), null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
            spriteBatch.Draw(texture2, new Vector2(newOrigin.X - frame2.Origin.X, newOrigin.Y - frame2.Origin.Y), null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            spriteBatch.End();

            graphicsDevice.SetRenderTarget(null);

            return renderTarget;
            


            //2
            /*
            var data1 = new Microsoft.Xna.Framework.Color[texture1.Width * texture1.Height / 4];
            var data2 = new Microsoft.Xna.Framework.Color[texture2.Width * texture2.Height / 4];
            var mergedData = new Microsoft.Xna.Framework.Color[width * height];
            for (int i = 0; i < mergedData.Length; i++)
            {
                mergedData[i] = new Microsoft.Xna.Framework.Color(255, 255, 255, 0);
            }
            Console.WriteLine(texture1.Format);
            Console.WriteLine(texture2.Format);
            texture1.GetData<Microsoft.Xna.Framework.Color>(data1);
            texture2.GetData<Microsoft.Xna.Framework.Color>(data2);

            /*
            RenderTarget2D renderTarget = new RenderTarget2D(graphicsDevice, texture1.Width, texture1.Height);
            RenderTarget2D renderTarget2 = new RenderTarget2D(graphicsDevice, texture2.Width, texture2.Height);
            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);

            graphicsDevice.SetRenderTarget(renderTarget);
            graphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);

            spriteBatch.Begin(SpriteSortMode.Deferred, new BlendState()
                {
                    AlphaSourceBlend = Blend.One,
                    AlphaDestinationBlend = Blend.InverseSourceAlpha,
                    AlphaBlendFunction = BlendFunction.Add,
                    ColorSourceBlend = Blend.SourceAlpha,
                    ColorDestinationBlend = Blend.InverseSourceAlpha,
                    ColorBlendFunction = BlendFunction.Add,
                }
            );
            spriteBatch.Draw(texture1, Vector2.Zero, Microsoft.Xna.Framework.Color.White);
            spriteBatch.End();
            renderTarget.GetData(data1, 0, texture1.Width * texture1.Height);
            graphicsDevice.SetRenderTarget(renderTarget2);
            graphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);

            spriteBatch.Begin(SpriteSortMode.Deferred, new BlendState()
            {
                AlphaSourceBlend = Blend.One,
                AlphaDestinationBlend = Blend.InverseSourceAlpha,
                AlphaBlendFunction = BlendFunction.Add,
                ColorSourceBlend = Blend.SourceAlpha,
                ColorDestinationBlend = Blend.InverseSourceAlpha,
                ColorBlendFunction = BlendFunction.Add,
            }
            );
            spriteBatch.Draw(texture2, Vector2.Zero, Microsoft.Xna.Framework.Color.White);
            spriteBatch.End();
            renderTarget2.GetData(data2, 0, texture2.Width * texture2.Height);
            */
            /*
            graphicsDevice.SetRenderTarget(null);

            for (int y = dt; y < dt + texture1.Height; y++)
            {
                for (int x = dl; x < dl + texture1.Width; x++)
                {
                    mergedData[y * width + x] = data1[(y - dt) * texture1.Width + (x - dl)];
                }
            }

            for (int y = newOrigin.Y - frame2.Origin.Y; y < newOrigin.Y - frame2.Origin.Y + texture2.Height; y++)
            {
                for (int x = newOrigin.X - frame2.Origin.X; x < newOrigin.X - frame2.Origin.X + texture2.Width; x++)
                {
                    mergedData[y * width + x] = MixColors(mergedData[y * width + x], data2[(y - newOrigin.Y + frame2.Origin.Y) * texture2.Width + (x - newOrigin.X + frame2.Origin.X)]);
                }
            }

            Texture2D mergedTexture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Bgra32);
            mergedTexture.SetData(mergedData);

            return mergedTexture;*/
        }

        private static Color MixColors(Color baseColor, Color addColor)
        {
            var newA = baseColor.A + addColor.A * (255 - baseColor.A) / 255;
            if (newA == 0) return new Color(255, 255, 255, 0);

            var newR = (baseColor.R * baseColor.A / 255 * (255 - addColor.A) / 255 + addColor.R * addColor.A / 255) * 255 / newA;
            var newG = (baseColor.G * baseColor.A / 255 * (255 - addColor.A) / 255 + addColor.G * addColor.A / 255) * 255 / newA;
            var newB = (baseColor.B * baseColor.A / 255 * (255 - addColor.A) / 255 + addColor.B * addColor.A / 255) * 255 / newA;

            return new Color(newR, newG, newB, newA);
        }
    }
}
