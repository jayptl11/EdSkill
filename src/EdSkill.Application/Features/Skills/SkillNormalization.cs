using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.Skills;

internal static partial class SkillNormalization
{
    public static string NormalizeWhitespace(string value)
    {
        return MultiSpaceRegex().Replace(value.Trim(), " ");
    }

    public static string NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : NormalizeWhitespace(value);
    }

    public static string NormalizeLookup(string value)
    {
        var normalized = NormalizeWhitespace(value).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();
    }

    public static string GenerateSlug(string value)
    {
        var lookup = NormalizeLookup(value);
        var builder = new StringBuilder(lookup.Length);
        var previousWasDash = false;

        foreach (var character in lookup)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasDash = false;
            }
            else if (!previousWasDash)
            {
                builder.Append('-');
                previousWasDash = true;
            }
        }

        return builder
            .ToString()
            .Trim('-');
    }

    public static List<string> NormalizeAliasCollection(IReadOnlyCollection<string>? aliases)
    {
        if (aliases is null || aliases.Count == 0)
        {
            return new List<string>();
        }

        var results = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var alias in aliases)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                continue;
            }

            var normalizedAlias = NormalizeWhitespace(alias);
            if (seen.Add(NormalizeLookup(normalizedAlias)))
            {
                results.Add(normalizedAlias);
            }
        }

        return results;
    }

    public static IReadOnlyDictionary<string, Skill> BuildLookup(IEnumerable<Skill> skills)
    {
        var lookup = new Dictionary<string, Skill>(StringComparer.Ordinal);

        foreach (var skill in skills)
        {
            lookup[NormalizeLookup(skill.Name)] = skill;
            lookup[NormalizeLookup(skill.Slug)] = skill;

            foreach (var alias in skill.Aliases)
            {
                lookup[NormalizeLookup(alias)] = skill;
            }
        }

        return lookup;
    }

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex MultiSpaceRegex();
}
