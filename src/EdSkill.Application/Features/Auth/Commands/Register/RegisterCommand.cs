using EdSkill.Application.Common.Models;
using MediatR;

namespace EdSkill.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string Email,
    string Username,
    string FirstName,
    string LastName,
    string Password,
    IReadOnlyCollection<string>? Roles
) : IRequest<Result>;
