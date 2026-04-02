using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

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
            this.WzVersionVerifyMode = Wz_Structure.DefaultWzVersionVerifyMode;
        }

        public List<Wz_File> wz_files;
        public List<IMapleStoryFile> ms_files;
        public Wz_Crypto encryption;
        public Wz_Node WzNode;
        public int img_number;
        public bool has_basewz;
        public bool sorted; //暂时弃用

        public Encoding TextEncoding { get; set; }
        public bool AutoDetectExtFiles { get; set; }
        public bool ImgCheckDisabled { get; set; }
        public WzVersionVerifyMode WzVersionVerifyMode {get;set;}

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
            this.sorted = false;
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

        public Wz_File LoadFile(string fileName, Wz_Node node, bool useBaseWz = false, bool loadWzAsFolder = false, string fallbackFileName = null, bool force = false)
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
                if (!this.encryption.IsDirEncDetected(file))
                {
                    this.encryption.DetectEncryption(file);
                }
                node.Value = file;
                file.Node = node;
                file.FileStream.Position = file.Header.DataStartPosition;
                if (force && file.Header.Signature == Wz_Header.PKG2)
                {
                    if (ShouldUseForcedPkg2TreeFallback(file, loadWzAsFolder, fileName))
                    {
                        this.LoadForcedPkg2Tree(file, node, useBaseWz, loadWzAsFolder, fileName, fallbackFileName);
                    }
                }
                else
                {
                    file.GetDirTree(node, useBaseWz, loadWzAsFolder, fileName, fallbackFileName);
                    file.Header.DirEndPosition = file.FileStream.Position;

                    if (file.Header.Signature == Wz_Header.PKG2 && node.Nodes.Count == 0)
                    {
                        file.RetryParsePkg2TreeWithCandidateVersions(useBaseWz, fileName, fallbackFileName);
                        if (node.Nodes.Count == 0 && ShouldUseForcedPkg2TreeFallback(file, loadWzAsFolder, fileName))
                        {
                            this.LoadForcedPkg2Tree(file, node, useBaseWz, loadWzAsFolder, fileName, fallbackFileName);
                        }
                    }
                }

                file.DetectWzType();
                file.DetectWzVersion();
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
            LoadWzFolder(Path.GetDirectoryName(fileName), ref this.WzNode, true, fallbackFileName == null ? null : Path.GetDirectoryName(fallbackFileName));
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
            bool hasIniFile = File.Exists(iniFile);
            bool hasShardFiles = HasCompanionShardFiles(fileName, fallbackFileName);
            if (!hasIniFile && !hasShardFiles)
            {
                return false;
            }

            // check if the file is an empty wzfile
            using (var file = new Wz_File(fileName, this, fallbackFileName))
            {
                if (!file.Loaded)
                {
                    return false;
                }
                try
                {
                    var tempNode = new Wz_Node();
                    if (!this.encryption.IsDirEncDetected(file))
                    {
                        this.encryption.DetectEncryption(file);
                    }
                    file.FileStream.Position = file.Header.DataStartPosition;
                    try
                    {
                        file.GetDirTree(tempNode);
                    }
                    catch
                    {
                        file.ForceGetDirTree(tempNode);
                    }
                    return file.ImageCount == 0;
                }
                catch
                {
                    return true;
                }
            }
        }

        public void LoadWzFolder(string folder, ref Wz_Node node, bool useBaseWz = false, string fallbackFolder = null, bool force = false)
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
            var entryWzf = this.LoadFile(entryWzFileName, node, useBaseWz, true, Path.ChangeExtension(fallbackBaseName, ".wz"), force: force);

            // load extra file
            var mergedExtraFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (lastWzIndex != null)
            {
                for (int i = 0, j = lastWzIndex.Value; i <= j; i++)
                {
                    string extraFile = extraWzFileName(i);
                    string fallbackExtraFile = fallbackExtraWzFileName(i);
                    var tempNode = new Wz_Node(Path.GetFileName(extraFile));
                    var extraWzf = this.LoadFile(extraFile, tempNode, false, true, fallbackExtraFile, force: force);
                    mergedExtraFiles.Add(Path.GetFullPath(extraFile));
                    if (!string.IsNullOrEmpty(fallbackExtraFile))
                    {
                        mergedExtraFiles.Add(Path.GetFullPath(fallbackExtraFile));
                    }

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

            if (entryWzf.Node.Nodes.Count == 0)
            {
                foreach (string extraFile in EnumerateSiblingWzFiles(folder, entryWzFileName, mergedExtraFiles))
                {
                    string extraFileName = Path.GetFileName(extraFile);
                    string fallbackExtraFile = fallbackFolder == null ? null : Path.Combine(fallbackFolder, extraFileName);
                    var tempNode = new Wz_Node(extraFileName);
                    var extraWzf = this.LoadFile(extraFile, tempNode, false, true, fallbackExtraFile, force: force);
                    entryWzf.MergeWzFile(extraWzf);
                }
            }

            if (entryWzf.Node.Nodes.Count == 0)
            {
                foreach (string childFolder in Directory.EnumerateDirectories(folder).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
                {
                    string childEntryFile = TryGetFolderEntryWzFile(childFolder);
                    if (string.IsNullOrEmpty(childEntryFile))
                    {
                        continue;
                    }

                    string childName = Path.GetFileName(childFolder);
                    string fallbackChildFolder = fallbackFolder == null ? null : Path.Combine(fallbackFolder, childName);
                    var childNode = entryWzf.Node.Nodes.Add(childName);
                    this.LoadWzFolder(childFolder, ref childNode, false, fallbackChildFolder, force: force);
                }
            }

            if (entryWzf.Node.Nodes.Count == 0)
            {
                foreach (string imgFile in Directory.EnumerateFiles(folder, "*.img").OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
                {
                    string imgName = Path.GetFileName(imgFile);
                    var imgNode = entryWzf.Node.Nodes.Add(imgName);
                    this.LoadImg(imgFile, imgNode);
                }
            }
        }

        private static IEnumerable<string> EnumerateSiblingWzFiles(string folder, string entryWzFileName, ISet<string> excludedFiles)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                yield break;
            }

            string entryName = Path.GetFileName(entryWzFileName);
            foreach (string file in Directory.EnumerateFiles(folder, "*.wz").OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                string fullPath = Path.GetFullPath(file);
                if (string.Equals(Path.GetFileName(file), entryName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (excludedFiles != null && excludedFiles.Contains(fullPath))
                {
                    continue;
                }
                yield return file;
            }
        }

        private static string TryGetFolderEntryWzFile(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                return null;
            }

            string folderName = Path.GetFileName(folder);
            string sameNameEntry = Path.Combine(folder, folderName + ".wz");
            if (File.Exists(sameNameEntry))
            {
                return sameNameEntry;
            }

            string[] wzFiles = Directory.EnumerateFiles(folder, "*.wz").Take(2).ToArray();
            return wzFiles.Length == 1 ? wzFiles[0] : null;
        }

        private void LoadForcedPkg2Tree(Wz_File file, Wz_Node node, bool useBaseWz, bool loadWzAsFolder, string fileName, string fallbackFileName)
        {
            file.FindAllHits(file);
            file.retInited = true;
            file.ForceGetDirTree(node, useBaseWz, loadWzAsFolder, fileName, fallbackFileName);
            file.Header.DirEndPosition = file.FileStream.Position;
            file.Forced = true;
        }

        private static bool ShouldUseForcedPkg2TreeFallback(Wz_File file, bool loadWzAsFolder, string fileName)
        {
            if (file?.Header?.Signature != Wz_Header.PKG2)
            {
                return false;
            }

            string effectiveFileName = fileName ?? file.Header.FileName;
            if (loadWzAsFolder && LooksLikeCompanionShardFile(effectiveFileName))
            {
                return !file.CanExposeAsStandaloneImageAtDefaultOffset();
            }

            return true;
        }

        private static bool LooksLikeCompanionShardFile(string fileName)
        {
            string name = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            int underscoreIndex = name.LastIndexOf('_');
            if (underscoreIndex <= 0 || underscoreIndex >= name.Length - 1)
            {
                return false;
            }

            for (int i = underscoreIndex + 1; i < name.Length; i++)
            {
                if (!char.IsDigit(name[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasCompanionShardFiles(string fileName, string fallbackFileName = null)
        {
            static bool HasShard(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                string directory = Path.GetDirectoryName(path);
                string baseName = Path.GetFileNameWithoutExtension(path);
                string entryFileName = Path.GetFileName(path);
                if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(baseName) || string.IsNullOrEmpty(entryFileName))
                {
                    return false;
                }

                return Directory.EnumerateFiles(directory, baseName + "_*.wz").Any()
                    || Directory.EnumerateFiles(directory, "*.wz")
                        .Any(file => !string.Equals(Path.GetFileName(file), entryFileName, StringComparison.OrdinalIgnoreCase))
                    || Directory.EnumerateFiles(directory, "*.img").Any()
                    || Directory.EnumerateDirectories(directory)
                        .Select(TryGetFolderEntryWzFile)
                        .Any(childEntry => !string.IsNullOrEmpty(childEntry));
            }

            return HasShard(fileName) || HasShard(fallbackFileName);
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

        #region Global Settings
        public static Encoding DefaultEncoding
        {
            get { return _defaultEncoding ?? Encoding.Default; }
            set { _defaultEncoding = value; }
        }

        private static Encoding _defaultEncoding;

        public static bool DefaultAutoDetectExtFiles { get; set; }

        public static bool DefaultImgCheckDisabled { get; set; }

        public static WzVersionVerifyMode DefaultWzVersionVerifyMode { get; set; }
        #endregion
    }
}
