using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.CharaSim
{
    public class MobElemAttr
    {
        public MobElemAttr(string elemAttr)
        {
            this.StringValue = elemAttr;
            if (string.IsNullOrEmpty(elemAttr))
            {
                return;
            }

            for (int i = 0; i < elemAttr.Length; i += 2)
            {
                ElemResistance resist = (ElemResistance)(elemAttr[i + 1] - 48);
                switch (elemAttr[i])
                {
                    case 'I': this.I = resist; break;
                    case 'L': this.L = resist; break;
                    case 'F': this.F = resist; break;
                    case 'S': this.S = resist; break;
                    case 'H': this.H = resist; break;
                    case 'D': this.D = resist; break;
                    case 'P': this.P = resist; break;
                }
            }
        }
        public string StringValue { get; private set; }
        public ElemResistance I { get; private set; }
        public ElemResistance L { get; private set; }
        public ElemResistance F { get; private set; }
        public ElemResistance S { get; private set; }
        public ElemResistance H { get; private set; }
        public ElemResistance D { get; private set; }
        public ElemResistance P { get; private set; }

        public IEnumerable<KeyValuePair<string, ElemResistance>> ElemResistances
        {
            get
            {
                yield return new KeyValuePair<string, ElemResistance>("얼음", I);
                yield return new KeyValuePair<string, ElemResistance>("번개", L);
                yield return new KeyValuePair<string, ElemResistance>("불", F);
                yield return new KeyValuePair<string, ElemResistance>("독", S);
                yield return new KeyValuePair<string, ElemResistance>("성", H);
                yield return new KeyValuePair<string, ElemResistance>("암흑", D);
                yield return new KeyValuePair<string, ElemResistance>("물리", P);
            }
        }
    }

    public enum ElemResistance : byte
    {
        Normal = 0,
        Immune = 1,
        Resist = 2,
        Weak = 3,
    }
}
