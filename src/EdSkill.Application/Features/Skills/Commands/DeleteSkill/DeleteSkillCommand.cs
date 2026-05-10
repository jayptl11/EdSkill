using EdSkill.Application.Common.Models;
using MediatR;

namespace EdSkill.Application.Features.Skills.Commands.DeleteSkill;

public record DeleteSkillCommand(Guid SkillId) : IRequest<Result>;
