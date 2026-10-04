// <copyright file="IncomeReceiptsApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using System.Net;
using System.Net.Http.Json;
using HouseholdLedger.Api.Contracts;

/// <summary>Calls the explicit receipt-confirmation endpoint.</summary>
public sealed class IncomeReceiptsApiClient(HttpClient httpClient) : IIncomeReceiptsApiClient
{
    /// <inheritdoc/>
    public async Task<IncomeReceiptResponse> ConfirmAsync(ConfirmIncomeReceiptRequest request, Guid requestId, CancellationToken cancellationToken)
    {
        var payload = request with { RequestId = requestId };
        using var response = await httpClient.PostAsJsonAsync("api/v1/income-receipts", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IncomeReceiptResponse>(cancellationToken)
            ?? throw new HttpRequestException("The confirmed income receipt response was empty.");
    }

    /// <inheritdoc/>
    public async Task<IncomeReceiptResponse> ReviseAsync(Guid receiptId, ConfirmIncomeReceiptRequest request, CancellationToken cancellationToken)
    {
        var payload = new UpdateIncomeReceiptRequest(request.ReceivedDate, request.Amount, request.Allocations);
        using var response = await httpClient.PutAsJsonAsync(GetReceiptRoute(receiptId), payload, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IncomeReceiptResponse>(cancellationToken)
            ?? throw new HttpRequestException("The updated income receipt response was empty.");
    }

    /// <inheritdoc/>
    public async Task<TransactionMutationResult> RemoveAsync(Guid receiptId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.DeleteAsync(GetReceiptRoute(receiptId), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return TransactionMutationResult.NotFound;
        }

        response.EnsureSuccessStatusCode();
        return TransactionMutationResult.Success;
    }

    private static string GetReceiptRoute(Guid receiptId) => $"api/v1/income-receipts/{receiptId}";
}
