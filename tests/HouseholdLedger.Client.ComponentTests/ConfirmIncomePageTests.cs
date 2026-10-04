// <copyright file="ConfirmIncomePageTests.cs" company="HouseholdLedger">
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

/// <summary>Verifies only explicitly confirmed receipts become actual income.</summary>
public sealed class ConfirmIncomePageTests
{
    private static readonly string[] SuggestedAllocationAmounts = ["60.00", "40.00"];
    private static readonly AccountResponse Checking = new(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Checking");
    private static readonly AccountResponse Cash = new(Guid.Parse("10000000-0000-0000-0000-000000000002"), "Cash");

    /// <summary>Verifies page initialization never records income and confirmation is required.</summary>
    [Fact]
    public void OpeningReceiptFormDoesNotRecordIncomeAndRequiresExplicitConfirmation()
    {
        using var context = CreateContext(out var api, out _);
        var component = Render(context);
        Assert.Equal(0, api.Calls);
        Assert.Equal("2026-10-03", component.Find("#received-date").GetAttribute("value"));
        FillReceipt(component, "100.01");
        Assert.True(component.Find("button[type='submit']").HasAttribute("disabled"));
        component.Find("form").Submit();
        Assert.Equal(0, api.Calls);
        Assert.Contains("Confirm that this money", component.Markup, StringComparison.Ordinal);
        Assert.Contains("not spending classifications or savings", component.Markup, StringComparison.Ordinal);
    }

    /// <summary>Verifies multiple existing-account destinations total the receipt exactly.</summary>
    [Fact]
    public void ExplicitConfirmationRecordsActualDateAndExactMultipleAllocations()
    {
        using var context = CreateContext(out var api, out _);
        DateOnly? notifiedDate = null;
        context.Services.GetRequiredService<SelectedDateState>().IncomeReceiptsChanged += date => notifiedDate = date;
        var component = Render(context);
        FillReceipt(component, "100.01");
        component.Find("input[id^='receipt-allocation-']").Input("60");
        component.FindAll("button").Single(button => button.TextContent == "Add allocation").Click();
        component.FindAll("select")[1].Change(Cash.Id.ToString());
        component.FindAll("input[id^='receipt-allocation-']")[1].Input("40.01");
        component.Find("#receipt-confirmation").Change(true);
        component.Find("form").Submit();

        Assert.Equal(1, api.Calls);
        Assert.NotNull(api.LastRequest);
        Assert.Equal(new DateOnly(2026, 10, 3), api.LastRequest.ReceivedDate);
        Assert.Equal(100.01m, api.LastRequest.Amount);
        Assert.Equivalent(new[] { new IncomeAllocationRequest(Checking.Id, 60m), new IncomeAllocationRequest(Cash.Id, 40.01m) }, api.LastRequest.Allocations);
        Assert.Equal(api.LastRequest.ReceivedDate, notifiedDate);
        Assert.Contains("Received income confirmed", component.Markup, StringComparison.Ordinal);
        Assert.Equal("/months/2026/10", component.Find("a[href='/months/2026/10']").GetAttribute("href"));
        Assert.True(component.Find("button[type='submit']").HasAttribute("disabled"));
        component.Find("form").Submit();
        Assert.Equal(1, api.Calls);
    }

    /// <summary>Verifies mismatched allocations do not create receipts.</summary>
    [Fact]
    public void AllocationMismatchDoesNotRecordIncome()
    {
        using var context = CreateContext(out var api, out _);
        var component = Render(context);
        FillReceipt(component, "100.01");
        component.Find("input[id^='receipt-allocation-']").Input("100");
        component.Find("#receipt-confirmation").Change(true);
        component.Find("form").Submit();
        Assert.Equal(0, api.Calls);
        Assert.Contains("must total the received amount exactly", component.Markup, StringComparison.Ordinal);
        Assert.Equal("100.01", component.Find("#received-amount").GetAttribute("value"));
    }

    /// <summary>Verifies non-money inputs cannot be confirmed.</summary>
    /// <param name="amount">An invalid receipt amount.</param>
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.001")]
    [InlineData("10000000000000000")]
    public void InvalidReceiptAmountCannotBeConfirmed(string amount)
    {
        using var context = CreateContext(out var api, out _);
        var component = Render(context);
        FillReceipt(component, amount);
        component.Find("#receipt-confirmation").Change(true);
        component.Find("form").Submit();
        Assert.Equal(0, api.Calls);
        Assert.Contains("Enter a positive received amount", component.Markup, StringComparison.Ordinal);
    }

    /// <summary>Verifies future expectations cannot be recorded as received income.</summary>
    [Fact]
    public void FutureDateCannotBeConfirmed()
    {
        using var context = CreateContext(out var api, out _);
        var component = Render(context);
        FillReceipt(component, "100");
        component.Find("#received-date").Change("2026-10-04");
        component.Find("#receipt-confirmation").Change(true);
        component.Find("form").Submit();
        Assert.Equal(0, api.Calls);
        Assert.Contains("not a future expected date", component.Markup, StringComparison.Ordinal);
    }

    /// <summary>Verifies editing confirmed input requires renewed explicit confirmation.</summary>
    [Fact]
    public void EditingReceiptRevokesConfirmation()
    {
        using var context = CreateContext(out var api, out _);
        var component = Render(context);
        FillReceipt(component, "100");
        component.Find("#receipt-confirmation").Change(true);
        component.Find("#received-amount").Input("101");
        Assert.True(component.Find("button[type='submit']").HasAttribute("disabled"));
        Assert.Equal(0, api.Calls);
    }

    /// <summary>Verifies unrecognized account identifiers cannot be submitted.</summary>
    [Fact]
    public void UnknownAccountCannotBeConfirmed()
    {
        using var context = CreateContext(out var api, out _);
        var component = Render(context);
        FillReceipt(component, "100");
        component.Find("select").Change(Guid.NewGuid().ToString());
        component.Find("#receipt-confirmation").Change(true);
        component.Find("form").Submit();
        Assert.Equal(0, api.Calls);
        Assert.Contains("Choose an existing account", component.Markup, StringComparison.Ordinal);
    }

    /// <summary>Verifies repeated account destinations cannot be submitted.</summary>
    [Fact]
    public void DuplicateAccountAllocationsCannotBeConfirmed()
    {
        using var context = CreateContext(out var api, out _);
        var component = Render(context);
        FillReceipt(component, "100");
        component.Find("input[id^='receipt-allocation-']").Input("50");
        component.FindAll("button").Single(button => button.TextContent == "Add allocation").Click();
        component.FindAll("select")[1].Change(Checking.Id.ToString());
        component.FindAll("input[id^='receipt-allocation-']")[1].Input("50");
        component.Find("#receipt-confirmation").Change(true);
        component.Find("form").Submit();
        Assert.Equal(0, api.Calls);
        Assert.Contains("Use each account only once", component.Markup, StringComparison.Ordinal);
    }

    /// <summary>Verifies an in-flight confirmation disables controls and prevents duplicate submission.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ConfirmationDisablesFormAndPreventsDuplicateSubmission()
    {
        using var context = CreateContext(out var api, out _);
        var pending = new TaskCompletionSource<IncomeReceiptResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        api.Loader = _ => pending.Task;
        var component = Render(context);
        FillReceipt(component, "100");
        component.Find("#receipt-confirmation").Change(true);
        var confirming = component.Find("form").SubmitAsync();
        Assert.True(component.Find("form > fieldset").HasAttribute("disabled"));
        component.Find("form").Submit();
        Assert.Equal(1, api.Calls);
        pending.SetResult(new IncomeReceiptResponse(Guid.NewGuid(), null, new DateOnly(2026, 10, 3), 100m, [new(Checking.Id, 100m)]));
        await confirming;
        Assert.Contains("Received income confirmed", component.Markup, StringComparison.Ordinal);
    }

    /// <summary>Verifies save errors preserve all entered receipt fields.</summary>
    [Fact]
    public void RejectedReceiptPreservesDateAmountAndAllocations()
    {
        using var context = CreateContext(out var api, out _);
        api.Failure = new HttpRequestException("Rejected", null, HttpStatusCode.BadRequest);
        var component = Render(context);
        FillReceipt(component, "123.45");
        component.Find("#receipt-confirmation").Change(true);
        component.Find("form").Submit();
        Assert.Contains("server rejected", component.Markup, StringComparison.Ordinal);
        Assert.Equal("123.45", component.Find("#received-amount").GetAttribute("value"));
        Assert.Equal("123.45", component.Find("input[id^='receipt-allocation-']").GetAttribute("value"));
        Assert.Equal(Checking.Id.ToString(), component.Find("select").GetAttribute("value"));
        Assert.DoesNotContain("Received income confirmed", component.Markup, StringComparison.Ordinal);
        var requestId = Assert.Single(api.RequestIds);
        component.Find("form").Submit();
        Assert.Equal(2, api.Calls);
        Assert.Equal(requestId, api.RequestIds[1]);

        component.Find("#received-amount").Input("124");
        component.Find("input[id^='receipt-allocation-']").Input("124");
        component.Find("#receipt-confirmation").Change(true);
        component.Find("form").Submit();
        Assert.Equal(3, api.Calls);
        Assert.NotEqual(requestId, api.RequestIds[2]);
    }

    /// <summary>Verifies unconfirmed receipt drafts are protected from internal navigation.</summary>
    [Fact]
    public void UnsavedReceiptDraftPromptsBeforeInternalNavigation()
    {
        using var context = CreateContext(out _, out _);
        var navigation = context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        navigation.NavigateTo("/income-receipts/2026/10/3");
        context.JSInterop.Setup<bool>("confirm").SetResult(false);
        var component = Render(context);
        FillReceipt(component, "100");

        navigation.NavigateTo("/accounts");

        Assert.Multiple(
            () => Assert.EndsWith("/income-receipts/2026/10/3", navigation.Uri, StringComparison.Ordinal),
            () => Assert.Equal("100", component.Find("#received-amount").GetAttribute("value")),
            () => Assert.Contains(context.JSInterop.Invocations, invocation => invocation.Identifier == "confirm"));
    }

    /// <summary>Verifies schedule details are suggestions only and never submit a receipt.</summary>
    [Fact]
    public void ScheduleQueryPrefillsSuggestedAmountsWithoutConfirmingOrSending()
    {
        using var context = CreateContext(out var api, out _);
        var scheduleId = Guid.NewGuid();
        context.Services.AddSingleton<IPaySchedulesApiClient>(new StubPaySchedulesApi(
            [new PayScheduleResponse(scheduleId, "Biweekly pay", new DateOnly(2026, 10, 3), PayPeriodCadence.Biweekly, 100m, [new IncomeAllocationResponse(Checking.Id, 60m), new IncomeAllocationResponse(Cash.Id, 40m)], null, false)]));
        context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
            .NavigateTo($"/income-receipts/2026/10/3?scheduleId={scheduleId}");
        var component = Render(context);

        Assert.Multiple(
            () => Assert.Equal("100.00", component.Find("#received-amount").GetAttribute("value")),
            () => Assert.Equal(SuggestedAllocationAmounts, component.FindAll("input[id^='receipt-allocation-']").Select(input => input.GetAttribute("value"))),
            () => Assert.False(component.Find("#receipt-confirmation").HasAttribute("checked")),
            () => Assert.Contains("nothing was confirmed or sent", component.Markup, StringComparison.Ordinal),
            () => Assert.Equal(0, api.Calls));
    }

    /// <summary>Verifies absent accounts and failed account loading are distinguishable.</summary>
    [Fact]
    public void MissingAccountsAndUnavailableAccountsPreventConfirmation()
    {
        using var context = CreateContext(out var api, out var accounts);
        accounts.Accounts = [];
        var component = Render(context);
        Assert.Contains("No accounts yet", component.Markup, StringComparison.Ordinal);
        Assert.Empty(component.FindAll("form"));
        Assert.Equal(0, api.Calls);

        using var failedContext = CreateContext(out var failedApi, out var failedAccounts);
        failedAccounts.Failure = new HttpRequestException("Unavailable");
        var failedComponent = Render(failedContext);
        Assert.Contains("Accounts are unavailable", failedComponent.Markup, StringComparison.Ordinal);
        Assert.Empty(failedComponent.FindAll("form"));
        Assert.Equal(0, failedApi.Calls);
        failedAccounts.Failure = null;
        failedComponent.Find("button").Click();
        Assert.Single(failedComponent.FindAll("form"));
    }

    private static BunitContext CreateContext(out StubIncomeApi api, out StubAccountsApi accounts)
    {
        var context = new BunitContext();
        api = new StubIncomeApi();
        accounts = new StubAccountsApi();
        context.Services.AddSingleton<IIncomeReceiptsApiClient>(api);
        context.Services.AddSingleton<IAccountsApiClient>(accounts);
        context.Services.AddSingleton<IPaySchedulesApiClient>(new StubPaySchedulesApi([]));
        context.Services.AddSingleton<SelectedDateState>();
        context.Services.AddSingleton<TimeProvider>(new FixedTimeProvider());
        return context;
    }

    private static IRenderedComponent<ConfirmIncomePage> Render(BunitContext context) =>
        context.Render<ConfirmIncomePage>(parameters => parameters.Add(page => page.Year, 2026).Add(page => page.Month, 10).Add(page => page.Day, 3));

    private static void FillReceipt(IRenderedComponent<ConfirmIncomePage> component, string amount)
    {
        component.Find("#received-amount").Input(amount);
        component.Find("select").Change(Checking.Id.ToString());
        component.Find("input[id^='receipt-allocation-']").Input(amount);
    }

    private sealed class StubIncomeApi : IIncomeReceiptsApiClient
    {
        public int Calls { get; private set; }

        public ConfirmIncomeReceiptRequest? LastRequest { get; private set; }

        public List<Guid> RequestIds { get; } = [];

        public HttpRequestException? Failure { get; set; }

        public Func<ConfirmIncomeReceiptRequest, Task<IncomeReceiptResponse>>? Loader { get; set; }

        public Task<IncomeReceiptResponse> ConfirmAsync(ConfirmIncomeReceiptRequest request, Guid requestId, CancellationToken cancellationToken)
        {
            this.Calls++;
            this.LastRequest = request;
            this.RequestIds.Add(requestId);
            if (this.Loader is not null)
            {
                return this.Loader(request);
            }

            return this.Failure is not null
                ? Task.FromException<IncomeReceiptResponse>(this.Failure)
                : Task.FromResult(new IncomeReceiptResponse(Guid.NewGuid(), null, request.ReceivedDate, request.Amount, request.Allocations.Select(allocation => new IncomeAllocationResponse(allocation.AccountId, allocation.Amount)).ToArray()));
        }

        public Task<IncomeReceiptResponse> ReviseAsync(Guid receiptId, ConfirmIncomeReceiptRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<TransactionMutationResult> RemoveAsync(Guid receiptId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class StubPaySchedulesApi(IReadOnlyList<PayScheduleResponse> schedules) : IPaySchedulesApiClient
    {
        public Task<IReadOnlyList<PayScheduleResponse>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(schedules);

        public Task<PayScheduleResponse> CreateAsync(CreatePayScheduleRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PayScheduleResponse?> ReviseAsync(Guid scheduleId, RevisePayScheduleRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> PauseAsync(Guid scheduleId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> ResumeAsync(Guid scheduleId, ResumePayScheduleRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<IncomeReceiptResponse>> ListReceiptsAsync(DateOnly from, DateOnly endDate, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<IncomeReceiptResponse>>([]);
    }

    private sealed class StubAccountsApi : IAccountsApiClient
    {
        public IReadOnlyList<AccountResponse> Accounts { get; set; } = [Checking, Cash];

        public HttpRequestException? Failure { get; set; }

        public Task<IReadOnlyList<AccountResponse>> ListAsync(CancellationToken cancellationToken) =>
            this.Failure is not null ? Task.FromException<IReadOnlyList<AccountResponse>>(this.Failure) : Task.FromResult(this.Accounts);

        public Task<AccountCreationResult> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);
    }
}
