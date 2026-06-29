using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.CharaSim
{
    public struct SummaryParams
    {
        private string r;
        private string n;
        private string cStart;
        private string cEnd;
        private string gStart;
        private string gEnd;
        private string xStart;
        private string xEnd;
        private string bracketIcon;
        private string arrowLeftIcon;
        private string arrowRightIcon;

        /// <summary>
        /// 获取或设置回车符(\r)的替换字符串。
        /// </summary>
        public string R
        {
            get { return r; }
            set { r = value; }
        }

        /// <summary>
        /// 获取或设置换行符(\n)的替换字符串。
        /// </summary>
        public string N
        {
            get { return n; }
            set { n = value; }
        }

        /// <summary>
        /// 获取或设置高亮起始符(#c)的替换字符串。
        /// </summary>
        public string CStart
        {
            get { return cStart; }
            set { cStart = value; }
        }

        /// <summary>
        /// 获取或设置高亮结束符(#)的替换字符串
        /// </summary>
        public string CEnd
        {
            get { return cEnd; }
            set { cEnd = value; }
        }

        /// <summary>
        /// 获取或设置自定义高亮起始符(#g)的替换字符串。
        /// </summary>
        public string GStart
        {
            get { return gStart; }
            set { gStart = value; }
        }

        /// <summary>
        /// 获取或设置自定义高亮结束符(#)的替换字符串
        /// </summary>
        public string GEnd
        {
            get { return gEnd; }
            set { gEnd = value; }
        }

        /// <summary>
        /// 获取或设置自定义高亮起始符(#x)的替换字符串。
        /// </summary>
        public string XStart
        {
            get { return xStart; }
            set { xStart = value; }
        }

        /// <summary>
        /// 获取或设置自定义高亮结束符(#)的替换字符串
        /// </summary>
        public string XEnd
        {
            get { return xEnd; }
            set { xEnd = value; }
        }

        public string BracketIcon
        {
            get { return bracketIcon; }
            set { bracketIcon = value; }
        }

        public string ArrowLeftIcon
        {
            get { return arrowLeftIcon; }
            set { arrowLeftIcon = value; }
        }

        public string ArrowRightIcon
        {
            get { return arrowRightIcon; }
            set { arrowRightIcon = value; }
        }

        /// <summary>
        /// 获取默认的替换字符串组合。
        /// </summary>
        public static SummaryParams Default
        {
            get
            {
                return new SummaryParams()
                {
                    R = @"\r",
                    N = @"\n",
                    cStart = @"#c",
                    cEnd = @"#",
                    gStart = @"#$g",
                    gEnd = @"#",
                    xStart = @"#$x",
                    xEnd = @"#",
                    bracketIcon = @" #@0/8/9@ ", // keep in sync with SkillTooltipRender2.ImageTable
                    arrowLeftIcon = @"#@1/14/9@",
                    arrowRightIcon = @"#@2/17/9@",
                };
            }
        }

        public static SummaryParams Text
        {
            get
            {
                return new SummaryParams()
                {
                    R = "\r",
                    N = "\n",
                    cStart = @"#c",
                    cEnd = @"#",
                    gStart = @"#$g",
                    gEnd = @"#",
                    xStart = @"#$x",
                    xEnd = @"#",
                };
            }
        }

        public static SummaryParams Html
        {
            get
            {
                return new SummaryParams()
                {
                    R = null,
                    N = "<br />",
                    cStart = @"<span style=""font-weight:bold; color:orange;"">",
                    cEnd = @"</span>",
                    gStart = @"<span style=""font-weight:bold; color:#3f0;"">",
                    gEnd = @"</span>",
                    xStart = @"<span style=""font-weight:bold; color:#ddfe01;"">",
                    xEnd = @"</span>",
                };
            }
        }
    }
}
