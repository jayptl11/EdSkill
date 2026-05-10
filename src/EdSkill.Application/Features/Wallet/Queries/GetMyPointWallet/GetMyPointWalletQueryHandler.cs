using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet;
using EdSkill.Application.Features.Wallet.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Wallet.Queries.GetMyPointWallet;

public class GetMyPointWalletQueryHandler : IRequestHandler<GetMyPointWalletQuery, Result<PointWalletSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyPointWalletQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PointWalletSummaryDto>> Handle(GetMyPointWalletQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var wallet = await _context.PointWallets
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (wallet == null)
        {
            return Result<PointWalletSummaryDto>.Failure("POINT_WALLET_NOT_FOUND", "Point wallet was not found.");
        }

        return Result<PointWalletSummaryDto>.Success(WalletDtoMapper.MapSummary(wallet));
    }
}
