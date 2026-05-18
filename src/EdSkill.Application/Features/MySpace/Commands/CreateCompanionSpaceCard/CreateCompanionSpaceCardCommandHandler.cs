using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.MySpace.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.MySpace.Commands.CreateCompanionSpaceCard;

public class CreateCompanionSpaceCardCommandHandler : IRequestHandler<CreateCompanionSpaceCardCommand, Result<CompanionSpaceCardDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateCompanionSpaceCardCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CompanionSpaceCardDto>> Handle(CreateCompanionSpaceCardCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var user = await _context.Users
            .Include(item => item.UserProfile)
            .Include(item => item.UserSkills)
            .ThenInclude(item => item.Skill)
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (user?.UserProfile is null)
        {
            return Result<CompanionSpaceCardDto>.Failure("PROFILE_NOT_FOUND", "Profile was not found.");
        }

        if (!user.Roles.Contains("companion"))
        {
            return Result<CompanionSpaceCardDto>.Failure("FORBIDDEN", "Only companions can create companion cards.");
        }

        var skillResult = MySpaceRules.ResolveOwnedSkill(user, request.SkillId, UserSkillType.Teach);
        if (skillResult.IsFailure)
        {
            return Result<CompanionSpaceCardDto>.Failure(skillResult.ErrorCode!, skillResult.ErrorMessage!);
        }

        var now = _dateTimeProvider.UtcNow;
        var card = new CompanionSpaceCard
        {
            CompanionSpaceCardId = Guid.NewGuid(),
            UserId = userId,
            User = user,
            SkillId = skillResult.Value!.SkillId,
            Skill = skillResult.Value,
            Title = request.Title.Trim(),
            Description = MySpaceRules.NormalizeOptionalString(request.Description),
            PricePoints = request.PricePoints,
            DurationMinutes = request.DurationMinutes,
            DeliveryModes = MySpaceRules.NormalizeDeliveryModes(request.DeliveryModes),
            Languages = MySpaceRules.NormalizeLanguages(request.Languages),
            CoverImageUrl = MySpaceRules.NormalizeOptionalString(request.CoverImageUrl),
            CredentialUrls = MySpaceRules.NormalizeCredentialUrls(request.CredentialUrls),
            IsPublished = request.IsPublished,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _context.CompanionSpaceCards.AddAsync(card, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<CompanionSpaceCardDto>.Success(MySpaceDtoMapper.MapCompanionCard(card));
    }
}
