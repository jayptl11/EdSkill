using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Sessions.Commands.BookSession;

public class BookSessionCommandHandler : IRequestHandler<BookSessionCommand, Result<SessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPointLedgerService _pointLedgerService;
    private readonly ITransactionExecutor _transactionExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BookSessionCommandHandler(
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

    public async Task<Result<SessionDto>> Handle(BookSessionCommand request, CancellationToken cancellationToken)
    {
        var learnerId = _currentUserService.GetUserId();

        return await _transactionExecutor.ExecuteAsync<SessionDto>(async ct =>
        {
            var learner = await _context.Users.FirstOrDefaultAsync(item => item.UserId == learnerId, ct);
            if (learner == null)
            {
                return Result<SessionDto>.Failure("USER_NOT_FOUND", "User was not found.");
            }

            if (!learner.Roles.Contains("learner"))
            {
                return Result<SessionDto>.Failure("FORBIDDEN", "Only Learner users can book a session.");
            }

            var session = await _context.Sessions.FirstOrDefaultAsync(item => item.SessionId == request.SessionId, ct);
            if (session == null)
            {
                return Result<SessionDto>.Failure("SESSION_NOT_FOUND", "Session was not found.");
            }

            if (session.Status != SessionStatus.Available || session.LearnerId.HasValue)
            {
                return Result<SessionDto>.Failure("SESSION_NOT_AVAILABLE", "Session is not available for booking.");
            }

            if (session.CompanionId == learnerId)
            {
                return Result<SessionDto>.Failure("SELF_BOOKING", "Không thể tự đặt phiên với chính mình.");
            }

            var wallet = await _pointLedgerService.GetOrCreateWalletAsync(learnerId, ct);
            var holdResult = _pointLedgerService.HoldPoints(wallet, session.PointCost, session.SessionId, "Points held for session booking.");
            if (!holdResult.IsSuccess)
            {
                return Result<SessionDto>.Failure(holdResult.ErrorCode!, holdResult.ErrorMessage!);
            }

            session.LearnerId = learnerId;
            session.Status = SessionStatus.Pending;
            session.UpdatedAt = _dateTimeProvider.UtcNow;

            return Result<SessionDto>.Success(SessionDtoMapper.Map(session));
        }, cancellationToken);
    }
}
