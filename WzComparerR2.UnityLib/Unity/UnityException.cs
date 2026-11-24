using System;

namespace WzComparerR2.UnityLib.Unity
{
    /// <summary>
    /// Unity 容器（UnityFS / SerializedFile / 物件佈局）解析失敗時擲出的例外。
    /// </summary>
    public class UnityException : Exception
    {
        public UnityException(string message)
            : base(message)
        {
        }

        public UnityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
