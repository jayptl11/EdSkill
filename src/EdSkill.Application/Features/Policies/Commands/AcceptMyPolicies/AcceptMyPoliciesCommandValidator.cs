using EdSkill.Application.Common.Policies;
using FluentValidation;

namespace EdSkill.Application.Features.Policies.Commands.AcceptMyPolicies;

public class AcceptMyPoliciesCommandValidator : AbstractValidator<AcceptMyPoliciesCommand>
{
    public AcceptMyPoliciesCommandValidator()
    {
        RuleFor(x => x.AcceptedPolicies)
            .NotNull()
            .WithMessage("Accepted policies are required.")
            .WithErrorCode("VALIDATION_ERROR")
            .Must(policies => policies is { Count: > 0 })
            .WithMessage("Accepted policies are required.")
            .WithErrorCode("VALIDATION_ERROR");

        RuleForEach(x => x.AcceptedPolicies!)
            .ChildRules(policy =>
            {
                policy.RuleFor(x => x.PolicyType)
                    .NotEmpty()
                    .WithMessage("Policy type is required.")
                    .WithErrorCode("VALIDATION_ERROR")
                    .Must(type => PolicyTypeMapper.TryParse(type, out _))
                    .WithMessage("Policy type is invalid.")
                    .WithErrorCode("VALIDATION_ERROR");

                policy.RuleFor(x => x.PolicyVersion)
                    .NotEmpty()
                    .WithMessage("Policy version is required.")
                    .WithErrorCode("VALIDATION_ERROR");
            });
    }
}
