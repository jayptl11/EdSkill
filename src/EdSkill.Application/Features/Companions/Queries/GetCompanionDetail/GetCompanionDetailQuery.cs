using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Companions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Companions.Queries.GetCompanionDetail;

public record GetCompanionDetailQuery(
    Guid CompanionId,
    Guid SkillId,
    int? MinimumDurationMinutes,
    int? MaxLearnerChargePoints,
    string? CredentialCountGroup,
    string? DeliveryMode,
    string? Location,
    int ReviewPage = 1,
    int ReviewLimit = 10) : IRequest<Result<CompanionDetailDto>>
{
    internal CompanionCredentialCountGroup? GetCredentialCountGroup()
    {
        return CompanionCredentialCountGroupParser.Parse(CredentialCountGroup);
    }
}
