using FluentValidation;

namespace EdSkill.Application.Features.Sessions.Queries.GetSessions;

public class GetSessionsQueryValidator : AbstractValidator<GetSessionsQuery>
{
    public GetSessionsQueryValidator()
    {
        RuleFor(item => item.Page).GreaterThan(0);
        RuleFor(item => item.Limit).InclusiveBetween(1, 100);
    }
}
