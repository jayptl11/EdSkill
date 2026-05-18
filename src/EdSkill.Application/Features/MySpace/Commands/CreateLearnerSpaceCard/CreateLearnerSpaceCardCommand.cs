using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.MySpace.DTOs;
using EdSkill.Domain.Enums;
using MediatR;

namespace EdSkill.Application.Features.MySpace.Commands.CreateLearnerSpaceCard;

public record CreateLearnerSpaceCardCommand(
    Guid SkillId,
    string Title,
    string? Description,
    int TargetPoints,
    int DurationMinutes,
    IReadOnlyCollection<SessionDeliveryMode> DeliveryModes,
    IReadOnlyCollection<string>? Languages,
    string? CoverImageUrl,
    bool IsPublished) : IRequest<Result<LearnerSpaceCardDto>>;
