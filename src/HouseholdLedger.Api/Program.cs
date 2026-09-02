// <copyright file="Program.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

var builder = WebApplication.CreateBuilder(args);

const string frontendCorsPolicy = "Frontend";
var connectionString = builder.Configuration.GetConnectionString("HouseholdLedger");
if (!string.IsNullOrWhiteSpace(connectionString) && HasUsableConnectionStringSyntax(connectionString))
{
    builder.Services.AddScoped<HouseholdLedger.Application.Transactions.ExpenseTransactionService>();
    builder.Services.AddHouseholdLedgerInfrastructure(connectionString);
}

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddOpenApi("v1", options =>
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Servers?.Clear();
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

app.MapStaticAssets();
app.MapOpenApi();
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
