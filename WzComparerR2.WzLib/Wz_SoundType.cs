using System;

namespace WzComparerR2.WzLib
{
    public enum Wz_SoundType
    {
        Unknown = 0,
        Mp3,
        Pcm,
        Binary,

        /// <summary>Ogg Vorbis（Unity 資料集的 FSB5 轉封裝結果）。</summary>
        Ogg
    }
}
