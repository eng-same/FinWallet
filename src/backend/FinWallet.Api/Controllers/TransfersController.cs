using FinWallet.Application.Common.DTOs;
using FinWallet.Application.Features.Transfers.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinWallet.Api.Controllers;

[Authorize]
public sealed class TransfersController : ApiControllerBase
{
    [HttpPost("send")]
    public async Task<ActionResult<TransactionDto>> SendMoney(SendMoneyRequest request)
    {
        var command = new SendMoneyCommand(UserId, request.ToWalletNumber, request.Amount, request.Currency, request.Description);
        var result = await Mediator.Send(command);
        return Ok(result);
    }
}

public record SendMoneyRequest(string ToWalletNumber, decimal Amount, string Currency, string? Description);
