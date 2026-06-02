using FinWallet.Application.Common.DTOs;
using FinWallet.Application.Features.Admin.Commands;
using FinWallet.Application.Features.Admin.Queries;
using FinWallet.Application.Features.Transactions.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinWallet.Api.Controllers;

[Authorize(Roles = "Admin")]
[Route("api/admin")]
public sealed class AdminController : ApiControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardDto>> GetDashboard()
    {
        var result = await Mediator.Send(new GetAdminDashboardQuery());
        return Ok(result);
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<UserDto>>> GetUsers([FromQuery] string? search = null)
    {
        var result = await Mediator.Send(new GetAdminUsersQuery(search));
        return Ok(result);
    }

    [HttpGet("users/{userId}")]
    public async Task<ActionResult<AdminUserDetailsDto>> GetUserDetails(Guid userId)
    {
        var result = await Mediator.Send(new GetAdminUserDetailsQuery(userId));
        return Ok(result);
    }

    [HttpGet("wallets")]
    public async Task<ActionResult<List<WalletDto>>> GetWallets([FromQuery] string? search = null)
    {
        var result = await Mediator.Send(new GetAdminWalletsQuery(search));
        return Ok(result);
    }

    [HttpGet("wallets/{walletId}")]
    public async Task<ActionResult<AdminWalletDetailsDto>> GetWalletDetails(Guid walletId)
    {
        var result = await Mediator.Send(new GetAdminWalletDetailsQuery(walletId));
        return Ok(result);
    }

    [HttpPut("wallets/{walletId}/freeze")]
    public async Task<ActionResult<WalletDto>> FreezeWallet(Guid walletId, [FromBody] AdminFreezeRequest request)
    {
        var command = new FreezeWalletCommand(walletId, request.Reason, UserId);
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPut("wallets/{walletId}/unfreeze")]
    public async Task<ActionResult<WalletDto>> UnfreezeWallet(Guid walletId, [FromBody] AdminUnfreezeRequest request)
    {
        var command = new UnfreezeWalletCommand(walletId, request.Reason, UserId);
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<List<TransactionDto>>> GetTransactions(
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        var result = await Mediator.Send(new GetAdminTransactionsQuery(type, status, search));
        return Ok(result);
    }

    [HttpGet("transactions/{transactionId}")]
    public async Task<ActionResult<TransactionDetailsDto>> GetTransactionDetails(Guid transactionId)
    {
        // Reuses standard GetTransactionDetailsQuery query with admin bypass enabled in the handler
        var result = await Mediator.Send(new GetTransactionDetailsQuery(transactionId, UserId));
        return Ok(result);
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<List<AuditLogDto>>> GetAuditLogs([FromQuery] string? search = null)
    {
        var result = await Mediator.Send(new GetAdminAuditLogsQuery(search));
        return Ok(result);
    }
}

public record AdminFreezeRequest(string Reason);
public record AdminUnfreezeRequest(string Reason);
