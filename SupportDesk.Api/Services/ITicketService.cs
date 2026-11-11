using SupportDesk.Api.Dtos;
using SupportDesk.Api.Models;

namespace SupportDesk.Api.Services;

public interface ITicketService
{
    Task<Ticket> CreateAsync(CreateTicketRequest request, CancellationToken ct = default);
    Task<Ticket?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Ticket> UpdateAsync(int id, UpdateTicketRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<Ticket> ChangeStatusAsync(int id, TicketStatus newStatus, CancellationToken ct = default);
    Task<Ticket> AssignAgentAsync(int ticketId, int agentId, CancellationToken ct = default);
    Task<Ticket> UnassignAgentAsync(int ticketId, CancellationToken ct = default);
    Task<Comment> AddCommentAsync(int ticketId, AddCommentRequest request, CancellationToken ct = default);
    Task<(List<Ticket> Items, int TotalCount)> ListAsync(TicketListQuery query, CancellationToken ct = default);
}