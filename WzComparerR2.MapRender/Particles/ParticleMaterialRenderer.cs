using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.MapRender.Particles
{
    internal sealed class ParticleMaterialRenderer : IDisposable
    {
        private readonly GraphicsDevice graphics;
        private readonly Dictionary<int, BlendState> blends = new Dictionary<int, BlendState>();
        private Texture2D white;

        internal ParticleMaterialRenderer(GraphicsDevice graphics)
        {
            this.graphics = graphics;
        }

        internal void Draw(ModularParticleEffect effect, SpriteBatch sprite, Vector2 position, Matrix camera)
        {
            foreach (var emitter in effect.Emitters)
            {
                for (int materialIndex = 0; materialIndex < emitter.Description.Materials.Count; materialIndex++)
                {
                    var material = emitter.Description.Materials[materialIndex];
                    var records = emitter.DrawRecords(materialIndex);
                    var transform = (emitter.LocalSpace && !emitter.IsTrail ? emitter.Transform : Matrix.Identity) * Matrix.CreateTranslation(position.X, position.Y, 0) * camera;
                    sprite.Begin(SpriteSortMode.Deferred, this.BlendFor(material), material.PixelPerfect ? SamplerState.PointClamp : SamplerState.LinearClamp,
                        DepthStencilState.None, RasterizerState.CullNone, transformMatrix: transform);
                    try
                    {
                        if (effect.Trails.TryGetValue(emitter, out var trail))
                        {
                            this.DrawTrail(sprite, records, trail.Connections, material);
                        }
                        else
                        {
                            this.DrawSprites(sprite, records, material);
                        }
                    }
                    finally
                    {
                        sprite.End();
                    }
                }
            }
        }

        private void DrawSprites(SpriteBatch sprite, ParticleDrawRecord[] records, ParticleMaterial material)
        {
            var frame = material.Texture;
            if (frame?.Texture == null)
            {
                return;
            }
            var atlas = frame.AtlasRect ?? new Rectangle(0, 0, frame.Texture.Width, frame.Texture.Height);
            int columns = Math.Max(1, (int)material.Grid.X), rows = Math.Max(1, (int)material.Grid.Y);
            int width = atlas.Width / columns, height = atlas.Height / rows;
            if (width <= 0 || height <= 0)
            {
                return;
            }
            var origin = frame.Origin.ToVector2() / new Vector2(columns, rows);
            foreach (var particle in records)
            {
                if (particle.Size == Vector2.Zero || !Finite(particle.Position) || !Finite(particle.Size) || float.IsNaN(particle.Rotation) || float.IsInfinity(particle.Rotation))
                {
                    continue;
                }
                // CS5 outputs animation cycles; the fractional part selects a cell in the sheet.
                int frameCount = columns * rows;
                float progress = float.IsNaN(particle.Frame) || float.IsInfinity(particle.Frame) ? 0 : Math.Max(0, particle.Frame);
                int index = Math.Min(frameCount - 1, (int)((progress - Math.Floor(progress)) * frameCount));
                var source = new Rectangle(atlas.X + index % columns * width, atlas.Y + index / columns * height, width, height);
                var position = particle.Position + material.Offset;
                if (material.PixelPerfect)
                {
                    position = new Vector2((float)Math.Round(position.X), (float)Math.Round(position.Y));
                }
                // CS5 scale has no texture aspect information. Use cell height as the shared reference axis.
                var scale = particle.Size / height;
                sprite.Draw(frame.Texture, position, source, new Color { PackedValue = particle.Color }, particle.Rotation,
                    origin, scale, SpriteEffects.None, 0);
            }
        }

        private void DrawTrail(SpriteBatch sprite, ParticleDrawRecord[] records, uint[] connections, ParticleMaterial material)
        {
            var texture = material.Texture?.Texture;
            var source = material.Texture?.AtlasRect;
            if (texture == null)
            {
                if (this.white == null)
                {
                    this.white = new Texture2D(this.graphics, 1, 1);
                    this.white.SetData(new[] { Color.White });
                }
                texture = this.white;
                source = null;
            }
            var rect = source ?? new Rectangle(0, 0, texture.Width, texture.Height);
            for (int i = 1; i < records.Length; i++)
            {
                if (connections[i] == 0 || connections[i] != connections[i - 1])
                {
                    continue;
                }
                var a = records[i - 1];
                var b = records[i];
                if (!Finite(a.Position) || !Finite(b.Position) || !Finite(a.Size) || !Finite(b.Size))
                {
                    continue;
                }
                var delta = b.Position - a.Position;
                float length = delta.Length();
                float width = Math.Abs(a.Size.X + b.Size.X) * 0.5f;
                if (length <= 0 || width <= 0 || rect.Width <= 0 || rect.Height <= 0)
                {
                    continue;
                }
                var color = Color.Lerp(new Color { PackedValue = a.Color }, new Color { PackedValue = b.Color }, 0.5f);
                sprite.Draw(texture, a.Position + material.Offset, source, color, (float)Math.Atan2(delta.Y, delta.X),
                    new Vector2(0, rect.Height * 0.5f), new Vector2(length / rect.Width, width / rect.Height), SpriteEffects.None, 0);
            }
        }

        private static bool Finite(Vector2 value)
        {
            return !float.IsNaN(value.X) && !float.IsNaN(value.Y) && !float.IsInfinity(value.X) && !float.IsInfinity(value.Y);
        }

        private BlendState BlendFor(ParticleMaterial material)
        {
            int key = material.BlendEnabled ? ((int)material.SourceBlend << 8) | (int)material.DestinationBlend : 0;
            if (!this.blends.TryGetValue(key, out var blend))
            {
                blend = new BlendState
                {
                    ColorSourceBlend = material.BlendEnabled ? ConvertBlend(material.SourceBlend, false) : Blend.One,
                    ColorDestinationBlend = material.BlendEnabled ? ConvertBlend(material.DestinationBlend, false) : Blend.Zero,
                    AlphaSourceBlend = material.BlendEnabled ? ConvertBlend(material.SourceBlend, true) : Blend.One,
                    AlphaDestinationBlend = material.BlendEnabled ? ConvertBlend(material.DestinationBlend, true) : Blend.Zero,
                    ColorBlendFunction = BlendFunction.Add, AlphaBlendFunction = BlendFunction.Add
                };
                this.blends.Add(key, blend);
            }
            return blend;
        }

        private static Blend ConvertBlend(ParticleBlendFunc value, bool alpha)
        {
            switch (value)
            {
                case ParticleBlendFunc.ZERO: return Blend.Zero;
                case ParticleBlendFunc.ONE: return Blend.One;
                case ParticleBlendFunc.SRC_COLOR: return alpha ? Blend.SourceAlpha : Blend.SourceColor;
                case ParticleBlendFunc.INV_SRC_COLOR: return alpha ? Blend.InverseSourceAlpha : Blend.InverseSourceColor;
                case ParticleBlendFunc.SRC_ALPHA: return Blend.SourceAlpha;
                case ParticleBlendFunc.INV_SRC_ALPHA: return Blend.InverseSourceAlpha;
                case ParticleBlendFunc.DEST_ALPHA: return Blend.DestinationAlpha;
                case ParticleBlendFunc.INV_DEST_ALPHA: return Blend.InverseDestinationAlpha;
                case ParticleBlendFunc.DEST_COLOR: return alpha ? Blend.DestinationAlpha : Blend.DestinationColor;
                case ParticleBlendFunc.INV_DEST_COLOR: return alpha ? Blend.InverseDestinationAlpha : Blend.InverseDestinationColor;
                default: throw new NotSupportedException("Unknown particle blend factor: " + value);
            }
        }

        public void Dispose()
        {
            foreach (var blend in this.blends.Values)
            {
                blend.Dispose();
            }
            this.blends.Clear();
            this.white?.Dispose();
        }
    }
}
