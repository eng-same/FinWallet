using FinWallet.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FinWallet.MockBankCore.Worker.Messaging.Consumers;

public sealed class BankTopUpRequestedConsumer : IConsumer<BankTopUpRequested>
{
    private readonly ILogger<BankTopUpRequestedConsumer> _logger;
    private static readonly Random RandomGenerator = new();

    private static readonly string[] RejectionReasons =
    {
        "Insufficient external bank balance",
        "Account temporarily blocked",
        "Daily transaction limit exceeded",
        "Suspicious transaction",
        "Bank core timeout",
        "Invalid account status",
        "Service unavailable",
        "Random rejection for simulation"
    };

    public BankTopUpRequestedConsumer(ILogger<BankTopUpRequestedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BankTopUpRequested> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received BankTopUpRequested. Request Number: {RequestNumber}, Amount: {Amount} {Currency}", 
            message.RequestNumber, message.Amount, message.Currency);

        // 1. Simulate processing delay (2 seconds)
        await Task.Delay(2000);

        // 2. Decide outcome (70% Accepted, 30% Rejected)
        var isAccepted = RandomGenerator.NextDouble() < 0.70;
        
        string decision;
        string? bankReference = null;
        string? rejectionReason = null;

        if (isAccepted)
        {
            decision = "Accepted";
            bankReference = $"BANK-SIM-{RandomGenerator.Next(100000, 999999)}";
            _logger.LogInformation("Mock Bank ACCEPTED Request Number: {RequestNumber}. Bank Ref: {BankReference}", 
                message.RequestNumber, bankReference);
        }
        else
        {
            decision = "Rejected";
            rejectionReason = RejectionReasons[RandomGenerator.Next(RejectionReasons.Length)];
            _logger.LogWarning("Mock Bank REJECTED Request Number: {RequestNumber}. Reason: {RejectionReason}", 
                message.RequestNumber, rejectionReason);
        }

        // 3. Publish response
        var response = new BankTopUpProcessed
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = message.CorrelationId,
            TransactionId = message.TransactionId,
            BankTopUpRequestId = message.BankTopUpRequestId,
            RequestNumber = message.RequestNumber,
            Decision = decision,
            BankReference = bankReference,
            RejectionReason = rejectionReason,
            ProcessedAt = DateTimeOffset.UtcNow
        };

        await context.Publish(response);
    }
}
