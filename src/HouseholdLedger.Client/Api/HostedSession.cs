// <copyright file="HostedSession.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using System.Text.Json.Serialization;

/// <summary>Describes the hosted browser session.</summary>
/// <param name="AuthenticationEnabled">Whether the host requires authentication.</param>
/// <param name="RequestToken">The framework antiforgery request token, when required.</param>
public sealed record HostedSession([property: JsonRequired] bool AuthenticationEnabled, string? RequestToken);
