// <copyright file="IIncomeReceiptsApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using HouseholdLedger.Api.Contracts;

/// <summary>Records only explicitly confirmed actual income.</summary>
public interface IIncomeReceiptsApiClient
{
    /// <summary>Confirms a receipt with its actual date and account destinations.</summary>
    /// <param name="request">The user-confirmed receipt.</param>
    /// <param name="requestId">The retry identifier retained for an unchanged confirmation payload.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The durable receipt.</returns>
    Task<IncomeReceiptResponse> ConfirmAsync(ConfirmIncomeReceiptRequest request, Guid requestId, CancellationToken cancellationToken);

    /// <summary>Corrects a previously confirmed receipt.</summary>
    /// <param name="receiptId">The receipt identifier.</param>
    /// <param name="request">The corrected receipt details.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The corrected receipt.</returns>
    Task<IncomeReceiptResponse> ReviseAsync(Guid receiptId, ConfirmIncomeReceiptRequest request, CancellationToken cancellationToken);

    /// <summary>Removes a previously confirmed receipt.</summary>
    /// <param name="receiptId">The receipt identifier.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The mutation outcome.</returns>
    Task<TransactionMutationResult> RemoveAsync(Guid receiptId, CancellationToken cancellationToken);
}
