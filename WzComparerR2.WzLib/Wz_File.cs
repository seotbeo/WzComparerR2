using System;
using System.Buffers;
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
        public bool Forced { get; set; }
        public List<Tuple<long, long>> ret { get; set; } = new();
        public bool retInited { get; set; }

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

            var encType = this.WzStructure.encryption.Pkg2EncType;
            var pkg1Keys = this.WzStructure.encryption.Pkg1Keys ?? this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.BMS);
            var pkg2Keys = this.WzStructure.encryption.Pkg2Keys ?? Wz_Crypto.Wz_NonOpCryptoKey.Instance;
            int encryptedEntryCount = reader.ReadCompressedInt32();
            int decryptedEntryCountV1 = this.DecryptPkg2EntryCountV1(encryptedEntryCount);
            int decryptedEntryCountV2 = this.DecryptPkg2EntryCountV2(encryptedEntryCount);

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

            List<Pkg2DirEntry> entries = new();
            while (true)
            {
                byte nodeType = reader.ReadByte();
                string name;
                if (nodeType == 0x03 || nodeType == 0x04)
                {
                    if (entries.Count == 0 && encType == Wz_CryptoKeyType.Unknown)
                    {
                        long nameStartPos = reader.BaseStream.Position;

                        bool TryReadNameCandidate(string decoderName, Func<string> readName, out string candidateName)
                        {
                            try
                            {
                                reader.BaseStream.Position = nameStartPos;
                                candidateName = readName();
                                if (!LooksLikePkg2NodeName(candidateName))
                                {
                                    return false;
                                }

                                _ = reader.ReadCompressedInt32();
                                _ = reader.ReadCompressedInt32();
                                byte nextMarker = reader.ReadByte();
                                return IsPlausibleNextMarker(nextMarker);
                            }
                            catch
                            {
                                candidateName = null;
                                return false;
                            }
                        }

                        if (TryReadNameCandidate("Pkg2DirString", () => reader.ReadPkg2DirString(Wz_Crypto.Pkg2DirStringKey.Instance), out string pkg2DirName))
                        {
                            reader.BaseStream.Position = nameStartPos;
                            name = reader.ReadPkg2DirString(Wz_Crypto.Pkg2DirStringKey.Instance);
                            encType = Wz_CryptoKeyType.KMST1198;
                            pkg2Keys = Wz_Crypto.Pkg2DirStringKey.Instance;
                        }
                        else if (TryReadNameCandidate("ReadString(BMS)", () => reader.ReadString(this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.BMS)), out string bmsName))
                        {
                            reader.BaseStream.Position = nameStartPos;
                            name = reader.ReadString(this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.BMS));
                            encType = Wz_CryptoKeyType.BMS;
                            pkg2Keys = this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.BMS);
                        }
                        else if (TryReadNameCandidate("ReadString(KMS)", () => reader.ReadString(this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.KMS)), out string kmsName))
                        {
                            reader.BaseStream.Position = nameStartPos;
                            name = reader.ReadString(this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.KMS));
                            encType = Wz_CryptoKeyType.KMS;
                            pkg2Keys = this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.KMS);
                        }
                        else if (TryReadNameCandidate("ReadString(GMS)", () => reader.ReadString(this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.GMS)), out string gmsName))
                        {
                            reader.BaseStream.Position = nameStartPos;
                            name = reader.ReadString(this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.GMS));
                            encType = Wz_CryptoKeyType.GMS;
                            pkg2Keys = this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.GMS);
                        }
                        else
                        {
                            reader.BaseStream.Position = nameStartPos - 1;
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
                        $"hashVersion={this.Header.HashVersion}, pkg2Hash1=0x{this.Header.Pkg2Hash1:X8}."
                    );
                }

                int size = reader.ReadCompressedInt32();
                int cs32 = reader.ReadCompressedInt32();
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

                if ((buffer.Length >= 9 && Encoding.ASCII.GetString(buffer, 0, 9) == "#Property")
                    || (buffer.Length >= 4 && Encoding.ASCII.GetString(buffer, 0, 4) == "Root"))
                {
                    return true;
                }

                var reader = new WzBinaryReader(this.fileStream, false);
                Wz_CryptoKeyType detectedEncType = this.Header?.Signature == Wz_Header.PKG2
                    ? this.WzStructure?.encryption?.Pkg2EncType ?? Wz_CryptoKeyType.Unknown
                    : this.WzStructure?.encryption?.Pkg1EncType ?? Wz_CryptoKeyType.Unknown;

                if (detectedEncType != Wz_CryptoKeyType.Unknown
                    && this.IsSupportedStandaloneImageRoot(reader, dataOffset, this.GetKeysOrNonOp(detectedEncType)))
                {
                    return true;
                }

                foreach (var keyType in new[] {
                    Wz_CryptoKeyType.Unknown,
                    Wz_CryptoKeyType.BMS,
                    Wz_CryptoKeyType.KMS,
                    Wz_CryptoKeyType.GMS,
                    Wz_CryptoKeyType.KMST1198,
                })
                {
                    if (keyType == detectedEncType)
                    {
                        continue;
                    }

                    if (this.IsSupportedStandaloneImageRoot(reader, dataOffset, this.GetKeysOrNonOp(keyType)))
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                this.fileStream.Position = originalPosition;
            }
        }

        private IWzDecrypter GetKeysOrNonOp(Wz_CryptoKeyType keyType)
        {
            return keyType == Wz_CryptoKeyType.Unknown
                ? Wz_Crypto.Wz_NonOpCryptoKey.Instance
                : this.WzStructure?.encryption?.GetKeys(keyType) ?? Wz_Crypto.Wz_NonOpCryptoKey.Instance;
        }

        private bool IsSupportedStandaloneImageRoot(WzBinaryReader reader, long dataOffset, IWzDecrypter decrypter)
        {
            reader.BaseStream.Position = dataOffset;
            try
            {
                switch (reader.ReadImageObjectTypeName(decrypter))
                {
                    case "Property":
                    case "Shape2D#Vector2D":
                    case "Canvas":
                    case "Shape2D#Convex2D":
                    case "Sound_DX8":
                    case "UOL":
                    case "RawData":
                        return true;

                    default:
                        return false;
                }
            }
            catch
            {
                reader.BaseStream.Position = dataOffset;
                return this.TryProbeImplicitStandalonePropertyRoot(reader, decrypter);
            }
        }

        private bool TryProbeImplicitStandalonePropertyRoot(WzBinaryReader reader, IWzDecrypter decrypter)
        {
            long startPosition = reader.BaseStream.Position;
            try
            {
                int entries = reader.ReadCompressedInt32();
                if (entries < 0 || entries > 0x100000)
                {
                    return false;
                }

                if (entries == 0)
                {
                    return true;
                }

                string firstName = reader.ReadImageString(decrypter);
                if (!this.IsPlausibleStandalonePropertyName(firstName))
                {
                    return false;
                }

                return this.IsSupportedStandaloneValueType(reader.ReadByte());
            }
            catch
            {
                return false;
            }
            finally
            {
                reader.BaseStream.Position = startPosition;
            }
        }

        private bool IsSupportedStandaloneValueType(byte flag)
        {
            switch (flag)
            {
                case 0x00:
                case 0x02:
                case 0x0B:
                case 0x03:
                case 0x13:
                case 0x14:
                case 0x04:
                case 0x05:
                case 0x08:
                case 0x09:
                    return true;

                default:
                    return false;
            }
        }

        private bool IsPlausibleStandalonePropertyName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length > 255)
            {
                return false;
            }

            foreach (char c in name)
            {
                if (char.IsControl(c))
                {
                    return false;
                }
            }

            return true;
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
                if (!this.TryReadCompressedInt32At(searchStart, out int rootEntryCount, out int encodedSize))
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

                    // Prefer the payload root implied by the repeated-count table layout.
                    if (rootEntryCount > 0 && rootEntryCount <= 0x10000)
                    {
                        long exactPayloadRootOffset = tableStart + (long)rootEntryCount * sizeof(int);
                        if (exactPayloadRootOffset > tableStart
                            && exactPayloadRootOffset < this.fileStream.Length
                            && this.IsStandaloneImageRootOffset(exactPayloadRootOffset))
                        {
                            dataOffset = exactPayloadRootOffset;
                            return true;
                        }
                    }

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

        internal bool CanExposeAsStandaloneImage()
        {
            return this.TryGetStandaloneImageDataOffset(out _);
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
                if (endOffset <= startOffset)
                {
                    return -1;
                }

                for (long position = startOffset; position < endOffset; position++)
                {
                    if (!this.IsStandaloneImageRootOffset(position))
                    {
                        continue;
                    }

                    this.fileStream.Position = position;
                    int first = this.fileStream.ReadByte();
                    if (first >= 0)
                    {
                        marker = "0x" + first.ToString("X2");
                    }

                    return position;
                }

                return -1;
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

        public void ForceGetDirTree(Wz_Node parent, bool useBaseWz = false, bool loadWzAsFolder = false, string fileName = null, string fallbackFileName = null)
        {
            var ps = new PartialStream(this.FileStream, this.header.DataStartPosition, this.fileStream.Length - this.header.DataStartPosition, true);
            ps.Position = 0;
            var reader = new WzBinaryReader(ps, false);
            this.ForceGetDirTree(reader, parent, useBaseWz, loadWzAsFolder, fileName, fallbackFileName);
        }

        private void ForceGetDirTree(WzBinaryReader reader, Wz_Node parent, bool useBaseWz = false, bool loadWzAsFolder = false, string fileName = null, string fallbackFileName = null)
        {
            List<string> dirs = new List<string>();

            if (this.header.Signature == Wz_Header.PKG2)
            {
                this.ForceReadDirTreePkg2(reader, parent, ref dirs, fileName);
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
                    var m = Regex.Match(dirs[i], @"^([A-Za-z]+)$");
                    if (m.Success)
                    {
                        string wzTypeName = m.Result("$1");

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
                    this.ForceGetDirTree(reader, t, false);
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
                                this.wzStructure.LoadWzFolder(wzFolder, ref t, false, fallbackWzFolder, force: true);
                                if (!willLoadBaseWz)
                                {
                                    var dirWzFile = t.GetValue<Wz_File>();
                                    dirWzFile.isSubDir = true;
                                }
                            }
                        }
                        else if (willLoadBaseWz)
                        {
                            string childFilePath = Path.Combine(baseFolder, dir + ".wz");
                            if (File.Exists(childFilePath))
                            {
                                this.WzStructure.LoadFile(childFilePath, t, false, loadWzAsFolder, null, force: true);
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }

            parent.Nodes.Trim();
        }

        private void ForceReadDirTreePkg2(WzBinaryReader reader, Wz_Node parent, ref List<string> dirs, string fileName = null)
        {
            this.WzStructure.encryption.Pkg2ForcedEncType = Wz_CryptoKeyType.Forced;
            var pkg1Keys = this.WzStructure.encryption.Pkg1Keys ?? this.WzStructure.encryption.GetKeys(Wz_CryptoKeyType.BMS);
            var pkg2Keys = this.WzStructure.encryption.Pkg2KeysForced;
            int encryptedEntryCount = reader.ReadCompressedInt32();
            this.FindAllHits(this);
            int hitcount = 0;

            List<Pkg2DirEntry> entries = new();
            while (true)
            {
                byte nodeType = reader.ReadByte();
                string name = string.Empty;
                if (nodeType == 0x03 || nodeType == 0x04)
                {
                    try
                    {
                        name = entries.Count == 0
                            ? reader.ReadPkg2DirStringForced(pkg2Keys, nodeType, fileName)
                            : reader.ReadString(pkg1Keys);
                    }
                    catch
                    {
                        this.WzStructure.encryption.Pkg2EncType = Wz_CryptoKeyType.BMS;
                        name = reader.ReadString(pkg2Keys);
                    }
                }
                else if (nodeType == 0x80 || (-127 <= encryptedEntryCount && encryptedEntryCount <= 127 && nodeType == encryptedEntryCount))
                {
                    reader.BaseStream.Position--;
                    break;
                }
                else
                {
                    throw new Exception($"Unknown type {nodeType} in WzDirTree.");
                }

                int size = reader.ReadCompressedInt32();
                int cs32 = reader.ReadCompressedInt32();
                long forcedOffset = 0;
                if (nodeType == 0x04 && hitcount < this.ret.Count)
                {
                    if (size == this.ret[hitcount].Item2)
                    {
                        forcedOffset = this.ret[hitcount].Item1;
                    }
                    else
                    {
                        forcedOffset = this.ret.FirstOrDefault(t => size == t.Item2)?.Item1 ?? 0;
                    }
                    hitcount++;
                }

                entries.Add(new Pkg2DirEntry
                {
                    NodeType = nodeType,
                    Name = name,
                    DataLength = size,
                    Checksum = cs32,
                    ForcedOffset = forcedOffset
                });
            }

            int encryptedOffsetCount = reader.ReadCompressedInt32();
            if (encryptedOffsetCount == encryptedEntryCount && entries.Count > 0)
            {
                Span<Pkg2DirEntry> list = CollectionsMarshal.AsSpan(entries);
                for (int i = 0; i < list.Length; i++)
                {
                    reader.ReadUInt32();
                    ref Pkg2DirEntry entry = ref list[i];
                    switch (entry.NodeType)
                    {
                        case 0x04:
                            Wz_Image img = new Wz_Image(entry.Name, entry.DataLength, entry.Checksum, 0, 0, this)
                            {
                                ForcedOffset = entry.ForcedOffset,
                                Offset = entry.ForcedOffset
                            };
                            Wz_Node childNode = parent.Nodes.Add(entry.Name);
                            childNode.Value = img;
                            img.OwnerNode = childNode;
                            this.imageCount++;
                            break;

                        case 0x03:
                            this.directories.Add(new Wz_Directory(entry.Name, entry.DataLength, entry.Checksum, 0, 0, this));
                            dirs.Add(entry.Name);
                            break;
                    }
                }
            }
        }

        public void FindAllHits(Wz_File file)
        {
            if (this.retInited)
            {
                return;
            }

            var ps = new PartialStream(file.FileStream, file.Header.DataStartPosition, file.FileStream.Length - file.Header.DataStartPosition, true);
            ps.Position = 0;
            var reader = new WzBinaryReader(ps, false);

            this.ret.Clear();
            byte[] target = { 0x73, 0xF8, 0xFA, 0xD9, 0xC3 };
            long originPos = reader.BaseStream.Position;
            reader.BaseStream.Position = 0;
            List<long> offsets = FindAllPatterns(reader, target);
            List<long> sizes = DiffAdjacent(offsets);
            if (offsets.Count == sizes.Count + 1)
            {
                for (int i = 0; i < sizes.Count; i++)
                {
                    long offset = offsets[i] + this.header.DataStartPosition;
                    this.ret.Add(new Tuple<long, long>(offset, sizes[i]));
                }
            }
            reader.BaseStream.Position = originPos;
        }

        private static List<long> FindAllPatterns(WzBinaryReader reader, byte[] pattern)
        {
            if (pattern == null || pattern.Length == 0)
            {
                throw new ArgumentException("pattern must not be empty");
            }

            const int BufferSize = 256 * 1024;
            int patternLength = pattern.Length;
            int overlap = patternLength - 1;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize + overlap);
            var positions = new List<long>();

            var skip = new int[256];
            for (int i = 0; i < skip.Length; i++)
            {
                skip[i] = patternLength;
            }

            for (int i = 0; i < patternLength - 1; i++)
            {
                skip[pattern[i]] = patternLength - 1 - i;
            }

            long filePos = 0;
            try
            {
                int bytesRead;
                while ((bytesRead = reader.BaseStream.Read(buffer, overlap, BufferSize)) > 0)
                {
                    int total = bytesRead + overlap;
                    int limit = total - patternLength;
                    int i = 0;

                    while (i <= limit)
                    {
                        int j = patternLength - 1;
                        while (j >= 0 && buffer[i + j] == pattern[j])
                        {
                            j--;
                        }

                        if (j < 0)
                        {
                            long pos = filePos + i - overlap;
                            if (pos >= 0)
                            {
                                positions.Add(pos);
                            }
                            i++;
                        }
                        else
                        {
                            i += skip[buffer[i + patternLength - 1]];
                        }
                    }

                    if (overlap > 0)
                    {
                        Buffer.BlockCopy(buffer, total - overlap, buffer, 0, overlap);
                    }

                    filePos += bytesRead;
                }

                positions.Add(reader.BaseStream.Length);
                return positions;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static List<long> DiffAdjacent(List<long> list)
        {
            if (list.Count == 0)
            {
                return list;
            }

            var diffs = new List<long>();
            for (int i = 1; i < list.Count; i++)
            {
                diffs.Add(list[i] - list[i - 1]);
            }
            return diffs;
        }

        public void DetectWzVersion()
        {
            IWzVersionVerifier wzVersionVerifier;

            if (this.Forced)
            {
                this.header.VersionChecked = true;
                return;
            }

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
                    img.Offset = img.ForcedOffset != -1
                        ? img.ForcedOffset
                        : this.CalcOffset(wzFile, img.HashedOffsetPosition, img.HashedOffset);
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
            public long ForcedOffset;
        }
    }

    public enum WzVersionVerifyMode
    {
        Default = 0,
        Fast = 1,
    }
}
