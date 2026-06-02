namespace FinWallet.Contracts;

public sealed record BankTopUpProcessed
{
    public Guid MessageId { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid TransactionId { get; init; }
    public Guid BankTopUpRequestId { get; init; }
    public string RequestNumber { get; init; } = string.Empty;
    public string Decision { get; init; } = string.Empty; // "Accepted" or "Rejected"
    public string? BankReference { get; init; }
    public string? RejectionReason { get; init; }
    public DateTimeOffset ProcessedAt { get; init; }
}
