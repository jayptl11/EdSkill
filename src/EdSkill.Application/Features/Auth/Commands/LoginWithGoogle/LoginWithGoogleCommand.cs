using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Auth.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Auth.Commands.LoginWithGoogle;

public record LoginWithGoogleCommand(
    string IdToken,
    string SignupIntent) : IRequest<Result<LoginResponse>>;
