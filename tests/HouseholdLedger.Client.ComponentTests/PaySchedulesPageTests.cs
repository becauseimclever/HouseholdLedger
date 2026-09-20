// <copyright file="PaySchedulesPageTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using Bunit;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.Pages;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>Verifies pay schedule management interactions.</summary>
public sealed class PaySchedulesPageTests
{
    private static readonly AccountResponse Account = new(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Checking");

    /// <summary>Verifies schedule creation exposes the authoritative allocation controls.</summary>
    [Fact]
    public void CreationFormExposesAuthoritativeAllocationControls()
    {
        using var context = CreateContext(out var api);
        var component = context.Render<PaySchedulesPage>();
        component.WaitForElement(".schedule-form");

        Assert.Multiple(
            () => Assert.Equal("Checking", component.Find("select[id^='account-'] option[value='10000000-0000-0000-0000-000000000001']").TextContent),
            () => Assert.Equal("Allocated: $0.00 of USD", component.Find(".allocation-total").TextContent),
            () => Assert.Equal(0, api.CreateCalls));
    }

    /// <summary>Verifies an additional account allocation row can be added.</summary>
    [Fact]
    public void AddingAllocationAddsAnotherAccountRow()
    {
        using var context = CreateContext(out _);
        var component = context.Render<PaySchedulesPage>();
        component.WaitForElement(".schedule-form");

        component.FindAll("button").Single(button => button.TextContent == "Add allocation").Click();

        Assert.Equal(2, component.FindAll("select[id^='account-']").Count);
    }

    /// <summary>Verifies all supported cadence choices are available to schedule creation.</summary>
    [Fact]
    public void CreationFormExposesEverySupportedCadence()
    {
        using var context = CreateContext(out var api);
        var component = context.Render<PaySchedulesPage>();
        component.WaitForElement(".schedule-form");

        Assert.Multiple(
            () => Assert.Equal(5, component.FindAll("#schedule-cadence option").Count),
            () => Assert.Contains("Semimonthly", component.Find("#schedule-cadence").TextContent, StringComparison.Ordinal),
            () => Assert.Equal(0, api.CreateCalls));
    }

    /// <summary>Verifies the paused schedule state exposes no-backfill resume controls.</summary>
    [Fact]
    public void PausedScheduleShowsResumeControlsAndNoBackfillStatus()
    {
        var schedule = Schedule(isPaused: true);
        using var context = CreateContext(out var api, schedule);
        var component = context.Render<PaySchedulesPage>();
        component.WaitForElement(".schedule-list");

        Assert.Multiple(
            () => Assert.Single(component.FindAll("input[id^='resume-date-']")),
            () => Assert.Equal("Resume", component.FindAll(".schedule-actions button").Single(button => button.TextContent == "Resume").TextContent),
            () => Assert.Contains("does not backfill", component.Find(".no-backfill-status").TextContent, StringComparison.Ordinal));
    }

    private static BunitContext CreateContext(out StubPaySchedulesApiClient paySchedulesApi, params PayScheduleResponse[] schedules)
    {
        var context = new BunitContext();
        paySchedulesApi = new StubPaySchedulesApiClient(schedules);
        context.Services.AddSingleton(TimeProvider.System);
        context.Services.AddSingleton<IAccountsApiClient>(new StubAccountsApiClient());
        context.Services.AddSingleton<IPaySchedulesApiClient>(paySchedulesApi);
        return context;
    }

    private static PayScheduleResponse Schedule(bool isPaused) => new(
        Guid.Parse("20000000-0000-0000-0000-000000000001"),
        "Salary",
        new DateOnly(2026, 9, 15),
        PayPeriodCadence.Biweekly,
        1000m,
        [new IncomeAllocationResponse(Account.Id, 1000m)],
        null,
        isPaused);

    private sealed class StubAccountsApiClient : IAccountsApiClient
    {
        public Task<AccountCreationResult> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken) => Task.FromResult(AccountCreationResult.Success);

        public Task<IReadOnlyList<AccountResponse>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<AccountResponse>>([Account]);
    }

    private sealed class StubPaySchedulesApiClient(params PayScheduleResponse[] schedules) : IPaySchedulesApiClient
    {
        private List<PayScheduleResponse> schedules = [.. schedules];

        public int CreateCalls { get; private set; }

        public int PauseCalls { get; private set; }

        public int ResumeCalls { get; private set; }

        public CreatePayScheduleRequest? LastCreate { get; private set; }

        public ResumePayScheduleRequest? LastResume { get; private set; }

        public Task<PayScheduleResponse> CreateAsync(CreatePayScheduleRequest request, CancellationToken cancellationToken)
        {
            this.CreateCalls++;
            this.LastCreate = request;
            var created = new PayScheduleResponse(Guid.NewGuid(), request.Name, request.FirstPayDate, request.Cadence, request.NetIncome, request.Allocations.Select(allocation => new IncomeAllocationResponse(allocation.AccountId, allocation.Amount)).ToArray(), request.SecondMonthlyPayDay, false);
            this.schedules.Add(created);
            return Task.FromResult(created);
        }

        public Task<IReadOnlyList<PayScheduleResponse>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PayScheduleResponse>>(this.schedules);

        public Task<IReadOnlyList<IncomeReceiptResponse>> ListReceiptsAsync(DateOnly from, DateOnly endDate, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<IncomeReceiptResponse>>([]);

        public Task<IReadOnlyList<IncomeReceiptResponse>> MaterializeAsync(DateOnly payDate, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<IncomeReceiptResponse>>([]);

        public Task<bool> PauseAsync(Guid scheduleId, CancellationToken cancellationToken)
        {
            this.PauseCalls++;
            this.schedules = this.schedules.Select(schedule => schedule.Id == scheduleId ? schedule with { IsPaused = true } : schedule).ToList();
            return Task.FromResult(true);
        }

        public Task<bool> ResumeAsync(Guid scheduleId, ResumePayScheduleRequest request, CancellationToken cancellationToken)
        {
            this.ResumeCalls++;
            this.LastResume = request;
            this.schedules = this.schedules.Select(schedule => schedule.Id == scheduleId ? schedule with { IsPaused = false } : schedule).ToList();
            return Task.FromResult(true);
        }

        public Task<PayScheduleResponse?> ReviseAsync(Guid scheduleId, RevisePayScheduleRequest request, CancellationToken cancellationToken) => Task.FromResult<PayScheduleResponse?>(null);
    }
}
