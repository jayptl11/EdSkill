using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.System;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Common.Services;

public sealed class TokenLedgerService : ITokenLedgerService
{
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    private readonly IApplicationDbContext _context;
    private readonly ISystemConfigService _systemConfigService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TokenLedgerService(
        IApplicationDbContext context,
        ISystemConfigService systemConfigService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _systemConfigService = systemConfigService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> AwardSessionCompletionTokensAsync(Session session, CancellationToken cancellationToken)
    {
        if (!session.LearnerId.HasValue)
        {
            return Result.Failure("SESSION_INVALID_STATUS", "Session does not have a learner.");
        }

        var learner = await _context.Users.FirstOrDefaultAsync(user => user.UserId == session.LearnerId.Value, cancellationToken);
        var companion = await _context.Users.FirstOrDefaultAsync(user => user.UserId == session.CompanionId, cancellationToken);
        if (learner == null || companion == null)
        {
            return Result.Failure("USER_NOT_FOUND", "Session participants were not found.");
        }

        var (learnerReward, companionReward) = await ResolveRewardsAsync(session, cancellationToken);

        var learnerResult = await CreditUserAsync(
            learner,
            TokenTransactionType.SessionCompletionLearnerReward,
            learnerReward,
            session.SessionId,
            "Learner token reward after valid completed session.",
            cancellationToken);
        if (!learnerResult.IsSuccess)
        {
            return learnerResult;
        }

        var companionResult = await CreditUserAsync(
            companion,
            TokenTransactionType.SessionCompletionCompanionReward,
            companionReward,
            session.SessionId,
            "Companion token reward after valid completed session.",
            cancellationToken);
        if (!companionResult.IsSuccess)
        {
            return companionResult;
        }

        return Result.Success();
    }

    private async Task<(decimal LearnerReward, decimal CompanionReward)> ResolveRewardsAsync(Session session, CancellationToken cancellationToken)
    {
        if (session.PricingModel == SessionPricingModel.FormulaV1)
        {
            var learnerChargePoints = session.LearnerChargePoints ?? 0;
            return (
                decimal.Round(learnerChargePoints * 0.05m, 2, MidpointRounding.AwayFromZero),
                decimal.Round(learnerChargePoints * 0.03m, 2, MidpointRounding.AwayFromZero));
        }

        var learnerReward = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.TokenLearnerPerSession, cancellationToken);
        var companionReward = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.TokenCompanionPerSession, cancellationToken);
        return (learnerReward, companionReward);
    }

    private async Task<Result> CreditUserAsync(
        User user,
        TokenTransactionType type,
        decimal requestedAmount,
        Guid sessionId,
        string note,
        CancellationToken cancellationToken)
    {
        if (requestedAmount <= 0)
        {
            return Result.Success();
        }

        var dailyLimit = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.TokenDailyEarnLimit, cancellationToken);
        var weeklyLimit = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.TokenWeeklyEarnLimit, cancellationToken);
        var (dayStartUtc, weekStartUtc) = GetEarnWindowStartUtc(_dateTimeProvider.UtcNow);

        var dailyEarned = await _context.TokenTransactions
            .Where(item => item.UserId == user.UserId && item.Amount > 0 && item.CreatedAt >= dayStartUtc)
            .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0m;
        var weeklyEarned = await _context.TokenTransactions
            .Where(item => item.UserId == user.UserId && item.Amount > 0 && item.CreatedAt >= weekStartUtc)
            .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0m;

        var remaining = Math.Min(dailyLimit - dailyEarned, weeklyLimit - weeklyEarned);
        if (remaining <= 0)
        {
            return Result.Success();
        }

        var awardedAmount = decimal.Round(Math.Min(requestedAmount, remaining), 2, MidpointRounding.AwayFromZero);
        if (awardedAmount <= 0)
        {
            return Result.Success();
        }

        var balanceBefore = user.TokenBalance;
        user.TokenBalance += awardedAmount;

        _context.TokenTransactions.Add(new TokenTransaction
        {
            TokenTransactionId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            Type = type,
            Amount = awardedAmount,
            BalanceBefore = balanceBefore,
            BalanceAfter = user.TokenBalance,
            SessionId = sessionId,
            Note = note,
            CreatedAt = _dateTimeProvider.UtcNow
        });

        return Result.Success();
    }

    private static (DateTime DayStartUtc, DateTime WeekStartUtc) GetEarnWindowStartUtc(DateTime utcNow)
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, VietnamTimeZone);
        var dayStartLocal = localNow.Date;
        var offsetFromMonday = ((int)localNow.DayOfWeek + 6) % 7;
        var weekStartLocal = dayStartLocal.AddDays(-offsetFromMonday);

        return (
            TimeZoneInfo.ConvertTimeToUtc(dayStartLocal, VietnamTimeZone),
            TimeZoneInfo.ConvertTimeToUtc(weekStartLocal, VietnamTimeZone));
    }

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        foreach (var id in new[] { "SE Asia Standard Time", "Asia/Bangkok" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
