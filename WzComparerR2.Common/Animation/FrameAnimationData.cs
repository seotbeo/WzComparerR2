using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WzComparerR2.WzLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Drawing;
using WzComparerR2.Rendering;

namespace WzComparerR2.Animation
{
    public class FrameAnimationData 
    {
        public FrameAnimationData()
        {
            this.Frames = new List<Frame>();
            this.Origin = new Microsoft.Xna.Framework.Point();
        }

        public FrameAnimationData(IEnumerable<Frame> frames)
        {
            this.Frames = new List<Frame>(frames);
            this.Origin = new Microsoft.Xna.Framework.Point();
        }

        public List<Frame> Frames { get; private set; }

        public Microsoft.Xna.Framework.Point Origin { get; private set; }

        public Microsoft.Xna.Framework.Rectangle GetBound()
        {
            Microsoft.Xna.Framework.Rectangle? bound = null;
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
                    bound = Microsoft.Xna.Framework.Rectangle.Union(frame.Rectangle, bound.Value);
                }
            }
            return bound ?? Microsoft.Xna.Framework.Rectangle.Empty;
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

        public static FrameAnimationData MergeAnimationData(FrameAnimationData baseData, FrameAnimationData addData, GraphicsDevice graphicsDevice, Microsoft.Xna.Framework.Color bgColor)
        {
            var anime = new FrameAnimationData();
            int baseCount = 0;
            int addCount = 0;
            int baseMax = baseData.Frames.Count;
            int addMax = addData.Frames.Count;
            int baseDelayAll = 0;
            int addDelayAll = 0;
            int globalDelay = 0;

            foreach (var frame in baseData.Frames)
            {
                baseDelayAll += frame.Delay;
            }
            foreach (var frame in addData.Frames)
            {
                addDelayAll += frame.Delay;
            }

            int maxDelay = Math.Min(baseDelayAll, addDelayAll);

            while (true)
            {
                int thisDelay = Math.Min(baseData.Frames[baseCount].Delay, addData.Frames[addCount].Delay);
                Microsoft.Xna.Framework.Point newOrigin;
                globalDelay += thisDelay;

                Frame thisFrame = new Frame(MergeFrameTextures(baseData.Frames[baseCount], addData.Frames[addCount], graphicsDevice, out newOrigin, bgColor));
                thisFrame.Origin = newOrigin;
                thisFrame.Z = baseData.Frames[baseCount].Z;
                thisFrame.Delay = thisDelay;
                thisFrame.A0 = 255;
                thisFrame.A1 = 255;
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
                if (globalDelay >= maxDelay || baseCount >= baseMax || addCount >= addMax) break;
            }

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

            if (anime.Frames.Count > 0)
                return anime;
            else
                return null;
        }

        private static Texture2D MergeFrameTextures(Frame frame1, Frame frame2, GraphicsDevice graphicsDevice, out Microsoft.Xna.Framework.Point newOrigin, Microsoft.Xna.Framework.Color bgColor)
        {
            Texture2D texture1 = frame1.Texture;
            Texture2D texture2 = frame2.Texture;

            int dl = Math.Max(frame2.Origin.X - frame1.Origin.X, 0);
            int dt = Math.Max(frame2.Origin.Y - frame1.Origin.Y, 0);
            int dr = Math.Max((-frame2.Origin.X + texture2.Width) - (-frame1.Origin.X + texture1.Width), 0);
            int db = Math.Max((-frame2.Origin.Y + texture2.Height) - (-frame1.Origin.Y + texture1.Height), 0);

            int width = texture1.Width + dl + dr;
            int height = texture1.Height + dt + db;
            newOrigin = new Microsoft.Xna.Framework.Point(frame1.Origin.X + dl, frame1.Origin.Y + dt);

            //1
            
            RenderTarget2D renderTarget = new RenderTarget2D(graphicsDevice, width, height, false, SurfaceFormat.Bgra32, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
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

            spriteBatch.Draw(texture1, new Vector2(dl, dt), null, Microsoft.Xna.Framework.Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            spriteBatch.Draw(texture2, new Vector2(newOrigin.X - frame2.Origin.X, newOrigin.Y - frame2.Origin.Y), null, Microsoft.Xna.Framework.Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

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

            Texture2D mergedTexture = new Texture2D(graphicsDevice, width, height, SurfaceFormat.Color);
            mergedTexture.SetData(mergedData);

            return mergedTexture;*/
        }

        private static Microsoft.Xna.Framework.Color MixColors(Microsoft.Xna.Framework.Color baseColor, Microsoft.Xna.Framework.Color addColor)
        {
            var newA = baseColor.A + addColor.A * (255 - baseColor.A) / 255;
            if (newA == 0) return new Microsoft.Xna.Framework.Color(255, 255, 255, 0);

            var newR = (baseColor.R * baseColor.A / 255 * (255 - addColor.A) / 255 + addColor.R * addColor.A / 255) * 255 / newA;
            var newG = (baseColor.G * baseColor.A / 255 * (255 - addColor.A) / 255 + addColor.G * addColor.A / 255) * 255 / newA;
            var newB = (baseColor.B * baseColor.A / 255 * (255 - addColor.A) / 255 + addColor.B * addColor.A / 255) * 255 / newA;

            return new Microsoft.Xna.Framework.Color(newR, newG, newB, newA);
        }
    }
}
