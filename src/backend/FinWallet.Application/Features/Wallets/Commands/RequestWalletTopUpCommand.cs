using FinWallet.Application.Common.DTOs;
using FinWallet.Application.Common.Exceptions;
using FinWallet.Application.Common.Interfaces;
using FinWallet.Contracts;
using FinWallet.Domain.Entities;
using FinWallet.Domain.Enums;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinWallet.Application.Features.Wallets.Commands;

public sealed record RequestWalletTopUpCommand(
    Guid UserId,
    decimal Amount,
    string Currency,
    string? Description
) : IRequest<TransactionDto>;

public sealed class RequestWalletTopUpCommandValidator : AbstractValidator<RequestWalletTopUpCommand>
{
    public RequestWalletTopUpCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Equal("LYD").WithMessage("Currency must be LYD.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}

public sealed class RequestWalletTopUpCommandHandler : IRequestHandler<RequestWalletTopUpCommand, TransactionDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public RequestWalletTopUpCommandHandler(IApplicationDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<TransactionDto> Handle(RequestWalletTopUpCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch user's wallet
        var wallet = await _dbContext.Wallets
            .Include(w => w.User)
            .FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);

        if (wallet == null)
        {
            throw new FinWalletException("WALLET_NOT_FOUND", "Wallet not found for this user.");
        }

        // 2. Validate wallet status
        if (wallet.Status == WalletStatus.Frozen)
        {
            throw new FinWalletException("WALLET_FROZEN", "Your wallet is frozen. Top-up is not allowed.");
        }
        if (wallet.Status == WalletStatus.Closed)
        {
            throw new FinWalletException("WALLET_CLOSED", "Your wallet is closed.");
        }

        // 3. Generate reference numbers
        var timestampStr = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var shortGuid = Guid.NewGuid().ToString()[..8].ToUpper();
        
        var txReference = $"TX-TOP-{timestampStr}-{shortGuid}";
        var requestNumber = $"REQ-{timestampStr}-{shortGuid}";
        
        var correlationId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        // 4. Create the Transaction entity
        var transaction = new Transaction
        {
            Id = transactionId,
            ReferenceNumber = txReference,
            Type = TransactionType.TopUp,
            Status = TransactionStatus.PendingBankApproval,
            ToWalletId = wallet.Id,
            InitiatedByUserId = request.UserId,
            Amount = request.Amount,
            Currency = request.Currency,
            Description = request.Description,
            CorrelationId = correlationId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Transactions.Add(transaction);

        // 5. Create the BankTopUpRequest entity
        var bankRequest = new BankTopUpRequest
        {
            Id = requestId,
            TransactionId = transactionId,
            CorrelationId = correlationId,
            RequestNumber = requestNumber,
            Status = BankTopUpRequestStatus.Pending,
            Amount = request.Amount,
            Currency = request.Currency,
            RequestedAt = DateTimeOffset.UtcNow
        };
        _dbContext.BankTopUpRequests.Add(bankRequest);

        // 6. Write Audit Log
        var audit = new AuditLog
        {
            UserId = request.UserId,
            Action = "WalletToppedUp",
            EntityName = nameof(Transaction),
            EntityId = transactionId.ToString(),
            Description = $"Wallet top-up initiated. Ref: {txReference}, Amount: {request.Amount} {request.Currency}",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.AuditLogs.Add(audit);

        // Save entity changes (will be wrapped in Db Transaction by pipeline)
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 7. Publish to RabbitMQ via MassTransit
        var message = new BankTopUpRequested
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            TransactionId = transactionId,
            BankTopUpRequestId = requestId,
            RequestNumber = requestNumber,
            ReferenceNumber = txReference,
            WalletNumber = wallet.WalletNumber,
            UserId = request.UserId,
            Amount = request.Amount,
            Currency = request.Currency,
            Description = request.Description,
            RequestedAt = DateTimeOffset.UtcNow
        };

        await _publishEndpoint.Publish(message, cancellationToken);

        return new TransactionDto(
            transaction.Id,
            transaction.ReferenceNumber,
            transaction.Type.ToString(),
            transaction.Status.ToString(),
            null,
            wallet.WalletNumber,
            wallet.User?.FullName ?? "Unknown User",
            transaction.Amount,
            transaction.Currency,
            transaction.Description,
            null,
            null,
            correlationId,
            transaction.CreatedAt,
            null
        );
    }
}
