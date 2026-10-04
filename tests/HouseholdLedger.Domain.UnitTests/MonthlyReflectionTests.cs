// <copyright file="MonthlyReflectionTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.UnitTests;

using HouseholdLedger.Domain.Planning;
using Xunit;

/// <summary>Verifies optional standalone reflection invariants.</summary>
public sealed class MonthlyReflectionTests
{
    /// <summary>Normalizes optional reflection text and preserves save time.</summary>
    [Fact]
    public void ReflectionNormalizesOptionalText()
    {
        var now = DateTimeOffset.UtcNow;
        var reflection = new MonthlyReflection(new DateOnly(2026, 9, 1), "  Cooked at home  ", " \t ", now);
        Assert.Equal("Cooked at home", reflection.WhatWorked);
        Assert.Null(reflection.NextMonthIntention);
        Assert.Equal(now, reflection.LastRevisedAt);
    }

    /// <summary>Accepts the maximum text length for both optional fields.</summary>
    [Fact]
    public void ReflectionAcceptsMaximumLength()
    {
        var text = new string('x', 1000);
        var reflection = new MonthlyReflection(new DateOnly(2026, 9, 1), text, text, DateTimeOffset.UtcNow);
        Assert.Equal(text, reflection.WhatWorked);
        Assert.Equal(text, reflection.NextMonthIntention);
    }

    /// <summary>Rejects either oversized field.</summary>
    /// <param name="firstField">Whether to exceed the first field's limit.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReflectionRejectsOversizedText(bool firstField)
    {
        var text = new string('x', 1001);
        Assert.Throws<ArgumentException>(() =>
            new MonthlyReflection(new DateOnly(2026, 9, 1), firstField ? text : null, firstField ? null : text, DateTimeOffset.UtcNow));
    }
}
