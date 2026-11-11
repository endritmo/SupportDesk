using Microsoft.AspNetCore.Mvc;
using SupportDesk.Api.Dtos;
using SupportDesk.Api.Services;

namespace SupportDesk.Api.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TicketListItemResponse>>> List([FromQuery] TicketListQuery query, CancellationToken ct)
    {
        var (items, totalCount) = await _ticketService.ListAsync(query, ct);
        return Ok(new PagedResult<TicketListItemResponse>
        {
            Items = items.Select(t => t.ToListItemResponse()).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDetailResponse>> GetById(int id, CancellationToken ct)
    {
        var ticket = await _ticketService.GetByIdAsync(id, ct);
        return ticket is null ? NotFound() : Ok(ticket.ToDetailResponse());
    }

    [HttpPost]
    public async Task<ActionResult<TicketDetailResponse>> Create([FromBody] CreateTicketRequest request, CancellationToken ct)
    {
        var created = await _ticketService.CreateAsync(request, ct);
        var full = await _ticketService.GetByIdAsync(created.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, full!.ToDetailResponse());
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TicketDetailResponse>> Update(int id, [FromBody] UpdateTicketRequest request, CancellationToken ct)
    {
        var ticket = await _ticketService.UpdateAsync(id, request, ct);
        return Ok(ticket.ToDetailResponse());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _ticketService.DeleteAsync(id, ct);
        return NoContent();
    }

    // Dedicated status endpoint, not a generic PUT — see README for why.
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<TicketDetailResponse>> ChangeStatus(int id, [FromBody] ChangeStatusRequest request, CancellationToken ct)
    {
        var ticket = await _ticketService.ChangeStatusAsync(id, request.Status, ct);
        return Ok(ticket.ToDetailResponse());
    }

    [HttpPost("{id:int}/assign")]
    public async Task<ActionResult<TicketDetailResponse>> AssignAgent(int id, [FromBody] AssignAgentRequest request, CancellationToken ct)
    {
        var ticket = await _ticketService.AssignAgentAsync(id, request.AgentId, ct);
        return Ok(ticket.ToDetailResponse());
    }

    [HttpDelete("{id:int}/assign")]
    public async Task<ActionResult<TicketDetailResponse>> UnassignAgent(int id, CancellationToken ct)
    {
        var ticket = await _ticketService.UnassignAgentAsync(id, ct);
        return Ok(ticket.ToDetailResponse());
    }

    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<CommentResponse>> AddComment(int id, [FromBody] AddCommentRequest request, CancellationToken ct)
    {
        var comment = await _ticketService.AddCommentAsync(id, request, ct);
        return Ok(comment.ToResponse());
    }
}