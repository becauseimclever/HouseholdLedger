// <copyright file="MonthlyReflectionServiceTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.UnitTests;

using HouseholdLedger.Application.Planning;
using HouseholdLedger.Domain.Planning;
using Xunit;

/// <summary>Verifies standalone reflection persistence without budget dependencies.</summary>
public sealed class MonthlyReflectionServiceTests
{
    /// <summary>Saves, reloads and clears a reflection without a budget plan.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task SavesReloadsAndClearsStandaloneReflection()
    {
        var now = new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
        var repository = new ReflectionRepository();
        var service = new MonthlyReflectionService(repository, new FixedClock(now));
        var cancellationToken = TestContext.Current.CancellationToken;
        Assert.Null(await service.GetAsync(2026, 9, cancellationToken));
        var saved = await service.SaveAsync(2026, 9, "  Cooked  ", "  Plan meals  ", cancellationToken);
        Assert.Equal("Cooked", saved.WhatWorked);
        Assert.Equal("Plan meals", saved.NextMonthIntention);
        Assert.Equal(now, saved.LastRevisedAt);
        Assert.Same(saved, await service.GetAsync(2026, 9, cancellationToken));
        var cleared = await service.SaveAsync(2026, 9, null, " ", cancellationToken);
        Assert.Null(cleared.WhatWorked);
        Assert.Null(cleared.NextMonthIntention);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class ReflectionRepository : IMonthlyReflectionRepository
    {
        private readonly Dictionary<DateOnly, MonthlyReflection> reflections = [];

        public Task<MonthlyReflection?> FindAsync(DateOnly month, CancellationToken cancellationToken) =>
            Task.FromResult(this.reflections.GetValueOrDefault(month));

        public Task UpsertAsync(MonthlyReflection reflection, CancellationToken cancellationToken)
        {
            this.reflections[reflection.Month] = reflection;
            return Task.CompletedTask;
        }
    }
}
