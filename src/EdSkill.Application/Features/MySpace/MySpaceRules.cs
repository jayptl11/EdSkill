using EdSkill.Application.Common.Models;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;

namespace EdSkill.Application.Features.MySpace;

internal static class MySpaceRules
{
    public static readonly int[] AllowedDurations = [30, 45, 60, 90, 120];

    public const int MaxTitleLength = 120;
    public const int MaxDescriptionLength = 2000;
    public const int MaxLanguages = 3;
    public const int MaxLanguageLength = 50;
    public const int MaxCompanionCredentialUrls = 4;

    public static string? NormalizeOptionalString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static List<string> NormalizeLanguages(IReadOnlyCollection<string>? values)
    {
        return NormalizeStringCollection(values);
    }

    public static List<string> NormalizeCredentialUrls(IReadOnlyCollection<string>? values)
    {
        return NormalizeStringCollection(values);
    }

    public static List<SessionDeliveryMode> NormalizeDeliveryModes(IReadOnlyCollection<SessionDeliveryMode>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        return values
            .Distinct()
            .OrderBy(value => value)
            .ToList();
    }

    public static Result<Skill> ResolveOwnedSkill(User user, Guid skillId, UserSkillType type)
    {
        var ownedSkill = user.UserSkills
            .Where(userSkill => userSkill.Type == type && userSkill.SkillId == skillId && userSkill.Skill is not null)
            .Select(userSkill => userSkill.Skill)
            .FirstOrDefault();

        if (ownedSkill is null)
        {
            return Result<Skill>.Failure("MY_SPACE_SKILL_NOT_OWNED", "The selected skill is not owned by the current user.");
        }

        if (!ownedSkill.IsActive || ownedSkill.IsDeleted)
        {
            return Result<Skill>.Failure("SKILL_INACTIVE", "Skill is inactive.");
        }

        return Result<Skill>.Success(ownedSkill);
    }

    private static List<string> NormalizeStringCollection(IReadOnlyCollection<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
