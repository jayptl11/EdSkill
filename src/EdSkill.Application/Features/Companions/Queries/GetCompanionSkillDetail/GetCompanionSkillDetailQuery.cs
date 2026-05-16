using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Companions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Companions.Queries.GetCompanionSkillDetail;

public record GetCompanionSkillDetailQuery(
    Guid CompanionId,
    Guid SkillId,
    int ReviewPage = 1,
    int ReviewLimit = 10,
    int OfferPage = 1,
    int OfferLimit = 20) : IRequest<Result<CompanionSkillDetailDto>>;
