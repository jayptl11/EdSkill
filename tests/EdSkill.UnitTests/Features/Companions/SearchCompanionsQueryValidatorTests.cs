using EdSkill.Application.Features.Companions.Queries.SearchCompanions;
using FluentValidation.TestHelper;

namespace EdSkill.UnitTests.Features.Companions;

public class SearchCompanionsQueryValidatorTests
{
    private readonly SearchCompanionsQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenSkillIdMissing_ShouldNotHaveError()
    {
        var result = _validator.TestValidate(new SearchCompanionsQuery(null, null, null, null, null, null));

        result.ShouldNotHaveValidationErrorFor(x => x.SkillId);
    }

    [Fact]
    public void Validate_WhenDeliveryModeProvided_ShouldHaveError()
    {
        var result = _validator.TestValidate(new SearchCompanionsQuery(Guid.NewGuid(), null, null, null, "Offline", null));

        result.ShouldHaveValidationErrorFor(x => x.DeliveryMode)
            .WithErrorCode("UNSUPPORTED_DELIVERY_MODE_FILTER");
    }

    [Fact]
    public void Validate_WhenLocationProvided_ShouldHaveError()
    {
        var result = _validator.TestValidate(new SearchCompanionsQuery(Guid.NewGuid(), null, null, null, null, "District 1"));

        result.ShouldHaveValidationErrorFor(x => x.Location)
            .WithErrorCode("UNSUPPORTED_LOCATION_FILTER");
    }

    [Fact]
    public void Validate_WhenMinimumDurationInvalid_ShouldHaveError()
    {
        var result = _validator.TestValidate(new SearchCompanionsQuery(Guid.NewGuid(), 75, null, null, null, null));

        result.ShouldHaveValidationErrorFor(x => x.MinimumDurationMinutes)
            .WithErrorCode("INVALID_MINIMUM_DURATION");
    }

    [Fact]
    public void Validate_WhenMaxLearnerChargePointsNonPositive_ShouldHaveError()
    {
        var result = _validator.TestValidate(new SearchCompanionsQuery(Guid.NewGuid(), null, 0, null, null, null));

        result.ShouldHaveValidationErrorFor(x => x.MaxLearnerChargePoints)
            .WithErrorCode("INVALID_MAX_LEARNER_CHARGE_POINTS");
    }

    [Fact]
    public void Validate_WhenCredentialCountGroupInvalid_ShouldHaveError()
    {
        var result = _validator.TestValidate(new SearchCompanionsQuery(Guid.NewGuid(), null, null, "Four", null, null));

        result.ShouldHaveValidationErrorFor(x => x.CredentialCountGroup)
            .WithErrorCode("INVALID_CREDENTIAL_COUNT_GROUP");
    }
}
