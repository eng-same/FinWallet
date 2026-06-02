using FinWallet.Domain.Enums;

namespace FinWallet.Domain.Entities;

public class LedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransactionId { get; set; }
    public Guid WalletId { get; set; }
    public LedgerEntryType EntryType { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Currency { get; set; } = "LYD";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public virtual Transaction? Transaction { get; set; }
    public virtual Wallet? Wallet { get; set; }
}
