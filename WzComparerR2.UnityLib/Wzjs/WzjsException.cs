using System;

namespace WzComparerR2.UnityLib.Wzjs
{
    /// <summary>
    /// WZJS payload 解析失敗時擲出的例外。
    /// </summary>
    public class WzjsException : Exception
    {
        public WzjsException(string message)
            : base(message)
        {
        }

        public WzjsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
