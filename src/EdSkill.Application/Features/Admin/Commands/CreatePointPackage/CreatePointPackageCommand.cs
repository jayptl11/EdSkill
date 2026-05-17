using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Admin.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Admin.Commands.CreatePointPackage;

public record CreatePointPackageCommand(
    string Code,
    string Name,
    string? Description,
    int Points,
    int BonusPoints,
    int PriceVnd,
    string? BadgeText,
    bool IsHighlighted,
    int DisplayOrder,
    bool IsActive,
    DateTime? StartsAt,
    DateTime? EndsAt) : IRequest<Result<AdminPointPackageDto>>;
