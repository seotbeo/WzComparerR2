using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Linq;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public readonly struct AstraSubWeaponInfo
    {
        public readonly int ID;
        public readonly int Index;
        public readonly int Job;

        public AstraSubWeaponInfo(int id, int index, int job)
        {
            ID = id;
            Index = index;
            Job = job;
        }
    }
}
