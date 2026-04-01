#nullable enable
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Testing;

public static class TestFixtureGates
{
    public static void EnsureAuthenticated(TiebaTestEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        if (!environment.HasCredentials)
            Assert.Inconclusive("Skipping test: BDUSS is not configured.");
    }

    public static long RequirePositiveLong(TiebaTestEnvironment environment, long? value, string operationName,
        string fixtureDescription, string configurationHint)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureDescription);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationHint);

        EnsureAuthenticated(environment);
        if (value is > 0) return value.Value;

        Assert.Inconclusive(
            $"Skipping {operationName}: no {fixtureDescription} is configured. Set {configurationHint} for the safe forum {environment.ConfiguredCanonicalSafeForumName}.");
        return default;
    }

    public static string RequireNonEmptyString(TiebaTestEnvironment environment, string? value, string operationName,
        string fixtureDescription, string configurationHint)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureDescription);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationHint);

        EnsureAuthenticated(environment);
        if (!string.IsNullOrWhiteSpace(value)) return value;

        Assert.Inconclusive(
            $"Skipping {operationName}: no {fixtureDescription} is configured. Set {configurationHint}.");
        return string.Empty;
    }

    public static void EnsureAdminMutationManualGate(TiebaTestEnvironment environment, string operationName)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        EnsureAuthenticated(environment);
        if (environment.EnableAdminMutationTests)
            return;

        Assert.Inconclusive(
            $"Skipping {operationName}: destructive admin verification requires an explicit manual gate. Set TIEBA_ENABLEADMINMUTATIONTESTS=true or TieBa:EnableAdminMutationTests=true only for an approved safe fixture run.");
    }
}
