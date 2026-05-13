using EdSkill.Application.Features.Sessions.Commands.BookSession;
using FluentValidation.TestHelper;

namespace EdSkill.UnitTests.Features.Sessions;

public class BookSessionCommandValidatorTests
{
    private readonly BookSessionCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenSelectedDurationUnsupported_ShouldHaveError()
    {
        var command = new BookSessionCommand(Guid.NewGuid(), 75);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.SelectedDurationMinutes)
            .WithErrorCode("INVALID_SELECTED_DURATION");
    }

    [Fact]
    public void Validate_WhenSelectedDurationSupported_ShouldNotHaveError()
    {
        var command = new BookSessionCommand(Guid.NewGuid(), 90);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.SelectedDurationMinutes);
    }
}
