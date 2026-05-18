using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.MySpace.DTOs;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.MySpace.Commands.UpdateLearnerSpaceCard;

public class UpdateLearnerSpaceCardCommandHandler : IRequestHandler<UpdateLearnerSpaceCardCommand, Result<LearnerSpaceCardDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateLearnerSpaceCardCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<LearnerSpaceCardDto>> Handle(UpdateLearnerSpaceCardCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var card = await _context.LearnerSpaceCards
            .Include(item => item.Skill)
            .FirstOrDefaultAsync(item => item.LearnerSpaceCardId == request.LearnerSpaceCardId && item.UserId == userId, cancellationToken);

        if (card is null)
        {
            return Result<LearnerSpaceCardDto>.Failure("MY_SPACE_CARD_NOT_FOUND", "My Space card was not found.");
        }

        if (request.HasSkillId)
        {
            var user = await _context.Users
                .Include(item => item.UserSkills)
                .ThenInclude(item => item.Skill)
                .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

            if (user is null)
            {
                return Result<LearnerSpaceCardDto>.Failure("PROFILE_NOT_FOUND", "Profile was not found.");
            }

            var skillResult = MySpaceRules.ResolveOwnedSkill(user, request.SkillId!.Value, UserSkillType.Learn);
            if (skillResult.IsFailure)
            {
                return Result<LearnerSpaceCardDto>.Failure(skillResult.ErrorCode!, skillResult.ErrorMessage!);
            }

            card.SkillId = skillResult.Value!.SkillId;
            card.Skill = skillResult.Value;
        }

        if (request.HasTitle)
        {
            card.Title = request.Title!.Trim();
        }

        if (request.HasDescription)
        {
            card.Description = MySpaceRules.NormalizeOptionalString(request.Description);
        }

        if (request.HasTargetPoints)
        {
            card.TargetPoints = request.TargetPoints!.Value;
        }

        if (request.HasDurationMinutes)
        {
            card.DurationMinutes = request.DurationMinutes!.Value;
        }

        if (request.HasDeliveryModes)
        {
            card.DeliveryModes = MySpaceRules.NormalizeDeliveryModes(request.DeliveryModes);
        }

        if (request.HasLanguages)
        {
            card.Languages = MySpaceRules.NormalizeLanguages(request.Languages);
        }

        if (request.HasCoverImageUrl)
        {
            card.CoverImageUrl = MySpaceRules.NormalizeOptionalString(request.CoverImageUrl);
        }

        if (request.HasIsPublished)
        {
            card.IsPublished = request.IsPublished!.Value;
        }

        card.UpdatedAt = _dateTimeProvider.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result<LearnerSpaceCardDto>.Success(MySpaceDtoMapper.MapLearnerCard(card));
    }
}
