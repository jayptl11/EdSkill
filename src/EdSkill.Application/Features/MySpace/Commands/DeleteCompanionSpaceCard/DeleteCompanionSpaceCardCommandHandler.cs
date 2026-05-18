using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.MySpace.Commands.DeleteCompanionSpaceCard;

public class DeleteCompanionSpaceCardCommandHandler : IRequestHandler<DeleteCompanionSpaceCardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteCompanionSpaceCardCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(DeleteCompanionSpaceCardCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var card = await _context.CompanionSpaceCards
            .FirstOrDefaultAsync(item => item.CompanionSpaceCardId == request.CompanionSpaceCardId && item.UserId == userId, cancellationToken);

        if (card is null)
        {
            return Result.Failure("MY_SPACE_CARD_NOT_FOUND", "My Space card was not found.");
        }

        _context.CompanionSpaceCards.Remove(card);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
