using FluentValidation;

namespace EdSkill.Application.Features.Wallet.Queries.GetMyPayments;

public class GetMyPaymentsQueryValidator : AbstractValidator<GetMyPaymentsQuery>
{
    public GetMyPaymentsQueryValidator()
    {
        RuleFor(item => item.Page).GreaterThan(0);
        RuleFor(item => item.Limit).InclusiveBetween(1, 100);
    }
}
