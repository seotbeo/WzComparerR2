using System;
using System.Collections.Generic;

namespace WzComparerR2.UnityLib.Classic
{
    public sealed class UnityPrefabNodeInfo
    {
        internal UnityPrefabNodeInfo(string typeName, long pathId, int fileId, int? classId = null)
        {
            this.TypeName = typeName;
            this.Fields.Add("Type: " + typeName);
            if (classId.HasValue)
                this.Fields.Add("Class ID: " + classId.Value);
            this.Fields.Add("File ID: " + fileId);
            this.Fields.Add("Path ID: " + pathId);
        }

        public string TypeName { get; }
        internal List<string> Fields { get; } = new List<string>();

        public string GetDetails() => string.Join(Environment.NewLine, this.Fields);
        public override string ToString() => this.TypeName;
    }
}
