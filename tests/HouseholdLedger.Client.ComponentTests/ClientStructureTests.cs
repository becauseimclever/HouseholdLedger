// <copyright file="ClientStructureTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.ComponentTests;

using System.Text.RegularExpressions;

using HouseholdLedger.Client;
using Microsoft.AspNetCore.Components;
using NUnit.Framework;

/// <summary>
/// Verifies structural rules for the replaceable client.
/// </summary>
public sealed class ClientStructureTests
{
    private static readonly string[] ExpectedProductReferences = ["HouseholdLedger.Api.Contracts"];

    /// <summary>
    /// Verifies that every Razor file uses a matching compiled partial code-behind type.
    /// </summary>
    [Test]
    public void EveryRazorFileHasMatchingPartialCodeBehindAndNoInlineCodeBlock()
    {
        var clientDirectory = FindClientDirectory();
        var razorFiles = Directory.GetFiles(clientDirectory, "*.razor", SearchOption.AllDirectories);

        Assert.That(razorFiles, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            foreach (var razorFile in razorFiles)
            {
                var codeBehindFile = razorFile + ".cs";
                var relativeTypeName = Path.GetRelativePath(clientDirectory, razorFile)[..^".razor".Length]
                    .Replace(Path.DirectorySeparatorChar, '.');
                var expectedTypeName = $"HouseholdLedger.Client.{relativeTypeName}";

                Assert.That(File.Exists(codeBehindFile), Is.True, $"Missing code-behind for {razorFile}");
                Assert.That(File.ReadAllText(razorFile), Does.Not.Contain("@code"), $"Inline code found in {razorFile}");
                Assert.That(
                    File.ReadAllText(codeBehindFile),
                    Does.Match($@"\bpublic\s+partial\s+class\s+{Regex.Escape(Path.GetFileNameWithoutExtension(razorFile))}\b"),
                    $"Code-behind does not declare the matching partial class for {razorFile}");

                var componentType = typeof(App).Assembly.GetType(expectedTypeName);
                Assert.That(componentType, Is.Not.Null, $"Compiled Razor type not found: {expectedTypeName}");
                Assert.That(
                    typeof(ComponentBase).IsAssignableFrom(componentType!),
                    Is.True,
                    $"Compiled Razor type is not a component: {expectedTypeName}");
            }
        });
    }

    /// <summary>
    /// Verifies that the client has no HouseholdLedger implementation assembly reference.
    /// </summary>
    [Test]
    public void ClientReferencesOnlyApiContractsWithinProductAssemblies()
    {
        var householdLedgerReferences = typeof(App).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name?.StartsWith("HouseholdLedger.", StringComparison.Ordinal) == true);

        Assert.That(householdLedgerReferences, Is.EquivalentTo(ExpectedProductReferences));
    }

    private static string FindClientDirectory()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
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
