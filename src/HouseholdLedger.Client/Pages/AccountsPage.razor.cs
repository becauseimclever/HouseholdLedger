// <copyright file="AccountsPage.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Pages;

using System.Text.Json;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using Microsoft.AspNetCore.Components;

/// <summary>Presents the authoritative account catalog and creation form.</summary>
public partial class AccountsPage : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private IReadOnlyList<AccountResponse> accounts = [];
    private bool isLoading = true;
    private bool isSaving;
    private bool loadError;
    private string nameText = string.Empty;
    private string? nameError;
    private string? saveError;
    private string? saveStatus;

    /// <summary>Gets or sets the account API client.</summary>
    [Inject]
    public IAccountsApiClient AccountsApiClient { get; set; } = null!;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.lifetimeCancellation.Cancel();
        this.lifetimeCancellation.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await this.LoadAsync();
    }

    private static bool IsApiFailure(Exception exception) =>
        exception is HttpRequestException or JsonException or NotSupportedException
        || exception is OperationCanceledException;

    private bool IsCreateDisabled() => this.isLoading || this.loadError || this.isSaving;

    private void UpdateName(ChangeEventArgs eventArgs)
    {
        this.nameText = eventArgs.Value?.ToString() ?? string.Empty;
        this.nameError = null;
    }

    private async Task LoadAsync()
    {
        this.isLoading = true;
        this.loadError = false;

        try
        {
            this.accounts = await this.AccountsApiClient.ListAsync(this.lifetimeCancellation.Token);
        }
        catch (Exception exception) when (IsApiFailure(exception))
        {
            this.accounts = [];
            this.loadError = true;
        }
        finally
        {
            this.isLoading = false;
        }
    }

    private async Task CreateAsync()
    {
        this.nameError = null;
        this.saveError = null;
        this.saveStatus = null;
        var trimmedName = this.nameText.Trim();
        if (trimmedName.Length == 0)
        {
            this.nameError = "Enter an account name.";
            return;
        }

        if (trimmedName.Length > 100)
        {
            this.nameError = "Account names cannot exceed 100 characters.";
            return;
        }

        this.isSaving = true;
        try
        {
            var result = await this.AccountsApiClient.CreateAsync(
                new CreateAccountRequest(trimmedName),
                this.lifetimeCancellation.Token);
            if (result == AccountCreationResult.Invalid)
            {
                this.nameError = "Use a unique account name between 1 and 100 characters.";
                return;
            }

            this.accounts = await this.AccountsApiClient.ListAsync(this.lifetimeCancellation.Token);
            this.nameText = string.Empty;
            this.saveStatus = "Account created.";
        }
        catch (Exception exception) when (IsApiFailure(exception))
        {
            this.saveError = "The account could not be saved. Try again.";
        }
        finally
        {
            this.isSaving = false;
        }
    }
}
