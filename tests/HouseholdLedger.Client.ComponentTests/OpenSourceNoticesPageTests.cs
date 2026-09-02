// <copyright file="OpenSourceNoticesPageTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using Bunit;
using HouseholdLedger.Client.Layout;
using HouseholdLedger.Client.OpenSourceNotices;
using HouseholdLedger.Client.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Verifies the rendered open-source notices content.
/// </summary>
public sealed class OpenSourceNoticesPageTests
{
    private static readonly (string PackageName, string Version, string PublicationForm)[] ExpectedPublishedThirdPartyClosure =
    [
        ("Blazicons.Lucide", "3.0.8", "Runtime assembly"),
        ("Blazicons", "4.0.21", "Both"),
        ("BlazorComponentUtilities", "1.8.0", "Runtime assembly"),
        ("Npgsql", "10.0.3", "Runtime assembly"),
        ("Npgsql.EntityFrameworkCore.PostgreSQL", "10.0.3", "Runtime assembly"),
    ];

    /// <summary>
    /// Verifies that the Blazicons.Lucide notice renders its complete manifest content.
    /// </summary>
    [Fact]
    public void PageRendersCompleteBlaziconsLucideManifestNotice()
    {
        using var context = new BunitContext();
        var component = context.Render<OpenSourceNoticesPage>();
        var manifestNotice = OpenSourceNoticesManifest.Notices.Single(notice => notice.PackageName == "Blazicons.Lucide");
        var entry = component.Find(".open-source-notice-entry");

        var assertions = new List<Action>
        {
            () => Assert.Equal(manifestNotice.PackageName, entry.QuerySelector("h3")?.TextContent),
            () => Assert.Equal($"Version {manifestNotice.Version}", entry.QuerySelector(".open-source-notice-version")?.TextContent),
            () => Assert.Equal("Runtime assembly", entry.QuerySelector("dd")?.TextContent),
            () => Assert.Equal(manifestNotice.RequiredNotices.Count, entry.QuerySelectorAll(".open-source-license-notice").Length),
        };

        for (var index = 0; index < manifestNotice.RequiredNotices.Count; index++)
        {
            var renderedNotice = entry.QuerySelectorAll(".open-source-license-notice")[index];
            var expectedNotice = manifestNotice.RequiredNotices[index];

            assertions.Add(() => Assert.Equal(expectedNotice.ComponentName, renderedNotice.QuerySelector("h5")?.TextContent));
            assertions.Add(() => Assert.Equal(
                NormalizeLineEndings(expectedNotice.LicenseText),
                NormalizeLineEndings(renderedNotice.QuerySelector("pre")?.TextContent)));
        }

        Assert.Multiple(assertions.ToArray());
    }

    /// <summary>
    /// Verifies that every source-controlled published third-party entry and its complete required notices render.
    /// </summary>
    [Fact]
    public void PageRendersExactPublishedThirdPartyClosureWithCompleteNoticeText()
    {
        using var context = new BunitContext();
        var component = context.Render<OpenSourceNoticesPage>();
        var entries = component.FindAll(".open-source-notice-entry");

        Assert.Equal(ExpectedPublishedThirdPartyClosure.Length, entries.Count);

        var assertions = new List<Action>();

        foreach (var expected in ExpectedPublishedThirdPartyClosure)
        {
            var manifestNotice = OpenSourceNoticesManifest.Notices.Single(notice => notice.PackageName == expected.PackageName);
            var entry = entries.Single(candidate => candidate.QuerySelector("h3")?.TextContent == expected.PackageName);
            var renderedNotices = entry.QuerySelectorAll(".open-source-license-notice");

            assertions.AddRange(
            [
                () => Assert.Equal($"Version {expected.Version}", entry.QuerySelector(".open-source-notice-version")?.TextContent),
                () => Assert.Equal(expected.PublicationForm, entry.QuerySelector("dd")?.TextContent),
                () => Assert.Equal(manifestNotice.ProjectUrl.AbsoluteUri, entry.QuerySelector("a")?.GetAttribute("href")),
                () => Assert.Equal("_blank", entry.QuerySelector("a")?.GetAttribute("target")),
                () => Assert.Equal("noopener noreferrer", entry.QuerySelector("a")?.GetAttribute("rel")),
                () => Assert.Equal(manifestNotice.RequiredNotices.Count, renderedNotices.Length),
            ]);

            for (var index = 0; index < manifestNotice.RequiredNotices.Count; index++)
            {
                var renderedNotice = renderedNotices[index];
                var expectedNotice = manifestNotice.RequiredNotices[index];

                assertions.Add(() => Assert.Equal(expectedNotice.ComponentName, renderedNotice.QuerySelector("h5")?.TextContent));
                assertions.Add(() => Assert.Equal(expectedNotice.CopyrightNotice, renderedNotice.QuerySelectorAll("p")[1].TextContent));
                assertions.Add(() => Assert.Equal(
                    NormalizeLineEndings(expectedNotice.LicenseText),
                    NormalizeLineEndings(renderedNotice.QuerySelector("pre")?.TextContent)));
                assertions.Add(() => Assert.Equal(expectedNotice.SourceUrl.AbsoluteUri, renderedNotice.QuerySelector("a")?.GetAttribute("href")));
                assertions.Add(() => Assert.Equal("License or source notice", renderedNotice.QuerySelector("a")?.TextContent));
            }
        }

        Assert.Multiple(assertions.ToArray());
    }

    /// <summary>
    /// Verifies the page's landmarks, heading hierarchy, scope statement, and output exclusions.
    /// </summary>
    [Fact]
    public void PageHasOneMainHeadingAndRendersManifestExclusions()
    {
        using var context = new BunitContext();
        var component = context.Render<OpenSourceNoticesPage>();

        var assertions = new List<Action>
        {
            () => Assert.Single(component.FindAll("main.open-source-notices-page")),
            () => Assert.Single(component.FindAll("main h1")),
            () => Assert.Equal("Open-source notices", component.Find("h1").TextContent),
            () => Assert.Equal(OpenSourceNoticesManifest.ScopeStatement, component.Find(".open-source-notices-scope").TextContent),
            () => Assert.Equal(2, component.FindAll("h2").Count),
            () => Assert.Equal(2, component.FindAll(".open-source-notices-exclusion-list > li").Count),
        };

        foreach (var exclusion in OpenSourceNoticesManifest.Exclusions)
        {
            var renderedExclusion = component.FindAll(".open-source-notices-exclusion-list > li")
                .Single(candidate => candidate.QuerySelector("h3")?.TextContent == exclusion.Name);

            assertions.Add(() => Assert.Contains(exclusion.Reason, renderedExclusion.TextContent, StringComparison.Ordinal));
        }

        Assert.Multiple(assertions.ToArray());
    }

    /// <summary>
    /// Verifies that the current notices route has one auxiliary native link after the workspace grid and no navigation destination.
    /// </summary>
    [Fact]
    public void CurrentNoticesRouteUsesOneAuxiliaryLinkOutsideEmptyPrimaryNavigation()
    {
        using var context = new BunitContext();
        context.Services.AddScoped<HouseholdLedger.Client.State.SelectedDateState>();
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/open-source-notices");

        var component = context.Render<MainLayout>();
        var link = component.Find(".workspace-auxiliary-link");

        Assert.Multiple(
            () => Assert.Single(component.FindAll(".workspace-auxiliary-link")),
            () => Assert.Equal("A", link.TagName),
            () => Assert.Equal("Open-source notices", link.TextContent),
            () => Assert.Equal("/open-source-notices", link.GetAttribute("href")),
            () => Assert.Equal("page", link.GetAttribute("aria-current")),
            () => Assert.Empty(component.FindAll("#workspace-navigation a, #workspace-navigation button")),
            () => Assert.Equal("Following", component.Find(".workspace-grid").CompareDocumentPosition(link).ToString()));
    }

    private static string NormalizeLineEndings(string? text) => text?.Replace("\r\n", "\n", StringComparison.Ordinal) ?? string.Empty;
}
