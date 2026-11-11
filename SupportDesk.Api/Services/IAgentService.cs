using SupportDesk.Api.Models;

namespace SupportDesk.Api.Services;

public interface IAgentService
{
    Task<List<Agent>> SearchAsync(string? search, CancellationToken ct = default);
    Task<Agent?> GetByIdAsync(int id, CancellationToken ct = default);
}