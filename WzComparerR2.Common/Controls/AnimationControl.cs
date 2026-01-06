using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.Animation;
using WzComparerR2.Rendering;

namespace WzComparerR2.Controls
{
    public partial class AnimationControl : GraphicsDeviceControl
    {
        public AnimationControl()
        {
            InitializeComponent();
            this.MouseDown += AnimationControl_MouseDown;
            this.MouseUp += AnimationControl_MouseUp;
            this.MouseMove += AnimationControl_MouseMove;
            this.MouseWheel += AnimationControl_MouseWheel;

            this.Items = new List<AnimationItem>();
            this.ItemTimes = new List<Tuple<int, int>>();
            this.MouseDragEnabled = true;
            this.GlobalScale = 1f;

            this.timer = new Timer();
            timer.Interval = 30;
            timer.Tick += Timer_Tick;
            timer.Enabled = true;
            this.sw = Stopwatch.StartNew();
        }

        public List<AnimationItem> Items { get; private set; }
        public List<Tuple<int, int>> ItemTimes { get; private set; }
        public bool MouseDragEnabled { get; set; }
        public bool MouseDragSaveEnabled { get; set; }
        public bool ShowPositionGridOnDrag { get; set; }
        public bool ShowOverlayAni { get; set; } = false;

        public float GlobalScale
        {
            get { return this.globalScale; }
            set { this.globalScale = MathHelper.Clamp(value, 0.1f, 10f); }
        }

        public bool IsPlaying
        {
            get { return this.timer.Enabled; }
            set
            {
                if (value)
                {
                    this.lastUpdateTime = TimeSpan.Zero;
                    this.sw.Restart();
                }
                this.timer.Enabled = value;
            }
        }

        public bool IsPaused
        {
            get { return this.paused; }
        }

        public int FrameInterval
        {
            get { return this.timer.Interval; }
            set { this.timer.Interval = value; }
        }

        public int MaxLength
        {
            get { return this.maxLength; }
            set { this.maxLength = value; }
        }

        public int CurrentTime
        {
            get { return (int)(this.lastUpdateTime + this.timeOffset).TotalMilliseconds; }
        }

        private float globalScale;
        private Timer timer;
        private Stopwatch sw;
        private TimeSpan lastUpdateTime;
        private TimeSpan timeOffset;
        private bool paused;
        private int maxLength;

        private SpriteBatchEx sprite;
        private AnimationGraphics graphics;

        //拖拽相关
        private MouseDragContext mouseDragContext;

        //离屏绘制相关

        protected override void Initialize()
        {
            sprite = new SpriteBatchEx(this.GraphicsDevice);
            graphics = new AnimationGraphics(this.GraphicsDevice, sprite);
        }

        protected virtual void Update(TimeSpan elapsed)
        {
            /*
            foreach (var animation in this.Items)
            {
                if (animation != null)
                {
                    animation.Update(elapsed);
                }
            }
            */
            var curTime = (int)(this.lastUpdateTime + this.timeOffset).TotalMilliseconds;
            var margin = this.timer.Interval; // 부자연스러운 전환 완화
            if (curTime > maxLength - margin)
            {
                ResetTimer();
                ResetAll();
            }
            else
            {
                var playingItems = GetPlayingAni(curTime);
                foreach (var animation in playingItems)
                {
                    if (animation.Item1 != null)
                    {
                        animation.Item1.Update(elapsed);
                    }
                }
            }
        }

        public virtual void DrawBackground()
        {
            this.GraphicsDevice.Clear(this.BackColor.ToXnaColor());
        }

        protected override void Draw()
        {
            //绘制背景色
            this.DrawBackground();
            //绘制场景
            Matrix mtViewport = Matrix.CreateTranslation(this.Padding.Left, this.Padding.Top, 0);
            Matrix mtAnimation = Matrix.CreateScale(GlobalScale, GlobalScale, 1) * mtViewport;

            var curTime = (int)(this.lastUpdateTime + this.timeOffset).TotalMilliseconds;
            var playingItems = GetPlayingAni(curTime);
            foreach (var animation in playingItems)
            {
                if (animation != null)
                {
                    if (animation.Item1 is FrameAnimator frameAni)
                    {
                        graphics.Draw(frameAni, mtAnimation);
                    }
                    else if (animation.Item1 is ISpineAnimator spineAni)
                    {
                        graphics.Draw(spineAni, mtAnimation);
                    }
                    else if (animation.Item1 is MultiFrameAnimator)
                    {
                        graphics.Draw((MultiFrameAnimator)animation.Item1, mtAnimation);
                    }
                }
            }

            //绘制辅助内容
            if (ShowPositionGridOnDrag && this.mouseDragContext.IsDragging && this.mouseDragContext.DraggingItem != null)
            {
                var pos = this.mouseDragContext.DraggingItem.Position;
                this.sprite.Begin(transformMatrix: mtViewport);
                this.sprite.DrawLine(new Point(0, pos.Y), new Point(this.Width, pos.Y), 1, Color.Indigo);
                this.sprite.DrawLine(new Point(pos.X, 0), new Point(pos.X, this.Height), 1, Color.Indigo);
                this.sprite.End();
            }
        }

        public virtual AnimationItem GetItemAt(int x, int y)
        {
            for(int i = this.Items.Count - 1; i >= 0; i--)
            {
                var item = this.Items[i];
                var bound = item.Measure();
                var rect = new Rectangle(
                    (int)Math.Round(item.Position.X + bound.X* this.GlobalScale),
                    (int)Math.Round(item.Position.Y + bound.Y * this.GlobalScale),
                    (int)Math.Round(bound.Width * this.GlobalScale),
                    (int)Math.Round(bound.Height * this.GlobalScale));
                if (rect.Contains(x, y))
                {
                    return item;
                }
            }
            return null;
        }

        public virtual IEnumerable<AnimationItem> GetItemsAt(int x, int y)
        {
            for (int i = this.Items.Count - 1; i >= 0; i--)
            {
                var item = this.Items[i];
                var bound = item.Measure();
                var rect = new Rectangle(
                    (int)Math.Round(item.Position.X + bound.X * this.GlobalScale),
                    (int)Math.Round(item.Position.Y + bound.Y * this.GlobalScale),
                    (int)Math.Round(bound.Width * this.GlobalScale),
                    (int)Math.Round(bound.Height * this.GlobalScale));
                if (rect.Contains(x, y))
                {
                    yield return item;
                }
            }
            yield return null;
        }

        public void ResetTimer()
        {
            this.timeOffset = TimeSpan.Zero;
            this.lastUpdateTime = TimeSpan.Zero;
            if (this.IsPaused) this.sw.Reset();
            else this.sw.Restart();
        }

        public void ResetAll()
        {
            foreach (var aniItem in this.Items)
            {
                aniItem.Reset();
            }
        }

        public void UpdateMaxLength()
        {
            this.MaxLength = this.ItemTimes.Select(item => item.Item2).DefaultIfEmpty(0).Max();
        }

        private IEnumerable<Tuple<AnimationItem, int>> GetPlayingAni(int curTime)
        {
            for (int i = 0; i < this.Items.Count; i++)
            {
                if (this.MaxLength == 0 || (curTime >= this.ItemTimes[i].Item1 && curTime < this.ItemTimes[i].Item2))
                {
                    yield return new Tuple<AnimationItem, int>(this.Items[i], i);
                }
            }
        }

        public void ClearItemList()
        {
            this.Items.Clear();
            this.ItemTimes.Clear();
            UpdateMaxLength();
            ResetTimer();
        }

        public void RemoveTopItem()
        {
            this.Items.RemoveAt(this.Items.Count - 1);
            this.ItemTimes.RemoveAt(this.ItemTimes.Count - 1);
            UpdateMaxLength();
        }

        public void AddItem(AnimationItem item, int start = 0, int end = 0)
        {
            this.Items.Add(item);
            this.ItemTimes.Add(new Tuple<int, int>(0 + start, Math.Max(item.Length, end) + start));
            UpdateMaxLength();
            ResetTimer();
            ResetAll();
        }

        protected void Pause()
        {
            this.sw.Stop();
            this.timer.Enabled = false;
            this.paused = true;
        }

        protected void Resume()
        {
            this.sw.Start();
            this.timer.Enabled = true;
            this.paused = false;
        }

        protected void UpdateTimeOffset(int ms)
        {
            if (!this.IsPaused || !this.Visible)
            {
                return;
            }

            var round = (10 - this.CurrentTime % 10) % 10;
            var offset = TimeSpan.FromMilliseconds(ms + round) + this.lastUpdateTime + this.timeOffset;

            offset = TimeSpan.FromMilliseconds(Math.Max(0, Math.Min((this.MaxLength - this.timer.Interval) / 10 * 10, offset.TotalMilliseconds)));
            this.ResetTimer();
            this.ResetAll();
            this.timeOffset = offset;

            this.Update(offset);
            this.Invalidate();
        }

        #region EVENTS
        protected virtual void OnItemDragSave(AnimationItemEventArgs e)
        {

        }


        private void AnimationControl_MouseDown(object sender, MouseEventArgs e)
        {
            this.Focus();

            if (this.MouseDragEnabled && e.Button == MouseButtons.Left)
            {
                //var item = GetItemAt(e.X, e.Y);
                var items = GetItemsAt(e.X, e.Y);
                foreach (var item in items)
                if (item != null)
                {
                    this.mouseDragContext.IsDragging = true;
                    this.mouseDragContext.MouseDownPoint = new Point(e.X, e.Y);
                    this.mouseDragContext.DraggingItem = item;
                    this.mouseDragContext.StartPosition = item.Position;
                }
                this.mouseDragContext.DraggingItems = items;
            }
            if ((Control.ModifierKeys & Keys.Control) != 0 && e.Button == MouseButtons.Middle)
            {
                this.GlobalScale = 1f;
            }
            if (this.IsPaused && this.Visible)
            {
                this.Invalidate();
            }
        }

        private void AnimationControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.MouseDragEnabled && e.Button == MouseButtons.Left)
            {
                this.mouseDragContext.IsDragging = false;
            }
            if (this.IsPaused && this.Visible)
            {
                this.Invalidate();
            }
        }

        private void AnimationControl_MouseMove(object sender, MouseEventArgs e)
        {
            /*
            if (this.MouseDragEnabled && this.mouseDragContext.IsDragging && this.mouseDragContext.DraggingItem != null)
            {
                this.mouseDragContext.DraggingItem.Position = new Point(
                    e.X - mouseDragContext.MouseDownPoint.X + mouseDragContext.StartPosition.X,
                    e.Y - mouseDragContext.MouseDownPoint.Y + mouseDragContext.StartPosition.Y);

                //处理拖拽保存
                if (this.MouseDragSaveEnabled && (Control.ModifierKeys & Keys.Control) != 0)
                {
                    var dragSize = SystemInformation.DragSize;
                    var dragBox = new Rectangle(mouseDragContext.MouseDownPoint, new Point(dragSize.Width, dragSize.Height));
                    if (!dragBox.Contains(new Point(e.X, e.Y)))
                    {
                        var e2 = new AnimationItemEventArgs(this.mouseDragContext.DraggingItem);
                        this.OnItemDragSave(e2);
                        if (e2.Handled)
                        {
                            this.mouseDragContext.IsDragging = false;
                        }
                    }
                }
            }
            */
            foreach (var draggingItem in this.mouseDragContext.DraggingItems ?? Enumerable.Empty<AnimationItem>())
            {
                if (this.MouseDragEnabled && this.mouseDragContext.IsDragging && draggingItem != null)
                {
                    draggingItem.Position = new Point(
                        e.X - mouseDragContext.MouseDownPoint.X + mouseDragContext.StartPosition.X,
                        e.Y - mouseDragContext.MouseDownPoint.Y + mouseDragContext.StartPosition.Y);

                    //处理拖拽保存
                    if (this.MouseDragSaveEnabled && (Control.ModifierKeys & Keys.Control) != 0)
                    {
                        var dragSize = SystemInformation.DragSize;
                        var dragBox = new Rectangle(mouseDragContext.MouseDownPoint, new Point(dragSize.Width, dragSize.Height));
                        if (!dragBox.Contains(new Point(e.X, e.Y)))
                        {
                            var e2 = new AnimationItemEventArgs(draggingItem);
                            this.OnItemDragSave(e2);
                            if (e2.Handled)
                            {
                                this.mouseDragContext.IsDragging = false;
                            }
                        }
                    }
                }
            }

            if (this.mouseDragContext.IsDragging && this.IsPaused && this.Visible)
            {
                this.Invalidate();
            }
        }

        private void AnimationControl_MouseWheel(object sender, MouseEventArgs e)
        {
            const int WHEEL_DELTA = 120;
            if ((Control.ModifierKeys & Keys.Control) != 0)
            {
                float wheelTicks = e.Delta / WHEEL_DELTA;
                float oldScale = this.GlobalScale;
                float newScale = oldScale * (1 + 0.1f * wheelTicks);
                if (oldScale.CompareTo(1f) * newScale.CompareTo(1f) == -1) // scaling cross 100%
                {
                    newScale = 1f;
                }
                this.GlobalScale = newScale;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var curTime = sw.Elapsed;
            var elapsed = curTime - lastUpdateTime;
            lastUpdateTime = curTime;

            if (this.Visible)
            {
                this.Update(elapsed);
                this.Invalidate();
            }
        }
        #endregion

        private struct MouseDragContext
        {
            public bool IsDragging;
            public Point MouseDownPoint;
            public Point StartPosition;
            public AnimationItem DraggingItem;
            public IEnumerable<AnimationItem> DraggingItems;
        }
    }
}
