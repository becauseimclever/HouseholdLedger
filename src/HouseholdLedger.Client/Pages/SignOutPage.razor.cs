// <copyright file="SignOutPage.razor.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Pages;

using System.Net.Http.Json;
using System.Text.Json;
using HouseholdLedger.Client.Api;
using Microsoft.AspNetCore.Components;

/// <summary>Provides a verified native form for ending the hosted session.</summary>
public partial class SignOutPage : ComponentBase
{
    private HostedSession? session;
    private bool error;

    [Inject]
    private HttpClient Client { get; set; } = null!;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        try
        {
            this.session = await this.Client.GetFromJsonAsync<HostedSession>("auth/session");
            this.error = this.session is null
                || (this.session.AuthenticationEnabled && string.IsNullOrWhiteSpace(this.session.RequestToken));
        }
        catch (HttpRequestException)
        {
            this.error = true;
        }
        catch (JsonException)
        {
            this.error = true;
        }
    }
}
