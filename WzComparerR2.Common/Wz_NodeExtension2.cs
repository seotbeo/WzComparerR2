using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.Common
{
    public static class Wz_NodeExtension2
    {
        public static Wz_Node GetLinkedSourceNode(this Wz_Node node, GlobalFindNodeFunction findNode, Wz_File wzf = null)
        {
            string path;

            if (!string.IsNullOrEmpty(path = node.Nodes["source"].GetValueEx<string>(null)))
            {
                return findNode?.Invoke(path, wzf);
            }
            else if (!string.IsNullOrEmpty(path = node.Nodes["_inlink"].GetValueEx<string>(null)))
            {
                var img = node.GetNodeWzImage();
                return img?.Node.FindNodeByPath(true, path.Split('/'));
            }
            else if (!string.IsNullOrEmpty(path = node.Nodes["_outlink"].GetValueEx<string>(null)))
            {
                return findNode?.Invoke(path, wzf);
            }
            else
            {
                return node;
            }
        }

        public static Wz_Node HandleFullUol(this Wz_Node node, GlobalFindNodeFunction findNode, Wz_File wzf = null)
        {
            if (node == null) return null;

            if (node?.Value is Wz_Uol uol)
            {
                if (uol.Uol.StartsWith("/"))
                {
                    return findNode?.Invoke(uol.Uol.TrimStart('/'), wzf);
                }
                else
                {
                    return uol.HandleUol(node);
                }
            }
            return node;
        }
    }
}
