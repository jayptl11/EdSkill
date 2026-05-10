using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Companions.DTOs;
using EdSkill.Domain.Enums;
using MediatR;

namespace EdSkill.Application.Features.Companions.Queries.GetCompanionDetail;

public record GetCompanionDetailQuery(
    Guid CompanionId,
    Guid SkillId,
    SessionDeliveryMode? DeliveryMode,
    string? Location,
    int ReviewPage = 1,
    int ReviewLimit = 10) : IRequest<Result<CompanionDetailDto>>;
