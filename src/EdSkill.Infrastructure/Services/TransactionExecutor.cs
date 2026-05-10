using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace EdSkill.Infrastructure.Services;

public class TransactionExecutor : ITransactionExecutor
{
    private readonly AppDbContext _dbContext;

    public TransactionExecutor(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> ExecuteAsync(Func<CancellationToken, Task<Result>> operation, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var result = await operation(cancellationToken);

        if (!result.IsSuccess)
        {
            await transaction.RollbackAsync(cancellationToken);
            return result;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<Result<T>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var result = await operation(cancellationToken);

        if (!result.IsSuccess)
        {
            await transaction.RollbackAsync(cancellationToken);
            return result;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
