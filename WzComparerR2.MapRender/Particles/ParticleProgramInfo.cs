using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.WzLib;

namespace WzComparerR2.MapRender.Particles
{
    // Decode the resource ABI before passing the complete original bytecode to Direct3D11.
    public sealed class ParticleProgramInfo
    {
        internal byte[] Bytecode { get; private set; }
        internal string Assembly { get; private set; }
        public string Profile { get; private set; }
        public int ParameterSlots { get; private set; }
        public int ThreadsPerGroup { get; private set; }
        public Dictionary<string, int> StructuredBuffers { get; } = new Dictionary<string, int>();

        [DllImport("d3dcompiler_47.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int D3DDisassemble(byte[] bytes, UIntPtr length, uint flags, IntPtr comment, out IntPtr result);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate IntPtr GetPointer(IntPtr self);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate UIntPtr GetSize(IntPtr self);

        internal static ParticleProgramInfo Decode(Wz_Node node)
        {
            if (node?.Value == null)
            {
                return null;
            }
            byte[] data = node.Value is byte[] bytes ? bytes : Convert.FromBase64String(Convert.ToString(node.Value));
            if (data.Length < 32 || Encoding.ASCII.GetString(data, 0, 4) != "DXBC")
            {
                Trace.TraceWarning("Invalid DXBC particle program: {0}", node.FullPathToFile);
                return null;
            }
            IntPtr blob = IntPtr.Zero;
            try
            {
                Marshal.ThrowExceptionForHR(D3DDisassemble(data, (UIntPtr)data.Length, 0, IntPtr.Zero, out blob));
                var table = Marshal.ReadIntPtr(blob);
                var pointer = (GetPointer)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(table, IntPtr.Size * 3), typeof(GetPointer));
                var size = (GetSize)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(table, IntPtr.Size * 4), typeof(GetSize));
                var text = Marshal.PtrToStringAnsi(pointer(blob), checked((int)size(blob).ToUInt64()));
                var result = new ParticleProgramInfo { Bytecode = data, Assembly = text };
                result.Profile = Regex.Match(text, @"(?m)^(cs|ps|vs)_\d_\d").Value;
                var slots = Regex.Match(text, @"dcl_constantbuffer CB2\[(\d+)\]");
                result.ParameterSlots = slots.Success ? int.Parse(slots.Groups[1].Value) : 0;
                var threads = Regex.Match(text, @"dcl_thread_group (\d+), 1, 1");
                result.ThreadsPerGroup = threads.Success ? int.Parse(threads.Groups[1].Value) : 0;
                foreach (Match match in Regex.Matches(text, @"dcl_(?:resource|uav)_structured\s+([tu]\d+),\s*(\d+)"))
                {
                    result.StructuredBuffers[match.Groups[1].Value] = int.Parse(match.Groups[2].Value);
                }
                return result;
            }
            catch (DllNotFoundException)
            {
                throw new NotSupportedException("Particle CS5 requires d3dcompiler_47.dll for resource validation.");
            }
            finally
            {
                if (blob != IntPtr.Zero)
                {
                    Marshal.Release(blob);
                }
            }
        }
    }
}
