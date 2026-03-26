using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.WzLib.Utilities;

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
            this.directories = new List<Wz_Directory>();
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
        private readonly List<Wz_Directory> directories;
        private string pkg2UnsupportedReason;

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

        public string Pkg2UnsupportedReason
        {
            get { return this.pkg2UnsupportedReason; }
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
            if (signature != Wz_Header.PKG1 && signature != Wz_Header.PKG2) { goto __failed; }

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
                this.Header = new Wz_Header(signature, copyright, fileName, headerSize, dataSize, filesize, dataStartPos);

                if (encverMissing)
                {
                    // not sure if nexon will change this magic version, just hard coded.
                    this.Header.SetWzVersion(777);
                    this.Header.VersionChecked = true;
                    this.Header.Capabilities |= Wz_Capabilities.EncverMissing;
                }
                else
                {
                    this.Header.SetOrdinalVersionDetector(encver);
                }
            }
            else if (signature == Wz_Header.PKG2)
            {
                uint hash1 = br.ReadUInt32();
                uint hash2 = br.ReadUInt32();
                int dataStartPos = (int)this.fileStream.Position;
                Wz_Header header = new(signature, copyright, fileName, headerSize, dataSize, filesize, dataStartPos);
                header.SetWzVersionPkg2(hash1, hash2);
                this.header = header;
            }
            else
            {
                goto __failed;
            }

            return true;

        __failed:
            this.header = new Wz_Header(null, null, fileName, 0, 0, filesize, 0);
            return false;
        }

        public uint CalcOffset(uint filePos, uint hashedOffset)
        {
            uint offset = (uint)(filePos - 0x3C) ^ 0xFFFFFFFF;
            int distance;

            offset *= this.Header.HashVersion;
            offset -= 0x581C3F6D;
            distance = (int)offset & 0x1F;
            offset = (offset << distance) | (offset >> (32 - distance));
            offset ^= hashedOffset;
            offset += 0x78;

            return offset;
        }

        // for KMST 1196-1197
        public uint CalcOffsetPkg2V1(uint filePos, uint hashedOffset)
        {
            uint headerLen = (uint)this.header.HeaderSize;
            uint hashVersion = this.header.HashVersion;
            uint hash1 = this.header.Pkg2Hash1;

            uint offset = filePos - headerLen;
            int distance;

            offset = ~offset;
            offset *= hashVersion;
            offset -= 0x581C3F6D;
            offset ^= hash1 * 0x01010101;
            distance = (byte)((hashVersion ^ hash1) & 0x1F);
            offset = (offset << distance) | (offset >> (32 - distance));
            offset ^= hashedOffset;
            offset += headerLen;

            return offset;
        }

        // for KMST 1198
        public uint CalcOffsetPkg2V2(uint filePos, uint hashedOffset)
        {
            uint headerLen = (uint)this.header.HeaderSize;
            uint hashVersion = this.header.HashVersion;
            uint hash1 = this.header.Pkg2Hash1;

            uint offset = filePos - headerLen;
            int distance;

            offset = ~offset;
            offset *= hashVersion ^ hash1;
            offset -= 0x581C3F6D;
            offset ^= hash1 * 0x01010101;
            distance = (byte)((hashVersion ^ hash1) & 0x1F);
            offset = (offset << distance) | (offset >> (32 - distance));
            offset ^= ~hashedOffset;
            offset += headerLen;

            return offset;
        }

        // for KMST 1196-1197
        public int DecryptPkg2EntryCountV1(int encryptedEntryCount)
        {
            uint hash1 = this.header.Pkg2Hash1;
            uint hashVersion = this.header.HashVersion;
            int entryCount = (int)(encryptedEntryCount ^ ((hash1 << 24) + (0x7F4A7C15 * hashVersion)));
            return entryCount;
        }

        // for KMST 1198
        public int DecryptPkg2EntryCountV2(int encryptedEntryCount)
        {
            uint hash1 = this.header.Pkg2Hash1;
            uint hashVersion = this.header.HashVersion;
            int entryCount = (int)(encryptedEntryCount ^ ((hash1 << 16) + (0x21524111 * hashVersion)));
            return entryCount;
        }

        public uint CalcHashVersionFromEntryCountV1(int encryptedEntryCount, int entryCount)
        {
            // calculate with modular inverse:
            uint hash1 = this.header.Pkg2Hash1;
            return ((uint)(entryCount ^ encryptedEntryCount) - (hash1 << 24)) * 0x9937733D;
        }

        public void GetDirTree(Wz_Node parent, bool useBaseWz = false, bool loadWzAsFolder = false, string fileName = null, string fallbackFileName = null)
        {
            var ps = new PartialStream(this.FileStream, this.header.DataStartPosition, this.fileStream.Length - this.header.DataStartPosition, true);
            ps.Position = 0;
            var reader = new WzBinaryReader(ps, false);
            this.GetDirTree(reader, parent, useBaseWz, loadWzAsFolder, fileName, fallbackFileName);
        }

        private void GetDirTree(WzBinaryReader reader, Wz_Node parent, bool useBaseWz = false, bool loadWzAsFolder = false, string fileName = null, string fallbackFileName = null)
        {
            List<string> dirs = new List<string>();

            if (this.header.Signature == Wz_Header.PKG1)
            {
                this.ReadDirTree(reader, parent, ref dirs);
            }
            else if (this.header.Signature == Wz_Header.PKG2)
            {
                this.ReadDirTreePkg2(reader, parent, ref dirs);
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
                    var m = Regex.Match(dirs[i], @"^([A-Za-z]+)$");
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
                                if (!dirs.Take(dirCount).Any(dir => extDirName.Equals(dir, StringComparison.OrdinalIgnoreCase)))
                                {
                                    dirs.Add(extDirName);
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
                                if (!dirs.Take(dirCount).Any(dir => extDirName.Equals(dir, StringComparison.OrdinalIgnoreCase)))
                                {
                                    dirs.Add(extDirName);
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
                string dir = dirs[i];
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
                    catch (Exception ex)
                    {
                    }
                }
            }

            parent.Nodes.Trim();
        }

        private void ReadDirTree(WzBinaryReader reader, Wz_Node parent, ref List<string> dirs)
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
                        Wz_Node childNode = parent.Nodes.Add(name);
                        childNode.Value = img;
                        img.OwnerNode = childNode;
                        this.imageCount++;
                        break;

                    case 0x03:
                        this.directories.Add(new Wz_Directory(name, size, cs32, hashOffset, pos, this));
                        dirs.Add(name);
                        break;
                }
            }
        }

        private void ReadDirTreePkg2(WzBinaryReader reader, Wz_Node parent, ref List<string> dirs)
        {
            if (!this.WzStructure.encryption.IsDirEncDetected(this))
            {
                this.WzStructure.encryption.DetectEncryption(this);
            }

            long dataStartPosition = this.Header.DataStartPosition;
            var encType = this.WzStructure.encryption.Pkg2EncType;
            var pkg1Keys = this.WzStructure.encryption.Pkg1Keys ?? this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.BMS);
            var pkg2Keys = this.WzStructure.encryption.Pkg2Keys ?? Wz_Crypto.Wz_NonOpCryptoKey.Instance;
            int encryptedEntryCount = reader.ReadCompressedInt32();
            int decryptedEntryCountV1 = this.DecryptPkg2EntryCountV1(encryptedEntryCount);
            int decryptedEntryCountV2 = this.DecryptPkg2EntryCountV2(encryptedEntryCount);
            string firstEntrySummary = null;
            var firstEntryDecoderDiagnostics = new List<string>();

            static bool MatchesCompressedIntByte(byte nodeType, int value)
            {
                return value is >= -127 and <= 127 && unchecked((byte)(sbyte)value) == nodeType;
            }

            static bool LooksLikePkg2NodeName(string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    return false;
                }

                if (name.EndsWith(".img", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                foreach (char c in name)
                {
                    if (!(0x20 <= c && c <= 0x7f))
                    {
                        return false;
                    }
                }

                return true;
            }

            bool IsPlausibleNextMarker(byte nextMarker)
            {
                return nextMarker == 0x03
                    || nextMarker == 0x04
                    || nextMarker == 0x80
                    || MatchesCompressedIntByte(nextMarker, encryptedEntryCount)
                    || MatchesCompressedIntByte(nextMarker, decryptedEntryCountV1)
                    || MatchesCompressedIntByte(nextMarker, decryptedEntryCountV2);
            }

            string DescribeProbeResult(string decoderName, string candidateName, int? candidateSize, int? candidateChecksum, byte? nextMarker, string error)
            {
                if (!string.IsNullOrEmpty(error))
                {
                    return $"{decoderName}: error={error}";
                }

                return $"{decoderName}: name={candidateName ?? "<null>"}, size={candidateSize?.ToString() ?? "<n/a>"}, cs32={candidateChecksum?.ToString() ?? "<n/a>"}, nextMarker={(nextMarker.HasValue ? $"0x{nextMarker.Value:X2}" : "<n/a>")}, plausible={(nextMarker.HasValue && IsPlausibleNextMarker(nextMarker.Value))}";
            }

            List<Pkg2DirEntry> entries = new();
            while (true)
            {
                byte nodeType = reader.ReadByte();
                string name;
                if (nodeType == 0x03 || nodeType == 0x04)
                {
                    if (entries.Count == 0 && encType == Wz_CryptoKeyType.Unknown)
                    {
                        long nodeTypeStartPos = reader.BaseStream.Position - 1;
                        long nameStartPos = reader.BaseStream.Position;
                        bool probeMatched = false;

                        bool TryReadNameCandidate(string decoderName, Func<string> readName, out string candidateName)
                        {
                            int? candidateSize = null;
                            int? candidateChecksum = null;
                            byte? nextMarker = null;

                            try
                            {
                                reader.BaseStream.Position = nameStartPos;
                                candidateName = readName();
                                if (!LooksLikePkg2NodeName(candidateName))
                                {
                                    firstEntryDecoderDiagnostics.Add(DescribeProbeResult(decoderName, candidateName, null, null, null, "name rejected"));
                                    return false;
                                }

                                candidateSize = reader.ReadCompressedInt32();
                                candidateChecksum = reader.ReadCompressedInt32();
                                nextMarker = reader.ReadByte();
                                bool plausible = IsPlausibleNextMarker(nextMarker.Value);
                                firstEntryDecoderDiagnostics.Add(DescribeProbeResult(decoderName, candidateName, candidateSize, candidateChecksum, nextMarker, null));
                                return plausible;
                            }
                            catch (Exception ex)
                            {
                                candidateName = null;
                                firstEntryDecoderDiagnostics.Add(DescribeProbeResult(decoderName, candidateName, candidateSize, candidateChecksum, nextMarker, ex.GetType().Name + ": " + ex.Message));
                                return false;
                            }
                        }

                        if (TryReadNameCandidate("Pkg2DirString", () => reader.ReadPkg2DirString(Wz_Crypto.Pkg2DirStringKey.Instance), out string pkg2DirName))
                        {
                            probeMatched = true;
                            reader.BaseStream.Position = nameStartPos;
                            name = reader.ReadPkg2DirString(Wz_Crypto.Pkg2DirStringKey.Instance);
                            encType = Wz_CryptoKeyType.KMST1198;
                            pkg2Keys = Wz_Crypto.Pkg2DirStringKey.Instance;
                        }
                        else if (TryReadNameCandidate("ReadString(BMS)", () => reader.ReadString(this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.BMS)), out string bmsName))
                        {
                            probeMatched = true;
                            reader.BaseStream.Position = nameStartPos;
                            name = reader.ReadString(this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.BMS));
                            encType = Wz_CryptoKeyType.BMS;
                            pkg2Keys = this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.BMS);
                        }
                        else if (TryReadNameCandidate("ReadString(KMS)", () => reader.ReadString(this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.KMS)), out string kmsName))
                        {
                            probeMatched = true;
                            reader.BaseStream.Position = nameStartPos;
                            name = reader.ReadString(this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.KMS));
                            encType = Wz_CryptoKeyType.KMS;
                            pkg2Keys = this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.KMS);
                        }
                        else if (TryReadNameCandidate("ReadString(GMS)", () => reader.ReadString(this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.GMS)), out string gmsName))
                        {
                            probeMatched = true;
                            reader.BaseStream.Position = nameStartPos;
                            name = reader.ReadString(this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.GMS));
                            encType = Wz_CryptoKeyType.GMS;
                            pkg2Keys = this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.GMS);
                        }
                        else
                        {
                            reader.BaseStream.Position = nodeTypeStartPos;
                            break;
                        }
                    }
                    else if (encType == Wz_CryptoKeyType.KMST1198)
                    {
                        name = entries.Count == 0 ? reader.ReadPkg2DirString(pkg2Keys) : reader.ReadString(pkg1Keys);
                    }
                    else
                    {
                        name = reader.ReadString(pkg2Keys);
                    }
                }
                else if (nodeType == 0x80
                    || MatchesCompressedIntByte(nodeType, encryptedEntryCount)
                    || MatchesCompressedIntByte(nodeType, decryptedEntryCountV1)
                    || MatchesCompressedIntByte(nodeType, decryptedEntryCountV2))
                {
                    // next byte is encryptedOffsetCount
                    reader.BaseStream.Position--;
                    break;
                }
                else
                {
                    if (entries.Count == 0)
                    {
                        // Some PKG2 files have no dir-entry block, and the next compressed int is offsetCount.
                        // Treat the first unknown byte as the start of that block instead of failing immediately.
                        reader.BaseStream.Position--;
                        break;
                    }
                    throw new Exception(
                        $"Unknown type {nodeType} (0x{nodeType:X2}) in WzDirTree. " +
                        $"file={this.Header.FileName}, streamPos={reader.BaseStream.Position}, entries={entries.Count}, " +
                        $"encType={encType}, encryptedEntryCount={encryptedEntryCount}, " +
                        $"decryptedEntryCountV1={decryptedEntryCountV1}, decryptedEntryCountV2={decryptedEntryCountV2}, " +
                        $"hashVersion={this.Header.HashVersion}, pkg2Hash1=0x{this.Header.Pkg2Hash1:X8}, " +
                        $"firstEntry={firstEntrySummary ?? "<none>"}, " +
                        $"decoderProbes={string.Join(" | ", firstEntryDecoderDiagnostics)}, " +
                        $"candidateVersions={this.BuildPkg2VersionCandidateSummary(encryptedEntryCount)}, " +
                        $"rawBytes={this.BuildHexWindow(dataStartPosition + reader.BaseStream.Position - 1, 24, 32)}."
                    );
                }

                int size = reader.ReadCompressedInt32();
                int cs32 = reader.ReadCompressedInt32();
                if (entries.Count == 0)
                {
                    firstEntrySummary = $"nodeType=0x{nodeType:X2}, name={name}, size={size}, cs32={cs32}, nextStreamPos={reader.BaseStream.Position}";
                }
                entries.Add(new Pkg2DirEntry
                {
                    NodeType = nodeType,
                    Name = name,
                    DataLength = size,
                    Checksum = cs32
                });
            }

            int encryptedOffsetCount = reader.ReadCompressedInt32();
            if (encryptedOffsetCount == encryptedEntryCount && entries.Count > 0)
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
                            Wz_Node childNode = parent.Nodes.Add(entry.Name);
                            childNode.Value = img;
                            img.OwnerNode = childNode;
                            this.imageCount++;
                            break;

                        case 0x03:
                            this.directories.Add(new Wz_Directory(entry.Name, entry.DataLength, entry.Checksum, hashOffset, pos, this));
                            dirs.Add(entry.Name);
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

        private string BuildHexWindow(long centerPosition, int bytesBefore, int bytesAfter)
        {
            if (!this.fileStream.CanSeek)
            {
                return "<stream is not seekable>";
            }

            long originalPos = this.fileStream.Position;
            try
            {
                long start = Math.Max(0, centerPosition - bytesBefore);
                int byteCount = (int)Math.Min(this.fileStream.Length - start, bytesBefore + bytesAfter + 1);
                if (byteCount <= 0)
                {
                    return "<no bytes available>";
                }

                byte[] buffer = new byte[byteCount];
                this.fileStream.Position = start;
                int read = this.fileStream.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    return "<no bytes read>";
                }

                if (read != buffer.Length)
                {
                    Array.Resize(ref buffer, read);
                }

                int markerIndex = (int)(centerPosition - start);
                var sb = new StringBuilder();
                sb.Append($"start={start}, center={centerPosition}: ");
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(' ');
                    }

                    if (i == markerIndex)
                    {
                        sb.Append('[');
                    }

                    sb.Append(buffer[i].ToString("X2"));

                    if (i == markerIndex)
                    {
                        sb.Append(']');
                    }
                }
                return sb.ToString();
            }
            finally
            {
                this.fileStream.Position = originalPos;
            }
        }

        private string BuildPkg2VersionCandidateSummary(int encryptedEntryCount)
        {
            if (this.header?.Signature != Wz_Header.PKG2)
            {
                return "<not pkg2>";
            }

            var results = new List<string>();
            this.header.ResetVersionDetector();
            while (this.header.TryGetNextVersion())
            {
                uint hashVersion = this.header.HashVersion;
                int wzVersion = this.header.WzVersion;
                int candidateEntryCountV1 = this.DecryptPkg2EntryCountV1(encryptedEntryCount);
                int candidateEntryCountV2 = this.DecryptPkg2EntryCountV2(encryptedEntryCount);
                results.Add($"wzVersion={wzVersion}, hashVersion=0x{hashVersion:X8}, decV1={candidateEntryCountV1}, decV2={candidateEntryCountV2}");
            }
            this.header.ResetVersionDetector();

            return results.Count > 0 ? string.Join(" | ", results) : "<no candidates>";
        }

        public string BuildPkg2OffsetCandidateReport(int maxCandidateVersions = 12, int maxOffsetsToSample = 16)
        {
            if (this.header?.Signature != Wz_Header.PKG2)
            {
                return "pkg2 offset candidates: <not pkg2>";
            }

            long originalPosition = this.fileStream.Position;
            try
            {
                long payloadOffset = Math.Max(0, Math.Max(this.Header.DataStartPosition, this.Header.DirEndPosition));
                if (payloadOffset >= this.fileStream.Length)
                {
                    return "pkg2 offset candidates: <payload offset out of range>";
                }

                this.fileStream.Position = payloadOffset;
                var reader = new WzBinaryReader(this.fileStream, false);
                int encryptedOffsetCount = reader.ReadCompressedInt32();
                long offsetTableStart = this.fileStream.Position;
                List<string> results = this.BuildPkg2OffsetCandidateLines(encryptedOffsetCount, offsetTableStart, maxCandidateVersions, maxOffsetsToSample);
                string nearbyStarts = this.BuildPkg2NearbyOffsetStartReport(payloadOffset, 96, maxCandidateVersions, maxOffsetsToSample, 12);
                string repeatedMarkerReport = this.BuildPkg2RepeatedCountMarkerReport(this.Header.DataStartPosition, 0x1000, 4);

                this.header.ResetVersionDetector();
                return "pkg2 encrypted offset count: " + encryptedOffsetCount
                    + "\r\npkg2 offset table start: " + offsetTableStart
                    + "\r\npkg2 offset candidates:\r\n"
                    + (results.Count > 0 ? string.Join("\r\n", results) : "<none>")
                    + "\r\npkg2 nearby count-field candidates:\r\n"
                    + nearbyStarts
                    + "\r\npkg2 repeated entry-count markers:\r\n"
                    + repeatedMarkerReport;
            }
            finally
            {
                this.fileStream.Position = originalPosition;
                this.header.ResetVersionDetector();
            }
        }

        private List<string> BuildPkg2OffsetCandidateLines(int encryptedOffsetCount, long offsetTableStart, int maxCandidateVersions, int maxOffsetsToSample)
        {
            var results = new List<string>();
            int testedCandidates = 0;

            for (int cryptoVersion = 2; cryptoVersion >= 1; cryptoVersion--)
            {
                this.header.ResetVersionDetector();
                while (this.header.TryGetNextVersion())
                {
                    if (++testedCandidates > maxCandidateVersions)
                    {
                        results.Add("<candidate limit reached>");
                        this.header.ResetVersionDetector();
                        return results;
                    }

                    uint hashVersion = this.header.HashVersion;
                    int wzVersion = this.header.WzVersion;
                    int offsetCount = cryptoVersion == 1
                        ? this.DecryptPkg2EntryCountV1(encryptedOffsetCount)
                        : this.DecryptPkg2EntryCountV2(encryptedOffsetCount);

                    results.Add(this.BuildPkg2OffsetCandidateLine(cryptoVersion, wzVersion, hashVersion, offsetCount, offsetTableStart, maxOffsetsToSample, out _));
                }
            }

            this.header.ResetVersionDetector();
            return results;
        }

        private string BuildPkg2NearbyOffsetStartReport(long payloadOffset, int scanByteCount, int maxCandidateVersions, int maxOffsetsToSample, int maxResults)
        {
            long scanStart = Math.Max(this.Header.DataStartPosition, payloadOffset - 16);
            long scanEnd = Math.Min(this.fileStream.Length - 1, payloadOffset + scanByteCount);
            var results = new List<string>();

            for (long position = scanStart; position <= scanEnd && results.Count < maxResults; position++)
            {
                if (!this.TryReadCompressedInt32At(position, out int encryptedOffsetCount, out int encodedSize))
                {
                    continue;
                }

                long offsetTableStart = position + encodedSize;
                if (offsetTableStart + 4 > this.fileStream.Length)
                {
                    continue;
                }

                int testedCandidates = 0;
                for (int cryptoVersion = 2; cryptoVersion >= 1 && results.Count < maxResults; cryptoVersion--)
                {
                    this.header.ResetVersionDetector();
                    while (this.header.TryGetNextVersion() && results.Count < maxResults)
                    {
                        if (++testedCandidates > maxCandidateVersions)
                        {
                            break;
                        }

                        uint hashVersion = this.header.HashVersion;
                        int wzVersion = this.header.WzVersion;
                        int offsetCount = cryptoVersion == 1
                            ? this.DecryptPkg2EntryCountV1(encryptedOffsetCount)
                            : this.DecryptPkg2EntryCountV2(encryptedOffsetCount);

                        if (offsetCount < 32 || offsetCount > 0x10000)
                        {
                            continue;
                        }

                        string line = this.BuildPkg2OffsetCandidateLine(cryptoVersion, wzVersion, hashVersion, offsetCount, offsetTableStart, maxOffsetsToSample, out bool strongCandidate);
                        if (strongCandidate)
                        {
                            results.Add($"start=0x{position:X}, enc={encryptedOffsetCount}, size={encodedSize}, table=0x{offsetTableStart:X}, {line}");
                        }
                    }
                }
            }

            this.header.ResetVersionDetector();
            return results.Count > 0
                ? string.Join("\r\n", results)
                : $"<none in window 0x{scanStart:X}-0x{scanEnd:X}>";
        }

        private string BuildPkg2OffsetCandidateLine(int cryptoVersion, int wzVersion, uint hashVersion, int offsetCount, long offsetTableStart, int maxOffsetsToSample, out bool strongCandidate)
        {
            strongCandidate = false;
            if (offsetCount <= 0 || offsetCount > 0x10000)
            {
                return $"cryptoV{cryptoVersion}, wz={wzVersion}, hash=0x{hashVersion:X8}, offsetCount={offsetCount}, skipped=invalid-count";
            }

            if (offsetTableStart + offsetCount * 4L > this.fileStream.Length)
            {
                return $"cryptoV{cryptoVersion}, wz={wzVersion}, hash=0x{hashVersion:X8}, offsetCount={offsetCount}, skipped=table-out-of-range";
            }

            this.fileStream.Position = offsetTableStart;
            var reader = new WzBinaryReader(this.fileStream, false);
            int sampleCount = Math.Min(offsetCount, maxOffsetsToSample);
            int plausibleCount = 0;
            bool monotonic = true;
            bool inBounds = true;
            uint previousOffset = 0;
            var samples = new List<string>();

            for (int i = 0; i < sampleCount; i++)
            {
                uint filePos = (uint)this.fileStream.Position;
                uint hashOffset = reader.ReadUInt32();
                uint actualOffset = cryptoVersion == 1
                    ? this.CalcOffsetPkg2V1(filePos, hashOffset)
                    : this.CalcOffsetPkg2V2(filePos, hashOffset);

                if (actualOffset < this.Header.DataStartPosition || actualOffset >= this.fileStream.Length)
                {
                    inBounds = false;
                }

                if (i > 0 && actualOffset <= previousOffset)
                {
                    monotonic = false;
                }
                previousOffset = actualOffset;

                bool plausible = this.TryDescribeStandaloneImagePayload(actualOffset, out string marker);
                if (plausible)
                {
                    plausibleCount++;
                }

                if (samples.Count < 4)
                {
                    samples.Add($"{i:D3}@0x{actualOffset:X8}:{marker}");
                }
            }

            strongCandidate = inBounds && monotonic && (plausibleCount > 0 || sampleCount >= 4);
            return $"cryptoV{cryptoVersion}, wz={wzVersion}, hash=0x{hashVersion:X8}, offsetCount={offsetCount}, plausible={plausibleCount}/{sampleCount}, monotonic={monotonic}, inBounds={inBounds}, samples={string.Join(", ", samples)}";
        }

        private bool TryReadCompressedInt32At(long position, out int value, out int encodedSize)
        {
            value = 0;
            encodedSize = 0;
            if (position < 0 || position >= this.fileStream.Length)
            {
                return false;
            }

            long originalPosition = this.fileStream.Position;
            try
            {
                this.fileStream.Position = position;
                int first = this.fileStream.ReadByte();
                if (first < 0)
                {
                    return false;
                }

                sbyte signedFirst = unchecked((sbyte)(byte)first);
                if (signedFirst != -128)
                {
                    value = signedFirst;
                    encodedSize = 1;
                    return true;
                }

                if (position + 5 > this.fileStream.Length)
                {
                    return false;
                }

                byte[] buffer = new byte[4];
                int read = this.fileStream.Read(buffer, 0, buffer.Length);
                if (read != buffer.Length)
                {
                    return false;
                }

                value = BitConverter.ToInt32(buffer, 0);
                encodedSize = 5;
                return true;
            }
            finally
            {
                this.fileStream.Position = originalPosition;
            }
        }

        private string BuildPkg2RepeatedCountMarkerReport(long searchStart, int maxSearchBytes, int maxMatches)
        {
            if (!this.TryReadCompressedInt32At(searchStart, out _, out int encodedSize))
            {
                return "<failed to read initial marker>";
            }

            byte[] markerBytes = new byte[encodedSize];
            long originalPosition = this.fileStream.Position;
            try
            {
                this.fileStream.Position = searchStart;
                if (this.fileStream.Read(markerBytes, 0, markerBytes.Length) != markerBytes.Length)
                {
                    return "<failed to read initial marker bytes>";
                }
            }
            finally
            {
                this.fileStream.Position = originalPosition;
            }

            long scanStart = Math.Max(this.Header.DataStartPosition, searchStart + encodedSize);
            long scanEnd = Math.Min(this.fileStream.Length - markerBytes.Length, searchStart + maxSearchBytes);
            var results = new List<string>();
            string markerHex = BitConverter.ToString(markerBytes).Replace("-", " ");

            for (long position = scanStart; position <= scanEnd && results.Count < maxMatches; position++)
            {
                if (!this.MatchesBytesAt(position, markerBytes))
                {
                    continue;
                }

                long tableStart = position + markerBytes.Length;
                long payloadMarkerOffset = this.FindNextLikelyImageRootOffset(tableStart, 0x400, out string payloadMarker);
                string entryCountHint = "unknown";
                if (payloadMarkerOffset >= 0 && payloadMarkerOffset > tableStart)
                {
                    long tableBytes = payloadMarkerOffset - tableStart;
                    if (tableBytes % 4 == 0)
                    {
                        entryCountHint = (tableBytes / 4).ToString();
                    }
                    else
                    {
                        entryCountHint = $"non-aligned({tableBytes} bytes)";
                    }
                }

                int hintedOffsetCount;
                bool hasHintedOffsetCount = int.TryParse(entryCountHint, out hintedOffsetCount);

                uint hashOffset0 = 0;
                string hashOffsetText = "<out-of-range>";
                if (tableStart + 4 <= this.fileStream.Length)
                {
                    long savedPosition = this.fileStream.Position;
                    try
                    {
                        this.fileStream.Position = tableStart;
                        byte[] buffer = new byte[4];
                        if (this.fileStream.Read(buffer, 0, buffer.Length) == buffer.Length)
                        {
                            hashOffset0 = BitConverter.ToUInt32(buffer, 0);
                            hashOffsetText = "0x" + hashOffset0.ToString("X8");
                        }
                    }
                    finally
                    {
                        this.fileStream.Position = savedPosition;
                    }
                }

                results.Add($"marker=0x{position:X}, table=0x{tableStart:X}, firstHash={hashOffsetText}, nextRoot={(payloadMarkerOffset >= 0 ? $"0x{payloadMarkerOffset:X}:{payloadMarker}" : "<none>")}, entryCountHint={entryCountHint}");
                if (hasHintedOffsetCount && hintedOffsetCount > 0)
                {
                    string exactTableReport = this.BuildPkg2ForcedOffsetCandidateReport(tableStart, hintedOffsetCount, 6, 8);
                    if (!string.IsNullOrEmpty(exactTableReport))
                    {
                        foreach (string line in exactTableReport.Split(new[] { "\r\n" }, StringSplitOptions.None))
                        {
                            results.Add("  " + line);
                        }
                    }
                }
            }

            string header = $"probeVersion=exact-marker-v2, initialMarker=0x{searchStart:X}, markerBytes={markerHex}, scanWindow=0x{scanStart:X}-0x{scanEnd:X}";
            return results.Count > 0
                ? header + "\r\n" + string.Join("\r\n", results)
                : header + "\r\n" + $"<none in window 0x{scanStart:X}-0x{scanEnd:X}>";
        }

        private string BuildPkg2ForcedOffsetCandidateReport(long offsetTableStart, int offsetCount, int maxCandidateVersions, int maxOffsetsToSample)
        {
            if (offsetCount <= 0)
            {
                return null;
            }

            var results = new List<string>
            {
                $"forcedCount={offsetCount}, table=0x{offsetTableStart:X}"
            };

            int testedCandidates = 0;
            for (int cryptoVersion = 2; cryptoVersion >= 1; cryptoVersion--)
            {
                this.header.ResetVersionDetector();
                while (this.header.TryGetNextVersion())
                {
                    if (++testedCandidates > maxCandidateVersions)
                    {
                        results.Add("<candidate limit reached>");
                        this.header.ResetVersionDetector();
                        return string.Join("\r\n", results);
                    }

                    uint hashVersion = this.header.HashVersion;
                    int wzVersion = this.header.WzVersion;
                    results.Add(this.BuildPkg2OffsetCandidateLine(cryptoVersion, wzVersion, hashVersion, offsetCount, offsetTableStart, maxOffsetsToSample, out _));
                }
            }

            this.header.ResetVersionDetector();
            return string.Join("\r\n", results);
        }

        private bool TryDetectUnsupportedPkg2OffsetTransform(long searchStart, int maxSearchBytes, out string reason)
        {
            reason = null;
            if (this.header?.Signature != Wz_Header.PKG2)
            {
                return false;
            }

            if (!this.TryReadCompressedInt32At(searchStart, out _, out int encodedSize))
            {
                return false;
            }

            byte[] markerBytes = new byte[encodedSize];
            long originalPosition = this.fileStream.Position;
            try
            {
                this.fileStream.Position = searchStart;
                if (this.fileStream.Read(markerBytes, 0, markerBytes.Length) != markerBytes.Length)
                {
                    return false;
                }
            }
            finally
            {
                this.fileStream.Position = originalPosition;
            }

            long scanStart = Math.Max(this.Header.DataStartPosition, searchStart + encodedSize);
            long scanEnd = Math.Min(this.fileStream.Length - markerBytes.Length, searchStart + maxSearchBytes);
            for (long position = scanStart; position <= scanEnd; position++)
            {
                if (!this.MatchesBytesAt(position, markerBytes))
                {
                    continue;
                }

                long tableStart = position + markerBytes.Length;
                long payloadMarkerOffset = this.FindNextLikelyImageRootOffset(tableStart, 0x400, out string payloadMarker);
                if (payloadMarkerOffset <= tableStart)
                {
                    continue;
                }

                long tableBytes = payloadMarkerOffset - tableStart;
                if (tableBytes % 4 != 0)
                {
                    continue;
                }

                int offsetCount = (int)(tableBytes / 4);
                if (offsetCount <= 0 || offsetCount > 0x10000)
                {
                    continue;
                }

                if (this.HasAnySupportedPkg2OffsetCandidate(tableStart, offsetCount, 6, 8))
                {
                    continue;
                }

                reason = $"Unsupported PKG2 offset transform detected: repeated marker at 0x{position:X}, table=0x{tableStart:X}, offsetCount={offsetCount}, nextRoot=0x{payloadMarkerOffset:X}:{payloadMarker}.";
                return true;
            }

            return false;
        }

        private bool HasAnySupportedPkg2OffsetCandidate(long offsetTableStart, int offsetCount, int maxCandidateVersions, int maxOffsetsToSample)
        {
            int testedCandidates = 0;
            for (int cryptoVersion = 2; cryptoVersion >= 1; cryptoVersion--)
            {
                this.header.ResetVersionDetector();
                while (this.header.TryGetNextVersion())
                {
                    if (++testedCandidates > maxCandidateVersions)
                    {
                        this.header.ResetVersionDetector();
                        return false;
                    }

                    _ = this.BuildPkg2OffsetCandidateLine(cryptoVersion, this.header.WzVersion, this.header.HashVersion, offsetCount, offsetTableStart, maxOffsetsToSample, out bool strongCandidate);
                    if (strongCandidate)
                    {
                        this.header.ResetVersionDetector();
                        return true;
                    }
                }
            }

            this.header.ResetVersionDetector();
            return false;
        }

        private void MarkUnsupportedPkg2OffsetTransform(string reason)
        {
            this.header.Capabilities |= Wz_Capabilities.UnsupportedPkg2OffsetTransform;
            this.pkg2UnsupportedReason = reason;
        }

        public bool TryGetPkg2UnsupportedReason(out string reason)
        {
            if (!string.IsNullOrEmpty(this.pkg2UnsupportedReason))
            {
                reason = this.pkg2UnsupportedReason;
                return true;
            }

            if (this.header.Signature != Wz_Header.PKG2)
            {
                reason = null;
                return false;
            }

            if (this.header.HasCapabilities(Wz_Capabilities.UnsupportedPkg2OffsetTransform))
            {
                reason = string.IsNullOrEmpty(this.pkg2UnsupportedReason)
                    ? "Unsupported PKG2 offset transform."
                    : this.pkg2UnsupportedReason;
                return true;
            }

            if (this.TryDetectUnsupportedPkg2OffsetTransform(this.Header.DataStartPosition, 0x1000, out string detectedReason))
            {
                this.MarkUnsupportedPkg2OffsetTransform(detectedReason);
                reason = detectedReason;
                return true;
            }

            reason = null;
            return false;
        }

        private bool IsStandaloneImageRootOffset(long dataOffset)
        {
            if (dataOffset < 0 || dataOffset >= this.fileStream.Length)
            {
                return false;
            }

            long originalPosition = this.fileStream.Position;
            try
            {
                this.fileStream.Position = dataOffset;
                int count = (int)Math.Min(9, this.fileStream.Length - dataOffset);
                if (count <= 0)
                {
                    return false;
                }

                byte[] buffer = new byte[count];
                int read = this.fileStream.Read(buffer, 0, count);
                if (read <= 0)
                {
                    return false;
                }

                if (read != buffer.Length)
                {
                    Array.Resize(ref buffer, read);
                }

                return buffer[0] == 0x73
                    || buffer[0] == 0x1B
                    || (buffer.Length >= 9 && Encoding.ASCII.GetString(buffer, 0, 9) == "#Property")
                    || (buffer.Length >= 4 && Encoding.ASCII.GetString(buffer, 0, 4) == "Root");
            }
            finally
            {
                this.fileStream.Position = originalPosition;
            }
        }

        private bool TryGetPkg2StandaloneImageRootOffset(long searchStart, int maxSearchBytes, out long dataOffset)
        {
            dataOffset = 0;
            if (this.header?.Signature != Wz_Header.PKG2)
            {
                return false;
            }

            long originalPosition = this.fileStream.Position;
            try
            {
                if (!this.TryReadCompressedInt32At(searchStart, out _, out int encodedSize))
                {
                    return false;
                }

                byte[] markerBytes = new byte[encodedSize];
                this.fileStream.Position = searchStart;
                if (this.fileStream.Read(markerBytes, 0, markerBytes.Length) != markerBytes.Length)
                {
                    return false;
                }

                long scanStart = Math.Max(this.Header.DataStartPosition, searchStart + encodedSize);
                long scanEnd = Math.Min(this.fileStream.Length - markerBytes.Length, searchStart + maxSearchBytes);
                for (long position = scanStart; position <= scanEnd; position++)
                {
                    if (!this.MatchesBytesAt(position, markerBytes))
                    {
                        continue;
                    }

                    long tableStart = position + markerBytes.Length;
                    long payloadRootOffset = this.FindNextLikelyImageRootOffset(tableStart, 0x400, out _);
                    if (payloadRootOffset <= tableStart)
                    {
                        continue;
                    }

                    long tableBytes = payloadRootOffset - tableStart;
                    if (tableBytes % 4 != 0)
                    {
                        continue;
                    }

                    int offsetCount = (int)(tableBytes / 4);
                    if (offsetCount <= 0 || offsetCount > 0x10000)
                    {
                        continue;
                    }

                    if (!this.IsStandaloneImageRootOffset(payloadRootOffset))
                    {
                        continue;
                    }

                    dataOffset = payloadRootOffset;
                    return true;
                }

                return false;
            }
            finally
            {
                this.fileStream.Position = originalPosition;
            }
        }

        private bool TryGetStandaloneImageDataOffset(out long dataOffset)
        {
            dataOffset = Math.Max(0, Math.Max(this.Header.DataStartPosition, this.Header.DirEndPosition));

            long originalPosition = this.fileStream.Position;
            try
            {
                if (this.IsStandaloneImageRootOffset(dataOffset))
                {
                    return true;
                }

                if (this.header?.Signature == Wz_Header.PKG2
                    && this.TryGetPkg2StandaloneImageRootOffset(this.Header.DataStartPosition, 0x1000, out long pkg2DataOffset)
                    && this.IsStandaloneImageRootOffset(pkg2DataOffset))
                {
                    dataOffset = pkg2DataOffset;
                    return true;
                }

                return false;
            }
            finally
            {
                this.fileStream.Position = originalPosition;
            }
        }

        private bool MatchesBytesAt(long position, byte[] expected)
        {
            if (expected == null || expected.Length == 0 || position < 0 || position + expected.Length > this.fileStream.Length)
            {
                return false;
            }

            long originalPosition = this.fileStream.Position;
            try
            {
                this.fileStream.Position = position;
                byte[] buffer = new byte[expected.Length];
                int read = this.fileStream.Read(buffer, 0, buffer.Length);
                if (read != expected.Length)
                {
                    return false;
                }

                for (int i = 0; i < expected.Length; i++)
                {
                    if (buffer[i] != expected[i])
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                this.fileStream.Position = originalPosition;
            }
        }

        private long FindNextLikelyImageRootOffset(long startOffset, int maxSearchBytes, out string marker)
        {
            marker = null;
            if (startOffset < 0 || startOffset >= this.fileStream.Length)
            {
                return -1;
            }

            long originalPosition = this.fileStream.Position;
            try
            {
                long endOffset = Math.Min(this.fileStream.Length, startOffset + maxSearchBytes);
                int byteCount = (int)(endOffset - startOffset);
                if (byteCount <= 0)
                {
                    return -1;
                }

                byte[] buffer = new byte[byteCount];
                this.fileStream.Position = startOffset;
                int read = this.fileStream.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    return -1;
                }

                for (int i = 0; i < read; i++)
                {
                    byte b = buffer[i];
                    if (b == 0x73 || b == 0x1B)
                    {
                        marker = "0x" + b.ToString("X2");
                        return startOffset + i;
                    }

                    if (i + 9 <= read && Encoding.ASCII.GetString(buffer, i, 9) == "#Property")
                    {
                        marker = "#Property";
                        return startOffset + i;
                    }

                    if (i + 4 <= read && Encoding.ASCII.GetString(buffer, i, 4) == "Root")
                    {
                        marker = "Root";
                        return startOffset + i;
                    }
                }

                return -1;
            }
            finally
            {
                this.fileStream.Position = originalPosition;
            }
        }

        private bool TryDescribeStandaloneImagePayload(uint offset, out string marker)
        {
            marker = "out-of-range";
            if (offset >= this.fileStream.Length)
            {
                return false;
            }

            long originalPosition = this.fileStream.Position;
            try
            {
                this.fileStream.Position = offset;
                int count = (int)Math.Min(9, this.fileStream.Length - offset);
                if (count <= 0)
                {
                    marker = "empty";
                    return false;
                }

                byte[] buffer = new byte[count];
                int read = this.fileStream.Read(buffer, 0, count);
                if (read <= 0)
                {
                    marker = "empty";
                    return false;
                }

                if (read != buffer.Length)
                {
                    Array.Resize(ref buffer, read);
                }

                if (buffer[0] == 0x73)
                {
                    marker = "0x73";
                    return true;
                }

                if (buffer[0] == 0x1B)
                {
                    marker = "0x1B";
                    return true;
                }

                if (buffer.Length >= 9 && Encoding.ASCII.GetString(buffer, 0, 9) == "#Property")
                {
                    marker = "#Property";
                    return true;
                }

                if (buffer.Length >= 4 && Encoding.ASCII.GetString(buffer, 0, 4) == "Root")
                {
                    marker = "Root";
                    return true;
                }

                marker = "0x" + buffer[0].ToString("X2");
                return false;
            }
            finally
            {
                this.fileStream.Position = originalPosition;
            }
        }

        private bool IsLikelySkillFileName(string wzName)
        {
            if (string.IsNullOrEmpty(wzName))
            {
                return false;
            }

            wzName = Path.GetFileNameWithoutExtension(wzName);
            return wzName.StartsWith("Skill", StringComparison.OrdinalIgnoreCase);
        }

        private bool HasLikelySkillRootNodes()
        {
            return this.node?.Nodes?.Any(child =>
                string.Equals(child.Text, "RidingSkillInfo.img", StringComparison.OrdinalIgnoreCase)
                || Regex.IsMatch(child.Text, @"^Recipe_\d+\.img$", RegexOptions.IgnoreCase)
                || Regex.IsMatch(child.Text, @"^\d+\.img$", RegexOptions.IgnoreCase)) == true;
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
                || this.node.Nodes["Recipe_9200.img"] != null
                || this.node.Nodes["RidingSkillInfo.img"] != null)
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

                if (this.IsLikelySkillFileName(wzName) || this.HasLikelySkillRootNodes())
                {
                    this.type = Wz_Type.Skill;
                    return;
                }

                Match m = Regex.Match(wzName, @"^([A-Za-z]+)_?(\d+)?(?:\.wz)?$");
                if (m.Success)
                {
                    wzName = m.Result("$1");
                }
                this.type = Enum.TryParse<Wz_Type>(wzName, true, out var result) ? result : Wz_Type.Unknown;
            }
        }

        public void DetectWzVersion()
        {
            IWzVersionVerifier wzVersionVerifier;

            if (this.Header.Signature == Wz_Header.PKG1)
            {
                switch (this.wzStructure?.WzVersionVerifyMode)
                {
                    default:
                    case WzVersionVerifyMode.Default:
                        wzVersionVerifier = new DefaultVersionVerifier();
                        break;

                    case WzVersionVerifyMode.Fast:
                        wzVersionVerifier = new FastVersionVerifier();
                        break;
                }
            }
            else if (this.header.Signature == Wz_Header.PKG2)
            {
                wzVersionVerifier = new Pkg2VersionVerifier();
            }
            else
            {
                throw new Exception($"Unknown signature: {this.header.Signature}");
            }

            wzVersionVerifier.Verify(this);
        }

        internal bool RetryParsePkg2TreeWithCandidateVersions(bool useBaseWz = false, string fileName = null, string fallbackFileName = null)
        {
            if (this.Header?.Signature != Wz_Header.PKG2 || this.node == null)
            {
                return false;
            }

            if (this.node.Nodes.Count > 0 || this.imageCount > 0 || this.directories.Count > 0)
            {
                return false;
            }

            long originalDirEndPosition = this.Header.DirEndPosition;
            this.header.ResetVersionDetector();
            while (this.header.TryGetNextVersion())
            {
                ResetParsedTreeState();

                var tempNode = new Wz_Node(this.node.Text)
                {
                    Value = this.node.Value
                };

                this.FileStream.Position = this.Header.DataStartPosition;
                this.GetDirTree(tempNode, useBaseWz, false, fileName, fallbackFileName);
                long dirEndPosition = this.FileStream.Position;

                if (tempNode.Nodes.Count > 0 || this.imageCount > 0 || this.directories.Count > 0)
                {
                    var children = tempNode.Nodes.ToList();
                    tempNode.Nodes.Clear();
                    foreach (var child in children)
                    {
                        this.node.Nodes.Add(child);
                    }

                    this.Header.DirEndPosition = dirEndPosition;
                    return true;
                }
            }

            ResetParsedTreeState();
            this.Header.DirEndPosition = originalDirEndPosition;
            this.header.ResetVersionDetector();
            return false;
        }

        private void ResetParsedTreeState()
        {
            this.imageCount = 0;
            this.directories.Clear();
            this.node?.Nodes.Clear();
        }

        public void MergeWzFile(Wz_File wz_File)
        {
            wz_File.isSubDir = true;
            if (wz_File.node.Nodes.Count > 0)
            {
                var children = wz_File.node.Nodes.ToList();
                wz_File.node.Nodes.Clear();
                foreach (var child in children)
                {
                    this.node.Nodes.Add(child);
                }
            }
            else
            {
                this.node.Nodes.Add(this.CreateMergedWholeFileNode(wz_File));
            }

            if (this.mergedWzFiles == null)
            {
                this.mergedWzFiles = new List<Wz_File>();
            }
            this.mergedWzFiles.Add(wz_File);

            wz_File.ownerWzFile = this;
        }

        private Wz_Node CreateMergedWholeFileNode(Wz_File wzFile)
        {
            return this.ShouldExposeMergedWholeFileAsImage(wzFile)
                ? this.CreateMergedWholeFileImageNode(wzFile)
                : this.CreateMergedWholeFileShardNode(wzFile);
        }

        private Wz_Node CreateMergedWholeFileImageNode(Wz_File wzFile)
        {
            string nodeName = this.GetMergedSubFileNodeName(wzFile, true);
            var childNode = new Wz_Node(nodeName);
            long dataOffset = Math.Max(0, Math.Max(wzFile.Header.DataStartPosition, wzFile.Header.DirEndPosition));
            if (wzFile.TryGetStandaloneImageDataOffset(out long effectiveDataOffset))
            {
                dataOffset = effectiveDataOffset;
            }

            long dataSize = Math.Max(0, wzFile.FileStream.Length - dataOffset);
            int imageSize = (int)Math.Min(int.MaxValue, Math.Max(0, dataSize));
            var img = new Wz_Image(nodeName, imageSize, 0, 0, 0, wzFile)
            {
                OwnerNode = childNode,
                Offset = dataOffset,
                IsChecksumChecked = true
            };

            childNode.Value = img;
            return childNode;
        }

        private Wz_Node CreateMergedWholeFileShardNode(Wz_File wzFile)
        {
            string nodeName = this.GetMergedSubFileNodeName(wzFile, false);
            var childNode = new Wz_Node(nodeName)
            {
                Value = wzFile
            };
            wzFile.Node = childNode;
            return childNode;
        }

        private bool ShouldExposeMergedWholeFileAsImage(Wz_File wzFile)
        {
            return wzFile.TryGetStandaloneImageDataOffset(out _);
        }

        private string GetMergedSubFileNodeName(Wz_File wzFile, bool asImage)
        {
            string fileName = Path.GetFileNameWithoutExtension(wzFile?.Header?.FileName);
            string parentName = this.node?.Text;

            if (string.IsNullOrEmpty(fileName))
            {
                return wzFile?.node?.Text;
            }

            if (!string.IsNullOrEmpty(parentName) && parentName.EndsWith(".wz", StringComparison.OrdinalIgnoreCase))
            {
                parentName = Path.GetFileNameWithoutExtension(parentName);
            }

            if (!string.IsNullOrEmpty(parentName)
                && fileName.StartsWith(parentName + "_", StringComparison.OrdinalIgnoreCase))
            {
                string suffix = fileName.Substring(parentName.Length + 1);
                if (suffix.Length > 0 && suffix.All(char.IsDigit))
                {
                    return suffix + (asImage ? ".img" : ".wz");
                }
            }

            return Path.GetFileName(wzFile.Header.FileName);
        }


        public interface IWzVersionVerifier
        {
            bool Verify(Wz_File wzFile);
        }

        public abstract class WzVersionVerifier
        {
            protected abstract uint CalcOffset(Wz_File wzFile, uint filePos, uint hashedOffset);

            protected IEnumerable<Wz_Image> EnumerableAllWzImage(Wz_Node parentNode)
            {
                foreach (var node in parentNode.Nodes)
                {
                    Wz_Image img = node.Value as Wz_Image;
                    if (img != null)
                    {
                        yield return img;
                    }

                    if (!(node.Value is Wz_File) && node.Nodes.Count > 0)
                    {
                        foreach (var imgChild in EnumerableAllWzImage(node))
                        {
                            yield return imgChild;
                        }
                    }
                }
            }

            protected bool FastCheckFirstByte(Wz_Image image, byte firstByte)
            {
                if (image.IsLuaImage)
                {
                    // for lua image, the first byte is always 01
                    return firstByte == 0x01;
                }
                else
                {
                    // first element is always a string
                    return firstByte == 0x73 || firstByte == 0x1b;
                }
            }

            protected void CalcOffset(Wz_File wzFile, IEnumerable<Wz_Image> imgList)
            {
                foreach (var img in imgList)
                {
                    img.Offset = this.CalcOffset(wzFile, img.HashedOffsetPosition, img.HashedOffset);
                }
            }

            protected bool DetectWithWzImage(Wz_File wzFile, Wz_Image testWzImg)
            {
                while (wzFile.header.TryGetNextVersion())
                {
                    uint offs = this.CalcOffset(wzFile, testWzImg.HashedOffsetPosition, testWzImg.HashedOffset);

                    if (offs < wzFile.header.DirEndPosition || offs + testWzImg.Size > wzFile.fileStream.Length)  //img offset out of file size
                    {
                        continue;
                    }

                    wzFile.fileStream.Position = offs;
                    var firstByte = (byte)wzFile.fileStream.ReadByte();
                    if (!FastCheckFirstByte(testWzImg, firstByte))
                    {
                        continue;
                    }

                    testWzImg.Offset = offs;
                    if (!testWzImg.TryExtract())
                    {
                        continue;
                    }

                    testWzImg.Unextract();
                    wzFile.header.VersionChecked = true;
                    break;
                }

                return wzFile.header.VersionChecked;
            }

            protected bool DetectWithAllWzDir(Wz_File wzFile)
            {
                while (wzFile.header.TryGetNextVersion())
                {
                    bool isSuccess = wzFile.directories.All(testDir =>
                    {
                        uint offs = this.CalcOffset(wzFile, testDir.HashedOffsetPosition, testDir.HashedOffset);

                        if (offs < wzFile.header.DataStartPosition || offs + 1 > wzFile.header.DirEndPosition) // dir offset out of file size.
                        {
                            return false;
                        }

                        wzFile.fileStream.Position = offs;
                        if (wzFile.fileStream.ReadByte() != 0) // for splitted wz format, dir data only contains one byte: 0x00
                        {
                            return false;
                        }

                        return true;
                    });

                    if (isSuccess)
                    {
                        wzFile.header.VersionChecked = true;
                        break;
                    }
                }

                return wzFile.header.VersionChecked;
            }

            protected bool FastDetectWithAllWzImages(Wz_File wzFile, IList<Wz_Image> imgList)
            {
                var imageSizes = new SizeRange[imgList.Count];
                while (wzFile.header.TryGetNextVersion())
                {
                    int count = 0;
                    bool isSuccess = imgList.All(img =>
                    {
                        uint offs = this.CalcOffset(wzFile, img.HashedOffsetPosition, img.HashedOffset);
                        if (offs < wzFile.header.DirEndPosition || offs + img.Size > wzFile.fileStream.Length)  //img offset out of file size
                        {
                            return false;
                        }

                        imageSizes[count++] = new SizeRange()
                        {
                            Start = offs,
                            End = offs + img.Size,
                        };
                        return true;
                    });

                    if (isSuccess)
                    {
                        // check if there's any image overlaps with another image.
                        Array.Sort(imageSizes, 0, count);
                        for (int i = 1; i < count; i++)
                        {
                            if (imageSizes[i - 1].End > imageSizes[i].Start)
                            {
                                isSuccess = false;
                                break;
                            }
                        }

                        if (isSuccess)
                        {
                            wzFile.header.VersionChecked = true;
                            break;
                        }
                    }
                }

                return wzFile.header.VersionChecked;
            }

            private struct SizeRange : IComparable<SizeRange>
            {
                public long Start;
                public long End;

                public int CompareTo(SizeRange sr)
                {
                    int result = this.Start.CompareTo(sr.Start);
                    if (result == 0)
                    {
                        result = this.End.CompareTo(sr.End);
                    }
                    return result;
                }
            }
        }

        public class DefaultVersionVerifier : WzVersionVerifier, IWzVersionVerifier
        {
            public bool Verify(Wz_File wzFile)
            {
                List<Wz_Image> imgList = EnumerableAllWzImage(wzFile.node).Where(_img => _img.WzFile == wzFile).ToList();

                if (wzFile.header.VersionChecked)
                {
                    this.CalcOffset(wzFile, imgList);
                }
                else
                {
                    // find the wzImage with minimum size.
                    Wz_Image minSizeImg = imgList.DefaultIfEmpty().Aggregate((_img1, _img2) => _img1.Size < _img2.Size ? _img1 : _img2);

                    if (minSizeImg == null && imgList.Count > 0)
                    {
                        minSizeImg = imgList[0];
                    }

                    if (minSizeImg != null)
                    {
                        this.DetectWithWzImage(wzFile, minSizeImg);
                    }
                    else if (wzFile.directories.Count > 0)
                    {
                        this.DetectWithAllWzDir(wzFile);
                    }

                    if (wzFile.header.VersionChecked)
                    {
                        this.CalcOffset(wzFile, imgList);
                    }
                }

                return wzFile.header.VersionChecked;
            }

            protected override uint CalcOffset(Wz_File wzFile, uint filePos, uint hashedOffset) => wzFile.CalcOffset(filePos, hashedOffset);
        }

        public class FastVersionVerifier : WzVersionVerifier, IWzVersionVerifier
        {
            public virtual bool Verify(Wz_File wzFile)
            {
                List<Wz_Image> imgList = EnumerableAllWzImage(wzFile.node).Where(_img => _img.WzFile == wzFile).ToList();

                if (wzFile.header.VersionChecked)
                {
                    this.CalcOffset(wzFile, imgList);
                }
                else
                {
                    if (imgList.Count > 0)
                    {
                        this.FastDetectWithAllWzImages(wzFile, imgList);
                    }
                    else if (wzFile.directories.Count > 0)
                    {
                        this.DetectWithAllWzDir(wzFile);
                    }

                    if (wzFile.header.VersionChecked)
                    {
                        this.CalcOffset(wzFile, imgList);
                    }
                }

                return wzFile.header.VersionChecked;
            }

            protected override uint CalcOffset(Wz_File wzFile, uint filePos, uint hashedOffset) => wzFile.CalcOffset(filePos, hashedOffset);
        }

        public class Pkg2VersionVerifier : FastVersionVerifier, IWzVersionVerifier
        {
            private const int CryptoVersionMin = 1;
            private const int CryptoVersionMax = 2;
            public override bool Verify(Wz_File wzFile)
            {
                for (int i = CryptoVersionMax; i >= CryptoVersionMin; i--)
                {
                    this.cryptoVersion = i;
                    wzFile.header.ResetVersionDetector();
                    if (base.Verify(wzFile))
                    {
                        break;
                    }
                }
                return wzFile.header.VersionChecked;
            }

            private int cryptoVersion;
            protected override uint CalcOffset(Wz_File wzFile, uint filePos, uint hashedOffset) => this.cryptoVersion switch
            {
                1 => wzFile.CalcOffsetPkg2V1(filePos, hashedOffset),
                2 => wzFile.CalcOffsetPkg2V2(filePos, hashedOffset),
                _ => throw new InvalidOperationException($"Unknown cryptoVersion {this.cryptoVersion}."),
            };
        }

        private struct Pkg2DirEntry
        {
            public int NodeType;
            public string Name;
            public int DataLength;
            public int Checksum;
        }
    }

    public enum WzVersionVerifyMode
    {
        Default = 0,
        Fast = 1,
    }
}
