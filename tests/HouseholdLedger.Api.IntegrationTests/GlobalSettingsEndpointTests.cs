// <copyright file="GlobalSettingsEndpointTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Application.Settings;
using HouseholdLedger.Domain.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

/// <summary>Verifies global settings through the ASP.NET Core host.</summary>
public sealed class GlobalSettingsEndpointTests
{
    /// <summary>Verifies missing settings return USD without persistence.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetReturnsUsdWhenNoSettingsExist()
    {
        await using var factory = new GlobalSettingsApiFactory();
        using var client = ApiTestClient.Create(factory);

        var settings = await client.GetFromJsonAsync<GlobalSettingsResponse>(
            "/api/v1/settings",
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal("USD", settings!.DisplayCurrency),
            () => Assert.Equal("workbench-dark", settings!.Theme),
            () => Assert.Equal(0, factory.Repository.UpsertCallCount));
    }

    /// <summary>Verifies supported updates persist and return the authoritative setting.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PutPersistsSupportedCurrency()
    {
        await using var factory = new GlobalSettingsApiFactory();
        using var client = ApiTestClient.Create(factory);

        using var response = await client.PutAsJsonAsync(
            "/api/v1/settings",
            new UpdateGlobalSettingsRequest("eur"),
            TestContext.Current.CancellationToken);
        var settings = await response.Content.ReadFromJsonAsync<GlobalSettingsResponse>(
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.OK, response.StatusCode),
            () => Assert.Equal("EUR", settings!.DisplayCurrency),
            () => Assert.Equal("workbench-dark", settings!.Theme),
            () => Assert.Equal(DisplayCurrency.EUR, factory.Repository.Settings!.DisplayCurrency));
    }

    /// <summary>Verifies the explicit generic display value persists through the existing contract.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PutPersistsGenericDisplay()
    {
        await using var factory = new GlobalSettingsApiFactory();
        using var client = ApiTestClient.Create(factory);

        using var response = await client.PutAsJsonAsync(
            "/api/v1/settings",
            new UpdateGlobalSettingsRequest("xxx"),
            TestContext.Current.CancellationToken);
        var settings = await response.Content.ReadFromJsonAsync<GlobalSettingsResponse>(
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.OK, response.StatusCode),
            () => Assert.Equal("XXX", settings!.DisplayCurrency),
            () => Assert.Equal(DisplayCurrency.XXX, factory.Repository.Settings!.DisplayCurrency));
    }

    /// <summary>Verifies unsupported updates return validation details without persistence.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PutRejectsUnsupportedCurrencyWithoutPersistence()
    {
        await using var factory = new GlobalSettingsApiFactory();
        using var client = ApiTestClient.Create(factory);

        using var response = await client.PutAsJsonAsync(
            "/api/v1/settings",
            new UpdateGlobalSettingsRequest("XYZ"),
            TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode),
            () => Assert.Contains("DisplayCurrency", problem!.Errors.Keys),
            () => Assert.Equal(0, factory.Repository.UpsertCallCount));
    }

    /// <summary>Verifies numeric enum values are not accepted as ISO currency codes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PutRejectsNumericEnumValueWithoutPersistence()
    {
        await using var factory = new GlobalSettingsApiFactory();
        using var client = ApiTestClient.Create(factory);

        using var response = await client.PutAsJsonAsync(
            "/api/v1/settings",
            new UpdateGlobalSettingsRequest("2"),
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode),
            () => Assert.Equal(0, factory.Repository.UpsertCallCount));
    }

    /// <summary>Verifies theme updates preserve the authoritative currency.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PutThemePersistsSupportedThemeWithoutChangingCurrency()
    {
        await using var factory = new GlobalSettingsApiFactory();
        using var client = ApiTestClient.Create(factory);
        await client.PutAsJsonAsync(
            "/api/v1/settings",
            new UpdateGlobalSettingsRequest("EUR"),
            TestContext.Current.CancellationToken);

        using var response = await client.PutAsJsonAsync(
            "/api/v1/settings/theme",
            new UpdateThemeRequest("workbench-light"),
            TestContext.Current.CancellationToken);
        var settings = await response.Content.ReadFromJsonAsync<GlobalSettingsResponse>(
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.OK, response.StatusCode),
            () => Assert.Equal("EUR", settings!.DisplayCurrency),
            () => Assert.Equal("workbench-light", settings!.Theme),
            () => Assert.Equal(WorkbenchTheme.WorkbenchLight, factory.Repository.Settings!.Theme));
    }

    /// <summary>Verifies unsupported themes are rejected without persistence.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PutThemeRejectsUnsupportedValueWithoutPersistence()
    {
        await using var factory = new GlobalSettingsApiFactory();
        using var client = ApiTestClient.Create(factory);

        using var response = await client.PutAsJsonAsync(
            "/api/v1/settings/theme",
            new UpdateThemeRequest("midnight"),
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode),
            () => Assert.Equal(0, factory.Repository.UpsertCallCount));
    }

    private sealed class GlobalSettingsApiFactory : WebApplicationFactory<Program>
    {
        public StubGlobalSettingsRepository Repository { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot(Path.Combine(FindRepositoryRoot(), "src", "HouseholdLedger.Api"));
            builder.ConfigureServices(services =>
            {
                services.AddScoped<GlobalSettingsService>();
                services.RemoveAll<IGlobalSettingsRepository>();
                services.AddSingleton<IGlobalSettingsRepository>(this.Repository);
            });
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HouseholdLedger.slnx")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName
                ?? throw new DirectoryNotFoundException("Could not locate the HouseholdLedger repository root.");
        }
    }

    private sealed class StubGlobalSettingsRepository : IGlobalSettingsRepository
    {
        public GlobalSettings? Settings { get; private set; }

        public int UpsertCallCount { get; private set; }

        public Task<GlobalSettings?> GetAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(this.Settings);
        }

        public Task<GlobalSettings> UpsertCurrencyAsync(
            DisplayCurrency displayCurrency,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.UpsertCallCount++;
            this.Settings ??= new GlobalSettings(displayCurrency);
            this.Settings.ChangeDisplayCurrency(displayCurrency);
            return Task.FromResult(this.Settings);
        }

        public Task<GlobalSettings> UpsertThemeAsync(
            WorkbenchTheme theme,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.UpsertCallCount++;
            this.Settings ??= new GlobalSettings(DisplayCurrency.USD, theme);
            this.Settings.ChangeTheme(theme);
            return Task.FromResult(this.Settings);
        }
    }
}
