using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet;
using EdSkill.Application.Features.Wallet.DTOs;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Wallet.Queries.GetMyPointTransactions;

public class GetMyPointTransactionsQueryHandler : IRequestHandler<GetMyPointTransactionsQuery, Result<PointTransactionHistoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyPointTransactionsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PointTransactionHistoryDto>> Handle(GetMyPointTransactionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var query = _context.PointTransactions
            .AsNoTracking()
            .Where(item => item.UserId == userId);

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            if (!Enum.TryParse<PointTransactionType>(request.Type, true, out var transactionType))
            {
                return Result<PointTransactionHistoryDto>.Failure("POINT_TRANSACTION_TYPE_INVALID", "Point transaction type is invalid.");
            }

            query = query.Where(item => item.Type == transactionType);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        return Result<PointTransactionHistoryDto>.Success(new PointTransactionHistoryDto(
            items.Select(WalletDtoMapper.MapTransaction).ToList(),
            total,
            request.Page,
            request.Limit));
    }
}
