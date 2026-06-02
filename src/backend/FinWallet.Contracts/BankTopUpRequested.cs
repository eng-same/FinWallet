namespace FinWallet.Contracts;

public sealed record BankTopUpRequested
{
    public Guid MessageId { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid TransactionId { get; init; }
    public Guid BankTopUpRequestId { get; init; }
    public string RequestNumber { get; init; } = string.Empty;
    public string ReferenceNumber { get; init; } = string.Empty;
    public string WalletNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "LYD";
    public string? Description { get; init; }
    public DateTimeOffset RequestedAt { get; init; }
}
