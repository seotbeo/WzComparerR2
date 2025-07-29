using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpineV2 = Spine.V2;
using WzComparerR2.Animation;
using WzComparerR2.Common;
using WzComparerR2.Config;
using WzComparerR2.Controls;
using WzComparerR2.Encoders;
using WzComparerR2.Rendering;
using WzComparerR2.WzLib;

namespace WzComparerR2
{
    public class PictureBoxEx : AnimationControl
    {
        public PictureBoxEx() : base()
        {
            this.AutoAdjustPosition = true;
            this.sbInfo = new StringBuilder();
        }

        public bool AutoAdjustPosition { get; set; }
        public string PictureName { get; set; }
        public bool ShowInfo { get; set; }

        public override System.Drawing.Font Font
        {
            get { return base.Font; }
            set
            {
                base.Font = value;
                this.xnaFont?.Dispose();
                this.xnaFont = new XnaFont(this.GraphicsDevice, value);
            }
        }

        public XnaFont XnaFont
        {
            get
            {
                if (xnaFont == null && this.Font != null)
                {
                    this.xnaFont = new XnaFont(this.GraphicsDevice, this.Font);
                }
                return this.xnaFont;
            }
        }

        private XnaFont xnaFont;
        private SpriteBatchEx sprite;
        private StringBuilder sbInfo;

        public void ShowImage(Wz_Png png)
        {
            this.ShowImage(png, 0);
        }

        public void ShowImage(Wz_Png png, int page)
        {
            if (this.ShowOverlayAni) return; // 애니메이션 중첩 중일때는 자동 png 미리보기 없음

            //添加到动画控件
            var frame = new Animation.Frame()
            {
                Texture = png.ToTexture(page, this.GraphicsDevice),
                Png = png,
                Page = page,
                Delay = 0,
                Origin = Point.Zero,
            };

            var frameData = new Animation.FrameAnimationData();
            frameData.Frames.Add(frame);

            this.ShowAnimation(frameData);
        }

        public FrameAnimationData LoadVideo(Wz_Video wzVideo, Wz_Vector origin = null)
        {
            return new MaplestoryCanvasVideoLoader().Load(wzVideo, this.GraphicsDevice, origin);
        }

        public FrameAnimationData LoadFrameAnimation(Wz_Node node, FrameAnimationCreatingOptions options = default)
        {
            return FrameAnimationData.CreateFromNode(node, this.GraphicsDevice, options, PluginBase.PluginManager.FindWz);
        }

        public ISpineAnimationData LoadSpineAnimation(Wz_Node node)
        {
            return this.LoadSpineAnimation(SpineLoader.Detect(node));
        }

        public ISpineAnimationData LoadSpineAnimation(SpineDetectionResult detectionResult)
        {
            if (!detectionResult.Success)
                return null;
            var textureLoader = new WzSpineTextureLoader(detectionResult.SourceNode.ParentNode, this.GraphicsDevice, PluginBase.PluginManager.FindWz);
            // workaround for Map/Back/bossLimbo.img/spine/3/02_Passage_01_BgColor.skel, #266
            textureLoader.EnableTextureMissingFallback = true;
            if (detectionResult.Version == SpineVersion.V2)
                return SpineAnimationDataV2.Create(detectionResult, textureLoader);
            else if (detectionResult.Version == SpineVersion.V4)
                return SpineAnimationDataV4.Create(detectionResult, textureLoader);
            else
                return null;
        }

        public MultiFrameAnimationData LoadMultiFrameAnimation(Wz_Node node)
        {
            return MultiFrameAnimationData.CreateFromNode(node, this.GraphicsDevice, PluginBase.PluginManager.FindWz);
        }

        public FrameAnimationData LoadPngFrameAnimation(Wz_Node node)
        {
            return FrameAnimationData.CreateFromPngNode(node, this.GraphicsDevice, PluginBase.PluginManager.FindWz);
        }

        public FrameAnimationData ConvertSpineToFrameAnimation(AnimationItem aniItem, int delay = 60)
        {
            var frameAnimationData = new FrameAnimationData();
            if (delay > 0)
            {
                var rec = new AnimationRecoder(this.GraphicsDevice);

                rec.Items.Add(aniItem);
                int length = Math.Min(rec.GetMaxLength(), 10000); // 최대 길이 10초 제한
                IEnumerable<int> frames = length == 0 ? new[] { 0 } : Enumerable.Range(0, Math.Max((int)Math.Ceiling(1.0 * length / delay) - 1, 0));

                rec.ResetAll();
                rec.BackgroundColor = Color.Transparent;
                Microsoft.Xna.Framework.Rectangle bounds = aniItem.Measure();
                if (length > 0)
                {
                    for (int i = 0; i < frames.Count(); i++)
                    {
                        rec.Update(TimeSpan.FromMilliseconds(delay));
                        var rect = aniItem.Measure();
                        bounds = Microsoft.Xna.Framework.Rectangle.Union(bounds, rect);
                    }
                }
                bounds.Offset(aniItem.Position);

                rec.ResetAll();
                rec.Begin(bounds);
                for (int i = 0; i < frames.Count(); i++)
                {
                    rec.Draw();
                    rec.Update(TimeSpan.FromMilliseconds(delay));
                    var t2d = rec.GetPngTexture();
                    var frame = new Frame(t2d, new Point(-bounds.Left, -bounds.Top), 0, delay, true);
                    frameAnimationData.Frames.Add(frame);
                }
                rec.End();
            }

            this.DisposeAnimationItem(aniItem);

            if (frameAnimationData.Frames.Count > 0)
                return frameAnimationData;
            else
                return null;
        }

        public void ShowAnimation(FrameAnimationData data)
        {
            this.ShowAnimation(new FrameAnimator(data));
            this.ShowOverlayAni = false;
        }

        public void ShowAnimation(ISpineAnimationData data)
        {
            this.ShowAnimation(data.CreateAnimator() as AnimationItem);
            this.ShowOverlayAni = false;
        }

        public void ShowAnimation(MultiFrameAnimationData data)
        {
            this.ShowAnimation(new MultiFrameAnimator(data));
            this.ShowOverlayAni = false;
        }

        // 애니메이션 중첩
        public void ShowOverlayAnimation(FrameAnimationData data, string multiFrameInfo = null, bool isPngFrameAni = false)
        {
            this.ShowOverlayAnimation(new FrameAnimator(data), multiFrameInfo, isPngFrameAni);
        }

        public void ShowAnimation(AnimationItem animator)
        {
            ClearItemList();

            this.Items.Add(animator);

            if (this.AutoAdjustPosition)
            {
                this.AdjustPosition();
            }

            this.Invalidate();
        }

        // 애니메이션 중첩
        public void ShowOverlayAnimation(AnimationItem animator, string multiFrameInfo, bool isPngFrameAni)
        {
            if (!ShowOverlayAni)
            {
                ShowOverlayAni = !ShowOverlayAni;
                ClearItemList();
            }

            FrameAnimator baseAniItem;
            if (this.Items.Count == 0)
            {
                var tmpFrame = new Frame(null, Point.Zero, 0, 0, true);
                var tmpFrameAnimationData = new FrameAnimationData();
                tmpFrameAnimationData.Frames.Add(tmpFrame);
                baseAniItem = new FrameAnimator(tmpFrameAnimationData);
            }
            else baseAniItem = (FrameAnimator)this.Items[0];

            FrameAnimator aniItem = (FrameAnimator)animator;

            var frmOverlayAniOptions = new FrmOverlayAniOptions(aniItem.Data.Frames, multiFrameInfo, isPngFrameAni);
            int delayOffset = 0;
            int moveX = 0;
            int moveY = 0;
            int frameStart = 0;
            int frameEnd = 0;
            int speedX = 0;
            int speedY = 0;
            int goX = 0;
            int goY = 0;
            int pngDelay = 120;
            bool fullMove = false;

            // 정보 받아오기
            if (frmOverlayAniOptions.ShowDialog() == DialogResult.OK)
            {
                frmOverlayAniOptions.GetValues(out delayOffset, out moveX, out moveY, out frameStart, out frameEnd, out speedX, out speedY, out goX, out goY, out fullMove, out pngDelay);
                frameStart = frameStart == -1 ? 0 : frameStart;
                frameEnd = frameEnd == -1 ? aniItem.Data.Frames.Count - 1 : frameEnd;

                if (frameStart > frameEnd)
                {
                    DisposeAnimationItem(aniItem);
                    return;
                }
            }
            else
            {
                DisposeAnimationItem(aniItem);
                return;
            }

            // png 하나의 딜레이 설정
            if (isPngFrameAni)
            {
                if (pngDelay == 0) return;
                aniItem.Data.Frames[0].Delay = pngDelay;
            }

            if ((speedX != 0 && goX != 0) || (speedY != 0 && goY != 0))
            {
                FrameAnimationData.ApplyMovement(this.GraphicsDevice, aniItem.Data, speedX, speedY, goX, goY, fullMove, frameStart, ref frameEnd);
            }
            var newAniItem = new FrameAnimator(FrameAnimationData.MergeAnimationData(baseAniItem.Data, aniItem.Data,
                    this.GraphicsDevice, delayOffset, moveX, moveY, frameStart, frameEnd));

            this.Items.Clear();
            this.Items.Add(newAniItem);

            if (this.AutoAdjustPosition)
            {
                this.AdjustPosition();
            }

            this.Invalidate();
        }

        public void AddHitboxOverlay()
        {
            FrameAnimator baseAniItem;
            if (this.Items.Count == 0)
            {
                var tmpFrame = new Frame(null, Point.Zero, 0, 0, true);
                var tmpFrameAnimationData = new FrameAnimationData();
                tmpFrameAnimationData.Frames.Add(tmpFrame);
                baseAniItem = new FrameAnimator(tmpFrameAnimationData);
            }
            else baseAniItem = (FrameAnimator)this.Items[0];

            FrameAnimator aniItem;

            var config = ImageHandlerConfig.Default;
            var baseDelayAll = 0;
            foreach (var frame in baseAniItem.Data.Frames)
            {
                baseDelayAll += frame.Delay;
            }

            var frmOverlayAniOptions = new FrmOverlayRectOptions(0, baseDelayAll, config);
            int startTime = 0;
            int endTime = 0;
            int radius = 0;
            int speedX = 0;
            int speedY = 0;
            int goX = 0;
            int goY = 0;
            var frameEnd = 0;
            Point lt;
            Point rb;
            Color bgColor = System.Drawing.Color.FromArgb(config.BackgroundType.Value == ImageBackgroundType.Transparent ? 0 : 255, config.BackgroundColor.Value).ToXnaColor();

            if (frmOverlayAniOptions.ShowDialog() == DialogResult.OK)
            {
                frmOverlayAniOptions.GetValues(out lt, out rb, out startTime, out endTime, out radius, out int alpha, out int type, out speedX, out speedY, out goX, out goY, config);
                Color fillColor = System.Drawing.Color.FromArgb((255 * alpha / 100), config.OverlayRectColor.Value).ToXnaColor();
                Color outlineColor = System.Drawing.Color.FromArgb(255, config.OverlayRectColor.Value).ToXnaColor();

                FrameAnimationData aniItemData = null;
                switch (type)
                {
                    case 0:
                        aniItemData = FrameAnimationData.CreateRectData(lt, rb, endTime - startTime, this.GraphicsDevice, fillColor, outlineColor);
                        break;
                    case 1:
                        aniItemData = FrameAnimationData.CreateCircleData(lt, radius, endTime - startTime, this.GraphicsDevice, fillColor, outlineColor);
                        break;
                    default:
                        break;
                }

                if (aniItemData == null) return;

                aniItem = new FrameAnimator(aniItemData);
            }
            else return;

            if ((speedX != 0 && goX != 0) || (speedY != 0 && goY != 0))
            {
                FrameAnimationData.ApplyMovement(this.GraphicsDevice, aniItem.Data, speedX, speedY, goX, goY, false, 0, ref frameEnd);
            }
            var newAniItem = new FrameAnimator(FrameAnimationData.MergeAnimationData(baseAniItem.Data, aniItem.Data,
                    this.GraphicsDevice, startTime, 0, 0, 0, frameEnd));

            this.Items.Clear();
            this.Items.Add(newAniItem);

            if (this.AutoAdjustPosition)
            {
                this.AdjustPosition();
            }

            this.Invalidate();
        }

        public void AdjustPosition()
        {
            if (this.Items.Count <= 0)
                return;

            var animator = this.Items[0];

            if (animator is FrameAnimator)
            {
                var aniItem = (FrameAnimator)animator;
                var rect = aniItem.Data.GetBound();
                aniItem.Position = new Point(-rect.Left, -rect.Top);
            }
            else if (animator is AnimationItem aniItem)
            {
                var rect = aniItem.Measure();
                aniItem.Position = new Point(-rect.Left, -rect.Top);
            }
            else if (animator is MultiFrameAnimator)
            {
                var multiAniItem = (MultiFrameAnimator)animator;
                var rect = multiAniItem.Data.GetBound(multiAniItem.SelectedAnimationName);
                multiAniItem.Position = new Point(-rect.Left, -rect.Top);
            }
        }

        public bool SaveAsGif(AnimationItem aniItem, string fileName, ImageHandlerConfig config, GifEncoder encoder, bool showOptions)
        {
            var rec = new AnimationRecoder(this.GraphicsDevice);
            var cap = encoder.Compatibility;

            rec.Items.Add(aniItem);
            int length = rec.GetMaxLength();
            int delay = Math.Max(cap.MinFrameDelay, config.MinDelay);
            int[] timeline = null;
            if (!cap.IsFixedFrameRate)
            {
                timeline = rec.GetGifTimeLine(delay, cap.MaxFrameDelay);
            }

            // calc available canvas area
            rec.ResetAll();
            Microsoft.Xna.Framework.Rectangle bounds = aniItem.Measure();
            if (length > 0)
            {
                IEnumerable<int> delays = timeline?.Take(timeline.Length - 1)
                    ?? Enumerable.Range(0, (int)Math.Ceiling(1.0 * length / delay) - 1);

                foreach (var frameDelay in delays)
                {
                    rec.Update(TimeSpan.FromMilliseconds(frameDelay));
                    var rect = aniItem.Measure();
                    bounds = Microsoft.Xna.Framework.Rectangle.Union(bounds, rect);
                }
            }
            bounds.Offset(aniItem.Position);

            // customize clip/scale options
            AnimationClipOptions clipOptions = new AnimationClipOptions()
            {
                StartTime = 0,
                StopTime = length,
                Left = bounds.Left,
                Top = bounds.Top,
                Right = bounds.Right,
                Bottom = bounds.Bottom,
                OutputWidth = bounds.Width,
                OutputHeight = bounds.Height,
            };

            if (showOptions)
            {
                var frmOptions = new FrmGifClipOptions()
                {
                    ClipOptions = clipOptions,
                    ClipOptionsNew = clipOptions,
                };
                if (frmOptions.ShowDialog() == DialogResult.OK)
                {
                    var clipOptionsNew = frmOptions.ClipOptionsNew;
                    clipOptions.StartTime = clipOptionsNew.StartTime ?? clipOptions.StartTime;
                    clipOptions.StopTime = clipOptionsNew.StopTime ?? clipOptions.StopTime;

                    clipOptions.Left = clipOptionsNew.Left ?? clipOptions.Left;
                    clipOptions.Top = clipOptionsNew.Top ?? clipOptions.Top;
                    clipOptions.Right = clipOptionsNew.Right ?? clipOptions.Right;
                    clipOptions.Bottom = clipOptionsNew.Bottom ?? clipOptions.Bottom;

                    clipOptions.OutputWidth = clipOptionsNew.OutputWidth ?? (clipOptions.Right - clipOptions.Left);
                    clipOptions.OutputHeight = clipOptionsNew.OutputHeight ?? (clipOptions.Bottom - clipOptions.Top);
                }
                else
                {
                    return false;
                }
            }

            // validate params
            bounds = new Rectangle(
                clipOptions.Left.Value,
                clipOptions.Top.Value,
                clipOptions.Right.Value - clipOptions.Left.Value,
                clipOptions.Bottom.Value - clipOptions.Top.Value
                );
            var targetSize = new Point(clipOptions.OutputWidth.Value, clipOptions.OutputHeight.Value);
            var startTime = clipOptions.StartTime.Value;
            var stopTime = clipOptions.StopTime.Value;

            if (bounds.Width <= 0 || bounds.Height <= 0
                || targetSize.X <= 0 || targetSize.Y <= 0
                || startTime < 0
                || stopTime - startTime <= 0)
            {
                return false;
            }
            length = stopTime - startTime;

            // create output dir
            string framesDirName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + ".frames");
            if (config.SavePngFramesEnabled && !Directory.Exists(framesDirName))
            {
                Directory.CreateDirectory(framesDirName);
            }

            // pre-render
            rec.ResetAll();
            switch (config.BackgroundType.Value)
            {
                default:
                case ImageBackgroundType.Transparent:
                    rec.BackgroundColor = Color.Transparent;
                    break;

                case ImageBackgroundType.Color:
                    rec.BackgroundColor = System.Drawing.Color.FromArgb(255, config.BackgroundColor.Value).ToXnaColor();
                    break;

                case ImageBackgroundType.Mosaic:
                    rec.BackgroundImage = MonogameUtils.CreateMosaic(GraphicsDevice,
                        config.MosaicInfo.Color0.ToXnaColor(),
                        config.MosaicInfo.Color1.ToXnaColor(),
                        Math.Max(1, config.MosaicInfo.BlockSize));
                    break;
            }

            // select encoder
            encoder.Init(fileName, targetSize.X, targetSize.Y);

            // pipeline functions
            IEnumerable<Tuple<byte[], int>> MergeFrames(IEnumerable<Tuple<byte[], int>> frames)
            {
                byte[] prevFrame = null;
                int prevDelay = 0;

                foreach (var frame in frames)
                {
                    byte[] currentFrame = frame.Item1;
                    int currentDelay = frame.Item2;

                    if (prevFrame == null)
                    {
                        prevFrame = currentFrame;
                        prevDelay = currentDelay;
                    }
                    else if (prevFrame.AsSpan().SequenceEqual(currentFrame.AsSpan()))
                    {
                        prevDelay += currentDelay;
                    }
                    else
                    {
                        yield return Tuple.Create(prevFrame, prevDelay);
                        prevFrame = currentFrame;
                        prevDelay = currentDelay;
                    }
                }

                if (prevFrame != null)
                {
                    yield return Tuple.Create(prevFrame, prevDelay);
                }
            }

            IEnumerable<int> RenderDelay()
            {
                int t = 0;
                while (t < length)
                {
                    int frameDelay = Math.Min(length - t, delay);
                    t += frameDelay;
                    yield return frameDelay;
                }
            }

            IEnumerable<int> ClipTimeline(int[] _timeline)
            {
                int t = 0;
                for (int i = 0; ; i = (i + 1) % timeline.Length)
                {
                    var frameDelay = timeline[i];
                    if (t < startTime)
                    {
                        if (t + frameDelay > startTime)
                        {
                            frameDelay = t + frameDelay - startTime;
                            t = startTime;
                        }
                        else
                        {
                            t += frameDelay;
                            continue;
                        }
                    }

                    if (t + frameDelay < stopTime)
                    {
                        yield return frameDelay;
                        t += frameDelay;
                    }
                    else
                    {
                        frameDelay = stopTime - t;
                        yield return frameDelay;
                        break;
                    }
                }
            }

            int prevTime = 0;
            async Task<int> ApplyFrame(byte[] frameData, int frameDelay)
            {
                byte[] gifData = null;
                if (cap.AlphaSupportMode != AlphaSupportMode.FullAlpha && config.BackgroundType.Value == ImageBackgroundType.Transparent)
                {
                    using (var rt2 = rec.GetGifTexture(config.BackgroundColor.Value.ToXnaColor(), config.MinMixedAlpha))
                    {
                        if (gifData == null)
                        {
                            gifData = new byte[frameData.Length];
                        }
                        rt2.GetData(gifData);
                    }
                }
                else
                {
                    gifData = frameData;
                }

                var tasks = new List<Task>();

                // save each frame as png
                if (config.SavePngFramesEnabled)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        string pngFileName = Path.Combine(framesDirName, $"{prevTime}_{prevTime + frameDelay}.png");
                        GCHandle gcHandle = GCHandle.Alloc(frameData, GCHandleType.Pinned);
                        try
                        {
                            using (var bmp = new System.Drawing.Bitmap(targetSize.X, targetSize.Y, targetSize.X * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, gcHandle.AddrOfPinnedObject()))
                            {
                                bmp.Save(pngFileName, System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }
                        finally
                        {
                            gcHandle.Free();
                        }
                    }));
                }

                // append frame data to gif stream
                tasks.Add(Task.Run(() =>
                {
                    // TODO: only for gif here?
                    frameDelay = Math.Max(10, (int)(Math.Round(frameDelay / 10.0) * 10));

                    GCHandle gcHandle = GCHandle.Alloc(frameData, GCHandleType.Pinned);
                    try
                    {
                        encoder.AppendFrame(gcHandle.AddrOfPinnedObject(), frameDelay);
                    }
                    finally
                    {
                        gcHandle.Free();
                    }
                }));

                await Task.WhenAll(tasks);
                prevTime += frameDelay;
                return prevTime;
            }

            async Task RenderJob(IProgressDialogContext context, CancellationToken cancellationToken)
            {
                bool isCompareAndMergeFrames = timeline == null && !cap.IsFixedFrameRate;

                // build pipeline
                IEnumerable<int> delayEnumerator = timeline == null ? RenderDelay() : ClipTimeline(timeline);
                var step1 = delayEnumerator.TakeWhile(_ => !cancellationToken.IsCancellationRequested);
                var frameRenderEnumerator = step1.Select(frameDelay =>
                {
                    rec.Draw();
                    rec.Update(TimeSpan.FromMilliseconds(frameDelay));
                    return frameDelay;
                });
                var step2 = frameRenderEnumerator.TakeWhile(_ => !cancellationToken.IsCancellationRequested);
                var getFrameData = step2.Select(frameDelay =>
                {
                    using (var t2d = rec.GetPngTexture())
                    {
                        byte[] frameData = new byte[t2d.Width * t2d.Height * 4];
                        t2d.GetData(frameData);
                        return Tuple.Create(frameData, frameDelay);
                    }
                });
                var step3 = getFrameData.TakeWhile(_ => !cancellationToken.IsCancellationRequested);
                if (isCompareAndMergeFrames)
                {
                    var mergedFrameData = MergeFrames(step3);
                    step3 = mergedFrameData.TakeWhile(_ => !cancellationToken.IsCancellationRequested);
                }

                var step4 = step3.Select(item => ApplyFrame(item.Item1, item.Item2));

                // run pipeline
                bool isPlaying = this.IsPlaying;
                try
                {
                    this.IsPlaying = false;
                    rec.Begin(bounds, targetSize);
                    if (startTime > 0)
                    {
                        rec.Update(TimeSpan.FromMilliseconds(startTime));
                    }
                    context.ProgressMin = 0;
                    context.ProgressMax = length;
                    foreach (var task in step4)
                    {
                        int currentTime = await task;
                        context.Progress = currentTime;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is AggregateException aggrEx && aggrEx.InnerExceptions.Count == 1)
                    {
                        context.Message = $"오류: {aggrEx.InnerExceptions[0].Message}";
                    }
                    else
                    {
                        context.Message = $"오류: {ex.Message}";
                    }
                    context.FullMessage = ex.ToString();
                    throw;
                }
                finally
                {
                    rec.End();
                    this.IsPlaying = isPlaying;
                }
            }

            var dialogResult = ProgressDialog.Show(this.FindForm(), "내보내는 중...", "애니메이션 저장하는 중...", true, false, RenderJob);
            return dialogResult == DialogResult.OK;
        }

        public override AnimationItem GetItemAt(int x, int y)
        {
            //固定获取当前显示的物件 无论鼠标在哪
            return this.Items.Count > 0 ? this.Items[0] : null;
        }

        protected override void Initialize()
        {
            base.Initialize();
            this.sprite = new SpriteBatchEx(this.GraphicsDevice);
        }

        protected override void Update(TimeSpan elapsed)
        {
            base.Update(elapsed);
        }

        protected override void Draw()
        {
            base.Draw();

            if (this.ShowInfo && this.XnaFont != null)
            {
                UpdateInfoText();
                sprite.Begin();
                sprite.DrawStringEx(this.XnaFont, this.sbInfo, Vector2.Zero, Color.Black);
                sprite.End();
            }
        }

        protected override void OnItemDragSave(AnimationItemEventArgs e)
        {
            var fileName = Path.GetTempFileName();

            if ((e.Item as FrameAnimator)?.Data.Frames.Count == 1)
            {
                using (var bmp = (e.Item as FrameAnimator).Data.Frames[0].Png.ExtractPng())
                {
                    bmp.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            else
            {
                //this.SaveAsGif(e.Item, fileName, ImageHandlerConfig.Default);
                // this is too lag so we don't support dragging gifs!
                return;
            }
            
            var imgObj = new ImageDataObject(null, fileName);
            this.DoDragDrop(imgObj, System.Windows.Forms.DragDropEffects.Copy);
            e.Handled = true;
        }

        private void UpdateInfoText()
        {
            this.sbInfo.Clear();
            if (ShowOverlayAni)
            {
                this.sbInfo.Append("애니메이션 중첩 중\n");
                this.Padding = new System.Windows.Forms.Padding(0, 28, 0, 0);
            }
            else
            {
                this.Padding = new System.Windows.Forms.Padding(0, 14, 0, 0);
            }
            if (this.Items.Count > 0)
            {
                var aniItem = this.Items[0];
                int time = 0;
                if (aniItem is FrameAnimator frameAni)
                {
                    time = frameAni.CurrentTime;
                }
                else if (aniItem is ISpineAnimator spineAni)
                {
                    time = spineAni.CurrentTime;
                }
                else if (aniItem is MultiFrameAnimator)
                {
                    time = ((MultiFrameAnimator)aniItem).CurrentTime;
                }
                this.sbInfo.AppendFormat("pos: {0}, scale: {1:p0}, play: {2} / {3}",
                    aniItem.Position,
                    base.GlobalScale,
                    aniItem.Length <= 0 ? 0 : (time % aniItem.Length),
                    aniItem.Length);
            }
        }

        private void DisposeAnimationItem(AnimationItem animationItem)
        {
            switch (animationItem)
            {
                case FrameAnimator frameAni:
                    if (frameAni.Data?.Frames != null)
                    {
                        foreach (var frame in frameAni.Data.Frames)
                        {
                            if (frame.Texture != null && !frame.Texture.IsDisposed)
                            {
                                frame.Texture.Dispose();
                            }
                        }
                    }
                    break;
                case SpineAnimatorV2 spineV2:
                    if (spineV2.Skeleton != null)
                    {
                        foreach(var slot in spineV2.Skeleton.Slots.Items)
                        {
                            var atlasRegion = (slot.Attachment switch
                            {
                                SpineV2.MeshAttachment mesh => mesh.RendererObject,
                                SpineV2.RegionAttachment region => region.RendererObject,
                                SpineV2.SkinnedMeshAttachment skinnedMesh => skinnedMesh.RendererObject,
                                _ => null
                            }) as SpineV2.AtlasRegion;
                            if (atlasRegion?.page?.rendererObject is Texture2D texture && !texture.IsDisposed)
                            {
                                texture.Dispose();
                            }
                        }
                    }
                    break;
                case SpineAnimatorV4 spineV4:
                    if (spineV4.Skeleton != null)
                    {
                        foreach (var slot in spineV4.Skeleton.Slots.Items)
                        {
                            var atlasRegion = (slot.Attachment switch
                            {
                                Spine.MeshAttachment mesh => mesh.Region,
                                Spine.RegionAttachment region => region.Region,
                                _ => null
                            }) as Spine.AtlasRegion;
                            if (atlasRegion?.page?.rendererObject is Texture2D texture && !texture.IsDisposed)
                            {
                                texture.Dispose();
                            }
                        }
                    }
                    break;
            }
        }

        public void ClearItemList()
        {
            if (this.Items.Count > 0)
            {
                var itemsCopy = new List<AnimationItem>(this.Items);
                this.Items.Clear();
                foreach (var aniItem in itemsCopy)
                {
                    this.DisposeAnimationItem(aniItem);
                }
            }
        }
    }
}
