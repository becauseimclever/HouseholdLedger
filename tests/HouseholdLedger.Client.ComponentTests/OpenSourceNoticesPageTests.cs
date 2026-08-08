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
using NUnit.Framework;

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
    [Test]
    public void PageRendersCompleteBlaziconsLucideManifestNotice()
    {
        using var context = new BunitContext();
        var component = context.Render<OpenSourceNoticesPage>();
        var manifestNotice = OpenSourceNoticesManifest.Notices.Single(notice => notice.PackageName == "Blazicons.Lucide");
        var entry = component.Find(".open-source-notice-entry");

        Assert.Multiple(() =>
        {
            Assert.That(entry.QuerySelector("h3")?.TextContent, Is.EqualTo(manifestNotice.PackageName));
            Assert.That(entry.QuerySelector(".open-source-notice-version")?.TextContent, Is.EqualTo($"Version {manifestNotice.Version}"));
            Assert.That(entry.QuerySelector("dd")?.TextContent, Is.EqualTo("Runtime assembly"));
            Assert.That(entry.QuerySelectorAll(".open-source-license-notice"), Has.Length.EqualTo(manifestNotice.RequiredNotices.Count));

            for (var index = 0; index < manifestNotice.RequiredNotices.Count; index++)
            {
                var renderedNotice = entry.QuerySelectorAll(".open-source-license-notice")[index];
                var expectedNotice = manifestNotice.RequiredNotices[index];

                Assert.That(renderedNotice.QuerySelector("h5")?.TextContent, Is.EqualTo(expectedNotice.ComponentName));
                Assert.That(
                    NormalizeLineEndings(renderedNotice.QuerySelector("pre")?.TextContent),
                    Is.EqualTo(NormalizeLineEndings(expectedNotice.LicenseText)));
            }
        });
    }

    /// <summary>
    /// Verifies that every source-controlled published third-party entry and its complete required notices render.
    /// </summary>
    [Test]
    public void PageRendersExactPublishedThirdPartyClosureWithCompleteNoticeText()
    {
        using var context = new BunitContext();
        var component = context.Render<OpenSourceNoticesPage>();
        var entries = component.FindAll(".open-source-notice-entry");

        Assert.That(entries, Has.Count.EqualTo(ExpectedPublishedThirdPartyClosure.Length));

        foreach (var expected in ExpectedPublishedThirdPartyClosure)
        {
            var manifestNotice = OpenSourceNoticesManifest.Notices.Single(notice => notice.PackageName == expected.PackageName);
            var entry = entries.Single(candidate => candidate.QuerySelector("h3")?.TextContent == expected.PackageName);
            var renderedNotices = entry.QuerySelectorAll(".open-source-license-notice");

            Assert.Multiple(() =>
            {
                Assert.That(entry.QuerySelector(".open-source-notice-version")?.TextContent, Is.EqualTo($"Version {expected.Version}"));
                Assert.That(entry.QuerySelector("dd")?.TextContent, Is.EqualTo(expected.PublicationForm));
                Assert.That(entry.QuerySelector("a")?.GetAttribute("href"), Is.EqualTo(manifestNotice.ProjectUrl.AbsoluteUri));
                Assert.That(entry.QuerySelector("a")?.GetAttribute("target"), Is.EqualTo("_blank"));
                Assert.That(entry.QuerySelector("a")?.GetAttribute("rel"), Is.EqualTo("noopener noreferrer"));
                Assert.That(renderedNotices, Has.Length.EqualTo(manifestNotice.RequiredNotices.Count));

                for (var index = 0; index < manifestNotice.RequiredNotices.Count; index++)
                {
                    var renderedNotice = renderedNotices[index];
                    var expectedNotice = manifestNotice.RequiredNotices[index];

                    Assert.That(renderedNotice.QuerySelector("h5")?.TextContent, Is.EqualTo(expectedNotice.ComponentName));
                    Assert.That(renderedNotice.QuerySelectorAll("p")[1].TextContent, Is.EqualTo(expectedNotice.CopyrightNotice));
                    Assert.That(
                        NormalizeLineEndings(renderedNotice.QuerySelector("pre")?.TextContent),
                        Is.EqualTo(NormalizeLineEndings(expectedNotice.LicenseText)));
                    Assert.That(renderedNotice.QuerySelector("a")?.GetAttribute("href"), Is.EqualTo(expectedNotice.SourceUrl.AbsoluteUri));
                    Assert.That(renderedNotice.QuerySelector("a")?.TextContent, Is.EqualTo("License or source notice"));
                }
            });
        }
    }

    /// <summary>
    /// Verifies the page's landmarks, heading hierarchy, scope statement, and output exclusions.
    /// </summary>
    [Test]
    public void PageHasOneMainHeadingAndRendersManifestExclusions()
    {
        using var context = new BunitContext();
        var component = context.Render<OpenSourceNoticesPage>();

        Assert.Multiple(() =>
        {
            Assert.That(component.FindAll("main.open-source-notices-page"), Has.Count.EqualTo(1));
            Assert.That(component.FindAll("main h1"), Has.Count.EqualTo(1));
            Assert.That(component.Find("h1").TextContent, Is.EqualTo("Open-source notices"));
            Assert.That(component.Find(".open-source-notices-scope").TextContent, Is.EqualTo(OpenSourceNoticesManifest.ScopeStatement));
            Assert.That(component.FindAll("h2"), Has.Count.EqualTo(2));
            Assert.That(component.FindAll(".open-source-notices-exclusion-list > li"), Has.Count.EqualTo(2));

            foreach (var exclusion in OpenSourceNoticesManifest.Exclusions)
            {
                var renderedExclusion = component.FindAll(".open-source-notices-exclusion-list > li")
                    .Single(candidate => candidate.QuerySelector("h3")?.TextContent == exclusion.Name);

                Assert.That(renderedExclusion.TextContent, Does.Contain(exclusion.Reason));
            }
        });
    }

    /// <summary>
    /// Verifies that the current notices route has one auxiliary native link after the workspace grid and no navigation destination.
    /// </summary>
    [Test]
    public void CurrentNoticesRouteUsesOneAuxiliaryLinkOutsideEmptyPrimaryNavigation()
    {
        using var context = new BunitContext();
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/open-source-notices");

        var component = context.Render<MainLayout>();
        var link = component.Find(".workspace-auxiliary-link");

        Assert.Multiple(() =>
        {
            Assert.That(component.FindAll(".workspace-auxiliary-link"), Has.Count.EqualTo(1));
            Assert.That(link.TagName, Is.EqualTo("A"));
            Assert.That(link.TextContent, Is.EqualTo("Open-source notices"));
            Assert.That(link.GetAttribute("href"), Is.EqualTo("/open-source-notices"));
            Assert.That(link.GetAttribute("aria-current"), Is.EqualTo("page"));
            Assert.That(component.FindAll("#workspace-navigation a, #workspace-navigation button"), Is.Empty);
            Assert.That(component.Find(".workspace-grid").CompareDocumentPosition(link).ToString(), Is.EqualTo("Following"));
        });
    }

    private static string NormalizeLineEndings(string? text) => text?.Replace("\r\n", "\n", StringComparison.Ordinal) ?? string.Empty;
}
