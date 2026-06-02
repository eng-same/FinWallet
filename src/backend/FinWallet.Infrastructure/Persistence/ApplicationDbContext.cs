using FinWallet.Application.Common.Interfaces;
using FinWallet.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinWallet.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<BankTopUpRequest> BankTopUpRequests => Set<BankTopUpRequest>();
    public DbSet<WalletStatusHistory> WalletStatusHistories => Set<WalletStatusHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1. Identity Schema Configuration
        builder.HasDefaultSchema("identity");
        
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users", "identity");
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.HasOne(u => u.Wallet)
                  .WithOne(w => w.User)
                  .HasForeignKey<Wallet>(w => w.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApplicationRole>().ToTable("Roles", "identity");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("UserRoles", "identity");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("UserClaims", "identity");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("UserLogins", "identity");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("UserTokens", "identity");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("RoleClaims", "identity");

        // 2. Wallet Schema Configuration
        builder.Entity<Wallet>(entity =>
        {
            entity.ToTable("Wallets", "wallet");
            entity.HasKey(w => w.Id);
            entity.HasIndex(w => w.WalletNumber).IsUnique();
            entity.HasIndex(w => w.UserId).IsUnique();
            
            entity.Property(w => w.WalletNumber).HasMaxLength(50).IsRequired();
            entity.Property(w => w.Currency).HasMaxLength(10).IsRequired();
            entity.Property(w => w.FrozenReason).HasMaxLength(500);

            // Precision for LYD (normally 3 decimal places e.g. 100.000)
            entity.Property(w => w.Balance).HasPrecision(18, 3);

            // Database constraint: Balance >= 0
            entity.ToTable(w => w.HasCheckConstraint("CK_Wallet_Balance_Min", "\"Balance\" >= 0"));
        });

        builder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transactions", "wallet");
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.ReferenceNumber).IsUnique();
            
            entity.Property(t => t.ReferenceNumber).HasMaxLength(50).IsRequired();
            entity.Property(t => t.Currency).HasMaxLength(10).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(500);
            entity.Property(t => t.RejectionReason).HasMaxLength(500);
            entity.Property(t => t.BankReference).HasMaxLength(100);

            entity.Property(t => t.Amount).HasPrecision(18, 3);
            entity.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(30);

            // Database constraint: Amount > 0
            entity.ToTable(t => t.HasCheckConstraint("CK_Transaction_Amount_Min", "\"Amount\" > 0"));

            entity.HasOne(t => t.FromWallet)
                  .WithMany()
                  .HasForeignKey(t => t.FromWalletId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.ToWallet)
                  .WithMany()
                  .HasForeignKey(t => t.ToWalletId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.InitiatedByUser)
                  .WithMany()
                  .HasForeignKey(t => t.InitiatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LedgerEntry>(entity =>
        {
            entity.ToTable("LedgerEntries", "wallet");
            entity.HasKey(le => le.Id);

            entity.Property(le => le.Currency).HasMaxLength(10).IsRequired();
            entity.Property(le => le.Amount).HasPrecision(18, 3);
            entity.Property(le => le.BalanceBefore).HasPrecision(18, 3);
            entity.Property(le => le.BalanceAfter).HasPrecision(18, 3);
            
            entity.Property(le => le.EntryType).HasConversion<string>().HasMaxLength(20);

            entity.ToTable(le => le.HasCheckConstraint("CK_LedgerEntry_Amount_Min", "\"Amount\" > 0"));

            entity.HasOne(le => le.Transaction)
                  .WithMany()
                  .HasForeignKey(le => le.TransactionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(le => le.Wallet)
                  .WithMany()
                  .HasForeignKey(le => le.WalletId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Receipt>(entity =>
        {
            entity.ToTable("Receipts", "wallet");
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.ReceiptNumber).IsUnique();

            entity.Property(r => r.ReceiptNumber).HasMaxLength(50).IsRequired();

            entity.HasOne(r => r.Transaction)
                  .WithOne()
                  .HasForeignKey<Receipt>(r => r.TransactionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 3. Integration Schema Configuration
        builder.Entity<BankTopUpRequest>(entity =>
        {
            entity.ToTable("BankTopUpRequests", "integration");
            entity.HasKey(b => b.Id);
            entity.HasIndex(b => b.RequestNumber).IsUnique();
            entity.HasIndex(b => b.CorrelationId).IsUnique();

            entity.Property(b => b.RequestNumber).HasMaxLength(50).IsRequired();
            entity.Property(b => b.Currency).HasMaxLength(10).IsRequired();
            entity.Property(b => b.BankReference).HasMaxLength(100);
            entity.Property(b => b.RejectionReason).HasMaxLength(500);
            entity.Property(b => b.Amount).HasPrecision(18, 3);
            entity.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(b => b.Transaction)
                  .WithMany()
                  .HasForeignKey(b => b.TransactionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 4. Audit Schema Configuration
        builder.Entity<WalletStatusHistory>(entity =>
        {
            entity.ToTable("WalletStatusHistories", "audit");
            entity.HasKey(wsh => wsh.Id);

            entity.Property(wsh => wsh.Reason).HasMaxLength(500).IsRequired();
            entity.Property(wsh => wsh.OldStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(wsh => wsh.NewStatus).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(wsh => wsh.Wallet)
                  .WithMany()
                  .HasForeignKey(wsh => wsh.WalletId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(wsh => wsh.ChangedByUser)
                  .WithMany()
                  .HasForeignKey(wsh => wsh.ChangedByUserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs", "audit");
            entity.HasKey(al => al.Id);

            entity.Property(al => al.Action).HasMaxLength(100).IsRequired();
            entity.Property(al => al.EntityName).HasMaxLength(100);
            entity.Property(al => al.EntityId).HasMaxLength(50);
            entity.Property(al => al.Description).HasMaxLength(1000).IsRequired();
            entity.Property(al => al.IpAddress).HasMaxLength(45);
            entity.Property(al => al.UserAgent).HasMaxLength(500);

            entity.HasOne(al => al.User)
                  .WithMany()
                  .HasForeignKey(al => al.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
