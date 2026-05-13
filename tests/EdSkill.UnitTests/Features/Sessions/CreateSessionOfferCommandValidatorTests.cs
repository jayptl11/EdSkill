using EdSkill.Application.Features.Sessions.Commands.CreateSessionOffer;
using EdSkill.Domain.Enums;
using FluentValidation.TestHelper;

namespace EdSkill.UnitTests.Features.Sessions;

public class CreateSessionOfferCommandValidatorTests
{
    private readonly CreateSessionOfferCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenDurationOptionsContainUnsupportedValue_ShouldHaveError()
    {
        var command = new CreateSessionOfferCommand(
            Guid.NewGuid(),
            "Description",
            SessionDeliveryMode.Online,
            null,
            new[] { 45, 50 },
            DateTime.UtcNow.AddDays(1));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DurationOptions)
            .WithErrorCode("INVALID_DURATION_OPTIONS");
    }

    [Fact]
    public void Validate_WhenOfflineSessionMissingLocation_ShouldHaveError()
    {
        var command = new CreateSessionOfferCommand(
            Guid.NewGuid(),
            "Description",
            SessionDeliveryMode.Offline,
            null,
            new[] { 45, 60 },
            DateTime.UtcNow.AddDays(1));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Location);
    }

    [Fact]
    public void Validate_WhenDurationOptionsAreSupported_ShouldNotHaveError()
    {
        var command = new CreateSessionOfferCommand(
            Guid.NewGuid(),
            "Description",
            SessionDeliveryMode.Online,
            null,
            new[] { 30, 45, 90 },
            DateTime.UtcNow.AddDays(1));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.DurationOptions);
    }
}
