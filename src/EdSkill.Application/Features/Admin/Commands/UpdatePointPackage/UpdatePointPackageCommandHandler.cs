using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Admin.DTOs;
using EdSkill.Application.Features.Wallet;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Admin.Commands.UpdatePointPackage;

public class UpdatePointPackageCommandHandler : IRequestHandler<UpdatePointPackageCommand, Result<AdminPointPackageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdatePointPackageCommandHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AdminPointPackageDto>> Handle(UpdatePointPackageCommand request, CancellationToken cancellationToken)
    {
        var package = await _context.PointPackages
            .FirstOrDefaultAsync(item => item.PointPackageId == request.PackageId, cancellationToken);

        if (package == null)
        {
            return Result<AdminPointPackageDto>.Failure("POINT_PACKAGE_NOT_FOUND", "Point package was not found.");
        }

        if (request.HasCode)
        {
            var code = PointPackageRules.NormalizeCode(request.Code ?? string.Empty);
            if (string.IsNullOrWhiteSpace(code))
            {
                return Result<AdminPointPackageDto>.Failure("POINT_PACKAGE_INVALID_CODE", "Point package code is invalid.");
            }

            var exists = await _context.PointPackages
                .AnyAsync(item => item.PointPackageId != request.PackageId && item.Code == code, cancellationToken);
            if (exists)
            {
                return Result<AdminPointPackageDto>.Failure("POINT_PACKAGE_CODE_EXISTS", "Point package code already exists.");
            }

            package.Code = code;
        }

        if (request.HasName)
        {
            package.Name = PointPackageRules.NormalizeWhitespace(request.Name!);
        }

        if (request.HasDescription)
        {
            package.Description = PointPackageRules.NormalizeOptionalText(request.Description);
        }

        if (request.HasPoints && request.Points.HasValue)
        {
            package.Points = request.Points.Value;
        }

        if (request.HasBonusPoints && request.BonusPoints.HasValue)
        {
            package.BonusPoints = request.BonusPoints.Value;
        }

        if (request.HasPriceVnd && request.PriceVnd.HasValue)
        {
            package.PriceVnd = request.PriceVnd.Value;
        }

        if (request.HasBadgeText)
        {
            package.BadgeText = PointPackageRules.NormalizeOptionalText(request.BadgeText);
        }

        if (request.HasIsHighlighted && request.IsHighlighted.HasValue)
        {
            package.IsHighlighted = request.IsHighlighted.Value;
        }

        if (request.HasDisplayOrder && request.DisplayOrder.HasValue)
        {
            package.DisplayOrder = request.DisplayOrder.Value;
        }

        if (request.HasIsActive && request.IsActive.HasValue)
        {
            package.IsActive = request.IsActive.Value;
        }

        var startsAt = request.HasStartsAt ? request.StartsAt : package.StartsAt;
        var endsAt = request.HasEndsAt ? request.EndsAt : package.EndsAt;
        if (startsAt.HasValue && endsAt.HasValue && startsAt.Value > endsAt.Value)
        {
            return Result<AdminPointPackageDto>.Failure("POINT_PACKAGE_INVALID_TIME_WINDOW", "Point package start time must be before or equal to end time.");
        }

        if (request.HasStartsAt)
        {
            package.StartsAt = request.StartsAt;
        }

        if (request.HasEndsAt)
        {
            package.EndsAt = request.EndsAt;
        }

        package.UpdatedAt = _dateTimeProvider.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result<AdminPointPackageDto>.Success(AdminPointPackageDtoMapper.Map(package));
    }
}
