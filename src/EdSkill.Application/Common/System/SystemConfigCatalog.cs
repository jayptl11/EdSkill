using EdSkill.Domain.Entities;

namespace EdSkill.Application.Common.System;

public sealed record SystemConfigDefinition(string Key, string DefaultValue, string Description);

public static class SystemConfigCatalog
{
    public static IReadOnlyCollection<SystemConfigDefinition> Definitions { get; } =
    [
        new(SystemConfigKeys.PointSignupBonus, "50", "Điểm khởi đầu khi đăng ký."),
        new(SystemConfigKeys.PointPlatformFeePct, "20", "% phí nền tảng trên mỗi giao dịch completed."),
        new(SystemConfigKeys.TokenLearnerPerSession, "5", "Token Learner nhận sau mỗi phiên hợp lệ."),
        new(SystemConfigKeys.TokenCompanionPerSession, "3", "Token Companion nhận sau mỗi phiên hợp lệ."),
        new(SystemConfigKeys.TokenDailyEarnLimit, "20", "Token tối đa nhận trong một ngày."),
        new(SystemConfigKeys.TokenWeeklyEarnLimit, "100", "Token tối đa nhận trong một tuần."),
        new(SystemConfigKeys.SessionMinDurationMinutes, "10", "Thời lượng tối thiểu để phiên hợp lệ."),
        new(SystemConfigKeys.SessionCancelDeadlineHours, "2", "Số giờ trước phiên được hủy không mất điểm."),
        new(SystemConfigKeys.SessionLateCancelCompanionPct, "80", "% điểm Companion nhận khi Learner hủy muộn."),
        new(SystemConfigKeys.SessionLateCancelPlatformPct, "20", "% điểm nền tảng nhận khi Learner hủy muộn."),
        new(SystemConfigKeys.SessionMaxPerDayPerCompanion, "8", "Số phiên tối đa một Companion mở trong ngày."),
        new(SystemConfigKeys.SessionBufferMinutes, "10", "Thời gian nghỉ tối thiểu giữa hai phiên.")
    ];

    public static bool TryGet(string key, out SystemConfigDefinition definition)
    {
        definition = Definitions.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))!;
        return definition is not null;
    }

    public static bool TryValidate(string key, string value, out string errorCode, out string errorMessage)
    {
        errorCode = string.Empty;
        errorMessage = string.Empty;

        if (!TryGet(key, out _))
        {
            errorCode = "SYSTEM_CONFIG_NOT_FOUND";
            errorMessage = "System config key was not found.";
            return false;
        }

        if (!int.TryParse(value, out var number))
        {
            errorCode = "SYSTEM_CONFIG_INVALID_VALUE";
            errorMessage = "System config value must be a valid integer.";
            return false;
        }

        var valid = key switch
        {
            SystemConfigKeys.PointSignupBonus => number >= 0,
            SystemConfigKeys.PointPlatformFeePct => number is >= 0 and <= 100,
            SystemConfigKeys.TokenLearnerPerSession => number >= 0,
            SystemConfigKeys.TokenCompanionPerSession => number >= 0,
            SystemConfigKeys.TokenDailyEarnLimit => number >= 0,
            SystemConfigKeys.TokenWeeklyEarnLimit => number >= 0,
            SystemConfigKeys.SessionMinDurationMinutes => number > 0,
            SystemConfigKeys.SessionCancelDeadlineHours => number >= 0,
            SystemConfigKeys.SessionLateCancelCompanionPct => number is >= 0 and <= 100,
            SystemConfigKeys.SessionLateCancelPlatformPct => number is >= 0 and <= 100,
            SystemConfigKeys.SessionMaxPerDayPerCompanion => number > 0,
            SystemConfigKeys.SessionBufferMinutes => number >= 0,
            _ => false
        };

        if (!valid)
        {
            errorCode = "SYSTEM_CONFIG_INVALID_VALUE";
            errorMessage = "System config value is outside the supported range.";
            return false;
        }

        return true;
    }

    public static IEnumerable<SystemConfig> CreateSeed(Guid? updatedBy, DateTime updatedAt)
    {
        return Definitions.Select(definition => new SystemConfig
        {
            Key = definition.Key,
            Value = definition.DefaultValue,
            Description = definition.Description,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy
        });
    }
}
