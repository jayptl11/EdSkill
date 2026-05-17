using EdSkill.Application.Features.Admin.Commands.CreatePointPackage;
using FluentValidation.TestHelper;

namespace EdSkill.UnitTests.Features.Admin;

public class CreatePointPackageCommandValidatorTests
{
    private readonly CreatePointPackageCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenPointsIsNotPositive_ReturnsError()
    {
        var command = new CreatePointPackageCommand(
            "starter",
            "Starter",
            null,
            0,
            0,
            59000,
            null,
            false,
            1,
            true,
            null,
            null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Points);
    }

    [Fact]
    public void Validate_WhenStartsAtAfterEndsAt_ReturnsError()
    {
        var command = new CreatePointPackageCommand(
            "starter",
            "Starter",
            null,
            500,
            0,
            59000,
            null,
            false,
            1,
            true,
            new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 19, 0, 0, 0, DateTimeKind.Utc));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("POINT_PACKAGE_INVALID_TIME_WINDOW");
    }
}
