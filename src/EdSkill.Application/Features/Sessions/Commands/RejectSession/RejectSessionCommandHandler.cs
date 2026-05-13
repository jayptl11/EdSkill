using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Sessions.Commands.RejectSession;

public class RejectSessionCommandHandler : IRequestHandler<RejectSessionCommand, Result<SessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPointLedgerService _pointLedgerService;
    private readonly ITransactionExecutor _transactionExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RejectSessionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPointLedgerService pointLedgerService,
        ITransactionExecutor transactionExecutor,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _pointLedgerService = pointLedgerService;
        _transactionExecutor = transactionExecutor;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<SessionDto>> Handle(RejectSessionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();

        return await _transactionExecutor.ExecuteAsync<SessionDto>(async ct =>
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(item => item.SessionId == request.SessionId, ct);
            if (session == null)
            {
                return Result<SessionDto>.Failure("SESSION_NOT_FOUND", "Session was not found.");
            }

            if (session.CompanionId != userId)
            {
                return Result<SessionDto>.Failure("FORBIDDEN", "Only the Companion can reject this session.");
            }

            if (session.Status != SessionStatus.Pending || !session.LearnerId.HasValue)
            {
                return Result<SessionDto>.Failure("SESSION_INVALID_STATUS", "Hanh dong khong hop le voi trang thai hien tai.");
            }

            var learnerChargePoints = session.PricingModel == SessionPricingModel.FormulaV1
                ? session.LearnerChargePoints ?? 0
                : session.PointCost;
            if (learnerChargePoints <= 0)
            {
                return Result<SessionDto>.Failure("SESSION_INVALID_STATUS", "Session pricing is invalid.");
            }

            var learnerWallet = await _pointLedgerService.GetOrCreateWalletAsync(session.LearnerId.Value, ct);
            var refundResult = _pointLedgerService.ReleaseHeldPoints(
                learnerWallet,
                learnerChargePoints,
                session.SessionId,
                PointTransactionType.Refund,
                "Points refunded after session rejection.");

            if (!refundResult.IsSuccess)
            {
                return Result<SessionDto>.Failure(refundResult.ErrorCode!, refundResult.ErrorMessage!);
            }

            session.Status = SessionStatus.Cancelled;
            session.CancelReason = request.Reason;
            session.CancelledBy = userId;
            session.CancelledAt = _dateTimeProvider.UtcNow;
            session.UpdatedAt = _dateTimeProvider.UtcNow;

            return Result<SessionDto>.Success(SessionDtoMapper.Map(session));
        }, cancellationToken);
    }
}
