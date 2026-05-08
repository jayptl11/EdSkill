using EdSkill.Application.Common.Interfaces;

namespace EdSkill.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
