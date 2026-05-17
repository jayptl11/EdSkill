namespace EdSkill.Application.Common.Services;

internal static class VietnamTimeWindowHelper
{
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    public static DateTime GetDayStartUtc(DateTime utcNow)
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, VietnamTimeZone);
        return TimeZoneInfo.ConvertTimeToUtc(localNow.Date, VietnamTimeZone);
    }

    public static DateTime GetWeekStartUtc(DateTime utcNow)
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, VietnamTimeZone);
        var dayStartLocal = localNow.Date;
        var offsetFromMonday = ((int)localNow.DayOfWeek + 6) % 7;
        var weekStartLocal = dayStartLocal.AddDays(-offsetFromMonday);
        return TimeZoneInfo.ConvertTimeToUtc(weekStartLocal, VietnamTimeZone);
    }

    public static (DateTime DayStartUtc, DateTime WeekStartUtc) GetEarnWindowStartUtc(DateTime utcNow)
    {
        return (GetDayStartUtc(utcNow), GetWeekStartUtc(utcNow));
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
