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
public class AssistantController : ApiControllerBase
{
    private readonly IBankingAiOrchestrator _orchestrator;
    private readonly ISavedPromptService _savedPromptService;
    private readonly ILogger<AssistantController> _logger;

    public AssistantController(
        IBankingAiOrchestrator orchestrator,
        ISavedPromptService savedPromptService,
        ILogger<AssistantController> logger)
    {
        _orchestrator = orchestrator;
        _savedPromptService = savedPromptService;
        _logger = logger;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _orchestrator.RespondAsync(GetUserId(), request, cancellationToken= default);
            if (response is null)
            {
                return NotFound();
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            return HandleException<ChatResponse>(ex, _logger);
        }
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<IReadOnlyList<ChatSessionListDto>>> ListSessions(CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _orchestrator.ListSessionsAsync(GetUserId(), cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException<IReadOnlyList<ChatSessionListDto>>(ex, _logger);
        }
    }

    [HttpGet("sessions/{sessionId:int}")]
    public async Task<ActionResult<ChatSessionDetailsDto>> GetSession([FromRoute] int sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _orchestrator.GetSessionAsync(GetUserId(), sessionId, cancellationToken);
            return session is null ? NotFound() : Ok(session);
        }
        catch (Exception ex)
        {
            return HandleException<ChatSessionDetailsDto>(ex, _logger);
        }
    }

    [HttpGet("prompts")]
    public async Task<ActionResult<IReadOnlyList<SavedPromptDto>>> ListPrompts(CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _savedPromptService.ListAsync(GetUserId(), cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException<IReadOnlyList<SavedPromptDto>>(ex, _logger);
        }
    }

    [HttpPost("prompts")]
    public async Task<ActionResult<SavedPromptDto>> SavePrompt([FromBody] SavePromptRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = await _savedPromptService.SaveAsync(GetUserId(), request, cancellationToken);
            return Ok(prompt);
        }
        catch (Exception ex)
        {
            return HandleException<SavedPromptDto>(ex, _logger);
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
