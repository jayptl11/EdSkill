using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.Wallet;

internal static partial class PointPackageRules
{
    public static string NormalizeWhitespace(string value)
    {
        return MultiSpaceRegex().Replace(value.Trim(), " ");
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

    public static string NormalizeCode(string value)
    {
        var lookup = NormalizeLookup(value);
        var builder = new StringBuilder(lookup.Length);
        var previousWasUnderscore = false;

        foreach (var character in lookup)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasUnderscore = false;
            }
            else if (!previousWasUnderscore)
            {
                builder.Append('_');
                previousWasUnderscore = true;
            }
        }

        return builder
            .ToString()
            .Trim('_');
    }

    public static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeWhitespace(value);
    }

    public static bool IsAvailableForSale(PointPackage package, DateTime utcNow)
    {
        if (!package.IsActive || package.IsDeleted)
        {
            return false;
        }

        if (package.StartsAt.HasValue && package.StartsAt.Value > utcNow)
        {
            return false;
        }

        if (package.EndsAt.HasValue && package.EndsAt.Value < utcNow)
        {
            return false;
        }

        return true;
    }

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex MultiSpaceRegex();
}
