using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.Common;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class SummaryParser
    {
        static SummaryParser()
        {
            GlobalVariableMapping = new Dictionary<string, string>();
            GlobalVariableMapping["comboConAran"] = "aranComboCon";
        }

        public static string GetSkillSummary(string H, int Level, Dictionary<string, string> CommonProps, SummaryParams param, SkillSummaryOptions options = default, int comparisonLevel = 0, GlobalFindNodeFunction findNode = null, Wz_File sourceWzFile = null)
        {
            if (H == null) return null;

            var refProps = new Dictionary<int, Dictionary<string, string>>();
            int idx = 0;
            StringBuilder sb = new StringBuilder();
            bool beginC = false;
            bool beginG = false;
            bool beginX = false;
            while (idx < H.Length)
            {
                if (H[idx] == '#')
                {
                    int propStart = idx + 1;
                    Dictionary<string, string> props = CommonProps;
                    if (propStart < H.Length && H[propStart] == '[')
                    {
                        Match reference = Regex.Match(H.Substring(propStart), @"^\[(\d+)\]([_A-Za-z][_A-Za-z0-9]*)");
                        if (reference.Success)
                        {
                            string idText = reference.Groups[1].Value;
                            props = null;
                            if (int.TryParse(idText, out int skillId) && findNode != null)
                            {
                                if (!refProps.TryGetValue(skillId, out props))
                                {
                                    props = GetRefSkillProps(skillId, Level, findNode, sourceWzFile);
                                    refProps[skillId] = props;
                                }
                            }
                            propStart += reference.Groups[2].Index;
                            if (props == null || !GetValueIgnoreCase(props, reference.Groups[2].Value, out _))
                            {
                                sb.Append(H, idx, reference.Length + 1);
                                idx += reference.Length + 1;
                                continue;
                            }
                        }
                    }
                    int end = propStart - 1, len = 0;
                    while ((++end) < H.Length)
                    {
                        if (H[end] == '_' ||
                            ('a' <= H[end] && H[end] <= 'z') ||
                            ('A' <= H[end] && H[end] <= 'Z') ||
                            (end - propStart > 0 && '0' <= H[end] && H[end] <= '9')) //^[_A-Za-z][_A-Za-z0-9]*$
                        {
                            len++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    //优先匹配common
                    string prop = null;
                    string propKey = null;
                    if (props != null)
                    {
                        for (int i = len; i > 0; i--)
                        {
                            propKey = H.Substring(propStart, i);
                            if (GetValueIgnoreCase(props, propKey, out prop))
                            {
                                len = i;
                                break;
                            }
                        }
                    }
                    if (prop != null)
                    {
                        try
                        {
                            decimal val = Calculator.Parse(prop.ToLower(), Level);
                            decimal val2 = comparisonLevel > 0 ? Calculator.Parse(prop.ToLower(), comparisonLevel) : val;
                            bool highlightVal = false;
                            if (val != val2 && options.LevelViewMode == SkillLevelViewMode.CurrentAndSelected)
                            {
                                highlightVal = true;
                                sb.Append(param.GStart);
                            }
                            if (options.ConvertCooltimeMS && propKey == "cooltimeMS")
                            {
                                sb.AppendFormat("{0:f2}", val / 1000);
                            }
                            else if (options.ConvertPerM && propKey.EndsWith("PerM", StringComparison.Ordinal))
                            {
                                sb.AppendFormat("{0:f1}", val / 100);
                            }
                            else
                            {
                                sb.Append(val);
                            }

                            if (highlightVal)
                            {
                                bool addPercent = propStart + len < H.Length && H[propStart + len] == '%';
                                if (addPercent)
                                {
                                    sb.Append("%");
                                }

                                sb.Append(param.BracketIcon);

                                if (options.ConvertCooltimeMS && propKey == "cooltimeMS")
                                {
                                    sb.AppendFormat("{0:f2}", val2 / 1000);
                                }
                                else if (options.ConvertPerM && propKey.EndsWith("PerM", StringComparison.Ordinal))
                                {
                                    sb.AppendFormat("{0:f1}", val2 / 100);
                                }
                                else
                                {
                                    sb.Append(val2);
                                }
                                if (addPercent)
                                {
                                    sb.Append("%");
                                    len++;
                                }

                                sb.Append(param.GEnd);
                            }
                        }
                        catch
                        {
                            if (options.IgnoreEvalError)
                            {
                                sb.Append("NaN");
                            }
                            else
                            {
                                throw;
                            }
                        }

                        idx = propStart + len;
                        continue;
                    }
                    else //试图匹配全局变量
                    {
                        string key = null;
                        for (int i = len; i > 0; i--)
                        {
                            key = H.Substring(idx + 1, i);
                            if (GlobalVariableMapping.TryGetValue(key, out prop))
                            {
                                break;
                            }
                        }
                        if (prop != null)
                        {
                            if (prop != "" && GetValueIgnoreCase(CommonProps, prop, out prop))
                            {
                                try
                                {
                                    decimal val = Calculator.Parse(prop.ToLower(), Level);
                                    sb.Append(val);
                                }
                                catch
                                {
                                    if (options.IgnoreEvalError)
                                    {
                                        sb.Append("NaN");
                                    }
                                    else
                                    {
                                        throw;
                                    }
                                }
                            }
                            else
                            {
                                sb.Append(param.GStart).Append("[").Append(key).Append("]").Append(param.GEnd);
                            }

                            idx += len + 1;
                            continue;
                        }
                    }
                    //匹配#c...#段落
                    if (idx + 1 < H.Length && H[idx + 1] == 'c')
                    {
                        beginC = true;
                        sb.Append(param.CStart);
                        idx += 2;
                    }
                    else if (idx + 1 < H.Length && H[idx + 1] == '$')
                    {
                        if (idx + 2 < H.Length && H[idx + 2] == 'g')
                        {
                            beginG = true;
                            sb.Append(param.GStart);
                            idx += 3;
                        }
                        else if (idx + 2 < H.Length && H[idx + 2] == 'x')
                        {
                            beginX = true;
                            sb.Append(param.XStart);
                            idx += 3;
                        }
                    }
                    else if (idx + 2 < H.Length && H.Substring(idx + 1, 2) == "fc") // #fc
                    {
                        if (idx + 11 < H.Length && H[idx + 11] == '#') // #fc(AA)(RR)(GG)(BB)#
                        {
                            sb.Append(H.Substring(idx, 12));
                            idx += 12;
                        }
                        else if (idx + 13 < H.Length && H[idx + 13] == '#') // #fc0x(AA)(RR)(GG)(BB)#
                        {
                            sb.Append(H.Substring(idx, 14));
                            idx += 14;
                        }
                        else idx++;
                    }
                    else if (idx + 1 < H.Length && H[idx + 1] == 'k') // #k
                    {
                        sb.Append("#k");
                        idx += 2;
                    }
                    else if (beginX)
                    {
                        beginX = false;
                        sb.Append(param.XEnd);
                        idx++;
                    }
                    else if (beginG)
                    {
                        beginG = false;
                        sb.Append(param.GEnd);
                        idx++;
                    }
                    else if (beginC)
                    {
                        beginC = false;
                        sb.Append(param.CEnd);
                        idx++;
                    }
                    else if (idx + 1 < H.Length && len == 0)//匹配省略c的段落
                    {
                        beginC = true;
                        sb.Append(param.CStart);
                        idx++;
                    }
                    else if (len > 0)//无法匹配 取最长的common段
                    {
                        string key = H.Substring(idx + 1, len);
                        if (Regex.IsMatch(key, @"^\d+$"))
                        {
                            sb.Append(key);
                        }
                        else
                        {
                            //sb.Append(0);//默认值
                        }
                        idx += len + 1;
                    }
                    else // skip last #
                    {
                        idx++;
                    }
                }
                else if (H[idx] == '\\')
                {
                    if (idx + 1 < H.Length)
                    {
                        switch (H[idx + 1])
                        {
                            case 'c': break; // \c忽略掉 原因不明
                            case 'r': sb.Append(param.R); break;
                            case 'n':
                                if (beginC && options.EndColorOnNewLine)
                                {
                                    beginC = false;
                                    sb.Append(param.CEnd);
                                }
                                sb.Append(param.N);
                                break;
                            case '\\': sb.Append('\\'); break;
                            default: sb.Append(H[idx + 1]); break;
                        }
                        idx += 2;
                    }
                    else //转义失败
                    {
                        idx++;
                    }
                }
                else
                {
                    sb.Append(H[idx++]);
                }
            }
            return Regex.Replace(sb.ToString().Replace("\t", ""), @"(\\r|\\n)+$", "");
        }

        private static Dictionary<string, string> GetRefSkillProps(int skillID, int level, GlobalFindNodeFunction findNode, Wz_File sourceWzFile)
        {
            Wz_Node skillNode = findNode?.Invoke($@"Skill\{(skillID / 10000000 == 8 ? skillID / 1000 : skillID / 10000)}.img\skill\{skillID}", sourceWzFile);
            if (skillNode == null)
            {
                return null;
            }

            var props = new Dictionary<string, string>();
            Wz_Node[] propNodes = { skillNode.Nodes["common"], skillNode.Nodes["level"]?.Nodes[level.ToString()] };
            foreach (Wz_Node propNode in propNodes)
            {
                if (propNode == null)
                {
                    continue;
                }
                foreach (Wz_Node child in propNode.Nodes)
                {
                    if (child.Value != null && !(child.Value is Wz_Vector))
                    {
                        props[child.Text] = child.Value.ToString();
                    }
                }
            }
            return props;
        }

        private static bool GetValueIgnoreCase(Dictionary<string, string> dict, string key, out string value)
        {
            //bool find = false;
            foreach (var kv in dict)
            {
                if (kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    value = kv.Value;
                    return true;
                }
            }
            value = null;
            return false;
        }

        public static string CalcSingleProp(int Level, string propKey, string prop, SkillSummaryOptions options = default)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                decimal val = Calculator.Parse(prop.ToLower(), Level);
                if (options.ConvertCooltimeMS && propKey == "cooltimeMS")
                {
                    sb.AppendFormat("{0:f2}", val / 1000);
                }
                else if (options.ConvertPerM && propKey.EndsWith("PerM", StringComparison.Ordinal))
                {
                    sb.AppendFormat("{0:f1}", val / 100);
                }
                else
                {
                    sb.Append(val);
                }
            }
            catch
            {
                if (options.IgnoreEvalError)
                {
                    sb.Append("NaN");
                }
                else
                {
                    throw;
                }
            }
            return sb.ToString();
        }

        public static string GetSkillSummary(Skill skill, StringResultSkill sr, SummaryParams param, GlobalFindNodeFunction findNode = null, Wz_File sourceWzFile = null)
        {
            if (skill == null)
                return null;
            return GetSkillSummary(skill, skill.Level, sr, param, findNode: findNode, sourceWzFile: sourceWzFile);
        }

        public static string GetSkillSummary(Skill skill, int level, StringResultSkill sr, SummaryParams param, SkillSummaryOptions options = default,
            bool doHighlight = false, Dictionary<string, string> overrideSkillCommon = null, Dictionary<int, HashSet<string>> DiffSkillTags = null,
            bool convertExtraProps = true, GlobalFindNodeFunction findNode = null, Wz_File sourceWzFile = null)
        {
            if (skill == null || sr == null)
                return null;

            string h = null;
            if (skill.PreBBSkill) //用level声明的技能
            {
                var levelCommon = overrideSkillCommon ?? skill.GetCommon(level);
                string hsSummary;
                if (skill.Level == level && levelCommon.TryGetValue("hs", out string hs)
                    && (hsSummary = sr[hs]) != null) // fix for skill 170001005, 170011005
                {
                    h = hsSummary;
                }
                else if (sr.SkillH.Count >= level)
                {
                    h = sr.SkillH[level - 1];
                }
                else if (sr.SkillH.Count == 1)
                {
                    h = sr.SkillH[0];
                }

                if (doHighlight && DiffSkillTags != null)
                {
                    foreach (var tags in DiffSkillTags[skill.SkillID])
                    {
                        h = (h == null ? null : Regex.Replace(h, "#" + tags + @"(?=[^a-zA-Z0-9]|$)", @"#$g#" + tags + "#"));
                    }
                }
                if (!convertExtraProps && skill.ExtraPropNames.Count > 0)
                {
                    foreach (var tags in skill.ExtraPropNames)
                    {
                        h = (h == null ? null : Regex.Replace(h, "#" + tags + @"(?=[^a-zA-Z0-9]|$)", @"#$x" + tags + "#"));
                    }
                }

                return GetSkillSummary(h, level, levelCommon, param, options, comparisonLevel: skill.ComparisonLevel, findNode: findNode, sourceWzFile: sourceWzFile);
            }
            else
            {
                if (sr.SkillH.Count > 0)
                {
                    h = sr.SkillH[0];
                }

                if (sr.SkillExtraH.Count > 0)
                {
                    // SkillExtraH is always sorted
                    foreach (var kv in sr.SkillExtraH)
                    {
                        if (level < kv.Key)
                        {
                            break;
                        }
                        h = kv.Value;
                    }
                }

                if (doHighlight && DiffSkillTags != null)
                {
                    foreach (var tags in DiffSkillTags[skill.SkillID])
                    {
                        h = (h == null ? null : Regex.Replace(h, "#" + tags + @"(?=[^a-zA-Z0-9]|$)", @"#$g#" + tags + "#"));
                    }
                }
                if (!convertExtraProps && skill.ExtraPropNames.Count > 0)
                {
                    foreach (var tags in skill.ExtraPropNames)
                    {
                        h = (h == null ? null : Regex.Replace(h, "#" + tags + @"(?=[^a-zA-Z0-9]|$)", @"#$x" + tags + "#"));
                    }
                }

                return GetSkillSummary(h, level, overrideSkillCommon ?? skill.Common, param, options, comparisonLevel: skill.ComparisonLevel, findNode: findNode, sourceWzFile: sourceWzFile);
            }
        }

        public static Dictionary<string, string> GlobalVariableMapping { get; private set; }
    }

    public struct SkillSummaryOptions
    {
        public bool ConvertCooltimeMS { get; set; }
        public bool ConvertPerM { get; set; }
        public bool IgnoreEvalError { get; set; }
        public bool EndColorOnNewLine { get; set; }
        public SkillLevelViewMode LevelViewMode { get; set; }
    }
}
