using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Admin.DTOs;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Admin.Commands.GrantPoints;

public class GrantPointsCommandHandler : IRequestHandler<GrantPointsCommand, Result<GrantPointsResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPointLedgerService _pointLedgerService;
    private readonly ITransactionExecutor _transactionExecutor;

    public GrantPointsCommandHandler(
        IApplicationDbContext context,
        IPointLedgerService pointLedgerService,
        ITransactionExecutor transactionExecutor)
    {
        _context = context;
        _pointLedgerService = pointLedgerService;
        _transactionExecutor = transactionExecutor;
    }

    public async Task<Result<GrantPointsResultDto>> Handle(GrantPointsCommand request, CancellationToken cancellationToken)
    {
        var distinctUserIds = request.UserIds.Distinct().ToList();

        return await _transactionExecutor.ExecuteAsync<GrantPointsResultDto>(async ct =>
        {
            var users = await _context.Users
                .Where(item => distinctUserIds.Contains(item.UserId))
                .ToListAsync(ct);

            if (users.Count != distinctUserIds.Count)
            {
                return Result<GrantPointsResultDto>.Failure("USER_NOT_FOUND", "One or more users were not found.");
            }

            foreach (var userId in distinctUserIds)
            {
                var wallet = await _pointLedgerService.GetOrCreateWalletAsync(userId, ct);
                var result = _pointLedgerService.CreditUser(wallet, PointTransactionType.AdminGrant, request.Amount, null, request.Note);
                if (!result.IsSuccess)
                {
                    return Result<GrantPointsResultDto>.Failure(result.ErrorCode!, result.ErrorMessage!);
                }
            }

            return Result<GrantPointsResultDto>.Success(new GrantPointsResultDto(distinctUserIds.Count));
        }, cancellationToken);
    }
}
