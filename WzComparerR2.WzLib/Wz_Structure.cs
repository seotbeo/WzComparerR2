using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using WzComparerR2.WzLib.Compatibility;

namespace WzComparerR2.WzLib
{
    public class Wz_Structure
    {
        public Wz_Structure()
        {
            this.wz_files = new List<Wz_File>();
            this.ms_files = new List<IMapleStoryFile>();
            this.encryption = new Wz_Crypto();
            this.img_number = 0;
            this.has_basewz = false;
            this.TextEncoding = Wz_Structure.DefaultEncoding;
            this.AutoDetectExtFiles = Wz_Structure.DefaultAutoDetectExtFiles;
            this.ImgCheckDisabled = Wz_Structure.DefaultImgCheckDisabled;
        }

        public List<Wz_File> wz_files;
        public List<IMapleStoryFile> ms_files;
        public Wz_Crypto encryption;
        public Wz_Node WzNode;
        public int img_number;
        public bool has_basewz;

        public Encoding TextEncoding { get; set; }
        public bool AutoDetectExtFiles { get; set; }
        public bool ImgCheckDisabled { get; set; }

        // Known profile cache: populated after successful detection, used as fast path for subsequent files.
        // TODO: allow external management of the cache.
        internal WzProfileDetectionCache ProfileCache { get; } = new();

        public void Clear()
        {
            foreach (Wz_File f in this.wz_files)
            {
                f.Close();
            }
            this.wz_files.Clear();
            foreach (IMapleStoryFile f in this.ms_files)
            {
                f.Dispose();
            }
            this.ms_files.Clear();
            this.encryption.Reset();
            this.img_number = 0;
            this.has_basewz = false;
            this.WzNode = null;
        }

        public void calculate_img_count()
        {
            foreach (Wz_File f in this.wz_files)
            {
                this.img_number += f.ImageCount;
            }
        }

        public void Load(string fileName, bool useBaseWz = false)
        {
            //现在我们已经不需要list了
            this.WzNode = new Wz_Node(Path.GetFileName(fileName));
            if (Path.GetFileName(fileName).ToLower() == "list.wz")
            {
                this.encryption.LoadListWz(Path.GetDirectoryName(fileName));
                foreach (string list in this.encryption.List)
                {
                    WzNode.Nodes.Add(list);
                }
            }
            else
            {
                LoadFile(fileName, WzNode, useBaseWz);
            }
            calculate_img_count();
        }

        public Wz_File LoadFile(string fileName, Wz_Node node, bool useBaseWz = false, bool loadWzAsFolder = false, string fallbackFileName = null, Wz_File imageNameOwner = null)
        {
            object originalValue = node.Value;
            var originalChildren = node.Nodes.ToArray();
            int originalFileCount = this.wz_files.Count;
            try
            {
                return this.LoadFileCore(fileName, node, useBaseWz, loadWzAsFolder, fallbackFileName);
            }
            catch (Exception ex) when (!(ex is IOException) || ex is EndOfStreamException || ex is InvalidDataException)
            {
                if (ex is OutOfMemoryException || ex is OperationCanceledException || ex is UnauthorizedAccessException)
                {
                    throw;
                }
                LogWzOpenError(fileName, ex);
                while (this.wz_files.Count > originalFileCount)
                {
                    int index = this.wz_files.Count - 1;
                    this.wz_files[index].Close();
                    this.wz_files.RemoveAt(index);
                }
                node.Nodes.Clear();
                foreach (var child in originalChildren)
                {
                    node.Nodes.Add(child);
                }
                node.Value = originalValue;

                var recovered = new Wz_File(fileName, this, true) { TextEncoding = this.TextEncoding };
                try
                {
                    var images = WzImageScanner.FindAllHits(recovered);
                    bool folderContainer = loadWzAsFolder && string.Equals(Path.GetFileNameWithoutExtension(fileName),
                        new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(fileName))).Name, StringComparison.OrdinalIgnoreCase);
                    if (images.Count == 0 && !folderContainer)
                    {
                        recovered.Close();
                        throw;
                    }
                    recovered.LoadScannedImages(node, images, imageNameOwner);
                    this.wz_files.Add(recovered);
                    recovered.DetectWzType();
                    return recovered;
                }
                catch
                {
                    recovered.Close();
                    throw;
                }
            }
        }

        private static readonly object wzOpenErrorLogLock = new object();

        private static void LogWzOpenError(string fileName, Exception error)
        {
            try
            {
                string logPath = Path.Combine(AppContext.BaseDirectory, "wz_open_error.log");
                string message = $"[{DateTimeOffset.Now:O}] {fileName}{Environment.NewLine}{error}{Environment.NewLine}{Environment.NewLine}";
                lock (wzOpenErrorLogLock)
                {
                    File.AppendAllText(logPath, message, Encoding.UTF8);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
            {
                System.Diagnostics.Trace.TraceError("Failed to write wz_open_error.log: {0}", ex);
            }
        }

        private Wz_File LoadFileCore(string fileName, Wz_Node node, bool useBaseWz, bool loadWzAsFolder, string fallbackFileName)
        {
            Wz_File file = null;

            try
            {
                file = new Wz_File(fileName, this, fallbackFileName);
                if (!file.Loaded)
                {
                    throw new Exception("The file is not a valid wz file.");
                }
                this.wz_files.Add(file);
                file.TextEncoding = this.TextEncoding;

                // 1. pre-read and detect version/read rule
                WzPreReadResult preReadResult = null;
                IWzFormatProfile matchedProfile = null;
                foreach (var preReader in WzPreReaders.All)
                {
                    if (preReader.TryPreRead(file, out var result))
                    {
                        matchedProfile = this.TryDetectWzProfile(file, result);
                        if (matchedProfile != null)
                        {
                            preReadResult = result;
                            break;
                        }
                    }
                }

                // Cache on success
                if (matchedProfile != null)
                {
                    this.ProfileCache.Upsert(matchedProfile.CreateCacheEntry(file));
                }

                // 2. detect string encryption, assign to crypto
                if (preReadResult != null && matchedProfile != null)
                {
                    if (!this.encryption.IsDirEncDetected(file))
                    {
                        this.DetectCryptoKeyType(file, matchedProfile, preReadResult);
                    }

                    matchedProfile.AssignDirStringReader(file, this.encryption);
                }

                // 3. full dir tree read
                node.Value = file;
                file.Node = node;
                file.FileStream.Position = file.Header.DirStartPosition;
                file.GetDirTree(node, useBaseWz, loadWzAsFolder, fileName, fallbackFileName);
                file.DetectWzType();
                return file;
            }
            catch
            {
                if (file != null)
                {
                    file.Close();
                    this.wz_files.Remove(file);
                }
                throw;
            }
        }

        private IWzFormatProfile TryDetectWzProfile(Wz_File file, WzPreReadResult preReadResult)
        {
            // Try cached profiles first.
            foreach (var profile in WzFormatProfiles.GetCandidates(preReadResult.Format))
            {
                foreach (var cached in this.ProfileCache.GetCandidates(profile.Name))
                {
                    if (profile.TryDetectCached(file, preReadResult, cached))
                    {
                        this.ProfileCache.MarkHit(cached);
                        return profile;
                    }
                }
            }

            // Full detection: iterate candidate profiles.
            foreach (var profile in WzFormatProfiles.GetCandidates(preReadResult.Format))
            {
                if (profile.TryDetect(file, preReadResult))
                {
                    return profile;
                }
            }

            return null;
        }

        public void LoadImg(string fileName)
        {
            this.WzNode = new Wz_Node(Path.GetFileName(fileName));
            this.LoadImg(fileName, WzNode);
        }

        public void LoadImg(string fileName, Wz_Node node)
        {
            Wz_File file = null;

            try
            {
                file = new Wz_File(fileName, this);
                file.TextEncoding = this.TextEncoding;
                file.Node = node;
                var imgNode = new Wz_Node(node.Text);
                //跳过checksum检测
                var img = new Wz_Image(node.Text, (int)file.FileStream.Length, 0, 0, 0, file)
                {
                    OwnerNode = imgNode,
                    Offset = 0,
                    IsChecksumChecked = true
                };
                imgNode.Value = img;

                node.Nodes.Add(imgNode);
                node.Value = file;
                this.wz_files.Add(file);
            }
            catch
            {
                file?.Close();
                throw;
            }
        }

        public void LoadKMST1125DataWz(string fileName, string fallbackFileName = null)
        {
            this.LoadWzFolder(Path.GetDirectoryName(fileName), ref this.WzNode, true, fallbackFileName == null ? null : Path.GetDirectoryName(fallbackFileName));
            calculate_img_count();
        }

        public bool IsKMST1125WzFormat(string fileName, string fallbackFileName = null)
        {
            if (!string.Equals(Path.GetExtension(fileName), ".wz", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string iniFile = Path.ChangeExtension(fileName, ".ini");
            if (!File.Exists(iniFile) && fallbackFileName != null)
            {
                iniFile = Path.ChangeExtension(fallbackFileName, ".ini");
            }
            if (!File.Exists(iniFile))
            {
                return false;
            }

            using (var file = new Wz_File(fileName, this, fallbackFileName))
            {
                if (!file.Loaded)
                {
                    return false;
                }
                foreach (var preReader in WzPreReaders.All)
                {
                    if (preReader.TryPreRead(file, out var result))
                    {
                        return !result.Nodes.Any(n => n.NodeType == 0x02 || n.NodeType == 0x04);
                    }
                }
                return false;
            }
        }

        public void LoadWzFolder(string folder, ref Wz_Node node, bool useBaseWz = false, string fallbackFolder = null)
        {
            string baseName = Path.Combine(folder, Path.GetFileName(folder));
            string fallbackBaseName = fallbackFolder == null ? null : Path.Combine(fallbackFolder, Path.GetFileName(fallbackFolder));
            string entryWzFileName = Path.ChangeExtension(baseName, ".wz");
            string iniFileName = Path.ChangeExtension(baseName, ".ini");
            Func<int, string> extraWzFileName = _index => Path.ChangeExtension($"{baseName}_{_index:D3}", ".wz");
            Func<int, string> fallbackExtraWzFileName = _index => Path.ChangeExtension($"{fallbackBaseName}_{_index:D3}", ".wz");

            // load iniFile
            int? lastWzIndex = null;
            if (!File.Exists(iniFileName))
            {
                iniFileName = Path.ChangeExtension(fallbackBaseName, ".ini");
            }
            if (File.Exists(iniFileName))
            {
                var iniConf = File.ReadAllLines(iniFileName).Select(row =>
                {
                    string[] columns = row.Split('|');
                    string key = columns.Length > 0 ? columns[0] : null;
                    string value = columns.Length > 1 ? columns[1] : null;
                    return new KeyValuePair<string, string>(key, value);
                });
                if (int.TryParse(iniConf.FirstOrDefault(kv => kv.Key == "LastWzIndex").Value, out var indexFromIni))
                {
                    lastWzIndex = indexFromIni;
                }
            }

            // ini file missing or unexpected format
            if (lastWzIndex == null)
            {
                for (int i = 0; ; i++)
                {
                    string extraFile = extraWzFileName(i);
                    string fallbackExtraFile = fallbackExtraWzFileName(i);
                    if (!File.Exists(extraFile) && !File.Exists(fallbackExtraFile))
                    {
                        break;
                    }
                    lastWzIndex = i;
                }
            }

            // load entry file
            if (node == null)
            {
                node = new Wz_Node(Path.GetFileName(entryWzFileName));
            }
            var entryWzf = this.LoadFile(entryWzFileName, node, useBaseWz, true, Path.ChangeExtension(fallbackBaseName, ".wz"));

            // load extra file
            if (lastWzIndex != null)
            {
                for (int i = 0, j = lastWzIndex.Value; i <= j; i++)
                {
                    string extraFile = extraWzFileName(i);
                    string fallbackExtraFile = fallbackExtraWzFileName(i);
                    var tempNode = new Wz_Node(Path.GetFileName(extraFile));
                    var extraWzf = this.LoadFile(extraFile, tempNode, false, true, fallbackExtraFile, entryWzf);

                    /*
                     * there is a little hack here, we'll move all img to the entry file, and each img still refers to the original wzfile.
                     * before:
                     *   base.wz (Wz_File)
                     *   |- a.img (Wz_Image)
                     *   base_000.wz (Wz_File)
                     *   |- b.img (Wz_Image) { wz_f = base_000.wz }
                     *   
                     * after:
                     *   base.wz (Wz_File) { mergedFiles = [base_000.wz] }
                     *   |- a.img (Wz_Image)  { wz_f = base.wz }
                     *   |- b.img (Wz_Image)  { wz_f = base_000.wz }
                     *   
                     * this.wz_files references all opened files so they can be closed correctly.
                     */

                    entryWzf.MergeWzFile(extraWzf);
                }
            }

            if (entryWzf.IsImageScanFallback)
            {
                string directoryRoot = useBaseWz ? Path.GetDirectoryName(Path.GetFullPath(folder)) : folder;
                foreach (string subFolder in Directory.GetDirectories(directoryRoot).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                {
                    string folderName = Path.GetFileName(subFolder);
                    if (!string.Equals(Path.GetFullPath(subFolder), Path.GetFullPath(folder), StringComparison.OrdinalIgnoreCase)
                        && node.Nodes[folderName] == null && File.Exists(Path.Combine(subFolder, folderName + ".wz")))
                    {
                        Wz_Node child = node.Nodes.Add(folderName);
                        this.LoadWzFolder(subFolder, ref child, false,
                            fallbackFolder == null ? null : Path.Combine(useBaseWz ? Path.GetDirectoryName(fallbackFolder) : fallbackFolder, folderName));
                    }
                }
                if (useBaseWz)
                {
                    this.has_basewz = true;
                }
            }
        }

        public void LoadMsFile(string fileName)
        {
            this.LoadMsFile(fileName, ref this.WzNode);
        }

        private void LoadMsFile(string fileName, ref Wz_Node node)
        {
            List<Exception> exceptions = new(2);
            if (node == null)
            {
                node = new Wz_Node(Path.GetFileName(fileName));
            }

            bool loaded = false;
            // try ms file v1
            if (!loaded)
            {
                Ms_File file = null;
                try
                {
                    file = new Ms_File(fileName, this);
                    file.ReadEntries();
                    file.GetDirTree(node);
                    this.ms_files.Add(file);
                    loaded = true;
                }
                catch(Exception ex)
                {
                    if (file != null)
                    {
                        file.Close();
                        this.ms_files.Remove(file);
                    }
                    exceptions.Add(ex);
                }
            }

            // try ms file v2
            if (!loaded)
            {
                Ms_FileV2 file = null;
                try
                {
                    file = new Ms_FileV2(fileName, this);
                    file.ReadEntries();
                    file.GetDirTree(node);
                    this.ms_files.Add(file);
                    loaded = true;
                }
                catch (Exception ex)
                {
                    if (file != null)
                    {
                        file.Close();
                        this.ms_files.Remove(file);
                    }
                    exceptions.Add(ex);
                }
            }
            
            // return errors
            if (!loaded)
            {
                throw new AggregateException("Failed to load ms files.", exceptions.ToArray());
            }
        }

        private void DetectCryptoKeyType(Wz_File file, IWzFormatProfile profile, WzPreReadResult preReadResult)
        {
            profile.DetectCryptoKeyType(file, this.encryption, preReadResult, out var pkg1KeyType, out var pkg2KeyType);
            if (pkg1KeyType != Wz_CryptoKeyType.Unknown)
                this.encryption.Pkg1EncType = pkg1KeyType;
            if (pkg2KeyType != Wz_CryptoKeyType.Unknown)
                this.encryption.Pkg2EncType = pkg2KeyType;
        }

        #region Global Settings
        public static Encoding DefaultEncoding
        {
            get { return _defaultEncoding ?? Encoding.Default; }
            set { _defaultEncoding = value; }
        }

        private static Encoding _defaultEncoding;

        public static bool DefaultAutoDetectExtFiles { get; set; }

        public static bool DefaultImgCheckDisabled { get; set; }

        #endregion
    }
}
