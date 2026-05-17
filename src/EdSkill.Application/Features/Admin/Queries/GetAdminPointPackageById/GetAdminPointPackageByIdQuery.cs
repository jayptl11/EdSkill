using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Admin.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Admin.Queries.GetAdminPointPackageById;

public record GetAdminPointPackageByIdQuery(Guid PackageId) : IRequest<Result<AdminPointPackageDto>>;
