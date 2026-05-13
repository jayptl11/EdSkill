using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.Services;
using EdSkill.Application.Features.Profile;
using EdSkill.Application.Features.Sessions.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;

namespace EdSkill.Application.Features.Sessions;

public static class SessionDtoMapper
{
    public static SessionDto Map(
        Session session,
        Skill? skill = null,
        UserProfile? companionProfile = null,
        int? platformMarkupPct = null)
    {
        var pricingPreview = BuildPricingPreview(session, skill, companionProfile, platformMarkupPct);

        return new SessionDto(
            session.SessionId,
            session.CompanionId,
            session.LearnerId,
            session.Skill,
            session.Description,
            session.DeliveryMode,
            session.Location,
            session.SelectedDurationMinutes ?? session.DurationMinutes,
            session.LearnerChargePoints ?? pricingPreview.MinLearnerChargePoints,
            session.PricingModel,
            session.DurationOptions.AsReadOnly(),
            session.SelectedDurationMinutes,
            new SessionPricingPreviewDto(
                pricingPreview.MinCompanionPayoutPoints,
                pricingPreview.MaxCompanionPayoutPoints,
                pricingPreview.MinLearnerChargePoints,
                pricingPreview.MaxLearnerChargePoints,
                pricingPreview.MinPlatformFeePoints,
                pricingPreview.MaxPlatformFeePoints),
            BuildPricingBreakdown(session),
            session.ScheduledAt,
            session.Status,
            session.JitsiRoomId,
            session.ActualStartAt,
            session.ActualEndAt,
            session.ActualDuration,
            session.LearnerConfirmed,
            session.CompanionConfirmed,
            session.CancelReason,
            session.CancelledAt,
            session.DisbursedAt,
            session.CreatedAt,
            session.UpdatedAt);
    }

    public static SessionPricingPreview BuildPricingPreview(
        Session session,
        Skill? skill = null,
        UserProfile? companionProfile = null,
        int? platformMarkupPct = null)
    {
        if (session.PricingModel == SessionPricingModel.FormulaV1)
        {
            if (session.LearnerChargePoints.HasValue
                && session.CompanionPayoutPoints.HasValue
                && session.PlatformFeePoints.HasValue)
            {
                return new SessionPricingPreview(
                    session.CompanionPayoutPoints.Value,
                    session.CompanionPayoutPoints.Value,
                    session.LearnerChargePoints.Value,
                    session.LearnerChargePoints.Value,
                    session.PlatformFeePoints.Value,
                    session.PlatformFeePoints.Value);
            }

            if (skill != null && companionProfile != null && platformMarkupPct.HasValue)
            {
                var previewResult = SessionPricingService.TryBuildOfferPreview(
                    skill,
                    CompanionCredentialRules.GetCredentialCount(companionProfile),
                    session.DurationOptions,
                    platformMarkupPct.Value);
                if (previewResult.IsSuccess && previewResult.Value != null)
                {
                    return previewResult.Value;
                }
            }

            return new SessionPricingPreview(0, 0, 0, 0, 0, 0);
        }

        return SessionPricingService.BuildLegacyPreview(session.PointCost);
    }

    private static SessionPricingBreakdownDto? BuildPricingBreakdown(Session session)
    {
        if (!session.LearnerChargePoints.HasValue
            || !session.CompanionPayoutPoints.HasValue
            || !session.PlatformFeePoints.HasValue)
        {
            return null;
        }

        return new SessionPricingBreakdownDto(
            session.LearnerChargePoints.Value,
            session.CompanionPayoutPoints.Value,
            session.PlatformFeePoints.Value,
            session.SkillBasePointsSnapshot,
            session.CredentialBonusPointsSnapshot,
            session.DurationMultiplierPercentSnapshot);
    }
}
