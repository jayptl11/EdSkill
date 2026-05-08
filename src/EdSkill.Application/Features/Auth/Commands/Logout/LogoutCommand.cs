using EdSkill.Application.Common.Models;
using MediatR;

namespace EdSkill.Application.Features.Auth.Commands.Logout;

public record LogoutCommand(
    string? AccessToken = null,
    string? RefreshToken = null
) : IRequest<Result>;
