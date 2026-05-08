using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Auth.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Identifier, string Password) : IRequest<Result<LoginResponse>>;
