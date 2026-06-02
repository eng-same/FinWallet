using FinWallet.Application.Common.DTOs;
using FinWallet.Application.Common.Exceptions;
using FinWallet.Application.Common.Interfaces;
using FinWallet.Domain.Entities;
using FinWallet.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinWallet.Application.Features.Admin.Queries;

// 1. Dashboard Query
public sealed record GetAdminDashboardQuery : IRequest<AdminDashboardDto>;

// 2. Users Query
public sealed record GetAdminUsersQuery(string? Search = null) : IRequest<List<UserDto>>;

// 3. User Details Query
public record AdminUserDetailsDto(
    UserDto User,
    WalletDto? Wallet,
    List<TransactionDto> RecentTransactions
);
public sealed record GetAdminUserDetailsQuery(Guid UserId) : IRequest<AdminUserDetailsDto>;

// 4. Wallets Query
public sealed record GetAdminWalletsQuery(string? Search = null) : IRequest<List<WalletDto>>;

// 5. Wallet Details Query
public record WalletStatusHistoryDto(
    Guid Id,
    string OldStatus,
    string NewStatus,
    string Reason,
    string ChangedByFullName,
    DateTimeOffset ChangedAt
);
public record AdminWalletDetailsDto(
    WalletDto Wallet,
    UserDto Owner,
    List<WalletStatusHistoryDto> StatusHistories
);
public sealed record GetAdminWalletDetailsQuery(Guid WalletId) : IRequest<AdminWalletDetailsDto>;

// 6. Transactions Query
public sealed record GetAdminTransactionsQuery(
    string? Type = null,
    string? Status = null,
    string? Search = null
) : IRequest<List<TransactionDto>>;

// 7. Audit Logs Query
public sealed record GetAdminAuditLogsQuery(string? Search = null) : IRequest<List<AuditLogDto>>;


// --- HANDLERS ---
public sealed class AdminQueriesHandler :
    IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>,
    IRequestHandler<GetAdminUsersQuery, List<UserDto>>,
    IRequestHandler<GetAdminUserDetailsQuery, AdminUserDetailsDto>,
    IRequestHandler<GetAdminWalletsQuery, List<WalletDto>>,
    IRequestHandler<GetAdminWalletDetailsQuery, AdminWalletDetailsDto>,
    IRequestHandler<GetAdminTransactionsQuery, List<TransactionDto>>,
    IRequestHandler<GetAdminAuditLogsQuery, List<AuditLogDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminQueriesHandler(IApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<AdminDashboardDto> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        // Total normal users (in User role)
        var totalUsers = await _dbContext.UserRoles
            .Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur, r })
            .Where(x => x.r.Name == "User")
            .CountAsync(cancellationToken);

        var totalWallets = await _dbContext.Wallets.CountAsync(cancellationToken);
        var activeWallets = await _dbContext.Wallets.CountAsync(w => w.Status == WalletStatus.Active, cancellationToken);
        var frozenWallets = await _dbContext.Wallets.CountAsync(w => w.Status == WalletStatus.Frozen, cancellationToken);

        var transactionsQuery = _dbContext.Transactions
            .Include(t => t.FromWallet).ThenInclude(w => w!.User)
            .Include(t => t.ToWallet).ThenInclude(w => w!.User)
            .Include(t => t.InitiatedByUser);

        var totalTransactions = await transactionsQuery.CountAsync(cancellationToken);
        
        var totalVolume = await _dbContext.Transactions
            .Where(t => t.Status == TransactionStatus.Completed)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var today = DateTimeOffset.UtcNow.Date;
        var todayTransactions = await _dbContext.Transactions
            .Where(t => t.CreatedAt >= today && t.Status == TransactionStatus.Completed)
            .CountAsync(cancellationToken);

        var todayVolume = await _dbContext.Transactions
            .Where(t => t.CreatedAt >= today && t.Status == TransactionStatus.Completed)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var recentTransactions = await transactionsQuery
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .Select(t => MapTransactionDto(t))
            .ToListAsync(cancellationToken);

        return new AdminDashboardDto(
            totalUsers,
            totalWallets,
            activeWallets,
            frozenWallets,
            totalTransactions,
            totalVolume,
            todayTransactions,
            todayVolume,
            recentTransactions
        );
    }

    public async Task<List<UserDto>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        var usersQuery = _dbContext.Users.AsQueryable();

        if (!string.IsNullOrEmpty(request.Search))
        {
            var search = request.Search.ToLower();
            usersQuery = usersQuery.Where(u => 
                u.FullName.ToLower().Contains(search) || 
                (u.Email != null && u.Email.ToLower().Contains(search)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
        }

        return await usersQuery
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserDto(u.Id, u.FullName, u.Email ?? string.Empty, u.PhoneNumber ?? string.Empty, u.IsActive, u.CreatedAt, u.LastLoginAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminUserDetailsDto> Handle(GetAdminUserDetailsQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            throw new FinWalletException("USER_NOT_FOUND", "User not found.");
        }

        var wallet = await _dbContext.Wallets
            .FirstOrDefaultAsync(w => w.UserId == user.Id, cancellationToken);

        WalletDto? walletDto = null;
        var recentTxDtos = new List<TransactionDto>();

        if (wallet != null)
        {
            walletDto = new WalletDto(
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

            recentTxDtos = await _dbContext.Transactions
                .Include(t => t.FromWallet).ThenInclude(w => w!.User)
                .Include(t => t.ToWallet).ThenInclude(w => w!.User)
                .Include(t => t.InitiatedByUser)
                .Where(t => t.FromWalletId == wallet.Id || t.ToWalletId == wallet.Id)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .Select(t => MapTransactionDto(t))
                .ToListAsync(cancellationToken);
        }

        var userDto = new UserDto(user.Id, user.FullName, user.Email ?? string.Empty, user.PhoneNumber ?? string.Empty, user.IsActive, user.CreatedAt, user.LastLoginAt);

        return new AdminUserDetailsDto(userDto, walletDto, recentTxDtos);
    }

    public async Task<List<WalletDto>> Handle(GetAdminWalletsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Wallets.Include(w => w.User).AsQueryable();

        if (!string.IsNullOrEmpty(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(w => 
                w.WalletNumber.ToLower().Contains(search) || 
                w.User!.FullName.ToLower().Contains(search) ||
                (w.User.Email != null && w.User.Email.ToLower().Contains(search)));
        }

        return await query
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WalletDto(w.Id, w.UserId, w.WalletNumber, w.Balance, w.Currency, w.Status.ToString(), w.CreatedAt, w.UpdatedAt, w.FrozenAt, w.FrozenReason))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminWalletDetailsDto> Handle(GetAdminWalletDetailsQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _dbContext.Wallets
            .Include(w => w.User)
            .FirstOrDefaultAsync(w => w.Id == request.WalletId, cancellationToken);

        if (wallet == null)
        {
            throw new FinWalletException("WALLET_NOT_FOUND", "Wallet not found.");
        }

        var owner = wallet.User;
        if (owner == null)
        {
            throw new FinWalletException("USER_NOT_FOUND", "Wallet owner not found.");
        }

        var walletDto = new WalletDto(wallet.Id, wallet.UserId, wallet.WalletNumber, wallet.Balance, wallet.Currency, wallet.Status.ToString(), wallet.CreatedAt, wallet.UpdatedAt, wallet.FrozenAt, wallet.FrozenReason);
        var ownerDto = new UserDto(owner.Id, owner.FullName, owner.Email ?? string.Empty, owner.PhoneNumber ?? string.Empty, owner.IsActive, owner.CreatedAt, owner.LastLoginAt);

        var histories = await _dbContext.WalletStatusHistories
            .Include(h => h.ChangedByUser)
            .Where(h => h.WalletId == wallet.Id)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new WalletStatusHistoryDto(
                h.Id,
                h.OldStatus.ToString(),
                h.NewStatus.ToString(),
                h.Reason,
                h.ChangedByUser!.FullName,
                h.ChangedAt
            ))
            .ToListAsync(cancellationToken);

        return new AdminWalletDetailsDto(walletDto, ownerDto, histories);
    }

    public async Task<List<TransactionDto>> Handle(GetAdminTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Transactions
            .Include(t => t.FromWallet).ThenInclude(w => w!.User)
            .Include(t => t.ToWallet).ThenInclude(w => w!.User)
            .Include(t => t.InitiatedByUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.Type) && Enum.TryParse<TransactionType>(request.Type, true, out var type))
        {
            query = query.Where(t => t.Type == type);
        }

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<TransactionStatus>(request.Status, true, out var status))
        {
            query = query.Where(t => t.Status == status);
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

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => MapTransactionDto(t))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AuditLogDto>> Handle(GetAdminAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.AuditLogs
            .Include(al => al.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(al => 
                al.Action.ToLower().Contains(search) || 
                al.Description.ToLower().Contains(search) ||
                (al.User != null && al.User.Email != null && al.User.Email.ToLower().Contains(search)));
        }

        return await query
            .OrderByDescending(al => al.CreatedAt)
            .Select(al => new AuditLogDto(
                al.Id,
                al.UserId,
                al.User != null ? al.User.Email : "System",
                al.Action,
                al.EntityName,
                al.EntityId,
                al.Description,
                al.IpAddress,
                al.UserAgent,
                al.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }

    private static TransactionDto MapTransactionDto(Transaction t)
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
