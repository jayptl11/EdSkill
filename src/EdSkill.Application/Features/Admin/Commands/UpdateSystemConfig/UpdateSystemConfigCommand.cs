using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Admin.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Admin.Commands.UpdateSystemConfig;

public record UpdateSystemConfigCommand(string Key, string Value) : IRequest<Result<SystemConfigDto>>;
