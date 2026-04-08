using BankingAIBot.API.Data;
using BankingAIBot.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BankingAIBot.API.Services;

public interface IAuthService
{
    Task<User> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default);
}

public sealed class AuthService : IAuthService
{
    private readonly BankingDbContext _context;
    private readonly ILogger<AuthService> _logger;

    public AuthService(BankingDbContext context, ILogger<AuthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default)
    {
        IDbContextTransaction? transaction = null;

        try
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Name, email, and password are required.");
            }

            var existing = await _context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
            if (existing)
            {
                throw new InvalidOperationException("An account with that email already exists.");
            }

            transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var user = new User
            {
                Name = name.Trim(),
                Email = normalizedEmail,
                Role = "Customer",
                ConsentToAiProcessing = true,
                ConsentToAnalytics = true
            };

            var passwordHasher = new PasswordHasher<User>();
            user.PasswordHash = passwordHasher.HashPassword(user, password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

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

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return user;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            _logger.LogError(ex, "Failed to register user with email {Email}.", email);
            throw;
        }
    }
}
