// <copyright file="PayScheduleEndpointTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Application.Accounts;
using HouseholdLedger.Application.Income;
using HouseholdLedger.Domain.Accounts;
using HouseholdLedger.Domain.Income;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using ApiPayPeriodCadence = HouseholdLedger.Api.Contracts.PayPeriodCadence;

/// <summary>Verifies pay schedule endpoints through the ASP.NET Core host.</summary>
public sealed class PayScheduleEndpointTests
{
    /// <summary>Verifies creation, lifecycle actions, and explicit receipt materialization.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CreatePauseResumeAndMaterializeReturnsScheduleAndReceipt()
    {
        var account = new Account(Guid.NewGuid(), "Income checking");
        await using var factory = new PayScheduleApiFactory(account);
        using var client = ApiTestClient.Create(factory);
        var payDate = new DateOnly(2026, 9, 15);

        using var createResponse = await client.PostAsJsonAsync(
            "/api/v1/pay-schedules",
            new CreatePayScheduleRequest("Salary", payDate, ApiPayPeriodCadence.Biweekly, 2500m, [new IncomeAllocationRequest(account.Id, 2500m)]),
            TestContext.Current.CancellationToken);
        var schedule = await createResponse.Content.ReadFromJsonAsync<PayScheduleResponse>(TestContext.Current.CancellationToken);
        using var reviseResponse = await client.PutAsJsonAsync(
            $"/api/v1/pay-schedules/{schedule!.Id}",
            new RevisePayScheduleRequest("Revised salary", payDate, ApiPayPeriodCadence.Biweekly, 3000m, [new IncomeAllocationRequest(account.Id, 3000m)]),
            TestContext.Current.CancellationToken);
        var revised = await reviseResponse.Content.ReadFromJsonAsync<PayScheduleResponse>(TestContext.Current.CancellationToken);
        using var pauseResponse = await client.PostAsync($"/api/v1/pay-schedules/{schedule!.Id}/pause", null, TestContext.Current.CancellationToken);
        using var pausedMaterialization = await client.PostAsync($"/api/v1/pay-schedules/materialize/{payDate:yyyy-MM-dd}", null, TestContext.Current.CancellationToken);
        using var resumeResponse = await client.PostAsJsonAsync(
            $"/api/v1/pay-schedules/{schedule.Id}/resume",
            new ResumePayScheduleRequest(payDate),
            TestContext.Current.CancellationToken);
        using var materializeResponse = await client.PostAsync($"/api/v1/pay-schedules/materialize/{payDate:yyyy-MM-dd}", null, TestContext.Current.CancellationToken);
        var receipts = await materializeResponse.Content.ReadFromJsonAsync<IncomeReceiptResponse[]>(TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode),
            () => Assert.Equal("Salary", schedule.Name),
            () => Assert.Equal(HttpStatusCode.OK, reviseResponse.StatusCode),
            () => Assert.Equal("Revised salary", revised!.Name),
            () => Assert.Equal(3000m, revised!.NetIncome),
            () => Assert.Equal(HttpStatusCode.NoContent, pauseResponse.StatusCode),
            () => Assert.Equal(HttpStatusCode.OK, pausedMaterialization.StatusCode),
            () => Assert.Equal(HttpStatusCode.NoContent, resumeResponse.StatusCode),
            () => Assert.Equal(HttpStatusCode.OK, materializeResponse.StatusCode),
            () => Assert.Equal(schedule!.Id, Assert.Single(receipts!).ScheduleId));
    }

    /// <summary>Verifies undefined cadence values are rejected as field-level validation errors.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CreateRejectsUndefinedCadence()
    {
        var account = new Account(Guid.NewGuid(), "Income checking");
        await using var factory = new PayScheduleApiFactory(account);
        using var client = ApiTestClient.Create(factory);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/pay-schedules",
            new CreatePayScheduleRequest("Salary", new DateOnly(2026, 9, 15), (ApiPayPeriodCadence)999, 2500m, [new IncomeAllocationRequest(account.Id, 2500m)]),
            TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode),
            () => Assert.Contains(nameof(CreatePayScheduleRequest.Cadence), problem!.Errors.Keys));
    }

    /// <summary>Verifies pay schedule and calendar receipt reads preserve persisted values.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ListGetAndListReceiptsReturnPersistedSchedulesAndAllocationSnapshots()
    {
        var checking = new Account(Guid.NewGuid(), "Income checking");
        var savings = new Account(Guid.NewGuid(), "Income savings");
        await using var factory = new PayScheduleApiFactory(checking, savings);
        using var client = ApiTestClient.Create(factory);
        var payDate = new DateOnly(2026, 9, 15);

        using var createResponse = await client.PostAsJsonAsync(
            "/api/v1/pay-schedules",
            new CreatePayScheduleRequest(
                "Salary",
                payDate,
                ApiPayPeriodCadence.Biweekly,
                2500m,
                [new IncomeAllocationRequest(checking.Id, 1800m), new IncomeAllocationRequest(savings.Id, 700m)]),
            TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<PayScheduleResponse>(TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Expected a created pay schedule response.");
        using var listResponse = await client.GetAsync("/api/v1/pay-schedules", TestContext.Current.CancellationToken);
        var schedules = await listResponse.Content.ReadFromJsonAsync<PayScheduleResponse[]>(TestContext.Current.CancellationToken);
        using var getResponse = await client.GetAsync($"/api/v1/pay-schedules/{created!.Id}", TestContext.Current.CancellationToken);
        var schedule = await getResponse.Content.ReadFromJsonAsync<PayScheduleResponse>(TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Expected a pay schedule response.");
        using var missingResponse = await client.GetAsync($"/api/v1/pay-schedules/{Guid.NewGuid()}", TestContext.Current.CancellationToken);
        var missingProblem = await missingResponse.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        using var materializeResponse = await client.PostAsync($"/api/v1/pay-schedules/materialize/{payDate:yyyy-MM-dd}", null, TestContext.Current.CancellationToken);
        var materialized = await materializeResponse.Content.ReadFromJsonAsync<IncomeReceiptResponse[]>(TestContext.Current.CancellationToken);
        using var receiptsResponse = await client.GetAsync("/api/v1/pay-schedules/receipts?from=2026-09-01&to=2026-09-30", TestContext.Current.CancellationToken);
        var receipts = await receiptsResponse.Content.ReadFromJsonAsync<IncomeReceiptResponse[]>(TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode),
            () => Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode),
            () => Assert.Equal(created.Id, Assert.Single(schedules!).Id),
            () => Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode),
            () => Assert.Equal(created.Id, schedule.Id),
            () => Assert.Equal(created.Name, schedule.Name),
            () => Assert.Equal(created.NetIncome, schedule.NetIncome),
            () => Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode),
            () => Assert.Equal("Pay schedule not found", missingProblem!.Title),
            () => Assert.Equal(HttpStatusCode.OK, receiptsResponse.StatusCode),
            () => Assert.Equal(Assert.Single(materialized!).Id, Assert.Single(receipts!).Id),
            () => Assert.Collection(
                receipts![0].Allocations,
                allocation => Assert.Equal(new IncomeAllocationResponse(checking.Id, 1800m), allocation),
                allocation => Assert.Equal(new IncomeAllocationResponse(savings.Id, 700m), allocation)));
    }

    /// <summary>Verifies receipt calendar range validation uses Problem Details.</summary>
    /// <param name="requestUri">The receipt range request URI.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData("/api/v1/pay-schedules/receipts?to=2026-09-30")]
    [InlineData("/api/v1/pay-schedules/receipts?from=2026-09-01")]
    [InlineData("/api/v1/pay-schedules/receipts?from=not-a-date&to=2026-09-30")]
    [InlineData("/api/v1/pay-schedules/receipts?from=2026-10-01&to=2026-09-30")]
    public async Task ListReceiptsRejectsMissingMalformedAndInvertedDates(string requestUri)
    {
        var account = new Account(Guid.NewGuid(), "Income checking");
        await using var factory = new PayScheduleApiFactory(account);
        using var client = ApiTestClient.Create(factory);

        using var response = await client.GetAsync(requestUri, TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode),
            () => Assert.NotEmpty(problem!.Errors));
    }

    private sealed class PayScheduleApiFactory(params Account[] accounts) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IncomeScheduleService>();
                services.RemoveAll<IAccountRepository>();
                services.RemoveAll<IIncomeScheduleRepository>();
                services.AddSingleton<IAccountRepository>(new InMemoryAccountRepository(accounts));
                services.AddSingleton<IIncomeScheduleRepository, InMemoryIncomeScheduleRepository>();
            });
        }
    }

    private sealed class InMemoryAccountRepository(params Account[] accounts) : IAccountRepository
    {
        public Task<Account?> FindAsync(Guid accountId, CancellationToken cancellationToken) => Task.FromResult(accounts.SingleOrDefault(account => account.Id == accountId));

        public Task<bool> TryAddAsync(Account account, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Account>>(accounts);
    }

    private sealed class InMemoryIncomeScheduleRepository : IIncomeScheduleRepository
    {
        private readonly List<PaySchedule> schedules = [];
        private readonly List<IncomeReceipt> receipts = [];

        public Task AddScheduleAsync(PaySchedule schedule, CancellationToken cancellationToken)
        {
            this.schedules.Add(schedule);
            return Task.CompletedTask;
        }

        public Task<PaySchedule?> FindScheduleAsync(Guid scheduleId, CancellationToken cancellationToken) => Task.FromResult(this.schedules.SingleOrDefault(schedule => schedule.Id == scheduleId));

        public Task<IReadOnlyList<PaySchedule>> ListSchedulesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PaySchedule>>(this.schedules);

        public Task<IReadOnlyList<IncomeReceipt>> ListReceiptsAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<IncomeReceipt>>(this.receipts.Where(receipt => receipt.PayDate >= startDate && receipt.PayDate <= endDate).ToArray());

        public Task UpdateScheduleAsync(PaySchedule schedule, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IncomeReceipt?> GetOrAddReceiptAsync(IncomeReceipt receipt, CancellationToken cancellationToken)
        {
            if (this.receipts.Any(existing => existing.ScheduleId == receipt.ScheduleId && existing.PayDate == receipt.PayDate))
            {
                return Task.FromResult<IncomeReceipt?>(null);
            }

            this.receipts.Add(receipt);
            return Task.FromResult<IncomeReceipt?>(receipt);
        }
    }
}
