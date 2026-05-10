using FluentValidation;

namespace EdSkill.Application.Features.Wallet.Queries.GetMyPointTransactions;

public class GetMyPointTransactionsQueryValidator : AbstractValidator<GetMyPointTransactionsQuery>
{
    public GetMyPointTransactionsQueryValidator()
    {
        RuleFor(item => item.Page).GreaterThan(0);
        RuleFor(item => item.Limit).InclusiveBetween(1, 100);
    }
}
