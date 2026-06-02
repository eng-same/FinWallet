using FinWallet.Application.Common.DTOs;
using FinWallet.Application.Features.Receipts.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinWallet.Api.Controllers;

[Authorize]
public sealed class ReceiptsController : ApiControllerBase
{
    [HttpGet("by-transaction/{transactionId}")]
    public async Task<ActionResult<ReceiptDto>> GetByTransaction(Guid transactionId)
    {
        var result = await Mediator.Send(new GetReceiptByTransactionQuery(transactionId, UserId));
        return Ok(result);
    }
}
