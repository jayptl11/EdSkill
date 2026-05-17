using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Admin.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Admin.Queries.GetAdminPointPackages;

public record GetAdminPointPackagesQuery(
    string? Query,
    bool IncludeInactive,
    bool IncludeDeleted) : IRequest<Result<IReadOnlyCollection<AdminPointPackageDto>>>;
