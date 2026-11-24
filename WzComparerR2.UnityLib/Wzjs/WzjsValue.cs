using System;
using System.Globalization;

namespace WzComparerR2.UnityLib.Wzjs
{
    /// <summary>
    /// WZJS 節點值的型別標籤。type tag 與值的對應是逆向實測定案：
    /// <list type="table">
    /// <item><term>2</term><description>SubTree（無值，純階層節點）</description></item>
    /// <item><term>3</term><description>u8（slot1 section，實測為 0/1 bool 類）</description></item>
    /// <item><term>5</term><description>u16（slot3 section）</description></item>
    /// <item><term>6</term><description>i32（slot4 section）</description></item>
    /// <item><term>7</term><description>i64（slot5 section）</description></item>
    /// <item><term>8</term><description>8 bytes 數值對（slot6 section），通常是 2×f32</description></item>
    /// <item><term>9</term><description>f64（slot8 section，如 unitPrice）</description></item>
    /// <item><term>11</term><description>pool C 字串（如 z 圖層名、islot；對應舊版 WZ 的 String property）</description></item>
    /// <item><term>12</term><description>pool C uol 連結（對應舊版 WZ 的 Uol property）</description></item>
    /// <item><term>14</term><description>vector 2×f32（slot7 section，origin / map 點）</description></item>
    /// <item><term>18/19</term><description>Canvas（圖片節點，像素在 spriteset/spritesheet，靠 path 關聯）</description></item>
    /// </list>
    /// </summary>
    public enum WzjsValueKind
    {
        SubTree,
        Canvas,
        U8,
        U16,
        Int,
        Long,
        Float2,
        Int2,
        Double,
        Vector,
        String,
        Uol,
    }

    /// <summary>
    /// WZJS 節點值。與舊版 WZ 的對應：type 11 → String、type 12 → Uol，
    /// 列舉值（z 圖層名、islot 等）在舊版 WZ 以字串儲存。
    /// </summary>
    public struct WzjsValue : IEquatable<WzjsValue>
    {
        private readonly long integer;
        private readonly double real;
        private readonly float x;
        private readonly float y;
        private readonly string text;

        private WzjsValue(WzjsValueKind kind, long integer, double real, float x, float y, string text)
        {
            this.Kind = kind;
            this.integer = integer;
            this.real = real;
            this.x = x;
            this.y = y;
            this.text = text;
        }

        public WzjsValueKind Kind { get; private set; }

        public bool HasValue
        {
            get { return this.Kind != WzjsValueKind.SubTree && this.Kind != WzjsValueKind.Canvas; }
        }

        /// <summary>u8 / u16 / i32 / i64 的整數內容；其他型別為 0。</summary>
        public long Integer
        {
            get { return this.integer; }
        }

        /// <summary>f64 內容；其他型別為 0。</summary>
        public double Real
        {
            get { return this.real; }
        }

        /// <summary>2×f32 / vector 的第一個分量。</summary>
        public float X
        {
            get { return this.x; }
        }

        /// <summary>2×f32 / vector 的第二個分量。</summary>
        public float Y
        {
            get { return this.y; }
        }

        /// <summary>type 11 字串 / type 12 uol 路徑。</summary>
        public string Text
        {
            get { return this.text; }
        }

        public static WzjsValue SubTree
        {
            get { return new WzjsValue(WzjsValueKind.SubTree, 0, 0, 0, 0, null); }
        }

        public static WzjsValue Canvas
        {
            get { return new WzjsValue(WzjsValueKind.Canvas, 0, 0, 0, 0, null); }
        }

        public static WzjsValue FromU8(byte value)
        {
            return new WzjsValue(WzjsValueKind.U8, value, 0, 0, 0, null);
        }

        public static WzjsValue FromU16(ushort value)
        {
            return new WzjsValue(WzjsValueKind.U16, value, 0, 0, 0, null);
        }

        public static WzjsValue FromInt(int value)
        {
            return new WzjsValue(WzjsValueKind.Int, value, 0, 0, 0, null);
        }

        public static WzjsValue FromLong(long value)
        {
            return new WzjsValue(WzjsValueKind.Long, value, 0, 0, 0, null);
        }

        public static WzjsValue FromFloat2(float x, float y)
        {
            return new WzjsValue(WzjsValueKind.Float2, 0, 0, x, y, null);
        }

        public static WzjsValue FromInt2(int a, int b)
        {
            return new WzjsValue(WzjsValueKind.Int2, ((long)b << 32) | (uint)a, 0, 0, 0, null);
        }

        public static WzjsValue FromDouble(double value)
        {
            return new WzjsValue(WzjsValueKind.Double, 0, value, 0, 0, null);
        }

        public static WzjsValue FromVector(float x, float y)
        {
            return new WzjsValue(WzjsValueKind.Vector, 0, 0, x, y, null);
        }

        public static WzjsValue FromString(string value)
        {
            return new WzjsValue(WzjsValueKind.String, 0, 0, 0, 0, value ?? string.Empty);
        }

        public static WzjsValue FromUol(string value)
        {
            return new WzjsValue(WzjsValueKind.Uol, 0, 0, 0, 0, value ?? string.Empty);
        }

        /// <summary>Int2 的第一個整數。</summary>
        public int Int2A
        {
            get { return unchecked((int)(this.integer & 0xFFFFFFFF)); }
        }

        /// <summary>Int2 的第二個整數。</summary>
        public int Int2B
        {
            get { return unchecked((int)(this.integer >> 32)); }
        }

        public bool Equals(WzjsValue other)
        {
            return this.Kind == other.Kind
                && this.integer == other.integer
                && this.real.Equals(other.real)
                && this.x.Equals(other.x)
                && this.y.Equals(other.y)
                && string.Equals(this.text, other.text, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is WzjsValue && this.Equals((WzjsValue)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)this.Kind;
                hash = (hash * 397) ^ this.integer.GetHashCode();
                hash = (hash * 397) ^ this.real.GetHashCode();
                hash = (hash * 397) ^ this.x.GetHashCode();
                hash = (hash * 397) ^ this.y.GetHashCode();
                hash = (hash * 397) ^ (this.text != null ? this.text.GetHashCode() : 0);
                return hash;
            }
        }

        public override string ToString()
        {
            switch (this.Kind)
            {
                case WzjsValueKind.SubTree:
                    return "<Property>";
                case WzjsValueKind.Canvas:
                    return "<Canvas>";
                case WzjsValueKind.U8:
                case WzjsValueKind.U16:
                case WzjsValueKind.Int:
                case WzjsValueKind.Long:
                    return this.integer.ToString(CultureInfo.InvariantCulture);
                case WzjsValueKind.Double:
                    return this.real.ToString("R", CultureInfo.InvariantCulture);
                case WzjsValueKind.Float2:
                case WzjsValueKind.Vector:
                    return this.x.ToString("R", CultureInfo.InvariantCulture)
                        + ", " + this.y.ToString("R", CultureInfo.InvariantCulture);
                case WzjsValueKind.Int2:
                    return this.Int2A.ToString(CultureInfo.InvariantCulture)
                        + ", " + this.Int2B.ToString(CultureInfo.InvariantCulture);
                case WzjsValueKind.String:
                case WzjsValueKind.Uol:
                    return this.text;
                default:
                    return string.Empty;
            }
        }
    }
}
