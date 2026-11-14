using System.Collections.Generic;
using Microsoft.Xna.Framework;
using WzComparerR2.Animation;

namespace WzComparerR2.MapRender.Particles
{
    public class ParticleDesc1
    {
        public string Name { get; set; }
        public int MaxParticleCount { get; set; }
        public float BurstCount { get; set; }
        public float BurstRate { get; set; }
        public float EmitterLifeTime { get; set; }
        public float DesignedFps { get; set; }
        public bool Loop { get; set; }
        public bool Prewarm { get; set; }
        public float PrewarmTime { get; set; }
        public float PrewarmFps { get; set; }
        public float LongestLifeTime { get; set; }
        public ParticleProgramInfo UpdateProgram { get; set; }
        public List<ParticleModule> Initializers { get; } = new List<ParticleModule>();
        public List<ParticleModule> Updaters { get; } = new List<ParticleModule>();
        public List<ParticleMaterial> Materials { get; } = new List<ParticleMaterial>();
    }

    public sealed class ParticleDesc2 : ParticleDesc1
    {
        public float LifeTime { get; set; }
        public float MinimumDistance { get; set; }
        public float ReceiveRate { get; set; }
        public float Probability { get; set; }
        public bool DieWithSource { get; set; }
        public bool SizeAffectsLife { get; set; }
        public bool SizeAffectsWidth { get; set; }
        public bool StretchTexture { get; set; }
    }

    public sealed class ParticleModule
    {
        public string Name { get; set; }
        public Dictionary<string, ParticleParameter> Parameters { get; } = new Dictionary<string, ParticleParameter>();

        internal List<ParticleParameter> OrderedParameters { get; } = new List<ParticleParameter>();

        internal Vector4 Sample(string key, float time, float random, Vector4 fallback)
        {
            return this.Parameters.TryGetValue(key, out var value) ? value.Sample(time, random) : fallback;
        }
    }

    public sealed class ParticleMaterial
    {
        public Frame Texture { get; set; }
        public Vector2 Grid { get; set; } = Vector2.One;
        public Vector2 Offset { get; set; }
        public Vector2 UvScale { get; set; } = Vector2.One;
        public bool BlendEnabled { get; set; }
        public ParticleBlendFunc SourceBlend { get; set; }
        public ParticleBlendFunc DestinationBlend { get; set; }
        public bool PixelPerfect { get; set; }
        public ParticleProgramInfo OutputProgram { get; set; }
        public List<ParticleModule> Modules { get; } = new List<ParticleModule>();
    }
}
