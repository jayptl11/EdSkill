using EdSkill.Application.Common.Models;
using MediatR;

namespace EdSkill.Application.Features.MySpace.Commands.DeleteCompanionSpaceCard;

public record DeleteCompanionSpaceCardCommand(Guid CompanionSpaceCardId) : IRequest<Result>;
