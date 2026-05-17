using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Admin.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Admin.Commands.UpdatePointPackage;

public record UpdatePointPackageCommand(
    Guid PackageId,
    bool HasCode,
    string? Code,
    bool HasName,
    string? Name,
    bool HasDescription,
    string? Description,
    bool HasPoints,
    int? Points,
    bool HasBonusPoints,
    int? BonusPoints,
    bool HasPriceVnd,
    int? PriceVnd,
    bool HasBadgeText,
    string? BadgeText,
    bool HasIsHighlighted,
    bool? IsHighlighted,
    bool HasDisplayOrder,
    int? DisplayOrder,
    bool HasIsActive,
    bool? IsActive,
    bool HasStartsAt,
    DateTime? StartsAt,
    bool HasEndsAt,
    DateTime? EndsAt) : IRequest<Result<AdminPointPackageDto>>;
