using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.MapRender.Particles
{
    internal sealed class ModularParticleEffect : IDisposable
    {
        internal readonly List<ParticleComputeEmitter> Emitters = new List<ParticleComputeEmitter>();
        internal readonly Dictionary<ParticleComputeEmitter, ParticleTrailHistory> Trails = new Dictionary<ParticleComputeEmitter, ParticleTrailHistory>();
        private readonly Node root;
        private double remaining;

        internal ModularParticleEffect(object description, GraphicsDevice graphics, IRandom random)
        {
            try
            {
                this.root = new Node(description, graphics, random, this);
                this.root.ResolveLeaders(this);
                foreach (var trail in this.Trails)
                {
                    if (trail.Value.Source == null)
                    {
                        Trace.TraceWarning("Trail has no source emitter in its group: {0}", trail.Key.Description.Name);
                    }
                }
            }
            catch
            {
                this.Dispose();
                throw;
            }
        }

        internal void Update(TimeSpan elapsed)
        {
            const float step = 1f / 60;
            this.remaining += Math.Max(0, elapsed.TotalSeconds);
            for (int i = 0; this.remaining >= step && i < 600; i++)
            {
                this.root.Step(step, true, Matrix.Identity, false);
                foreach (var trail in this.Trails.Values)
                {
                    trail.Step(step);
                }
                this.remaining -= step;
            }
        }

        public void Dispose()
        {
            foreach (var emitter in this.Emitters)
            {
                emitter.Dispose();
            }
            this.Emitters.Clear();
            this.Trails.Clear();
        }

        private sealed class Node
        {
            private readonly ParticleTrack track;
            private readonly ParticleComputeEmitter emitter;
            private readonly List<Node> children = new List<Node>();
            private readonly float seed;
            private double time;
            private long cycle;
            private bool warmed;

            internal Node(object description, GraphicsDevice graphics, IRandom random, ModularParticleEffect owner)
            {
                this.seed = random.NextVar(0.5f, 0.5f);
                if (description is ParticleDesc1 desc)
                {
                    this.emitter = new ParticleComputeEmitter(desc, graphics, random);
                    owner.Emitters.Add(this.emitter);
                    if (desc is ParticleDesc2 trail)
                    {
                        owner.Trails.Add(this.emitter, new ParticleTrailHistory(this.emitter, trail, random));
                    }
                }
                else if (description is ParticleDesc3 group)
                {
                    this.track = group.Transform;
                    foreach (var child in group.Tracks)
                    {
                        this.children.Add(new Node(child, graphics, random, owner));
                    }
                }
                else if (description is ParticleTrack item)
                {
                    this.track = item;
                    this.children.Add(new Node(item.Item, graphics, random, owner));
                }
                else if (description != null)
                {
                    Trace.TraceWarning("Unsupported nested particle description: {0}", description.GetType().Name);
                }
            }

            private ParticleComputeEmitter FirstEmitter()
            {
                if (this.emitter != null)
                {
                    return this.emitter;
                }
                foreach (var child in this.children)
                {
                    var result = child.FirstEmitter();
                    if (result != null)
                    {
                        return result;
                    }
                }
                return null;
            }

            internal void ResolveLeaders(ModularParticleEffect owner)
            {
                foreach (var child in this.children)
                {
                    if (child.track?.LeaderIndex >= 0)
                    {
                        var target = child.FirstEmitter();
                        var sibling = this.children.Find(item => item.track?.Index == child.track.LeaderIndex);
                        var source = sibling?.FirstEmitter();
                        if (target != null && owner.Trails.TryGetValue(target, out var trail))
                        {
                            if (source != null && !source.IsTrail)
                            {
                                trail.Source = source;
                            }
                            else
                            {
                                Trace.TraceWarning("Unresolved type-2 trail leader: {0}, index {1}", target.Description.Name, child.track.LeaderIndex);
                            }
                        }
                    }
                    child.ResolveLeaders(owner);
                }
            }

            internal void Step(float dt, bool active, Matrix transform, bool local)
            {
                if (this.track != null)
                {
                    if (active)
                    {
                        this.time += dt;
                    }
                    float delay = Math.Max(0, this.track.Delay?.Sample(0, this.seed).X ?? 0);
                    float duration = Math.Max(0, this.track.Duration?.Sample(0, this.seed).X ?? 0);
                    double phase = this.time - delay;
                    active &= phase >= 0 && (duration <= 0 || this.track.Loop || phase < duration);
                    if (active && duration > 0 && this.track.Loop)
                    {
                        long cycle = (long)(phase / duration);
                        if (this.cycle != cycle)
                        {
                            this.cycle = cycle;
                            foreach (var child in this.children)
                            {
                                child.Reset();
                            }
                        }
                        phase %= duration;
                    }
                    float t = this.track.AnimateProperties && duration > 0 ? MathHelper.Clamp((float)phase / duration, 0, 1) : 0;
                    var position = this.track.Position?.Sample(t, this.seed) ?? Vector4.Zero;
                    var scale = this.track.Scale?.Sample(t, this.seed) ?? Vector4.One;
                    float rotation = this.track.Rotation?.Sample(t, this.seed).X ?? 0;
                    transform = Matrix.CreateScale(scale.X, scale.Y, 1) * Matrix.CreateRotationZ(rotation) * Matrix.CreateTranslation(position.X, position.Y, 0) * transform;
                    local |= this.track.LocalSpace;
                }
                if (this.emitter != null)
                {
                    this.emitter.Transform = transform;
                    this.emitter.LocalSpace = local;
                    if (active && !this.warmed)
                    {
                        this.warmed = true;
                        var desc = this.emitter.Description;
                        if (desc.Prewarm && !this.emitter.IsTrail)
                        {
                            float duration = desc.PrewarmTime > 0 ? desc.PrewarmTime : desc.LongestLifeTime;
                            float fps = MathHelper.Clamp(desc.PrewarmFps > 0 ? desc.PrewarmFps : desc.DesignedFps, 1, 240);
                            for (float left = Math.Min(Math.Max(0, duration), 120); left > 0;)
                            {
                                float warmStep = Math.Min(left, 1 / fps);
                                this.emitter.Step(warmStep, true);
                                left -= warmStep;
                            }
                        }
                    }
                    this.emitter.Step(dt, active);
                }
                foreach (var child in this.children)
                {
                    child.Step(dt, active, transform, local);
                }
            }

            private void Reset()
            {
                this.time = 0;
                this.cycle = 0;
                this.warmed = false;
                this.emitter?.Reset();
                foreach (var child in this.children)
                {
                    child.Reset();
                }
            }
        }
    }
}
