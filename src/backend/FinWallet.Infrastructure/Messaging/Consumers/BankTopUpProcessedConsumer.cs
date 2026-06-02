using FinWallet.Contracts;
using FinWallet.Domain.Entities;
using FinWallet.Domain.Enums;
using FinWallet.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinWallet.Infrastructure.Messaging.Consumers;

public sealed class BankTopUpProcessedConsumer : IConsumer<BankTopUpProcessed>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<BankTopUpProcessedConsumer> _logger;

    public BankTopUpProcessedConsumer(ApplicationDbContext dbContext, ILogger<BankTopUpProcessedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BankTopUpProcessed> context)
    {
        var message = context.Message;
        _logger.LogInformation("Consuming BankTopUpProcessed response. Request ID: {RequestId}, Decision: {Decision}", 
            message.BankTopUpRequestId, message.Decision);

        // Start a database transaction to ensure atomicity
        await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var topUpRequest = await _dbContext.BankTopUpRequests
                .Include(r => r.Transaction)
                .FirstOrDefaultAsync(r => r.Id == message.BankTopUpRequestId);

            if (topUpRequest == null)
            {
                _logger.LogWarning("BankTopUpRequest with ID {RequestId} not found.", message.BankTopUpRequestId);
                return;
            }

            // Check if already processed (Idempotency)
            if (topUpRequest.Status != BankTopUpRequestStatus.Pending)
            {
                _logger.LogInformation("BankTopUpRequest {RequestId} already processed.", message.BankTopUpRequestId);
                return;
            }

            var transaction = topUpRequest.Transaction;
            if (transaction == null)
            {
                _logger.LogError("Associated transaction not found for top-up request {RequestId}", topUpRequest.Id);
                return;
            }

            var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.Id == transaction.ToWalletId);
            if (wallet == null)
            {
                _logger.LogError("Wallet {WalletId} not found for transaction {TransactionId}", transaction.ToWalletId, transaction.Id);
                return;
            }

            if (message.Decision == "Accepted")
            {
                // Update top-up request
                topUpRequest.Status = BankTopUpRequestStatus.Accepted;
                topUpRequest.BankReference = message.BankReference;
                topUpRequest.ProcessedAt = message.ProcessedAt;

                // Update wallet balance
                var balanceBefore = wallet.Balance;
                wallet.Balance += topUpRequest.Amount;
                wallet.UpdatedAt = DateTimeOffset.UtcNow;

                // Update transaction
                transaction.Status = TransactionStatus.Completed;
                transaction.BankReference = message.BankReference;
                transaction.CompletedAt = message.ProcessedAt;

                // Create ledger entry
                var ledgerEntry = new LedgerEntry
                {
                    TransactionId = transaction.Id,
                    WalletId = wallet.Id,
                    EntryType = LedgerEntryType.Credit,
                    Amount = topUpRequest.Amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance,
                    Currency = topUpRequest.Currency,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _dbContext.LedgerEntries.Add(ledgerEntry);

                // Create receipt
                var receiptNumber = $"REC-{DateTimeOffset.UtcNow.ToString("yyyyMMdd")}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
                var receipt = new Receipt
                {
                    TransactionId = transaction.Id,
                    ReceiptNumber = receiptNumber,
                    IssuedAt = DateTimeOffset.UtcNow
                };
                _dbContext.Receipts.Add(receipt);

                // Create Audit Logs
                var auditLog = new AuditLog
                {
                    UserId = transaction.InitiatedByUserId,
                    Action = "BankTopUpAccepted",
                    EntityName = nameof(BankTopUpRequest),
                    EntityId = topUpRequest.Id.ToString(),
                    Description = $"Bank top-up of {topUpRequest.Amount} {topUpRequest.Currency} accepted. Bank Ref: {message.BankReference}",
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _dbContext.AuditLogs.Add(auditLog);

                var receiptAuditLog = new AuditLog
                {
                    UserId = transaction.InitiatedByUserId,
                    Action = "ReceiptGenerated",
                    EntityName = nameof(Receipt),
                    EntityId = receipt.Id.ToString(),
                    Description = $"Receipt {receiptNumber} generated for top-up transaction {transaction.Id}",
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _dbContext.AuditLogs.Add(receiptAuditLog);
            }
            else
            {
                // Rejected flow
                topUpRequest.Status = BankTopUpRequestStatus.Rejected;
                topUpRequest.RejectionReason = message.RejectionReason;
                topUpRequest.ProcessedAt = message.ProcessedAt;

                transaction.Status = TransactionStatus.Rejected;
                transaction.RejectionReason = message.RejectionReason;
                transaction.CompletedAt = message.ProcessedAt;

                // Create Audit Log
                var auditLog = new AuditLog
                {
                    UserId = transaction.InitiatedByUserId,
                    Action = "BankTopUpRejected",
                    EntityName = nameof(BankTopUpRequest),
                    EntityId = topUpRequest.Id.ToString(),
                    Description = $"Bank top-up of {topUpRequest.Amount} {topUpRequest.Currency} rejected. Reason: {message.RejectionReason}",
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _dbContext.AuditLogs.Add(auditLog);
            }

            await _dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            _logger.LogInformation("Top-up request {RequestId} successfully processed as {Decision}.", 
                topUpRequest.Id, message.Decision);
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Error processing BankTopUpProcessed message for Request ID: {RequestId}", message.BankTopUpRequestId);
            throw;
        }
    }
}
