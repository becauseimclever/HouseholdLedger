// <copyright file="MonthlyBudgetPageTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using System.Net;
using Bunit;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.Pages;
using HouseholdLedger.Client.State;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>Verifies deliberate monthly planning and authoritative review states.</summary>
public sealed class MonthlyBudgetPageTests
{
    private static readonly DateTimeOffset RevisedAt = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Verifies six labeled inputs, exact reconciliation, and deliberate saving.</summary>
    [Fact]
    public void PlanRequiresExactReconciliationAndSavesAllFourClassifications()
    {
        using var context = CreateContext(out var api);
        var component = Render(context);
        Assert.Contains("No plan has been saved", component.Markup, StringComparison.Ordinal);
        Assert.Equal(6, component.FindAll(".plan-fields label").Count);
        component.Find("#expected-income").Input("100.01");
        component.Find("#intended-savings").Input("20");
        component.Find("#necessities").Input("30");
        component.Find("#optional").Input("25");
        component.Find("#culture").Input("15");
        component.Find("#unexpected").Input("10");
        Assert.True(component.Find("button[type='submit']").HasAttribute("disabled"));
        Assert.Contains("0.01", component.Find("#plan-reconciliation").TextContent, StringComparison.Ordinal);
        component.Find("form").Submit();
        Assert.Equal(0, api.SaveCalls);

        component.Find("#unexpected").Input("10.01");
        Assert.False(component.Find("button[type='submit']").HasAttribute("disabled"));
        component.Find("form").Submit();

        Assert.Equal(new MonthlyBudgetPlanRequest(100.01m, 20m, 30m, 25m, 15m, 10.01m), api.LastSave);
        Assert.Contains("Monthly plan saved", component.Markup, StringComparison.Ordinal);
        Assert.Contains("Latest plan revision", component.Markup, StringComparison.Ordinal);
        Assert.Equal(1, api.SaveCalls);
    }

    /// <summary>Verifies unsupported money values cannot be saved or treated as zero.</summary>
    /// <param name="value">An invalid money input.</param>
    [Theory]
    [InlineData("-0.01")]
    [InlineData("0.001")]
    [InlineData("10000000000000000")]
    [InlineData("")]
    [InlineData("1e2")]
    public void InvalidMoneyDisablesSavingAndShowsIncompleteReconciliation(string value)
    {
        using var context = CreateContext(out var api);
        var component = Render(context);
        component.Find("#expected-income").Input(value);
        Assert.True(component.Find("button[type='submit']").HasAttribute("disabled"));
        Assert.Contains("Enter a valid amount", component.Find("#plan-reconciliation").TextContent, StringComparison.Ordinal);
        component.Find("form").Submit();
        Assert.Equal(0, api.SaveCalls);
    }

    /// <summary>Verifies the supported money ceiling is accepted without rounding.</summary>
    [Fact]
    public void MaximumMoneyReconcilesExactly()
    {
        using var context = CreateContext(out var api);
        var component = Render(context);
        component.Find("#expected-income").Input("9999999999999999.99");
        component.Find("#intended-savings").Input("9999999999999999.99");
        component.Find("form").Submit();
        Assert.Equal(9999999999999999.99m, api.LastSave?.ExpectedIncome);
    }

    /// <summary>Verifies save rejection preserves unsaved intention rather than reloading it.</summary>
    [Fact]
    public void FailedSavePreservesUserInput()
    {
        using var context = CreateContext(out var api);
        api.SaveFailure = new HttpRequestException("Rejected", null, HttpStatusCode.BadRequest);
        var component = Render(context);
        component.Find("#expected-income").Input("123.45");
        component.Find("#necessities").Input("123.45");
        component.Find("form").Submit();
        Assert.Equal("123.45", component.Find("#expected-income").GetAttribute("value"));
        Assert.Equal("123.45", component.Find("#necessities").GetAttribute("value"));
        Assert.Contains("server rejected", component.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Monthly plan saved", component.Markup, StringComparison.Ordinal);
    }

    /// <summary>Verifies authoritative comparisons, revision labeling, and honest savings language.</summary>
    [Fact]
    public void ReviewUsesBackendVariancesAndNeverClaimsActualSavings()
    {
        using var context = CreateContext(out var api);
        api.Plan = Plan(2026, 10, 100m);
        api.Review = new MonthlyBudgetReviewResponse(
            100m,
            20m,
            90m,
            -10m,
            55m,
            RevisedAt,
            [
                new("Necessities", 30m, 35m, 5m),
                new("Optional", 25m, 0m, -25m),
                new("Culture", 15m, 0m, -15m),
                new("Unexpected", 10m, 0m, -10m),
            ],
            55m);
        var component = Render(context);
        Assert.Equal(4, component.FindAll("tbody tr").Count);
        Assert.Contains("35.00", component.Find("tbody tr").TextContent, StringComparison.Ordinal);
        Assert.Contains("5.00", component.Find("tbody tr").TextContent, StringComparison.Ordinal);
        Assert.Contains("Income recorded so far (confirmed)", component.Markup, StringComparison.Ordinal);
        Assert.Contains("90.00", component.Find(".review-totals").TextContent, StringComparison.Ordinal);
        Assert.Contains("Intended savings (target, not actual savings)", component.Markup, StringComparison.Ordinal);
        Assert.Contains("Unallocated remainder", component.Markup, StringComparison.Ordinal);
        Assert.Contains("55.00", component.Find(".review-totals").TextContent, StringComparison.Ordinal);
        Assert.Contains("Recorded cashflow (income minus recorded expenses)", component.Find(".review-totals").TextContent, StringComparison.Ordinal);
        Assert.Equal("$55.00", component.FindAll(".review-totals dd")[5].TextContent);
        Assert.Contains("not a bank balance or overdraft indicator", component.Markup, StringComparison.Ordinal);
        Assert.Contains("Review uses latest plan revision", component.Markup, StringComparison.Ordinal);
        Assert.Equal(2, component.FindAll("time[datetime='2026-10-03T12:00:00.0000000+00:00']").Count);
    }

    /// <summary>Verifies missing plans, empty actuals, and unavailable data are distinct.</summary>
    [Fact]
    public void MissingPlanEmptyActivityAndUnavailableDataHaveDistinctStates()
    {
        using var context = CreateContext(out var api);
        var component = Render(context);
        Assert.Contains("No plan has been saved", component.Markup, StringComparison.Ordinal);
        Assert.Contains("No confirmed income or recorded expenses", component.Markup, StringComparison.Ordinal);
        Assert.Contains("No saved plan", component.Find(".review-totals").TextContent, StringComparison.Ordinal);

        api.LoadFailure = new HttpRequestException("Unavailable");
        component.Render(parameters => parameters.Add(page => page.Month, 11));
        Assert.Contains("monthly plan is unavailable", component.Markup, StringComparison.Ordinal);
        Assert.Contains("actual totals are not known", component.Markup, StringComparison.Ordinal);
        Assert.Empty(component.FindAll("form.plan-form"));
        Assert.Single(component.FindAll("form.reflection-form"));
        Assert.Empty(component.FindAll(".review-totals"));
    }

    /// <summary>Verifies actual activity remains visible even without a saved plan.</summary>
    [Fact]
    public void ActualActivityWithoutPlanIsNotPresentedAsZeroOrSavings()
    {
        using var context = CreateContext(out var api);
        api.Review = new MonthlyBudgetReviewResponse(null, null, 100m, null, 75m, null, [new("Culture", 0m, 25m, 25m)], 75m);
        var component = Render(context);
        Assert.Contains("100.00", component.Find(".review-totals").TextContent, StringComparison.Ordinal);
        var totals = component.FindAll(".review-totals dd");
        Assert.Equal("No saved plan", totals[0].TextContent);
        Assert.Equal("No saved plan", totals[1].TextContent);
        Assert.Equal("No saved plan", totals[3].TextContent);
        Assert.Contains("75.00", totals[4].TextContent, StringComparison.Ordinal);
        Assert.Equal("$75.00", totals[5].TextContent);
        Assert.Empty(component.FindAll("time"));
        Assert.Contains("25.00", component.Find("tbody").TextContent, StringComparison.Ordinal);
        Assert.Contains("No saved plan", component.Find("tbody").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("No confirmed income", component.Markup, StringComparison.Ordinal);
    }

    /// <summary>Verifies signed cashflow remains negative when recorded expenses exceed income.</summary>
    [Fact]
    public void RecordedCashflowShowsNegativeDifferenceWithoutCallingItAnOverdraft()
    {
        using var context = CreateContext(out var api);
        api.Review = new MonthlyBudgetReviewResponse(null, null, 20m, null, 0m, null, [new("Necessities", 0m, 35m, 35m)], -15m);

        var component = Render(context);

        Assert.StartsWith("-", component.FindAll(".review-totals dd")[5].TextContent, StringComparison.Ordinal);
        Assert.Contains("not a bank balance or overdraft indicator", component.Markup, StringComparison.Ordinal);
    }

    /// <summary>Verifies failed review refreshes hide stale totals without discarding unsaved plan edits.</summary>
    [Fact]
    public void ReviewFailureHidesStaleTotalsAndRetryPreservesPlanEdits()
    {
        using var context = CreateContext(out var api);
        var component = Render(context);
        component.Find("#expected-income").Input("321");
        api.ReviewLoader = (_, _) => Task.FromException<MonthlyBudgetReviewResponse>(new HttpRequestException("Unavailable"));
        context.Services.GetRequiredService<SelectedDateState>().NotifyTransactionsChanged(new DateOnly(2026, 10, 3));
        component.WaitForAssertion(() => Assert.Contains("actual totals are not known", component.Markup, StringComparison.Ordinal));
        Assert.Empty(component.FindAll(".review-totals"));
        api.ReviewLoader = null;
        component.FindAll("button").Single(button => button.TextContent == "Retry review").Click();
        Assert.Single(component.FindAll(".review-totals"));
        Assert.Equal("321", component.Find("#expected-income").GetAttribute("value"));
    }

    /// <summary>Verifies a save finishing after navigation cannot replace the next month's plan.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ObsoleteSaveCannotReplaceNextMonthPlanOrStatus()
    {
        using var context = CreateContext(out var api);
        var lateSave = new TaskCompletionSource<MonthlyBudgetPlanResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        api.SaveLoader = (_, _) => lateSave.Task;
        var component = Render(context);
        var saving = component.Find("form").SubmitAsync();
        Assert.True(component.Find("fieldset").HasAttribute("disabled"));
        api.Plan = Plan(2026, 11, 200m);
        component.Render(parameters => parameters.Add(page => page.Month, 11));
        lateSave.SetResult(Plan(2026, 10, 999m));
        await saving;
        Assert.Equal("200.00", component.Find("#expected-income").GetAttribute("value"));
        Assert.DoesNotContain("Monthly plan saved", component.Markup, StringComparison.Ordinal);
    }

    /// <summary>Verifies invalid routes show an actionable error instead of querying the API.</summary>
    [Fact]
    public void InvalidMonthDoesNotQueryBackend()
    {
        using var context = CreateContext(out var api);
        var component = context.Render<MonthlyBudgetPage>(parameters => parameters.Add(page => page.Year, 2026).Add(page => page.Month, 13));
        Assert.Contains("Choose a valid calendar month", component.Markup, StringComparison.Ordinal);
        Assert.Equal(0, api.PlanCalls);
    }

    /// <summary>Verifies reflection persists independently and preserves drafts on failure.</summary>
    [Fact]
    public void MonthlyReflectionSavesOptionalPromptsSeparatelyFromThePlan()
    {
        using var context = CreateContext(out var api);
        var component = Render(context);
        component.Find("#reflection-worked").Input("  Kept a spending note  ");
        component.Find("#reflection-intention").Input("  Wait before buying  ");
        component.Find("form.reflection-form").Submit();

        var reflectionApi = Assert.IsType<StubReflectionApi>(context.Services.GetRequiredService<IMonthlyReflectionApiClient>());
        Assert.Equal(new MonthlyReflectionRequest("Kept a spending note", "Wait before buying"), reflectionApi.LastSave);
        Assert.Equal(0, api.SaveCalls);
        Assert.Contains("Monthly reflection saved separately", component.Markup, StringComparison.Ordinal);
        Assert.Equal("Kept a spending note", component.Find("#reflection-worked").TextContent);
    }

    /// <summary>Verifies unsaved plan and reflection drafts remain in the page guard state.</summary>
    [Fact]
    public void EditingPlanOrReflectionPresentsUnsavedNavigationProtection()
    {
        using var context = CreateContext(out _);
        var navigation = context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        navigation.NavigateTo("/months/2026/10");
        var component = Render(context);
        context.JSInterop.Setup<bool>("confirm").SetResult(false);

        component.Find("#expected-income").Input("10");
        navigation.NavigateTo("/settings");
        Assert.EndsWith("/months/2026/10", navigation.Uri, StringComparison.Ordinal);
        Assert.Contains(context.JSInterop.Invocations, invocation => invocation.Identifier == "confirm");
    }

    /// <summary>Verifies late plan and review responses cannot replace the active month.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ObsoleteMonthResponsesCannotReplaceActiveMonth()
    {
        using var context = CreateContext(out var api);
        var latePlan = new TaskCompletionSource<MonthlyBudgetPlanResponse?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var lateReview = new TaskCompletionSource<MonthlyBudgetReviewResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        api.PlanLoader = (_, month) => month == 10 ? latePlan.Task : Task.FromResult<MonthlyBudgetPlanResponse?>(Plan(2026, 11, 200m));
        api.ReviewLoader = (_, month) => month == 10 ? lateReview.Task : Task.FromResult(EmptyReview());
        var component = Render(context);
        Assert.Contains("Loading monthly plan", component.Markup, StringComparison.Ordinal);
        component.Render(parameters => parameters.Add(page => page.Month, 11));
        component.WaitForAssertion(() => Assert.Equal("200.00", component.Find("#expected-income").GetAttribute("value")));

        var renderCount = component.RenderCount;
        latePlan.SetResult(Plan(2026, 10, 999m));
        lateReview.SetResult(new MonthlyBudgetReviewResponse(999m, 0m, 999m, 0m, 999m, RevisedAt, []));
        await component.InvokeAsync(() => Task.CompletedTask);
        component.WaitForAssertion(() =>
        {
            Assert.True(component.RenderCount > renderCount);
            Assert.Contains("November 2026", component.Markup, StringComparison.Ordinal);
            Assert.Equal("200.00", component.Find("#expected-income").GetAttribute("value"));
            Assert.DoesNotContain("999.00", component.Markup, StringComparison.Ordinal);
        });
    }

    /// <summary>Verifies an exhausted remainder is displayed as backend-provided zero without implying savings.</summary>
    [Fact]
    public void ZeroRemainderWithoutPlanRemainsAnAuthoritativeAmount()
    {
        using var context = CreateContext(out var api);
        api.Review = new MonthlyBudgetReviewResponse(null, null, 10m, null, 0m, null, [new("Necessities", 0m, 25m, 25m)]);
        var component = Render(context);
        Assert.Contains("0.00", component.FindAll(".review-totals dd")[4].TextContent, StringComparison.Ordinal);
        Assert.Contains("10.00", component.FindAll(".review-totals dd")[2].TextContent, StringComparison.Ordinal);
        Assert.Contains("25.00", component.Find("tbody").TextContent, StringComparison.Ordinal);
        Assert.Contains("Unallocated remainder is not savings", component.Markup, StringComparison.Ordinal);
    }

    private static BunitContext CreateContext(out StubBudgetApi api)
    {
        var context = new BunitContext();
        api = new StubBudgetApi();
        context.Services.AddSingleton<IMonthlyReflectionApiClient>(new StubReflectionApi());
        context.Services.AddSingleton<IMonthlyBudgetApiClient>(api);
        context.Services.AddSingleton<SelectedDateState>();
        return context;
    }

    private static IRenderedComponent<MonthlyBudgetPage> Render(BunitContext context) =>
        context.Render<MonthlyBudgetPage>(parameters => parameters.Add(page => page.Year, 2026).Add(page => page.Month, 10));

    private static MonthlyBudgetPlanResponse Plan(int year, int month, decimal expected) =>
        new(year, month, expected, 0m, expected, 0m, 0m, 0m, RevisedAt);

    private static MonthlyBudgetReviewResponse EmptyReview() =>
        new(null, null, 0m, null, 0m, null, [new("Necessities", 0m, 0m, 0m), new("Optional", 0m, 0m, 0m), new("Culture", 0m, 0m, 0m), new("Unexpected", 0m, 0m, 0m)]);

    private sealed class StubBudgetApi : IMonthlyBudgetApiClient
    {
        public MonthlyBudgetPlanResponse? Plan { get; set; }

        public MonthlyBudgetReviewResponse Review { get; set; } = EmptyReview();

        public MonthlyBudgetPlanRequest? LastSave { get; private set; }

        public HttpRequestException? SaveFailure { get; set; }

        public HttpRequestException? LoadFailure { get; set; }

        public Func<int, int, Task<MonthlyBudgetPlanResponse?>>? PlanLoader { get; set; }

        public Func<int, int, Task<MonthlyBudgetReviewResponse>>? ReviewLoader { get; set; }

        public Func<int, int, Task<MonthlyBudgetPlanResponse>>? SaveLoader { get; set; }

        public int SaveCalls { get; private set; }

        public int PlanCalls { get; private set; }

        public Task<MonthlyBudgetPlanResponse?> GetPlanAsync(int year, int month, CancellationToken cancellationToken)
        {
            this.PlanCalls++;
            if (this.LoadFailure is not null)
            {
                return Task.FromException<MonthlyBudgetPlanResponse?>(this.LoadFailure);
            }

            return this.PlanLoader?.Invoke(year, month) ?? Task.FromResult(this.Plan);
        }

        public Task<MonthlyBudgetReviewResponse> GetReviewAsync(int year, int month, CancellationToken cancellationToken) =>
            this.LoadFailure is not null ? Task.FromException<MonthlyBudgetReviewResponse>(this.LoadFailure) : this.ReviewLoader?.Invoke(year, month) ?? Task.FromResult(this.Review);

        public Task<MonthlyBudgetPlanResponse> SavePlanAsync(int year, int month, MonthlyBudgetPlanRequest request, CancellationToken cancellationToken)
        {
            this.SaveCalls++;
            this.LastSave = request;
            if (this.SaveFailure is not null)
            {
                return Task.FromException<MonthlyBudgetPlanResponse>(this.SaveFailure);
            }

            if (this.SaveLoader is not null)
            {
                return this.SaveLoader(year, month);
            }

            this.Plan = new MonthlyBudgetPlanResponse(year, month, request.ExpectedIncome, request.IntendedSavings, request.Necessities, request.Optional, request.Culture, request.Unexpected, RevisedAt);
            return Task.FromResult(this.Plan);
        }
    }

    private sealed class StubReflectionApi : IMonthlyReflectionApiClient
    {
        public MonthlyReflectionRequest? LastSave { get; private set; }

        public Task<MonthlyReflectionResponse?> GetAsync(int year, int month, CancellationToken cancellationToken) =>
            Task.FromResult<MonthlyReflectionResponse?>(null);

        public Task<MonthlyReflectionResponse> SaveAsync(int year, int month, MonthlyReflectionRequest request, CancellationToken cancellationToken)
        {
            this.LastSave = request;
            return Task.FromResult(new MonthlyReflectionResponse(request.WhatWorked, request.NextMonthIntention, RevisedAt));
        }
    }
}
