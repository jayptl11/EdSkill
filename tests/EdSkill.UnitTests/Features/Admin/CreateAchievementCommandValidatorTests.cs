using EdSkill.Application.Features.Admin.Commands.CreateAchievement;
using FluentValidation.TestHelper;

namespace EdSkill.UnitTests.Features.Admin;

public class CreateAchievementCommandValidatorTests
{
    private readonly CreateAchievementCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenDistinctCompletedLearnersUsesLearnerTrack_ReturnsError()
    {
        var command = new CreateAchievementCommand(
            "Learner Milestone",
            "Description",
            null,
            "learner",
            "distinct_completed_learners",
            1,
            0);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode("INVALID_ACHIEVEMENT_METRIC");
    }

    [Fact]
    public void Validate_WhenThresholdIsNotPositive_ReturnsError()
    {
        var command = new CreateAchievementCommand(
            "Companion Milestone",
            "Description",
            null,
            "companion",
            "completed_sessions",
            0,
            0);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Threshold)
            .WithErrorCode("INVALID_ACHIEVEMENT_THRESHOLD");
    }
}
