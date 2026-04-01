using BankingAIBot.API.Contracts;
using BankingAIBot.API.Data;
using BankingAIBot.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BankingAIBot.API.Services;

public interface ISavedPromptService
{
    Task<IReadOnlyList<SavedPromptDto>> ListAsync(int userId, CancellationToken cancellationToken = default);
    Task<SavedPromptDto> SaveAsync(int userId, SavePromptRequest request, CancellationToken cancellationToken = default);
}

public sealed class SavedPromptService : ISavedPromptService
{
    private readonly BankingDbContext _context;

    public SavedPromptService(BankingDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SavedPromptDto>> ListAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.SavedPrompts
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.IsPinned)
            .ThenByDescending(p => p.UpdatedAt)
            .Select(p => new SavedPromptDto(
                p.SavedPromptId,
                p.Title,
                p.PromptText,
                p.UsageCount,
                p.IsPinned,
                p.CreatedAt,
                p.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<SavedPromptDto> SaveAsync(int userId, SavePromptRequest request, CancellationToken cancellationToken = default)
    {
        var title = string.IsNullOrWhiteSpace(request.Title) ? "Saved prompt" : request.Title.Trim();
        var promptText = request.PromptText.Trim();
        if (string.IsNullOrWhiteSpace(promptText))
        {
            throw new ArgumentException("Prompt text is required.", nameof(request));
        }

        var existing = await _context.SavedPrompts
            .FirstOrDefaultAsync(p => p.UserId == userId && p.PromptText == promptText, cancellationToken);

        if (existing is not null)
        {
            existing.Title = title;
            existing.IsPinned = request.IsPinned;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UsageCount += 1;
            await _context.SaveChangesAsync(cancellationToken);
            return Map(existing);
        }

        var prompt = new SavedPrompt
        {
            UserId = userId,
            Title = title,
            PromptText = promptText,
            IsPinned = request.IsPinned,
            UsageCount = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SavedPrompts.Add(prompt);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(prompt);
    }

    private static SavedPromptDto Map(SavedPrompt prompt)
        => new(
            prompt.SavedPromptId,
            prompt.Title,
            prompt.PromptText,
            prompt.UsageCount,
            prompt.IsPinned,
            prompt.CreatedAt,
            prompt.UpdatedAt);
}
