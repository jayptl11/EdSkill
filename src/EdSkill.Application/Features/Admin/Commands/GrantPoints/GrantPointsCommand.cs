using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Admin.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Admin.Commands.GrantPoints;

public record GrantPointsCommand(
    IReadOnlyCollection<Guid> UserIds,
    int Amount,
    string Note) : IRequest<Result<GrantPointsResultDto>>;
