using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Models;
using SupportDesk.Api.Services;

namespace SupportDesk.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Tickets.AnyAsync()) return; // already seeded, skip

        var agents = new List<Agent>
        {
            new() { FullName = "Alma Krasniqi", Email = "alma.krasniqi@supportdesk.com", Department = Department.Technical, Active = true },
            new() { FullName = "Blerim Hoxha", Email = "blerim.hoxha@supportdesk.com", Department = Department.Billing, Active = true },
            new() { FullName = "Fatlum Berisha", Email = "fatlum.berisha@supportdesk.com", Department = Department.General, Active = true },
            new() { FullName = "Vjosa Gashi", Email = "vjosa.gashi@supportdesk.com", Department = Department.Technical, Active = false }, // inactive on purpose — exercises rule #4
            new() { FullName = "Driton Morina", Email = "driton.morina@supportdesk.com", Department = Department.Billing, Active = true },
        };
        context.Agents.AddRange(agents);
        await context.SaveChangesAsync(); // so agents get real Ids before tickets reference them

        var rnd = new Random(42); // fixed seed = reproducible data every time you reseed
        var priorities = Enum.GetValues<Priority>();
        var statuses = new[] { TicketStatus.New, TicketStatus.InProgress, TicketStatus.Resolved, TicketStatus.Closed };

        var tickets = new List<Ticket>();
        for (int i = 1; i <= 20; i++)
        {
            var priority = priorities[rnd.Next(priorities.Length)];
            var status = statuses[rnd.Next(statuses.Length)];

            // Spread creation dates into the past so several tickets are
            // already overdue no matter when this seed actually runs.
            var createdDate = DateTime.UtcNow.AddDays(-rnd.Next(1, 20));
            var dueDate = createdDate + SlaCalculator.GetSla(priority);

            tickets.Add(new Ticket
            {
                Reference = $"TCK-{createdDate.Year}-{i:D4}",
                Title = $"Sample issue #{i}",
                Description = "Seeded ticket for local development/testing.",
                CustomerName = $"Customer {i}",
                CustomerEmail = $"customer{i}@example.com",
                Priority = priority,
                Status = status,
                CreatedDate = createdDate,
                LastModifiedDate = createdDate,
                DueDate = dueDate,
                AssignedAgentId = status == TicketStatus.New ? null : agents[rnd.Next(agents.Count)].Id,
                ResolvedDate = status is TicketStatus.Resolved or TicketStatus.Closed ? createdDate.AddDays(1) : null,
                ClosedDate = status == TicketStatus.Closed ? createdDate.AddDays(2) : null,
            });
        }

        context.Tickets.AddRange(tickets);
        await context.SaveChangesAsync();
    }
}