using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender.Particles
{
    internal sealed class ParticleComputeParameters
    {
        internal const int Resolution = 1024;
        // These orders are the compiled cm[] ABI, not the WZ property's insertion order.
        private static readonly Dictionary<string, string[]> ModuleParameters = new Dictionary<string, string[]>
        {
            { "shape_rectangle_easy", new[] { "rect", "thickness", "direction", "rotation", "randomize_direction", "randomize_position" } },
            { "shape_rectangle", new[] { "rect", "thickness", "direction", "speed", "rotation", "randomize_direction", "randomize_position" } },
            { "shape_circle_easy", new[] { "radius", "radius_thickness", "start_angle", "rotation", "arc", "mode", "spread", "loop_speed", "randomize_direction", "spherize_direction", "randomize_position", "scale" } },
            { "shape_circle", new[] { "radius", "radius_thickness", "start_angle", "rotation", "arc", "speed", "mode", "spread", "loop_speed", "randomize_direction", "spherize_direction", "randomize_position", "scale" } },
            { "force_over_noise", new[] { "fieldsize", "strength", "speed_limit", "scroll_speed", "frequency", "distance", "circle_radius" } },
            { "force_over_lifetime", new[] { "linear_force", "orbital_force", "radial_force" } },
            { "texture_sheet_animation", new[] { "frame_over_time", "cycles" } },
            { "curl_noise", new[] { "fieldsize", "strength", "scroll_speed", "frequency" } },
            { "start_velocity", new[] { "init_speed", "angle" } },
            { "shape_edge", new[] { "direction", "radius", "mode", "spread", "loop_speed", "randomize_direction", "spherize_direction", "randomize_position" } },
            { "shape_edge_easy", new[] { "rotation", "radius", "mode", "spread", "loop_speed", "randomize_direction", "spherize_direction", "randomize_position" } },
            { "position_over_noise", new[] { "fieldsize", "strength", "scroll_speed", "frequency" } },
            { "spring_force", new[] { "center", "center_world_bias", "strength", "damping" } },
            { "radial_velocity", new[] { "offset", "speed" } },
            { "start_from_texture", new[] { "grid_size", "grid_scale", "offset" } },
            { "shape_rectangle_texture", new[] { "rect", "direction", "speed", "rotation", "randomize_direction", "randomize_position", "sampling_texture" } },
            { "maple_color_over_lifetime", new[] { "r", "g", "b", "a" } },
            { "limit_velocity_over_lifetime_advanced", new[] { "speed_limit", "dampen", "dampen_per_second", "drag", "multiply_by_scale", "multiply_by_velocity" } },
            // CS5 divides by group_count; the remaining two parameters are multiplied together.
            { "texture_sheet_group_animation", new[] { "group_count", "frame_over_time", "cycles" } },
            { "shape_concentric_circle", new[] { "radius", "number_of_circle", "radius_thickness", "start_angle", "rotation", "arc", "speed", "mode", "spread", "loop_speed", "randomize_direction", "spherize_direction", "randomize_position", "scale" } }
        };
        internal readonly List<Vector4> Constants = new List<Vector4>();
        internal readonly List<Vector4[]> Curves = new List<Vector4[]>();
        internal int CurveRows => Math.Max(1, this.Curves.Count);

        internal ParticleComputeParameters(ParticleDesc1 description)
        {
            var references = new List<int>();
            this.Add(description.Initializers, references);
            this.Add(description.Updaters, references);
            foreach (var material in description.Materials)
            {
                this.Add(material.Modules, references);
            }
            foreach (int reference in references)
            {
                var value = this.Constants[reference];
                value.Y = (value.Y + 0.5f) / this.CurveRows;
                value.Z = (value.Z + 0.5f) / this.CurveRows;
                this.Constants[reference] = value;
            }
            int required = description.UpdateProgram?.ParameterSlots ?? 0;
            foreach (var material in description.Materials)
            {
                required = Math.Max(required, material.OutputProgram?.ParameterSlots ?? 0);
            }
            if (required > this.Constants.Count)
            {
                System.Diagnostics.Trace.TraceWarning("Particle '{0}': padding missing shader parameters from {1} to {2} float4 slots.", description.Name, this.Constants.Count, required);
            }
            while (this.Constants.Count < Math.Max(1, required))
            {
                this.Constants.Add(Vector4.Zero);
            }
            if (this.Constants.Count > 4096)
            {
                throw new NotSupportedException("Particle parameter buffer exceeds the D3D11 constant-buffer limit.");
            }
        }

        private static IEnumerable<ParticleParameter> ParametersFor(ParticleModule module)
        {
            if (!ModuleParameters.TryGetValue(module.Name, out var order))
            {
                foreach (var parameter in module.OrderedParameters)
                {
                    yield return parameter;
                }
                yield break;
            }
            if (module.Parameters.Count != order.Length)
            {
                throw new NotSupportedException("Unexpected particle parameter count for " + module.Name);
            }
            foreach (string name in order)
            {
                if (!module.Parameters.TryGetValue(name, out var parameter))
                {
                    throw new NotSupportedException("Missing particle parameter: " + module.Name + "/" + name);
                }
                yield return parameter;
            }
        }

        private void Add(List<ParticleModule> modules, List<int> references)
        {
            foreach (var module in modules)
            {
                foreach (var parameter in ParametersFor(module))
                {
                    if (parameter.Mode == 4 && parameter.Components == 0)
                    {
                        continue;
                    }
                    int mode = parameter.Mode == 6 ? 0 : parameter.Mode;
                    if (((mode < 0 || mode > 3) && mode != 7) || parameter.Components < 1 || parameter.Components > 4)
                    {
                        throw new NotSupportedException("Unsupported particle parameter format: " + module.Name);
                    }
                    var meta = new Vector4(mode, 0, 0, 0);
                    if (mode == 2 || mode == 3)
                    {
                        meta.Y = this.AddCurve(parameter, 0);
                        meta.Z = mode == 3 ? this.AddCurve(parameter, 1) : meta.Y;
                        references.Add(this.Constants.Count);
                    }
                    else if (parameter.Components == 1)
                    {
                        meta.Y = parameter.Lower.X;
                        meta.Z = parameter.Upper.X;
                    }
                    this.Constants.Add(meta);
                    if (parameter.Components == 2)
                    {
                        this.Constants.Add(new Vector4(parameter.Lower.X, parameter.Lower.Y, parameter.Upper.X, parameter.Upper.Y));
                    }
                    else if (parameter.Components > 2)
                    {
                        this.Constants.Add(parameter.Lower);
                        this.Constants.Add(parameter.Upper);
                    }
                }
            }
        }

        private int AddCurve(ParticleParameter parameter, int set)
        {
            int index = this.Curves.Count;
            var scale = parameter.GetCurveMultiplier(set);
            var samples = new Vector4[Resolution];
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = parameter.SampleCurve(set, i / (float)(Resolution - 1)) * scale;
            }
            this.Curves.Add(samples);
            return index;
        }
    }
}
