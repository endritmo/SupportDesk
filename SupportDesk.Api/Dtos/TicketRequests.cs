using SupportDesk.Api.Models;

namespace SupportDesk.Api.Dtos;

public record CreateTicketRequest(
    string Title,
    string Description,
    string CustomerName,
    string CustomerEmail,
    Priority Priority);

public record UpdateTicketRequest(
    string Title,
    string Description,
    string CustomerName,
    string CustomerEmail,
    Priority Priority);

public record AddCommentRequest(string AuthorName, string Body);

public record ChangeStatusRequest(TicketStatus Status);
public record AssignAgentRequest(int AgentId);

public record TicketListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public TicketStatus? Status { get; init; }
    public Priority? Priority { get; init; }
    public int? AssignedAgentId { get; init; }
    public bool OverdueOnly { get; init; } = false;
}