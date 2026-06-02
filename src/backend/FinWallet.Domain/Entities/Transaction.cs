using FinWallet.Domain.Enums;

namespace FinWallet.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ReferenceNumber { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Created;
    public Guid? FromWalletId { get; set; }
    public Guid? ToWalletId { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "LYD";
    public string? Description { get; set; }
    public string? RejectionReason { get; set; }
    public string? BankReference { get; set; }
    public Guid? CorrelationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    // Navigation properties
    public virtual Wallet? FromWallet { get; set; }
    public virtual Wallet? ToWallet { get; set; }
    public virtual ApplicationUser? InitiatedByUser { get; set; }
}
