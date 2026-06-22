using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.WzLib
{
    [Flags]
    public enum Wz_Capabilities
    {
        Default = 0,
        EncverMissing = 1,
        /// <summary>
        /// Introduced in KMST1201.
        /// </summary>
        Pkg2RandomHeader = 2,
        /// <summary>
        /// Introduced in KMST1202.
        /// </summary>
        Pkg2RandomHeader64 = 4,
    }
}
