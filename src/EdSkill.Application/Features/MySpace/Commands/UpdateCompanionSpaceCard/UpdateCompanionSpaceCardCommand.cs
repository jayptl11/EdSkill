using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.MySpace.DTOs;
using EdSkill.Domain.Enums;
using MediatR;

namespace EdSkill.Application.Features.MySpace.Commands.UpdateCompanionSpaceCard;

public record UpdateCompanionSpaceCardCommand(
    Guid CompanionSpaceCardId,
    bool HasSkillId,
    Guid? SkillId,
    bool HasTitle,
    string? Title,
    bool HasDescription,
    string? Description,
    bool HasPricePoints,
    int? PricePoints,
    bool HasDurationMinutes,
    int? DurationMinutes,
    bool HasDeliveryModes,
    IReadOnlyCollection<SessionDeliveryMode>? DeliveryModes,
    bool HasLanguages,
    IReadOnlyCollection<string>? Languages,
    bool HasCoverImageUrl,
    string? CoverImageUrl,
    bool HasCredentialUrls,
    IReadOnlyCollection<string>? CredentialUrls,
    bool HasIsPublished,
    bool? IsPublished) : IRequest<Result<CompanionSpaceCardDto>>;
