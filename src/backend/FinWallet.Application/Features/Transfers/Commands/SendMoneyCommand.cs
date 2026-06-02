using FinWallet.Application.Common.DTOs;
using FinWallet.Application.Common.Exceptions;
using FinWallet.Application.Common.Interfaces;
using FinWallet.Domain.Entities;
using FinWallet.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinWallet.Application.Features.Transfers.Commands;

public sealed record SendMoneyCommand(
    Guid InitiatorUserId,
    string ToWalletNumber,
    decimal Amount,
    string Currency,
    string? Description
) : IRequest<TransactionDto>;

public sealed class SendMoneyCommandValidator : AbstractValidator<SendMoneyCommand>
{
    public SendMoneyCommandValidator()
    {
        RuleFor(x => x.ToWalletNumber)
            .NotEmpty().WithMessage("Recipient wallet number is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Equal("LYD").WithMessage("Currency must be LYD.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}

public sealed class SendMoneyCommandHandler : IRequestHandler<SendMoneyCommand, TransactionDto>
{
    private readonly IApplicationDbContext _dbContext;

    public SendMoneyCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TransactionDto> Handle(SendMoneyCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch sender's wallet
        var senderWallet = await _dbContext.Wallets
            .Include(w => w.User)
            .FirstOrDefaultAsync(w => w.UserId == request.InitiatorUserId, cancellationToken);

        if (senderWallet == null)
        {
            throw new FinWalletException("WALLET_NOT_FOUND", "Sender wallet not found.");
        }

        // 2. Fetch receiver's wallet
        var receiverWallet = await _dbContext.Wallets
            .Include(w => w.User)
            .FirstOrDefaultAsync(w => w.WalletNumber == request.ToWalletNumber, cancellationToken);

        if (receiverWallet == null)
        {
            throw new FinWalletException("WALLET_NOT_FOUND", $"Recipient wallet {request.ToWalletNumber} not found.");
        }

        // 3. Prevent transfer to self
        if (senderWallet.Id == receiverWallet.Id)
        {
            throw new FinWalletException("CANNOT_TRANSFER_TO_SELF", "You cannot send money to your own wallet.");
        }

        // 4. Validate statuses
        if (senderWallet.Status == WalletStatus.Frozen)
        {
            throw new FinWalletException("WALLET_FROZEN", "Your wallet is frozen. Outgoing transfers are blocked.");
        }
        if (senderWallet.Status == WalletStatus.Closed)
        {
            throw new FinWalletException("WALLET_CLOSED", "Your wallet is closed.");
        }

        if (receiverWallet.Status == WalletStatus.Frozen)
        {
            throw new FinWalletException("WALLET_FROZEN", "Recipient wallet is frozen. Transfer blocked.");
        }
        if (receiverWallet.Status == WalletStatus.Closed)
        {
            throw new FinWalletException("WALLET_CLOSED", "Recipient wallet is closed.");
        }

        // 5. Check sender has enough balance
        if (senderWallet.Balance < request.Amount)
        {
            throw new FinWalletException("INSUFFICIENT_BALANCE", "Insufficient balance for this transfer.");
        }

        // 6. Atomically update balances
        var senderBefore = senderWallet.Balance;
        senderWallet.Balance -= request.Amount;
        senderWallet.UpdatedAt = DateTimeOffset.UtcNow;

        var receiverBefore = receiverWallet.Balance;
        receiverWallet.Balance += request.Amount;
        receiverWallet.UpdatedAt = DateTimeOffset.UtcNow;

        // 7. Generate reference numbers
        var timestampStr = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var shortGuid = Guid.NewGuid().ToString()[..8].ToUpper();
        
        var txReference = $"TX-TRF-{timestampStr}-{shortGuid}";
        var receiptNumber = $"REC-TRF-{timestampStr}-{shortGuid}";

        var transactionId = Guid.NewGuid();

        // 8. Create the Transaction entity
        var transaction = new Transaction
        {
            Id = transactionId,
            ReferenceNumber = txReference,
            Type = TransactionType.Transfer,
            Status = TransactionStatus.Completed,
            FromWalletId = senderWallet.Id,
            ToWalletId = receiverWallet.Id,
            InitiatedByUserId = request.InitiatorUserId,
            Amount = request.Amount,
            Currency = request.Currency,
            Description = request.Description,
            CreatedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Transactions.Add(transaction);

        // 9. Create Ledger Entries
        var debitEntry = new LedgerEntry
        {
            TransactionId = transactionId,
            WalletId = senderWallet.Id,
            EntryType = LedgerEntryType.Debit,
            Amount = request.Amount,
            BalanceBefore = senderBefore,
            BalanceAfter = senderWallet.Balance,
            Currency = request.Currency,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.LedgerEntries.Add(debitEntry);

        var creditEntry = new LedgerEntry
        {
            TransactionId = transactionId,
            WalletId = receiverWallet.Id,
            EntryType = LedgerEntryType.Credit,
            Amount = request.Amount,
            BalanceBefore = receiverBefore,
            BalanceAfter = receiverWallet.Balance,
            Currency = request.Currency,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.LedgerEntries.Add(creditEntry);

        // 10. Create Receipt
        var receipt = new Receipt
        {
            TransactionId = transactionId,
            ReceiptNumber = receiptNumber,
            IssuedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Receipts.Add(receipt);

        // 11. Write Audit Logs
        var senderAudit = new AuditLog
        {
            UserId = request.InitiatorUserId,
            Action = "TransferCompleted",
            EntityName = nameof(Transaction),
            EntityId = transactionId.ToString(),
            Description = $"Sent {request.Amount} {request.Currency} to {request.ToWalletNumber}. Ref: {txReference}",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.AuditLogs.Add(senderAudit);

        var receiptAudit = new AuditLog
        {
            UserId = request.InitiatorUserId,
            Action = "ReceiptGenerated",
            EntityName = nameof(Receipt),
            EntityId = receipt.Id.ToString(),
            Description = $"Receipt {receiptNumber} generated for transfer {txReference}",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.AuditLogs.Add(receiptAudit);

        // Save changes (the TransactionBehavior will commit the database transaction)
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TransactionDto(
            transaction.Id,
            transaction.ReferenceNumber,
            transaction.Type.ToString(),
            transaction.Status.ToString(),
            senderWallet.WalletNumber,
            receiverWallet.WalletNumber,
            senderWallet.User?.FullName ?? "Unknown User",
            transaction.Amount,
            transaction.Currency,
            transaction.Description,
            null,
            null,
            null,
            transaction.CreatedAt,
            transaction.CompletedAt
        );
    }
}
