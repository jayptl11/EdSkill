using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Companions.DTOs;
using EdSkill.Domain.Enums;
using MediatR;

namespace EdSkill.Application.Features.Companions.Queries.SearchCompanions;

public record SearchCompanionsQuery(
    Guid SkillId,
    SessionDeliveryMode? DeliveryMode,
    string? Location,
    int Page = 1,
    int Limit = 20) : IRequest<Result<CompanionSearchResultDto>>;
