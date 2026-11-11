using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Data;
using SupportDesk.Api.Models;

namespace SupportDesk.Api.Services;

public class AgentService : IAgentService
{
    private readonly AppDbContext _db;

    public AgentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Agent>> SearchAsync(string? search, CancellationToken ct = default)
    {
        var query = _db.Agents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a => EF.Functions.ILike(a.FullName, $"%{term}%") || EF.Functions.ILike(a.Email, $"%{term}%"));
        }

        return await query.OrderBy(a => a.FullName).ToListAsync(ct);
    }

    public async Task<Agent?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await _db.Agents.FindAsync(new object[] { id }, ct);
}