using EdSkill.Application.Common.Models;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Common.Interfaces;

public interface ITokenLedgerService
{
    Task<Result> AwardSessionCompletionTokensAsync(Session session, CancellationToken cancellationToken);
}
