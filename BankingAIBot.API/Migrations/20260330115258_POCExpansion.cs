using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingAIBot.API.Migrations
{
    /// <inheritdoc />
    public partial class POCExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "analytics");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "core",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "core",
                table: "Users",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "ConsentToAiProcessing",
                schema: "core",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ConsentToAnalytics",
                schema: "core",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                schema: "core",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionType",
                schema: "core",
                table: "Transactions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "MerchantName",
                schema: "core",
                table: "Transactions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                schema: "core",
                table: "Transactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<decimal>(
                name: "BalanceAfter",
                schema: "core",
                table: "Transactions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "core",
                table: "Transactions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExternalTransactionId",
                schema: "core",
                table: "Transactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPending",
                schema: "core",
                table: "Transactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostedAt",
                schema: "core",
                table: "Transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TitleSummary",
                schema: "chat",
                table: "ChatSessions",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ContextSummary",
                schema: "chat",
                table: "ChatSessions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMessageAt",
                schema: "chat",
                table: "ChatSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                schema: "chat",
                table: "ChatSessions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "chat",
                table: "ChatSessions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                schema: "chat",
                table: "ChatMessages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "CompletionTokens",
                schema: "chat",
                table: "ChatMessages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                schema: "chat",
                table: "ChatMessages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromptTokens",
                schema: "chat",
                table: "ChatMessages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptVersion",
                schema: "chat",
                table: "ChatMessages",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToolArgumentsJson",
                schema: "chat",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToolCallId",
                schema: "chat",
                table: "ChatMessages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToolName",
                schema: "chat",
                table: "ChatMessages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToolResultJson",
                schema: "chat",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalTokens",
                schema: "chat",
                table: "ChatMessages",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                schema: "core",
                table: "Accounts",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "AccountType",
                schema: "core",
                table: "Accounts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AccountStatus",
                schema: "core",
                table: "Accounts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "AvailableBalance",
                schema: "core",
                table: "Accounts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                schema: "core",
                table: "Accounts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExternalAccountId",
                schema: "core",
                table: "Accounts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "core",
                table: "Accounts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "ConsentRecords",
                schema: "audit",
                columns: table => new
                {
                    ConsentRecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ConsentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Granted = table.Column<bool>(type: "bit", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentRecords", x => x.ConsentRecordId);
                    table.ForeignKey(
                        name: "FK_ConsentRecords_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "core",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModelInvocationLogs",
                schema: "audit",
                columns: table => new
                {
                    ModelInvocationLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: true),
                    Endpoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PromptVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    LatencyMs = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelInvocationLogs", x => x.ModelInvocationLogId);
                    table.ForeignKey(
                        name: "FK_ModelInvocationLogs_ChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "chat",
                        principalTable: "ChatSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ModelInvocationLogs_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "core",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "SavingsSuggestions",
                schema: "analytics",
                columns: table => new
                {
                    SavingsSuggestionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedMonthlySavings = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavingsSuggestions", x => x.SavingsSuggestionId);
                    table.ForeignKey(
                        name: "FK_SavingsSuggestions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "core",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpendingSummaries",
                schema: "analytics",
                columns: table => new
                {
                    SpendingSummaryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionCount = table.Column<int>(type: "int", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpendingSummaries", x => x.SpendingSummaryId);
                    table.ForeignKey(
                        name: "FK_SpendingSummaries_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "core",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "core",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ExternalTransactionId",
                schema: "core",
                table: "Transactions",
                column: "ExternalTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentRecords_UserId",
                schema: "audit",
                table: "ConsentRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelInvocationLogs_SessionId",
                schema: "audit",
                table: "ModelInvocationLogs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelInvocationLogs_UserId",
                schema: "audit",
                table: "ModelInvocationLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SavingsSuggestions_UserId",
                schema: "analytics",
                table: "SavingsSuggestions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SpendingSummaries_UserId",
                schema: "analytics",
                table: "SpendingSummaries",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsentRecords",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "ModelInvocationLogs",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "SavingsSuggestions",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "SpendingSummaries",
                schema: "analytics");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                schema: "core",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ExternalTransactionId",
                schema: "core",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ConsentToAiProcessing",
                schema: "core",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ConsentToAnalytics",
                schema: "core",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                schema: "core",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BalanceAfter",
                schema: "core",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "core",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ExternalTransactionId",
                schema: "core",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsPending",
                schema: "core",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PostedAt",
                schema: "core",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ContextSummary",
                schema: "chat",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "LastMessageAt",
                schema: "chat",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "ModelName",
                schema: "chat",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "chat",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "CompletionTokens",
                schema: "chat",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ModelName",
                schema: "chat",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "PromptTokens",
                schema: "chat",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "PromptVersion",
                schema: "chat",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ToolArgumentsJson",
                schema: "chat",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ToolCallId",
                schema: "chat",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ToolName",
                schema: "chat",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ToolResultJson",
                schema: "chat",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "TotalTokens",
                schema: "chat",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "AccountStatus",
                schema: "core",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "AvailableBalance",
                schema: "core",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                schema: "core",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "ExternalAccountId",
                schema: "core",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "core",
                table: "Accounts");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "core",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "core",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(320)",
                oldMaxLength: 320);

            migrationBuilder.AlterColumn<string>(
                name: "TransactionType",
                schema: "core",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "MerchantName",
                schema: "core",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                schema: "core",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "TitleSummary",
                schema: "chat",
                table: "ChatSessions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                schema: "chat",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                schema: "core",
                table: "Accounts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "AccountType",
                schema: "core",
                table: "Accounts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
