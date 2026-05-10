using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Sessions;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Sessions.Commands.ConfirmSession;

public class ConfirmSessionCommandHandler : IRequestHandler<ConfirmSessionCommand, Result<SessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITransactionExecutor _transactionExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ConfirmSessionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ITransactionExecutor transactionExecutor,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _transactionExecutor = transactionExecutor;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<SessionDto>> Handle(ConfirmSessionCommand request, CancellationToken cancellationToken)
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
                return Result<SessionDto>.Failure("FORBIDDEN", "Only the Companion can confirm this session.");
            }

            if (session.Status != SessionStatus.Pending)
            {
                return Result<SessionDto>.Failure("SESSION_INVALID_STATUS", "Hành động không hợp lệ với trạng thái hiện tại.");
            }

            session.Status = SessionStatus.Confirmed;
            session.JitsiRoomId = session.DeliveryMode == SessionDeliveryMode.Online
                ? $"edskill-{session.SessionId:N}"
                : null;
            session.UpdatedAt = _dateTimeProvider.UtcNow;

            return Result<SessionDto>.Success(SessionDtoMapper.Map(session));
        }, cancellationToken);
    }
}
