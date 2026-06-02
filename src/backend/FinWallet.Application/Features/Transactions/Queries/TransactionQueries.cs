using FinWallet.Application.Common.DTOs;
using FinWallet.Application.Common.Exceptions;
using FinWallet.Application.Common.Interfaces;
using FinWallet.Domain.Entities;
using FinWallet.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinWallet.Application.Features.Transactions.Queries;

public record PagedList<T>(List<T> Items, int TotalCount, int Page, int PageSize, int TotalPages);

public sealed record GetMyTransactionsQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 10,
    string? Type = null,
    string? Status = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    string? Search = null
) : IRequest<PagedList<TransactionDto>>;

public sealed record GetMyRecentTransactionsQuery(Guid UserId, int Count = 5) : IRequest<List<TransactionDto>>;

public record TransactionDetailsDto(
    TransactionDto Transaction,
    List<LedgerEntryDto> LedgerEntries,
    ReceiptDto? Receipt
);
public sealed record GetTransactionDetailsQuery(Guid TransactionId, Guid UserId) : IRequest<TransactionDetailsDto>;

public sealed class TransactionQueriesHandler :
    IRequestHandler<GetMyTransactionsQuery, PagedList<TransactionDto>>,
    IRequestHandler<GetMyRecentTransactionsQuery, List<TransactionDto>>,
    IRequestHandler<GetTransactionDetailsQuery, TransactionDetailsDto>
{
    private readonly IApplicationDbContext _dbContext;

    public TransactionQueriesHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedList<TransactionDto>> Handle(GetMyTransactionsQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);
        if (wallet == null)
        {
            throw new FinWalletException("WALLET_NOT_FOUND", "Wallet not found.");
        }

        var query = _dbContext.Transactions
            .Include(t => t.FromWallet).ThenInclude(w => w!.User)
            .Include(t => t.ToWallet).ThenInclude(w => w!.User)
            .Include(t => t.InitiatedByUser)
            .Where(t => t.FromWalletId == wallet.Id || t.ToWalletId == wallet.Id);

        // Filters
        if (!string.IsNullOrEmpty(request.Type) && Enum.TryParse<TransactionType>(request.Type, true, out var txType))
        {
            query = query.Where(t => t.Type == txType);
        }

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<TransactionStatus>(request.Status, true, out var txStatus))
        {
            query = query.Where(t => t.Status == txStatus);
        }

        if (request.DateFrom.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= request.DateTo.Value);
        }

        if (!string.IsNullOrEmpty(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(t => 
                t.ReferenceNumber.ToLower().Contains(search) || 
                (t.Description != null && t.Description.ToLower().Contains(search)) ||
                (t.FromWallet != null && t.FromWallet.WalletNumber.ToLower().Contains(search)) ||
                (t.ToWallet != null && t.ToWallet.WalletNumber.ToLower().Contains(search)));
        }

        query = query.OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, request.PageSize);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => MapToDto(t))
            .ToListAsync(cancellationToken);

        return new PagedList<TransactionDto>(items, totalCount, page, pageSize, totalPages);
    }

    public async Task<List<TransactionDto>> Handle(GetMyRecentTransactionsQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);
        if (wallet == null)
        {
            return new List<TransactionDto>();
        }

        return await _dbContext.Transactions
            .Include(t => t.FromWallet).ThenInclude(w => w!.User)
            .Include(t => t.ToWallet).ThenInclude(w => w!.User)
            .Include(t => t.InitiatedByUser)
            .Where(t => t.FromWalletId == wallet.Id || t.ToWalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Take(request.Count)
            .Select(t => MapToDto(t))
            .ToListAsync(cancellationToken);
    }

    public async Task<TransactionDetailsDto> Handle(GetTransactionDetailsQuery request, CancellationToken cancellationToken)
    {
        // Fetch transaction and make sure the user is part of it (either sender, receiver or initiator)
        var transaction = await _dbContext.Transactions
            .Include(t => t.FromWallet).ThenInclude(w => w!.User)
            .Include(t => t.ToWallet).ThenInclude(w => w!.User)
            .Include(t => t.InitiatedByUser)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

        if (transaction == null)
        {
            throw new FinWalletException("TRANSACTION_NOT_FOUND", "Transaction not found.");
        }

        // Fetch wallet of the requesting user
        var myWallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);
        
        // Authorization check: User must be initiator, sender, or receiver
        var isAuthorized = transaction.InitiatedByUserId == request.UserId || 
                            (myWallet != null && (transaction.FromWalletId == myWallet.Id || transaction.ToWalletId == myWallet.Id));
        
        // Also check if user is admin (we can allow admin to query details - admin check can be handled by controller, but we can also check role here. To keep it decoupled, we allow request.UserId check).
        // Let's assume if they are authorized, or we'll bypass user check if it's admin (we can pass a flag or check if user is in Admin role. For simple scoping, we can check if there's a match, but if we query via Admin APIs we'll have a different command/query or bypass).
        if (!isAuthorized)
        {
            // Check if user is Admin
            var isAdmin = await _dbContext.UserRoles
                .AnyAsync(ur => ur.UserId == request.UserId && 
                                _dbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin"), cancellationToken);
            if (!isAdmin)
            {
                throw new FinWalletException("FORBIDDEN", "You are not authorized to view this transaction.");
            }
        }

        var txDto = MapToDto(transaction);

        // Fetch ledger entries for this transaction
        var ledgers = await _dbContext.LedgerEntries
            .Include(le => le.Wallet)
            .Where(le => le.TransactionId == transaction.Id)
            .Select(le => new LedgerEntryDto(
                le.Id,
                le.TransactionId,
                le.Wallet!.WalletNumber,
                le.EntryType.ToString(),
                le.Amount,
                le.BalanceBefore,
                le.BalanceAfter,
                le.Currency,
                le.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        // Fetch receipt if completed
        ReceiptDto? receiptDto = null;
        var receipt = await _dbContext.Receipts.FirstOrDefaultAsync(r => r.TransactionId == transaction.Id, cancellationToken);
        if (receipt != null)
        {
            receiptDto = new ReceiptDto(
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

        return new TransactionDetailsDto(txDto, ledgers, receiptDto);
    }

    private static TransactionDto MapToDto(Transaction t)
    {
        return new TransactionDto(
            t.Id,
            t.ReferenceNumber,
            t.Type.ToString(),
            t.Status.ToString(),
            t.FromWallet?.WalletNumber,
            t.ToWallet?.WalletNumber,
            t.InitiatedByUser?.FullName ?? "System",
            t.Amount,
            t.Currency,
            t.Description,
            t.RejectionReason,
            t.BankReference,
            t.CorrelationId,
            t.CreatedAt,
            t.CompletedAt
        );
    }
}
