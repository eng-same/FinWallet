using FinWallet.Application.Common.DTOs;
using FinWallet.Application.Common.Exceptions;
using FinWallet.Application.Common.Interfaces;
using FinWallet.Domain.Entities;
using FinWallet.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinWallet.Application.Features.Admin.Commands;

public sealed record FreezeWalletCommand(
    Guid WalletId,
    string Reason,
    Guid AdminUserId
) : IRequest<WalletDto>;

public sealed class FreezeWalletCommandValidator : AbstractValidator<FreezeWalletCommand>
{
    public FreezeWalletCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Freeze reason is required.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");
    }
}

public sealed class FreezeWalletCommandHandler : IRequestHandler<FreezeWalletCommand, WalletDto>
{
    private readonly IApplicationDbContext _dbContext;

    public FreezeWalletCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WalletDto> Handle(FreezeWalletCommand request, CancellationToken cancellationToken)
    {
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.Id == request.WalletId, cancellationToken);
        if (wallet == null)
        {
            throw new FinWalletException("WALLET_NOT_FOUND", "Wallet not found.");
        }

        if (wallet.Status == WalletStatus.Frozen)
        {
            throw new FinWalletException("VALIDATION_ERROR", "Wallet is already frozen.");
        }

        var oldStatus = wallet.Status;
        wallet.Status = WalletStatus.Frozen;
        wallet.FrozenAt = DateTimeOffset.UtcNow;
        wallet.FrozenReason = request.Reason;
        wallet.UpdatedAt = DateTimeOffset.UtcNow;

        // Create Status History
        var history = new WalletStatusHistory
        {
            WalletId = wallet.Id,
            OldStatus = oldStatus,
            NewStatus = WalletStatus.Frozen,
            Reason = request.Reason,
            ChangedByUserId = request.AdminUserId,
            ChangedAt = DateTimeOffset.UtcNow
        };
        _dbContext.WalletStatusHistories.Add(history);

        // Create Audit Log
        var audit = new AuditLog
        {
            UserId = request.AdminUserId,
            Action = "WalletFrozen",
            EntityName = nameof(Wallet),
            EntityId = wallet.Id.ToString(),
            Description = $"Wallet {wallet.WalletNumber} frozen. Reason: {request.Reason}",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.AuditLogs.Add(audit);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new WalletDto(
            wallet.Id,
            wallet.UserId,
            wallet.WalletNumber,
            wallet.Balance,
            wallet.Currency,
            wallet.Status.ToString(),
            wallet.CreatedAt,
            wallet.UpdatedAt,
            wallet.FrozenAt,
            wallet.FrozenReason
        );
    }
}
