using EdSkill.Application.Features.Admin.DTOs;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.Admin;

public static class SystemConfigDtoMapper
{
    public static SystemConfigDto Map(SystemConfig config)
    {
        return new SystemConfigDto(
            config.Key,
            config.Value,
            config.Description,
            config.UpdatedAt,
            config.UpdatedBy);
    }
}
