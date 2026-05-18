using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.MySpace.DTOs;
using EdSkill.Domain.Enums;
using MediatR;

namespace EdSkill.Application.Features.MySpace.Commands.UpdateLearnerSpaceCard;

public record UpdateLearnerSpaceCardCommand(
    Guid LearnerSpaceCardId,
    bool HasSkillId,
    Guid? SkillId,
    bool HasTitle,
    string? Title,
    bool HasDescription,
    string? Description,
    bool HasTargetPoints,
    int? TargetPoints,
    bool HasDurationMinutes,
    int? DurationMinutes,
    bool HasDeliveryModes,
    IReadOnlyCollection<SessionDeliveryMode>? DeliveryModes,
    bool HasLanguages,
    IReadOnlyCollection<string>? Languages,
    bool HasCoverImageUrl,
    string? CoverImageUrl,
    bool HasIsPublished,
    bool? IsPublished) : IRequest<Result<LearnerSpaceCardDto>>;
