using System;
using System.Collections.Generic;
using System.Linq;

namespace WzComparerR2.CharaSim
{
    public class SkillPropertyResolver : ISkillPropertyResolver
    {
        public SkillPropertyResolver(Skill skill, int level, bool applyPerJobProperties = false)
        {
            this.skill = skill ?? throw new ArgumentNullException(nameof(skill));
            this.level = level;
            this.currentProperties = skill.GetCommon(level);
            if (applyPerJobProperties && skill.PerJobAttackInfo.Count > 0)
            {
                this.perJobProperties = skill.PerJobAttackInfo.ElementAt(skill.PerJobIndex).Value;
            }
        }

        private readonly Skill skill;
        private readonly int level;
        private readonly Dictionary<string, string> currentProperties;
        private readonly Dictionary<string, string> perJobProperties;

        public bool TryResolve(string propertyName, int? skillId, int? skillLevel, out ResolvedSkillProperty value)
        {
            if (skillId == null || skillId == this.skill.SkillID)
            {
                int evaluationLevel = skillLevel ?? this.level;
                var properties = evaluationLevel == this.level
                    ? this.currentProperties
                    : this.skill.GetCommon(evaluationLevel);
                if (TryGetProperty(this.perJobProperties, propertyName, out string expression)
                    || TryGetProperty(properties, propertyName, out expression))
                {
                    value = new ResolvedSkillProperty(expression, evaluationLevel, skill.ComparisonLevel);
                    return true;
                }
            }
            else
            {
                Skill target = this.FindSkill(skillId.Value);
                if (target != null && target.MaxLevel > 0)
                {
                    // we don't know which level should be used for the referenced summary, so we always use the max level.
                    if (TryGetProperty(target.GetCommon(target.MaxLevel), propertyName, out string expression))
                    {
                        value = new ResolvedSkillProperty(expression, target.MaxLevel);
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        protected virtual Skill FindSkill(int skillId)
        {
            return null;
        }

        private static bool TryGetProperty(Dictionary<string, string> properties, string propertyName, out string expression)
        {
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    if (string.Equals(property.Key, propertyName, StringComparison.OrdinalIgnoreCase)
                        && property.Value != null)
                    {
                        expression = property.Value;
                        return true;
                    }
                }
            }

            expression = null;
            return false;
        }
    }
}