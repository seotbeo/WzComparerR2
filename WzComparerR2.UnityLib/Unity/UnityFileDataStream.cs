using System;
using System.IO;

namespace WzComparerR2.UnityLib.Unity
{
    /// <summary>
    /// 以共用 <see cref="FileStream"/> + seek 實作的隨機讀取來源。
    /// </summary>
    public sealed class UnityFileDataStream : IUnityDataStream
    {
        private readonly object syncRoot = new object();
        private readonly FileStream stream;
        private readonly long length;

        public UnityFileDataStream(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            this.stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            this.length = this.stream.Length;
        }

        public long Length
        {
            get { return this.length; }
        }

        public void ReadAt(long offset, byte[] buffer, int bufferOffset, int count)
        {
            if (offset < 0 || count < 0 || offset > this.length - count)
            {
                throw new UnityException(string.Format(
                    "檔案讀取越界：offset={0}, count={1}, length={2}", offset, count, this.length));
            }
            lock (this.syncRoot)
            {
                this.stream.Position = offset;
                int read = 0;
                while (read < count)
                {
                    int got = this.stream.Read(buffer, bufferOffset + read, count - read);
                    if (got <= 0)
                    {
                        throw new UnityException(string.Format(
                            "檔案讀取不足：offset={0}，需要 {1} bytes，實際 {2} bytes", offset, count, read));
                    }
                    read += got;
                }
            }
        }

        public void Dispose()
        {
            this.stream.Dispose();
        }
    }
}
