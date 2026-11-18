using System;
using Microsoft.Xna.Framework;
using WzComparerR2.Animation;

namespace WzComparerR2.MapRender.Patches2
{
    public class MobProjectileItem : SceneItem
    {
        public MobProjectileItem(Definition definition, BehaviorController owner)
        {
            this.definition = definition;
            this.FlipX = owner.FlipX;
            this.start = owner.CurPos + new Vector2(this.FlipX ? -definition.StartPoint.X : definition.StartPoint.X, definition.StartPoint.Y);
            this.Position = this.start;
            this.Index = owner.Owner.Index;
            this.Animator = new RepeatableFrameAnimator(definition.Animation) { IsLoop = true };
        }

        private const double FadeTime = 0.3;
        private readonly Definition definition;
        private readonly Vector2 start;
        private double distance;

        public RepeatableFrameAnimator Animator { get; }
        public Vector2 Position { get; private set; }
        public bool FlipX { get; }
        public bool IsExpired => this.distance >= this.definition.Range;
        public int Alpha
        {
            get
            {
                double fadeDistance = this.definition.Speed * FadeTime;
                double opacity = (this.definition.Range - this.distance) / fadeDistance;
                return (int)(255 * Math.Max(0, Math.Min(1, opacity)));
            }
        }

        public void Update(TimeSpan elapsed)
        {
            this.distance = Math.Min(this.definition.Range, this.distance + this.definition.Speed * elapsed.TotalSeconds);
            this.Position = this.start + new Vector2((float)this.distance * (this.FlipX ? 1 : -1), 0);
            this.Animator.Update(elapsed);
        }

        public class Definition
        {
            public RepeatableFrameAnimationData Animation { get; set; }
            public Vector2 StartPoint { get; set; }
            public int AttackAfter { get; set; }
            public int Range { get; set; }
            public int Speed { get; set; }
        }
    }
}
