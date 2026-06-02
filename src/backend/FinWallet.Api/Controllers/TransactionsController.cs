using FinWallet.Application.Common.DTOs;
using FinWallet.Application.Features.Transactions.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinWallet.Api.Controllers;

[Authorize]
public sealed class TransactionsController : ApiControllerBase
{
    [HttpGet("my-transactions")]
    public async Task<ActionResult<PagedList<TransactionDto>>> GetMyTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTimeOffset? dateFrom = null,
        [FromQuery] DateTimeOffset? dateTo = null,
        [FromQuery] string? search = null)
    {
        var query = new GetMyTransactionsQuery(UserId, page, pageSize, type, status, dateFrom, dateTo, search);
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("my-recent")]
    public async Task<ActionResult<List<TransactionDto>>> GetMyRecent([FromQuery] int count = 5)
    {
        var result = await Mediator.Send(new GetMyRecentTransactionsQuery(UserId, count));
        return Ok(result);
    }

    [HttpGet("{transactionId}")]
    public async Task<ActionResult<TransactionDetailsDto>> GetDetails(Guid transactionId)
    {
        var result = await Mediator.Send(new GetTransactionDetailsQuery(transactionId, UserId));
        return Ok(result);
    }
}
