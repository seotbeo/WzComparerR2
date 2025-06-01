using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2
{
    public delegate Wz_Node GlobalFindNodeFunction(string fullPath);

    public delegate Wz_Node GlobalFindNodeFunction2(string fullPath, Wz_File sourceWzFile);
}
