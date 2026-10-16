// <copyright file="OpenIdConnectTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;

/// <summary>Exercises the actual OIDC code handler with an isolated protocol fixture, not a real provider.</summary>
public sealed class OpenIdConnectTests
{
    private const string Issuer = "https://identity.example/application/o/ledger/";

    /// <summary>Verifies signed code exchange, owner restriction, and the resulting session cookie.</summary>
    /// <param name="subject">The identity returned by the provider fixture.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData("owner")]
    [InlineData("other")]
    public async Task CodeCallbackOnlyCreatesOwnerSession(string subject)
    {
        using var rsa = RSA.Create(2048);
        var signingKey = new RsaSecurityKey(rsa) { KeyId = "fixture-signing-key" };
        Exception? protocolFailure = null;
        using var backchannel = new TokenEndpointHandler(signingKey, subject);
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Authentication:Authority", Issuer);
            builder.UseSetting("Authentication:ClientId", "ledger");
            builder.UseSetting("Authentication:ClientSecret", "fixture-only");
            builder.UseSetting("Authentication:OwnerSubject", "owner");
            builder.UseSetting("ConnectionStrings:HouseholdLedger", string.Empty);
            builder.ConfigureServices(services =>
                services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    options.Configuration = new OpenIdConnectConfiguration
                    {
                        Issuer = Issuer,
                        AuthorizationEndpoint = $"{Issuer}authorize",
                        TokenEndpoint = $"{Issuer}token",
                    };
                    options.Configuration.SigningKeys.Add(signingKey);
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(options.Configuration);
                    options.Backchannel = new HttpClient(backchannel);
                    var handleFailure = options.Events.OnRemoteFailure;
                    options.Events.OnRemoteFailure = context =>
                    {
                        protocolFailure = context.Failure;
                        return handleFailure(context);
                    };
                }));
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

        using var challenge = await client.GetAsync("/auth/login", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
        var query = QueryHelpers.ParseQuery(challenge.Headers.Location!.Query);
        Assert.Equal("code", query["response_type"].ToString());
        Assert.Equal("S256", query["code_challenge_method"].ToString());
        backchannel.Nonce = query["nonce"].ToString();

        using var callbackBody = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = "fixture-code",
            ["state"] = query["state"].ToString(),
        });
        using var callback = await client.PostAsync("/signin-oidc", callbackBody, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        if (subject == "owner")
        {
            Assert.Null(protocolFailure);
            Assert.Equal("/", callback.Headers.Location!.OriginalString);
            var session = await client.GetFromJsonAsync<SessionDetails>("/auth/session", TestContext.Current.CancellationToken);
            Assert.True(session!.AuthenticationEnabled);
            Assert.NotEmpty(session.RequestToken!);
            var cookie = callback.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("__Host-HouseholdLedger=", StringComparison.Ordinal));
            Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.Equal("/auth/error", callback.Headers.Location!.OriginalString);
            using var session = await client.GetAsync("/auth/session", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, session.StatusCode);
        }

        using var invalidReturn = await client.GetAsync("/auth/login?returnUrl=https://other.example", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalidReturn.StatusCode);
    }

    private sealed record SessionDetails(bool AuthenticationEnabled, string? RequestToken);

    private sealed class TokenEndpointHandler(SecurityKey signingKey, string subject) : HttpMessageHandler
    {
        public string Nonce { get; set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal($"{Issuer}token", request.RequestUri!.AbsoluteUri);
            var form = await request.Content!.ReadAsStringAsync(cancellationToken);
            Assert.Contains("code_verifier=", form, StringComparison.Ordinal);
            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: "ledger",
                claims:
                [
                    new Claim("sub", subject),
                    new Claim("nonce", this.Nonce),
                    new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
                ],
                notBefore: DateTime.UtcNow.AddMinutes(-1),
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    access_token = "fixture-access-token",
                    token_type = "Bearer",
                    expires_in = 300,
                    id_token = new JwtSecurityTokenHandler().WriteToken(token),
                }),
            };
        }
    }
}
