using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Companions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Companions.Queries.SearchCompanions;

public record SearchCompanionsQuery(
    Guid SkillId,
    int? MinimumDurationMinutes,
    int? MaxLearnerChargePoints,
    string? CredentialCountGroup,
    string? DeliveryMode,
    string? Location,
    int Page = 1,
    int Limit = 20) : IRequest<Result<CompanionSearchResultDto>>
{
    internal CompanionCredentialCountGroup? GetCredentialCountGroup()
    {
        return CompanionCredentialCountGroupParser.Parse(CredentialCountGroup);
    }
}
