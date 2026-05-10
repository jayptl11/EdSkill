using FluentValidation;

namespace EdSkill.Application.Features.Reviews.Commands.CreateReview;

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(item => item.SessionId).NotEmpty();
        RuleFor(item => item.Rating).InclusiveBetween(1, 5);
        RuleFor(item => item.Comment)
            .MaximumLength(1000)
            .When(item => !string.IsNullOrWhiteSpace(item.Comment));
    }
}
