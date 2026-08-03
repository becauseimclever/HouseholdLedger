// <copyright file="W3cWebDriver.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.EndToEndTests;

using System.Net.Http.Json;
using System.Text.Json;

internal sealed class W3cWebDriver : IAsyncDisposable
{
    private const string ElementIdentifier = "element-6066-11e4-a52e-4f735466cecf";
    private readonly HttpClient client;
    private string? sessionId;

    public W3cWebDriver(Uri remoteAddress)
    {
        this.client = new HttpClient
        {
            BaseAddress = remoteAddress,
            Timeout = TimeSpan.FromSeconds(10),
        };
    }

    public string SessionId => this.sessionId
        ?? throw new InvalidOperationException("A WebDriver session has not been created.");

    public async Task<JsonElement> GetStatusAsync(CancellationToken cancellationToken)
    {
        return await this.SendAsync(HttpMethod.Get, "/status", null, cancellationToken);
    }

    public async Task<JsonElement> CreateSessionAsync(
        string firefoxBinary,
        string profilePath,
        IReadOnlyDictionary<string, object> preferences,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            capabilities = new
            {
                alwaysMatch = new Dictionary<string, object>
                {
                    ["browserName"] = "firefox",
                    ["acceptInsecureCerts"] = false,
                    ["pageLoadStrategy"] = "eager",
                    ["moz:firefoxOptions"] = new
                    {
                        binary = firefoxBinary,
                        args = new[] { "-headless", "-profile", profilePath },
                        prefs = preferences,
                    },
                },
            },
        };

        var value = await this.SendAsync(HttpMethod.Post, "/session", payload, cancellationToken);
        this.sessionId = value.GetProperty("sessionId").GetString()
            ?? throw new InvalidOperationException("geckodriver did not return a session identifier.");
        return value.GetProperty("capabilities").Clone();
    }

    public async Task NavigateAsync(Uri address, CancellationToken cancellationToken)
    {
        await this.SendAsync(
            HttpMethod.Post,
            $"/session/{this.SessionId}/url",
            new { url = address.AbsoluteUri },
            cancellationToken);
    }

    public async Task SetWindowRectAsync(int width, int height, CancellationToken cancellationToken)
    {
        await this.SendAsync(
            HttpMethod.Post,
            $"/session/{this.SessionId}/window/rect",
            new { width, height },
            cancellationToken);
    }

    public async Task<string> FindElementAsync(string cssSelector, CancellationToken cancellationToken)
    {
        var value = await this.SendAsync(
            HttpMethod.Post,
            $"/session/{this.SessionId}/element",
            new { @using = "css selector", value = cssSelector },
            cancellationToken);
        return value.GetProperty(ElementIdentifier).GetString()
            ?? throw new InvalidOperationException($"Element '{cssSelector}' had no W3C identifier.");
    }

    public async Task ClickAsync(string elementId, CancellationToken cancellationToken)
    {
        await this.SendAsync(
            HttpMethod.Post,
            $"/session/{this.SessionId}/element/{elementId}/click",
            new { },
            cancellationToken);
    }

    public async Task SendKeysAsync(string elementId, string text, CancellationToken cancellationToken)
    {
        await this.SendAsync(
            HttpMethod.Post,
            $"/session/{this.SessionId}/element/{elementId}/value",
            new { text },
            cancellationToken);
    }

    public async Task<JsonElement> ExecuteScriptAsync(
        string script,
        IReadOnlyList<object>? arguments,
        CancellationToken cancellationToken)
    {
        return await this.SendAsync(
            HttpMethod.Post,
            $"/session/{this.SessionId}/execute/sync",
            new { script, args = arguments ?? Array.Empty<object>() },
            cancellationToken);
    }

    public async Task<JsonElement> ExecuteAsyncScriptAsync(
        string script,
        IReadOnlyList<object>? arguments,
        CancellationToken cancellationToken)
    {
        return await this.SendAsync(
            HttpMethod.Post,
            $"/session/{this.SessionId}/execute/async",
            new { script, args = arguments ?? Array.Empty<object>() },
            cancellationToken);
    }

    public async Task<byte[]> TakeScreenshotAsync(CancellationToken cancellationToken)
    {
        var value = await this.SendAsync(
            HttpMethod.Get,
            $"/session/{this.SessionId}/screenshot",
            null,
            cancellationToken);
        return Convert.FromBase64String(value.GetString()
            ?? throw new InvalidOperationException("The screenshot response was empty."));
    }

    public async ValueTask DisposeAsync()
    {
        if (this.sessionId is not null)
        {
            try
            {
                await this.SendAsync(
                    HttpMethod.Delete,
                    $"/session/{this.sessionId}",
                    null,
                    CancellationToken.None);
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }

            this.sessionId = null;
        }

        this.client.Dispose();
    }

    private async Task<JsonElement> SendAsync(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        using var response = await this.client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"WebDriver {method} {path} returned HTTP {(int)response.StatusCode}: {body}");
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        if (!root.TryGetProperty("value", out var value))
        {
            throw new InvalidOperationException($"WebDriver {method} {path} returned no value: {body}");
        }

        return value.Clone();
    }
}
