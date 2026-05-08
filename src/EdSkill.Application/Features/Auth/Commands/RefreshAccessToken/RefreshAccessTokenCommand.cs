using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Auth.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Auth.Commands.RefreshAccessToken;

public record RefreshAccessTokenCommand(string RefreshToken) : IRequest<Result<LoginResponse>>;
