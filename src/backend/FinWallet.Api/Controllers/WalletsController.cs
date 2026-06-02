using FinWallet.Application.Common.DTOs;
using FinWallet.Application.Features.Wallets.Commands;
using FinWallet.Application.Features.Wallets.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinWallet.Api.Controllers;

[Authorize]
public sealed class WalletsController : ApiControllerBase
{
    [HttpGet("my-wallet")]
    public async Task<ActionResult<WalletDto>> GetMyWallet()
    {
        var result = await Mediator.Send(new GetMyWalletQuery(UserId));
        return Ok(result);
    }

    [HttpGet("lookup/{walletNumber}")]
    public async Task<ActionResult<LookupWalletResult>> LookupWallet(string walletNumber)
    {
        var result = await Mediator.Send(new LookupWalletQuery(walletNumber));
        return Ok(result);
    }

    [HttpPost("top-up")]
    public async Task<ActionResult<TransactionDto>> TopUp(TopUpRequest request)
    {
        var command = new RequestWalletTopUpCommand(UserId, request.Amount, request.Currency, request.Description);
        var result = await Mediator.Send(command);
        return Ok(result);
    }
}

public record TopUpRequest(decimal Amount, string Currency, string? Description);
