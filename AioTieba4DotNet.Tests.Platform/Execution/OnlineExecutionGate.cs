#nullable enable
using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Tests.Platform.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Platform.Execution;

public static class OnlineExecutionGate
{
    public static OnlineExecutionScope RequireSafe(
        OnlineTestEnvironment environment,
        string operationName,
        OnlineExecutionCapability capability = OnlineExecutionCapability.None,
        OnlineTestCompensationScope? compensation = null)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        EnsureSafeCapability(environment.Safe, capability, operationName);
        return new OnlineExecutionScope(environment, capability, false, compensation ?? new OnlineTestCompensationScope());
    }

    public static OnlineExecutionScope RequireRestricted(
        OnlineTestEnvironment environment,
        string operationName,
        OnlineExecutionCapability capability,
        OnlineTestCompensationScope? compensation = null)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        EnsureRestrictedOptIn(environment.Restricted, operationName);
        EnsureRestrictedAccount(environment.Restricted, operationName);
        EnsureRestrictedCapability(environment.Restricted, capability, operationName);

        return new OnlineExecutionScope(environment, capability, true, compensation ?? new OnlineTestCompensationScope());
    }

    public static void ExecuteSafe(
        OnlineTestEnvironment environment,
        string operationName,
        Action<OnlineExecutionScope> action,
        OnlineExecutionCapability capability = OnlineExecutionCapability.None,
        OnlineTestCompensationScope? compensation = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        var scope = RequireSafe(environment, operationName, capability, compensation);
        action(scope);
    }

    public static async Task ExecuteSafeAsync(
        OnlineTestEnvironment environment,
        string operationName,
        Func<OnlineExecutionScope, Task> action,
        OnlineExecutionCapability capability = OnlineExecutionCapability.None,
        OnlineTestCompensationScope? compensation = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        var scope = RequireSafe(environment, operationName, capability, compensation);
        await action(scope);
    }

    public static void ExecuteRestricted(
        OnlineTestEnvironment environment,
        string operationName,
        OnlineExecutionCapability capability,
        Action<OnlineExecutionScope> action,
        OnlineTestCompensationScope? compensation = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        var scope = RequireRestricted(environment, operationName, capability, compensation);
        action(scope);
    }

    public static async Task ExecuteRestrictedAsync(
        OnlineTestEnvironment environment,
        string operationName,
        OnlineExecutionCapability capability,
        Func<OnlineExecutionScope, Task> action,
        OnlineTestCompensationScope? compensation = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        var scope = RequireRestricted(environment, operationName, capability, compensation);
        await action(scope);
    }

    private static void EnsureSafeCapability(
        OnlineSafeProfile safe,
        OnlineExecutionCapability capability,
        string operationName)
    {
        switch (capability)
        {
            case OnlineExecutionCapability.None:
                return;
            case OnlineExecutionCapability.Authenticated:
                EnsureSafeAccount(
                    safe,
                    operationName,
                    "safe authenticated execution requires dedicated safe credentials");
                return;
            case OnlineExecutionCapability.Messaging:
                EnsureSafeAccount(
                    safe,
                    operationName,
                    "safe messaging execution requires dedicated safe credentials");
                return;
            case OnlineExecutionCapability.Moderation:
            case OnlineExecutionCapability.Admin:
                throw new InvalidOperationException(
                    $"Capability '{capability}' cannot be satisfied from the safe execution tier.");
            default:
                throw new ArgumentOutOfRangeException(nameof(capability), capability, null);
        }
    }

    private static void EnsureSafeAccount(
        OnlineSafeProfile safe,
        string operationName,
        string requirementDescription)
    {
        if (!safe.Account.IsConfigured)
            Assert.Inconclusive(
                $"Skipping {operationName}: {requirementDescription}. Set {OnlineTestEnvironmentVariables.SafeAccountBduss} (and {OnlineTestEnvironmentVariables.SafeAccountStoken} when the scenario needs STOKEN).");
    }

    private static void EnsureRestrictedOptIn(OnlineRestrictedProfile restricted, string operationName)
    {
        if (!restricted.OptIn)
            Assert.Inconclusive(
                $"Skipping {operationName}: restricted execution requires explicit opt-in. Set {OnlineTestEnvironmentVariables.RestrictedOptIn}=true.");
    }

    private static void EnsureRestrictedAccount(OnlineRestrictedProfile restricted, string operationName)
    {
        if (!restricted.Account.IsConfigured)
            Assert.Inconclusive(
                $"Skipping {operationName}: restricted execution requires dedicated restricted credentials. Set {OnlineTestEnvironmentVariables.RestrictedAccountBduss} (and {OnlineTestEnvironmentVariables.RestrictedAccountStoken} when the scenario needs STOKEN).");
    }

    private static void EnsureRestrictedCapability(
        OnlineRestrictedProfile restricted,
        OnlineExecutionCapability capability,
        string operationName)
    {
        switch (capability)
        {
            case OnlineExecutionCapability.None:
            case OnlineExecutionCapability.Authenticated:
            case OnlineExecutionCapability.Messaging:
                return;
            case OnlineExecutionCapability.Moderation:
                if (!restricted.Capabilities.Moderation)
                    Assert.Inconclusive(
                        $"Skipping {operationName}: restricted moderation requires explicit capability opt-in. Set {OnlineTestEnvironmentVariables.RestrictedCapabilitiesModeration}=true.");
                return;
            case OnlineExecutionCapability.Admin:
                if (!restricted.Capabilities.Admin)
                    Assert.Inconclusive(
                        $"Skipping {operationName}: restricted admin execution requires explicit capability opt-in. Set {OnlineTestEnvironmentVariables.RestrictedCapabilitiesAdmin}=true.");
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(capability), capability, null);
        }
    }
}
