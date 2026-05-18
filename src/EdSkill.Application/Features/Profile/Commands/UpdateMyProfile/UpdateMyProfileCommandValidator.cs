using EdSkill.Application.Common.Interfaces;
using FluentValidation;
using System.Text.RegularExpressions;

namespace EdSkill.Application.Features.Profile.Commands.UpdateMyProfile;

public partial class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    private const int MaxSkillsPerList = 20;
    private const int MaxSkillLength = 50;
    private const int MaxCredentials = 10;
    private const int MaxAddressLength = 200;

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

        When(x => x.HasDateOfBirth, () =>
        {
            RuleFor(x => x.DateOfBirth)
                .Must(value => value is null || (value.Value.Date >= new DateTime(1900, 1, 1) && value.Value.Date <= DateTime.UtcNow.Date))
                .WithMessage("Date of birth is invalid")
                .WithErrorCode("INVALID_DATE_OF_BIRTH");
        });

        When(x => x.HasPhone && x.Phone is not null, () =>
        {
            RuleFor(x => x.Phone!)
                .Must(phone => phone.Trim().Length is >= 8 and <= 20)
                .WithMessage("Phone number length is invalid")
                .WithErrorCode("INVALID_PHONE")
                .Must(phone => PhoneRegex().IsMatch(phone.Trim()))
                .WithMessage("Phone number format is invalid")
                .WithErrorCode("INVALID_PHONE");
        });

        When(x => x.HasSocialLinkUrl && x.SocialLinkUrl is not null, () =>
        {
            RuleFor(x => x.SocialLinkUrl!)
                .Must(BeAbsoluteUrl)
                .WithMessage("Social link URL is invalid")
                .WithErrorCode("INVALID_SOCIAL_LINK_URL");
        });

        When(x => x.HasAddress && x.Address is not null, () =>
        {
            RuleFor(x => x.Address!)
                .Must(address => address.Trim().Length <= MaxAddressLength)
                .WithMessage($"Address must not exceed {MaxAddressLength} characters")
                .WithErrorCode("INVALID_ADDRESS");
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

        When(x => x.HasDegreeUrl, () =>
        {
            RuleFor(x => x.DegreeUrl)
                .Must(url => string.IsNullOrWhiteSpace(url) || objectStorageService.IsPublicUrl(url))
                .WithMessage("Degree URL is invalid")
                .WithErrorCode("INVALID_DEGREE_URL");
        });

        When(x => x.HasCredentialUrls, () =>
        {
            RuleFor(x => x.CredentialUrls)
                .Must(urls => HaveValidCredentialUrls(urls, objectStorageService))
                .WithMessage("Credential URLs are invalid")
                .WithErrorCode("INVALID_CREDENTIAL_URLS");
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

    private static bool HaveValidCredentialUrls(IReadOnlyCollection<string>? urls, IObjectStorageService objectStorageService)
    {
        if (urls is null)
        {
            return true;
        }

        if (urls.Count > MaxCredentials)
        {
            return false;
        }

        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var url in urls)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            var trimmed = url.Trim();
            if (!objectStorageService.IsPublicUrl(trimmed) || !normalized.Add(trimmed))
            {
                return false;
            }
        }

        return true;
    }

    private static bool BeAbsoluteUrl(string url)
    {
        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    [GeneratedRegex(@"^[\p{L}\p{N} ]+$", RegexOptions.Compiled)]
    private static partial Regex DisplayNameRegex();

    [GeneratedRegex(@"^[0-9+\-() ]+$", RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();
}
