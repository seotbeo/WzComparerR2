using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;
using System.Drawing;
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

        public static Dictionary<string, string> GlobalVariableMapping { get; private set; }

        public static string GetSkillSummary(string H, int Level, ISkillPropertyResolver resolver, SummaryParams param, SkillSummaryOptions options = default)
        {
            if (H == null)
            {
                return null;
            }

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
                    if (TryReadSkillReference(H, idx, out int referenceEnd, out int skillId, out string referenceProperty))
                    {
                        if (TryResolveProperty(resolver, referenceProperty, skillId, null,
                            out string matchedProperty, out ResolvedSkillProperty resolvedReference))
                        {
                            bool addPercent = false;

                            AppendPropertyValue(sb, resolvedReference.Expression, matchedProperty,
                                resolvedReference.EvaluationLevel ?? Level, resolvedReference.ComparisonEvaluationLevel, param, options, true, ref addPercent);
                            referenceEnd -= referenceProperty.Length - matchedProperty.Length;
                        }
                        else
                        {
                            sb.Append(H, idx, referenceEnd - idx);
                        }

                        idx = referenceEnd;
                        continue;
                    }

                    int len = ReadPropertyLength(H, idx + 1);
                    string propertyToken = H.Substring(idx + 1, len);
                    if (TryResolveProperty(resolver, propertyToken, null, Level,
                        out string propKey, out ResolvedSkillProperty property))
                    {
                        int propertyEnd = idx + 1 + propKey.Length;
                        bool addPercent = propertyEnd < H.Length && H[propertyEnd] == '%';

                        AppendPropertyValue(sb, property.Expression, propKey,
                            property.EvaluationLevel ?? Level, property.ComparisonEvaluationLevel, param, options, true, ref addPercent);
                        idx += propKey.Length + 1 + (addPercent ? 1 : 0);
                        continue;
                    }

                    if (TryResolveGlobalVariable(sb, propertyToken, Level, resolver, param, options, out int globalLength))
                    {
                        idx += globalLength + 1;
                        continue;
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

        public static string GetSkillSummary(Skill skill, int level, StringResultSkill sr,
            ISkillPropertyResolver resolver, SummaryParams param, SkillSummaryOptions options = default,
            bool doHighlight = false, Dictionary<int, HashSet<string>> diffSkillTags = null, bool convertExtraProps = true)
        {
            if (skill == null || sr == null)
                return null;

            string summary = SelectSkillSummary(skill, level, sr, resolver);
            if (doHighlight && diffSkillTags != null)
            {
                summary = HighlightSkillSummary(summary, skill, diffSkillTags);
            }
            if (!convertExtraProps && skill.ExtraPropNames.Count > 0)
            {
                summary = MarkExtraPropsToGreen(summary, skill);
            }
            return GetSkillSummary(summary, level, resolver, param, options);
        }

        private static string SelectSkillSummary(Skill skill, int level, StringResultSkill sr, ISkillPropertyResolver resolver)
        {
            if (skill.PreBBSkill)
            {
                if (skill.Level == level
                    && resolver != null
                    && resolver.TryResolve("hs", null, level, out ResolvedSkillProperty selector)
                    && selector.Expression != null
                    && sr[selector.Expression] is string selectedSummary)
                {
                    return selectedSummary;
                }

                if (level <= 0)
                    return null;
                if (sr.SkillH.Count >= level)
                    return sr.SkillH[level - 1];
                return sr.SkillH.Count == 1 ? sr.SkillH[0] : null;
            }

            string summary = sr.SkillH.Count > 0 ? sr.SkillH[0] : null;
            foreach (var entry in sr.SkillExtraH)
            {
                if (level < entry.Key)
                    break;
                summary = entry.Value;
            }
            return summary;
        }

        private static string HighlightSkillSummary(string summary, Skill skill, Dictionary<int, HashSet<string>> diffSkillTags)
        {
            if (diffSkillTags.ContainsKey(skill.SkillID))
            {
                foreach (var tags in diffSkillTags[skill.SkillID])
                {
                    summary = (summary == null ? null : Regex.Replace(summary, "#" + tags + @"(?=[^a-zA-Z0-9]|$)", @"#$g#" + tags + "#"));
                }
            }
            return summary;
        }

        private static string MarkExtraPropsToGreen(string summary, Skill skill)
        {
            foreach (var tags in skill.ExtraPropNames)
            {
                summary = (summary == null ? null : Regex.Replace(summary, "#" + tags + @"(?=[^a-zA-Z0-9]|$)", @"#$x" + tags + "#"));
            }
            return summary;
        }

        private static bool TryResolveProperty(ISkillPropertyResolver resolver, string token, int? skillId, int? skillLevel,
            out string propertyName, out ResolvedSkillProperty value)
        {
            if (resolver != null)
            {
                for (int length = token.Length; length > 0; length--)
                {
                    string candidate = token.Substring(0, length);
                    if (resolver.TryResolve(candidate, skillId, skillLevel, out value) && value.Expression != null)
                    {
                        propertyName = candidate;
                        return true;
                    }
                }
            }

            propertyName = null;
            value = default;
            return false;
        }

        private static bool TryResolveGlobalVariable(StringBuilder output, string token, int level,
            ISkillPropertyResolver resolver, SummaryParams param, SkillSummaryOptions options, out int matchedLength)
        {
            for (int length = token.Length; length > 0; length--)
            {
                string key = token.Substring(0, length);
                if (!GlobalVariableMapping.TryGetValue(key, out string propertyName) || propertyName == null)
                    continue;

                if (propertyName.Length > 0
                    && resolver != null
                    && resolver.TryResolve(propertyName, null, level, out ResolvedSkillProperty property)
                    && property.Expression != null)
                {
                    bool addPercent = false;

                    AppendPropertyValue(output, property.Expression, key,
                        property.EvaluationLevel ?? level, property.ComparisonEvaluationLevel, param, options, false, ref addPercent);
                }
                else
                {
                    output.Append(param.GStart).Append("[").Append(key).Append("]").Append(param.GEnd);
                }

                matchedLength = length;
                return true;
            }

            matchedLength = 0;
            return false;
        }

        private static bool TryReadSkillReference(string text, int start, out int end, out int skillId, out string propertyName)
        {
            end = start;
            skillId = 0;
            propertyName = null;
            if (start + 1 >= text.Length || text[start + 1] != '[')
            {
                return false;
            }

            int idStart = start + 2;
            int cursor = idStart;
            while (cursor < text.Length && text[cursor] >= '0' && text[cursor] <= '9')
            {
                cursor++;
            }
            if (cursor == idStart
                || cursor >= text.Length
                || text[cursor] != ']'
                || !int.TryParse(text.Substring(idStart, cursor - idStart), out skillId))
            {
                return false;
            }

            int propertyStart = cursor + 1;
            int propertyLength = ReadPropertyLength(text, propertyStart);
            if (propertyLength == 0)
            {
                return false;
            }

            propertyName = text.Substring(propertyStart, propertyLength);
            end = propertyStart + propertyLength;
            return true;
        }

        private static int ReadPropertyLength(string text, int start)
        {
            if (start >= text.Length || !IsPropertyStart(text[start]))
                return 0;

            int end = start + 1;
            while (end < text.Length && (IsPropertyStart(text[end]) || text[end] >= '0' && text[end] <= '9'))
            {
                end++;
            }
            return end - start;
        }

        private static bool IsPropertyStart(char value)
        {
            return value == '_'
                || value >= 'a' && value <= 'z'
                || value >= 'A' && value <= 'Z';
        }

        private static void AppendPropertyValue(StringBuilder sb, string expression, string propertyName, int level, int? comparisonLevel,
            SummaryParams param, SkillSummaryOptions options, bool applyPropertyFormatting, ref bool addPercent)
        {
            try
            {
                decimal value = Calculator.Parse(expression, level);
                decimal value2 = comparisonLevel != null && comparisonLevel > 0 ? Calculator.Parse(expression, comparisonLevel.Value) : value;
                bool highlightVal = false;
                if (value != value2 && options.LevelViewMode == SkillLevelViewMode.CurrentAndSelected)
                {
                    highlightVal = true;
                    sb.Append(param.GStart);
                }
                if (applyPropertyFormatting && options.ConvertCooltimeMS && propertyName == "cooltimeMS")
                {
                    sb.AppendFormat("{0:f2}", value / 1000);
                }
                else if (applyPropertyFormatting && options.ConvertPerM && propertyName.EndsWith("PerM", StringComparison.Ordinal))
                {
                    sb.AppendFormat("{0:f1}", value / 100);
                }
                else
                {
                    sb.Append(value);
                }

                if (highlightVal)
                {
                    if (addPercent)
                    {
                        sb.Append("%");
                    }

                    sb.Append(param.BracketIcon);

                    if (applyPropertyFormatting && options.ConvertCooltimeMS && propertyName == "cooltimeMS")
                    {
                        sb.AppendFormat("{0:f2}", value2 / 1000);
                    }
                    else if (applyPropertyFormatting && options.ConvertPerM && propertyName.EndsWith("PerM", StringComparison.Ordinal))
                    {
                        sb.AppendFormat("{0:f1}", value2 / 100);
                    }
                    else
                    {
                        sb.Append(value2);
                    }
                    if (addPercent)
                    {
                        sb.Append("%");
                    }

                    sb.Append(param.GEnd);
                }
                else
                {
                    addPercent = false;
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
        }

    }

    public struct SkillSummaryOptions
    {
        public bool ConvertCooltimeMS { get; set; }
        public bool ConvertPerM { get; set; }
        public bool IgnoreEvalError { get; set; }
        public bool EndColorOnNewLine { get; set; }
        public SkillLevelViewMode LevelViewMode { get; set; }
    }

    public interface ISkillPropertyResolver
    {
        bool TryResolve(string propertyName, int? skillId, int? skillLevel, out ResolvedSkillProperty value);
    }

    public readonly struct ResolvedSkillProperty
    {
        public ResolvedSkillProperty(string expression, int? evaluationLevel, int? comparisonEvaluationLevel = null)
        {
            Expression = expression;
            EvaluationLevel = evaluationLevel;
            ComparisonEvaluationLevel = comparisonEvaluationLevel;
        }

        public string Expression { get; }
        public int? EvaluationLevel { get; }
        public int? ComparisonEvaluationLevel { get; }
    }
}
