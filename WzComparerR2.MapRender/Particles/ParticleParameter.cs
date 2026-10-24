using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using WzComparerR2.WzLib;

namespace WzComparerR2.MapRender.Particles
{
    public sealed class ParticleParameter
    {
        public int Components { get; private set; }
        public int Mode { get; private set; }
        private Vector4 lower, upper;
        private readonly List<CurveSegment>[][] curves = { new List<CurveSegment>[4], new List<CurveSegment>[4] };
        private float salt;
        internal Vector4 Lower => this.lower;
        internal Vector4 Upper => this.upper;
        private Vector4 curveMultiplier = Vector4.One;
        internal Vector4 SampleCurve(int set, float time) => this.Curve(set, time, set == 0 ? this.lower : this.upper);

        internal Vector4 GetCurveMultiplier(int set)
        {
            var result = new[] { this.curveMultiplier.X, this.curveMultiplier.Y, this.curveMultiplier.Z, this.curveMultiplier.W };
            for (int channel = 0; channel < 4; channel++)
            {
                if (channel >= this.Components || this.curves[set][channel] == null || this.curves[set][channel].Count == 0)
                {
                    result[channel] = 1f;
                }
            }
            return new Vector4(result[0], result[1], result[2], result[3]);
        }

        internal static float Number(Wz_Node node, float fallback = 0)
        {
            return node?.Value == null ? fallback : Convert.ToSingle(node.Value);
        }

        private static float ReadCurveMultiplier(Wz_Node node)
        {
            float value = Number(node, 1f);
            return value == 0f ? 1f : value;
        }

        internal static Vector4 Vector(Wz_Node node)
        {
            return new Vector4(Number(node?.Nodes["x"]), Number(node?.Nodes["y"]), Number(node?.Nodes["z"]), Number(node?.Nodes["w"]));
        }

        internal static ParticleParameter Read(Wz_Node node)
        {
            if (node == null)
            {
                return null;
            }
            var parameter = new ParticleParameter
            {
                Components = node.Nodes["element_count"].GetValueEx(1), Mode = node.Nodes["variable_type"].GetValueEx(0),
                lower = Vector(node.Nodes["fvalue0"]), upper = Vector(node.Nodes["fvalue1"]),
                curveMultiplier = new Vector4(ReadCurveMultiplier(node.Nodes["fvalue0"]?.Nodes["x"]), ReadCurveMultiplier(node.Nodes["fvalue0"]?.Nodes["y"]),
                    ReadCurveMultiplier(node.Nodes["fvalue0"]?.Nodes["z"]), ReadCurveMultiplier(node.Nodes["fvalue0"]?.Nodes["w"]))
            };
            foreach (char ch in node.Text)
            {
                parameter.salt = (parameter.salt * 17 + ch) % 8192;
            }
            for (int set = 0; set < 2; set++)
            {
                var curve = node.Nodes["cvalue" + set] ?? node.Nodes["curve_value" + set];
                for (int channel = 0; channel < 4; channel++)
                {
                    var source = curve?.Nodes[channel.ToString()];
                    if (source == null)
                    {
                        continue;
                    }
                    var segments = new List<CurveSegment>();
                    foreach (var part in source.Nodes)
                    {
                        segments.Add(new CurveSegment
                        {
                            Begin = Number(part.Nodes["begin"]), End = Number(part.Nodes["end"]),
                            A = Number(part.Nodes["p0"]), B = Number(part.Nodes["p1"]), C = Number(part.Nodes["p2"]), D = Number(part.Nodes["p3"])
                        });
                    }
                    segments.Sort((a, b) => a.Begin != b.Begin ? a.Begin.CompareTo(b.Begin) : a.End.CompareTo(b.End));
                    parameter.curves[set][channel] = segments;
                }
            }
            return parameter;
        }

        public Vector4 Sample(float time, float random)
        {
            int mode = this.Mode == 6 ? 0 : this.Mode;
            var a = (mode == 2 || mode == 3) ? this.Curve(0, time, this.lower) : this.lower;
            if (mode == 0 || mode == 2)
            {
                return a;
            }
            var b = mode == 3 ? this.Curve(1, time, this.upper) : this.upper;
            float sharedMix = Hash(random + this.salt);
            var mix = mode == 7 ? new Vector4(sharedMix) : new Vector4(sharedMix, Hash(random + this.salt + 1), Hash(random + this.salt + 2), Hash(random + this.salt + 3));
            return a + (b - a) * mix;
        }

        private Vector4 Curve(int set, float time, Vector4 fallback)
        {
            var result = new[] { fallback.X, fallback.Y, fallback.Z, fallback.W };
            for (int channel = 0; channel < Math.Min(this.Components, 4); channel++)
            {
                var segments = this.curves[set][channel];
                if (segments == null || segments.Count == 0)
                {
                    continue;
                }
                result[channel] = segments[segments.Count - 1].D;
                foreach (var segment in segments)
                {
                    if (time < segment.Begin)
                    {
                        result[channel] = segment.A;
                        break;
                    }
                    if (time <= segment.End && segment.End > segment.Begin)
                    {
                        float t = MathHelper.Clamp((time - segment.Begin) / (segment.End - segment.Begin), 0, 1);
                        float s = 1 - t;
                        result[channel] = s * s * s * segment.A + 3 * s * s * t * segment.B + 3 * s * t * t * segment.C + t * t * t * segment.D;
                        break;
                    }
                }
            }
            return new Vector4(result[0], result[1], result[2], result[3]);
        }

        internal static float Hash(float seed)
        {
            double value = Math.Sin(seed * 12.9898 + 78.233) * 43758.5453;
            return (float)(value - Math.Floor(value));
        }

        private struct CurveSegment
        {
            internal float Begin, End, A, B, C, D;
        }
    }
}
