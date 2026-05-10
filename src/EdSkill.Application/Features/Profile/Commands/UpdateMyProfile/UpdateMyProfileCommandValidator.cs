using EdSkill.Application.Common.Interfaces;
using FluentValidation;
using System.Text.RegularExpressions;

namespace EdSkill.Application.Features.Profile.Commands.UpdateMyProfile;

public partial class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    private const int MaxSkillsPerList = 20;
    private const int MaxSkillLength = 50;

    public UpdateMyProfileCommandValidator(IObjectStorageService objectStorageService)
    {
        When(x => x.HasDisplayName, () =>
        {
            RuleFor(x => x.DisplayName)
                .NotEmpty()
                .WithMessage("Display name is required")
                .WithErrorCode("INVALID_DISPLAY_NAME")
                .Must(name => name is not null && name.Trim().Length >= 2)
                .WithMessage("Display name must be at least 2 characters")
                .WithErrorCode("INVALID_DISPLAY_NAME")
                .Must(name => name is not null && name.Trim().Length <= 50)
                .WithMessage("Display name must not exceed 50 characters")
                .WithErrorCode("INVALID_DISPLAY_NAME")
                .Must(name => name is not null && DisplayNameRegex().IsMatch(name.Trim()))
                .WithMessage("Display name contains unsupported characters")
                .WithErrorCode("INVALID_DISPLAY_NAME");
        });

        When(x => x.HasBio && x.Bio is not null, () =>
        {
            RuleFor(x => x.Bio!)
                .MaximumLength(500)
                .WithMessage("Bio must not exceed 500 characters")
                .WithErrorCode("INVALID_BIO");
        });

        When(x => x.HasUniversity && x.University is not null, () =>
        {
            RuleFor(x => x.University!)
                .MaximumLength(200)
                .WithMessage("University must not exceed 200 characters")
                .WithErrorCode("INVALID_UNIVERSITY");
        });

        When(x => x.HasFaculty && x.Faculty is not null, () =>
        {
            RuleFor(x => x.Faculty!)
                .MaximumLength(200)
                .WithMessage("Faculty must not exceed 200 characters")
                .WithErrorCode("INVALID_FACULTY");
        });

        When(x => x.HasYearOfStudy, () =>
        {
            RuleFor(x => x.YearOfStudy)
                .Must(year => year is null || (year >= 1 && year <= 6))
                .WithMessage("Year of study must be between 1 and 6")
                .WithErrorCode("INVALID_YEAR_OF_STUDY");
        });

        When(x => x.HasSkillsToTeach, () =>
        {
            RuleFor(x => x.SkillsToTeach)
                .Must(HaveValidSkills)
                .WithMessage("Skills to teach are invalid")
                .WithErrorCode("INVALID_SKILLS_TO_TEACH");
        });

        When(x => x.HasSkillsToLearn, () =>
        {
            RuleFor(x => x.SkillsToLearn)
                .Must(HaveValidSkills)
                .WithMessage("Skills to learn are invalid")
                .WithErrorCode("INVALID_SKILLS_TO_LEARN");
        });

        When(x => x.HasAvatarUrl, () =>
        {
            RuleFor(x => x.AvatarUrl)
                .Must(url => string.IsNullOrWhiteSpace(url) || objectStorageService.IsPublicUrl(url))
                .WithMessage("Avatar URL is invalid")
                .WithErrorCode("INVALID_AVATAR_URL");
        });

        When(x => x.HasIsPublic, () =>
        {
            RuleFor(x => x.IsPublic)
                .NotNull()
                .WithMessage("Profile visibility is required")
                .WithErrorCode("INVALID_PROFILE_VISIBILITY");
        });
    }

    private static bool HaveValidSkills(IReadOnlyCollection<string>? skills)
    {
        if (skills is null)
        {
            return true;
        }

        if (skills.Count > MaxSkillsPerList)
        {
            return false;
        }

        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in skills)
        {
            if (string.IsNullOrWhiteSpace(skill))
            {
                return false;
            }

            var trimmed = skill.Trim();
            if (trimmed.Length > MaxSkillLength || !normalized.Add(trimmed))
            {
                return false;
            }
        }

        return true;
    }

    [GeneratedRegex(@"^[\p{L}\p{N} ]+$", RegexOptions.Compiled)]
    private static partial Regex DisplayNameRegex();
}
