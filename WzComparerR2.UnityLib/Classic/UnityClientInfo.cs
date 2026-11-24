using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.UnityLib.Unity;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// Unity クライアントの基本情報: 名前（PlayerSettings.productName）、バージョン
    /// （PlayerSettings.bundleVersion）。
    /// <para>
    /// データソースは次の順序です: <c>&lt;Client&gt;_Data/globalgamemanagers</c> の PlayerSettings、
    /// <c>&lt;Client&gt;_Data/app.info</c>、および <c>StreamingAssets/aa/settings.json</c>
    /// 中の Addressables の <c>catalog_&lt;version&gt;.hash</c>。いずれかのソースがない場合、対応するフィールドは null。
    /// </para>
    /// </summary>
    public sealed class UnityClientInfo
    {
        private const int PlayerSettingsClassId = 129;

        private static readonly Regex CatalogVersionPattern = new Regex(
            @"catalog_([0-9][0-9A-Za-z._-]*)\.hash", RegexOptions.CultureInvariant);

        private static readonly Regex VersionTextPattern = new Regex(
            @"^[0-9]+(\.[0-9]+)+$", RegexOptions.CultureInvariant);

        private UnityClientInfo()
        {
        }

        /// <summary>開発元名（PlayerSettings.companyName）。</summary>
        public string CompanyName { get; private set; }

        /// <summary>クライアント名（PlayerSettings.productName）。</summary>
        public string ProductName { get; private set; }

        /// <summary>クライアントのバージョン（PlayerSettings.bundleVersion）。</summary>
        public string Version { get; private set; }

        /// <summary>解析された <c>&lt;Client&gt;_Data</c> ディレクトリ。見つからない場合は null。</summary>
        public string DataFolder { get; private set; }

        /// <summary>「名前 バージョン」の表示文字列。どちらもない場合は null。</summary>
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(this.ProductName))
                {
                    return string.IsNullOrEmpty(this.Version) ? null : this.Version;
                }
                return string.IsNullOrEmpty(this.Version)
                    ? this.ProductName
                    : this.ProductName + " " + this.Version;
            }
        }

        /// <summary>
        /// bundle ディレクトリ（<c>StreamingAssets/aa/StandaloneWindows64</c>）からクライアント情報を解析します。
        /// 見つからない項目は null のままで、例外は投げません。
        /// </summary>
        /// <param name="bundleDirectory">bundle のあるディレクトリ（<c>*_Data</c> と <c>aa/settings.json</c> を上位に探すために使用）。</param>
        public static UnityClientInfo Resolve(string bundleDirectory)
        {
            UnityClientInfo info = new UnityClientInfo();

            try
            {
                string dataFolder = FindDataFolder(bundleDirectory);
                info.DataFolder = dataFolder;
                if (dataFolder != null)
                {
                    info.ApplyPlayerSettings(Path.Combine(dataFolder, "globalgamemanagers"));
                    if (string.IsNullOrEmpty(info.ProductName))
                    {
                        info.ApplyAppInfo(Path.Combine(dataFolder, "app.info"));
                    }
                }

                if (string.IsNullOrEmpty(info.Version))
                {
                    // Addressables の catalog ファイル名には Application.version が含まれており、推測ではないリテラル値です。
                    info.Version = ReadAddressablesVersion(bundleDirectory);
                }
                if (string.IsNullOrEmpty(info.Version) && dataFolder != null)
                {
                    info.ApplyPlayerSettingsVersion(Path.Combine(dataFolder, "globalgamemanagers"));
                }
            }
            catch (Exception)
            {
                // 情報は表示専用で、解析に失敗してもデータセットの読み込みには影響しません。
            }

            return info;
        }

        /// <summary><c>*_Data</c> ディレクトリから上位へ辿り、bundle ディレクトリが属するクライアントフォルダーを探します。</summary>
        private static string FindDataFolder(string bundleDirectory)
        {
            if (string.IsNullOrEmpty(bundleDirectory))
            {
                return null;
            }
            string current = bundleDirectory;
            for (int i = 0; i < 5 && !string.IsNullOrEmpty(current); i++)
            {
                if (File.Exists(Path.Combine(current, "globalgamemanagers"))
                    || File.Exists(Path.Combine(current, "app.info")))
                {
                    return current;
                }
                DirectoryInfo parent = Directory.GetParent(current);
                current = parent?.FullName;
                if (current != null && current.EndsWith("_Data", StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(Path.Combine(current, "globalgamemanagers"))
                        || File.Exists(Path.Combine(current, "app.info")))
                    {
                        return current;
                    }
                }
            }
            return null;
        }

        /// <summary>PlayerSettings（class 129）を読んで companyName / productName / bundleVersion を取得します。</summary>
        private void ApplyPlayerSettings(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            byte[] data = File.ReadAllBytes(path);
            UnitySerializedFile file = UnitySerializedFile.Parse(data);
            List<string> strings = null;
            foreach (UnityObjectInfo obj in file.Objects)
            {
                if (obj.ClassId != PlayerSettingsClassId)
                {
                    continue;
                }
                long start = file.DataOffset + obj.ByteStart;
                if (start < 0 || start + obj.ByteSize > data.Length)
                {
                    continue;
                }
                strings = ReadAlignedStrings(data, (int)start, (int)obj.ByteSize);
                break;
            }
            if (strings == null || strings.Count == 0)
            {
                return;
            }

            // PlayerSettings で最初に現れる 2 つの文字列は companyName、productName です。
            if (strings.Count >= 2)
            {
                this.CompanyName = strings[0];
                this.ProductName = strings[1];
            }
            else
            {
                this.ProductName = strings[0];
            }
        }

        private void ApplyPlayerSettingsVersion(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }
            try
            {
                byte[] data = File.ReadAllBytes(path);
                UnitySerializedFile file = UnitySerializedFile.Parse(data);
                foreach (UnityObjectInfo obj in file.Objects)
                {
                    if (obj.ClassId != PlayerSettingsClassId)
                    {
                        continue;
                    }
                    long start = file.DataOffset + obj.ByteStart;
                    if (start < 0 || start + obj.ByteSize > data.Length)
                    {
                        continue;
                    }
                    ApplyVersionFromStrings(
                        ReadAlignedStrings(data, (int)start, (int)obj.ByteSize), 0);
                    break;
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// bundleVersion は PlayerSettings 内の複数のバージョン文字列の最後（それ以前は
        /// iPhone/Android など各プラットフォーム用のバージョンフィールド）なので、「最も完全な」バージョン文字列を採用します。
        /// </summary>
        private void ApplyVersionFromStrings(List<string> strings, int skip)
        {
            if (!string.IsNullOrEmpty(this.Version))
            {
                return;
            }
            string best = null;
            for (int i = skip; i < strings.Count; i++)
            {
                string text = strings[i];
                if (string.IsNullOrEmpty(text) || !VersionTextPattern.IsMatch(text))
                {
                    continue;
                }
                if (best == null || CountSegments(text) >= CountSegments(best))
                {
                    best = text;
                }
            }
            this.Version = best;
        }

        private static int CountSegments(string version)
        {
            int count = 1;
            foreach (char c in version)
            {
                if (c == '.')
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>app.info: 1 行目が会社名、2 行目が製品名（UTF-8）。</summary>
        private void ApplyAppInfo(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length >= 1 && this.CompanyName == null)
            {
                this.CompanyName = lines[0].Trim();
            }
            if (lines.Length >= 2 && this.ProductName == null)
            {
                this.ProductName = lines[1].Trim();
            }
        }

        /// <summary>
        /// Addressables のリモート catalog ファイル名は <c>catalog_&lt;Application.version&gt;.hash</c> で、
        /// そのため <c>aa/settings.json</c> からクライアントのバージョンを取得できます。
        /// </summary>
        private static string ReadAddressablesVersion(string bundleDirectory)
        {
            if (string.IsNullOrEmpty(bundleDirectory))
            {
                return null;
            }
            string settings = Path.Combine(bundleDirectory, "..", "settings.json");
            if (!File.Exists(settings))
            {
                settings = Path.Combine(bundleDirectory, "settings.json");
            }
            if (!File.Exists(settings))
            {
                return null;
            }

            string text = File.ReadAllText(settings, Encoding.UTF8);
            Match match = CatalogVersionPattern.Match(text);
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>オブジェクト内の「u32 長さ + 内容 + 4 bytes 整列」の印刷可能文字列をすべて読み出します。</summary>
        private static List<string> ReadAlignedStrings(byte[] data, int offset, int size)
        {
            List<string> strings = new List<string>();
            int end = offset + size;
            for (int pos = offset + 8; pos + 5 <= end; pos++)
            {
                string text;
                int next;
                if (TryReadString(data, pos, end, out text, out next) && text.Length > 0)
                {
                    strings.Add(text);
                    pos = next - 1;
                }
            }
            return strings;
        }

        /// <summary><paramref name="pos"/> から長さプレフィックス付き文字列を読むことを試みます。成功時は <paramref name="next"/> が整列後の位置になります。</summary>
        private static bool TryReadString(byte[] data, int pos, int end, out string text, out int next)
        {
            text = null;
            next = pos;
            int length = BitConverter.ToInt32(data, pos);
            if (length <= 0 || length > 512 || pos + 4 + length > end)
            {
                return false;
            }

            for (int i = 0; i < length; i++)
            {
                byte b = data[pos + 4 + i];
                if (b < 0x20 && b != '\r' && b != '\n' && b != '\t')
                {
                    return false;
                }
            }

            try
            {
                text = Utf8Strict.GetString(data, pos + 4, length);
            }
            catch (ArgumentException)
            {
                return false;
            }

            next = (pos + 4 + length + 3) & ~3;
            return true;
        }

        private static readonly UTF8Encoding Utf8Strict = new UTF8Encoding(false, true);
    }
}
