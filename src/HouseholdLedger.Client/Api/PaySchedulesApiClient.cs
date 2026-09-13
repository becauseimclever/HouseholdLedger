// <copyright file="PaySchedulesApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using System.Net;
using System.Net.Http.Json;
using HouseholdLedger.Api.Contracts;

/// <summary>Calls the versioned pay-schedules endpoints.</summary>
public sealed class PaySchedulesApiClient(HttpClient httpClient) : IPaySchedulesApiClient
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<PayScheduleResponse>> ListAsync(CancellationToken cancellationToken) =>
        await httpClient.GetFromJsonAsync<PayScheduleResponse[]>("api/v1/pay-schedules", cancellationToken) ?? [];

    /// <inheritdoc/>
    public async Task<PayScheduleResponse> CreateAsync(CreatePayScheduleRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync("api/v1/pay-schedules", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PayScheduleResponse>(cancellationToken)
            ?? throw new HttpRequestException("The pay schedule response was empty.");
    }

    /// <inheritdoc/>
    public async Task<PayScheduleResponse?> ReviseAsync(Guid scheduleId, RevisePayScheduleRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PutAsJsonAsync(GetScheduleRoute(scheduleId), request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PayScheduleResponse>(cancellationToken)
            ?? throw new HttpRequestException("The pay schedule response was empty.");
    }

    /// <inheritdoc/>
    public async Task<bool> PauseAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync($"{GetScheduleRoute(scheduleId)}/pause", null, cancellationToken);
        return IsSuccessfulOrNotFound(response);
    }

    /// <inheritdoc/>
    public async Task<bool> ResumeAsync(Guid scheduleId, ResumePayScheduleRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync($"{GetScheduleRoute(scheduleId)}/resume", request, cancellationToken);
        return IsSuccessfulOrNotFound(response);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IncomeReceiptResponse>> MaterializeAsync(DateOnly payDate, CancellationToken cancellationToken) =>
        await this.PostForReceiptsAsync($"api/v1/pay-schedules/materialize/{payDate:yyyy-MM-dd}", cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IncomeReceiptResponse>> ListReceiptsAsync(DateOnly from, DateOnly endDate, CancellationToken cancellationToken) =>
        await httpClient.GetFromJsonAsync<IncomeReceiptResponse[]>(
            $"api/v1/pay-schedules/receipts?from={from:yyyy-MM-dd}&to={endDate:yyyy-MM-dd}",
            cancellationToken) ?? [];

    private static string GetScheduleRoute(Guid scheduleId) => $"api/v1/pay-schedules/{scheduleId}";

    private static bool IsSuccessfulOrNotFound(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    private async Task<IReadOnlyList<IncomeReceiptResponse>> PostForReceiptsAsync(string route, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(route, null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IncomeReceiptResponse[]>(cancellationToken) ?? [];
    }
}
