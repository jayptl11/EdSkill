using EdSkill.Application.Features.Auth;
using FluentValidation;

namespace EdSkill.Application.Features.Auth.Commands.LoginWithGoogle;

public class LoginWithGoogleCommandValidator : AbstractValidator<LoginWithGoogleCommand>
{
    public LoginWithGoogleCommandValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty()
            .WithMessage("IdToken is required")
            .WithErrorCode("ID_TOKEN_REQUIRED");

        RuleFor(x => x.SignupIntent)
            .NotEmpty()
            .WithMessage("Signup intent is required")
            .WithErrorCode("INVALID_SIGNUP_INTENT")
            .Must(SignupIntents.IsValid)
            .WithMessage("Signup intent must be learn or teach")
            .WithErrorCode("INVALID_SIGNUP_INTENT");
    }
}
