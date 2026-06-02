namespace FinWallet.Domain.Entities;

public class Receipt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransactionId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation property
    public virtual Transaction? Transaction { get; set; }
}
