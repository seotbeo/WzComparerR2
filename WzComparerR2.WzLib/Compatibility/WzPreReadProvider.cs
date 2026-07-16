using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WzComparerR2.WzLib.Utilities;

namespace WzComparerR2.WzLib.Compatibility
{
    /// <summary>
    /// Interface for pre-reading wz directory tree structure without decryption.
    /// </summary>
    internal interface IWzPreReader
    {
        bool TryPreRead(Wz_File wzFile, out WzPreReadResult result);
    }

    /// <summary>
    /// Static registry of all pre-reader implementations, ordered by priority.
    /// </summary>
    internal static class WzPreReaders
    {
        private static readonly IWzPreReader[] readers = new IWzPreReader[]
        {
            new Pkg1PreReader(),
            new Pkg2PreReader(WzFileFormat.Pkg2Kmst1196, false),
            new Pkg2PreReader(WzFileFormat.Pkg2Kmst1198, true),
            new Pkg2PreReader(WzFileFormat.Pkg2Kmst1201, true, true),
            new Pkg2PreReader64(WzFileFormat.Pkg2Kmst1202),
        };

        private static readonly IWzPreReader[] readers_UNK = new IWzPreReader[]
        {
            new Pkg2PreReader64_UNK(WzFileFormat.Pkg2Kmst1202),
        };

        public static IReadOnlyList<IWzPreReader> All => readers;
        public static IReadOnlyList<IWzPreReader> All_UNK => readers_UNK;
    }

    #region PKG1

    internal sealed class Pkg1PreReader : IWzPreReader
    {
        public bool TryPreRead(Wz_File wzFile, out WzPreReadResult result)
        {
            result = null;
            if (!wzFile.Header.IsPkg1)
                return false;

            try
            {
                wzFile.FileStream.Position = wzFile.Header.DirStartPosition;
                var reader = new WzBinaryReader(wzFile.FileStream, false);
                long dirStartPos = wzFile.FileStream.Position;
                result = new WzPreReadResult(WzFileFormat.Pkg1, new List<WzPreReadNodeInfo>(), dirStartPos, 0);
                ReadTree(reader, result);
                result.DirEndPosition = reader.BaseStream.Position;
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        private static void ReadTree(WzBinaryReader reader, WzPreReadResult result)
        {
            int count = reader.ReadCompressedInt32();
            int dirCount = 0;

            for (int i = 0; i < count; i++)
            {
                byte nodeType = reader.ReadByte();
                switch (nodeType)
                {
                    case 0x02:
                        reader.ReadInt32();
                        break;
                    case 0x03:
                    case 0x04:
                        if (result.FirstStringRawBytes == null)
                        {
                            result.FirstStringRawBytes = WzPreReadHelper.ReadStringRawBytes(reader, false, out var enc);
                            result.FirstStringEncoding = enc;
                        }
                        else
                        {
                            WzPreReadHelper.SkipString(reader);
                        }
                        break;
                    default:
                        throw new InvalidDataException($"Unknown type {nodeType} in dir pre-read.");
                }

                int size = reader.ReadCompressedInt32();
                reader.ReadCompressedInt32();
                uint offsetPos = (uint)reader.BaseStream.Position;
                uint hashedOffset = reader.ReadUInt32();

                result.Nodes.Add(new WzPreReadNodeInfo
                {
                    NodeType = nodeType,
                    DataLength = size,
                    HashedOffsetPosition = offsetPos,
                    HashedOffset = hashedOffset,
                });

                if (nodeType == 0x03)
                    dirCount++;
            }

            for (int i = 0; i < dirCount; i++)
                ReadTree(reader, result);
        }
    }

    #endregion

    #region PKG2

    internal sealed class Pkg2PreReader : IWzPreReader
    {
        public Pkg2PreReader(WzFileFormat format, bool isPkg2DirString = false, bool supportRandomHeader = false)
        {
            this.rule = new Pkg2PreReadRule(format, isPkg2DirString, supportRandomHeader);
        }

        private readonly IPkg2PreReadRule rule;

        public bool TryPreRead(Wz_File wzFile, out WzPreReadResult result)
        {
            return Pkg2PreReadTreeWalker.TryPreRead(wzFile, this.rule, out result);
        }
    }

    internal sealed class Pkg2PreReader64 : IWzPreReader
    {
        public Pkg2PreReader64(WzFileFormat format)
        {
            this.rule = new Pkg2PreReadRule64();
        }

        private readonly IPkg2PreReadRule rule;

        public bool TryPreRead(Wz_File wzFile, out WzPreReadResult result)
        {
            return Pkg2PreReadTreeWalker.TryPreRead(wzFile, rule, out result);
        }
    }

    internal sealed class Pkg2PreReader64_UNK : IWzPreReader
    {
        public Pkg2PreReader64_UNK(WzFileFormat format)
        {
            this.rule = new Pkg2PreReadRule64_UNK();
        }

        private readonly IPkg2PreReadRule rule;

        public bool TryPreRead(Wz_File wzFile, out WzPreReadResult result)
        {
            return Pkg2PreReadTreeWalker.TryPreRead(wzFile, rule, out result);
        }
    }

    internal static class Pkg2PreReadTreeWalker
    {
        public static bool TryPreRead(Wz_File wzFile, IPkg2PreReadRule rule, out WzPreReadResult result)
        {
            result = null;
            if (!rule.CanHandle(wzFile, out var context))
                return false;

            try
            {
                wzFile.FileStream.Position = wzFile.Header.DirStartPosition;
                var reader = new WzBinaryReader(wzFile.FileStream, false);
                long dirStartPos = wzFile.FileStream.Position;
                result = new WzPreReadResult(rule.Format, new List<WzPreReadNodeInfo>(), dirStartPos, 0)
                {
                    Pkg2DirEntryCounts = new List<Pkg2DirEntryCount>(),
                };

                ReadTree(reader, result, rule, context, wzFile.FileStream.Length, true);
                result.DirEndPosition = reader.BaseStream.Position;
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        private static void ReadTree(WzBinaryReader reader, WzPreReadResult result, IPkg2PreReadRule rule, Pkg2PreReadContext context, long fileLen, bool isTopLevel)
        {
            var header = rule.ReadDirectoryHeader(reader, context);
            var entries = new List<Pkg2PreReadEntry>();
            long entriesStartPosition = reader.BaseStream.Position;

            if (header.IsFixedEntryCount)
            {
                for (long i = 0; i < header.FixedEntryCount; i++)
                {
                    ReadOneEntry(reader, result, rule, context, entries.Count, entries);
                }
            }
            else
            {
                while (true)
                {
                    if (reader.BaseStream.Position >= reader.BaseStream.Length)
                    {
                        if (rule.AllowEntryBoundaryProbe)
                            break;
                        throw new EndOfStreamException("Unexpected end of PKG2 directory pre-read.");
                    }

                    long entryStartPosition = reader.BaseStream.Position;
                    byte nodeType = reader.ReadByte();
                    if (nodeType == 0x03 || nodeType == 0x04)
                    {
                        try
                        {
                            rule.ReadEntryName(reader, result, context, entries.Count);
                            int size = reader.ReadCompressedInt32();
                            if (rule.ValidateImageLength && (size < 0 || (nodeType == 0x03 && size != 0)))
                            {
                                reader.BaseStream.Position = entryStartPosition;
                                break;
                            }
                            reader.ReadCompressedInt32();
                            entries.Add(new Pkg2PreReadEntry { NodeType = nodeType, DataLength = size, EndPosition = reader.BaseStream.Position });
                        }
                        catch when (rule.AllowEntryBoundaryProbe)
                        {
                            reader.BaseStream.Position = entryStartPosition;
                            break;
                        }
                    }
                    else if (rule.IsDirectoryTerminator(nodeType, header))
                    {
                        reader.BaseStream.Position--;
                        break;
                    }
                    else if (rule.AllowEntryBoundaryProbe)
                    {
                        reader.BaseStream.Position = entryStartPosition;
                        break;
                    }
                    else
                    {
                        throw new InvalidDataException($"Invalid node type {nodeType} in pkg2 pre-read.");
                    }
                }
            }

            do
            {
                rule.ValidateOffsetSection(reader, header, entries.Count);

                result.Pkg2DirEntryCounts.Add(new Pkg2DirEntryCount
                {
                    EncryptedEntryCount = header.EncryptedEntryCount,
                    ActualEntryCount = entries.Count,
                    ActualImgCount = entries.Count(e => e.NodeType == 0x04),
                });

                for (int i = 0; i < entries.Count; i++)
                {
                    uint offsetPos = (uint)reader.BaseStream.Position;
                    uint hashedOffset = reader.ReadUInt32();
                    result.Nodes.Add(new WzPreReadNodeInfo
                    {
                        NodeType = entries[i].NodeType,
                        DataLength = entries[i].DataLength,
                        HashedOffsetPosition = offsetPos,
                        HashedOffset = hashedOffset,
                    });
                }

                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].NodeType == 0x03)
                        ReadTree(reader, result, rule, context, fileLen, false);
                }

                if (isTopLevel && rule.ValidateImageLength)
                {
                    // Validate the total image length against the file length.
                    long dirEndPos = reader.BaseStream.Position;
                    long imageTotalLength = 0;
                    foreach (var node in result.Nodes)
                    {
                        if (node.NodeType == 0x04)
                        {
                            imageTotalLength += node.DataLength;
                        }
                    }
                    if (dirEndPos + imageTotalLength > fileLen)
                    {
                        // rollback 1 entry and try again.
                        if (entries.Count > 1)
                        {
                            entries.RemoveAt(entries.Count - 1);
                            reader.BaseStream.Position = entries[entries.Count - 1].EndPosition;
                            result.Pkg2DirEntryCounts.Clear();
                            result.Nodes.Clear();
                            continue;
                        }
                        else
                        {
                            throw new InvalidDataException("PKG2 image length exceeds file length.");
                        }
                    }
                }

                break;
            }
            while (true);
        }

        private static void ReadOneEntry(WzBinaryReader reader, WzPreReadResult result, IPkg2PreReadRule rule, Pkg2PreReadContext context, int entryIndex, List<Pkg2PreReadEntry> entries)
        {
            byte nodeType = reader.ReadByte();
            if (nodeType != 0x03 && nodeType != 0x04)
            {
                throw new InvalidDataException($"Invalid node type {nodeType} in pkg2 pre-read.");
            }

            rule.ReadEntryName(reader, result, context, entryIndex);
            int size = reader.ReadCompressedInt32();
            reader.ReadCompressedInt32();
            entries.Add(new Pkg2PreReadEntry { NodeType = nodeType, DataLength = size, EndPosition = reader.BaseStream.Position });
        }
    }

    internal interface IPkg2PreReadRule
    {
        WzFileFormat Format { get; }
        bool AllowEntryBoundaryProbe { get; }
        bool ValidateImageLength { get; }
        bool CanHandle(Wz_File wzFile, out Pkg2PreReadContext context);
        Pkg2DirHeader ReadDirectoryHeader(WzBinaryReader reader, Pkg2PreReadContext context);
        bool IsDirectoryTerminator(byte nodeType, Pkg2DirHeader header);
        void ReadEntryName(WzBinaryReader reader, WzPreReadResult result, Pkg2PreReadContext context, int entryIndex);
        void ValidateOffsetSection(WzBinaryReader reader, Pkg2DirHeader header, int actualEntryCount);
    }

    internal sealed class Pkg2PreReadRule : IPkg2PreReadRule
    {
        public Pkg2PreReadRule(WzFileFormat format, bool isPkg2DirString, bool supportRandomHeader)
        {
            this.Format = format;
            this.isPkg2DirString = isPkg2DirString;
            this.supportRandomHeader = supportRandomHeader;
        }

        private readonly bool isPkg2DirString;
        private readonly bool supportRandomHeader;

        public WzFileFormat Format { get; }
        public bool AllowEntryBoundaryProbe => false;
        public bool ValidateImageLength => false;

        public bool CanHandle(Wz_File wzFile, out Pkg2PreReadContext context)
        {
            context = new Pkg2PreReadContext();
            if (!wzFile.Header.IsPkg2)
                return false;

            if (wzFile.Header.HasCapabilities(Wz_Capabilities.Pkg2RandomHeader64))
                return false;

            if (wzFile.Header.HasCapabilities(Wz_Capabilities.Pkg2RandomHeader) && !this.supportRandomHeader)
                return false;

            context.IsPkg2DirString = this.isPkg2DirString;
            context.Header64 = null;
            return true;
        }

        public Pkg2DirHeader ReadDirectoryHeader(WzBinaryReader reader, Pkg2PreReadContext context)
        {
            return new Pkg2DirHeader
            {
                EncryptedEntryCount = reader.ReadCompressedInt32(),
                IsFixedEntryCount = false,
                FixedEntryCount = 0,
            };
        }

        public bool IsDirectoryTerminator(byte nodeType, Pkg2DirHeader header)
        {
            return nodeType == 0x80
                || (header.EncryptedEntryCount >= -127 && header.EncryptedEntryCount <= 127 && nodeType == header.EncryptedEntryCount);
        }

        public void ReadEntryName(WzBinaryReader reader, WzPreReadResult result, Pkg2PreReadContext context, int entryIndex)
        {
            if (result.FirstStringRawBytes == null && entryIndex == 0)
            {
                result.FirstStringRawBytes = WzPreReadHelper.ReadStringRawBytes(reader, context.IsPkg2DirString, out var enc);
                result.FirstStringEncoding = enc;
            }
            else if (context.IsPkg2DirString && result.SecondStringRawBytes == null && entryIndex == 1)
            {
                result.SecondStringRawBytes = WzPreReadHelper.ReadStringRawBytes(reader, false, out var enc);
                result.SecondStringEncoding = enc;
            }
            else
            {
                WzPreReadHelper.SkipString(reader);
            }
        }

        public void ValidateOffsetSection(WzBinaryReader reader, Pkg2DirHeader header, int actualEntryCount)
        {
            int encryptedOffsetCount = reader.ReadCompressedInt32();
            if (encryptedOffsetCount != header.EncryptedEntryCount)
                throw new InvalidDataException("Offset count does not match entry count.");
        }
    }

    internal sealed class Pkg2PreReadRule64 : IPkg2PreReadRule
    {
        public WzFileFormat Format => WzFileFormat.Pkg2Kmst1202;
        public bool AllowEntryBoundaryProbe => true;
        public bool ValidateImageLength => true;

        public bool CanHandle(Wz_File wzFile, out Pkg2PreReadContext context)
        {
            context = new Pkg2PreReadContext();
            if (!wzFile.Header.IsPkg2)
                return false;
            if (wzFile.Header is not Wz_Header.WzPkg2Header64 header64)
                return false;

            context.IsPkg2DirString = true;
            context.Header64 = header64;
            return true;
        }

        public Pkg2DirHeader ReadDirectoryHeader(WzBinaryReader reader, Pkg2PreReadContext context)
        {
            return new Pkg2DirHeader
            {
                EncryptedEntryCount = reader.ReadCompressedInt64(),
                IsFixedEntryCount = false,
                FixedEntryCount = 0,
            };
        }

        public bool IsDirectoryTerminator(byte nodeType, Pkg2DirHeader header)
        {
            return false;
        }

        public void ReadEntryName(WzBinaryReader reader, WzPreReadResult result, Pkg2PreReadContext context, int entryIndex)
        {
            if (result.FirstStringRawBytes == null && entryIndex == 0)
            {
                result.FirstStringRawBytes = WzPreReadHelper.ReadPkg2DirStringV2RawBytes(reader, out var enc);
                result.FirstStringEncoding = enc;
            }
            else if (result.SecondStringRawBytes == null && entryIndex == 1)
            {
                result.SecondStringRawBytes = WzPreReadHelper.ReadStringRawBytes(reader, false, out var enc);
                result.SecondStringEncoding = enc;
            }
            else if (entryIndex == 0)
            {
                WzPreReadHelper.SkipPkg2DirStringV2(reader);
            }
            else
            {
                WzPreReadHelper.SkipString(reader);
            }
        }

        public void ValidateOffsetSection(WzBinaryReader reader, Pkg2DirHeader header, int actualEntryCount)
        {
            // KMST1202 has no encrypted offset-count prefix.
        }
    }

    internal sealed class Pkg2PreReadRule64_UNK : IPkg2PreReadRule
    {
        public WzFileFormat Format => WzFileFormat.Pkg2Kmst1202;
        public bool AllowEntryBoundaryProbe => true;
        public bool ValidateImageLength => false;

        public bool CanHandle(Wz_File wzFile, out Pkg2PreReadContext context)
        {
            context = new Pkg2PreReadContext();
            if (!wzFile.Header.IsPkg2)
                return false;
            if (wzFile.Header is not Wz_Header.WzPkg2Header64 header64)
                return false;

            context.IsPkg2DirString = true;
            context.Header64 = header64;
            return true;
        }

        public Pkg2DirHeader ReadDirectoryHeader(WzBinaryReader reader, Pkg2PreReadContext context)
        {
            return new Pkg2DirHeader
            {
                EncryptedEntryCount = reader.ReadCompressedInt64(),
                IsFixedEntryCount = false,
                FixedEntryCount = 0,
            };
        }

        public bool IsDirectoryTerminator(byte nodeType, Pkg2DirHeader header)
        {
            return false;
        }

        public void ReadEntryName(WzBinaryReader reader, WzPreReadResult result, Pkg2PreReadContext context, int entryIndex)
        {
            if (result.FirstStringRawBytes == null && entryIndex == 0)
            {
                result.FirstStringRawBytes = WzPreReadHelper.ReadPkg2DirStringV2RawBytes(reader, out var enc);
                result.FirstStringEncoding = enc;
            }
            else if (result.SecondStringRawBytes == null && entryIndex == 1)
            {
                result.SecondStringRawBytes = WzPreReadHelper.ReadPkg2DirStringV2RawBytes(reader, out var enc);
                result.SecondStringEncoding = enc;
            }
            else
            {
                WzPreReadHelper.SkipPkg2DirStringV2(reader);
            }
        }

        public void ValidateOffsetSection(WzBinaryReader reader, Pkg2DirHeader header, int actualEntryCount)
        {
            // KMST1202 has no encrypted offset-count prefix.
        }
    }

    internal struct Pkg2PreReadContext
    {
        public bool IsPkg2DirString;
        public Wz_Header.WzPkg2Header64 Header64;
    }

    internal struct Pkg2DirHeader
    {
        public long EncryptedEntryCount;
        public bool IsFixedEntryCount;
        public long FixedEntryCount;
    }

    internal struct Pkg2PreReadEntry
    {
        public int NodeType;
        public int DataLength;
        public long EndPosition;
    }

    #endregion

    #region Shared helpers

    internal static class WzPreReadHelper
    {
        public static byte[] ReadStringRawBytes(this WzBinaryReader reader, bool isPkg2DirString, out WzStringEncoding encoding)
        {
            sbyte lenPrefix = reader.ReadSByte();
            if (isPkg2DirString)
            {
                encoding = WzStringEncoding.UTF16;
                if (lenPrefix < 0) return reader.ReadBytes((-lenPrefix) * 2);
                if (lenPrefix == 0) return Array.Empty<byte>();
                throw new InvalidDataException("Unexpected positive length in pkg2 dir string.");
            }

            if (lenPrefix < 0)
            {
                encoding = WzStringEncoding.ASCII;
                int size = lenPrefix == -128 ? reader.ReadInt32() : -lenPrefix;
                return reader.ReadBytes(size);
            }
            if (lenPrefix > 0)
            {
                encoding = WzStringEncoding.UTF16;
                int size = lenPrefix == 127 ? reader.ReadInt32() : lenPrefix;
                return reader.ReadBytes(size * 2);
            }
            encoding = WzStringEncoding.Unknown;
            return Array.Empty<byte>();
        }

        public static byte[] ReadPkg2DirStringV2RawBytes(this WzBinaryReader reader, out WzStringEncoding encoding)
        {
            int len = reader.ReadInt16();
            encoding = WzStringEncoding.UTF16;
            if (len < 0)
            {
                return reader.ReadBytes((-len) * 2);
            }
            if (len == 0)
            {
                return Array.Empty<byte>();
            }
            throw new InvalidDataException("Unexpected positive length in pkg2 dir string v2.");
        }

        public static void SkipString(this WzBinaryReader reader)
        {
            sbyte len = reader.ReadSByte();
            if (len < 0)
            {
                int size = len == -128 ? reader.ReadInt32() : -len;
                reader.BaseStream.Position += size;
            }
            else if (len > 0)
            {
                int size = len == 127 ? reader.ReadInt32() : len;
                reader.BaseStream.Position += size * 2;
            }
        }

        public static void SkipPkg2DirStringV2(this WzBinaryReader reader)
        {
            int len = reader.ReadInt16();
            if (len < 0)
            {
                reader.BaseStream.Position += (-len) * 2;
            }
            else if (len > 0)
            {
                throw new InvalidDataException("Unexpected positive length in pkg2 dir string v2.");
            }
        }

        public static bool IsLegalNodeName(ReadOnlySpan<char> utf16NodeName)
        {
            if (utf16NodeName.Length == 0) return false;
            if (utf16NodeName.EndsWith(".img".AsSpan()) || utf16NodeName.EndsWith(".lua".AsSpan())) return true;
            foreach (var c in utf16NodeName)
            {
                if (!(0x20 <= c && c <= 0x7f)) return false;
            }
            return true;
        }

        public static bool IsLegalNodeName(ReadOnlySpan<byte> asciiNodeName)
        {
            if (asciiNodeName.Length == 0) return false;
            if (asciiNodeName.EndsWith(".img"u8) || asciiNodeName.EndsWith(".lua"u8)) return true;
            foreach (var c in asciiNodeName)
            {
                if (!(0x20 <= c && c <= 0x7f)) return false;
            }
            return true;
        }
    }

    #endregion

    #region Enums and result types

    public enum WzFileFormat
    {
        Pkg1,
        Pkg2Kmst1196,
        Pkg2Kmst1198,
        Pkg2Kmst1201,
        Pkg2Kmst1202,
    }

    public enum WzStringEncoding
    {
        Unknown,
        ASCII,
        UTF16,
    }

    public sealed class WzPreReadResult
    {
        public WzPreReadResult(WzFileFormat format, List<WzPreReadNodeInfo> nodes, long dirStartPosition, long dirEndPosition)
        {
            this.Format = format;
            this.Nodes = nodes;
            this.DirStartPosition = dirStartPosition;
            this.DirEndPosition = dirEndPosition;
        }

        public WzFileFormat Format { get; }
        public List<WzPreReadNodeInfo> Nodes { get; }
        public long DirStartPosition { get; }
        public long DirEndPosition { get; internal set; }
        public WzStringEncoding FirstStringEncoding { get; set; }
        public byte[] FirstStringRawBytes { get; set; }
        public WzStringEncoding SecondStringEncoding { get; set; }
        public byte[] SecondStringRawBytes { get; set; }

        /// <summary>
        /// Per-directory-level encrypted and actual entry counts for PKG2 files.
        /// </summary>
        public List<Pkg2DirEntryCount> Pkg2DirEntryCounts { get; set; }
    }

    public struct WzPreReadNodeInfo
    {
        public int NodeType;
        public int DataLength;
        public uint HashedOffsetPosition;
        public uint HashedOffset;
    }

    public struct Pkg2DirEntryCount
    {
        public long EncryptedEntryCount;
        public int ActualEntryCount;
        public int ActualImgCount;
    }

    #endregion
}
