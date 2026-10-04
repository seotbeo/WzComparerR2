using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.Common;

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

            int idx = 0;
            StringBuilder sb = new StringBuilder();
            bool beginC = false;
            while (idx < H.Length)
            {
                if (H[idx] == '#')
                {
                    if (TryReadSkillReference(H, idx, out int referenceEnd, out int skillId, out string referenceProperty))
                    {
                        if (TryResolveProperty(resolver, referenceProperty, skillId, null,
                            out string matchedProperty, out ResolvedSkillProperty resolvedReference))
                        {
                            AppendPropertyValue(sb, resolvedReference.Expression, matchedProperty,
                                resolvedReference.EvaluationLevel ?? Level, options, true);
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
                        AppendPropertyValue(sb, property.Expression, propKey,
                            property.EvaluationLevel ?? Level, options, true);
                        idx += propKey.Length + 1;
                        continue;
                    }

                    if (TryResolveGlobalVariable(sb, propertyToken, Level, resolver, param, options, out int globalLength))
                    {
                        idx += globalLength + 1;
                        continue;
                    }

                    //匹配#c...#段落
                    if (beginC)
                    {
                        beginC = false;
                        sb.Append(param.CEnd);
                        idx++;
                    }
                    else if (idx + 1 < H.Length && H[idx + 1] == 'c')
                    {
                        beginC = true;
                        sb.Append(param.CStart);
                        idx += 2;
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
                            sb.Append(0);//默认值
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
            return sb.ToString();
        }

        public static string GetSkillSummary(Skill skill, int level, StringResultSkill sr,
            ISkillPropertyResolver resolver, SummaryParams param, SkillSummaryOptions options = default)
        {
            if (skill == null || sr == null)
                return null;

            string summary = SelectSkillSummary(skill, level, sr, resolver);
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
                    AppendPropertyValue(output, property.Expression, key,
                        property.EvaluationLevel ?? level, options, false);
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

        private static void AppendPropertyValue(StringBuilder sb, string expression, string propertyName, int level,
            SkillSummaryOptions options, bool applyPropertyFormatting)
        {
            try
            {
                decimal value = Calculator.Parse(expression, level);
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
    }

    public interface ISkillPropertyResolver
    {
        bool TryResolve(string propertyName, int? skillId, int? skillLevel, out ResolvedSkillProperty value);
    }

    public readonly struct ResolvedSkillProperty
    {
        public ResolvedSkillProperty(string expression, int? evaluationLevel)
        {
            Expression = expression;
            EvaluationLevel = evaluationLevel;
        }

        public string Expression { get; }
        public int? EvaluationLevel { get; }
    }
}
