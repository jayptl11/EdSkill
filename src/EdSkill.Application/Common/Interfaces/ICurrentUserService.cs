namespace EdSkill.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid GetUserId();
    Guid? TryGetUserId();
}
