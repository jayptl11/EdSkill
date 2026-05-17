using EdSkill.Application.Features.Admin.Commands.UpdatePointPackage;
using FluentValidation.TestHelper;

namespace EdSkill.UnitTests.Features.Admin;

public class UpdatePointPackageCommandValidatorTests
{
    private readonly UpdatePointPackageCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenBonusPointsNegative_ReturnsError()
    {
        var command = new UpdatePointPackageCommand(
            Guid.NewGuid(),
            false, null,
            false, null,
            false, null,
            false, null,
            true, -1,
            false, null,
            false, null,
            false, null,
            false, null,
            false, null,
            false, null,
            false, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.BonusPoints);
    }
}
