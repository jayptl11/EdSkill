using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Admin.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Admin.Queries.GetAdminPointPackageById;

public class GetAdminPointPackageByIdQueryHandler : IRequestHandler<GetAdminPointPackageByIdQuery, Result<AdminPointPackageDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAdminPointPackageByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AdminPointPackageDto>> Handle(GetAdminPointPackageByIdQuery request, CancellationToken cancellationToken)
    {
        var package = await _context.PointPackages
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.PointPackageId == request.PackageId, cancellationToken);

        if (package == null)
        {
            return Result<AdminPointPackageDto>.Failure("POINT_PACKAGE_NOT_FOUND", "Point package was not found.");
        }

        return Result<AdminPointPackageDto>.Success(AdminPointPackageDtoMapper.Map(package));
    }
}
