using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.MySpace.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.MySpace.Commands.CreateLearnerSpaceCard;

public class CreateLearnerSpaceCardCommandHandler : IRequestHandler<CreateLearnerSpaceCardCommand, Result<LearnerSpaceCardDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateLearnerSpaceCardCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<LearnerSpaceCardDto>> Handle(CreateLearnerSpaceCardCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var user = await _context.Users
            .Include(item => item.UserProfile)
            .Include(item => item.UserSkills)
            .ThenInclude(item => item.Skill)
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (user?.UserProfile is null)
        {
            return Result<LearnerSpaceCardDto>.Failure("PROFILE_NOT_FOUND", "Profile was not found.");
        }

        var skillResult = MySpaceRules.ResolveOwnedSkill(user, request.SkillId, UserSkillType.Learn);
        if (skillResult.IsFailure)
        {
            return Result<LearnerSpaceCardDto>.Failure(skillResult.ErrorCode!, skillResult.ErrorMessage!);
        }

        var now = _dateTimeProvider.UtcNow;
        var card = new LearnerSpaceCard
        {
            LearnerSpaceCardId = Guid.NewGuid(),
            UserId = userId,
            User = user,
            SkillId = skillResult.Value!.SkillId,
            Skill = skillResult.Value,
            Title = request.Title.Trim(),
            Description = MySpaceRules.NormalizeOptionalString(request.Description),
            TargetPoints = request.TargetPoints,
            DurationMinutes = request.DurationMinutes,
            DeliveryModes = MySpaceRules.NormalizeDeliveryModes(request.DeliveryModes),
            Languages = MySpaceRules.NormalizeLanguages(request.Languages),
            CoverImageUrl = MySpaceRules.NormalizeOptionalString(request.CoverImageUrl),
            IsPublished = request.IsPublished,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _context.LearnerSpaceCards.AddAsync(card, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<LearnerSpaceCardDto>.Success(MySpaceDtoMapper.MapLearnerCard(card));
    }
}
