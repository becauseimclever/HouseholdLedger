// <copyright file="Program.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

using System.Net;
using HouseholdLedger.Api.Hosting;
using HouseholdLedger.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

if (args.Contains("--check-readiness", StringComparer.Ordinal))
{
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    client.DefaultRequestHeaders.Host = Environment.GetEnvironmentVariable("AllowedHosts")?
        .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .FirstOrDefault(host => !host.Contains('*'));
    using var response = await client.GetAsync("http://127.0.0.1:8080/health/ready");
    response.EnsureSuccessStatusCode();
    return;
}

var builder = WebApplication.CreateBuilder(args
    .Where(argument => argument != "--migrate-database" && argument != "--initialize-development-database")
    .ToArray());

foreach (var key in new[] { "ConnectionStrings:HouseholdLedger", "Authentication:ClientSecret" })
{
    var file = builder.Configuration[$"{key}File"];
    if (!string.IsNullOrWhiteSpace(file))
    {
        builder.Configuration[key] = File.ReadAllText(file).Trim();
    }
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    foreach (var proxy in builder.Configuration.GetSection("ReverseProxy:KnownProxies").GetChildren())
    {
        options.KnownProxies.Add(IPAddress.Parse(proxy.Value!));
    }
});

var authenticationEnabled = builder.AddHouseholdAuthentication();
var protection = builder.Services.AddDataProtection().SetApplicationName("HouseholdLedger");
var keyPath = builder.Configuration["DataProtection:KeyPath"];
if (!string.IsNullOrWhiteSpace(keyPath))
{
    protection.PersistKeysToFileSystem(new DirectoryInfo(keyPath));
}

const string frontendCorsPolicy = "Frontend";
var connectionString = builder.Configuration.GetConnectionString("HouseholdLedger");
if (!string.IsNullOrWhiteSpace(connectionString) && HasUsableConnectionStringSyntax(connectionString))
{
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddScoped<HouseholdLedger.Application.Accounts.AccountService>();
    builder.Services.AddScoped<HouseholdLedger.Application.Income.IncomeScheduleService>();
    builder.Services.AddScoped<HouseholdLedger.Application.Income.IncomeReceiptService>();
    builder.Services.AddScoped<HouseholdLedger.Application.Planning.MonthlyBudgetPlanService>();
    builder.Services.AddScoped<HouseholdLedger.Application.Planning.MonthlyReflectionService>();
    builder.Services.AddScoped<HouseholdLedger.Application.Settings.GlobalSettingsService>();
    builder.Services.AddScoped<HouseholdLedger.Application.Transactions.ExpenseTransactionService>();
    builder.Services.AddHouseholdLedgerInfrastructure(connectionString);
}

builder.Services.AddProblemDetails();
builder.Services.AddControllersWithViews(options =>
{
    if (authenticationEnabled)
    {
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    }
});
builder.Services.AddHealthChecks().AddCheck<DatabaseReadinessCheck>("database");
builder.Services.AddOpenApi("v1", options =>
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Servers?.Clear();
        document.Info.Description = "Hosted deployments require the configured household owner's OIDC session for ledger endpoints. Unauthenticated API calls return 401; other identities return 403. Acquire a request token from GET /auth/session and send X-CSRF-TOKEN on writes. The health and OpenAPI documents are public. Authentication is optional only in local Development.";
        return Task.CompletedTask;
    }));
builder.Services.AddCors(options =>
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .GetChildren()
            .Select(origin => origin.Value)
            .OfType<string>()
            .ToArray();

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    }));

var app = builder.Build();

if (args.Contains("--migrate-database", StringComparer.Ordinal))
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetService<HouseholdLedgerDbContext>()
        ?? throw new InvalidOperationException("Configure ConnectionStrings:HouseholdLedger before migrating.");
    await database.Database.MigrateAsync();
    Console.WriteLine("Database migrations completed. No sample data was added.");
    return;
}

if (args.Contains("--initialize-development-database", StringComparer.Ordinal))
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Development database initialization requires the Development environment.");
    }

    await using var scope = app.Services.CreateAsyncScope();
    var initializer = scope.ServiceProvider.GetService<
        HouseholdLedger.Infrastructure.Persistence.DevelopmentDatabaseInitializer>()
        ?? throw new InvalidOperationException(
            "Configure ConnectionStrings:HouseholdLedger before initializing the development database.");
    await initializer.InitializeAsync();
    Console.WriteLine("The development database is migrated and contains the canonical fixture.");
    return;
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors(frontendCorsPolicy);
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();
app.MapOpenApi().AllowAnonymous();
app.MapHealthChecks("/health/ready").AllowAnonymous();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

static bool HasUsableConnectionStringSyntax(string connectionString)
{
    try
    {
        var parsedConnectionString = new System.Data.Common.DbConnectionStringBuilder
        {
            ConnectionString = connectionString,
        };

        return parsedConnectionString.Count > 0;
    }
    catch (ArgumentException)
    {
        return false;
    }
}

/// <summary>
/// Provides the API composition root to integration tests.
/// </summary>
public partial class Program
{
}
