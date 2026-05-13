using FluentValidation;

namespace EdSkill.Application.Features.Sessions.Commands.BookSession;

public class BookSessionCommandValidator : AbstractValidator<BookSessionCommand>
{
    private static readonly int[] AllowedDurations = [30, 45, 60, 90, 120];

    public BookSessionCommandValidator()
    {
        RuleFor(item => item.SessionId)
            .NotEmpty()
            .WithMessage("Session id is required.")
            .WithErrorCode("SESSION_NOT_FOUND");

        RuleFor(item => item.SelectedDurationMinutes)
            .Must(value => AllowedDurations.Contains(value))
            .WithMessage("Selected duration is invalid.")
            .WithErrorCode("INVALID_SELECTED_DURATION");
    }
}
