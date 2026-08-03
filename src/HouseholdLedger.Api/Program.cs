// <copyright file="Program.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

var builder = WebApplication.CreateBuilder(args);

const string frontendCorsPolicy = "Frontend";
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

app.MapOpenApi();
app.MapControllers();

app.Run();

/// <summary>
/// Provides the API composition root to integration tests.
/// </summary>
public partial class Program
{
}
