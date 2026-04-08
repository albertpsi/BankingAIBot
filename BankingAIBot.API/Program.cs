using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using BankingAIBot.API.Data;
using BankingAIBot.API.Services;
using BankingAIBot.API.Options;
using BankingAIBot.API.Configuration;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var frontendOptions = builder.Configuration.GetSection("Cors").Get<FrontendOptions>() ?? new FrontendOptions();

builder.Services.AddCors(options =>
{
    var methods = new[] { HttpMethod.Get.Method, HttpMethod.Post.Method, HttpMethod.Patch.Method };
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(frontendOptions.AllowedOrigin)
            .AllowAnyHeader()
            .WithMethods(methods);
    });

});

builder.Services.AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection("ConnectionStrings"))
    .Validate(options => !string.IsNullOrWhiteSpace(options.DefaultConnection), "Database connection string is required.");

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "JWT issuer is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "JWT audience is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Key), "JWT signing key is required.");

builder.Services.AddOptions<OpenAiOptions>()
    .Bind(builder.Configuration.GetSection("OpenAI"));

builder.Services.AddDbContext<BankingDbContext>((sp, options) =>
{
    var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    options.UseSqlServer(dbOptions.DefaultConnection);
});

builder.Services.AddDbContextFactory<BankingDbContext>((sp, options) =>
{
    var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    options.UseSqlServer(dbOptions.DefaultConnection);
}, ServiceLifetime.Scoped);

builder.Services.AddHttpClient<OpenAiChatClient>();
builder.Services.AddScoped<IBankingInsightsService, BankingInsightsService>();
builder.Services.AddScoped<IBankingToolDataService, BankingToolDataService>();
builder.Services.AddScoped<IBankingToolExecutor, BankingToolExecutor>();
builder.Services.AddScoped<IBankingAiOrchestrator, BankingAiOrchestrator>();
builder.Services.AddScoped<ISavedPromptService, SavedPromptService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerOptionsSetup>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
    context.Database.Migrate();
    DbSeeder.Seed(context);
}

app.Run();
