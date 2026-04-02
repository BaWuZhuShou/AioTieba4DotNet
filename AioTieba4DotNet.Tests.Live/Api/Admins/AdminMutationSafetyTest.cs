using System;
using AioTieba4DotNet.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.Admins;

[TestClass]
[TestCategory(TestCategoryNames.Live)]
[TestCategory(TestCategoryNames.ForumExtensions)]
public sealed class AdminMutationSafetyTest : TestBase
{
    [TestMethod]
    public void AdminMutationManualGate_IsExplicitlyRequired_ForLiveTask13Mutations()
    {
        EnsureAdminMutationManualGate(nameof(AdminMutationManualGate_IsExplicitlyRequired_ForLiveTask13Mutations));

        Assert.IsTrue(EnableAdminMutationTests);
        Console.WriteLine($"adminMutationGate={EnableAdminMutationTests}");
    }

    [TestMethod]
    public void SafeTargetUserNameFixture_IsExplicitlyRequired_ForLiveAddBaWuMutations()
    {
        EnsureAdminMutationManualGate(nameof(SafeTargetUserNameFixture_IsExplicitlyRequired_ForLiveAddBaWuMutations));
        var userName =
            RequireSafeTargetUserNameFixture(
                nameof(SafeTargetUserNameFixture_IsExplicitlyRequired_ForLiveAddBaWuMutations));

        Assert.IsFalse(string.IsNullOrWhiteSpace(userName));
        Console.WriteLine($"safeTargetUserName={userName}");
    }

    [TestMethod]
    public void SafeTargetUserIdFixture_IsExplicitlyRequired_ForLiveBlacklistAndUnblockMutations()
    {
        EnsureAdminMutationManualGate(
            nameof(SafeTargetUserIdFixture_IsExplicitlyRequired_ForLiveBlacklistAndUnblockMutations));
        var userId =
            RequireSafeTargetUserIdFixture(
                nameof(SafeTargetUserIdFixture_IsExplicitlyRequired_ForLiveBlacklistAndUnblockMutations));

        Assert.IsGreaterThan(0L, userId);
        Console.WriteLine($"safeTargetUserId={userId}");
    }

    [TestMethod]
    public void SafeTargetPortraitFixture_IsExplicitlyRequired_ForLiveBaWuRollbackMutations()
    {
        EnsureAdminMutationManualGate(
            nameof(SafeTargetPortraitFixture_IsExplicitlyRequired_ForLiveBaWuRollbackMutations));
        var portrait =
            RequireSafeTargetPortraitFixture(
                nameof(SafeTargetPortraitFixture_IsExplicitlyRequired_ForLiveBaWuRollbackMutations));

        Assert.IsFalse(string.IsNullOrWhiteSpace(portrait));
        Console.WriteLine($"safeTargetPortrait={portrait}");
    }
}
