using FluentValidation;

namespace EdSkill.Application.Features.Companions.Queries.GetCompanionDetail;

public class GetCompanionDetailQueryValidator : AbstractValidator<GetCompanionDetailQuery>
{
    private static readonly int[] AllowedDurations = [30, 45, 60, 90, 120];

    public GetCompanionDetailQueryValidator()
    {
        RuleFor(item => item.CompanionId)
            .NotEmpty();

        RuleFor(item => item.SkillId)
            .NotEmpty();

        RuleFor(item => item.MinimumDurationMinutes)
            .Must(value => value is null || AllowedDurations.Contains(value.Value))
            .WithMessage("Minimum duration filter is invalid.")
            .WithErrorCode("INVALID_MINIMUM_DURATION");

        RuleFor(item => item.MaxLearnerChargePoints)
            .Must(value => value is null || value > 0)
            .WithMessage("Max learner charge points filter is invalid.")
            .WithErrorCode("INVALID_MAX_LEARNER_CHARGE_POINTS");

        RuleFor(item => item.CredentialCountGroup)
            .Must(value => string.IsNullOrWhiteSpace(value) || CompanionCredentialCountGroupParser.IsValid(value))
            .WithMessage("Credential count group filter is invalid.")
            .WithErrorCode("INVALID_CREDENTIAL_COUNT_GROUP");

        RuleFor(item => item.DeliveryMode)
            .Must(string.IsNullOrWhiteSpace)
            .WithMessage("Companion discovery now supports online offers only. Remove deliveryMode from the request.")
            .WithErrorCode("UNSUPPORTED_DELIVERY_MODE_FILTER");

        RuleFor(item => item.Location)
            .Must(string.IsNullOrWhiteSpace)
            .WithMessage("Companion discovery now supports online offers only. Remove location from the request.")
            .WithErrorCode("UNSUPPORTED_LOCATION_FILTER");

        RuleFor(item => item.ReviewPage)
            .GreaterThan(0);

        RuleFor(item => item.ReviewLimit)
            .InclusiveBetween(1, 100);
    }
}
