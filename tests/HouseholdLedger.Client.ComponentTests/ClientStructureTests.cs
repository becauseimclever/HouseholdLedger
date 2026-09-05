// <copyright file="ClientStructureTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using System.Text.RegularExpressions;

using HouseholdLedger.Client;
using Microsoft.AspNetCore.Components;
using Xunit;

/// <summary>
/// Verifies structural rules for the replaceable client.
/// </summary>
public sealed class ClientStructureTests
{
    private static readonly string[] ExpectedProductReferences = ["HouseholdLedger.Api.Contracts"];
    private static readonly string[] RequiredThemeTokens =
    [
        "--hl-surface-canvas",
        "--hl-surface-chrome",
        "--hl-surface-work",
        "--hl-surface-panel",
        "--hl-surface-input",
        "--hl-text-primary",
        "--hl-text-secondary",
        "--hl-text-link",
        "--hl-border-default",
        "--hl-border-input",
        "--hl-action-primary",
        "--hl-action-focus",
        "--hl-status-success",
        "--hl-status-warning",
        "--hl-status-error",
        "--hl-status-information",
        "--hl-font-ui",
        "--hl-font-monospace",
        "--hl-radius-control",
        "--hl-transition-short",
        "--hl-workspace-areas",
        "--hl-workspace-columns",
        "--hl-workspace-no-navigation-areas",
        "--hl-workspace-no-inspector-areas",
        "--hl-workspace-main-only-areas",
    ];

    /// <summary>
    /// Verifies that every Razor file uses a matching compiled partial code-behind type.
    /// </summary>
    [Fact]
    public void EveryRazorFileHasMatchingPartialCodeBehindAndNoInlineCodeBlock()
    {
        var clientDirectory = FindClientDirectory();
        var razorFiles = Directory.GetFiles(clientDirectory, "*.razor", SearchOption.AllDirectories);

        Assert.NotEmpty(razorFiles);
        var assertions = new List<Action>();
        foreach (var razorFile in razorFiles)
        {
            var codeBehindFile = razorFile + ".cs";
            var relativeTypeName = Path.GetRelativePath(clientDirectory, razorFile)[..^".razor".Length]
                .Replace(Path.DirectorySeparatorChar, '.');
            var expectedTypeName = $"HouseholdLedger.Client.{relativeTypeName}";
            var componentType = typeof(App).Assembly.GetType(expectedTypeName);

            assertions.Add(() => Assert.True(File.Exists(codeBehindFile), $"Missing code-behind for {razorFile}"));
            assertions.Add(() => Assert.DoesNotContain("@code", File.ReadAllText(razorFile), StringComparison.Ordinal));
            assertions.Add(() => Assert.Matches(
                $@"\bpublic\s+partial\s+class\s+{Regex.Escape(Path.GetFileNameWithoutExtension(razorFile))}\b",
                File.ReadAllText(codeBehindFile)));
            assertions.Add(() => Assert.NotNull(componentType));
            assertions.Add(() => Assert.True(
                typeof(ComponentBase).IsAssignableFrom(componentType!),
                $"Compiled Razor type is not a component: {expectedTypeName}"));
        }

        Assert.Multiple(assertions.ToArray());
    }

    /// <summary>
    /// Verifies that the client has no HouseholdLedger implementation assembly reference.
    /// </summary>
    [Fact]
    public void ClientReferencesOnlyApiContractsWithinProductAssemblies()
    {
        var householdLedgerReferences = typeof(App).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name?.StartsWith("HouseholdLedger.", StringComparison.Ordinal) == true);

        Assert.Equivalent(ExpectedProductReferences, householdLedgerReferences);
    }

    /// <summary>Verifies Workbench Dark is established before WebAssembly starts.</summary>
    [Fact]
    public void StaticHostDeclaresWorkbenchDarkThemeAndMatchingBrowserChrome()
    {
        var index = File.ReadAllText(Path.Combine(FindClientDirectory(), "wwwroot", "index.html"));

        Assert.Multiple(
            () => Assert.Contains("data-theme=\"workbench-dark\"", index, StringComparison.Ordinal),
            () => Assert.Contains("name=\"theme-color\" content=\"#181818\"", index, StringComparison.Ordinal),
            () => Assert.Contains("href=\"HouseholdLedger.Api.styles.css\"", index, StringComparison.Ordinal));
    }

    /// <summary>Verifies all scoped styles consume the single semantic token contract.</summary>
    [Fact]
    public void ClientStylesUseOnlySemanticThemeTokensForRenderedColors()
    {
        var clientDirectory = FindClientDirectory();
        var appCss = File.ReadAllText(Path.Combine(clientDirectory, "wwwroot", "css", "app.css"));
        var scopedStyleFiles = Directory.GetFiles(clientDirectory, "*.razor.css", SearchOption.AllDirectories);
        var assertions = RequiredThemeTokens
            .Select<string, Action>(token => () => Assert.Contains(token, appCss, StringComparison.Ordinal))
            .ToList();

        assertions.Add(() => Assert.Contains("color-scheme: dark", appCss, StringComparison.Ordinal));
        assertions.Add(() => Assert.DoesNotContain("--workspace-", appCss, StringComparison.Ordinal));
        assertions.Add(() => Assert.DoesNotContain("--calendar-", appCss, StringComparison.Ordinal));
        foreach (var styleFile in scopedStyleFiles)
        {
            var css = File.ReadAllText(styleFile);
            assertions.Add(() => Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|\brgb\(|\bhsl\(", css));
            assertions.Add(() => Assert.DoesNotContain("--workspace-", css, StringComparison.Ordinal));
            assertions.Add(() => Assert.DoesNotContain("--calendar-", css, StringComparison.Ordinal));
        }

        Assert.Multiple(assertions.ToArray());
    }

    /// <summary>Verifies theme placement does not depend on markup order or side-specific controls.</summary>
    [Fact]
    public void WorkspaceUsesStableSemanticOrderAndThemeOwnedNamedAreas()
    {
        var clientDirectory = FindClientDirectory();
        var markup = File.ReadAllText(Path.Combine(clientDirectory, "Layout", "MainLayout.razor"));
        var layoutCss = File.ReadAllText(Path.Combine(clientDirectory, "Layout", "MainLayout.razor.css"));
        var navigationIndex = markup.IndexOf("<nav", StringComparison.Ordinal);
        var mainIndex = markup.IndexOf("<section id=\"calendar-workspace\"", StringComparison.Ordinal);
        var inspectorIndex = markup.IndexOf("<aside", StringComparison.Ordinal);

        Assert.Multiple(
            () => Assert.True(navigationIndex >= 0 && navigationIndex < mainIndex && mainIndex < inspectorIndex),
            () => Assert.Contains("grid-area: navigation", layoutCss, StringComparison.Ordinal),
            () => Assert.Contains("grid-area: main", layoutCss, StringComparison.Ordinal),
            () => Assert.Contains("grid-area: inspector", layoutCss, StringComparison.Ordinal),
            () => Assert.DoesNotContain("PanelLeft", markup, StringComparison.Ordinal),
            () => Assert.DoesNotContain("PanelRight", markup, StringComparison.Ordinal),
            () => Assert.Contains("NavigationToggleLabel", markup, StringComparison.Ordinal),
            () => Assert.Contains("InspectorToggleLabel", markup, StringComparison.Ordinal));
    }

    private static string FindClientDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "src", "HouseholdLedger.Client");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate src/HouseholdLedger.Client.");
    }
}
