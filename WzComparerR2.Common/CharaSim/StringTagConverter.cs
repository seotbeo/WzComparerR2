using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public static class StringTagConverter
    {
        public static string ConvertDateWZ2(string info, GlobalFindNodeFunction findNode, Wz_File sourceWzFile = null)
        {
            var para = info.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (para.Length < 2)
            {
                return info;
            }

            var timeParseFormat = "yyyyMMddHHmm";
            string timeConvertFormat = null;
            Wz_Node datelistNode = findNode?.Invoke($@"Etc\DateListWZ2.img", sourceWzFile);
            Wz_Node datetimeNode = datelistNode?.FindNodeByPath($@"DateList\{para[0]}\{para[1]}") ?? null;
            var datetime = datetimeNode.GetValueEx<string>(null);
            if (datetime == null || !DateTime.TryParseExact(datetime, timeParseFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            {
                return info;
            }

            if (para.Length == 3)
            {
                var returnType = para[2];
                Wz_Node returnTypeNode = datelistNode?.FindNodeByPath($@"returnType\{returnType}") ?? null;
                timeConvertFormat = returnTypeNode.GetValueEx<string>(null);
            }
            if (string.IsNullOrEmpty(timeConvertFormat))
            {
                timeConvertFormat = "{YYYY}년 {MM}월 {DD}일 {hh}시 {mm}분";
            }

            var datedict = new Dictionary<string, string>
            {
                { "YYYY", "yyyy" },
                { "YY", "yy" },
                { "MM", "MM" },
                { "M", "M" },
                { "DD", "dd" },
                { "D", "d" },
                { "hh", "HH" },
                { "h", "H" },
                { "mm", "mm" },
                { "m", "m" },
            };

            timeConvertFormat = Regex.Replace(timeConvertFormat, @"\{(.*?)\}|([^{}]+)", match =>
            {
                if (match.Value.StartsWith("{") && match.Value.EndsWith("}"))
                {
                    string format = match.Groups[1].Value;
                    return datedict.ContainsKey(format) ? datedict[format] : match.Value;
                }
                else
                {
                    return $@"'{match.Value}'";
                }
            });

            return time.ToString(timeConvertFormat);
        }
    }
}
