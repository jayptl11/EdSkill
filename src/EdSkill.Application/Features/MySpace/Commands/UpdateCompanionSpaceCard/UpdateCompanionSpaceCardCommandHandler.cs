using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.MySpace.DTOs;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.MySpace.Commands.UpdateCompanionSpaceCard;

public class UpdateCompanionSpaceCardCommandHandler : IRequestHandler<UpdateCompanionSpaceCardCommand, Result<CompanionSpaceCardDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateCompanionSpaceCardCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CompanionSpaceCardDto>> Handle(UpdateCompanionSpaceCardCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var card = await _context.CompanionSpaceCards
            .Include(item => item.Skill)
            .FirstOrDefaultAsync(item => item.CompanionSpaceCardId == request.CompanionSpaceCardId && item.UserId == userId, cancellationToken);

        if (card is null)
        {
            return Result<CompanionSpaceCardDto>.Failure("MY_SPACE_CARD_NOT_FOUND", "My Space card was not found.");
        }

        if (request.HasSkillId)
        {
            var user = await _context.Users
                .Include(item => item.UserSkills)
                .ThenInclude(item => item.Skill)
                .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

            if (user is null || !user.Roles.Contains("companion"))
            {
                return Result<CompanionSpaceCardDto>.Failure("FORBIDDEN", "Only companions can update companion cards.");
            }

            var skillResult = MySpaceRules.ResolveOwnedSkill(user, request.SkillId!.Value, UserSkillType.Teach);
            if (skillResult.IsFailure)
            {
                return Result<CompanionSpaceCardDto>.Failure(skillResult.ErrorCode!, skillResult.ErrorMessage!);
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

        if (request.HasPricePoints)
        {
            card.PricePoints = request.PricePoints!.Value;
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

        if (request.HasCredentialUrls)
        {
            card.CredentialUrls = MySpaceRules.NormalizeCredentialUrls(request.CredentialUrls);
        }

        if (request.HasIsPublished)
        {
            card.IsPublished = request.IsPublished!.Value;
        }

        card.UpdatedAt = _dateTimeProvider.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result<CompanionSpaceCardDto>.Success(MySpaceDtoMapper.MapCompanionCard(card));
    }
}
