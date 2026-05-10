using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.Skills;

internal static class SkillCatalogRules
{
    public static string? GetConflictCode(
        Guid? currentSkillId,
        string candidateName,
        string candidateSlug,
        IReadOnlyCollection<string> candidateAliases,
        IReadOnlyCollection<Skill> existingSkills)
    {
        var candidateNameKey = SkillNormalization.NormalizeLookup(candidateName);
        var candidateAliasKeys = candidateAliases
            .Select(SkillNormalization.NormalizeLookup)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var skill in existingSkills.Where(skill => skill.SkillId != currentSkillId))
        {
            if (string.Equals(skill.Slug, candidateSlug, StringComparison.OrdinalIgnoreCase))
            {
                return "SKILL_SLUG_EXISTS";
            }

            var existingNameKey = SkillNormalization.NormalizeLookup(skill.Name);
            if (existingNameKey == candidateNameKey)
            {
                return "SKILL_NAME_EXISTS";
            }

            var existingAliasKeys = skill.Aliases
                .Select(SkillNormalization.NormalizeLookup)
                .ToHashSet(StringComparer.Ordinal);

            if (existingAliasKeys.Contains(candidateNameKey) ||
                candidateAliasKeys.Contains(existingNameKey) ||
                candidateAliasKeys.Overlaps(existingAliasKeys))
            {
                return "SKILL_ALIAS_CONFLICT";
            }
        }

        return null;
    }

    public static bool HasDuplicateAliases(string candidateName, IReadOnlyCollection<string>? aliases)
    {
        if (aliases is null || aliases.Count == 0)
        {
            return false;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal)
        {
            SkillNormalization.NormalizeLookup(candidateName)
        };

        foreach (var alias in aliases)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                return true;
            }

            if (!seen.Add(SkillNormalization.NormalizeLookup(alias)))
            {
                return true;
            }
        }

        return false;
    }
}
