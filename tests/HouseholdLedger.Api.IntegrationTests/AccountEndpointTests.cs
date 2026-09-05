// <copyright file="AccountEndpointTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Application.Accounts;
using HouseholdLedger.Domain.Accounts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

/// <summary>Verifies account endpoints through the ASP.NET Core host.</summary>
public sealed class AccountEndpointTests
{
    /// <summary>Verifies valid creation is persisted and returned by an authoritative reread.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CreateThenListReturnsTrimmedAccount()
    {
        await using var factory = new AccountApiFactory();
        using var client = ApiTestClient.Create(factory);

        using var createResponse = await client.PostAsJsonAsync(
            "/api/v1/accounts",
            new CreateAccountRequest("  Household Checking  "),
            TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<AccountResponse>(
            TestContext.Current.CancellationToken);
        var accounts = await client.GetFromJsonAsync<AccountResponse[]>(
            "/api/v1/accounts",
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode),
            () => Assert.Equal("Household Checking", created!.Name),
            () => Assert.Equal(created, Assert.Single(accounts!)));
    }

    /// <summary>Verifies invalid names return field-level validation without persistence.</summary>
    /// <param name="name">The invalid account name.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData("   ")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task CreateRejectsInvalidName(string name)
    {
        await using var factory = new AccountApiFactory();
        using var client = ApiTestClient.Create(factory);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/accounts",
            new CreateAccountRequest(name),
            TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
            TestContext.Current.CancellationToken);
        var accounts = await client.GetFromJsonAsync<AccountResponse[]>(
            "/api/v1/accounts",
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode),
            () => Assert.Contains(nameof(CreateAccountRequest.Name), problem!.Errors.Keys),
            () => Assert.Empty(accounts!));
    }

    /// <summary>Verifies names that differ only by case are rejected as duplicates.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CreateRejectsCaseInsensitiveDuplicate()
    {
        await using var factory = new AccountApiFactory();
        using var client = ApiTestClient.Create(factory);

        using var firstResponse = await client.PostAsJsonAsync(
            "/api/v1/accounts",
            new CreateAccountRequest("Cash Wallet"),
            TestContext.Current.CancellationToken);
        using var duplicateResponse = await client.PostAsJsonAsync(
            "/api/v1/accounts",
            new CreateAccountRequest("cash wallet"),
            TestContext.Current.CancellationToken);
        var problem = await duplicateResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>(
            TestContext.Current.CancellationToken);
        var accounts = await client.GetFromJsonAsync<AccountResponse[]>(
            "/api/v1/accounts",
            TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode),
            () => Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode),
            () => Assert.Contains("already exists", problem!.Errors[nameof(CreateAccountRequest.Name)].Single(), StringComparison.Ordinal),
            () => Assert.Single(accounts!));
    }

    private sealed class AccountApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<AccountService>();
                services.RemoveAll<IAccountRepository>();
                services.AddSingleton<IAccountRepository, InMemoryAccountRepository>();
            });
        }
    }

    private sealed class InMemoryAccountRepository : IAccountRepository
    {
        private readonly List<Account> accounts = [];

        public Task<bool> TryAddAsync(Account account, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (this.accounts.Any(existing => existing.NormalizedName == account.NormalizedName))
            {
                return Task.FromResult(false);
            }

            this.accounts.Add(account);
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<Account>>(this.accounts.ToArray());
        }
    }
}
