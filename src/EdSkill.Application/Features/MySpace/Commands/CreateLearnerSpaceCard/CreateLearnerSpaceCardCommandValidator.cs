using EdSkill.Application.Common.Interfaces;
using FluentValidation;

namespace EdSkill.Application.Features.MySpace.Commands.CreateLearnerSpaceCard;

public class CreateLearnerSpaceCardCommandValidator : AbstractValidator<CreateLearnerSpaceCardCommand>
{
    public CreateLearnerSpaceCardCommandValidator(IObjectStorageService objectStorageService)
    {
        RuleFor(x => x.SkillId).NotEmpty();
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(MySpaceRules.MaxTitleLength);
        RuleFor(x => x.Description)
            .MaximumLength(MySpaceRules.MaxDescriptionLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
        RuleFor(x => x.TargetPoints).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DurationMinutes)
            .Must(duration => MySpaceRules.AllowedDurations.Contains(duration))
            .WithErrorCode("INVALID_DURATION_MINUTES")
            .WithMessage("Duration minutes are invalid.");
        RuleFor(x => x.DeliveryModes)
            .Must(modes => modes is { Count: > 0 })
            .WithErrorCode("INVALID_DELIVERY_MODES")
            .WithMessage("At least one delivery mode is required.");
        RuleFor(x => x.Languages)
            .Must(HaveValidLanguages)
            .WithErrorCode("INVALID_LANGUAGES")
            .WithMessage("Languages are invalid.");
        RuleFor(x => x.CoverImageUrl)
            .Must(url => string.IsNullOrWhiteSpace(url) || objectStorageService.IsPublicUrl(url))
            .WithErrorCode("INVALID_COVER_IMAGE_URL")
            .WithMessage("Cover image URL is invalid.");
    }

    private static bool HaveValidLanguages(IReadOnlyCollection<string>? languages)
    {
        if (languages is null)
        {
            return true;
        }

        if (languages.Count > MySpaceRules.MaxLanguages)
        {
            return false;
        }

        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var language in languages)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return false;
            }

            var trimmed = language.Trim();
            if (trimmed.Length > MySpaceRules.MaxLanguageLength || !normalized.Add(trimmed))
            {
                return false;
            }
        }

        return true;
    }
}
