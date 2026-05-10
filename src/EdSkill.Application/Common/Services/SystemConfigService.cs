using EdSkill.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Common.Services;

public class SystemConfigService : ISystemConfigService
{
    private readonly IApplicationDbContext _context;

    public SystemConfigService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetIntValueAsync(string key, CancellationToken cancellationToken)
    {
        var config = await _context.SystemConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Key == key, cancellationToken);

        if (config == null || !int.TryParse(config.Value, out var value))
        {
            throw new InvalidOperationException($"System config '{key}' is missing or invalid.");
        }

        return value;
    }
}
