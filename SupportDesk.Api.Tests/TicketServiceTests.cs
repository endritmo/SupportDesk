using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Common;
using SupportDesk.Api.Data;
using SupportDesk.Api.Dtos;
using SupportDesk.Api.Models;
using SupportDesk.Api.Services;
using Xunit;

namespace SupportDesk.Api.Tests;

public class TicketServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<Agent> AddAgentAsync(AppDbContext db, bool active = true)
    {
        var agent = new Agent
        {
            FullName = "Test Agent",
            Email = $"{Guid.NewGuid()}@example.com",
            Department = Department.Technical,
            Active = active
        };
        db.Agents.Add(agent);
        await db.SaveChangesAsync();
        return agent;
    }

    private static CreateTicketRequest NewTicketRequest(Priority priority = Priority.Normal) =>
        new("Title", "Description", "Customer", "customer@example.com", priority);

    [Theory]
    [InlineData(Priority.Critical, 0, 4)]
    [InlineData(Priority.High, 1, 0)]
    [InlineData(Priority.Normal, 3, 0)]
    [InlineData(Priority.Low, 7, 0)]
    public async Task CreateAsync_SetsDueDate_BasedOnPriority(Priority priority, int expectedDays, int expectedHours)
    {
        var db = CreateContext();
        var service = new TicketService(db);

        var before = DateTime.UtcNow;
        var ticket = await service.CreateAsync(NewTicketRequest(priority));
        var after = DateTime.UtcNow;

        var expectedSpan = TimeSpan.FromDays(expectedDays) + TimeSpan.FromHours(expectedHours);
        Assert.InRange(ticket.DueDate,
            before + expectedSpan - TimeSpan.FromSeconds(5),
            after + expectedSpan + TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateAsync_ChangingPriority_RecalculatesDueDate_FromOriginalCreatedDate()
    {
        var db = CreateContext();
        var service = new TicketService(db);

        var created = await service.CreateAsync(NewTicketRequest(Priority.Low));
        var originalCreatedDate = created.CreatedDate;

        await Task.Delay(50);

        var updated = await service.UpdateAsync(created.Id,
            new UpdateTicketRequest("Title", "Description", "Customer", "customer@example.com", Priority.Critical));

        var expectedDueDate = originalCreatedDate + SlaCalculator.GetSla(Priority.Critical);
        Assert.Equal(expectedDueDate, updated.DueDate);
    }

    [Fact]
    public async Task ChangeStatusAsync_NewToInProgress_WithActiveAgentAssigned_Succeeds()
    {
        var db = CreateContext();
        var service = new TicketService(db);
        var agent = await AddAgentAsync(db);

        var ticket = await service.CreateAsync(NewTicketRequest());
        await service.AssignAgentAsync(ticket.Id, agent.Id);

        var result = await service.ChangeStatusAsync(ticket.Id, TicketStatus.InProgress);

        Assert.Equal(TicketStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task ChangeStatusAsync_NewDirectlyToResolved_IsRejected()
    {
        var db = CreateContext();
        var service = new TicketService(db);
        var ticket = await service.CreateAsync(NewTicketRequest());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.ChangeStatusAsync(ticket.Id, TicketStatus.Resolved));

        Assert.Equal("INVALID_STATUS_TRANSITION", ex.Code);
    }

    [Fact]
    public async Task ChangeStatusAsync_ClosedTicket_CanNeverBeReopened()
    {
        var db = CreateContext();
        var service = new TicketService(db);
        var agent = await AddAgentAsync(db);

        var ticket = await service.CreateAsync(NewTicketRequest());
        await service.AssignAgentAsync(ticket.Id, agent.Id);
        await service.ChangeStatusAsync(ticket.Id, TicketStatus.InProgress);
        await service.ChangeStatusAsync(ticket.Id, TicketStatus.Resolved);
        await service.ChangeStatusAsync(ticket.Id, TicketStatus.Closed);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.ChangeStatusAsync(ticket.Id, TicketStatus.InProgress));

        Assert.Equal("TICKET_CLOSED", ex.Code);
    }

    [Fact]
    public async Task ChangeStatusAsync_ToResolved_SetsResolvedDate_AndReopening_ClearsIt()
    {
        var db = CreateContext();
        var service = new TicketService(db);
        var agent = await AddAgentAsync(db);

        var ticket = await service.CreateAsync(NewTicketRequest());
        await service.AssignAgentAsync(ticket.Id, agent.Id);
        await service.ChangeStatusAsync(ticket.Id, TicketStatus.InProgress);

        var resolved = await service.ChangeStatusAsync(ticket.Id, TicketStatus.Resolved);
        Assert.NotNull(resolved.ResolvedDate);

        var reopened = await service.ChangeStatusAsync(ticket.Id, TicketStatus.InProgress);
        Assert.Null(reopened.ResolvedDate);
    }

    [Fact]
    public async Task ChangeStatusAsync_ToInProgress_WithoutAssignedAgent_IsRejected()
    {
        var db = CreateContext();
        var service = new TicketService(db);
        var ticket = await service.CreateAsync(NewTicketRequest());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.ChangeStatusAsync(ticket.Id, TicketStatus.InProgress));

        Assert.Equal("AGENT_REQUIRED", ex.Code);
    }

    [Fact]
    public async Task ChangeStatusAsync_ToInProgress_WithInactiveAssignedAgent_IsRejected()
    {
        var db = CreateContext();
        var service = new TicketService(db);
        var agent = await AddAgentAsync(db, active: true);

        var ticket = await service.CreateAsync(NewTicketRequest());
        await service.AssignAgentAsync(ticket.Id, agent.Id);

        agent.Active = false;
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.ChangeStatusAsync(ticket.Id, TicketStatus.InProgress));

        Assert.Equal("AGENT_INACTIVE", ex.Code);
    }

    [Fact]
    public async Task AssignAgentAsync_InactiveAgent_IsRejected()
    {
        var db = CreateContext();
        var service = new TicketService(db);
        var inactiveAgent = await AddAgentAsync(db, active: false);
        var ticket = await service.CreateAsync(NewTicketRequest());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.AssignAgentAsync(ticket.Id, inactiveAgent.Id));

        Assert.Equal("AGENT_INACTIVE", ex.Code);
    }

    [Fact]
    public async Task AddCommentAsync_OnClosedTicket_IsRejected()
    {
        var db = CreateContext();
        var service = new TicketService(db);
        var agent = await AddAgentAsync(db);

        var ticket = await service.CreateAsync(NewTicketRequest());
        await service.AssignAgentAsync(ticket.Id, agent.Id);
        await service.ChangeStatusAsync(ticket.Id, TicketStatus.InProgress);
        await service.ChangeStatusAsync(ticket.Id, TicketStatus.Resolved);
        await service.ChangeStatusAsync(ticket.Id, TicketStatus.Closed);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.AddCommentAsync(ticket.Id, new AddCommentRequest("Someone", "Comment")));

        Assert.Equal("TICKET_CLOSED", ex.Code);
    }
}
