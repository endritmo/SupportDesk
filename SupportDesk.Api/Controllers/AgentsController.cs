using Microsoft.AspNetCore.Mvc;
using SupportDesk.Api.Dtos;
using SupportDesk.Api.Services;

namespace SupportDesk.Api.Controllers;

[ApiController]
[Route("api/agents")]
public class AgentsController : ControllerBase
{
    private readonly IAgentService _agentService;

    public AgentsController(IAgentService agentService)
    {
        _agentService = agentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AgentResponse>>> List([FromQuery] string? search, CancellationToken ct)
    {
        var agents = await _agentService.SearchAsync(search, ct);
        return Ok(agents.Select(a => a.ToResponse()).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AgentResponse>> GetById(int id, CancellationToken ct)
    {
        var agent = await _agentService.GetByIdAsync(id, ct);
        return agent is null ? NotFound() : Ok(agent.ToResponse());
    }
}