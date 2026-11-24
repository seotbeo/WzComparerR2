using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.UnityLib.Wzjs
{
    /// <summary>
    /// 解出的 WZJS 節點樹模型，對應傳統 MapleStory WZ 的 Property / Canvas / Uol 階層。
    /// </summary>
    public sealed class WzjsNode
    {
        private static readonly WzjsNode[] EmptyChildren = new WzjsNode[0];
        private readonly WzjsNode[] children;

        internal WzjsNode(string name, WzjsValue value, string path, WzjsNode[] children, int typeTag)
        {
            this.Name = name;
            this.Value = value;
            this.Path = path;
            this.children = children ?? EmptyChildren;
            this.TypeTag = typeTag;
        }

        public string Name { get; private set; }

        public WzjsValue Value { get; private set; }

        /// <summary>
        /// pool B 路徑：從 img 根出發的相對路徑（根節點則是資產路徑，
        /// 例如 <c>Character/Afterimage/blank</c>）。
        /// </summary>
        public string Path { get; private set; }

        public IReadOnlyList<WzjsNode> Children
        {
            get { return this.children; }
        }

        /// <summary>原始 type tag（偵錯與匯出對照用）。</summary>
        public int TypeTag { get; private set; }

        public bool IsCanvas
        {
            get { return this.Value.Kind == WzjsValueKind.Canvas; }
        }

        /// <summary>Canvas 節點本身有像素；UOL 節點是連結，但 WZSS 節點鍵池同樣收錄了它。</summary>
        public bool IsImageCapable
        {
            get
            {
                return WzjsImage.IsCanvasTag(this.TypeTag) || this.TypeTag == WzjsImage.TypeUol;
            }
        }

        /// <summary>依路徑段逐一往下找子節點（名稱區分大小寫）。</summary>
        public WzjsNode Find(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            WzjsNode current = this;
            int index = 0;
            while (index <= path.Length)
            {
                int slash = path.IndexOf('/', index);
                int end = slash < 0 ? path.Length : slash;
                string segment = path.Substring(index, end - index);
                current = current.FindChild(segment);
                if (current == null)
                {
                    return null;
                }
                if (slash < 0)
                {
                    break;
                }
                index = slash + 1;
            }
            return current;
        }

        public WzjsNode FindChild(string name)
        {
            for (int i = 0; i < this.children.Length; i++)
            {
                if (string.Equals(this.children[i].Name, name, StringComparison.Ordinal))
                {
                    return this.children[i];
                }
            }
            return null;
        }

        /// <summary>節點總數（含自己）。</summary>
        public int CountNodes()
        {
            int count = 1;
            for (int i = 0; i < this.children.Length; i++)
            {
                count += this.children[i].CountNodes();
            }
            return count;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(this.Name).Append(" (type ").Append(this.TypeTag).Append(')');
            if (this.Value.HasValue)
            {
                builder.Append(" = ").Append(this.Value);
            }
            return builder.ToString();
        }
    }
}
