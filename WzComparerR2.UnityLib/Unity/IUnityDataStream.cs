namespace WzComparerR2.UnityLib.Unity
{
    /// <summary>
    /// bundle 檔案的隨機讀取抽象。桌面版以 <see cref="UnityFileDataStream"/> 實作，
    /// 其他來源（例如記憶體或網路快取）只要實作這個介面即可重用整套解析邏輯。
    /// </summary>
    public interface IUnityDataStream : System.IDisposable
    {
        /// <summary>檔案總長度。</summary>
        long Length { get; }

        /// <summary>從絕對 offset 讀滿 buffer。</summary>
        void ReadAt(long offset, byte[] buffer, int bufferOffset, int count);
    }
}
