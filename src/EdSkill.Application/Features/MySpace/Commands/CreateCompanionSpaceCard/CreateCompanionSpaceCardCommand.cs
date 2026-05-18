using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.MySpace.DTOs;
using EdSkill.Domain.Enums;
using MediatR;

namespace EdSkill.Application.Features.MySpace.Commands.CreateCompanionSpaceCard;

public record CreateCompanionSpaceCardCommand(
    Guid SkillId,
    string Title,
    string? Description,
    int PricePoints,
    int DurationMinutes,
    IReadOnlyCollection<SessionDeliveryMode> DeliveryModes,
    IReadOnlyCollection<string>? Languages,
    string? CoverImageUrl,
    IReadOnlyCollection<string>? CredentialUrls,
    bool IsPublished) : IRequest<Result<CompanionSpaceCardDto>>;
