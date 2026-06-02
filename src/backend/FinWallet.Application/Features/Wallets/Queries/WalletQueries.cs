using FinWallet.Application.Common.DTOs;
using FinWallet.Application.Common.Exceptions;
using FinWallet.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinWallet.Application.Features.Wallets.Queries;

public sealed record GetMyWalletQuery(Guid UserId) : IRequest<WalletDto>;

public record LookupWalletResult(string WalletNumber, string FullName);
public sealed record LookupWalletQuery(string WalletNumber) : IRequest<LookupWalletResult>;

public sealed class WalletQueriesHandler : 
    IRequestHandler<GetMyWalletQuery, WalletDto>,
    IRequestHandler<LookupWalletQuery, LookupWalletResult>
{
    private readonly IApplicationDbContext _dbContext;

    public WalletQueriesHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WalletDto> Handle(GetMyWalletQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _dbContext.Wallets
            .FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);

        if (wallet == null)
        {
            throw new FinWalletException("WALLET_NOT_FOUND", "Wallet not found for this user.");
        }

        return new WalletDto(
            wallet.Id,
            wallet.UserId,
            wallet.WalletNumber,
            wallet.Balance,
            wallet.Currency,
            wallet.Status.ToString(),
            wallet.CreatedAt,
            wallet.UpdatedAt,
            wallet.FrozenAt,
            wallet.FrozenReason
        );
    }

    public async Task<LookupWalletResult> Handle(LookupWalletQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _dbContext.Wallets
            .Include(w => w.User)
            .FirstOrDefaultAsync(w => w.WalletNumber == request.WalletNumber, cancellationToken);

        if (wallet == null)
        {
            throw new FinWalletException("WALLET_NOT_FOUND", $"Wallet with number {request.WalletNumber} was not found.");
        }

        if (wallet.Status == Domain.Enums.WalletStatus.Closed)
        {
            throw new FinWalletException("WALLET_CLOSED", "This wallet is closed and cannot receive money.");
        }

        return new LookupWalletResult(
            wallet.WalletNumber,
            wallet.User?.FullName ?? "Unknown User"
        );
    }
}
