using FinWallet.Application.Common.DTOs;
using FinWallet.Application.Common.Exceptions;
using FinWallet.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinWallet.Application.Features.Receipts.Queries;

public sealed record GetReceiptByTransactionQuery(Guid TransactionId, Guid UserId) : IRequest<ReceiptDto>;

public sealed class GetReceiptByTransactionQueryHandler : IRequestHandler<GetReceiptByTransactionQuery, ReceiptDto>
{
    private readonly IApplicationDbContext _dbContext;

    public GetReceiptByTransactionQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReceiptDto> Handle(GetReceiptByTransactionQuery request, CancellationToken cancellationToken)
    {
        var receipt = await _dbContext.Receipts
            .Include(r => r.Transaction).ThenInclude(t => t!.FromWallet)
            .Include(r => r.Transaction).ThenInclude(t => t!.ToWallet)
            .Include(r => r.Transaction).ThenInclude(t => t!.InitiatedByUser)
            .FirstOrDefaultAsync(r => r.TransactionId == request.TransactionId, cancellationToken);

        if (receipt == null)
        {
            throw new FinWalletException("RECEIPT_NOT_FOUND", "Receipt not found for this transaction.");
        }

        var transaction = receipt.Transaction;
        if (transaction == null)
        {
            throw new FinWalletException("TRANSACTION_NOT_FOUND", "Associated transaction not found.");
        }

        // Auth check: Must be sender, receiver or initiator of the transaction, or Admin
        var myWallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);
        var isAuthorized = transaction.InitiatedByUserId == request.UserId || 
                            (myWallet != null && (transaction.FromWalletId == myWallet.Id || transaction.ToWalletId == myWallet.Id));

        if (!isAuthorized)
        {
            var isAdmin = await _dbContext.UserRoles
                .AnyAsync(ur => ur.UserId == request.UserId && 
                                _dbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin"), cancellationToken);
            if (!isAdmin)
            {
                throw new FinWalletException("FORBIDDEN", "You are not authorized to view this receipt.");
            }
        }

        return new ReceiptDto(
            receipt.Id,
            receipt.TransactionId,
            receipt.ReceiptNumber,
            receipt.IssuedAt,
            transaction.Amount,
            transaction.Currency,
            transaction.FromWallet?.WalletNumber,
            transaction.ToWallet?.WalletNumber,
            transaction.InitiatedByUser?.FullName ?? "System",
            transaction.CompletedAt ?? transaction.CreatedAt,
            transaction.Description
        );
    }
}
