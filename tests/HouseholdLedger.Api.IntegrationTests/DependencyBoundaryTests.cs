// <copyright file="DependencyBoundaryTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using HouseholdLedger.Api.Contracts;
using NUnit.Framework;

/// <summary>
/// Verifies API assembly dependency boundaries.
/// </summary>
public sealed class DependencyBoundaryTests
{
    /// <summary>
    /// Verifies that the API can be built and hosted without the built-in client assembly.
    /// </summary>
    [Test]
    public void ApiDoesNotReferenceClient()
    {
        var references = typeof(Program).Assembly.GetReferencedAssemblies();
        var referenceNames = references.Select(reference => reference.Name);

        Assert.That(referenceNames, Does.Not.Contain("HouseholdLedger.Client"));
    }

    /// <summary>
    /// Verifies that the .NET convenience contract has no implementation assembly dependency.
    /// </summary>
    [Test]
    public void ApiContractsDoesNotReferenceImplementationAssemblies()
    {
        var references = typeof(HealthResponse).Assembly.GetReferencedAssemblies();
        var householdLedgerReferences = references
            .Select(reference => reference.Name)
            .Where(name => name?.StartsWith("HouseholdLedger.", StringComparison.Ordinal) == true);

        Assert.That(householdLedgerReferences, Is.Empty);
    }
}
