using System.Security.Claims;
using BankingAIBot.API.Contracts;
using BankingAIBot.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace BankingAIBot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AssistantController : ControllerBase
{
    private readonly IBankingAiOrchestrator _orchestrator;
    private readonly ISavedPromptService _savedPromptService;

    public AssistantController(IBankingAiOrchestrator orchestrator, ISavedPromptService savedPromptService)
    {
        _orchestrator = orchestrator;
        _savedPromptService = savedPromptService;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _orchestrator.RespondAsync(GetUserId(), request, cancellationToken= default);
            if ( response is null)
            {
                return NotFound();
            }
            return Ok(response);
        }
        catch(ArgumentException ex)
        {
            return BadRequest();
        }
        catch(Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<IReadOnlyList<ChatSessionListDto>>> ListSessions(CancellationToken cancellationToken = default)
    {
        return Ok(await _orchestrator.ListSessionsAsync(GetUserId(), cancellationToken));
    }

    [HttpGet("sessions/{sessionId:int}")]
    public async Task<ActionResult<ChatSessionDetailsDto>> GetSession([FromRoute] int sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _orchestrator.GetSessionAsync(GetUserId(), sessionId, cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpGet("prompts")]
    public async Task<ActionResult<IReadOnlyList<SavedPromptDto>>> ListPrompts(CancellationToken cancellationToken = default)
    {
        return Ok(await _savedPromptService.ListAsync(GetUserId(), cancellationToken));
    }

    [HttpPost("prompts")]
    public async Task<ActionResult<SavedPromptDto>> SavePrompt([FromBody] SavePromptRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = await _savedPromptService.SaveAsync(GetUserId(), request, cancellationToken);
            return Ok(prompt);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("User identity is missing.");
    }
}
