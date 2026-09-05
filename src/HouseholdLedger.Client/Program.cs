// <copyright file="Program.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client;

using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Starts the standalone WebAssembly client.
/// </summary>
public static class Program
{
    /// <summary>
    /// Configures and runs the client application.
    /// </summary>
    /// <param name="args">Command-line arguments supplied by the host.</param>
    /// <returns>A task representing the application lifetime.</returns>
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");
        builder.Services.AddScoped(serviceProvider => new HttpClient
        {
            BaseAddress = ApiConfiguration.GetBaseAddress(
                builder.Configuration,
                new Uri(serviceProvider.GetRequiredService<NavigationManager>().BaseUri, UriKind.Absolute)),
        });
        builder.Services.AddScoped<IHealthApiClient, HealthApiClient>();
        builder.Services.AddScoped<IMonthlyExpenseSummaryApiClient, MonthlyExpenseSummaryApiClient>();
        builder.Services.AddScoped<ITransactionsApiClient, TransactionsApiClient>();
        builder.Services.AddScoped<SelectedDateState>();
        builder.Services.AddSingleton(TimeProvider.System);

        await builder.Build().RunAsync();
    }
}
