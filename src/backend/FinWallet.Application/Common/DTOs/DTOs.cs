namespace FinWallet.Application.Common.DTOs;

public record UserDto(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt
);

public record WalletDto(
    Guid Id,
    Guid UserId,
    string WalletNumber,
    decimal Balance,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? FrozenAt,
    string? FrozenReason
);

public record AuthResponseDto(
    string Token,
    UserDto User,
    string Role,
    WalletDto? Wallet
);

public record TransactionDto(
    Guid Id,
    string ReferenceNumber,
    string Type,
    string Status,
    string? FromWalletNumber,
    string? ToWalletNumber,
    string InitiatedByFullName,
    decimal Amount,
    string Currency,
    string? Description,
    string? RejectionReason,
    string? BankReference,
    Guid? CorrelationId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt
);

public record ReceiptDto(
    Guid Id,
    Guid TransactionId,
    string ReceiptNumber,
    DateTimeOffset IssuedAt,
    decimal Amount,
    string Currency,
    string? FromWalletNumber,
    string? ToWalletNumber,
    string InitiatedByFullName,
    DateTimeOffset CompletedAt,
    string? Description
);

public record LedgerEntryDto(
    Guid Id,
    Guid TransactionId,
    string WalletNumber,
    string EntryType,
    decimal Amount,
    decimal BalanceBefore,
    decimal BalanceAfter,
    string Currency,
    DateTimeOffset CreatedAt
);

public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string? UserEmail,
    string Action,
    string? EntityName,
    string? EntityId,
    string Description,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset CreatedAt
);

public record AdminDashboardDto(
    int TotalUsers,
    int TotalWallets,
    int ActiveWallets,
    int FrozenWallets,
    int TotalTransactions,
    decimal TotalTransactionVolume,
    int TodayTransactions,
    decimal TodayTransactionVolume,
    List<TransactionDto> RecentTransactions
);
