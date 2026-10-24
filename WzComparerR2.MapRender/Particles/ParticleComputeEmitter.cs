using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using WzComparerR2.Rendering;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Sampler = SharpDX.Direct3D11.SamplerState;
using NativeTexture = SharpDX.Direct3D11.Texture2D;

namespace WzComparerR2.MapRender.Particles
{
    internal struct ParticleDrawRecord
    {
        internal Vector2 Position, Size;
        internal float Rotation, Frame;
        internal uint Color, Id;
    }

    // Original CS5 instructions execute on the shared D3D11 device. Readback only bridges to SpriteBatch.
    internal sealed class ParticleComputeEmitter : IDisposable
    {
        internal readonly ParticleDesc1 Description;
        internal Matrix Transform = Matrix.Identity;
        internal bool LocalSpace;
        internal bool IsTrail => this.Description is ParticleDesc2;
        internal bool Active;
        internal double Time { get; private set; }
        private readonly Device device;
        private readonly DeviceContext context;
        private readonly List<IDisposable> owned = new List<IDisposable>();
        private readonly List<Output> outputs = new List<Output>();
        private readonly Buffer life, simulation, state, world, settings, parameters;
        private readonly UnorderedAccessView lifeUav, simUav, stateUav;
        private readonly ShaderResourceView lifeSrv, simSrv, curves, externalTexture;
        private readonly Sampler sampler;
        private readonly ComputeShader update;
        private readonly int lifeStride, simStride;
        private readonly bool modern;
        private readonly float seed;
        private readonly int capacity;
        private Buffer lifeReadback;
        private byte[] lifeReadbackBytes;
        private float[] sourceEndTimes;
        private double pendingBirths, nextBurstTime;
        private uint nextId;
        private bool disposed, started;
        private long revision;

        internal ParticleComputeEmitter(ParticleDesc1 description, GraphicsDevice graphics, IRandom random)
        {
            this.Description = description;
            this.device = graphics._d3dDevice();
            this.context = graphics._d3dContext();
            this.seed = random.NextVar(10000f, 10000f);
            this.capacity = checked(description.MaxParticleCount + 1);
            if ((int)this.device.FeatureLevel < 0xb000)
            {
                throw new NotSupportedException("Particle CS5 needs Direct3D feature level 11.0.");
            }
            try
            {
                if (this.IsTrail)
                {
                    var program = description.Materials[0].OutputProgram ?? throw new NotSupportedException("Missing trail output_cs5_bin: " + description.Name);
                    this.lifeStride = Stride(program, "t0");
                    this.simStride = 4;
                    this.modern = this.lifeStride == 44;
                }
                else
                {
                    var program = description.UpdateProgram ?? throw new NotSupportedException("Missing update_cs5_bin: " + description.Name);
                    this.lifeStride = Stride(program, "u0");
                    this.simStride = Stride(program, "u1");
                    this.modern = this.lifeStride == 44;
                    this.update = this.Own(new ComputeShader(this.device, program.Bytecode));
                }
                if (this.lifeStride != 8 && this.lifeStride != 32 && this.lifeStride != 44)
                {
                    throw new NotSupportedException("Unknown particle life/input layout: " + description.Name);
                }
                var packed = new ParticleComputeParameters(description);
                this.world = this.MakeBuffer(new byte[64], BindFlags.ConstantBuffer);
                this.settings = this.MakeBuffer(new byte[128], BindFlags.ConstantBuffer);
                this.parameters = this.MakeBuffer(Encode(packed.Constants.ToArray()), BindFlags.ConstantBuffer);
                this.life = this.MakeBuffer(this.InitialLife(), BindFlags.ShaderResource | BindFlags.UnorderedAccess, this.lifeStride);
                this.simulation = this.MakeBuffer(new byte[checked(this.capacity * this.simStride)], BindFlags.ShaderResource | BindFlags.UnorderedAccess, this.simStride);
                this.state = this.MakeBuffer(new byte[8], BindFlags.UnorderedAccess, 8);
                this.lifeUav = this.Own(new UnorderedAccessView(this.device, this.life));
                this.simUav = this.Own(new UnorderedAccessView(this.device, this.simulation));
                this.stateUav = this.Own(new UnorderedAccessView(this.device, this.state));
                this.lifeSrv = this.Own(new ShaderResourceView(this.device, this.life));
                this.simSrv = this.Own(new ShaderResourceView(this.device, this.simulation));
                var curveValues = new Vector4[ParticleComputeParameters.Resolution * packed.CurveRows];
                for (int i = 0; i < packed.Curves.Count; i++)
                {
                    Array.Copy(packed.Curves[i], 0, curveValues, i * ParticleComputeParameters.Resolution, ParticleComputeParameters.Resolution);
                }
                var curveBytes = Encode(curveValues);
                var handle = GCHandle.Alloc(curveBytes, GCHandleType.Pinned);
                try
                {
                    var texture = this.Own(new NativeTexture(this.device, new Texture2DDescription
                    {
                        Width = ParticleComputeParameters.Resolution, Height = packed.CurveRows, MipLevels = 1, ArraySize = 1,
                        Format = Format.R32G32B32A32_Float, SampleDescription = new SampleDescription(1, 0),
                        Usage = ResourceUsage.Immutable, BindFlags = BindFlags.ShaderResource
                    }, new SharpDX.DataRectangle(handle.AddrOfPinnedObject(), ParticleComputeParameters.Resolution * 16)));
                    this.curves = this.Own(new ShaderResourceView(this.device, texture));
                }
                finally
                {
                    handle.Free();
                }
                // Actor-provided blackboard textures are absent from map WZ dumps. Use a declared fallback.
                var whiteBytes = new byte[] { 255, 255, 255, 255 };
                var whiteHandle = GCHandle.Alloc(whiteBytes, GCHandleType.Pinned);
                try
                {
                    var texture = this.Own(new NativeTexture(this.device, new Texture2DDescription
                    {
                        Width = 1, Height = 1, MipLevels = 1, ArraySize = 1, Format = Format.R8G8B8A8_UNorm,
                        SampleDescription = new SampleDescription(1, 0), Usage = ResourceUsage.Immutable, BindFlags = BindFlags.ShaderResource
                    }, new SharpDX.DataRectangle(whiteHandle.AddrOfPinnedObject(), 4)));
                    this.externalTexture = this.Own(new ShaderResourceView(this.device, texture));
                }
                finally
                {
                    whiteHandle.Free();
                }
                if (description.UpdateProgram?.Assembly.Contains(" t5") == true)
                {
                    System.Diagnostics.Trace.TraceWarning("Particle '{0}': actor sampling texture is unavailable; t5 uses opaque white.", description.Name);
                }
                this.sampler = this.Own(new Sampler(this.device, new SamplerStateDescription
                {
                    Filter = Filter.MinMagMipLinear, AddressU = SharpDX.Direct3D11.TextureAddressMode.Clamp,
                    AddressV = SharpDX.Direct3D11.TextureAddressMode.Clamp, AddressW = SharpDX.Direct3D11.TextureAddressMode.Clamp,
                    MaximumLod = float.MaxValue, MaximumAnisotropy = 1, ComparisonFunction = Comparison.Never
                }));
                foreach (var material in description.Materials)
                {
                    var program = material.OutputProgram ?? throw new NotSupportedException("Missing output_cs5_bin: " + description.Name);
                    int stride = Stride(program, "u0");
                    if (stride != 32 && stride != 44)
                    {
                        throw new NotSupportedException("Unknown particle output record: " + stride);
                    }
                    int inputStride = Stride(program, this.modern || this.IsTrail ? "t0" : "t1");
                    if (inputStride != (this.modern || this.IsTrail ? this.lifeStride : this.simStride))
                    {
                        throw new NotSupportedException("Particle update/output layouts disagree: " + description.Name);
                    }
                    var buffer = this.MakeBuffer(new byte[checked(this.capacity * stride)], BindFlags.UnorderedAccess, stride);
                    this.outputs.Add(new Output
                    {
                        Program = program, Shader = this.Own(new ComputeShader(this.device, program.Bytecode)), Buffer = buffer,
                        Uav = this.Own(new UnorderedAccessView(this.device, buffer)), Stride = stride,
                        Readback = this.CreateReadback(this.capacity * stride),
                        ReadbackBytes = new byte[this.capacity * stride],
                        Records = new ParticleDrawRecord[description.MaxParticleCount]
                    });
                }
            }
            catch
            {
                this.Dispose();
                throw;
            }
        }

        private static int Stride(ParticleProgramInfo program, string binding)
        {
            if (program.Profile != "cs_5_0" || program.ThreadsPerGroup <= 0 || !program.StructuredBuffers.TryGetValue(binding, out int stride))
            {
                throw new NotSupportedException("Unsupported CS5 resource binding: " + binding);
            }
            return stride;
        }

        private byte[] InitialLife()
        {
            var bytes = new byte[checked(this.capacity * this.lifeStride)];
            if (!this.IsTrail && !this.modern)
            {
                for (int i = 0; i < this.Description.MaxParticleCount; i++)
                {
                    float birth = this.Description.BurstRate > 0 && this.Description.BurstCount > 0
                        ? -(float)(Math.Floor(i / this.Description.BurstCount) / this.Description.BurstRate) : -float.MaxValue;
                    Put(bytes, i * this.lifeStride, birth);
                }
            }
            return bytes;
        }

        internal void Reset()
        {
            this.Time = this.pendingBirths = this.nextBurstTime = 0;
            this.started = false;
            this.Upload(this.life, this.InitialLife());
            this.Upload(this.simulation, new byte[this.capacity * this.simStride]);
            this.Upload(this.state, new byte[8]);
            this.revision++;
        }

        internal void Step(float dt, bool active)
        {
            this.Active = active;
            if (this.IsTrail || dt <= 0 || (!this.started && !active))
            {
                return;
            }
            this.started |= active;
            this.Time += dt;
            if (this.modern)
            {
                this.ScheduleBirths(dt, active);
            }
            this.BindConstants(dt, active);
            using (new ComputeScope(this.context))
            {
                this.Bind();
                this.context.ComputeShader.Set(this.update);
                this.context.ComputeShader.SetUnorderedAccessViews(0, this.lifeUav, this.simUav, this.stateUav);
                this.Dispatch(this.Description.UpdateProgram);
            }
            this.revision++;
        }

        private void ScheduleBirths(float dt, bool active)
        {
            bool emitting = active && (this.Description.Loop || this.Description.EmitterLifeTime <= 0 || this.Time <= this.Description.EmitterLifeTime);
            if (!emitting)
            {
                return;
            }
            if (this.Description.BurstRate <= 0 || this.Description.BurstCount <= 0)
            {
                return;
            }
            if (this.Time >= this.nextBurstTime)
            {
                double bursts = Math.Floor((this.Time - this.nextBurstTime) * this.Description.BurstRate) + 1;
                this.pendingBirths += bursts * this.Description.BurstCount;
                this.nextBurstTime += bursts / this.Description.BurstRate;
            }
            int births = (int)Math.Min(this.Description.MaxParticleCount, Math.Floor(this.pendingBirths));
            this.pendingBirths -= Math.Floor(this.pendingBirths);
            if (births <= 0)
            {
                return;
            }
            this.EnsureLifeReadback();
            var bytes = this.lifeReadbackBytes;
            this.Read(this.life, this.lifeReadback, bytes);
            for (int i = 0; i < this.Description.MaxParticleCount && births > 0; i++)
            {
                int start = i * this.lifeStride;
                if (BitConverter.ToUInt32(bytes, start + 8) != 0)
                {
                    continue;
                }
                Array.Clear(bytes, start, this.lifeStride);
                Put(bytes, start + 4, (float)this.Time);
                Put(bytes, start + 8, 1u);
                Put(bytes, start + 12, ++this.nextId);
                births--;
            }
            this.Upload(this.life, bytes);
        }

        internal void SetTrailInput(byte[] records, float dt)
        {
            this.Time += dt;
            this.Upload(this.life, records);
            this.BindConstants(dt, this.Active);
            this.revision++;
        }

        internal int TrailInputStride => this.lifeStride;

        // The output stage owns this array; the next revision overwrites its contents.
        internal ParticleDrawRecord[] DrawRecords(int index)
        {
            var stage = this.outputs[index];
            if (stage.Revision == this.revision && stage.Records != null)
            {
                return stage.Records;
            }
            using (new ComputeScope(this.context))
            {
                this.Bind();
                this.context.ComputeShader.Set(stage.Shader);
                this.context.ComputeShader.SetShaderResources(0, this.lifeSrv, this.simSrv);
                this.context.ComputeShader.SetUnorderedAccessView(0, stage.Uav);
                this.Dispatch(stage.Program);
            }
            var data = stage.ReadbackBytes;
            var records = stage.Records;
            this.Read(stage.Buffer, stage.Readback, data);
            for (int i = 0; i < records.Length; i++)
            {
                int start = i * stage.Stride;
                int position = start + (stage.Stride == 44 ? 16 : 0);
                records[i] = new ParticleDrawRecord
                {
                    Position = new Vector2(BitConverter.ToSingle(data, position), BitConverter.ToSingle(data, position + 4)),
                    Size = new Vector2(BitConverter.ToSingle(data, position + 8), BitConverter.ToSingle(data, position + 12)),
                    Rotation = BitConverter.ToSingle(data, position + 16), Frame = BitConverter.ToSingle(data, position + 20),
                    Color = BitConverter.ToUInt32(data, position + 24),
                    Id = BitConverter.ToUInt32(data, stage.Stride == 44 ? start + 12 : start + 28)
                };
            }
            stage.Revision = this.revision;
            return records;
        }

        // Callers retaining history must copy values rather than retain this array.
        internal float[] SourceEndTimes()
        {
            this.EnsureLifeReadback();
            if (this.sourceEndTimes == null)
            {
                this.sourceEndTimes = new float[this.Description.MaxParticleCount];
            }
            var bytes = this.lifeReadbackBytes;
            var times = this.sourceEndTimes;
            this.Read(this.life, this.lifeReadback, bytes);
            for (int i = 0; i < times.Length; i++)
            {
                times[i] = BitConverter.ToSingle(bytes, i * this.lifeStride);
            }
            return times;
        }

        private void Dispatch(ParticleProgramInfo program)
        {
            int count = this.Description.MaxParticleCount;
            if (count > 0)
            {
                this.context.Dispatch((count + program.ThreadsPerGroup - 1) / program.ThreadsPerGroup, 1, 1);
            }
        }

        private void BindConstants(float dt, bool active)
        {
            var m = this.LocalSpace ? Matrix.Identity : this.Transform;
            var worldData = new float[16];
            worldData[0] = m.M11; worldData[1] = m.M21; worldData[3] = m.M41;
            worldData[4] = m.M12; worldData[5] = m.M22; worldData[7] = m.M42;
            worldData[10] = worldData[15] = 1;
            this.Upload(this.world, Encode(worldData));
            var settingsData = new float[32];
            settingsData[2] = (float)this.Time; settingsData[3] = dt;
            settingsData[4] = BitConverter.ToSingle(BitConverter.GetBytes(this.Description.MaxParticleCount), 0);
            settingsData[5] = this.Description.EmitterLifeTime > 0 ? this.Description.EmitterLifeTime : float.MaxValue;
            settingsData[6] = this.Description.BurstRate; settingsData[7] = this.Description.BurstCount;
            settingsData[8] = this.Description.Loop ? 1 : 0; settingsData[9] = this.seed; settingsData[10] = active ? 1 : 0;
            this.Upload(this.settings, Encode(settingsData));
        }

        private void Bind()
        {
            this.context.ComputeShader.SetConstantBuffers(0, this.world, this.settings, this.parameters);
            this.context.ComputeShader.SetShaderResource(4, this.curves);
            this.context.ComputeShader.SetSampler(4, this.sampler);
            this.context.ComputeShader.SetShaderResource(5, this.externalTexture);
            this.context.ComputeShader.SetSampler(5, this.sampler);
        }

        internal static void Put(byte[] bytes, int offset, float value) => System.Buffer.BlockCopy(BitConverter.GetBytes(value), 0, bytes, offset, 4);
        internal static void Put(byte[] bytes, int offset, uint value) => System.Buffer.BlockCopy(BitConverter.GetBytes(value), 0, bytes, offset, 4);

        private static byte[] Encode<T>(T[] values) where T : struct
        {
            var bytes = new byte[checked(values.Length * Marshal.SizeOf(typeof(T)))];
            var handle = GCHandle.Alloc(values, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(handle.AddrOfPinnedObject(), bytes, 0, bytes.Length);
            }
            finally
            {
                handle.Free();
            }
            return bytes;
        }

        private void Upload(Buffer buffer, byte[] bytes)
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                this.context.UpdateSubresource(new SharpDX.DataBox(handle.AddrOfPinnedObject(), 0, 0), buffer, 0);
            }
            finally
            {
                handle.Free();
            }
        }

        private Buffer MakeBuffer(byte[] bytes, BindFlags flags, int stride = 0)
        {
            var buffer = this.Own(new Buffer(this.device, new BufferDescription
            {
                SizeInBytes = bytes.Length, Usage = ResourceUsage.Default, BindFlags = flags, StructureByteStride = stride,
                OptionFlags = stride == 0 ? ResourceOptionFlags.None : ResourceOptionFlags.BufferStructured
            }));
            this.Upload(buffer, bytes);
            return buffer;
        }

        private Buffer CreateReadback(int size) => this.Own(new Buffer(this.device, new BufferDescription { SizeInBytes = size, Usage = ResourceUsage.Staging, CpuAccessFlags = CpuAccessFlags.Read }));

        private void EnsureLifeReadback()
        {
            if (this.lifeReadback == null)
            {
                this.lifeReadback = this.CreateReadback(this.capacity * this.lifeStride);
                this.lifeReadbackBytes = new byte[this.capacity * this.lifeStride];
            }
        }

        private void Read(Buffer source, Buffer staging, byte[] destination)
        {
            this.context.CopyResource(source, staging);
            var mapped = this.context.MapSubresource(staging, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
            try
            {
                Marshal.Copy(mapped.DataPointer, destination, 0, destination.Length);
            }
            finally
            {
                this.context.UnmapSubresource(staging, 0);
            }
        }

        private T Own<T>(T resource) where T : IDisposable
        {
            this.owned.Add(resource);
            return resource;
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }
            this.disposed = true;
            for (int i = this.owned.Count - 1; i >= 0; i--)
            {
                this.owned[i].Dispose();
            }
            this.owned.Clear();
            this.outputs.Clear();
            this.lifeReadbackBytes = null;
            this.sourceEndTimes = null;
        }

        private sealed class Output
        {
            internal ParticleProgramInfo Program;
            internal ComputeShader Shader;
            internal Buffer Buffer, Readback;
            internal UnorderedAccessView Uav;
            internal int Stride;
            internal long Revision = -1;
            internal byte[] ReadbackBytes;
            internal ParticleDrawRecord[] Records;
        }

        private sealed class ComputeScope : IDisposable
        {
            private readonly DeviceContext context;
            private readonly ComputeShader shader;
            private readonly Buffer[] buffers;
            private readonly ShaderResourceView[] views;
            private readonly UnorderedAccessView[] outputs;
            private readonly Sampler[] samplers;
            internal ComputeScope(DeviceContext context)
            {
                this.context = context;
                this.shader = context.ComputeShader.Get();
                this.buffers = context.ComputeShader.GetConstantBuffers(0, 3);
                this.views = context.ComputeShader.GetShaderResources(0, 8);
                this.outputs = context.ComputeShader.GetUnorderedAccessViews(0, 8);
                this.samplers = context.ComputeShader.GetSamplers(0, 8);
                context.ComputeShader.SetShaderResources(0, new ShaderResourceView[8]);
                context.ComputeShader.SetUnorderedAccessViews(0, new UnorderedAccessView[8]);
            }
            public void Dispose()
            {
                var stage = this.context.ComputeShader;
                stage.SetShaderResources(0, new ShaderResourceView[8]);
                stage.SetUnorderedAccessViews(0, new UnorderedAccessView[8]);
                stage.Set(this.shader); stage.SetConstantBuffers(0, this.buffers);
                stage.SetShaderResources(0, this.views); stage.SetUnorderedAccessViews(0, this.outputs); stage.SetSamplers(0, this.samplers);
                this.shader?.Dispose();
                foreach (var item in this.buffers) { item?.Dispose(); }
                foreach (var item in this.views) { item?.Dispose(); }
                foreach (var item in this.outputs) { item?.Dispose(); }
                foreach (var item in this.samplers) { item?.Dispose(); }
            }
        }
    }
}
