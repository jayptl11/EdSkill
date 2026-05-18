using EdSkill.Application.Common.Interfaces;
using FluentValidation;

namespace EdSkill.Application.Features.MySpace.Commands.UpdateLearnerSpaceCard;

public class UpdateLearnerSpaceCardCommandValidator : AbstractValidator<UpdateLearnerSpaceCardCommand>
{
    public UpdateLearnerSpaceCardCommandValidator(IObjectStorageService objectStorageService)
    {
        RuleFor(x => x.LearnerSpaceCardId).NotEmpty();

        When(x => x.HasSkillId, () =>
        {
            RuleFor(x => x.SkillId).NotNull().NotEmpty();
        });

        When(x => x.HasTitle, () =>
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(MySpaceRules.MaxTitleLength);
        });

        When(x => x.HasDescription && x.Description is not null, () =>
        {
            RuleFor(x => x.Description!)
                .MaximumLength(MySpaceRules.MaxDescriptionLength);
        });

        When(x => x.HasTargetPoints, () =>
        {
            RuleFor(x => x.TargetPoints)
                .NotNull()
                .GreaterThanOrEqualTo(0);
        });

        When(x => x.HasDurationMinutes, () =>
        {
            RuleFor(x => x.DurationMinutes)
                .NotNull()
                .Must(duration => duration.HasValue && MySpaceRules.AllowedDurations.Contains(duration.Value))
                .WithErrorCode("INVALID_DURATION_MINUTES")
                .WithMessage("Duration minutes are invalid.");
        });

        When(x => x.HasDeliveryModes, () =>
        {
            RuleFor(x => x.DeliveryModes)
                .Must(modes => modes is { Count: > 0 })
                .WithErrorCode("INVALID_DELIVERY_MODES")
                .WithMessage("At least one delivery mode is required.");
        });

        When(x => x.HasLanguages, () =>
        {
            RuleFor(x => x.Languages)
                .Must(HaveValidLanguages)
                .WithErrorCode("INVALID_LANGUAGES")
                .WithMessage("Languages are invalid.");
        });

        When(x => x.HasCoverImageUrl, () =>
        {
            RuleFor(x => x.CoverImageUrl)
                .Must(url => string.IsNullOrWhiteSpace(url) || objectStorageService.IsPublicUrl(url))
                .WithErrorCode("INVALID_COVER_IMAGE_URL")
                .WithMessage("Cover image URL is invalid.");
        });

        When(x => x.HasIsPublished, () =>
        {
            RuleFor(x => x.IsPublished).NotNull();
        });
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
