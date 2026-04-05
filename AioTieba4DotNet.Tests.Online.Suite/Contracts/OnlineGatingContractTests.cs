#nullable enable
using System;
using System.Collections.Generic;
using AioTieba4DotNet.Tests.Infrastructure.Configuration;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;
using AioTieba4DotNet.Tests.Infrastructure.Execution;
using AioTieba4DotNet.Tests.Infrastructure.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Suite.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Gating)]
public sealed class OnlineGatingContractTests
{
    [TestMethod]
    [TestCategory(OnlineTestTierCategories.Safe)]
    [TestCategory(OnlineTestCapabilityCategories.Authenticated)]
    public void SafeAuthenticatedGateRequiresDedicatedSafeCredentialsBeforeExecution()
    {
        var environment = CreateEnvironment();
        var mutationAttempted = false;

        var exception = Assert.ThrowsExactly<AssertInconclusiveException>(
            () => OnlineExecutionGate.ExecuteSafe(
                environment,
                "safe authenticated gate contract probe",
                _ => mutationAttempted = true,
                OnlineExecutionCapability.Authenticated));

        Assert.IsFalse(mutationAttempted, "Safe authenticated gate must stop before any guarded body executes.");
        Assert.Contains("dedicated safe credentials", exception.Message);
        Assert.Contains(OnlineTestEnvironmentVariables.SafeAccountBduss, exception.Message);
    }

    [TestMethod]
    [TestCategory(OnlineTestTierCategories.Safe)]
    [TestCategory(OnlineTestCapabilityCategories.Messaging)]
    public void SafeMessagingGateRequiresDedicatedSafeCredentialsBeforeExecution()
    {
        var environment = CreateEnvironment();
        var mutationAttempted = false;

        var exception = Assert.ThrowsExactly<AssertInconclusiveException>(
            () => OnlineExecutionGate.ExecuteSafe(
                environment,
                "safe messaging gate contract probe",
                _ => mutationAttempted = true,
                OnlineExecutionCapability.Messaging));

        Assert.IsFalse(mutationAttempted, "Safe messaging gate must stop before any guarded body executes.");
        Assert.Contains("safe messaging execution", exception.Message);
        Assert.Contains(OnlineTestEnvironmentVariables.SafeAccountBduss, exception.Message);
    }

    [TestMethod]
    [TestCategory(OnlineTestTierCategories.Restricted)]
    [TestCategory(OnlineTestCapabilityCategories.Moderation)]
    public void RestrictedModerationGateRequiresExplicitOptInBeforeCredentialsOrCapability()
    {
        var environment = CreateEnvironment(
            (OnlineTestEnvironmentVariables.RestrictedAccountBduss, "restricted-bduss"),
            (OnlineTestEnvironmentVariables.RestrictedCapabilitiesModeration, "true"));
        var mutationAttempted = false;

        var exception = Assert.ThrowsExactly<AssertInconclusiveException>(
            () => OnlineExecutionGate.ExecuteRestricted(
                environment,
                "restricted moderation gate contract probe",
                OnlineExecutionCapability.Moderation,
                _ => mutationAttempted = true));

        Assert.IsFalse(mutationAttempted, "Restricted moderation gate must stop before any guarded body executes.");
        Assert.Contains("explicit opt-in", exception.Message);
        Assert.Contains(OnlineTestEnvironmentVariables.RestrictedOptIn, exception.Message);
    }

    [TestMethod]
    [TestCategory(OnlineTestTierCategories.Restricted)]
    [TestCategory(OnlineTestCapabilityCategories.Moderation)]
    public void RestrictedModerationGateRequiresDedicatedRestrictedCredentialsAfterOptIn()
    {
        var environment = CreateEnvironment(
            (OnlineTestEnvironmentVariables.RestrictedOptIn, "true"),
            (OnlineTestEnvironmentVariables.RestrictedCapabilitiesModeration, "true"));
        var mutationAttempted = false;

        var exception = Assert.ThrowsExactly<AssertInconclusiveException>(
            () => OnlineExecutionGate.ExecuteRestricted(
                environment,
                "restricted moderation gate contract probe",
                OnlineExecutionCapability.Moderation,
                _ => mutationAttempted = true));

        Assert.IsFalse(mutationAttempted, "Restricted moderation gate must stop before any guarded body executes.");
        Assert.Contains("dedicated restricted credentials", exception.Message);
        Assert.Contains(OnlineTestEnvironmentVariables.RestrictedAccountBduss, exception.Message);
    }

    [TestMethod]
    [TestCategory(OnlineTestTierCategories.Restricted)]
    [TestCategory(OnlineTestCapabilityCategories.Moderation)]
    public void RestrictedModerationGateRequiresCapabilityAfterOptInAndCredentials()
    {
        var environment = CreateEnvironment(
            (OnlineTestEnvironmentVariables.RestrictedOptIn, "true"),
            (OnlineTestEnvironmentVariables.RestrictedAccountBduss, "restricted-bduss"));
        var mutationAttempted = false;

        var exception = Assert.ThrowsExactly<AssertInconclusiveException>(
            () => OnlineExecutionGate.ExecuteRestricted(
                environment,
                "restricted moderation gate contract probe",
                OnlineExecutionCapability.Moderation,
                _ => mutationAttempted = true));

        Assert.IsFalse(mutationAttempted, "Restricted moderation gate must stop before any guarded body executes.");
        Assert.Contains("explicit capability opt-in", exception.Message);
        Assert.Contains(OnlineTestEnvironmentVariables.RestrictedCapabilitiesModeration, exception.Message);
    }

    [TestMethod]
    [TestCategory(OnlineTestTierCategories.Restricted)]
    [TestCategory(OnlineTestCapabilityCategories.Admin)]
    public void RestrictedAdminGateRequiresCapabilityAfterOptInAndCredentials()
    {
        var environment = CreateEnvironment(
            (OnlineTestEnvironmentVariables.RestrictedOptIn, "true"),
            (OnlineTestEnvironmentVariables.RestrictedAccountBduss, "restricted-bduss"));
        var mutationAttempted = false;

        var exception = Assert.ThrowsExactly<AssertInconclusiveException>(
            () => OnlineExecutionGate.ExecuteRestricted(
                environment,
                "restricted admin gate contract probe",
                OnlineExecutionCapability.Admin,
                _ => mutationAttempted = true));

        Assert.IsFalse(mutationAttempted, "Restricted admin gate must stop before any guarded body executes.");
        Assert.Contains("explicit capability opt-in", exception.Message);
        Assert.Contains(OnlineTestEnvironmentVariables.RestrictedCapabilitiesAdmin, exception.Message);
    }

    [TestMethod]
    [TestCategory(OnlineTestTierCategories.Safe)]
    public void SafeReadOnlyGateAllowsExecutionWithoutCredentials()
    {
        var environment = CreateEnvironment();
        var executed = false;

        OnlineExecutionGate.ExecuteSafe(
            environment,
            "safe read-only gate contract probe",
            _ => executed = true,
            OnlineExecutionCapability.None);

        Assert.IsTrue(executed, "Safe read-only gating should not block credential-free execution.");
    }

    private static OnlineTestEnvironment CreateEnvironment(params (string Key, string? Value)[] overrides)
    {
        var environmentVariables = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in overrides)
            environmentVariables[key] = value;

        return OnlineTestEnvironment.LoadFromRepository(RepositoryPaths.FindRepositoryRoot(), environmentVariables);
    }
}
