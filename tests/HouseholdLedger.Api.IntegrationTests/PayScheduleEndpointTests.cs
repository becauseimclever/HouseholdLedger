// <copyright file="PayScheduleEndpointTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Application.Accounts;
using HouseholdLedger.Application.Income;
using HouseholdLedger.Application.Planning;
using HouseholdLedger.Application.Transactions;
using HouseholdLedger.Domain.Accounts;
using HouseholdLedger.Domain.Income;
using HouseholdLedger.Domain.Planning;
using HouseholdLedger.Domain.Transactions;
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
    /// <summary>Retries identical confirmations, rejects conflicting tokens, and corrects or removes actuals.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ReceiptRetryCorrectionAndDeletionPreserveExplicitActuals()
    {
        var account = new Account(Guid.NewGuid(), "Checking");
        await using var factory = new PayScheduleApiFactory(account);
        using var client = ApiTestClient.Create(factory);
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new ConfirmIncomeReceiptRequest(
            new DateOnly(2026, 9, 15),
            100m,
            [new IncomeAllocationRequest(account.Id, 100m)],
            Guid.NewGuid());
        using var first = await client.PostAsJsonAsync("/api/v1/income-receipts", request, cancellationToken);
        var receipt = (await first.Content.ReadFromJsonAsync<IncomeReceiptResponse>(cancellationToken))!;
        using var retry = await client.PostAsJsonAsync("/api/v1/income-receipts", request, cancellationToken);
        var replay = (await retry.Content.ReadFromJsonAsync<IncomeReceiptResponse>(cancellationToken))!;
        Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
        Assert.Equal(receipt.Id, replay.Id);
        using var conflict = await client.PostAsJsonAsync(
            "/api/v1/income-receipts",
            request with { Amount = 101m, Allocations = [new IncomeAllocationRequest(account.Id, 101m)] },
            cancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        using var separate = await client.PostAsJsonAsync("/api/v1/income-receipts", request with { RequestId = Guid.NewGuid() }, cancellationToken);
        var separateReceipt = (await separate.Content.ReadFromJsonAsync<IncomeReceiptResponse>(cancellationToken))!;
        Assert.NotEqual(receipt.Id, separateReceipt.Id);
        using var update = await client.PutAsJsonAsync(
            $"/api/v1/income-receipts/{receipt.Id}",
            new UpdateIncomeReceiptRequest(new DateOnly(2026, 9, 16), 120m, [new IncomeAllocationRequest(account.Id, 120m)]),
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var corrected = (await update.Content.ReadFromJsonAsync<IncomeReceiptResponse>(cancellationToken))!;
        Assert.Equal(new DateOnly(2026, 9, 16), corrected.PayDate);
        Assert.Equal(120m, corrected.NetIncome);
        var review = (await client.GetFromJsonAsync<MonthlyBudgetReviewResponse>("/api/v1/months/2026/9/budget-review", cancellationToken))!;
        Assert.Equal(220m, review.ActualIncome);
        using var deletion = await client.DeleteAsync($"/api/v1/income-receipts/{receipt.Id}", cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deletion.StatusCode);
        using var replayDeleted = await client.PostAsJsonAsync("/api/v1/income-receipts", request, cancellationToken);
        Assert.Equal(HttpStatusCode.Created, replayDeleted.StatusCode);
        using var missing = await client.GetAsync($"/api/v1/income-receipts/{receipt.Id}", cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var afterDeletion = (await client.GetFromJsonAsync<MonthlyBudgetReviewResponse>("/api/v1/months/2026/9/budget-review", cancellationToken))!;
        Assert.Equal(100m, afterDeletion.ActualIncome);
        using var deleteMissing = await client.DeleteAsync($"/api/v1/income-receipts/{receipt.Id}", cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, deleteMissing.StatusCode);
    }

    /// <summary>Rejects unreceived dates, malformed retry tokens and broken allocations without mutation.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ReceiptRejectsFutureDefaultDateEmptyTokenAndAllocationMismatch()
    {
        var account = new Account(Guid.NewGuid(), "Checking");
        await using var factory = new PayScheduleApiFactory(account);
        using var client = ApiTestClient.Create(factory);
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new ConfirmIncomeReceiptRequest(new DateOnly(2026, 9, 15), 100m, [new IncomeAllocationRequest(account.Id, 100m)]);
        foreach (var invalid in new[]
        {
            request with { ReceivedDate = new DateOnly(9999, 12, 31) },
            request with { ReceivedDate = default },
            request with { RequestId = Guid.Empty },
            request with { Amount = 101m },
            request with { Allocations = [new IncomeAllocationRequest(account.Id, 50m), new IncomeAllocationRequest(account.Id, 50m)] },
        })
        {
            using var response = await client.PostAsJsonAsync("/api/v1/income-receipts", invalid, cancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        using var created = await client.PostAsJsonAsync("/api/v1/income-receipts", request, cancellationToken);
        var receipt = (await created.Content.ReadFromJsonAsync<IncomeReceiptResponse>(cancellationToken))!;
        using var invalidUpdate = await client.PutAsJsonAsync(
            $"/api/v1/income-receipts/{receipt.Id}",
            new UpdateIncomeReceiptRequest(new DateOnly(9999, 12, 31), 100m, request.Allocations),
            cancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalidUpdate.StatusCode);
        var unchanged = (await client.GetFromJsonAsync<IncomeReceiptResponse>($"/api/v1/income-receipts/{receipt.Id}", cancellationToken))!;
        Assert.Equal(request.ReceivedDate, unchanged.PayDate);
    }

    /// <summary>Saves and clears optional reflection without requiring a plan.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ReflectionIsStandaloneOptionalAndBounded()
    {
        await using var factory = new PayScheduleApiFactory();
        using var client = ApiTestClient.Create(factory);
        var cancellationToken = TestContext.Current.CancellationToken;
        const string route = "/api/v1/months/2026/9/reflection";
        using var absent = await client.GetAsync(route, cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, absent.StatusCode);
        using var saved = await client.PutAsJsonAsync(route, new MonthlyReflectionRequest("  Cooked  ", "  Plan meals  "), cancellationToken);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var reflection = (await client.GetFromJsonAsync<MonthlyReflectionResponse>(route, cancellationToken))!;
        Assert.Equal("Cooked", reflection.WhatWorked);
        Assert.Equal("Plan meals", reflection.NextMonthIntention);
        Assert.NotEqual(default, reflection.LastRevisedAt);
        using var invalid = await client.PutAsJsonAsync(route, new MonthlyReflectionRequest(new string('x', 1001)), cancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var cleared = await client.PutAsJsonAsync(route, new MonthlyReflectionRequest(), cancellationToken);
        var empty = (await cleared.Content.ReadFromJsonAsync<MonthlyReflectionResponse>(cancellationToken))!;
        Assert.Null(empty.WhatWorked);
        Assert.Null(empty.NextMonthIntention);
        using var plan = await client.GetAsync("/api/v1/months/2026/9/budget-plan", cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, plan.StatusCode);
    }

    /// <summary>Verifies schedule lifecycle without implicitly recording received income.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CreatePauseAndResumeDoNotCreateIncomeReceipts()
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
        using var resumeResponse = await client.PostAsJsonAsync(
            $"/api/v1/pay-schedules/{schedule.Id}/resume",
            new ResumePayScheduleRequest(payDate),
            TestContext.Current.CancellationToken);
        using var materializeResponse = await client.PostAsync($"/api/v1/pay-schedules/materialize/{payDate:yyyy-MM-dd}", null, TestContext.Current.CancellationToken);
        using var receiptsResponse = await client.GetAsync("/api/v1/pay-schedules/receipts?from=2026-09-01&to=2026-09-30", TestContext.Current.CancellationToken);
        var receipts = await receiptsResponse.Content.ReadFromJsonAsync<IncomeReceiptResponse[]>(TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode),
            () => Assert.Equal("Salary", schedule.Name),
            () => Assert.Equal(HttpStatusCode.OK, reviseResponse.StatusCode),
            () => Assert.Equal("Revised salary", revised!.Name),
            () => Assert.Equal(3000m, revised!.NetIncome),
            () => Assert.Equal(HttpStatusCode.NoContent, pauseResponse.StatusCode),
            () => Assert.Equal(HttpStatusCode.NoContent, resumeResponse.StatusCode),
            () => Assert.False(materializeResponse.IsSuccessStatusCode),
            () => Assert.Equal(HttpStatusCode.OK, receiptsResponse.StatusCode),
            () => Assert.Empty(receipts!));
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
        using var confirmReceiptResponse = await client.PostAsJsonAsync(
            "/api/v1/income-receipts",
            new ConfirmIncomeReceiptRequest(payDate, 2500m, [new IncomeAllocationRequest(checking.Id, 1800m), new IncomeAllocationRequest(savings.Id, 700m)]),
            TestContext.Current.CancellationToken);
        var confirmedReceipt = await confirmReceiptResponse.Content.ReadFromJsonAsync<IncomeReceiptResponse>(TestContext.Current.CancellationToken);
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
            () => Assert.Equal(HttpStatusCode.Created, confirmReceiptResponse.StatusCode),
            () => Assert.NotEqual(Guid.Empty, confirmedReceipt!.Id),
            () => Assert.Null(confirmedReceipt!.ScheduleId),
            () => Assert.Equal(HttpStatusCode.OK, receiptsResponse.StatusCode),
            () => Assert.Equal(confirmedReceipt!.Id, Assert.Single(receipts!).Id),
            () => Assert.Collection(
                receipts![0].Allocations,
                allocation => Assert.Equal(new IncomeAllocationResponse(checking.Id, 1800m), allocation),
                allocation => Assert.Equal(new IncomeAllocationResponse(savings.Id, 700m), allocation)));
    }

    /// <summary>Verifies monthly plans are saved and reviewed through the HTTP contract.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task SavePlanAndGetReviewReturnIntentionsAndActuals()
    {
        await using var factory = new PayScheduleApiFactory();
        using var client = ApiTestClient.Create(factory);

        using var saveResponse = await client.PutAsJsonAsync(
            "/api/v1/months/2026/9/budget-plan",
            new MonthlyBudgetPlanRequest(1000m, 200m, 400m, 100m, 100m, 200m),
            TestContext.Current.CancellationToken);
        var savedPlan = await saveResponse.Content.ReadFromJsonAsync<MonthlyBudgetPlanResponse>(TestContext.Current.CancellationToken);
        using var getResponse = await client.GetAsync("/api/v1/months/2026/9/budget-plan", TestContext.Current.CancellationToken);
        var loadedPlan = await getResponse.Content.ReadFromJsonAsync<MonthlyBudgetPlanResponse>(TestContext.Current.CancellationToken);
        using var reviewResponse = await client.GetAsync("/api/v1/months/2026/9/budget-review", TestContext.Current.CancellationToken);
        var review = await reviewResponse.Content.ReadFromJsonAsync<MonthlyBudgetReviewResponse>(TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode),
            () => Assert.Equal(2026, savedPlan!.Year),
            () => Assert.Equal(9, savedPlan!.Month),
            () => Assert.Equal(savedPlan, loadedPlan),
            () => Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode),
            () => Assert.Equal(1000m, review!.ExpectedIncome),
            () => Assert.Equal(0m, review!.ActualIncome),
            () => Assert.Equal(-1000m, review!.IncomeVariance),
            () => Assert.Equal(4, review!.Classifications.Count));
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
                services.AddScoped<IncomeReceiptService>();
                services.AddScoped<MonthlyBudgetPlanService>();
                services.AddScoped<MonthlyReflectionService>();
                services.AddSingleton(TimeProvider.System);
                services.RemoveAll<IAccountRepository>();
                services.RemoveAll<IIncomeScheduleRepository>();
                services.RemoveAll<IMonthlyBudgetPlanRepository>();
                services.RemoveAll<IMonthlyReflectionRepository>();
                services.RemoveAll<IExpenseTransactionRepository>();
                services.AddSingleton<IAccountRepository>(new InMemoryAccountRepository(accounts));
                services.AddSingleton<IIncomeScheduleRepository, InMemoryIncomeScheduleRepository>();
                services.AddSingleton<IMonthlyBudgetPlanRepository, InMemoryMonthlyBudgetPlanRepository>();
                services.AddSingleton<IMonthlyReflectionRepository, InMemoryMonthlyReflectionRepository>();
                services.AddSingleton<IExpenseTransactionRepository, EmptyExpenseTransactionRepository>();
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
        private readonly Dictionary<Guid, IncomeReceipt> requests = [];

        public Task AddScheduleAsync(PaySchedule schedule, CancellationToken cancellationToken)
        {
            this.schedules.Add(schedule);
            return Task.CompletedTask;
        }

        public Task<PaySchedule?> FindScheduleAsync(Guid scheduleId, CancellationToken cancellationToken) => Task.FromResult(this.schedules.SingleOrDefault(schedule => schedule.Id == scheduleId));

        public Task<IReadOnlyList<PaySchedule>> ListSchedulesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PaySchedule>>(this.schedules);

        public Task<IReadOnlyList<IncomeReceipt>> ListReceiptsAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<IncomeReceipt>>(this.receipts.Where(receipt => receipt.PayDate >= startDate && receipt.PayDate <= endDate).ToArray());

        public Task<IncomeReceipt?> FindReceiptAsync(Guid receiptId, CancellationToken cancellationToken) =>
            Task.FromResult(this.receipts.SingleOrDefault(receipt => receipt.Id == receiptId));

        public Task AddReceiptAsync(IncomeReceipt receipt, CancellationToken cancellationToken)
        {
            this.receipts.Add(receipt);
            return Task.CompletedTask;
        }

        public Task UpdateScheduleAsync(PaySchedule schedule, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IncomeReceipt> ConfirmReceiptAsync(IncomeReceipt receipt, Guid? requestId, CancellationToken cancellationToken)
        {
            if (requestId is Guid token && this.requests.TryGetValue(token, out var original))
            {
                if (original.PayDate != receipt.PayDate || original.NetIncome != receipt.NetIncome
                    || !original.Allocations.OrderBy(item => item.AccountId).Select(item => (item.AccountId, item.Amount))
                        .SequenceEqual(receipt.Allocations.OrderBy(item => item.AccountId).Select(item => (item.AccountId, item.Amount))))
                {
                    throw new IncomeReceiptRequestConflictException();
                }

                return Task.FromResult(original);
            }

            this.receipts.Add(receipt);
            if (requestId is Guid newToken)
            {
                this.requests.Add(newToken, receipt);
            }

            return Task.FromResult(receipt);
        }

        public Task<bool> UpdateReceiptAsync(IncomeReceipt receipt, CancellationToken cancellationToken)
        {
            var index = this.receipts.FindIndex(item => item.Id == receipt.Id);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            this.receipts[index] = receipt;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteReceiptAsync(Guid receiptId, CancellationToken cancellationToken) =>
            Task.FromResult(this.receipts.RemoveAll(item => item.Id == receiptId) > 0);
    }

    private sealed class InMemoryMonthlyBudgetPlanRepository : IMonthlyBudgetPlanRepository
    {
        private readonly Dictionary<DateOnly, MonthlyBudgetPlan> plans = [];

        public Task<MonthlyBudgetPlan?> FindAsync(DateOnly month, CancellationToken cancellationToken) =>
            Task.FromResult(this.plans.GetValueOrDefault(month));

        public Task UpsertAsync(MonthlyBudgetPlan plan, CancellationToken cancellationToken)
        {
            this.plans[plan.Month] = plan;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryMonthlyReflectionRepository : IMonthlyReflectionRepository
    {
        private readonly Dictionary<DateOnly, MonthlyReflection> reflections = [];

        public Task<MonthlyReflection?> FindAsync(DateOnly month, CancellationToken cancellationToken) =>
            Task.FromResult(this.reflections.GetValueOrDefault(month));

        public Task UpsertAsync(MonthlyReflection reflection, CancellationToken cancellationToken)
        {
            this.reflections[reflection.Month] = reflection;
            return Task.CompletedTask;
        }
    }

    private sealed class EmptyExpenseTransactionRepository : IExpenseTransactionRepository
    {
        public Task AddAsync(ExpenseTransaction transaction, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ExpenseTransaction?> FindAsync(DateOnly ledgerDate, Guid transactionId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ExpenseTransactionDto>> ListByDateAsync(DateOnly ledgerDate, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ExpenseTransactionDto>> ListByAccountAsync(Guid accountId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ExpenseTransaction>> ListByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ExpenseTransaction>>([]);

        public Task UpdateAsync(ExpenseTransaction transaction, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RemoveAsync(ExpenseTransaction transaction, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
