using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.WzLib.Compatibility;
using WzComparerR2.WzLib.Utilities;
using static WzComparerR2.WzLib.Utilities.MathHelper;

#if NET6_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace WzComparerR2.WzLib
{
    public class Wz_File : IMapleStoryFile, IDisposable
    {
        public Wz_File(string fileName, Wz_Structure wz, string fallbackFileName = null)
        {
            this.imageCount = 0;
            this.wzStructure = wz;
            this.fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            this.loaded = this.GetHeader(fileName);
        }

        private FileStream fileStream;
        private Wz_Structure wzStructure;
        private Wz_Header header;
        private Wz_Node node;
        private int imageCount;
        private bool loaded;
        private bool isSubDir;
        private Wz_Type type;
        private List<Wz_File> mergedWzFiles;
        private Wz_File ownerWzFile;
        public bool UnknownPkg2 { get; set; }

        internal WzFileReadContext ReadContext { get; set; }

        public Encoding TextEncoding { get; set; }

        public object ReadLock => this.fileStream;

        public FileStream FileStream
        {
            get { return fileStream; }
        }

        public Wz_Structure WzStructure
        {
            get { return wzStructure; }
            set { wzStructure = value; }
        }

        public Wz_Header Header
        {
            get { return header; }
            private set { header = value; }
        }

        public Wz_Node Node
        {
            get { return node; }
            set { node = value; }
        }

        public int ImageCount
        {
            get { return imageCount; }
        }

        public bool Loaded
        {
            get { return loaded; }
        }

        public bool IsSubDir
        {
            get { return this.isSubDir; }
        }

        public Wz_Type Type
        {
            get { return type; }
            set { type = value; }
        }

        public IEnumerable<Wz_File> MergedWzFiles
        {
            get { return this.mergedWzFiles ?? Enumerable.Empty<Wz_File>(); }
        }

        public Wz_File OwnerWzFile
        {
            get { return this.ownerWzFile; }
        }

        Wz_Structure IMapleStoryFile.WzStructure => this.wzStructure;

        Stream IMapleStoryFile.FileStream => this.fileStream;

        object IMapleStoryFile.ReadLock => this.ReadLock;

        public void Close()
        {
            if (this.fileStream != null)
                this.fileStream.Close();
        }

        void IDisposable.Dispose()
        {
            this.Close();
        }

        private bool GetHeader(string fileName)
        {
            this.fileStream.Position = 0;
            var br = new WzBinaryReader(this.fileStream, false);

            long filesize = this.FileStream.Length;
            if (filesize < 4) { goto __failed; }

            string signature = new string(br.ReadChars(4));
            if (signature != Wz_Header.PKG1 && signature != Wz_Header.PKG2)
            {
                // KMST1205: 163-byte random header carrying 64-bit hashes
                if (this.TryReadPkg2KMST1205Header(fileName, out var header64))
                {
                    this.Header = header64;
                    return true;
                }
                // KMST1204: 200-byte random header carrying 64-bit hashes
                if (this.TryReadPkg2KMST1204Header(fileName, out header64))
                {
                    this.Header = header64;
                    return true;
                }
                // KMST1202: 150-byte random header carrying 64-bit hashes
                if (this.TryReadPkg2KMST1202Header(fileName, out header64))
                {
                    this.Header = header64;
                    return true;
                }
                // KMST1201: use rand num header instead of signature
                if (this.TryReadPkg2KMST1201Header(fileName, out var header))
                {
                    this.Header = header;
                    return true;
                }
                goto __failed;
            }

            long dataSize = br.ReadInt64();
            int headerSize = br.ReadInt32();
            string copyright = new string(br.ReadChars(headerSize - (int)this.FileStream.Position));

            if (signature == Wz_Header.PKG1)
            {
                // encver detecting:
                // Since KMST1132, wz removed the 2 bytes encver, and use a fixed wzver '777'.
                // Here we try to read the first 2 bytes from data part and guess if it looks like an encver.
                bool encverMissing = false;
                int encver = -1;
                if (dataSize >= 2)
                {
                    this.fileStream.Position = headerSize;
                    encver = br.ReadUInt16();
                    // encver always less than 256
                    if (encver > 0xff)
                    {
                        encverMissing = true;
                    }
                    else if (encver == 0x80)
                    {
                        // there's an exceptional case that the first field of data part is a compressed int which determined property count,
                        // if the value greater than 127 and also to be a multiple of 256, the first 5 bytes will become to
                        //   80 00 xx xx xx
                        // so we additional check the int value, at most time the child node count in a wz won't greater than 65536.
                        if (dataSize >= 5)
                        {
                            this.fileStream.Position = headerSize;
                            int propCount = br.ReadCompressedInt32();
                            if (propCount > 0 && (propCount & 0xff) == 0 && propCount <= 0xffff)
                            {
                                encverMissing = true;
                            }
                        }
                    }
                }
                else
                {
                    // Obviously, if data part have only 1 byte, encver must be deleted.
                    encverMissing = true;
                }

                int dataStartPos = headerSize + (encverMissing ? 0 : 2);
                this.Header = new Wz_Header.WzPkg1Header(signature, copyright, fileName, headerSize, dataSize, filesize, dataStartPos, encverMissing, encver);
            }
            else if (signature == Wz_Header.PKG2)
            {
                uint hash1 = br.ReadUInt32();
                uint hash2 = br.ReadUInt32();
                int dataStartPos = (int)this.fileStream.Position;
                this.Header = new Wz_Header.WzPkg2Header(signature, copyright, fileName, headerSize, dataSize, filesize, dataStartPos, hash1, hash2);
            }
            else
            {
                goto __failed;
            }

            return true;

        __failed:
            br.BaseStream.Position = 353;
            while (true)
            {
                try
                {
                    if (br.ReadByte() == 0x80)
                    {
                        var dataStartPos = (int)this.fileStream.Position - 1;
                        if (dataStartPos >= 354) break;
                        this.header = new Wz_Header.WzPkg2Header64(Wz_Header.PKG2, null, fileName, dataStartPos, 0, filesize, dataStartPos, 0, 0);
                        this.Header.Capabilities |= Wz_Capabilities.Pkg2RandomHeader;
                        this.UnknownPkg2 = true;
                        return true;
                    }
                }
                catch { break; }
            }
            this.header = new Wz_Header(null, null, fileName, 0, 0, filesize, 0);
            return false;
        }

        private bool TryReadPkg2KMST1201Header(string fileName, out Wz_Header.WzPkg2Header header)
        {
            const int headerLen = 68;
            ReadOnlySpan<int> hash1Offsets = stackalloc int[] { 0x43, 0x1A, 0x30, 0x10 };
            ReadOnlySpan<int> hash2Offsets = stackalloc int[] { 0x2D, 0x07, 0x3F, 0x2E };
            ReadOnlySpan<int> dataSizeOffsets = stackalloc int[] { 0x15, 0x19, 0x39, 0x41 };

            header = null;
            long fileSize = this.fileStream.Length;
            if (fileSize < headerLen)
            {
                return false;
            }

            long expectedDataSize = fileSize - headerLen;
            if (expectedDataSize > uint.MaxValue)
                return false;

            this.fileStream.Position = 0;
            Span<byte> buffer = stackalloc byte[headerLen];
            this.fileStream.ReadExactly(buffer);

            uint hash1 = MathHelper.GatherAsUInt32(buffer, hash1Offsets);
            uint hash2 = MathHelper.GatherAsUInt32(buffer, hash2Offsets);
            uint dataSize = MathHelper.GatherAsUInt32(buffer, dataSizeOffsets);

            if (dataSize != (uint)expectedDataSize)
                return false;

            header = new Wz_Header.WzPkg2Header(Wz_Header.PKG2, null, fileName, headerLen, dataSize, fileSize, headerLen, hash1, hash2);
            header.Capabilities |= Wz_Capabilities.Pkg2RandomHeader;
            this.fileStream.Position = headerLen;
            return true;
        }

        private bool TryReadPkg2KMST1202Header(string fileName, out Wz_Header.WzPkg2Header64 header)
        {
            const int headerLen = 150;
            ReadOnlySpan<int> hash1Offsets = stackalloc int[] { 0x48, 0x24, 0x0F, 0x31, 0x46, 0x47, 0x63, 0x67 };
            ReadOnlySpan<int> hash2Offsets = stackalloc int[] { 0x8E, 0x8C, 0x93, 0x0E, 0x64, 0x7B, 0x2E, 0x4D };
            ReadOnlySpan<int> dataSizeOffsets = stackalloc int[] { 0x12, 0x09, 0x02, 0x95 };
            return this.TryReadPkg2RandomHeader64(fileName, headerLen, hash1Offsets, hash2Offsets, dataSizeOffsets, out header);
        }

        private bool TryReadPkg2KMST1204Header(string fileName, out Wz_Header.WzPkg2Header64 header)
        {
            const int headerLen = 200;
            ReadOnlySpan<int> hash1Offsets = stackalloc int[] { 0x1E, 0x1A, 0x10, 0x01, 0x0F, 0x48, 0xC5, 0x99 };
            ReadOnlySpan<int> hash2Offsets = stackalloc int[] { 0x64, 0x6C, 0x25, 0x16, 0x0A, 0x03, 0xA2, 0xAA };
            ReadOnlySpan<int> dataSizeOffsets = stackalloc int[] { 0x14, 0xB0, 0xB6, 0xB7 };
            return this.TryReadPkg2RandomHeader64(fileName, headerLen, hash1Offsets, hash2Offsets, dataSizeOffsets, out header);
        }

        private bool TryReadPkg2KMST1205Header(string fileName, out Wz_Header.WzPkg2Header64 header)
        {
            const int headerLen = 163;
            ReadOnlySpan<int> hash1Offsets = stackalloc int[] { 0x84, 0x32, 0x43, 0x7F, 0x62, 0x01, 0x83, 0x27 };
            ReadOnlySpan<int> hash2Offsets = stackalloc int[] { 0xA2, 0x1B, 0x05, 0x0D, 0x9A, 0x85, 0x79, 0x4D };
            ReadOnlySpan<int> dataSizeOffsets = stackalloc int[] { 0x08, 0x8F, 0x3F, 0x63 };
            return this.TryReadPkg2RandomHeader64(fileName, headerLen, hash1Offsets, hash2Offsets, dataSizeOffsets, out header);
        }

        private bool TryReadPkg2RandomHeader64(string fileName, int headerLen, ReadOnlySpan<int> hash1Offsets, ReadOnlySpan<int> hash2Offsets, ReadOnlySpan<int> dataSizeOffsets, out Wz_Header.WzPkg2Header64 header)
        {
            header = null;
            long fileSize = this.fileStream.Length;
            if (fileSize < headerLen)
            {
                return false;
            }

            long expectedDataSize = fileSize - headerLen;
            if (expectedDataSize > uint.MaxValue)
                return false;

            this.fileStream.Position = 0;
            Span<byte> buffer = stackalloc byte[headerLen];
            this.fileStream.ReadExactly(buffer);

            uint dataSize = MathHelper.GatherAsUInt32(buffer, dataSizeOffsets);
            if (dataSize != (uint)expectedDataSize)
                return false;

            ulong hash1 = MathHelper.GatherAsUInt64(buffer, hash1Offsets);
            ulong hash2 = MathHelper.GatherAsUInt64(buffer, hash2Offsets);

            header = new Wz_Header.WzPkg2Header64(Wz_Header.PKG2, null, fileName, headerLen, dataSize, fileSize, headerLen, hash1, hash2);
            this.fileStream.Position = headerLen;
            return true;
        }

        public void GetDirTree(Wz_Node parent, bool useBaseWz = false, bool loadWzAsFolder = false, string fileName = null, string fallbackFileName = null)
        {
            var ps = new PartialStream(this.FileStream, this.header.DirStartPosition, this.fileStream.Length - this.header.DirStartPosition, true);
            ps.Position = 0;
            var reader = new WzBinaryReader(ps, false);
            this.GetDirTree(reader, parent, useBaseWz, loadWzAsFolder, fileName, fallbackFileName);
        }

        private void GetDirTree(WzBinaryReader reader, Wz_Node parent, bool useBaseWz = false, bool loadWzAsFolder = false, string fileName = null, string fallbackFileName = null)
        {
            List<Wz_Directory> dirs = new List<Wz_Directory>();

            if (this.header.IsPkg1)
            {
                this.ReadDirTree(reader, parent, ref dirs);
            }
            else if (this.header.IsPkg2)
            {
                this.ReadDirTreePkg2(reader, parent, ref dirs, fileName);
            }
            else
            {
                throw new Exception($"Unknown signature: {this.header.Signature}");
            }

            int dirCount = dirs.Count;
            bool willLoadBaseWz = useBaseWz ? parent.Text.Equals("base.wz", StringComparison.OrdinalIgnoreCase) : false;

            var baseFolder = Path.GetDirectoryName(fileName ?? this.header.FileName);
            var fallbackBaseFolder = Path.GetDirectoryName(fallbackFileName);

            if (willLoadBaseWz && this.WzStructure.AutoDetectExtFiles)
            {
                for (int i = 0; i < dirCount; i++)
                {
                    //检测文件名
                    var m = Regex.Match(dirs[i].Name, @"^([A-Za-z]+)$");
                    if (m.Success)
                    {
                        string wzTypeName = m.Result("$1");

                        //检测扩展wz文件
                        for (int fileID = 2; ; fileID++)
                        {
                            string extDirName = wzTypeName + fileID;
                            string extWzFile = Path.Combine(baseFolder, extDirName + ".wz");
                            if (File.Exists(extWzFile))
                            {
                                if (!dirs.Take(dirCount).Any(dir => extDirName.Equals(dir.Name, StringComparison.OrdinalIgnoreCase)))
                                {
                                    dirs.Add(new Wz_Directory(extDirName, 0, 0, 0, 0, this));
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        //检测KMST1058的wz文件
                        for (int fileID = 1; ; fileID++)
                        {
                            string extDirName = wzTypeName + fileID.ToString("D3");
                            string extWzFile = Path.Combine(baseFolder, extDirName + ".wz");
                            if (File.Exists(extWzFile))
                            {
                                if (!dirs.Take(dirCount).Any(dir => extDirName.Equals(dir.Name, StringComparison.OrdinalIgnoreCase)))
                                {
                                    dirs.Add(new Wz_Directory(extDirName, 0, 0, 0, 0, this));
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < dirs.Count; i++)
            {
                string dir = dirs[i].Name;
                Wz_Node t = parent.Nodes.Add(dir);
                if (i < dirCount)
                {
                    this.GetDirTree(reader, t, false);
                }

                if (t.Nodes.Count == 0)
                {
                    this.WzStructure.has_basewz |= willLoadBaseWz;

                    try
                    {
                        if (loadWzAsFolder)
                        {
                            string wzFolder = willLoadBaseWz ? Path.Combine(Path.GetDirectoryName(baseFolder), dir) : Path.Combine(baseFolder, dir);
                            string fallbackWzFolder = fallbackBaseFolder == null ? null : (willLoadBaseWz ? Path.Combine(Path.GetDirectoryName(fallbackBaseFolder), dir) : Path.Combine(fallbackBaseFolder, dir));
                            if (Directory.Exists(wzFolder) || Directory.Exists(fallbackWzFolder))
                            {
                                this.wzStructure.LoadWzFolder(wzFolder, ref t, false, fallbackWzFolder);
                                if (!willLoadBaseWz)
                                {
                                    var dirWzFile = t.GetValue<Wz_File>();
                                    dirWzFile.Type = Wz_Type.Unknown;
                                    dirWzFile.isSubDir = true;
                                }
                            }
                        }
                        else if (willLoadBaseWz)
                        {
                            string filePath = Path.Combine(baseFolder, dir + ".wz");
                            if (File.Exists(filePath))
                            {
                                this.WzStructure.LoadFile(filePath, t, false, loadWzAsFolder);
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            parent.Nodes.Trim();
        }

        private void ReadDirTree(WzBinaryReader reader, Wz_Node parent, ref List<Wz_Directory> dirs)
        {
            var cryptoKey = this.WzStructure.encryption.Pkg1Keys;
            int count = reader.ReadCompressedInt32();

            for (int i = 0; i < count; i++)
            {
                byte nodeType = reader.ReadByte();
                string name;
                switch (nodeType)
                {
                    case 0x02:
                        int stringOffAdd = this.Header.HasCapabilities(Wz_Capabilities.EncverMissing) ? 2 : -1;
                        name = reader.ReadStringAt(reader.ReadInt32() + stringOffAdd, cryptoKey);
                        break;
                    case 0x04:
                    case 0x03:
                        name = reader.ReadString(cryptoKey);
                        break;
                    default:
                        throw new Exception($"Unknown type {nodeType} in WzDirTree.");
                }

                int size = reader.ReadCompressedInt32();
                int cs32 = reader.ReadCompressedInt32();
                uint pos = (uint)this.fileStream.Position;
                uint hashOffset = reader.ReadUInt32();

                switch (nodeType)
                {
                    case 0x02:
                    case 0x04:
                        Wz_Image img = new Wz_Image(name, size, cs32, hashOffset, pos, this);
                        if (this.ReadContext?.OffsetCalc != null)
                            img.Offset = this.ReadContext.OffsetCalc.CalcOffset(pos, hashOffset);
                        Wz_Node childNode = parent.Nodes.Add(name);
                        childNode.Value = img;
                        img.OwnerNode = childNode;
                        this.imageCount++;
                        break;

                    case 0x03:
                        var dir = new Wz_Directory(name, size, cs32, hashOffset, pos, this);
                        if (this.ReadContext?.OffsetCalc != null)
                            dir.Offset = this.ReadContext.OffsetCalc.CalcOffset(pos, hashOffset);
                        dirs.Add(dir);
                        break;
                }
            }
        }

        private void ReadDirTreePkg2(WzBinaryReader reader, Wz_Node parent, ref List<Wz_Directory> dirs, string fileName = null)
        {
            var context = this.ReadContext;
            if (context == null)
            {
                throw new InvalidOperationException("PKG2 dir tree reading requires a detected read context.");
            }
            var rule = context.Pkg2DirTreeRule ?? throw new InvalidOperationException("PKG2 dir tree reading requires a PKG2 read rule.");
            int entryCount = rule.ReadEntryCount(reader, context.OffsetCalc);
            int hitcount = 0;
            long forcedOffset = -1;
            bool force = false;
            if (this.ForcedCounts.Count > 0)
            {
                entryCount = this.ForcedCounts.Dequeue();
                force = true;
            }

            List<Pkg2DirEntry> entries = new();
            for (int i = 0; i < entryCount; i++)
            {
                byte nodeType = reader.ReadByte();
                string name = null;
                if (nodeType == 0x03 || nodeType == 0x04)
                {
                    if (rule.EntryNamePosition == Pkg2EntryNamePosition.BeforeData)
                        name = force ? context.DirStringReader.ForceReadName(reader, entries.Count == 0, nodeType, fileName) : context.DirStringReader.ReadName(reader, entries.Count == 0);
                }
                else
                {
                    throw new Exception($"Unknown type {nodeType} in WzDirTree.");
                }

                uint sizePosition = (uint)this.fileStream.Position;
                int size = reader.ReadCompressedInt32();
                if (rule.EntryNamePosition == Pkg2EntryNamePosition.BetweenData)
                    name = force ? context.DirStringReader.ForceReadName(reader, entries.Count == 0, nodeType, fileName) : context.DirStringReader.ReadName(reader, entries.Count == 0);
                uint checksumPosition = (uint)this.fileStream.Position;
                int cs32 = reader.ReadCompressedInt32();
                if (context.LengthCalc != null)
                {
                    size = context.LengthCalc.CalcLength(sizePosition, size);
                    cs32 = context.LengthCalc.CalcLength(checksumPosition, cs32);
                }
                if (force && nodeType == 0x04 && hitcount < this.CandidateImageInfos.Count)
                {
                    forcedOffset = this.CandidateImageInfos[hitcount].Item1;
                    size = (int)this.CandidateImageInfos[hitcount].Item2;
                    hitcount++;
                }
                if (rule.EntryNamePosition == Pkg2EntryNamePosition.AfterData)
                    name = force ? context.DirStringReader.ForceReadName(reader, entries.Count == 0, nodeType, fileName) : context.DirStringReader.ReadName(reader, entries.Count == 0);
                entries.Add(new Pkg2DirEntry
                {
                    NodeType = nodeType,
                    Name = name,
                    DataLength = size,
                    Checksum = cs32,
                    ForcedOffset = forcedOffset
                });
            }

            if (rule.ShouldReadOffsets(reader, context.OffsetCalc, entries.Count))
            {
                Span<Pkg2DirEntry> list = CollectionsMarshal.AsSpan(entries);
                for (int i = 0; i < list.Length; i++)
                {
                    uint pos = (uint)this.fileStream.Position;
                    uint hashOffset = reader.ReadUInt32();
                    ref Pkg2DirEntry entry = ref list[i];
                    switch (entry.NodeType)
                    {
                        case 0x04:
                            Wz_Image img = new Wz_Image(entry.Name, entry.DataLength, entry.Checksum, hashOffset, pos, this);
                            if (context.OffsetCalc != null)
                                img.Offset = context.OffsetCalc.CalcOffset(pos, hashOffset);
                            if (entry.ForcedOffset >= 0)
                                img.Offset = entry.ForcedOffset;
                            if (force)
                            {
                                img.Checksum = 0;
                                img.IgnoreChecksum = true;
                            }
                            Wz_Node childNode = parent.Nodes.Add(entry.Name);
                            childNode.Value = img;
                            img.OwnerNode = childNode;
                            this.imageCount++;
                            break;

                        case 0x03:
                            var dir = new Wz_Directory(entry.Name, entry.DataLength, entry.Checksum, hashOffset, pos, this);
                            if (context.OffsetCalc != null)
                                dir.Offset = context.OffsetCalc.CalcOffset(pos, hashOffset);
                            dirs.Add(dir);
                            break;
                    }
                }
            }
        }

        private string getFullPath(Wz_Node parent, string name)
        {
            List<string> path = new List<string>(5);
            path.Add(name.ToLower());
            while (parent != null && !(parent.Value is Wz_File))
            {
                path.Insert(0, parent.Text.ToLower());
                parent = parent.ParentNode;
            }
            if (parent != null)
            {
                path.Insert(0, parent.Text.ToLower().Replace(".wz", ""));
            }
            return string.Join("/", path.ToArray());
        }

        public void DetectWzType()
        {
            this.type = Wz_Type.Unknown;
            if (this.node == null)
            {
                return;
            }

            if (this.node.Nodes["smap.img"] != null
                || this.node.Nodes["zmap.img"] != null)
            {
                this.type = Wz_Type.Base;
            }
            else if (this.node.Nodes["00002000.img"] != null
                || this.node.Nodes["Accessory"] != null
                || this.node.Nodes["Weapon"] != null)
            {
                this.type = Wz_Type.Character;
            }
            else if (this.node.Nodes["BasicEff.img"] != null
                || this.node.Nodes["SetItemInfoEff.img"] != null)
            {
                this.type = Wz_Type.Effect;
            }
            else if (this.node.Nodes["Commodity.img"] != null
                || this.node.Nodes["Curse.img"] != null)
            {
                this.type = Wz_Type.Etc;
            }
            else if (this.node.Nodes["Cash"] != null
                || this.node.Nodes["Consume"] != null)
            {
                this.type = Wz_Type.Item;
            }
            else if (this.node.Nodes["Back"] != null
                || this.node.Nodes["Obj"] != null
                || this.node.Nodes["Physics.img"] != null)
            {
                this.type = Wz_Type.Map;
            }
            else if (this.node.Nodes["PQuest.img"] != null
                || this.node.Nodes["QuestData"] != null)
            {
                this.type = Wz_Type.Quest;
            }
            else if (this.node.Nodes["Attacktype.img"] != null
                || this.node.Nodes["Recipe_9200.img"] != null)
            {
                this.type = Wz_Type.Skill;
            }
            else if (this.node.Nodes["Bgm00.img"] != null
                || this.node.Nodes["BgmUI.img"] != null)
            {
                this.type = Wz_Type.Sound;
            }
            else if (this.node.Nodes["MonsterBook.img"] != null
                || this.node.Nodes["EULA.img"] != null)
            {
                this.type = Wz_Type.String;
            }
            else if (this.node.Nodes["CashShop.img"] != null
                || this.node.Nodes["UIWindow.img"] != null)
            {
                this.type = Wz_Type.UI;
            }

            if (this.type == Wz_Type.Unknown) //用文件名来判断
            {
                string wzName = this.node.Text;

                Match m = Regex.Match(wzName, @"^([A-Za-z]+)_?(\d+)?(?:\.wz)?$");
                if (m.Success)
                {
                    wzName = m.Result("$1");
                }
                this.type = Enum.TryParse<Wz_Type>(wzName, true, out var result) ? result : Wz_Type.Unknown;
            }
        }

        public void MergeWzFile(Wz_File wz_File)
        {
            var children = wz_File.node.Nodes.ToList();
            wz_File.node.Nodes.Clear();
            foreach (var child in children)
            {
                this.node.Nodes.Add(child);
            }

            if (this.mergedWzFiles == null)
            {
                this.mergedWzFiles = new List<Wz_File>();
            }
            this.mergedWzFiles.Add(wz_File);

            wz_File.ownerWzFile = this;
        }

        #region temp workaround for unknown pkg2 encryption
        public List<Tuple<long, long>> CandidateImageInfos { get; set; } = new();
        public Queue<int> ForcedCounts { get; set; } = new();

        public void FindAllHits(int maxSearchCount)
        {
            var reader = new WzBinaryReader(this.FileStream, false);
            var originPos = reader.BaseStream.Position;
            reader.BaseStream.Position = 0;

            byte[] target = { 0x73, 0xF8, 0xFA, 0xD9, 0xC3 }; // 73 F8 FA D9 C3
            reader.BaseStream.Position = 0;
            var offsets = FindAllPatterns(reader, target, maxSearchCount);
            var sizes = DiffAdjacent(offsets);
            var count = offsets.Count;
            if (count == sizes.Count + 1)
            {
                for (int i = 0; i < sizes.Count; i++)
                {
                    long offset = offsets[i];
                    CandidateImageInfos.Add(new Tuple<long, long>(offset, sizes[i]));
                }
            }
            reader.BaseStream.Position = originPos;
        }

        public static unsafe List<long> FindAllPatterns(WzBinaryReader reader, byte[] pattern, int maxSearchCount)
        {
            if (pattern == null || pattern.Length == 0)
                throw new ArgumentException("pattern must not be empty");

            const int BufferSize = 256 * 1024;

            int m = pattern.Length;
            int overlap = m - 1;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize + overlap);
            var positions = new List<long>();

            long filePos = 0;
            bool found = false;

            try
            {
                int bytesRead;

#if NET6_0_OR_GREATER
                if (Avx2.IsSupported && m >= 4)
                {
                    byte last = pattern[m - 1];
                    Vector256<byte> lastVec = Vector256.Create(last);

                    while ((bytesRead = reader.BaseStream.Read(buffer, overlap, BufferSize)) > 0)
                    {
                        int total = bytesRead + overlap;
                        int limit = total - m;

                        fixed (byte* pBuffer = buffer)
                        fixed (byte* pPattern = pattern)
                        {
                            int i = 0;

                            while (i <= limit - 31)
                            {
                                Vector256<byte> data = Avx.LoadVector256(pBuffer + i + m - 1);
                                Vector256<byte> cmp = Avx2.CompareEqual(data, lastVec);

                                uint mask = (uint)Avx2.MoveMask(cmp);

                                while (mask != 0)
                                {
                                    int bit = BitOperations.TrailingZeroCount(mask);
                                    int pos = i + bit;

                                    if (FastMatch(pBuffer + pos, pPattern, m))
                                    {
                                        long realPos = filePos + pos - overlap;
                                        if (realPos >= 0)
                                        {
                                            positions.Add(realPos);
                                            if (positions.Count >= maxSearchCount)
                                            {
                                                found = true;
                                                break;
                                            }
                                        }
                                    }

                                    mask &= mask - 1;
                                }

                                i += 32;
                                if (found) break;
                            }

                            while (i <= limit)
                            {
                                if (pBuffer[i + m - 1] == last && FastMatch(pBuffer + i, pPattern, m))
                                {
                                    long realPos = filePos + i - overlap;
                                    if (realPos >= 0)
                                    {
                                        positions.Add(realPos);
                                        if (positions.Count >= maxSearchCount)
                                        {
                                            found = true;
                                            break;
                                        }
                                    }
                                }

                                i++;
                            }
                        }

                        if (found) break;

                        if (overlap > 0)
                            Buffer.BlockCopy(buffer, total - overlap, buffer, 0, overlap);

                        filePos += bytesRead;
                    }

                    positions.Add(reader.BaseStream.Length);
                    return positions;
                }
                else
#endif
                {
                    Span<int> skip = stackalloc int[256];

                    for (int i = 0; i < 256; i++)
                        skip[i] = m;

                    for (int i = 0; i < m - 1; i++)
                        skip[pattern[i]] = m - 1 - i;

                    while ((bytesRead = reader.BaseStream.Read(buffer, overlap, BufferSize)) > 0)
                    {
                        int total = bytesRead + overlap;
                        int limit = total - m;

                        int pos = 0;

                        while (pos <= limit)
                        {
                            int j = m - 1;

                            while (j >= 0 && buffer[pos + j] == pattern[j])
                                j--;

                            if (j < 0)
                            {
                                long realPos = filePos + pos - overlap;
                                if (realPos >= 0)
                                {
                                    positions.Add(realPos);
                                    if (positions.Count >= maxSearchCount)
                                    {
                                        found = true;
                                        break;
                                    }
                                }

                                pos++;
                            }
                            else
                            {
                                pos += skip[buffer[pos + m - 1]];
                            }
                        }

                        if (found) break;

                        if (overlap > 0)
                            Buffer.BlockCopy(buffer, total - overlap, buffer, 0, overlap);

                        filePos += bytesRead;
                    }

                    positions.Add(reader.BaseStream.Length);
                    return positions;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

#if NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool FastMatch(byte* data, byte* pattern, int length)
        {
            int i = 0;

            while (length - i >= 8)
            {
                if (*(ulong*)(data + i) != *(ulong*)(pattern + i))
                    return false;

                i += 8;
            }

            if (length - i >= 4)
            {
                if (*(uint*)(data + i) != *(uint*)(pattern + i))
                    return false;

                i += 4;
            }

            while (i < length)
            {
                if (data[i] != pattern[i])
                    return false;

                i++;
            }

            return true;
        }
#endif

        static List<long> DiffAdjacent(List<long> list)
        {
            var diffs = new List<long>();
            if (list.Count <= 1) return diffs;

            for (int i = 1; i < list.Count; i++)
                diffs.Add(list[i] - list[i - 1]);

            return diffs;
        }
        #endregion

        private struct Pkg2DirEntry
        {
            public int NodeType;
            public string Name;
            public int DataLength;
            public int Checksum;
            public long ForcedOffset;
        }
    }

}
