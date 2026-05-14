using EdSkill.Application.Features.Companions.Queries.GetCompanionDetail;
using FluentValidation.TestHelper;

namespace EdSkill.UnitTests.Features.Companions;

public class GetCompanionDetailQueryValidatorTests
{
    private readonly GetCompanionDetailQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenFiltersAreValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new GetCompanionDetailQuery(
            Guid.NewGuid(),
            Guid.NewGuid(),
            60,
            300,
            "ThreeOrMore",
            null,
            null,
            1,
            10));

        result.ShouldNotHaveValidationErrorFor(x => x.MinimumDurationMinutes);
        result.ShouldNotHaveValidationErrorFor(x => x.MaxLearnerChargePoints);
        result.ShouldNotHaveValidationErrorFor(x => x.CredentialCountGroup);
    }

    [Fact]
    public void Validate_WhenLegacyFiltersProvided_ShouldHaveErrors()
    {
        var result = _validator.TestValidate(new GetCompanionDetailQuery(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null,
            "Online",
            "District 1",
            1,
            10));

        result.ShouldHaveValidationErrorFor(x => x.DeliveryMode)
            .WithErrorCode("UNSUPPORTED_DELIVERY_MODE_FILTER");
        result.ShouldHaveValidationErrorFor(x => x.Location)
            .WithErrorCode("UNSUPPORTED_LOCATION_FILTER");
    }
}
