namespace EdSkill.Application.Common.Models;

public sealed record SessionPricingPreview(
    int MinCompanionPayoutPoints,
    int MaxCompanionPayoutPoints,
    int MinLearnerChargePoints,
    int MaxLearnerChargePoints,
    int MinPlatformFeePoints,
    int MaxPlatformFeePoints);

public sealed record SessionDurationPricingOption(
    int DurationMinutes,
    int LearnerChargePoints,
    int CompanionPayoutPoints,
    int PlatformFeePoints,
    int DurationMultiplierPercent);

public sealed record FormulaSessionPricingSnapshot(
    int SelectedDurationMinutes,
    int CompanionPayoutPoints,
    int LearnerChargePoints,
    int PlatformFeePoints,
    int SkillBasePointsSnapshot,
    int CredentialBonusPointsSnapshot,
    int DurationMultiplierPercentSnapshot);
