using SupportDesk.Api.Models;
using SupportDesk.Api.Services;

namespace SupportDesk.Api.Dtos;

public static class MappingExtensions
{
    public static TicketListItemResponse ToListItemResponse(this Ticket t) => new(
        t.Id, t.Reference, t.Title, t.CustomerName, t.Priority, t.Status,
        t.AssignedAgent?.FullName, t.DueDate, TicketService.IsOverdue(t));

    public static TicketDetailResponse ToDetailResponse(this Ticket t) => new(
        t.Id, t.Reference, t.Title, t.Description, t.CustomerName, t.CustomerEmail,
        t.Priority, t.Status,
        t.AssignedAgentId, t.AssignedAgent?.FullName,
        t.CreatedDate, t.LastModifiedDate, t.ResolvedDate, t.ClosedDate, t.DueDate,
        TicketService.IsOverdue(t),
        t.Comments.OrderBy(c => c.CreatedDate).Select(c => c.ToResponse()).ToList());

    public static CommentResponse ToResponse(this Comment c) => new(c.Id, c.AuthorName, c.Body, c.CreatedDate);

    public static AgentResponse ToResponse(this Agent a) => new(a.Id, a.FullName, a.Email, a.Department, a.Active);
}