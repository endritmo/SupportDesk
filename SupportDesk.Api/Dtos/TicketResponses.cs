using SupportDesk.Api.Models;

namespace SupportDesk.Api.Dtos;

public record TicketListItemResponse(
    int Id, string Reference, string Title, string CustomerName,
    Priority Priority, TicketStatus Status, string? AssignedAgentName,
    DateTime DueDate, bool Overdue);

public record TicketDetailResponse(
    int Id, string Reference, string Title, string Description,
    string CustomerName, string CustomerEmail,
    Priority Priority, TicketStatus Status,
    int? AssignedAgentId, string? AssignedAgentName,
    DateTime CreatedDate, DateTime LastModifiedDate,
    DateTime? ResolvedDate, DateTime? ClosedDate, DateTime DueDate, bool Overdue,
    List<CommentResponse> Comments);

public record CommentResponse(int Id, string AuthorName, string Body, DateTime CreatedDate);

public record AgentResponse(int Id, string FullName, string Email, Department Department, bool Active);

public class PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}