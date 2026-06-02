using FinWallet.Domain.Enums;

namespace FinWallet.Domain.Entities;

public class WalletStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WalletId { get; set; }
    public WalletStatus OldStatus { get; set; }
    public WalletStatus NewStatus { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public virtual Wallet? Wallet { get; set; }
    public virtual ApplicationUser? ChangedByUser { get; set; }
}
