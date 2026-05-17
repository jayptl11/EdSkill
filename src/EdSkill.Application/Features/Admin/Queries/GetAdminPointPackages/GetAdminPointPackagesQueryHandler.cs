using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Admin.DTOs;
using EdSkill.Application.Features.Wallet;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Admin.Queries.GetAdminPointPackages;

public class GetAdminPointPackagesQueryHandler : IRequestHandler<GetAdminPointPackagesQuery, Result<IReadOnlyCollection<AdminPointPackageDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAdminPointPackagesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyCollection<AdminPointPackageDto>>> Handle(GetAdminPointPackagesQuery request, CancellationToken cancellationToken)
    {
        var normalizedQuery = string.IsNullOrWhiteSpace(request.Query)
            ? null
            : PointPackageRules.NormalizeLookup(request.Query);

        var packages = await _context.PointPackages
            .AsNoTracking()
            .Where(item => request.IncludeDeleted || !item.IsDeleted)
            .Where(item => request.IncludeInactive || item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.PriceVnd)
            .ToListAsync(cancellationToken);

        var results = packages
            .Where(item => normalizedQuery is null || MatchesQuery(item, normalizedQuery))
            .Select(AdminPointPackageDtoMapper.Map)
            .ToList();

        return Result<IReadOnlyCollection<AdminPointPackageDto>>.Success(results);
    }

    private static bool MatchesQuery(Domain.Entities.PointPackage package, string normalizedQuery)
    {
        return PointPackageRules.NormalizeLookup(package.Code).Contains(normalizedQuery, StringComparison.Ordinal)
            || PointPackageRules.NormalizeLookup(package.Name).Contains(normalizedQuery, StringComparison.Ordinal)
            || (!string.IsNullOrWhiteSpace(package.Description)
                && PointPackageRules.NormalizeLookup(package.Description).Contains(normalizedQuery, StringComparison.Ordinal))
            || (!string.IsNullOrWhiteSpace(package.BadgeText)
                && PointPackageRules.NormalizeLookup(package.BadgeText).Contains(normalizedQuery, StringComparison.Ordinal));
    }
}
