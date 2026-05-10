namespace EdSkill.Application.Features.Admin.DTOs;

public record GrantPointsRequest(
    IReadOnlyCollection<Guid> UserIds,
    int Amount,
    string Note
);

public record GrantPointsResultDto(int Granted);
