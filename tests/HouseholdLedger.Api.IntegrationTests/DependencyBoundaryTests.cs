// <copyright file="DependencyBoundaryTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Api.IntegrationTests;

using Xunit;

/// <summary>
/// Verifies API assembly dependency boundaries.
/// </summary>
public sealed class DependencyBoundaryTests
{
    /// <summary>
    /// Verifies that the API can be built and hosted without the built-in client assembly.
    /// </summary>
    [Fact]
    public void ApiDoesNotReferenceClient()
    {
        var references = typeof(Program).Assembly.GetReferencedAssemblies();
        var referenceNames = references.Select(reference => reference.Name);

        Assert.DoesNotContain("HouseholdLedger.Client", referenceNames);
    }
}
