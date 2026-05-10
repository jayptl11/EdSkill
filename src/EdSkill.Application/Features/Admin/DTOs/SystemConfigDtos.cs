namespace EdSkill.Application.Features.Admin.DTOs;

public record SystemConfigDto(
    string Key,
    string Value,
    string Description,
    DateTime UpdatedAt,
    Guid? UpdatedBy
);

public record UpdateSystemConfigRequest(string Value);
