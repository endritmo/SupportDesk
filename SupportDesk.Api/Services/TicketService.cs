using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Common;
using SupportDesk.Api.Data;
using SupportDesk.Api.Dtos;
using SupportDesk.Api.Models;

namespace SupportDesk.Api.Services;

public class TicketService : ITicketService
{
    private readonly AppDbContext _db;

    public TicketService(AppDbContext db)
    {
        _db = db;
    }

    // Rule 2: the only transitions the PDF allows. Closed maps to an empty
    // array on purpose — it enforces "a Closed ticket can never be reopened"
    // for free, without a special case.
    private static readonly Dictionary<TicketStatus, TicketStatus[]> AllowedTransitions = new()
    {
        [TicketStatus.New] = new[] { TicketStatus.InProgress },
        [TicketStatus.InProgress] = new[] { TicketStatus.Resolved },
        [TicketStatus.Resolved] = new[] { TicketStatus.Closed, TicketStatus.InProgress },
        [TicketStatus.Closed] = Array.Empty<TicketStatus>()
    };

    public async Task<Ticket> CreateAsync(CreateTicketRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var reference = await GenerateReferenceAsync(now.Year, ct);

        var ticket = new Ticket
        {
            Reference = reference,
            Title = request.Title,
            Description = request.Description,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            Priority = request.Priority,
            Status = TicketStatus.New,        // data model: new tickets always start here
            CreatedDate = now,
            LastModifiedDate = now,
            DueDate = now + SlaCalculator.GetSla(request.Priority), // Rule 1
            AssignedAgentId = null              // data model: starts unassigned
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(ct);
        return ticket;
    }

    public async Task<(List<Ticket> Items, int TotalCount)> ListAsync(TicketListQuery query, CancellationToken ct = default)
    {
        var q = _db.Tickets.Include(t => t.AssignedAgent).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(t => EF.Functions.ILike(t.Reference, $"%{term}%")
                        || EF.Functions.ILike(t.Title, $"%{term}%")
                        || EF.Functions.ILike(t.CustomerName, $"%{term}%"));
        }

        if (query.Status.HasValue) q = q.Where(t => t.Status == query.Status.Value);
        if (query.Priority.HasValue) q = q.Where(t => t.Priority == query.Priority.Value);
        if (query.AssignedAgentId.HasValue) q = q.Where(t => t.AssignedAgentId == query.AssignedAgentId.Value);

        if (query.OverdueOnly)
        {
            var now = DateTime.UtcNow;
            q = q.Where(t => t.DueDate < now && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed);
        }

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(t => t.CreatedDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Ticket?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Tickets
            .Include(t => t.Comments)
            .Include(t => t.AssignedAgent)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<Ticket> UpdateAsync(int id, UpdateTicketRequest request, CancellationToken ct = default)
    {
        var ticket = await GetTicketOrThrowAsync(id, ct);
        EnsureNotClosed(ticket); // Rule 5: closed = read-only, no edits

        ticket.Title = request.Title;
        ticket.Description = request.Description;
        ticket.CustomerName = request.CustomerName;
        ticket.CustomerEmail = request.CustomerEmail;

        if (request.Priority != ticket.Priority)
        {
            ticket.Priority = request.Priority;
            // Rule 1: recalculated from the ORIGINAL creation date, not from now
            ticket.DueDate = ticket.CreatedDate + SlaCalculator.GetSla(request.Priority);
        }

        ticket.LastModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ticket;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var ticket = await GetTicketOrThrowAsync(id, ct);
        _db.Tickets.Remove(ticket);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Ticket> ChangeStatusAsync(int id, TicketStatus newStatus, CancellationToken ct = default)
    {
        var ticket = await GetTicketOrThrowAsync(id, ct);
        EnsureNotClosed(ticket); // Rule 5 — gives a clearer message than falling through to "invalid transition"

        if (!AllowedTransitions.TryGetValue(ticket.Status, out var allowed) || !allowed.Contains(newStatus))
        {
            var allowedList = allowed?.Length > 0 ? string.Join(", ", allowed) : "none";
            throw new BusinessRuleException(
                "INVALID_STATUS_TRANSITION",
                $"Cannot change status from '{ticket.Status}' to '{newStatus}'. Allowed from '{ticket.Status}': {allowedList}.");
        }

        if (newStatus == TicketStatus.InProgress)
        {
            // Rule 3 — applies to New -> InProgress AND the Resolved -> InProgress reopen path
            if (ticket.AssignedAgentId is null)
                throw new BusinessRuleException("AGENT_REQUIRED", "An agent must be assigned before moving a ticket to In Progress.");

            var agent = await _db.Agents.FindAsync(new object[] { ticket.AssignedAgentId.Value }, ct);
            if (agent is null || !agent.Active)
                throw new BusinessRuleException("AGENT_INACTIVE", "The assigned agent must be active for the ticket to move to In Progress.");
        }

        var now = DateTime.UtcNow;
        ticket.Status = newStatus;
        ticket.LastModifiedDate = now;

        // Rule 6: system sets these, never the client
        if (newStatus == TicketStatus.Resolved)
        {
            ticket.ResolvedDate = now;
        }
        else if (newStatus == TicketStatus.Closed)
        {
            ticket.ClosedDate = now;
        }
        else if (newStatus == TicketStatus.InProgress && ticket.ResolvedDate is not null)
        {
            // Reopening: it's no longer "resolved", so clear the resolved timestamp.
            // Not explicitly stated in the PDF — worth listing as an assumption in the README.
            ticket.ResolvedDate = null;
        }

        await _db.SaveChangesAsync(ct);
        return ticket;
    }

    public async Task<Ticket> AssignAgentAsync(int ticketId, int agentId, CancellationToken ct = default)
    {
        var ticket = await GetTicketOrThrowAsync(ticketId, ct);
        EnsureNotClosed(ticket); // Rule 5

        var agent = await _db.Agents.FindAsync(new object[] { agentId }, ct)
            ?? throw new NotFoundException($"Agent {agentId} not found.");

        if (!agent.Active) // Rule 4
            throw new BusinessRuleException("AGENT_INACTIVE", "Inactive agents cannot be assigned to tickets.");

        ticket.AssignedAgentId = agentId;
        ticket.LastModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ticket;
    }

    public async Task<Ticket> UnassignAgentAsync(int ticketId, CancellationToken ct = default)
    {
        var ticket = await GetTicketOrThrowAsync(ticketId, ct);
        EnsureNotClosed(ticket); // Rule 5

        ticket.AssignedAgentId = null;
        ticket.LastModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ticket;
    }

    public async Task<Comment> AddCommentAsync(int ticketId, AddCommentRequest request, CancellationToken ct = default)
    {
        var ticket = await GetTicketOrThrowAsync(ticketId, ct);
        EnsureNotClosed(ticket); // Rule 5: no new comments on a closed ticket

        var comment = new Comment
        {
            TicketId = ticketId,
            AuthorName = request.AuthorName,
            Body = request.Body,
            CreatedDate = DateTime.UtcNow
        };

        _db.Comments.Add(comment);
        ticket.LastModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return comment;
    }

    // Rule 7 — used later (Chapter 4) for the overdue filter and in response DTOs.
    public static bool IsOverdue(Ticket ticket) =>
        ticket.DueDate < DateTime.UtcNow
        && ticket.Status != TicketStatus.Resolved
        && ticket.Status != TicketStatus.Closed;

    private async Task<Ticket> GetTicketOrThrowAsync(int id, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        return ticket ?? throw new NotFoundException($"Ticket {id} not found.");
    }

    private static void EnsureNotClosed(Ticket ticket)
    {
        if (ticket.Status == TicketStatus.Closed)
            throw new BusinessRuleException("TICKET_CLOSED", "This ticket is closed and is read-only.");
    }

    private async Task<string> GenerateReferenceAsync(int year, CancellationToken ct)
    {
        var prefix = $"TCK-{year}-";
        var lastRef = await _db.Tickets
            .Where(t => t.Reference.StartsWith(prefix))
            .OrderByDescending(t => t.Reference)
            .Select(t => t.Reference)
            .FirstOrDefaultAsync(ct);

        var nextSeq = 1;
        if (lastRef is not null && int.TryParse(lastRef[prefix.Length..], out var parsed))
        {
            nextSeq = parsed + 1;
        }

        return $"{prefix}{nextSeq:D4}";
        // Known limitation worth a line in the README: under concurrent requests,
        // two creates could read the same "last" reference before either saves,
        // producing a duplicate. A DB sequence or serializable transaction would
        // close that gap in production; out of scope for this assignment.
    }
}