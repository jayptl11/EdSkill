using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.System;
using EdSkill.Application.Features.Admin.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Admin.Commands.UpdateSystemConfig;

public class UpdateSystemConfigCommandHandler : IRequestHandler<UpdateSystemConfigCommand, Result<SystemConfigDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateSystemConfigCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<SystemConfigDto>> Handle(UpdateSystemConfigCommand request, CancellationToken cancellationToken)
    {
        if (!SystemConfigCatalog.TryValidate(request.Key, request.Value, out var errorCode, out var errorMessage))
        {
            return Result<SystemConfigDto>.Failure(errorCode, errorMessage);
        }

        var config = await _context.SystemConfigs
            .FirstOrDefaultAsync(item => item.Key == request.Key, cancellationToken);

        if (config == null)
        {
            return Result<SystemConfigDto>.Failure("SYSTEM_CONFIG_NOT_FOUND", "System config key was not found.");
        }

        if (request.Key == SystemConfigKeys.SessionLateCancelCompanionPct
            || request.Key == SystemConfigKeys.SessionLateCancelPlatformPct)
        {
            var otherKey = request.Key == SystemConfigKeys.SessionLateCancelCompanionPct
                ? SystemConfigKeys.SessionLateCancelPlatformPct
                : SystemConfigKeys.SessionLateCancelCompanionPct;

            var otherConfig = await _context.SystemConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Key == otherKey, cancellationToken);

            var currentValue = int.Parse(request.Value);
            var otherValue = int.Parse(otherConfig?.Value ?? "0");
            if (currentValue + otherValue != 100)
            {
                return Result<SystemConfigDto>.Failure(
                    "SYSTEM_CONFIG_INVALID_VALUE",
                    "Late cancel companion and platform percentages must total 100.");
            }
        }

        config.Value = request.Value;
        config.UpdatedAt = _dateTimeProvider.UtcNow;
        config.UpdatedBy = _currentUserService.GetUserId();

        await _context.SaveChangesAsync(cancellationToken);

        return Result<SystemConfigDto>.Success(SystemConfigDtoMapper.Map(config));
    }
}
