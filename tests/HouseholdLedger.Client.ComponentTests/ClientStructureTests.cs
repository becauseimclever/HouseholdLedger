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
