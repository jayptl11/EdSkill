using EdSkill.Application.Features.Admin.Commands.UpdateAchievement;
using FluentValidation.TestHelper;

namespace EdSkill.UnitTests.Features.Admin;

public class UpdateAchievementCommandValidatorTests
{
    private readonly UpdateAchievementCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenTrackIsInvalid_ReturnsError()
    {
        var command = new UpdateAchievementCommand(
            Guid.NewGuid(),
            false,
            null,
            false,
            null,
            false,
            null,
            true,
            "mentor",
            false,
            null,
            false,
            null,
            false,
            null,
            false,
            null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Track)
            .WithErrorCode("INVALID_ACHIEVEMENT_TRACK");
    }
}
