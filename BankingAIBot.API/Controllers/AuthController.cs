using Microsoft.AspNetCore.Mvc;
using BankingAIBot.API.Data;
using BankingAIBot.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BankingAIBot.API.Services;

namespace BankingAIBot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly BankingDbContext _context;
    private readonly IJwtTokenService _tokenService;
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        BankingDbContext context,
        IJwtTokenService tokenService,
        IAuthService authService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _authService = authService;
        _logger = logger;
    }

    public record LoginRequest(string Email, string Password);
    public record RegisterRequest(string Name, string Email, string Password, string ConfirmPassword);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return Unauthorized("Invalid credentials.");
            }

            if (!VerifyPassword(user, request.Password))
            {
                return Unauthorized("Invalid credentials.");
            }

            return Ok(BuildAuthPayload(user));
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger);
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
            {
                return BadRequest("Passwords do not match.");
            }

            var user = await _authService.RegisterAsync(request.Name, request.Email, request.Password);

            return Ok(BuildAuthPayload(user));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration conflict for email {Email}.", request.Email);
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleException(ex, _logger);
        }
    }

    private bool VerifyPassword(User user, string password)
    {
        var passwordHasher = new PasswordHasher<User>();
        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return verification != PasswordVerificationResult.Failed;
    }

    private object BuildAuthPayload(User user)
    {
        var payload = _tokenService.CreateAuthPayload(user);
        return new
        {
            token = payload.Token,
            expiration = payload.Expiration,
            user = payload.User
        };
    }
}
