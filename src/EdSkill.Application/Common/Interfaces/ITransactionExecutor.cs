using EdSkill.Application.Common.Models;

namespace EdSkill.Application.Common.Interfaces;

public interface ITransactionExecutor
{
    Task<Result> ExecuteAsync(Func<CancellationToken, Task<Result>> operation, CancellationToken cancellationToken = default);
    Task<Result<T>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken = default);
}
