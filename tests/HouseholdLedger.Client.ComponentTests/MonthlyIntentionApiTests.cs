// <copyright file="MonthlyIntentionApiTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using Xunit;

/// <summary>Verifies the typed monthly and explicit receipt wire contracts.</summary>
public sealed class MonthlyIntentionApiTests
{
    /// <summary>Verifies exact versioned routes, request amounts, and response deserialization.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task PlanPutAndGetUseExpectedContracts()
    {
        var request = new MonthlyBudgetPlanRequest(100.01m, 20m, 30m, 25m, 15m, 10.01m);
        var plan = new MonthlyBudgetPlanResponse(2026, 10, 100.01m, 20m, 30m, 25m, 15m, 10.01m, DateTimeOffset.UnixEpoch);
        using var handler = new StubHandler(async message =>
        {
            Assert.Equal("/api/v1/months/2026/10/budget-plan", message.RequestUri!.AbsolutePath);
            if (message.Method == HttpMethod.Put)
            {
                Assert.NotNull(message.Content);
                Assert.Equal(request, await message.Content.ReadFromJsonAsync<MonthlyBudgetPlanRequest>());
            }
            else
            {
                Assert.Equal(HttpMethod.Get, message.Method);
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(plan) };
        });
        using var http = CreateHttp(handler);
        var api = new MonthlyBudgetApiClient(http);
        Assert.Equal(plan, await api.SavePlanAsync(2026, 10, request, CancellationToken.None));
        Assert.Equal(plan, await api.GetPlanAsync(2026, 10, CancellationToken.None));
    }

    /// <summary>Verifies missing plans differ from unavailable and invalid responses.</summary>
    /// <param name="status">The response status.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task OnlyNotFoundMeansMissingPlan(HttpStatusCode status)
    {
        using var handler = new StubHandler(_ => Task.FromResult(new HttpResponseMessage(status)));
        using var http = CreateHttp(handler);
        var api = new MonthlyBudgetApiClient(http);
        if (status == HttpStatusCode.NotFound)
        {
            Assert.Null(await api.GetPlanAsync(2026, 10, CancellationToken.None));
        }
        else
        {
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => api.GetPlanAsync(2026, 10, CancellationToken.None));
            Assert.Equal(status, exception.StatusCode);
        }
    }

    /// <summary>Verifies review uses the backend response without recomputing actuals.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ReviewUsesDedicatedMonthlyEndpoint()
    {
        var review = new MonthlyBudgetReviewResponse(100m, 20m, 90m, -10m, 55m, DateTimeOffset.UnixEpoch, [new("Necessities", 30m, 35m, 5m)]);
        using var handler = new StubHandler(message =>
        {
            Assert.Equal(HttpMethod.Get, message.Method);
            Assert.Equal("/api/v1/months/2026/10/budget-review", message.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(review) });
        });
        using var http = CreateHttp(handler);
        var result = await new MonthlyBudgetApiClient(http).GetReviewAsync(2026, 10, CancellationToken.None);
        Assert.Equivalent(review, result);
    }

    /// <summary>Verifies confirmation posts actual date and allocations without schedule materialization.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ConfirmationPostsUserEnteredActualReceipt()
    {
        var accountId = Guid.NewGuid();
        var request = new ConfirmIncomeReceiptRequest(new DateOnly(2026, 10, 3), 100.01m, [new(accountId, 100.01m)]);
        var requestId = Guid.NewGuid();
        var receipt = new IncomeReceiptResponse(Guid.NewGuid(), null, request.ReceivedDate, request.Amount, [new(accountId, request.Amount)]);
        using var handler = new StubHandler(async message =>
        {
            Assert.Equal(HttpMethod.Post, message.Method);
            Assert.Equal("/api/v1/income-receipts", message.RequestUri!.AbsolutePath);
            Assert.NotNull(message.Content);
            Assert.Equivalent(request with { RequestId = requestId }, await message.Content.ReadFromJsonAsync<ConfirmIncomeReceiptRequest>());
            using var json = JsonDocument.Parse(await message.Content.ReadAsStringAsync());
            Assert.Equal(requestId, json.RootElement.GetProperty("requestId").GetGuid());
            return new HttpResponseMessage(HttpStatusCode.Created) { Content = JsonContent.Create(receipt) };
        });
        using var http = CreateHttp(handler);
        var result = await new IncomeReceiptsApiClient(http).ConfirmAsync(request, requestId, CancellationToken.None);
        Assert.Equivalent(receipt, result);
    }

    /// <summary>Verifies corrections and removals use the receipt-specific routes.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ReceiptCorrectionsAndRemovalUseIdRoutes()
    {
        var receiptId = Guid.NewGuid();
        var update = new ConfirmIncomeReceiptRequest(new DateOnly(2026, 10, 4), 120m, [new(Guid.NewGuid(), 120m)]);
        var receipt = new IncomeReceiptResponse(receiptId, null, update.ReceivedDate, update.Amount, update.Allocations.Select(item => new IncomeAllocationResponse(item.AccountId, item.Amount)).ToArray());
        using var handler = new StubHandler(async message =>
        {
            Assert.Equal($"/api/v1/income-receipts/{receiptId}", message.RequestUri!.AbsolutePath);
            if (message.Method == HttpMethod.Put)
            {
                Assert.Equivalent(new UpdateIncomeReceiptRequest(update.ReceivedDate, update.Amount, update.Allocations), await message.Content!.ReadFromJsonAsync<UpdateIncomeReceiptRequest>());
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(receipt) };
            }

            Assert.Equal(HttpMethod.Delete, message.Method);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        using var http = CreateHttp(handler);
        var api = new IncomeReceiptsApiClient(http);
        Assert.Equivalent(receipt, await api.ReviseAsync(receiptId, update, CancellationToken.None));
        Assert.Equal(TransactionMutationResult.Success, await api.RemoveAsync(receiptId, CancellationToken.None));
    }

    /// <summary>Verifies GET and PUT use the separate monthly reflection endpoint.</summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ReflectionUsesSeparateMonthlyEndpoint()
    {
        var request = new MonthlyReflectionRequest("Saved consistently", "Pause before optional spending");
        var response = new MonthlyReflectionResponse(request.WhatWorked, request.NextMonthIntention, DateTimeOffset.UnixEpoch);
        using var handler = new StubHandler(async message =>
        {
            Assert.Equal("/api/v1/months/2026/10/reflection", message.RequestUri!.AbsolutePath);
            if (message.Method == HttpMethod.Put)
            {
                Assert.Equal(request, await message.Content!.ReadFromJsonAsync<MonthlyReflectionRequest>());
            }
            else
            {
                Assert.Equal(HttpMethod.Get, message.Method);
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) };
        });
        using var http = CreateHttp(handler);
        var api = new MonthlyReflectionApiClient(http);
        Assert.Equal(response, await api.SaveAsync(2026, 10, request, CancellationToken.None));
        Assert.Equal(response, await api.GetAsync(2026, 10, CancellationToken.None));
    }

    private static HttpClient CreateHttp(HttpMessageHandler handler) => new(handler) { BaseAddress = new Uri("https://ledger.test") };

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request);
    }
}
