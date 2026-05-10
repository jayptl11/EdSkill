using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Admin.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Admin.Queries.GetSystemConfigs;

public record GetSystemConfigsQuery() : IRequest<Result<IReadOnlyCollection<SystemConfigDto>>>;
