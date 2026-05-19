using EdSkill.Domain.Entities;

namespace EdSkill.Application.Common.System;

public sealed record SystemConfigDefinition(string Key, string DefaultValue, string Description);

public static class SystemConfigCatalog
{
    public static IReadOnlyCollection<SystemConfigDefinition> Definitions { get; } =
    [
        new(SystemConfigKeys.PointSignupBonus, "50", "Diem khoi dau khi dang ky."),
        new(SystemConfigKeys.PointPlatformFeePct, "20", "% phi nen tang tren moi giao dich completed legacy."),
        new(SystemConfigKeys.PointPlatformMarkupPct, "25", "% markup cong len gia Companion cho Formula Pricing."),
        new(SystemConfigKeys.TokenLearnerPerSession, "5", "Token Learner nhan sau moi phien hop le legacy."),
        new(SystemConfigKeys.TokenCompanionPerSession, "3", "Token Companion nhan sau moi phien hop le legacy."),
        new(SystemConfigKeys.TokenDailyEarnLimit, "20", "Token toi da nhan trong mot ngay."),
        new(SystemConfigKeys.TokenWeeklyEarnLimit, "100", "Token toi da nhan trong mot tuan."),
        new(SystemConfigKeys.SessionMinDurationMinutes, "10", "Thoi luong toi thieu de phien hop le."),
        new(SystemConfigKeys.SessionCancelDeadlineHours, "2", "So gio truoc phien duoc huy khong mat diem."),
        new(SystemConfigKeys.SessionLateCancelCompanionPct, "80", "% diem Companion nhan khi Learner huy muon."),
        new(SystemConfigKeys.SessionLateCancelPlatformPct, "20", "% diem nen tang nhan khi Learner huy muon."),
        new(SystemConfigKeys.SessionMaxPerDayPerCompanion, "8", "So phien toi da mot Companion mo trong ngay."),
        new(SystemConfigKeys.SessionBufferMinutes, "10", "Thoi gian nghi toi thieu giua hai phien."),
        new(SystemConfigKeys.SessionJoinEarlyMinutes, "10", "So phut duoc vao phong truoc gio hoc."),
        new(SystemConfigKeys.SessionJoinLateGraceMinutes, "30", "So phut cho phep vao phong sau gio ket thuc du kien.")
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
            SystemConfigKeys.PointPlatformMarkupPct => number is >= 0 and <= 100,
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
            SystemConfigKeys.SessionJoinEarlyMinutes => number >= 0,
            SystemConfigKeys.SessionJoinLateGraceMinutes => number >= 0,
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
