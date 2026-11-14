using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender.Particles
{
    // Trail geometry is external to the provided output program; only history sampling is inferred.
    internal sealed class ParticleTrailHistory
    {
        internal ParticleComputeEmitter Source;
        internal readonly uint[] Connections;
        private readonly ParticleComputeEmitter target;
        private readonly ParticleDesc2 description;
        private readonly IRandom random;
        private readonly Dictionary<int, Strip> heads = new Dictionary<int, Strip>();
        private readonly List<Strip> strips = new List<Strip>();
        private uint serial;
        private double time, sampleTime, previousTargetTime;

        internal ParticleTrailHistory(ParticleComputeEmitter target, ParticleDesc2 description, IRandom random)
        {
            this.target = target;
            this.description = description;
            this.random = random;
            this.Connections = new uint[description.MaxParticleCount];
        }

        internal void Step(float dt)
        {
            if (this.target.Time < this.previousTargetTime)
            {
                this.strips.Clear();
                this.heads.Clear();
                this.time = this.sampleTime = 0;
            }
            this.time += dt;
            this.sampleTime += dt;
            float interval = this.description.ReceiveRate > 0 ? 1 / this.description.ReceiveRate : float.MaxValue;
            if (this.sampleTime >= interval)
            {
                this.sampleTime %= interval;
                this.SampleSource();
            }
            int count = 0;
            foreach (var strip in this.strips)
            {
                strip.Points.RemoveAll(point => this.time >= point.Death);
                count += strip.Points.Count;
            }
            while (count > this.description.MaxParticleCount)
            {
                Strip oldest = null;
                foreach (var strip in this.strips)
                {
                    if (strip.Points.Count > 0 && (oldest == null || strip.Points[0].Birth < oldest.Points[0].Birth))
                    {
                        oldest = strip;
                    }
                }
                oldest.Points.RemoveAt(0);
                count--;
            }
            var input = new byte[(this.description.MaxParticleCount + 1) * this.target.TrailInputStride];
            Array.Clear(this.Connections, 0, this.Connections.Length);
            int index = 0;
            foreach (var strip in this.strips)
            {
                float length = 0;
                for (int i = 1; i < strip.Points.Count; i++)
                {
                    length += Vector2.Distance(strip.Points[i - 1].Position, strip.Points[i].Position);
                }
                float distance = 0;
                for (int i = strip.Points.Count - 1; i >= 0; i--)
                {
                    var point = strip.Points[i];
                    if (i + 1 < strip.Points.Count)
                    {
                        distance += Vector2.Distance(point.Position, strip.Points[i + 1].Position);
                    }
                    int start = index * this.target.TrailInputStride;
                    int record = start;
                    float progress = length > 0 ? distance / length : 0;
                    if (this.target.TrailInputStride == 44)
                    {
                        ParticleComputeEmitter.Put(input, start, (float)point.Death);
                        ParticleComputeEmitter.Put(input, start + 4, (float)point.Birth);
                        ParticleComputeEmitter.Put(input, start + 8, 2u);
                        ParticleComputeEmitter.Put(input, start + 12, strip.Id);
                        record += 16;
                    }
                    ParticleComputeEmitter.Put(input, record, point.Position.X);
                    ParticleComputeEmitter.Put(input, record + 4, point.Position.Y);
                    ParticleComputeEmitter.Put(input, record + 8, point.Width);
                    ParticleComputeEmitter.Put(input, record + 12, Math.Min(progress, 0.999999f));
                    ParticleComputeEmitter.Put(input, record + 16, (float)((this.time - point.Birth) / Math.Max(0.000001, point.Death - point.Birth)));
                    ParticleComputeEmitter.Put(input, record + 20, this.description.StretchTexture ? progress : distance);
                    ParticleComputeEmitter.Put(input, record + 24, point.Color);
                    if (this.target.TrailInputStride == 32)
                    {
                        ParticleComputeEmitter.Put(input, record + 28, strip.Id);
                    }
                    this.Connections[index++] = strip.Id;
                }
            }
            this.strips.RemoveAll(strip => strip.Points.Count == 0 && !this.heads.ContainsValue(strip));
            this.target.SetTrailInput(input, dt);
            this.previousTargetTime = this.target.Time;
        }

        private void SampleSource()
        {
            if (this.Source == null || !this.target.Active)
            {
                if (this.description.DieWithSource)
                {
                    this.heads.Clear();
                    this.strips.Clear();
                }
                return;
            }
            var records = this.Source.DrawRecords(0);
            var endTimes = this.Source.SourceEndTimes();
            for (int i = 0; i < records.Length; i++)
            {
                var source = records[i];
                this.heads.TryGetValue(i, out var strip);
                if (source.Size == Vector2.Zero || endTimes[i] <= this.Source.Time)
                {
                    if (strip != null && this.description.DieWithSource)
                    {
                        strip.Points.Clear();
                    }
                    this.heads.Remove(i);
                    continue;
                }
                if (strip == null || strip.SourceEnd != endTimes[i])
                {
                    if (strip != null && this.description.DieWithSource)
                    {
                        strip.Points.Clear();
                    }
                    strip = new Strip { Id = ++this.serial, SourceEnd = endTimes[i], Accepted = this.random.NextPercent(this.description.Probability) };
                    this.heads[i] = strip;
                    this.strips.Add(strip);
                }
                if (!strip.Accepted)
                {
                    continue;
                }
                var position = this.Source.LocalSpace ? Vector2.Transform(source.Position, this.Source.Transform) : source.Position;
                if (strip.Points.Count > 0 && Vector2.DistanceSquared(position, strip.Points[strip.Points.Count - 1].Position) < this.description.MinimumDistance * this.description.MinimumDistance)
                {
                    continue;
                }
                float width = this.description.SizeAffectsWidth ? Math.Abs(source.Size.X) : 1;
                float life = this.description.LifeTime * (this.description.SizeAffectsLife ? Math.Abs(source.Size.X) : 1);
                strip.Points.Add(new Point { Position = position, Width = width, Color = source.Color, Birth = this.time, Death = this.time + life });
            }
        }

        private sealed class Strip
        {
            internal uint Id;
            internal float SourceEnd;
            internal bool Accepted;
            internal readonly List<Point> Points = new List<Point>();
        }

        private struct Point
        {
            internal Vector2 Position;
            internal float Width;
            internal uint Color;
            internal double Birth, Death;
        }
    }
}
