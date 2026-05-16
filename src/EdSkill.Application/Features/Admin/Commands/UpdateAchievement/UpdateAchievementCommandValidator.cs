using EdSkill.Application.Features.Achievements;
using FluentValidation;

namespace EdSkill.Application.Features.Admin.Commands.UpdateAchievement;

public class UpdateAchievementCommandValidator : AbstractValidator<UpdateAchievementCommand>
{
    public UpdateAchievementCommandValidator()
    {
        RuleFor(item => item.AchievementId)
            .NotEmpty();

        RuleFor(item => item.Name)
            .MaximumLength(120)
            .When(item => item.HasName && item.Name is not null);

        RuleFor(item => item.Description)
            .MaximumLength(500)
            .When(item => item.HasDescription && item.Description is not null);

        RuleFor(item => item.Track)
            .Must(value => value is null || AchievementParsing.TryParseTrack(value, out _))
            .When(item => item.HasTrack)
            .WithMessage("Achievement track is invalid.")
            .WithErrorCode("INVALID_ACHIEVEMENT_TRACK");

        RuleFor(item => item.Metric)
            .Must(value => value is null || AchievementParsing.TryParseMetric(value, out _))
            .When(item => item.HasMetric)
            .WithMessage("Achievement metric is invalid.")
            .WithErrorCode("INVALID_ACHIEVEMENT_METRIC");

        RuleFor(item => item.Threshold)
            .Must(value => !value.HasValue || value.Value > 0)
            .When(item => item.HasThreshold)
            .WithMessage("Achievement threshold is invalid.")
            .WithErrorCode("INVALID_ACHIEVEMENT_THRESHOLD");
    }
}
