namespace EdSkill.Application.Common.Interfaces;

public interface ISystemConfigService
{
    Task<int> GetIntValueAsync(string key, CancellationToken cancellationToken);
}
