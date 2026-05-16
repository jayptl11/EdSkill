using EdSkill.Application.Features.Achievements;
using FluentValidation;

namespace EdSkill.Application.Features.Admin.Commands.CreateAchievement;

public class CreateAchievementCommandValidator : AbstractValidator<CreateAchievementCommand>
{
    public CreateAchievementCommandValidator()
    {
        RuleFor(item => item.Name)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(item => item.Description)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(item => item.Track)
            .Must(value => AchievementParsing.TryParseTrack(value, out _))
            .WithMessage("Achievement track is invalid.")
            .WithErrorCode("INVALID_ACHIEVEMENT_TRACK");

        RuleFor(item => item.Metric)
            .Must(value => AchievementParsing.TryParseMetric(value, out _))
            .WithMessage("Achievement metric is invalid.")
            .WithErrorCode("INVALID_ACHIEVEMENT_METRIC");

        RuleFor(item => item.Threshold)
            .GreaterThan(0)
            .WithMessage("Achievement threshold is invalid.")
            .WithErrorCode("INVALID_ACHIEVEMENT_THRESHOLD");

        RuleFor(item => item)
            .Must(item => !AchievementParsing.TryParseMetric(item.Metric, out var metric)
                || metric != Domain.Enums.AchievementMetric.DistinctCompletedLearners
                || string.Equals(item.Track, "companion", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Distinct completed learners metric only supports companion track.")
            .WithErrorCode("INVALID_ACHIEVEMENT_METRIC");
    }
}
