using System;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// 資產路徑不存在、類型不符，或 catalog 快取位置失效時擲出的例外。
    /// </summary>
    public class ClassicAssetException : Exception
    {
        public ClassicAssetException(string message)
            : base(message)
        {
        }

        public ClassicAssetException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
