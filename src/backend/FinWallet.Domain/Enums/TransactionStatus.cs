namespace FinWallet.Domain.Enums;

public enum TransactionStatus
{
    Created,
    PendingBankApproval,
    Completed,
    Rejected,
    Failed,
    Cancelled
}
