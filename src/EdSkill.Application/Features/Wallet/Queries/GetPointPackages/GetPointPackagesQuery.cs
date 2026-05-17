using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Wallet.Queries.GetPointPackages;

public record GetPointPackagesQuery() : IRequest<Result<PointPackageListDto>>;
