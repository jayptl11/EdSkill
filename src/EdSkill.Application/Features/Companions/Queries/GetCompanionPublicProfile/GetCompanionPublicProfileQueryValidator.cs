using FluentValidation;

namespace EdSkill.Application.Features.Companions.Queries.GetCompanionPublicProfile;

public class GetCompanionPublicProfileQueryValidator : AbstractValidator<GetCompanionPublicProfileQuery>
{
    public GetCompanionPublicProfileQueryValidator()
    {
        RuleFor(item => item.CompanionId)
            .NotEmpty();
    }
}
