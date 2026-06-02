using FinWallet.Domain.Enums;

namespace FinWallet.Domain.Entities;

public class Wallet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string WalletNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "LYD";
    public WalletStatus Status { get; set; } = WalletStatus.Active;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FrozenAt { get; set; }
    public string? FrozenReason { get; set; }

    // Navigation property
    public virtual ApplicationUser? User { get; set; }
}
