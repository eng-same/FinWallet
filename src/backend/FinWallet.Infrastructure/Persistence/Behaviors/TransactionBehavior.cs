using FinWallet.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinWallet.Infrastructure.Persistence.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(ApplicationDbContext dbContext, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        // Only wrap Commands in a transaction
        if (!requestName.EndsWith("Command"))
        {
            return await next();
        }

        _logger.LogInformation("Beginning transaction for request {RequestName}", requestName);

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<object?, TResponse>(
            state: null,
            operation: async (context, state, ct) =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(ct);
                try
                {
                    var response = await next();
                    await context.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    _logger.LogInformation("Committed transaction for request {RequestName}", requestName);
                    return response;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(ct);
                    _logger.LogError(ex, "Rolled back transaction for request {RequestName} due to error", requestName);
                    throw;
                }
            },
            verifySucceeded: null,
            cancellationToken: cancellationToken);
    }
}
