using FinWallet.Domain.Enums;

namespace FinWallet.Domain.Entities;

public class BankTopUpRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransactionId { get; set; }
    public Guid CorrelationId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public BankTopUpRequestStatus Status { get; set; } = BankTopUpRequestStatus.Pending;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "LYD";
    public string? BankReference { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }

    // Navigation property
    public virtual Transaction? Transaction { get; set; }
}
