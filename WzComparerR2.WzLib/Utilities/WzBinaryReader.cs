using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;

namespace WzComparerR2.WzLib.Utilities
{
    public class WzBinaryReader
    {
        public WzBinaryReader(Stream stream, bool useStringPool)
            : this(stream, useStringPool ? new SimpleWzStringPool() : null)
        {
        }

        public WzBinaryReader(Stream stream, IWzStringPool stringPool)
        {
            this.BaseStream = stream;
            this.bReader = new BinaryReader(this.BaseStream, System.Text.Encoding.ASCII, true);
            this.stringPool = stringPool;
        }

        public Stream BaseStream { get; private set; }
        public int StringReferenceOffsetBytes { get; set; }
        private BinaryReader bReader;
        private IWzStringPool stringPool;

        public byte ReadByte()
        {
            return this.bReader.ReadByte();
        }

        public sbyte ReadSByte()
        {
            return this.bReader.ReadSByte();
        }

        public short ReadInt16()
        {
            return this.bReader.ReadInt16();
        }

        public ushort ReadUInt16()
        {
            return this.bReader.ReadUInt16();
        }

        public int ReadCompressedInt32()
        {
            int s = this.bReader.ReadSByte();
            return (s == -128) ? this.bReader.ReadInt32() : s;
        }

        public uint ReadCompressedUInt32()
        {
            return (uint)this.ReadCompressedInt32();
        }

        public int ReadInt32()
        {
            return this.bReader.ReadInt32();
        }

        public uint ReadUInt32()
        {
            return this.bReader.ReadUInt32();
        }

        public long ReadCompressedInt64()
        {
            int s = this.bReader.ReadSByte();
            return (s == -128) ? this.bReader.ReadInt64() : s;
        }

        public long ReadInt64()
        {
            return this.bReader.ReadInt64();
        }

        public float ReadCompressedSingle()
        {
            Span<int> bits = stackalloc int[1];
            bits[0] = this.ReadCompressedInt32();
            return MemoryMarshal.Cast<int, float>(bits)[0];
        }

        public double ReadDouble()
        {
            return this.bReader.ReadDouble();
        }

        public char[] ReadChars(int count)
        {
            return this.bReader.ReadChars(count);
        }

        public string ReadString(IWzDecrypter decrypter)
        {
            long currentPos = this.BaseStream.Position;

            int size = this.ReadSByte();
            if (size < 0) // read ASCII/cp1252 string
            {
                size = (size == -128) ? this.ReadInt32() : -size;

                return this.ReadAsciiString(size, currentPos, decrypter);
            }
            else if (size > 0) // read UTF-16LE string
            {
                if (size == 127)
                {
                    size = this.bReader.ReadInt32();
                }
                return this.ReadUtf16String(size, currentPos, decrypter, true);
            }
            else
            {
                return string.Empty;
            }
        }

        // Introduced in KMST1198
        public string ReadPkg2DirString(IWzDecrypter decrypter)
        {
            long currentPos = this.BaseStream.Position;

            int size = this.ReadSByte();
            if (size < 0)
            {
                size = -size;
                return this.ReadUtf16String(size, currentPos, decrypter, false);
            }
            else if (size > 0)
            {
                throw new Exception($"Unexpected string length: {size}");
            }
            else
            {
                return string.Empty;
            }
        }

        // Introduced in KMST1202, 16bit length prefix string
        public string ReadPkg2DirStringV2(IWzDecrypter decrypter)
        {
            long currentPos = this.BaseStream.Position;

            int size = this.ReadInt16();
            if (size < 0)
            {
                size = -size;
                return this.ReadUtf16String(size, currentPos, decrypter, false);
            }
            else if (size > 0)
            {
                throw new Exception($"Unexpected string length: {size}");
            }
            else
            {
                return string.Empty;
            }
        }

        private const int StackBufferSize = 256;

        private string ReadAsciiString(int size, long offset, IWzDecrypter decrypter)
        {
            if (size < 0)
                throw new InvalidDataException("Negative string length.");

            byte[] rentedBytes = null;
            char[] rentedChars = null;
            Span<byte> buffer =
#if NET6_0_OR_GREATER
                size <= StackBufferSize ? stackalloc byte[size] :
#endif
                (rentedBytes = ArrayPool<byte>.Shared.Rent(size)).AsSpan(0, size);
            try
            {
#if NET6_0_OR_GREATER
                this.BaseStream.ReadExactly(buffer);
#else
                this.BaseStream.ReadExactly(rentedBytes, 0, size);
#endif
                decrypter.Decrypt(buffer);

                Span<char> chars =
#if NET6_0_OR_GREATER
                    size <= StackBufferSize / sizeof(char) ? stackalloc char[size] :
#endif
                    (rentedChars = ArrayPool<char>.Shared.Rent(size)).AsSpan(0, size);
                MathHelper.DecodeWzStringAscii(buffer, chars);
                return this.stringPool != null ? this.stringPool.GetOrAdd(offset, chars) : chars.ToString();
            }
            finally
            {
                if (rentedChars != null)
                    ArrayPool<char>.Shared.Return(rentedChars);
                if (rentedBytes != null)
                    ArrayPool<byte>.Shared.Return(rentedBytes);
            }
        }

        private string ReadUtf16String(int size, long offset, IWzDecrypter decrypter, bool applyMask)
        {
            if (size < 0)
                throw new InvalidDataException("Negative string length.");

            int byteSize = checked(size * sizeof(char));
            byte[] rentedBytes = null;
            Span<byte> buffer =
#if NET6_0_OR_GREATER
                byteSize <= StackBufferSize ? stackalloc byte[byteSize] :
#endif
                (rentedBytes = ArrayPool<byte>.Shared.Rent(byteSize)).AsSpan(0, byteSize);
            try
            {
#if NET6_0_OR_GREATER
                this.BaseStream.ReadExactly(buffer);
#else
                this.BaseStream.ReadExactly(rentedBytes, 0, byteSize);
#endif
                decrypter.Decrypt(buffer);

                Span<char> chars = MemoryMarshal.Cast<byte, char>(buffer);
                if (applyMask)
                    MathHelper.ApplyWzStringCharMask(chars, chars);
                return this.stringPool != null ? this.stringPool.GetOrAdd(offset, chars) : chars.ToString();
            }
            finally
            {
                if (rentedBytes != null)
                    ArrayPool<byte>.Shared.Return(rentedBytes);
            }
        }

        public string ReadImageObjectTypeName(IWzDecrypter decrypter)
        {
            int flag = this.bReader.ReadByte();
            switch (flag)
            {
                case 0x73:
                    return this.ReadString(decrypter);
                case 0x1B:
                    return this.ReadStringAt(this.ReadInt32() + this.StringReferenceOffsetBytes, decrypter);
                default:
                    throw new Exception($"Unexpected flag '{flag}' when reading string at {this.BaseStream.Position}.");
            }
        }

        public string ReadImageString(IWzDecrypter decrypter)
        {
            int flag = this.bReader.ReadByte();
            switch (flag)
            {
                case 0x00:
                    return this.ReadString(decrypter);
                case 0x01:
                    return this.ReadStringAt(this.ReadInt32() + this.StringReferenceOffsetBytes, decrypter);
                case 0x04: 
                    this.SkipBytes(8);
                    return null;
                default:
                    throw new Exception($"Unexpected flag '{flag}' when reading string at {this.BaseStream.Position}.");
            }
        }

        public string ReadStringAt(long offset, IWzDecrypter decrypter)
        {
            if (this.stringPool != null && this.stringPool.TryGet(offset, out string s))
            {
                return s;
            }
            long currentPos = this.BaseStream.Position;
            this.BaseStream.Position = offset;
            s = this.ReadString(decrypter);
            this.BaseStream.Position = currentPos;
            return s;
        }

        public byte[] ReadBytes(int count)
        {
            return this.bReader.ReadBytes(count);
        }

        public void SkipBytes(int count)
        {
            if (this.BaseStream.CanSeek)
            {
                this.BaseStream.Position += count;
            }
            else
            {
                var buffer = ArrayPool<byte>.Shared.Rent(Math.Min(count, 16384));
                try
                {
                    while (count > 0)
                    {
                        int actual = this.BaseStream.Read(buffer, 0, Math.Min(count, buffer.Length));
                        if (actual == 0)
                        {
                            throw new EndOfStreamException();
                        }
                        count -= actual;
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
    }
}
