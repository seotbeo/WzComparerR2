using System.Collections.Generic;

namespace WzComparerR2.MapRender.Particles
{
    public sealed class ParticleDesc3
    {
        public ParticleTrack Transform { get; set; }
        public List<ParticleTrack> Tracks { get; } = new List<ParticleTrack>();
    }

    public sealed class ParticleTrack
    {
        public int Index { get; set; }
        public int LeaderIndex { get; set; } = -1;
        public ParticleParameter Position { get; set; }
        public ParticleParameter Scale { get; set; }
        public ParticleParameter Rotation { get; set; }
        public ParticleParameter Delay { get; set; }
        public ParticleParameter Duration { get; set; }
        public bool Loop { get; set; }
        public bool LocalSpace { get; set; }
        public bool AnimateProperties { get; set; }
        public object Item { get; set; }
    }
}
