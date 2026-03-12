using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.MapRender
{
    public static class SummonPreset
    {
        public static readonly IReadOnlyDictionary<int, IReadOnlyList<string>> MapPresets = new Dictionary<int, IReadOnlyList<string>>
        {
            [220080100] = new List<string> { "시간의 구", "시간의 틈", "이계의 파풀라투스" },
            [220080200] = new List<string> { "시간의 구", "시간의 틈", "이계의 파풀라투스" },
            [220080300] = new List<string> { "시간의 구", "시간의 틈", "이계의 파풀라투스" },

            [410007140] = new List<string> { "카링 1페이즈 궁기" },
            [410007180] = new List<string> { "카링 1페이즈 도올" },
            [410007220] = new List<string> { "카링 1페이즈 혼돈" },
            [410007260] = new List<string> { "카링 2페이즈" },
            [410007300] = new List<string> { "카링 3페이즈 모두", "카링 3페이즈 흉수만" },

            [410008040] = new List<string> { "카링 1페이즈 궁기" },
            [410008080] = new List<string> { "카링 1페이즈 도올" },
            [410008120] = new List<string> { "카링 1페이즈 혼돈" },
            [410008160] = new List<string> { "카링 2페이즈" },
            [410008200] = new List<string> { "카링 3페이즈 모두", "카링 3페이즈 흉수만" },

            [410008340] = new List<string> { "카링 1페이즈 궁기" },
            [410008380] = new List<string> { "카링 1페이즈 도올" },
            [410008420] = new List<string> { "카링 1페이즈 혼돈" },
            [410008460] = new List<string> { "카링 2페이즈" },
            [410008500] = new List<string> { "카링 3페이즈 모두", "카링 3페이즈 흉수만" },

            [410008640] = new List<string> { "카링 1페이즈 궁기" },
            [410008680] = new List<string> { "카링 1페이즈 도올" },
            [410008720] = new List<string> { "카링 1페이즈 혼돈" },
            [410008760] = new List<string> { "카링 2페이즈" },
            [410008800] = new List<string> { "카링 3페이즈 모두", "카링 3페이즈 흉수만" },
        };

        public static readonly IReadOnlyDictionary<string, IReadOnlyList<SummonInfo>> AllPresets = new Dictionary<string, IReadOnlyList<SummonInfo>>
        {
            ["시간의 구"] = new List<SummonInfo>
            {
                new SummonInfo { MobID = 8500000, X = 0, Y = 178, Z0 = 0, Z1 = 0, Foothold = -1, Flip = false, Regen = true},
            },
            ["시간의 틈"] = new List<SummonInfo>
            {
                new SummonInfo { MobID = 8500014, X = -562, Y = 179, Z0 = 0, Z1 = 0, Foothold = -1, Flip = false, Regen = true},
                new SummonInfo { MobID = 8500014, X = -9, Y = 179, Z0 = 0, Z1 = 0, Foothold = -1, Flip = false, Regen = true},
                new SummonInfo { MobID = 8500014, X = 662, Y = 179, Z0 = 0, Z1 = 0, Foothold = -1, Flip = false, Regen = true},
            },
            ["이계의 파풀라투스"] = new List<SummonInfo>
            {
                new SummonInfo { MobID = 8500013, X = -400, Y = -100, Z0 = 0, Z1 = 0, Foothold = -1, Flip = false, Regen = true},
                new SummonInfo { MobID = 8500013, X = -9, Y = -150, Z0 = 0, Z1 = 0, Foothold = -1, Flip = false, Regen = true},
                new SummonInfo { MobID = 8500013, X = 200, Y = -100, Z0 = 0, Z1 = 0, Foothold = -1, Flip = false, Regen = true},
                new SummonInfo { MobID = 8500013, X = 400, Y = 5, Z0 = 0, Z1 = 0, Foothold = -1, Flip = false, Regen = true},
            },
            ["카링 1페이즈 궁기"] = new List<SummonInfo>
            {
                new SummonInfo { MobID = 8880830, X = 512, Y = 106, Z0 = 0, Z1 = 0, Foothold = -1, Flip = true, Regen = true},
            },
            ["카링 1페이즈 도올"] = new List<SummonInfo>
            {
                new SummonInfo { MobID = 8880831, X = -1, Y = 405, Z0 = 0, Z1 = 0, Foothold = -1, Flip = true, Regen = true},
            },
            ["카링 1페이즈 혼돈"] = new List<SummonInfo>
            {
                new SummonInfo { MobID = 8880832, X = 534, Y = 106, Z0 = 0, Z1 = 0, Foothold = -1, Flip = true, Regen = true},
            },
            ["카링 2페이즈"] = new List<SummonInfo>
            {
                new SummonInfo { MobID = 8880837, X = 398, Y = 106, Z0 = 0, Z1 = 0, Foothold = -1, Flip = true, Regen = true},
            },
            ["카링 3페이즈 모두"] = new List<SummonInfo>
            {
                new SummonInfo { MobID = 8880839, X = -1598, Y = 362, Z0 = 0, Z1 = 0, Foothold = -1, Flip = true, Regen = true},
                new SummonInfo { MobID = 8880841, X = -594, Y = 362, Z0 = 0, Z1 = 0, Foothold = -1, Flip = false, Regen = true},
                new SummonInfo { MobID = 8880840, X = 402, Y = 362, Z0 = 0, Z1 = 0, Foothold = -1, Flip = false, Regen = true},
                new SummonInfo { MobID = 8880842, X = -243, Y = 398, Z0 = 0, Z1 = 0, Foothold = -1, Flip = true, Regen = true},
            },
            ["카링 3페이즈 흉수만"] = new List<SummonInfo>
            {
                new SummonInfo { MobID = 8880839, X = -1598, Y = 362, Z0 = 0, Z1 = 0, Foothold = -1, Flip = true, Regen = true},
                new SummonInfo { MobID = 8880841, X = -594, Y = 362, Z0 = 0, Z1 = 0, Foothold = -1, Flip = false, Regen = true},
                new SummonInfo { MobID = 8880840, X = 402, Y = 362, Z0 = 0, Z1 = 0, Foothold = -1, Flip = false, Regen = true},
            },
        };
    }

    public struct SummonInfo
    {
        public int MobID;
        public int X;
        public int Y;
        public int Z0;
        public int Z1;
        public int Foothold;
        public bool Flip;
        public bool Regen;
    }
}
