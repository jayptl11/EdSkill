using EdSkill.Application.Common.Models;
using MediatR;

namespace EdSkill.Application.Features.MySpace.Commands.DeleteLearnerSpaceCard;

public record DeleteLearnerSpaceCardCommand(Guid LearnerSpaceCardId) : IRequest<Result>;
