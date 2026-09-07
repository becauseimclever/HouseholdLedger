// <copyright file="GlobalSettingsState.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.State;

using System.Text.Json;
using HouseholdLedger.Api.Contracts;
using HouseholdLedger.Client.Api;
using HouseholdLedger.Client.Formatting;

/// <summary>Maintains authoritative client-local global settings.</summary>
public sealed class GlobalSettingsState(IGlobalSettingsApiClient apiClient) : IDisposable
{
    private readonly object loadLock = new();
    private readonly SemaphoreSlim saveLock = new(1, 1);
    private Task? loadTask;
    private long requestVersion;

    /// <summary>Occurs when loading, saving, or authoritative settings change.</summary>
    public event Action? Changed;

    /// <summary>Gets the effective currency code.</summary>
    public string CurrentCode { get; private set; } = MoneyFormatter.DefaultCurrencyCode;

    /// <summary>Gets the effective theme identifier.</summary>
    public string CurrentTheme { get; private set; } = ThemeCatalog.DefaultTheme;

    /// <summary>Gets a value indicating whether authoritative settings have loaded.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Gets a value indicating whether settings are loading.</summary>
    public bool IsLoading { get; private set; }

    /// <summary>Gets a value indicating whether the most recent load failed.</summary>
    public bool LoadFailed { get; private set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.saveLock.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Loads settings once unless a retry is requested.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the load.</returns>
    public Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (this.IsLoaded)
        {
            return Task.CompletedTask;
        }

        return this.GetOrStartLoad(cancellationToken);
    }

    /// <summary>Retries loading authoritative settings.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the load.</returns>
    public Task ReloadAsync(CancellationToken cancellationToken) => this.GetOrStartLoad(cancellationToken);

    /// <summary>Persists and publishes a supported display currency.</summary>
    /// <param name="currencyCode">The supported currency code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authoritative saved currency code.</returns>
    public async Task<string> SaveCurrencyAsync(string currencyCode, CancellationToken cancellationToken)
    {
        if (!MoneyFormatter.IsSupported(currencyCode))
        {
            throw new ArgumentException("Choose a supported display currency.", nameof(currencyCode));
        }

        var response = await this.SaveAsync(
            token => apiClient.UpdateCurrencyAsync(currencyCode, token),
            cancellationToken);
        return response.DisplayCurrency;
    }

    /// <summary>Persists and publishes a supported workbench theme.</summary>
    /// <param name="theme">The supported theme identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authoritative saved theme identifier.</returns>
    public async Task<string> SaveThemeAsync(string theme, CancellationToken cancellationToken)
    {
        if (!ThemeCatalog.IsSupported(theme))
        {
            throw new ArgumentException("Choose a supported workbench theme.", nameof(theme));
        }

        var response = await this.SaveAsync(
            token => apiClient.UpdateThemeAsync(theme, token),
            cancellationToken);
        return response.Theme;
    }

    private static bool IsApiFailure(Exception exception) =>
        exception is HttpRequestException or JsonException or NotSupportedException
        || exception is OperationCanceledException;

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var version = ++this.requestVersion;
        this.IsLoading = true;
        this.LoadFailed = false;
        this.Changed?.Invoke();

        try
        {
            var response = await apiClient.GetAsync(cancellationToken);
            if (version == this.requestVersion)
            {
                this.Publish(response);
            }
        }
        catch (Exception exception) when (IsApiFailure(exception))
        {
            if (version == this.requestVersion && !cancellationToken.IsCancellationRequested)
            {
                this.LoadFailed = true;
            }
        }
        finally
        {
            if (version == this.requestVersion)
            {
                this.IsLoading = false;
                this.Changed?.Invoke();
            }
        }
    }

    private Task GetOrStartLoad(CancellationToken cancellationToken)
    {
        lock (this.loadLock)
        {
            if (this.loadTask is not null)
            {
                return this.loadTask;
            }

            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            this.loadTask = completion.Task;
            _ = this.RunLoadAsync(completion, cancellationToken);
            return completion.Task;
        }
    }

    private async Task RunLoadAsync(TaskCompletionSource completion, CancellationToken cancellationToken)
    {
        try
        {
            await this.LoadAsync(cancellationToken);
            completion.SetResult();
        }
        catch (Exception exception)
        {
            completion.SetException(exception);
        }
        finally
        {
            lock (this.loadLock)
            {
                this.loadTask = null;
            }
        }
    }

    private async Task<GlobalSettingsResponse> SaveAsync(
        Func<CancellationToken, Task<GlobalSettingsResponse>> update,
        CancellationToken cancellationToken)
    {
        await this.saveLock.WaitAsync(cancellationToken);
        try
        {
            this.requestVersion++;
            var response = await update(cancellationToken);
            this.Publish(response);
            return response;
        }
        finally
        {
            this.saveLock.Release();
        }
    }

    private void Publish(GlobalSettingsResponse response)
    {
        if (!MoneyFormatter.IsSupported(response.DisplayCurrency))
        {
            throw new JsonException("The server returned an unsupported display currency.");
        }

        if (!ThemeCatalog.IsSupported(response.Theme))
        {
            throw new JsonException("The server returned an unsupported workbench theme.");
        }

        this.CurrentCode = response.DisplayCurrency.ToUpperInvariant();
        this.CurrentTheme = response.Theme.ToLowerInvariant();
        this.IsLoaded = true;
        this.LoadFailed = false;
        this.Changed?.Invoke();
    }
}
