using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Profile.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Profile.Commands.EnableCompanion;

public record EnableCompanionCommand : IRequest<Result<ProfileDto>>;
