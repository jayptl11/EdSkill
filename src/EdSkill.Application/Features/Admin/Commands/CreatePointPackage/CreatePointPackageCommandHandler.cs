using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Admin.DTOs;
using EdSkill.Application.Features.Wallet;
using EdSkill.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Admin.Commands.CreatePointPackage;

public class CreatePointPackageCommandHandler : IRequestHandler<CreatePointPackageCommand, Result<AdminPointPackageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreatePointPackageCommandHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AdminPointPackageDto>> Handle(CreatePointPackageCommand request, CancellationToken cancellationToken)
    {
        var code = PointPackageRules.NormalizeCode(request.Code);
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result<AdminPointPackageDto>.Failure("POINT_PACKAGE_INVALID_CODE", "Point package code is invalid.");
        }

        var name = PointPackageRules.NormalizeWhitespace(request.Name);
        var startsAt = request.StartsAt;
        var endsAt = request.EndsAt;
        if (startsAt.HasValue && endsAt.HasValue && startsAt.Value > endsAt.Value)
        {
            return Result<AdminPointPackageDto>.Failure("POINT_PACKAGE_INVALID_TIME_WINDOW", "Point package start time must be before or equal to end time.");
        }

        var exists = await _context.PointPackages
            .AnyAsync(item => item.Code == code, cancellationToken);
        if (exists)
        {
            return Result<AdminPointPackageDto>.Failure("POINT_PACKAGE_CODE_EXISTS", "Point package code already exists.");
        }

        var now = _dateTimeProvider.UtcNow;
        var package = new PointPackage
        {
            PointPackageId = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = PointPackageRules.NormalizeOptionalText(request.Description),
            Points = request.Points,
            BonusPoints = request.BonusPoints,
            PriceVnd = request.PriceVnd,
            Currency = "VND",
            BadgeText = PointPackageRules.NormalizeOptionalText(request.BadgeText),
            IsHighlighted = request.IsHighlighted,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            IsDeleted = false,
            StartsAt = startsAt,
            EndsAt = endsAt,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _context.PointPackages.AddAsync(package, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<AdminPointPackageDto>.Success(AdminPointPackageDtoMapper.Map(package));
    }
}
