using FinWallet.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinWallet.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<ApplicationRole> Roles { get; }
    DbSet<IdentityUserRole<Guid>> UserRoles { get; }
    DbSet<Wallet> Wallets { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }
    DbSet<Receipt> Receipts { get; }
    DbSet<BankTopUpRequest> BankTopUpRequests { get; }
    DbSet<WalletStatusHistory> WalletStatusHistories { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
