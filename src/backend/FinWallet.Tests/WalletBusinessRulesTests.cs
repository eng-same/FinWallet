using FinWallet.Application.Common.Exceptions;
using FinWallet.Application.Features.Transfers.Commands;
using FinWallet.Application.Features.Wallets.Commands;
using FinWallet.Domain.Entities;
using FinWallet.Domain.Enums;
using FinWallet.Infrastructure.Persistence;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace FinWallet.Tests;

public sealed class WalletBusinessRulesTests
{
    private readonly DbContextOptions<ApplicationDbContext> _dbOptions;

    public WalletBusinessRulesTests()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private ApplicationDbContext CreateDbContext()
    {
        return new ApplicationDbContext(_dbOptions);
    }

    [Fact]
    public async Task SendMoneyCommand_ShouldFail_WhenBalanceIsInsufficient()
    {
        // Arrange
        using var context = CreateDbContext();
        var user1 = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Ahmed Ali", Email = "ahmed@example.com" };
        var user2 = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Mohamed Ali", Email = "mohamed@example.com" };
        context.Users.AddRange(user1, user2);

        var wallet1 = new Wallet { Id = Guid.NewGuid(), UserId = user1.Id, WalletNumber = "FW-001", Balance = 10m, Status = WalletStatus.Active };
        var wallet2 = new Wallet { Id = Guid.NewGuid(), UserId = user2.Id, WalletNumber = "FW-002", Balance = 0m, Status = WalletStatus.Active };
        context.Wallets.AddRange(wallet1, wallet2);
        await context.SaveChangesAsync();

        var handler = new SendMoneyCommandHandler(context);
        var command = new SendMoneyCommand(user1.Id, "FW-002", 50m, "LYD", "Transfer test");

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FinWalletException>()
            .Where(e => e.Code == "INSUFFICIENT_BALANCE");
    }

    [Fact]
    public async Task SendMoneyCommand_ShouldFail_WhenTransferToSelf()
    {
        // Arrange
        using var context = CreateDbContext();
        var user = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Ahmed Ali", Email = "ahmed@example.com" };
        context.Users.Add(user);

        var wallet = new Wallet { Id = Guid.NewGuid(), UserId = user.Id, WalletNumber = "FW-001", Balance = 100m, Status = WalletStatus.Active };
        context.Wallets.Add(wallet);
        await context.SaveChangesAsync();

        var handler = new SendMoneyCommandHandler(context);
        var command = new SendMoneyCommand(user.Id, "FW-001", 10m, "LYD", "Self transfer");

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FinWalletException>()
            .Where(e => e.Code == "CANNOT_TRANSFER_TO_SELF");
    }

    [Fact]
    public async Task SendMoneyCommand_ShouldFail_WhenSenderWalletIsFrozen()
    {
        // Arrange
        using var context = CreateDbContext();
        var user1 = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Ahmed Ali", Email = "ahmed@example.com" };
        var user2 = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Mohamed Ali", Email = "mohamed@example.com" };
        context.Users.AddRange(user1, user2);

        var wallet1 = new Wallet { Id = Guid.NewGuid(), UserId = user1.Id, WalletNumber = "FW-001", Balance = 100m, Status = WalletStatus.Frozen };
        var wallet2 = new Wallet { Id = Guid.NewGuid(), UserId = user2.Id, WalletNumber = "FW-002", Balance = 0m, Status = WalletStatus.Active };
        context.Wallets.AddRange(wallet1, wallet2);
        await context.SaveChangesAsync();

        var handler = new SendMoneyCommandHandler(context);
        var command = new SendMoneyCommand(user1.Id, "FW-002", 50m, "LYD", "Transfer from frozen");

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FinWalletException>()
            .Where(e => e.Code == "WALLET_FROZEN");
    }

    [Fact]
    public async Task SendMoneyCommand_ShouldSucceed_WhenBalancesAndLedgersAreValid()
    {
        // Arrange
        using var context = CreateDbContext();
        var user1 = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Ahmed Ali", Email = "ahmed@example.com" };
        var user2 = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Mohamed Ali", Email = "mohamed@example.com" };
        context.Users.AddRange(user1, user2);

        var wallet1 = new Wallet { Id = Guid.NewGuid(), UserId = user1.Id, WalletNumber = "FW-001", Balance = 100m, Status = WalletStatus.Active };
        var wallet2 = new Wallet { Id = Guid.NewGuid(), UserId = user2.Id, WalletNumber = "FW-002", Balance = 50m, Status = WalletStatus.Active };
        context.Wallets.AddRange(wallet1, wallet2);
        await context.SaveChangesAsync();

        var handler = new SendMoneyCommandHandler(context);
        var command = new SendMoneyCommand(user1.Id, "FW-002", 30m, "LYD", "Valid transfer");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Completed");
        result.Amount.Should().Be(30m);

        // Reload contexts
        var w1 = await context.Wallets.FindAsync(wallet1.Id);
        var w2 = await context.Wallets.FindAsync(wallet2.Id);

        w1!.Balance.Should().Be(70m);
        w2!.Balance.Should().Be(80m);

        // Assert Ledger entries
        var ledgers = await context.LedgerEntries.ToListAsync();
        ledgers.Should().HaveCount(2);
        
        var debit = ledgers.First(l => l.EntryType == LedgerEntryType.Debit);
        debit.WalletId.Should().Be(wallet1.Id);
        debit.Amount.Should().Be(30m);
        debit.BalanceBefore.Should().Be(100m);
        debit.BalanceAfter.Should().Be(70m);

        var credit = ledgers.First(l => l.EntryType == LedgerEntryType.Credit);
        credit.WalletId.Should().Be(wallet2.Id);
        credit.Amount.Should().Be(30m);
        credit.BalanceBefore.Should().Be(50m);
        credit.BalanceAfter.Should().Be(80m);

        // Assert Receipt
        var receipt = await context.Receipts.FirstOrDefaultAsync(r => r.TransactionId == result.Id);
        receipt.Should().NotBeNull();
        receipt!.ReceiptNumber.Should().StartWith("REC-TRF");
    }

    [Fact]
    public async Task RequestWalletTopUpCommand_ShouldCreatePendingTransaction()
    {
        // Arrange
        using var context = CreateDbContext();
        var user = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Ahmed Ali", Email = "ahmed@example.com" };
        context.Users.Add(user);

        var wallet = new Wallet { Id = Guid.NewGuid(), UserId = user.Id, WalletNumber = "FW-001", Balance = 0m, Status = WalletStatus.Active };
        context.Wallets.Add(wallet);
        await context.SaveChangesAsync();

        var publishMock = Substitute.For<IPublishEndpoint>();
        var handler = new RequestWalletTopUpCommandHandler(context, publishMock);
        
        var command = new RequestWalletTopUpCommand(user.Id, 100m, "LYD", "Top up request");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("PendingBankApproval");
        
        var request = await context.BankTopUpRequests.FirstOrDefaultAsync(r => r.TransactionId == result.Id);
        request.Should().NotBeNull();
        request!.Status.Should().Be(BankTopUpRequestStatus.Pending);
        request.Amount.Should().Be(100m);

        // Verify RabbitMQ publish was invoked
        await publishMock.Received(1).Publish(Arg.Any<Contracts.BankTopUpRequested>(), Arg.Any<CancellationToken>());
    }
}
