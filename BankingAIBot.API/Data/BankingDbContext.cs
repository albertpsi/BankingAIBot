using Microsoft.EntityFrameworkCore;
using BankingAIBot.API.Models;

namespace BankingAIBot.API.Data;

public class BankingDbContext : DbContext
{
    public BankingDbContext(DbContextOptions<BankingDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<SpendingSummary> SpendingSummaries { get; set; }
    public DbSet<SavingsSuggestion> SavingsSuggestions { get; set; }
    public DbSet<ConsentRecord> ConsentRecords { get; set; }
    public DbSet<ModelInvocationLog> ModelInvocationLogs { get; set; }
    public DbSet<SavedPrompt> SavedPrompts { get; set; }
    public DbSet<AccountTypeData> AccountTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Primary keys
        modelBuilder.Entity<User>().HasKey(u => u.UserId);
        modelBuilder.Entity<Account>().HasKey(a => a.AccountId);
        modelBuilder.Entity<Transaction>().HasKey(t => t.TransactionId);
        modelBuilder.Entity<ChatSession>().HasKey(s => s.SessionId);
        modelBuilder.Entity<ChatMessage>().HasKey(m => m.MessageId);
        modelBuilder.Entity<SpendingSummary>().HasKey(s => s.SpendingSummaryId);
        modelBuilder.Entity<SavingsSuggestion>().HasKey(s => s.SavingsSuggestionId);
        modelBuilder.Entity<ConsentRecord>().HasKey(c => c.ConsentRecordId);
        modelBuilder.Entity<ModelInvocationLog>().HasKey(l => l.ModelInvocationLogId);
        modelBuilder.Entity<SavedPrompt>().HasKey(p => p.SavedPromptId);
        modelBuilder.Entity<AccountTypeData>().HasKey(a => a.Id);

        // Core schema mapping
        modelBuilder.Entity<User>().ToTable("Users", schema: "core");
        modelBuilder.Entity<Account>().ToTable("Accounts", schema: "core");
        modelBuilder.Entity<Transaction>().ToTable("Transactions", schema: "core");
        modelBuilder.Entity<AccountTypeData>().ToTable("AccountTypes", schema: "core");

        // Chat schema mapping
        modelBuilder.Entity<ChatSession>().ToTable("ChatSessions", schema: "chat");
        modelBuilder.Entity<ChatMessage>().ToTable("ChatMessages", schema: "chat");

        // Analytics schema mapping
        modelBuilder.Entity<SpendingSummary>().ToTable("SpendingSummaries", schema: "analytics");
        modelBuilder.Entity<SavingsSuggestion>().ToTable("SavingsSuggestions", schema: "analytics");

        // Audit schema mapping
        modelBuilder.Entity<ConsentRecord>().ToTable("ConsentRecords", schema: "audit");
        modelBuilder.Entity<ModelInvocationLog>().ToTable("ModelInvocationLogs", schema: "audit");
        modelBuilder.Entity<SavedPrompt>().ToTable("SavedPrompts", schema: "chat");

        // Configuring precision for financial data
        modelBuilder.Entity<Account>()
            .Property(a => a.Balance)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Account>()
            .Property(a => a.AvailableBalance)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Transaction>()
            .Property(t => t.BalanceAfter)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SpendingSummary>()
            .Property(s => s.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SavingsSuggestion>()
            .Property(s => s.EstimatedMonthlySavings)
            .HasPrecision(18, 2);

        // Unique / lookup indexes
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.ExternalTransactionId);

        // Configuring relationships
        modelBuilder.Entity<User>()
            .HasMany(u => u.Accounts)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId);
            
        modelBuilder.Entity<Account>()
            .HasMany(a => a.Transactions)
            .WithOne(t => t.Account)
            .HasForeignKey(t => t.AccountId);
            
        modelBuilder.Entity<User>()
            .HasMany(u => u.ChatSessions)
            .WithOne(c => c.User)
            .HasForeignKey(c => c.UserId);
            
        modelBuilder.Entity<ChatSession>()
            .HasMany(s => s.Messages)
            .WithOne(m => m.Session)
            .HasForeignKey(m => m.SessionId);

        modelBuilder.Entity<User>()
            .HasMany(u => u.ConsentRecords)
            .WithOne(c => c.User)
            .HasForeignKey(c => c.UserId);

        modelBuilder.Entity<User>()
            .HasMany(u => u.SpendingSummaries)
            .WithOne(s => s.User)
            .HasForeignKey(s => s.UserId);

        modelBuilder.Entity<User>()
            .HasMany(u => u.SavingsSuggestions)
            .WithOne(s => s.User)
            .HasForeignKey(s => s.UserId);

        modelBuilder.Entity<User>()
            .HasMany(u => u.SavedPrompts)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(u => u.ModelInvocationLogs)
            .WithOne(l => l.User)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ChatSession>()
            .HasMany(s => s.ModelInvocationLogs)
            .WithOne(l => l.Session)
            .HasForeignKey(l => l.SessionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Helpful lengths for text columns used in filtering or display
        modelBuilder.Entity<User>().Property(u => u.Name).HasMaxLength(200);
        modelBuilder.Entity<User>().Property(u => u.Email).HasMaxLength(320);
        modelBuilder.Entity<User>().Property(u => u.Role).HasMaxLength(50);

        modelBuilder.Entity<Account>().Property(a => a.AccountType).HasMaxLength(50);
        modelBuilder.Entity<Account>().Property(a => a.DisplayName).HasMaxLength(100);
        modelBuilder.Entity<Account>().Property(a => a.ExternalAccountId).HasMaxLength(100);
        modelBuilder.Entity<Account>().Property(a => a.AccountStatus).HasMaxLength(50);
        modelBuilder.Entity<Account>().Property(a => a.Currency).HasMaxLength(3);

        modelBuilder.Entity<AccountTypeData>().Property(a => a.Name).HasMaxLength(50);

        modelBuilder.Entity<Transaction>().Property(t => t.ExternalTransactionId).HasMaxLength(100);
        modelBuilder.Entity<Transaction>().Property(t => t.TransactionType).HasMaxLength(20);
        modelBuilder.Entity<Transaction>().Property(t => t.Category).HasMaxLength(100);
        modelBuilder.Entity<Transaction>().Property(t => t.Description).HasMaxLength(500);
        modelBuilder.Entity<Transaction>().Property(t => t.MerchantName).HasMaxLength(200);

        modelBuilder.Entity<ChatSession>().Property(s => s.Status).HasMaxLength(30);
        modelBuilder.Entity<ChatSession>().Property(s => s.ModelName).HasMaxLength(100);
        modelBuilder.Entity<ChatSession>().Property(s => s.TitleSummary).HasMaxLength(250);
        modelBuilder.Entity<ChatSession>().Property(s => s.ContextSummary).HasMaxLength(1000);

        modelBuilder.Entity<ChatMessage>().Property(m => m.Role).HasMaxLength(20);
        modelBuilder.Entity<ChatMessage>().Property(m => m.ToolName).HasMaxLength(100);
        modelBuilder.Entity<ChatMessage>().Property(m => m.ToolCallId).HasMaxLength(100);
        modelBuilder.Entity<ChatMessage>().Property(m => m.ModelName).HasMaxLength(100);
        modelBuilder.Entity<ChatMessage>().Property(m => m.PromptVersion).HasMaxLength(50);

        modelBuilder.Entity<SpendingSummary>().Property(s => s.Category).HasMaxLength(100);
        modelBuilder.Entity<SavingsSuggestion>().Property(s => s.Title).HasMaxLength(200);
        modelBuilder.Entity<SavingsSuggestion>().Property(s => s.Priority).HasMaxLength(20);
        modelBuilder.Entity<SavingsSuggestion>().Property(s => s.Status).HasMaxLength(30);

        modelBuilder.Entity<ConsentRecord>().Property(c => c.ConsentType).HasMaxLength(100);
        modelBuilder.Entity<ConsentRecord>().Property(c => c.Source).HasMaxLength(100);

        modelBuilder.Entity<ModelInvocationLog>().Property(l => l.Endpoint).HasMaxLength(200);
        modelBuilder.Entity<ModelInvocationLog>().Property(l => l.ModelName).HasMaxLength(100);
        modelBuilder.Entity<ModelInvocationLog>().Property(l => l.PromptVersion).HasMaxLength(50);

        modelBuilder.Entity<SavedPrompt>().Property(p => p.Title).HasMaxLength(100);
        modelBuilder.Entity<SavedPrompt>().Property(p => p.PromptText).HasMaxLength(1000);
    }
}
