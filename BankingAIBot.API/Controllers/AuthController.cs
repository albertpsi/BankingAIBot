using Microsoft.AspNetCore.Mvc;
using BankingAIBot.API.Data;
using BankingAIBot.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BankingAIBot.API.Services;

namespace BankingAIBot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly BankingDbContext _context;
    private readonly IJwtTokenService _tokenService;

    public AuthController(BankingDbContext context, IJwtTokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public record LoginRequest(string Email, string Password);
    public record RegisterRequest(string Name, string Email, string Password, string ConfirmPassword);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
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

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Name, email, and password are required.");
        }

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return BadRequest("Passwords do not match.");
        }

        var existing = await _context.Users.AnyAsync(u => u.Email == email);
        if (existing)
        {
            return Conflict("An account with that email already exists.");
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            Role = "Customer",
            ConsentToAiProcessing = true,
            ConsentToAnalytics = true
        };

        var passwordHasher = new PasswordHasher<User>();
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _context.ConsentRecords.AddRange(
            new ConsentRecord
            {
                UserId = user.UserId,
                ConsentType = "AI Processing",
                Granted = true,
                Source = "Registration"
            },
            new ConsentRecord
            {
                UserId = user.UserId,
                ConsentType = "Analytics",
                Granted = true,
                Source = "Registration"
            });

        _context.Accounts.AddRange(
            new Account
            {
                UserId = user.UserId,
                AccountType = "Current",
                DisplayName = "Current",
                ExternalAccountId = $"acct_{user.UserId}_current",
                AccountStatus = "Active",
                Balance = 0m,
                AvailableBalance = 0m,
                Currency = "INR"
            },
            new Account
            {
                UserId = user.UserId,
                AccountType = "Savings",
                DisplayName = "Savings",
                ExternalAccountId = $"acct_{user.UserId}_savings",
                AccountStatus = "Active",
                Balance = 0m,
                AvailableBalance = 0m,
                Currency = "INR"
            });

        await _context.SaveChangesAsync();

        return Ok(BuildAuthPayload(user));
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
