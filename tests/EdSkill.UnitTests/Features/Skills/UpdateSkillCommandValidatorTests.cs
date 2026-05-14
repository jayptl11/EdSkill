using EdSkill.Application.Features.Skills.Commands.UpdateSkill;
using FluentValidation.TestHelper;

namespace EdSkill.UnitTests.Features.Skills;

public class UpdateSkillCommandValidatorTests
{
    private readonly UpdateSkillCommandValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("book-open")]
    [InlineData("code")]
    [InlineData("paintbrush")]
    public void Validate_WhenIconKeyIsOptionalOrValid_DoesNotReturnError(string? iconKey)
    {
        var command = new UpdateSkillCommand(
            Guid.NewGuid(),
            false, null,
            false, null,
            false, null,
            true, iconKey,
            false, null,
            false, null,
            false, null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.IconKey);
    }

    [Theory]
    [InlineData("Book-Open")]
    [InlineData("book open")]
    [InlineData("book_open")]
    [InlineData("book/open")]
    [InlineData("book@open")]
    public void Validate_WhenIconKeyHasUnsafeFormat_ReturnsError(string iconKey)
    {
        var command = new UpdateSkillCommand(
            Guid.NewGuid(),
            false, null,
            false, null,
            false, null,
            true, iconKey,
            false, null,
            false, null,
            false, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.IconKey)
            .WithErrorCode("INVALID_SKILL_ICON_KEY");
    }

    [Fact]
    public void Validate_WhenIconKeyExceedsMaxLength_ReturnsError()
    {
        var iconKey = new string('a', 51);
        var command = new UpdateSkillCommand(
            Guid.NewGuid(),
            false, null,
            false, null,
            false, null,
            true, iconKey,
            false, null,
            false, null,
            false, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.IconKey)
            .WithErrorCode("INVALID_SKILL_ICON_KEY");
    }
}
