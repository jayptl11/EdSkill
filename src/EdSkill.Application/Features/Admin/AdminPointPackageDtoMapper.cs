using EdSkill.Application.Features.Admin.DTOs;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.Admin;

public static class AdminPointPackageDtoMapper
{
    public static AdminPointPackageDto Map(PointPackage package)
    {
        return new AdminPointPackageDto(
            package.PointPackageId,
            package.Code,
            package.Name,
            package.Description,
            package.Points,
            package.BonusPoints,
            package.Points + package.BonusPoints,
            package.PriceVnd,
            package.Currency,
            package.BadgeText,
            package.IsHighlighted,
            package.DisplayOrder,
            package.IsActive,
            package.IsDeleted,
            package.StartsAt,
            package.EndsAt,
            package.CreatedAt,
            package.UpdatedAt);
    }
}
