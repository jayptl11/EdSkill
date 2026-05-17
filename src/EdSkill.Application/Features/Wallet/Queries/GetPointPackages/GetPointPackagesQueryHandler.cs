using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Wallet.Queries.GetPointPackages;

public class GetPointPackagesQueryHandler : IRequestHandler<GetPointPackagesQuery, Result<PointPackageListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetPointPackagesQueryHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<PointPackageListDto>> Handle(GetPointPackagesQuery request, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow;
        var packages = await _context.PointPackages
            .AsNoTracking()
            .Where(item => item.IsActive && !item.IsDeleted)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.PriceVnd)
            .ToListAsync(cancellationToken);

        var results = packages
            .Where(item => PointPackageRules.IsAvailableForSale(item, utcNow))
            .Select(WalletDtoMapper.MapPackage)
            .ToList();

        return Result<PointPackageListDto>.Success(new PointPackageListDto(results));
    }
}
