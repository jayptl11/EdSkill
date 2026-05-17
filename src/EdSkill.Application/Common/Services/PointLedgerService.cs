using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.System;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Common.Services;

public class PointLedgerService : IPointLedgerService
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PointLedgerService(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PointWallet> GetOrCreateWalletAsync(Guid userId, CancellationToken cancellationToken)
    {
        var wallet = await _context.PointWallets
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (wallet != null)
        {
            return wallet;
        }

        wallet = new PointWallet
        {
            PointWalletId = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = _dateTimeProvider.UtcNow,
            UpdatedAt = _dateTimeProvider.UtcNow
        };

        await _context.PointWallets.AddAsync(wallet, cancellationToken);
        return wallet;
    }

    public async Task<SystemLedgerAccount> GetPlatformLedgerAsync(CancellationToken cancellationToken)
    {
        var ledger = await _context.SystemLedgerAccounts
            .FirstOrDefaultAsync(item => item.Code == SystemLedgerAccountCodes.PlatformFee, cancellationToken);

        if (ledger == null)
        {
            throw new InvalidOperationException("Platform fee ledger account is missing.");
        }

        return ledger;
    }

    public async Task<Result> ApplySignupBonusAsync(Guid userId, int amount, string? note, CancellationToken cancellationToken)
    {
        var wallet = await GetOrCreateWalletAsync(userId, cancellationToken);
        return CreditUser(wallet, PointTransactionType.SignupBonus, amount, null, note);
    }

    public Result HoldPoints(PointWallet wallet, int amount, Guid sessionId, string? note = null)
    {
        if (amount <= 0)
        {
            return Result.Failure("INVALID_POINT_AMOUNT", "Point amount must be greater than zero.");
        }

        if (wallet.Balance < amount)
        {
            return Result.Failure("INSUFFICIENT_POINTS", "Số điểm không đủ.");
        }

        var balanceBefore = wallet.Balance;
        var heldBefore = wallet.HeldBalance;

        wallet.Balance -= amount;
        wallet.HeldBalance += amount;
        wallet.UpdatedAt = _dateTimeProvider.UtcNow;

        _context.PointTransactions.Add(CreateTransaction(
            wallet.UserId,
            null,
            PointTransactionType.Held,
            0,
            balanceBefore,
            wallet.Balance,
            heldBefore,
            wallet.HeldBalance,
            sessionId,
            note));

        return Result.Success();
    }

    public Result ReleaseHeldPoints(PointWallet wallet, int amount, Guid sessionId, PointTransactionType type, string? note = null)
    {
        if (amount <= 0)
        {
            return Result.Failure("INVALID_POINT_AMOUNT", "Point amount must be greater than zero.");
        }

        if (wallet.HeldBalance < amount)
        {
            return Result.Failure("INSUFFICIENT_HELD_POINTS", "Held points are not enough for this operation.");
        }

        var balanceBefore = wallet.Balance;
        var heldBefore = wallet.HeldBalance;

        wallet.HeldBalance -= amount;
        wallet.Balance += amount;
        wallet.UpdatedAt = _dateTimeProvider.UtcNow;

        _context.PointTransactions.Add(CreateTransaction(
            wallet.UserId,
            null,
            type,
            amount,
            balanceBefore,
            wallet.Balance,
            heldBefore,
            wallet.HeldBalance,
            sessionId,
            note));

        return Result.Success();
    }

    public Result CompleteSessionPayment(PointWallet wallet, int amount, Guid sessionId, string? note = null)
    {
        if (amount <= 0)
        {
            return Result.Failure("INVALID_POINT_AMOUNT", "Point amount must be greater than zero.");
        }

        if (wallet.HeldBalance < amount)
        {
            return Result.Failure("INSUFFICIENT_HELD_POINTS", "Held points are not enough for this operation.");
        }

        var balanceBefore = wallet.Balance;
        var heldBefore = wallet.HeldBalance;

        wallet.HeldBalance -= amount;
        wallet.TotalSpent += amount;
        wallet.UpdatedAt = _dateTimeProvider.UtcNow;

        _context.PointTransactions.Add(CreateTransaction(
            wallet.UserId,
            null,
            PointTransactionType.SessionPayment,
            -amount,
            balanceBefore,
            wallet.Balance,
            heldBefore,
            wallet.HeldBalance,
            sessionId,
            note));

        return Result.Success();
    }

    public Result CreditUser(PointWallet wallet, PointTransactionType type, int amount, Guid? sessionId, string? note = null)
    {
        if (amount < 0)
        {
            return Result.Failure("INVALID_POINT_AMOUNT", "Point amount must not be negative.");
        }

        var balanceBefore = wallet.Balance;
        var heldBefore = wallet.HeldBalance;

        wallet.Balance += amount;
        if (type is PointTransactionType.SignupBonus
            or PointTransactionType.AdminGrant
            or PointTransactionType.SessionEarning
            or PointTransactionType.Purchase)
        {
            wallet.TotalEarned += amount;
        }

        wallet.UpdatedAt = _dateTimeProvider.UtcNow;

        _context.PointTransactions.Add(CreateTransaction(
            wallet.UserId,
            null,
            type,
            amount,
            balanceBefore,
            wallet.Balance,
            heldBefore,
            wallet.HeldBalance,
            sessionId,
            note));

        return Result.Success();
    }

    public Result CreditPlatform(SystemLedgerAccount ledgerAccount, int amount, Guid? sessionId, string? note = null)
    {
        if (amount < 0)
        {
            return Result.Failure("INVALID_POINT_AMOUNT", "Point amount must not be negative.");
        }

        var balanceBefore = ledgerAccount.Balance;
        ledgerAccount.Balance += amount;
        ledgerAccount.UpdatedAt = _dateTimeProvider.UtcNow;

        _context.PointTransactions.Add(CreateTransaction(
            null,
            ledgerAccount.SystemLedgerAccountId,
            PointTransactionType.PlatformFee,
            amount,
            balanceBefore,
            ledgerAccount.Balance,
            0,
            0,
            sessionId,
            note));

        return Result.Success();
    }

    private PointTransaction CreateTransaction(
        Guid? userId,
        Guid? systemLedgerAccountId,
        PointTransactionType type,
        int amount,
        int balanceBefore,
        int balanceAfter,
        int heldBalanceBefore,
        int heldBalanceAfter,
        Guid? sessionId,
        string? note)
    {
        return new PointTransaction
        {
            PointTransactionId = Guid.NewGuid(),
            UserId = userId,
            SystemLedgerAccountId = systemLedgerAccountId,
            Type = type,
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            HeldBalanceBefore = heldBalanceBefore,
            HeldBalanceAfter = heldBalanceAfter,
            SessionId = sessionId,
            Note = note,
            CreatedAt = _dateTimeProvider.UtcNow
        };
    }
}
