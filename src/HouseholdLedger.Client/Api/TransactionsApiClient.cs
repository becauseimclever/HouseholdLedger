// <copyright file="TransactionsApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using System.Net;
using System.Net.Http.Json;
using HouseholdLedger.Api.Contracts;

/// <summary>
/// Calls the versioned selected-day transaction endpoints.
/// </summary>
public sealed class TransactionsApiClient(HttpClient httpClient) : ITransactionsApiClient
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<ExpenseTransactionResponse>> ListAsync(
        DateOnly ledgerDate,
        CancellationToken cancellationToken)
    {
        var route = GetRoute(ledgerDate);
        return await httpClient.GetFromJsonAsync<ExpenseTransactionResponse[]>(route, cancellationToken) ?? [];
    }

    /// <inheritdoc/>
    public async Task<bool> CreateAsync(
        DateOnly ledgerDate,
        CreateExpenseTransactionRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(GetRoute(ledgerDate), request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    /// <inheritdoc/>
    public async Task<TransactionMutationResult> ReviseAsync(
        DateOnly ledgerDate,
        Guid transactionId,
        UpdateExpenseTransactionRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PutAsJsonAsync(
            GetTransactionRoute(ledgerDate, transactionId),
            request,
            cancellationToken);
        return GetMutationResult(response);
    }

    /// <inheritdoc/>
    public async Task<TransactionMutationResult> RemoveAsync(
        DateOnly ledgerDate,
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.DeleteAsync(
            GetTransactionRoute(ledgerDate, transactionId),
            cancellationToken);
        return GetMutationResult(response);
    }

    private static TransactionMutationResult GetMutationResult(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return TransactionMutationResult.Invalid;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return TransactionMutationResult.NotFound;
        }

        response.EnsureSuccessStatusCode();
        return TransactionMutationResult.Success;
    }

    private static string GetRoute(DateOnly ledgerDate) =>
        $"api/v1/days/{ledgerDate:yyyy-MM-dd}/transactions";

    private static string GetTransactionRoute(DateOnly ledgerDate, Guid transactionId) =>
        $"{GetRoute(ledgerDate)}/{transactionId}";
}
